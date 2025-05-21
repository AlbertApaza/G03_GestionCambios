using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Renci.SshNet; // Si aún usas SSH para subida de archivos
using G03_ProyectoGestion.Models;
using G03_ProyectoGestion.Services; // Asegúrate que RupService está aquí
using G03_ProyectoGestion.ViewModels; // Asegúrate que tus ViewModels están aquí


namespace G03_ProyectoGestion.Controllers
{
    public class RUPController : Controller
    {
        private RupService _rupService = new RupService();
        private g03_databaseEntities db = new g03_databaseEntities();
        private const int RUP_METHODOLOGY_ID = 2;
        // El ID de un rol por defecto, por ejemplo, "Analista". Verifica que este ID exista en tu tabla tbRoles.
        private const int DEFAULT_ROLE_ID_FOR_ACTIVITY = 10;

        private int DEFAULT_USER_ID
        {
            get
            {
                var userId = Session["idUsuario"];
                if (userId == null || !int.TryParse(userId.ToString(), out int parsedId)) return 0;
                return parsedId;
            }
        }

        // --- INICIO: Helpers de Mapeo (Sin cambios respecto a la versión anterior) ---
        private string GetPhaseKeyFromDbId(int phaseId)
        {
            switch (phaseId) { case 1: return "inception"; case 2: return "elaboration"; case 3: return "construction"; case 4: return "transition"; default: return $"unknown_phase_{phaseId}"; }
        }
        private int GetPhaseDbIdFromKey(string phaseKey)
        {
            switch (phaseKey?.ToLower()) { case "inception": return 1; case "elaboration": return 2; case "construction": return 3; case "transition": return 4; default: return 0; }
        }
        private string GetRoleJsKeyFromDb(tbRoles role)
        {
            if (role == null) return $"unknown_role_{Guid.NewGuid().ToString().Substring(0, 4)}";
            return role.nombreRol.Trim().ToLowerInvariant()
                       .Replace(" ", "_")
                       .Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u")
                       .Replace("ñ", "n");
        }
        private string GetActivityStatusJsFromDb(string dbStatus)
        {
            if (string.IsNullOrEmpty(dbStatus)) return "not_started";
            switch (dbStatus.ToLowerInvariant())
            {
                case "pendiente": return "not_started";
                case "en progreso": return "in_progress";
                case "completada": return "completed";
                case "bloqueada": return "delayed";
                case "en revisión": return "in_progress";
                default: return "not_started";
            }
        }
        private string GetActivityStatusDbFromJs(string jsStatus)
        {
            if (string.IsNullOrEmpty(jsStatus)) return "Pendiente";
            switch (jsStatus.ToLowerInvariant())
            {
                case "not_started": return "Pendiente";
                case "in_progress": return "En Progreso";
                case "completed": return "Completada";
                case "delayed": return "Bloqueada";
                case "review": return "En Revisión";
                default: return "Pendiente";
            }
        }
        // --- FIN: Helpers de Mapeo ---

        public ActionResult Index(int? id)
        {
            // --- (Validación de sesión, proyecto y acceso de usuario: SIN CAMBIOS) ---
            if (DEFAULT_USER_ID == 0)
            {
                TempData["ErrorMessage"] = "Sesión expirada o inválida. Por favor, inicie sesión nuevamente.";
                return RedirectToAction("Login", "Home");
            }
            if (!id.HasValue)
            {
                TempData["ErrorMessage"] = "No se especificó un proyecto.";
                return RedirectToAction("Index", "Proyecto");
            }

            var projectDb = db.tbProyectos
                            .Include(p => p.tbMetodologias)
                            .Include(p => p.tbRupFases)
                            .FirstOrDefault(p => p.idProyecto == id.Value && p.idMetodologia == RUP_METHODOLOGY_ID);

            if (projectDb == null)
            {
                TempData["ErrorMessage"] = "Proyecto no encontrado o no es RUP.";
                return RedirectToAction("Index", "Proyecto");
            }

            bool hasAccess = db.tbProyectoUsuarios.Any(pu => pu.idProyecto == id.Value && pu.idUsuario == DEFAULT_USER_ID);
            if (!hasAccess)
            {
                TempData["ErrorMessage"] = "No tiene acceso a este proyecto.";
                return RedirectToAction("Index", "Proyecto");
            }

            // --- Datos para el Cronograma JS (ViewBag.ProjectTimelineData, ViewBag.RupPhasesForJs,
            //      ViewBag.ProjectUsersForJs, ViewBag.ProjectRolesForJs, ViewBag.ActivityStatusesForJs: SIN CAMBIOS) ---
            ViewBag.ProjectTimelineData = new ProjectTimelineViewModel
            {
                Id = projectDb.idProyecto,
                Name = projectDb.nombreProyecto,
                StartDate = projectDb.fechaInicio?.ToString("yyyy-MM-dd") ?? DateTime.Today.ToString("yyyy-MM-dd"),
                EndDate = projectDb.fechaFin?.ToString("yyyy-MM-dd") ?? DateTime.Today.AddMonths(3).ToString("yyyy-MM-dd")
            };

            var allRupPhasesDb = db.tbRupFases.OrderBy(f => f.idFase).ToList();
            long projectTotalDays = 1;
            if (projectDb.fechaInicio.HasValue && projectDb.fechaFin.HasValue && projectDb.fechaFin.Value >= projectDb.fechaInicio.Value)
            {
                projectTotalDays = (long)(projectDb.fechaFin.Value - projectDb.fechaInicio.Value).TotalDays + 1;
                if (projectTotalDays <= 0) projectTotalDays = 1;
            }

            int phaseCount = allRupPhasesDb.Count;
            long basePhaseDuration = (phaseCount > 0 && projectTotalDays > phaseCount) ? (projectTotalDays / phaseCount) : 1;
            DateTime currentPhaseStartDate = projectDb.fechaInicio ?? DateTime.Today;

            ViewBag.RupPhasesForJs = allRupPhasesDb.Select((ph, index) => {
                var phaseVm = new RupPhaseViewModel
                {
                    Id = ph.idFase,
                    Key = GetPhaseKeyFromDbId(ph.idFase),
                    Name = $"FASE {ph.idFase}: {ph.nombre}",
                    TitleColorClass = $"phase-title-{GetPhaseKeyFromDbId(ph.idFase)}",
                    TimelineBarColorClass = $"phase-timeline-bar-{GetPhaseKeyFromDbId(ph.idFase)}",
                    DefaultActivities = GetDefaultActivitiesForPhase(ph.idFase)
                };
                phaseVm.StartDate = currentPhaseStartDate.ToString("yyyy-MM-dd");
                DateTime phaseEndDateCalc = currentPhaseStartDate.AddDays(basePhaseDuration - 1);
                if (index == phaseCount - 1) phaseEndDateCalc = projectDb.fechaFin ?? currentPhaseStartDate.AddDays(basePhaseDuration - 1);
                phaseVm.EndDate = phaseEndDateCalc.ToString("yyyy-MM-dd");
                currentPhaseStartDate = phaseEndDateCalc.AddDays(1);
                return phaseVm;
            }).ToList();

            ViewBag.ProjectUsersForJs = db.tbUsuarios
                .Where(u => u.tbProyectoUsuarios.Any(pu => pu.idProyecto == projectDb.idProyecto))
                .Select(u => new UserViewModel { Id = u.idUsuario, Name = u.nombreUsuario })
                .ToList();

            ViewBag.ProjectRolesForJs = db.tbRoles
                .ToList()
                .Select(r => new RoleViewModel
                {
                    DbId = r.idRol,
                    Id = GetRoleJsKeyFromDb(r),
                    Name = r.nombreRol,
                    Members = db.tbProyectoUsuarios
                                .Where(pu => pu.idProyecto == projectDb.idProyecto && pu.idRol == r.idRol)
                                .Select(pu => new UserViewModel { Id = pu.tbUsuarios.idUsuario, Name = pu.tbUsuarios.nombreUsuario })
                                .ToList()
                })
                .Where(r => r.Members.Any())
                .OrderBy(r => r.Name)
                .ToList();

            ViewBag.ActivityStatusesForJs = new List<object> {
                new { key = "not_started", name = "Sin Empezar", colorClass = "status-not_started" },
                new { key = "in_progress", name = "En Proceso", colorClass = "status-in_progress" },
                new { key = "completed", name = "Completado", colorClass = "status-completed" },
                new { key = "delayed", name = "Atrasado", colorClass = "status-delayed" }
            };


            // --- Datos para la pestaña de Documentos (Alpine.js) (ViewBag.SelectedProjectDataForAlpine,
            //      ViewBag.ProjectId, ViewBag.PhasesForAlpine, etc.: SIN CAMBIOS) ---
            ViewBag.SelectedProjectDataForAlpine = new ProjectViewModel
            {
                id = projectDb.idProyecto,
                name = projectDb.nombreProyecto,
                scope = projectDb.descripcionProyecto,
                current_phase = projectDb.idFase ?? (allRupPhasesDb.Any() ? allRupPhasesDb.First().idFase : 0)
            };
            ViewBag.ProjectId = projectDb.idProyecto;
            ViewBag.PhasesForAlpine = allRupPhasesDb
                                .Select(p => new { id = p.idFase, name = p.nombre })
                                .ToList();
            ViewBag.RolesForAlpine = db.tbRoles
                                .Select(r => new { id = r.idRol, name = r.nombreRol })
                                .ToList();
            ViewBag.DocumentTypesForAlpine = db.tbRupTiposDocumento
                                .Select(dt => new { id = dt.idTipoDocumento, name = dt.nombre, clave = dt.clave })
                                .ToList();
            ViewBag.CurrentUserId = DEFAULT_USER_ID;


            return View();
        }

        // GetDefaultActivitiesForPhase: SIN CAMBIOS
        private List<string> GetDefaultActivitiesForPhase(int phaseId)
        {
            var key = GetPhaseKeyFromDbId(phaseId);
            switch (key)
            {
                case "inception": return new List<string> { "Definir visión del proyecto", "Identificar stakeholders", "Análisis de requisitos iniciales", "Definir casos de uso principales", "Planificación inicial" };
                case "elaboration": return new List<string> { "Refinar casos de uso", "Diseñar arquitectura", "Crear prototipos", "Definir plan de pruebas", "Análisis de riesgos" };
                case "construction": return new List<string> { "Implementar funcionalidades", "Realizar pruebas unitarias", "Integración continua", "Revisión de código", "Documentación técnica" };
                case "transition": return new List<string> { "Pruebas de aceptación", "Capacitación de usuarios", "Despliegue en producción", "Soporte post-implementación", "Evaluación del proyecto" };
                default: return new List<string>();
            }
        }


        [HttpGet]
        public JsonResult GetActivitiesForCronograma(int projectId)
        {
            var activitiesDb = db.tbRupActividades
                .Where(a => a.idProyecto == projectId)
                .Include(a => a.tbRupActividadAsignaciones.Select(aa => aa.tbUsuarios))
                .Include(a => a.tbRoles)
                .ToList();

            var activitiesVm = activitiesDb.Select(a => {
                // Ahora usamos a.fechaInicio y a.fechaLimite directamente
                int duration = 0;
                if (a.fechaInicio.HasValue && a.fechaLimite.HasValue && a.fechaLimite.Value >= a.fechaInicio.Value)
                {
                    duration = (int)(a.fechaLimite.Value - a.fechaInicio.Value).TotalDays + 1;
                }

                return new RupActivityViewModel
                {
                    DbId = a.idActividad,
                    Id = $"db-{a.idActividad}",
                    Name = a.descripcion,
                    PhaseKey = GetPhaseKeyFromDbId(a.idFase),
                    StartDate = a.fechaInicio, // Directo del modelo EF
                    EndDate = a.fechaLimite,   // Directo del modelo EF
                    DurationDays = duration,
                    Status = GetActivityStatusJsFromDb(a.estado),
                    RoleJsKey = a.tbRoles != null ? GetRoleJsKeyFromDb(a.tbRoles) : null,
                    RoleDbId = (int)a.idRol,
                    Assignees = a.tbRupActividadAsignaciones
                                 .Select(aa => new UserViewModel { Id = aa.tbUsuarios.idUsuario, Name = aa.tbUsuarios.nombreUsuario })
                                 .ToList()
                };
            }).ToList();

            return Json(activitiesVm, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult CreateCronogramaActivity(CronogramaActivityCreateModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return Json(new { success = false, message = "Datos inválidos: " + string.Join("; ", errors), errors = errors });
            }
            if (model.AssigneeUserIds == null || !model.AssigneeUserIds.Any())
            {
                return Json(new { success = false, message = "Debe asignar al menos un usuario a la actividad." });
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    int activityRoleId;
                    tbRoles roleEntity = null;

                    if (!string.IsNullOrEmpty(model.RoleJsKey) && model.RoleJsKey != "no_role")
                    {
                        roleEntity = db.tbRoles.ToList().FirstOrDefault(r => GetRoleJsKeyFromDb(r) == model.RoleJsKey);
                        if (roleEntity == null)
                            return Json(new { success = false, message = $"Rol con clave '{model.RoleJsKey}' no encontrado." });
                        activityRoleId = roleEntity.idRol;
                    }
                    else // No se especificó un RoleJsKey (el JS debería evitar esto ahora), o si se envía "no_role"
                    {
                        var defaultRole = db.tbRoles.Find(DEFAULT_ROLE_ID_FOR_ACTIVITY) ?? db.tbRoles.OrderBy(r => r.idRol).FirstOrDefault();
                        if (defaultRole == null)
                            return Json(new { success = false, message = "No se pudo asignar un rol por defecto. No hay roles en el sistema." });
                        activityRoleId = defaultRole.idRol;
                        roleEntity = defaultRole;
                    }

                    var newDbActivity = new tbRupActividades
                    {
                        idProyecto = model.ProjectId,
                        idFase = GetPhaseDbIdFromKey(model.PhaseKey),
                        descripcion = model.Name,
                        fechaInicio = model.StartDate, // Directamente del modelo
                        fechaLimite = model.EndDate,   // Directamente del modelo
                        estado = GetActivityStatusDbFromJs(model.Status),
                        idRol = activityRoleId
                    };

                    db.tbRupActividades.Add(newDbActivity);
                    db.SaveChanges(); // Para obtener newDbActivity.idActividad

                    foreach (var userId in model.AssigneeUserIds.Distinct())
                    {
                        var projectUser = db.tbProyectoUsuarios.FirstOrDefault(pu => pu.idProyecto == model.ProjectId && pu.idUsuario == userId);
                        if (projectUser == null) continue;

                        db.tbRupActividadAsignaciones.Add(new tbRupActividadAsignaciones
                        {
                            idActividad = newDbActivity.idActividad,
                            idUsuario = userId
                        });
                    }
                    db.SaveChanges();
                    transaction.Commit();

                    // Calcular duración para la respuesta
                    int duration = 0;
                    if (newDbActivity.fechaInicio.HasValue && newDbActivity.fechaLimite.HasValue && newDbActivity.fechaLimite.Value >= newDbActivity.fechaInicio.Value)
                    {
                        duration = (int)(newDbActivity.fechaLimite.Value - newDbActivity.fechaInicio.Value).TotalDays + 1;
                    }

                    var createdActivityVm = new RupActivityViewModel
                    {
                        DbId = newDbActivity.idActividad,
                        Id = $"db-{newDbActivity.idActividad}",
                        Name = newDbActivity.descripcion,
                        PhaseKey = GetPhaseKeyFromDbId(newDbActivity.idFase),
                        StartDate = newDbActivity.fechaInicio,
                        EndDate = newDbActivity.fechaLimite,
                        DurationDays = duration,
                        Status = GetActivityStatusJsFromDb(newDbActivity.estado),
                        RoleJsKey = roleEntity != null ? GetRoleJsKeyFromDb(roleEntity) : null,
                        RoleDbId = (int)newDbActivity.idRol,
                        Assignees = db.tbRupActividadAsignaciones
                                      .Where(aa => aa.idActividad == newDbActivity.idActividad)
                                      .Select(aa => new UserViewModel { Id = aa.tbUsuarios.idUsuario, Name = aa.tbUsuarios.nombreUsuario })
                                      .ToList()
                    };
                    return Json(new { success = true, activity = createdActivityVm });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    // Loggear ex.ToString() para detalles completos en el servidor
                    return Json(new { success = false, message = "Error al crear actividad: " + ex.Message + (ex.InnerException != null ? " Inner: " + ex.InnerException.Message : "") });
                }
            }
        }

        // UpdateActivityStatusCronograma: SIN CAMBIOS
        [HttpPost]
        public JsonResult UpdateActivityStatusCronograma(int activityDbId, string newJsStatus)
        {
            var activity = db.tbRupActividades.Find(activityDbId);
            if (activity == null) return Json(new { success = false, message = "Actividad no encontrada." });

            activity.estado = GetActivityStatusDbFromJs(newJsStatus);
            db.SaveChanges();
            return Json(new { success = true });
        }

        // GetMyProjectActivities y GetAllProjectActivities: SIN CAMBIOS respecto a la versión anterior
        [HttpGet]
        public JsonResult GetMyProjectActivities(int projectId)
        {
            if (DEFAULT_USER_ID == 0) return Json(new { success = false, message = "Usuario no autenticado." }, JsonRequestBehavior.AllowGet);

            var myActivities = db.tbRupActividades
                .Where(a => a.idProyecto == projectId &&
                            a.tbRupActividadAsignaciones.Any(aa => aa.idUsuario == DEFAULT_USER_ID))
                .Include(a => a.tbRupFases)
                .Include(a => a.tbRoles)
                .Include(a => a.tbRupActividadAsignaciones.Select(aa => aa.tbUsuarios))
                .OrderBy(a => a.idFase).ThenBy(a => a.fechaInicio)
                .ToList()
                .Select(a => new {
                    id = a.idActividad,
                    description = a.descripcion,
                    phase = a.tbRupFases?.nombre,
                    role = a.tbRoles?.nombreRol,
                    startDate = a.fechaInicio?.ToString("dd/MM/yyyy") ?? "N/A",
                    endDate = a.fechaLimite?.ToString("dd/MM/yyyy") ?? "N/A",
                    status = a.estado,
                    assignees = a.tbRupActividadAsignaciones.Select(aa => aa.tbUsuarios.nombreUsuario).ToList()
                }).ToList();
            return Json(new { success = true, activities = myActivities }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetAllProjectActivities(int projectId)
        {
            var allActivities = db.tbRupActividades
                .Where(a => a.idProyecto == projectId)
                .Include(a => a.tbRupFases)
                .Include(a => a.tbRoles)
                .Include(a => a.tbRupActividadAsignaciones.Select(aa => aa.tbUsuarios))
                .OrderBy(a => a.idFase).ThenBy(a => a.fechaInicio)
                .ToList()
                .Select(a => new {
                    id = a.idActividad,
                    description = a.descripcion,
                    phase = a.tbRupFases?.nombre,
                    role = a.tbRoles?.nombreRol,
                    startDate = a.fechaInicio?.ToString("dd/MM/yyyy") ?? "N/A",
                    endDate = a.fechaLimite?.ToString("dd/MM/yyyy") ?? "N/A",
                    status = a.estado,
                    assignees = a.tbRupActividadAsignaciones.Select(aa => aa.tbUsuarios.nombreUsuario).ToList()
                }).ToList();
            return Json(new { success = true, activities = allActivities }, JsonRequestBehavior.AllowGet);
        }


        // --- Métodos para la pestaña de Documentos (Alpine.js) (UpdateProjectPhase, GetUsersByRoleInProject,
        //      CreateActivityOriginal, UpdateActivityStatusOriginal, GetDocumentsForPhase, CreateDocument,
        //      UpdateDocumentStatus, DownloadDocument: SIN CAMBIOS respecto a la versión anterior) ---
        [HttpPost]
        public JsonResult UpdateProjectPhase(int projectId, int phaseId)
        {
            var success = _rupService.ActualizarFaseDelProyecto(projectId, phaseId);
            if (success) return Json(new { success = true });
            return Json(new { success = false, message = "No se pudo actualizar la fase del proyecto." });
        }

        [HttpGet]
        public JsonResult GetUsersByRoleInProject(int projectId, int roleId)
        {
            var result = _rupService.ObtenerUsuariosPorRolEnProyecto(projectId, roleId);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult CreateActivityOriginal(ActivityCreatePostModel activityData)
        {
            if (ModelState.IsValid)
            {
                // El RupService.CrearActividad debe ser capaz de manejar fechas
                // que vienen de ActivityCreatePostModel (ej. Due_Date como fechaInicio o fechaLimite)
                var result = _rupService.CrearActividad(activityData);
                return Json(result);
            }
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return Json(new { success = false, message = "Datos inválidos para actividad original.", errors = errors });
        }

        [HttpPost]
        public JsonResult UpdateActivityStatusOriginal(int activityId, string status) // Para Alpine
        {
            var activity = db.tbRupActividades.Find(activityId);
            if (activity == null) return Json(new { success = false, message = "Actividad no encontrada." });
            activity.estado = status;
            db.SaveChanges();
            return Json(new { success = true });
        }


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
            if (project == null) return Json(new { success = false, message = "Proyecto no encontrado." });
            var phase = db.tbRupFases.FirstOrDefault(f => f.idFase == documentData.PhaseId);
            if (phase == null) return Json(new { success = false, message = "Fase no encontrada." });


            if (ModelState.IsValid)
            {
                // --- INICIO LÓGICA SSH (DE TU CÓDIGO ORIGINAL) ---
                // Si no usas SSH, reemplaza esta sección con tu lógica de guardado local.
                string originalFileName = Path.GetFileName(docFile.FileName);
                string fileExtension = Path.GetExtension(originalFileName);
                string uniqueRemoteFileName = Guid.NewGuid().ToString() + fileExtension;

                string vpsHost = "161.132.38.250"; // Considera leer esto de Web.config
                string vpsUsername = "root";
                string vpsPassword = "patitochera123"; // ¡NUNCA hardcodear contraseñas en producción! Usar Key-based auth o config cifrada.

                string remoteBaseUploadPath = "/root/rup_manager/uploads";
                string remoteProjectFolder = $"Proyecto_{documentData.ProjectId}";
                string remotePhaseFolder = $"Fase_{documentData.PhaseId}";
                string remoteDirectoryPathOnVps = $"{remoteBaseUploadPath}/{remoteProjectFolder}/{remotePhaseFolder}";
                string remoteFilePathOnVps = $"{remoteDirectoryPathOnVps}/{uniqueRemoteFileName}";

                string tempUploadDir = Server.MapPath("~/App_Data/TempUploadsForVPS");
                Directory.CreateDirectory(tempUploadDir); // Asegura que el directorio exista
                string tempFilePath = Path.Combine(tempUploadDir, Guid.NewGuid().ToString() + fileExtension);

                try
                {
                    docFile.SaveAs(tempFilePath); // Guardar temporalmente en el servidor web

                    var connectionInfo = new ConnectionInfo(vpsHost, vpsUsername,
                                            new PasswordAuthenticationMethod(vpsUsername, vpsPassword));

                    using (var sshClient = new SshClient(connectionInfo)) // Para crear directorios
                    {
                        sshClient.Connect();
                        // Usar -p para crear directorios padres si no existen
                        sshClient.RunCommand($"mkdir -p {remoteDirectoryPathOnVps}");
                        sshClient.Disconnect();
                    }

                    using (var scpClient = new ScpClient(connectionInfo))
                    {
                        scpClient.Connect();
                        using (var fs = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read))
                        {
                            scpClient.Upload(fs, remoteFilePathOnVps);
                        }
                        scpClient.Disconnect();
                    }
                }
                catch (Renci.SshNet.Common.SshAuthenticationException authEx)
                {
                    if (System.IO.File.Exists(tempFilePath)) System.IO.File.Delete(tempFilePath);
                    return Json(new { success = false, message = "Error de autenticación SSH con el VPS: " + authEx.Message });
                }
                catch (Exception ex)
                {
                    if (System.IO.File.Exists(tempFilePath)) System.IO.File.Delete(tempFilePath);
                    // Log ex.ToString() para más detalles en el servidor
                    return Json(new { success = false, message = "Error al subir o registrar el documento (SSH): " + ex.Message });
                }
                finally
                {
                    if (System.IO.File.Exists(tempFilePath))
                    {
                        System.IO.File.Delete(tempFilePath);
                    }
                }
                // --- FIN LÓGICA SSH ---


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
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return Json(new { success = false, message = "Datos inválidos para documento.", errors = errors });
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
                var projectId = db.tbRupDocumentos.Where(d => d.idDocumento == documentId).Select(d => (int?)d.idProyecto).FirstOrDefault();
                return projectId.HasValue ? RedirectToAction("Index", new { id = projectId.Value }) : RedirectToAction("Index", "Proyecto");
            }

            // --- INICIO LÓGICA SSH DESCARGA (DE TU CÓDIGO ORIGINAL) ---
            // Si no usas SSH, reemplaza esta sección.
            string vpsHost = "161.132.38.250"; // Considera leer esto de Web.config
            string vpsUsername = "root";
            string vpsPassword = "patitochera123"; // ¡NUNCA hardcodear contraseñas en producción!
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
            catch (Renci.SshNet.Common.SftpPathNotFoundException) // O genérica si no es SFTP
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
            // --- FIN LÓGICA SSH DESCARGA ---
            var projIdForRedirect = db.tbRupDocumentos.Where(d => d.idDocumento == documentId).Select(d => (int?)d.idProyecto).FirstOrDefault();
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