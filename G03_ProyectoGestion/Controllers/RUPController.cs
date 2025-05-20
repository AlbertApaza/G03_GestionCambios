using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Renci.SshNet;
using G03_ProyectoGestion.Models; // Asegúrate que los modelos de EF están aquí
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
                // Manejo robusto de sesión
                var userId = Session["idUsuario"];
                if (userId == null || !int.TryParse(userId.ToString(), out int parsedId))
                {
                    return 0; // O un valor que indique sesión inválida
                }
                return parsedId;
            }
        }

        public ActionResult Index(int? id)
        {
            if (DEFAULT_USER_ID == 0)
            {
                TempData["ErrorMessage"] = "Sesión expirada o inválida. Por favor, inicie sesión nuevamente.";
                return RedirectToAction("Login", "Home");
            }

            if (!id.HasValue)
            {
                TempData["ErrorMessage"] = "No se especificó un proyecto para gestionar.";
                return RedirectToAction("Index", "Proyecto");
            }

            var project = db.tbProyectos
                            .Where(p => p.idProyecto == id.Value &&
                                        p.idMetodologia == RUP_METHODOLOGY_ID &&
                                        p.tbProyectoUsuarios.Any(pu => pu.idUsuario == DEFAULT_USER_ID && pu.idProyecto == id.Value))
                            .Select(p => new ProjectViewModel
                            {
                                id = p.idProyecto,
                                name = p.nombreProyecto,
                                scope = p.descripcionProyecto,
                                current_phase = p.idFase ?? 0 // Usar 0 o un ID de fase por defecto si es null
                            })
                            .FirstOrDefault();

            if (project == null)
            {
                TempData["ErrorMessage"] = "Proyecto no encontrado, no es un proyecto RUP válido o no tiene acceso.";
                return RedirectToAction("Index", "Proyecto");
            }
            // Si current_phase es 0 (o un valor por defecto inválido) y hay fases, asigna la primera fase.
            if (project.current_phase == 0)
            {
                var firstPhase = db.tbRupFases.OrderBy(f => f.idFase).FirstOrDefault();
                if (firstPhase != null)
                {
                    project.current_phase = firstPhase.idFase;
                    // Actualizar en BD
                    var dbProject = db.tbProyectos.Find(project.id);
                    if (dbProject != null)
                    {
                        dbProject.idFase = firstPhase.idFase;
                        db.SaveChanges();
                    }
                }
                else
                {
                    // No hay fases definidas, esto es un problema de configuración
                    TempData["ErrorMessage"] = "No hay fases RUP definidas en el sistema. Contacte al administrador.";
                    // Podrías redirigir o mostrar una vista de error específica.
                    // Por ahora, lo dejamos para que la vista lo maneje si es necesario.
                }
            }


            ViewBag.Title = $"Gestor RUP - {project.name}";
            ViewBag.SelectedProjectData = project;
            ViewBag.ProjectId = project.id;

            ViewBag.Phases = db.tbRupFases
                                .OrderBy(f => f.idFase) // Asegurar orden
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

        // ELIMINADO: GetIterationsForPhase
        // ELIMINADO: CreateIteration
        // ELIMINADO: UpdateIterationStatus

        [HttpGet]
        public JsonResult GetActivitiesForPhase(int projectId, int phaseId) // Cambiado
        {
            var result = _rupService.ObtenerActividadesPorFase(projectId, phaseId);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult CreateActivity(ActivityCreatePostModel activityData)
        {
            if (ModelState.IsValid)
            {
                // Asignar ProjectId y PhaseId desde el modelo que ya los tiene
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
        public JsonResult GetDocumentsForPhase(int projectId, int phaseId) // Cambiado
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

            // Validar que el proyecto y la fase existen y son coherentes
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
                string remoteProjectFolder = $"Proyecto_{documentData.ProjectId}"; // Usar ProjectId
                string remotePhaseFolder = $"Fase_{documentData.PhaseId}";       // Usar PhaseId
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
                        using (var sshClient = new SshClient(connectionInfo)) // Para crear directorios
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
                        idProyecto = documentData.ProjectId, // Cambiado
                        idFase = documentData.PhaseId,       // Cambiado
                        idTipoDocumento = tipoDocumento.idTipoDocumento,
                        nombreArchivo = originalFileName,
                        rutaArchivo = remoteFilePathOnVps,
                        Version = documentData.Version,
                        Estado = "Pendiente", // O el estado por defecto que prefieras
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
                    // Loggear ex.ToString() para más detalles en el servidor
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
                // Redirigir al index del proyecto actual si es posible, o a la lista general de proyectos
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
            // Usar un nombre de archivo temporal único para evitar colisiones
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
            catch (Renci.SshNet.Common.SftpPathNotFoundException) // SftpPathNotFoundException o genérica si no es SFTP
            {
                TempData["ErrorMessage"] = "El archivo no fue encontrado en el servidor remoto.";
            }
            catch (Renci.SshNet.Common.SshAuthenticationException authEx)
            {
                TempData["ErrorMessage"] = "Error de autenticación SSH con el VPS: " + authEx.Message;
            }
            catch (Exception ex)
            {
                // Loggear ex.ToString() para más detalles en el servidor
                TempData["ErrorMessage"] = "Ocurrió un error al intentar descargar el archivo: " + ex.Message;
            }
            finally
            {
                if (System.IO.File.Exists(tempLocalFilePath))
                {
                    System.IO.File.Delete(tempLocalFilePath);
                }
            }
            // Si hubo error, redirigir de vuelta al proyecto
            var projIdForRedirect = (db.tbRupDocumentos.Where(d => d.idDocumento == documentId).Select(d => (int?)d.idProyecto).FirstOrDefault());
            return projIdForRedirect.HasValue ? RedirectToAction("Index", new { id = projIdForRedirect.Value }) : RedirectToAction("Index", "Proyecto");
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