using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Renci.SshNet;
using G03_ProyectoGestion.Models;

namespace YourProjectName.Controllers
{
    public class RUPController : Controller
    {



        private g03_databaseEntities db = new g03_databaseEntities();
        private const int RUP_METHODOLOGY_ID = 2;
        //private const int DEFAULT_USER_ID = 1;

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
        public ActionResult Index()
        {
            ViewBag.Title = "Gestor RUP";
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

        [HttpGet]
        public JsonResult GetProjects()
        {
            var projects = db.tbProyectoUsuarios
                .Where(p => p.tbProyectos.idMetodologia == RUP_METHODOLOGY_ID && p.idUsuario == DEFAULT_USER_ID)
                .Select(p => new
                {
                    id = p.tbProyectos.idProyecto,
                    name = p.tbProyectos.nombreProyecto,
                    scope = p.tbProyectos.descripcionProyecto,
                    current_phase = p.tbProyectos.idFase
                })
                .ToList();
            return Json(projects, JsonRequestBehavior.AllowGet);
        }

        public class ProjectCreatePostModel
        {
            public string Name { get; set; }
            public string Scope { get; set; }
            public int InitialPhaseId { get; set; }
        }

        [HttpPost]
        public JsonResult CreateProject(ProjectCreatePostModel projectData)
        {
            if (ModelState.IsValid)
            {
                var phaseExists = db.tbRupFases.Any(f => f.idFase == projectData.InitialPhaseId);
                if (!phaseExists)
                {
                    return Json(new { success = false, message = "Fase inicial inválida." });
                }

                tbProyectos newDbProject = new tbProyectos
                {
                    nombreProyecto = projectData.Name,
                    descripcionProyecto = projectData.Scope,
                    idFase = projectData.InitialPhaseId,
                    idMetodologia = RUP_METHODOLOGY_ID,
                    idUsuario = DEFAULT_USER_ID,
                    fechaInicio = DateTime.Today
                };

                db.tbProyectos.Add(newDbProject);
                db.SaveChanges();
                return Json(new
                {
                    success = true,
                    id = newDbProject.idProyecto,
                    name = newDbProject.nombreProyecto,
                    scope = newDbProject.descripcionProyecto,
                    current_phase = newDbProject.idFase
                });
            }
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return Json(new { success = false, message = "Datos inválidos.", errors = errors });
        }

        [HttpPost]
        public JsonResult UpdateProjectPhase(int projectId, int phaseId)
        {
            var project = db.tbProyectos.FirstOrDefault(p => p.idProyecto == projectId && p.idMetodologia == RUP_METHODOLOGY_ID);
            if (project == null) return Json(new { success = false, message = "Proyecto RUP no encontrado." });

            var phaseExists = db.tbRupFases.Any(f => f.idFase == phaseId);
            if (!phaseExists) return Json(new { success = false, message = "Fase inválida." });

            project.idFase = phaseId;
            db.SaveChanges();
            return Json(new { success = true });
        }

        [HttpGet]
        public JsonResult GetIterationsForPhase(int projectId, int phaseId)
        {
            var iterations = db.tbRupIteraciones
                .Where(i => i.idProyecto == projectId && i.idFase == phaseId)
                .Select(i => new
                {
                    id = i.idIteracion,
                    project_id = i.idProyecto,
                    phase_id = i.idFase,
                    name = i.nombre,
                    objective = i.objetivo,
                    start_date = i.fechaInicio,
                    end_date = i.fechaFin,
                    status = i.Estado
                })
                .ToList();

            var result = iterations.Select(i => new {
                i.id,
                i.project_id,
                i.phase_id,
                i.name,
                i.objective,
                start_date = i.start_date?.ToString("yyyy-MM-dd"),
                end_date = i.end_date?.ToString("yyyy-MM-dd"),
                i.status
            }).ToList();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public class IterationCreatePostModel
        {
            public int ProjectId { get; set; }
            public int PhaseId { get; set; }
            public string Name { get; set; }
            public string Objective { get; set; }
            public DateTime? Start_Date { get; set; }
            public DateTime? End_Date { get; set; }
        }

        [HttpPost]
        public JsonResult CreateIteration(IterationCreatePostModel iterationData)
        {
            if (ModelState.IsValid)
            {
                tbRupIteraciones newDbIteration = new tbRupIteraciones
                {
                    idProyecto = iterationData.ProjectId,
                    idFase = iterationData.PhaseId,
                    nombre = iterationData.Name,
                    objetivo = iterationData.Objective,
                    fechaInicio = iterationData.Start_Date,
                    fechaFin = iterationData.End_Date,
                    Estado = "Planificada"
                };
                db.tbRupIteraciones.Add(newDbIteration);
                db.SaveChanges();
                return Json(new
                {
                    success = true,
                    id = newDbIteration.idIteracion,
                    project_id = newDbIteration.idProyecto,
                    phase_id = newDbIteration.idFase,
                    name = newDbIteration.nombre,
                    objective = newDbIteration.objetivo,
                    start_date = newDbIteration.fechaInicio?.ToString("yyyy-MM-dd"),
                    end_date = newDbIteration.fechaFin?.ToString("yyyy-MM-dd"),
                    status = newDbIteration.Estado
                });
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
            var activities = db.tbRupActividades
                .Where(a => a.idIteracion == iterationId)
                .Select(a => new
                {
                    id = a.idActividad,
                    iteration_id = a.idIteracion,
                    description = a.descripcion,
                    assigned_role = a.idRol, // Rol de contexto
                    status = a.estado,
                    due_date = a.fechaLimite,
                    // Obtener los usuarios asignados
                    assigned_users = db.tbRupActividadAsignaciones
                                       .Where(aa => aa.idActividad == a.idActividad)
                                       .Select(aa => new {
                                           id = aa.tbUsuarios.idUsuario,
                                           name = aa.tbUsuarios.nombreUsuario
                                       }).ToList()
                })
                .ToList();

            var result = activities.Select(a => new {
                a.id,
                a.iteration_id,
                a.description,
                a.assigned_role, // Rol de contexto
                a.status,
                due_date = a.due_date?.ToString("yyyy-MM-dd"),
                a.assigned_users
            }).ToList();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public class ActivityCreatePostModel
        {
            public int IterationId { get; set; }
            public string Description { get; set; }
            public int ContextRoleId { get; set; } // El rol general de la actividad
            public List<int> AssignedUserIds { get; set; } // IDs de los usuarios asignados
            public string Status { get; set; }
            public DateTime? Due_Date { get; set; }
        }

        [HttpPost]
        public JsonResult CreateActivity(ActivityCreatePostModel activityData)
        {
            if (ModelState.IsValid)
            {
                if (activityData.AssignedUserIds == null || !activityData.AssignedUserIds.Any())
                {
                    return Json(new { success = false, message = "Debe asignar al menos un usuario a la actividad." });
                }

                // Validar que los usuarios asignados existen y tienen el rol de contexto en el proyecto
                var iteration = db.tbRupIteraciones.Find(activityData.IterationId);
                if (iteration == null)
                {
                    return Json(new { success = false, message = "Iteración no encontrada." });
                }
                int projectId = iteration.idProyecto;

                foreach (var userId in activityData.AssignedUserIds)
                {
                    var userInProjectWithRole = db.tbProyectoUsuarios
                                                  .Any(pu => pu.idProyecto == projectId &&
                                                             pu.idUsuario == userId &&
                                                             pu.idRol == activityData.ContextRoleId);
                    if (!userInProjectWithRole)
                    {
                        var user = db.tbUsuarios.Find(userId);
                        var role = db.tbRoles.Find(activityData.ContextRoleId);
                        return Json(new { success = false, message = $"El usuario '{user?.nombreUsuario ?? "Desconocido"}' no tiene el rol '{role?.nombreRol ?? "Desconocido"}' en este proyecto o no existe." });
                    }
                }


                tbRupActividades newDbActivity = new tbRupActividades
                {
                    idIteracion = activityData.IterationId,
                    descripcion = activityData.Description,
                    idRol = activityData.ContextRoleId, // Rol de contexto de la actividad
                    estado = string.IsNullOrEmpty(activityData.Status) ? "Pendiente" : activityData.Status,
                    fechaLimite = activityData.Due_Date
                    // idActividad se autogenerará si está configurado como IDENTITY en la BD
                };
                db.tbRupActividades.Add(newDbActivity);
                db.SaveChanges(); // Guardar actividad para obtener su ID

                // Guardar las asignaciones de usuarios
                foreach (var userId in activityData.AssignedUserIds)
                {
                    db.tbRupActividadAsignaciones.Add(new tbRupActividadAsignaciones
                    {
                        idActividad = newDbActivity.idActividad,
                        idUsuario = userId
                    });
                }
                db.SaveChanges(); // Guardar asignaciones

                // Devolver la actividad con los usuarios asignados (para la UI)
                var assignedUsers = db.tbUsuarios
                    .Where(u => activityData.AssignedUserIds.Contains(u.idUsuario))
                    .Select(u => new { id = u.idUsuario, name = u.nombreUsuario })
                    .ToList();

                return Json(new
                {
                    success = true,
                    id = newDbActivity.idActividad,
                    iteration_id = newDbActivity.idIteracion,
                    description = newDbActivity.descripcion,
                    assigned_role = newDbActivity.idRol, // Rol de contexto
                    assigned_users = assignedUsers, // Lista de usuarios asignados
                    status = newDbActivity.estado,
                    due_date = newDbActivity.fechaLimite?.ToString("yyyy-MM-dd")
                });
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
            var documents = db.tbRupDocumentos
                .Where(d => d.idIteracion == iterationId)
                .Include(d => d.tbRupTiposDocumento)
                .Select(d => new
                {
                    id = d.idDocumento,
                    iteration_id = d.idIteracion,
                    type = d.tbRupTiposDocumento.clave,
                    file_name = d.nombreArchivo,
                    version = d.Version,
                    status = d.Estado,
                    uploaded_at = d.FechaSubida
                })
                .ToList();
            var result = documents.Select(d => new {
                d.id,
                d.iteration_id,
                d.type,
                d.file_name,
                d.version,
                d.status,
                uploaded_at = d.uploaded_at.ToString("o")
            }).ToList();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public class DocumentCreatePostModel
        {
            public int IterationId { get; set; }
            public string TypeClave { get; set; }
            public string Version { get; set; }
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
            var usersInRoleForProject = db.tbProyectoUsuarios
                .Where(pu => pu.idProyecto == projectId && pu.idRol == roleId)
                .Select(pu => new
                {
                    id = pu.tbUsuarios.idUsuario,
                    name = pu.tbUsuarios.nombreUsuario
                })
                .ToList();
            return Json(usersInRoleForProject, JsonRequestBehavior.AllowGet);
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