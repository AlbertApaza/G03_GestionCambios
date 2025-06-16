using System;
using System.Web;
using System.Web.Mvc;
using G03_GestionDeCambios.Models;
using G03_GestionDeCambios.Service;
using System.IO;
using Renci.SshNet;
using System.Diagnostics;
using System.Data.Entity.Validation;
using System.Linq;

namespace G03_GestionDeCambios.Controllers
{
    public class DocumentosController : Controller
    {
        private readonly DocumentoService _documentoService;

        public DocumentosController()
        {
            _documentoService = new DocumentoService();
        }

        public ActionResult Index(int idProyecto)
        {
            if (Session["idUsuario"] == null)
            {
                return RedirectToAction("Index", "Login", new { returnUrl = Url.Action("Index", "Documentos", new { idProyecto }) });
            }
            ViewBag.ProjectId = idProyecto;
            var proyecto = _documentoService.GetProyectoConCicloActual(idProyecto);
            if (proyecto == null)
            {
                TempData["Error"] = "Proyecto no encontrado.";
                return RedirectToAction("Index", "Proyecto");
            }
            bool puedeSubir = !string.IsNullOrWhiteSpace(proyecto.codCicloActual);
            var viewModel = new DocumentosIndexViewModel
            {
                IdProyecto = idProyecto,
                NombreProyecto = proyecto.nombre,
                CodCicloActual = proyecto.codCicloActual,
                NombreCicloActual = proyecto.tbCiclos?.nombre ?? "N/D",
                Documentos = _documentoService.GetDocumentosPorProyectoYCiclo(idProyecto, proyecto.codCicloActual),
                PuedeSubirDocumentos = puedeSubir
            };
            if (TempData["SuccessMessage"] != null) ViewBag.SuccessMessage = TempData["SuccessMessage"].ToString();
            if (TempData["ErrorMessage"] != null) ViewBag.ErrorMessage = TempData["ErrorMessage"].ToString();
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SubirDocumento(DocumentosIndexViewModel model)
        {
            if (Session["idUsuario"] == null)
            {
                return Json(new { success = false, message = "Sesión expirada. Por favor, inicie sesión." });
            }
            int idUsuarioSubida = Convert.ToInt32(Session["idUsuario"]);
            var proyecto = _documentoService.GetProyectoConCicloActual(model.IdProyecto);
            if (proyecto == null || string.IsNullOrWhiteSpace(proyecto.codCicloActual))
            {
                return Json(new { success = false, message = "No se puede subir el documento. El proyecto no tiene un ciclo actual definido o no existe." });
            }
            if (model.ArchivoSubido == null || model.ArchivoSubido.ContentLength == 0)
            {
                ModelState.AddModelError("ArchivoSubido", "Debe seleccionar un archivo.");
            }
            if (ModelState.IsValid)
            {
                string errorMessage = null;
                bool subido = false;
                try
                {
                    subido = _documentoService.SubirDocumento(
                       model.IdProyecto,
                       proyecto.codCicloActual,
                       model.ArchivoSubido,
                       model.VersionDocumento,
                       model.ComentariosDocumento,
                       idUsuarioSubida,
                       proyecto.nombre
                   );
                    if (subido)
                    {
                        return Json(new { success = true, message = "Documento subido exitosamente." });
                    }
                    else
                    {
                        errorMessage = "Error al subir el documento (falló la operación en el servicio). Revise los logs del servidor.";
                    }
                }
                catch (DbEntityValidationException dbEx)
                {
                    var errorMessages = dbEx.EntityValidationErrors
                        .SelectMany(x => x.ValidationErrors)
                        .Select(x => $"Propiedad: {x.PropertyName} Error: {x.ErrorMessage}");
                    errorMessage = "Error de validación al guardar en BD: " + string.Join("; ", errorMessages);
                    Debug.WriteLine("DbEntityValidationException en Controller: " + errorMessage);
                }
                catch (System.Data.SqlClient.SqlException sqlEx)
                {
                    errorMessage = $"Error de SQL Server ({sqlEx.Number}): {sqlEx.Message}. Revise los logs del servidor.";
                    Debug.WriteLine("SqlException en Controller: " + errorMessage);
                }
                catch (Exception ex)
                {
                    errorMessage = "Error inesperado al subir el documento: " + ex.Message;
                    Debug.WriteLine("Exception en Controller: " + errorMessage + " StackTrace: " + ex.ToString());
                }
                return Json(new { success = false, message = errorMessage ?? "Error desconocido al subir el documento." });
            }
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return Json(new { success = false, message = "Datos inválidos. Por favor, corrija los errores.", errors = errors });
        }

        public ActionResult DescargarDocumento(int idDocumento)
        {
            if (Session["idUsuario"] == null)
            {
                return new HttpUnauthorizedResult();
            }
            string rutaCompletaEnVps;
            string nombreOriginalParaCliente;
            var docInfoDb = _documentoService.GetDocumentoParaDescarga(idDocumento, out rutaCompletaEnVps, out nombreOriginalParaCliente);
            if (docInfoDb == null || string.IsNullOrWhiteSpace(rutaCompletaEnVps))
            {
                TempData["ErrorMessage"] = "Documento no encontrado o información inválida para la descarga.";
                if (docInfoDb != null && docInfoDb.idProyecto.HasValue)
                    return RedirectToAction("Index", new { idProyecto = docInfoDb.idProyecto.Value });
                return RedirectToAction("Index", "Proyecto");
            }
            string vpsHost = "161.132.38.250";
            string vpsUsername = "root";
            string vpsPassword = "patitochera123";
            try
            {
                using (var client = new SftpClient(vpsHost, vpsUsername, vpsPassword))
                {
                    client.Connect();
                    if (!client.Exists(rutaCompletaEnVps))
                    {
                        TempData["ErrorMessage"] = "El archivo no existe en el servidor remoto.";
                        return RedirectToAction("Index", "Documentos", new { idProyecto = docInfoDb.idProyecto });
                    }
                    var memoryStream = new MemoryStream();
                    client.DownloadFile(rutaCompletaEnVps, memoryStream);
                    memoryStream.Position = 0;
                    client.Disconnect();
                    string contentType = MimeMapping.GetMimeMapping(nombreOriginalParaCliente);
                    return File(memoryStream, contentType, nombreOriginalParaCliente);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error descargando archivo {idDocumento} desde VPS: {ex.ToString()}");
                TempData["ErrorMessage"] = "Error al intentar descargar el archivo desde el servidor: " + ex.Message;
                return RedirectToAction("Index", "Documentos", new { idProyecto = docInfoDb.idProyecto });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EliminarDocumento(int idDocumento, int idProyecto)
        {
            if (Session["idUsuario"] == null)
            {
                return Json(new { success = false, message = "Sesión expirada." });
            }

            string mensajeErrorServicio;
            bool eliminado = _documentoService.EliminarDocumento(idDocumento, out mensajeErrorServicio);

            if (eliminado)
            {
                TempData["SuccessMessage"] = "Documento eliminado exitosamente.";
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = true, message = "Documento eliminado exitosamente." });
                }
            }
            else
            {
                TempData["ErrorMessage"] = mensajeErrorServicio;
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = mensajeErrorServicio });
                }
            }
            return RedirectToAction("Index", new { idProyecto = idProyecto });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _documentoService.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}