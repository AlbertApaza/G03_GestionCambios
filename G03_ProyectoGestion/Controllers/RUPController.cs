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
using G03_ProyectoGestion.ViewModels;

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
                var userId = Session["idUsuario"];
                if (userId == null || !int.TryParse(userId.ToString(), out int parsedId))
                {
                    return 0;
                }
                return parsedId;
            }
        }

        public ActionResult Index(int? id)
        {

            var project = db.tbProyectos
                            .Where(p => p.idProyecto == id.Value &&
                                        p.idMetodologia == RUP_METHODOLOGY_ID &&
                                        p.tbProyectoUsuarios.Any(pu => pu.idUsuario == DEFAULT_USER_ID && pu.idProyecto == id.Value))
                            .Select(p => new ProjectViewModel
                            {
                                id = p.idProyecto,
                                name = p.nombreProyecto,
                                scope = p.descripcionProyecto,
                                current_phase = p.idFase ?? 0
                            })
                            .FirstOrDefault();

            if (project == null)
            {
                TempData["ErrorMessage"] = "Proyecto no encontrado, no es un proyecto RUP válido o no tiene acceso.";
                return RedirectToAction("Index", "Proyecto");
            }

            if (project.current_phase == 0)
            {
                var firstPhase = db.tbRupFases.OrderBy(f => f.idFase).FirstOrDefault();
                if (firstPhase != null)
                {
                    project.current_phase = firstPhase.idFase;
                    var dbProject = db.tbProyectos.Find(project.id);
                    if (dbProject != null)
                    {
                        dbProject.idFase = firstPhase.idFase;
                        db.SaveChanges();
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "No hay fases RUP definidas en el sistema. Contacte al administrador.";
                }
            }

            ViewBag.Title = $"Gestor RUP - {project.name}";
            ViewBag.SelectedProjectData = project;
            ViewBag.ProjectId = project.id;

            ViewBag.Phases = db.tbRupFases
                                .OrderBy(f => f.idFase)
                                .Select(p => new { id = p.idFase, name = p.nombre })
                                .ToList();
            // ELIMINADO: ViewBag.Roles
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

        // ELIMINADO: GetIterationsForPhase
        // ELIMINADO: CreateIteration
        // ELIMINADO: UpdateIterationStatus
        // ELIMINADO: GetActivitiesForPhase
        // ELIMINADO: CreateActivity
        // ELIMINADO: UpdateActivityStatus
        // ELIMINADO: GetUsersByRoleInProject

        [HttpGet]
        public JsonResult GetDocumentsForPhase(int projectId, int phaseId)
        {
            var result = _rupService.ObtenerDocumentosPorFase(projectId, phaseId);
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

            var project = db.tbProyectos.FirstOrDefault(p => p.idProyecto == documentData.ProjectId);
            if (project == null)
            {
                return Json(new { success = false, message = "Proyecto no encontrado para el documento." });
            }
            var phase = db.tbRupFases.FirstOrDefault(f => f.idFase == documentData.PhaseId);
            if (phase == null)
            {
                return Json(new { success = false, message = "Fase no encontrada para el documento." });
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
                string remoteProjectFolder = $"Proyecto_{documentData.ProjectId}";
                string remotePhaseFolder = $"Fase_{documentData.PhaseId}";
                string remoteFilePathOnVps = $"{remoteBaseUploadPath}/{remoteProjectFolder}/{remotePhaseFolder}/{uniqueRemoteFileName}";

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
                            sshClient.RunCommand($"mkdir -p {remoteBaseUploadPath}/{remoteProjectFolder}/{remotePhaseFolder}");
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
                        idProyecto = documentData.ProjectId,
                        idFase = documentData.PhaseId,
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
                        project_id = newDbDocument.idProyecto,
                        phase_id = newDbDocument.idFase,
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

        [HttpGet]
        public ActionResult DownloadDocument(int documentId)
        {
            var document = db.tbRupDocumentos.Find(documentId);
            if (document == null || string.IsNullOrEmpty(document.rutaArchivo))
            {
                TempData["ErrorMessage"] = "Documento no encontrado o ruta inválida.";
                var projectId = (db.tbRupDocumentos.Where(d => d.idDocumento == documentId).Select(d => (int?)d.idProyecto).FirstOrDefault());
                return projectId.HasValue ? RedirectToAction("Index", new { id = projectId.Value }) : RedirectToAction("Index", "Proyecto");
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
            }
            catch (Renci.SshNet.Common.SshAuthenticationException authEx)
            {
                TempData["ErrorMessage"] = "Error de autenticación SSH con el VPS: " + authEx.Message;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Ocurrió un error al intentar descargar el archivo: " + ex.Message;
            }
            finally
            {
                if (System.IO.File.Exists(tempLocalFilePath))
                {
                    System.IO.File.Delete(tempLocalFilePath);
                }
            }
            var projIdForRedirect = (db.tbRupDocumentos.Where(d => d.idDocumento == documentId).Select(d => (int?)d.idProyecto).FirstOrDefault());
            return projIdForRedirect.HasValue ? RedirectToAction("Index", new { id = projIdForRedirect.Value }) : RedirectToAction("Index", "Proyecto");
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