using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Renci.SshNet;
using G03_ProyectoGestion.Models;
using G03_ProyectoGestion.Services;

namespace G03_ProyectoGestion.Controllers
{
    public class RUPController : Controller
    {

        RupService _rupService = new RupService();

        private g03_databaseEntities db = new g03_databaseEntities();
        private const int RUP_METHODOLOGY_ID = 2;

        private int DEFAULT_USER_ID
        {
            get
            {
                return Convert.ToInt32(Session["idUsuario"]);
            }
        }

        public ActionResult Detalles()
        {
            return RedirectToAction("Index");
        }

        public ActionResult Index(int? id) // Recibe el ID del proyecto
        {
            if (DEFAULT_USER_ID == 0) // manejo de sesión inválida
            {
                TempData["ErrorMessage"] = "Sesión expirada o inválida. Por favor, inicie sesión nuevamente.";
                return RedirectToAction("Login", "Home"); // O tu controlador/acción de login
            }

            if (!id.HasValue)
            {
                TempData["ErrorMessage"] = "No se especificó un proyecto para gestionar.";
                return RedirectToAction("Index", "Proyecto"); // Redirige a tu lista principal de proyectos
            }

            // Validar que el proyecto existe, es RUP y pertenece al usuario (o tiene acceso)
            var project = db.tbProyectos
                            .Where(p => p.idProyecto == id.Value &&
                                        p.idMetodologia == RUP_METHODOLOGY_ID &&
                                        p.tbProyectoUsuarios.Any(pu => pu.idUsuario == DEFAULT_USER_ID && pu.idProyecto == id.Value)) // Asumiendo tbProyectoUsuarios para relación muchos-a-muchos
                            .Select(p => new ProjectViewModel // Un ViewModel simple para los datos iniciales del proyecto
                            {
                                id = p.idProyecto,
                                name = p.nombreProyecto,
                                scope = p.descripcionProyecto,
                                current_phase = p.idFase ?? 0 // Asegurar que no sea null, o manejarlo
                            })
                            .FirstOrDefault();


            if (project == null)
            {
                TempData["ErrorMessage"] = "Proyecto no encontrado, no es un proyecto RUP válido o no tiene acceso.";
                return RedirectToAction("Index", "Proyecto"); // Redirige a tu lista principal
            }

            ViewBag.Title = $"Gestor RUP - {project.name}";
            ViewBag.SelectedProjectData = project; // Pasar los datos del proyecto directamente
            ViewBag.ProjectId = project.id;       // Pasar el ID también para Alpine

            ViewBag.Phases = db.tbRupFases
                                .Select(p => new { id = p.idFase, name = p.nombre })
                                .ToList();
            ViewBag.Roles = db.tbRoles
                                .Select(r => new { id = r.idRol, name = r.nombreRol })
                                .ToList();
            ViewBag.DocumentTypes = db.tbRupTiposDocumento
                                .Select(dt => new { id = dt.idTipoDocumento, name = dt.nombre, clave = dt.clave })
                                .ToList();
            return View();
        }


        [HttpPost]
        public JsonResult UpdateProjectPhase(int projectId, int phaseId)
        {
            var success = _rupService.ActualizarFaseDelProyecto(projectId, phaseId);
            if (success)
            {
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "No se pudo actualizar la fase del proyecto." });
        }

        [HttpGet]
        public JsonResult GetIterationsForPhase(int projectId, int phaseId)
        {
            var result = _rupService.ObtenerIteracionesPorFase(projectId, phaseId);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        

        [HttpPost]
        public JsonResult CreateIteration(IterationCreatePostModel iterationData)
        {
            if (ModelState.IsValid)
            {
                var result = _rupService.CrearIteracion(iterationData);
                return Json(result);
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return Json(new { success = false, message = "Datos inválidos.", errors = errors });
        }

        [HttpPost]
        public JsonResult UpdateIterationStatus(int iterationId, string status)
        {
            var iteration = db.tbRupIteraciones.Find(iterationId);
            if (iteration == null) return Json(new { success = false, message = "Iteración no encontrada." });

            iteration.Estado = status;
            db.SaveChanges();
            return Json(new { success = true });
        }

        [HttpGet]
        public JsonResult GetActivitiesForIteration(int iterationId)
        {
            var result = _rupService.ObtenerActividadesPorIteracion(iterationId);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        

        [HttpPost]
        public JsonResult CreateActivity(ActivityCreatePostModel activityData)
        {
            if (ModelState.IsValid)
            {
                var result = _rupService.CrearActividad(activityData);
                return Json(result);
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return Json(new { success = false, message = "Datos inválidos.", errors = errors });
        }

        [HttpPost]
        public JsonResult UpdateActivityStatus(int activityId, string status)
        {
            var activity = db.tbRupActividades.Find(activityId);
            if (activity == null) return Json(new { success = false, message = "Actividad no encontrada." });

            activity.estado = status;
            db.SaveChanges();
            return Json(new { success = true });
        }

        [HttpGet]
        public JsonResult GetDocumentsForIteration(int iterationId)
        {
            var result = _rupService.ObtenerDocumentosPorIteracion(iterationId);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        

        [HttpPost]
        public JsonResult CreateDocument(DocumentCreatePostModel documentData, HttpPostedFileBase docFile)
        {
            if (docFile == null || docFile.ContentLength == 0)
            {
                return Json(new { success = false, message = "Archivo no adjuntado o vacío." });
            }

            var tipoDocumento = db.tbRupTiposDocumento.FirstOrDefault(td => td.clave == documentData.TypeClave);
            if (tipoDocumento == null)
            {
                return Json(new { success = false, message = "Tipo de documento inválido." });
            }

            var iteration = db.tbRupIteraciones.Find(documentData.IterationId);
            if (iteration == null)
            {
                return Json(new { success = false, message = "Iteración no encontrada para el documento." });
            }

            if (ModelState.IsValid)
            {
                var originalFileName = Path.GetFileName(docFile.FileName);
                var fileExtension = Path.GetExtension(originalFileName);
                var uniqueRemoteFileName = Guid.NewGuid().ToString() + fileExtension;

                string vpsHost = "161.132.38.250";
                string vpsUsername = "root";
                string vpsPassword = "patitochera123";

                string remoteBaseUploadPath = "/root/rup_manager/uploads";
                string remoteProjectFolder = $"Proyecto_{iteration.idProyecto}";
                string remoteIterationFolder = $"Iteracion_{iteration.idIteracion}";
                string remoteFilePathOnVps = $"{remoteBaseUploadPath}/{remoteProjectFolder}/{remoteIterationFolder}/{uniqueRemoteFileName}";

                string tempUploadDir = Server.MapPath("~/App_Data/TempUploadsForVPS");
                Directory.CreateDirectory(tempUploadDir);
                string tempFilePath = Path.Combine(tempUploadDir, Guid.NewGuid().ToString() + fileExtension);

                try
                {
                    docFile.SaveAs(tempFilePath);

                    var connectionInfo = new ConnectionInfo(vpsHost, vpsUsername,
                                            new PasswordAuthenticationMethod(vpsUsername, vpsPassword));

                    using (var scpClient = new ScpClient(connectionInfo))
                    {
                        scpClient.Connect();

                        using (var sshClient = new SshClient(connectionInfo))
                        {
                            sshClient.Connect();
                            sshClient.RunCommand($"mkdir -p {remoteBaseUploadPath}/{remoteProjectFolder}/{remoteIterationFolder}");
                            sshClient.Disconnect();
                        }

                        using (var fs = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read))
                        {
                            scpClient.Upload(fs, remoteFilePathOnVps);
                        }
                        scpClient.Disconnect();
                    }

                    tbRupDocumentos newDbDocument = new tbRupDocumentos
                    {
                        idIteracion = documentData.IterationId,
                        idTipoDocumento = tipoDocumento.idTipoDocumento,
                        nombreArchivo = originalFileName,
                        rutaArchivo = remoteFilePathOnVps,
                        Version = documentData.Version,
                        Estado = "Pendiente",
                        FechaSubida = DateTime.Now
                    };

                    db.tbRupDocumentos.Add(newDbDocument);
                    db.SaveChanges();

                    return Json(new
                    {
                        success = true,
                        id = newDbDocument.idDocumento,
                        iteration_id = newDbDocument.idIteracion,
                        type = tipoDocumento.clave,
                        file_name = newDbDocument.nombreArchivo,
                        version = newDbDocument.Version,
                        status = newDbDocument.Estado,
                        uploaded_at = newDbDocument.FechaSubida.ToString("o")
                    });
                }
                catch (Renci.SshNet.Common.SshAuthenticationException authEx)
                {
                    return Json(new { success = false, message = "Error de autenticación SSH con el VPS: " + authEx.Message });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Error al subir o registrar el documento: " + ex.Message });
                }
                finally
                {
                    if (System.IO.File.Exists(tempFilePath))
                    {
                        System.IO.File.Delete(tempFilePath);
                    }
                }
            }
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return Json(new { success = false, message = "Datos inválidos.", errors = errors });
        }

        [HttpPost]
        public JsonResult UpdateDocumentStatus(int documentId, string status)
        {
            var document = db.tbRupDocumentos.Find(documentId);
            if (document == null) return Json(new { success = false, message = "Documento no encontrado." });

            document.Estado = status;
            db.SaveChanges();
            return Json(new { success = true });
        }

        // -----comentario(seccion modificada)
        [HttpGet]
        public ActionResult DownloadDocument(int documentId)
        {
            var document = db.tbRupDocumentos.Find(documentId);
            if (document == null || string.IsNullOrEmpty(document.rutaArchivo))
            {
                TempData["ErrorMessage"] = "Documento no encontrado o ruta inválida.";
                return RedirectToAction("Index");
            }

            string vpsHost = "161.132.38.250";
            string vpsUsername = "root";
            string vpsPassword = "patitochera123";
            string remoteFilePathOnVps = document.rutaArchivo;
            string originalFileNameToDownload = document.nombreArchivo;

            string tempDownloadDir = Server.MapPath("~/App_Data/TempDownloads");
            Directory.CreateDirectory(tempDownloadDir);
            string tempLocalFilePath = Path.Combine(tempDownloadDir, Guid.NewGuid().ToString() + Path.GetExtension(originalFileNameToDownload));

            try
            {
                var connectionInfo = new ConnectionInfo(vpsHost, vpsUsername,
                                        new PasswordAuthenticationMethod(vpsUsername, vpsPassword));

                using (var scpClient = new ScpClient(connectionInfo))
                {
                    scpClient.Connect();
                    using (var fs = new FileStream(tempLocalFilePath, FileMode.Create, FileAccess.Write))
                    {
                        scpClient.Download(remoteFilePathOnVps, fs);
                    }
                    scpClient.Disconnect();
                }

                byte[] fileBytes = System.IO.File.ReadAllBytes(tempLocalFilePath);

                return File(fileBytes, MimeMapping.GetMimeMapping(originalFileNameToDownload), originalFileNameToDownload);
            }
            catch (Renci.SshNet.Common.SftpPathNotFoundException)
            {
                TempData["ErrorMessage"] = "El archivo no fue encontrado en el servidor remoto.";
                return RedirectToAction("Index");
            }
            catch (Renci.SshNet.Common.SshAuthenticationException authEx)
            {
                TempData["ErrorMessage"] = "Error de autenticación SSH con el VPS: " + authEx.Message;
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Ocurrió un error al intentar descargar el archivo: " + ex.Message;
                return RedirectToAction("Index");
            }
            finally
            {
                if (System.IO.File.Exists(tempLocalFilePath))
                {
                    System.IO.File.Delete(tempLocalFilePath);
                }
            }
        }


        [HttpGet]
        public JsonResult GetUsersByRoleInProject(int projectId, int roleId)
        {
            var result = _rupService.ObtenerUsuariosPorRolEnProyecto(projectId, roleId);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}