using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using G03_ProyectoGestion.Models;
using G03_ProyectoGestion.ViewModels;

namespace G03_ProyectoGestion.Controllers
{
    public class SCRUMController : Controller
    {
        private g03_databaseEntities db = new g03_databaseEntities();
        private const int SCRUM_METHODOLOGY_ID = 1;

        private int GetCurrentUserRoleId()
        {
            if (Session["CurrentUserRoleId"] != null && Session["CurrentUserRoleId"] is int roleId)
            {
                return roleId;
            }
            return 1;
        }

        private int GetCurrentUserId()
        {
            if (Session["CurrentUserId"] != null && Session["CurrentUserId"] is int userId)
            {
                return userId;
            }
            return 1;
        }

        public ActionResult Index(int id)
        {
            var proyecto = db.tbProyectos.Include(p => p.tbMetodologias).FirstOrDefault(p => p.idProyecto == id);
            if (proyecto == null || proyecto.idMetodologia != SCRUM_METHODOLOGY_ID)
            {
                TempData["ErrorMessage"] = "Proyecto SCRUM no encontrado o no es de tipo SCRUM.";
                return RedirectToAction("Index", "Proyecto");
            }

            bool hasSprints = db.tbScrumSprints.Any(s => s.idProyecto == proyecto.idProyecto);
            bool hasBacklogItems = db.tbScrumBacklog.Any(b => b.idProyecto == proyecto.idProyecto);

            if (!hasSprints && !hasBacklogItems)
            {
                if (proyecto.fechaInicio.HasValue && proyecto.fechaFin.HasValue)
                {
                    if (proyecto.fechaFin.Value < proyecto.fechaInicio.Value)
                    {
                        TempData["WarningMessage"] = "La fecha de fin del proyecto es anterior a la fecha de inicio. No se pudo generar la plantilla SCRUM.";
                    }
                    else
                    {
                        try
                        {
                            GenerateScrumTemplateData(proyecto);
                            TempData["SuccessMessage"] = "Proyecto inicializado con plantilla SCRUM.";
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Trace.TraceError("Error generando plantilla SCRUM: " + ex.ToString());
                            TempData["ErrorMessage"] = "Error al generar la plantilla SCRUM: " + ex.Message;
                        }
                    }
                }
                else
                {
                    TempData["WarningMessage"] = "El proyecto no tiene fechas de inicio y fin definidas. No se pudo generar la plantilla SCRUM.";
                }
            }

            int currentUserRoleId = GetCurrentUserRoleId();
            ViewBag.CurrentUserRoleId = currentUserRoleId;
            ViewBag.CurrentUserId = GetCurrentUserId();
            ViewBag.Title = $"Tablero SCRUM: {proyecto.nombreProyecto}";
            ViewBag.CurrentProjectId = proyecto.idProyecto;
            ViewBag.ProjectStartDateForValidation = proyecto.fechaInicio?.ToString("yyyy-MM-dd");
            ViewBag.ProjectEndDateForValidation = proyecto.fechaFin?.ToString("yyyy-MM-dd");
            ViewBag.ProyectoElementosParaTablero = new List<ProyectoElementoViewModel>();

            if (currentUserRoleId != 7)
            {
                ViewBag.Usuarios = db.tbUsuarios.Where(u => u.estadoUsuario == "activo")
                                    .Select(u => new { id = u.idUsuario, name = u.nombreUsuario }).OrderBy(u => u.name).ToList();
                ViewBag.PrioridadesBacklog = new List<string> { "alta", "media", "baja" };
                ViewBag.EstadosBacklogVisual = new List<string> { "Por Hacer", "En Progreso", "Terminado" };
                ViewBag.ProjectUsersAndRoles = db.tbProyectoUsuarios.Where(up => up.idProyecto == id)
                    .Include(up => up.tbUsuarios).Include(up => up.tbRoles)
                    .Select(up => new { userName = up.tbUsuarios.nombreUsuario, roleName = up.tbRoles.nombreRol }).ToList<dynamic>();

                if (currentUserRoleId != 6)
                {
                    List<ProyectoElementoViewModel> proyectoElementosVM = db.tbProyectoElemento
                        .Where(pe => pe.idProyecto == id && pe.idElemento.HasValue && pe.tbElementos != null)
                        .Include(pe => pe.tbElementos)
                        .OrderBy(pe => pe.fechaInicio)
                        .ToList()
                        .Select(pe => new ProyectoElementoViewModel
                        {
                            IdProyectoElemento = pe.idProyectoElemento,
                            IdElemento = pe.idElemento.Value,
                            ElementoNombre = pe.tbElementos.nombre ?? "Nombre no disponible",
                            FechaInicio = pe.fechaInicio?.ToString("dd/MM/yyyy"),
                            FechaFin = pe.fechaFin?.ToString("dd/MM/yyyy"),
                            FechaInicioEditable = pe.fechaInicio?.ToString("yyyy-MM-dd"),
                            FechaFinEditable = pe.fechaFin?.ToString("yyyy-MM-dd"),
                            FaseSprintIteracionMostrable = pe.FASE_SPRINT_ITERACION.HasValue ? pe.FASE_SPRINT_ITERACION.Value.ToString() : "N/A"
                        }).ToList();
                    ViewBag.ProyectoElementosParaTablero = proyectoElementosVM;
                }
            }
            return View("TableroScrum", proyecto);
        }

        [HttpPost]
        public JsonResult UpdateProyectoElementoFechas(int idProyectoElemento, string fechaInicioStr, string fechaFinStr)
        {
            try
            {
                var proyectoElemento = db.tbProyectoElemento
                                         .Include(pe => pe.tbProyectos)
                                         .FirstOrDefault(pe => pe.idProyectoElemento == idProyectoElemento);

                if (proyectoElemento == null)
                {
                    return Json(new { success = false, message = "Elemento del proyecto no encontrado." });
                }

                var proyecto = proyectoElemento.tbProyectos;
                if (proyecto == null)
                {
                    return Json(new { success = false, message = "No se pudo cargar la información del proyecto asociado al elemento." });
                }

                DateTime? nuevaFechaInicio = null;
                if (!string.IsNullOrEmpty(fechaInicioStr))
                {
                    if (DateTime.TryParseExact(fechaInicioStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
                    {
                        nuevaFechaInicio = parsedDate;
                    }
                    else
                    {
                        return Json(new { success = false, message = "Formato de fecha de inicio inválido. Use YYYY-MM-DD." });
                    }
                }

                DateTime? nuevaFechaFin = null;
                if (!string.IsNullOrEmpty(fechaFinStr))
                {
                    if (DateTime.TryParseExact(fechaFinStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
                    {
                        nuevaFechaFin = parsedDate;
                    }
                    else
                    {
                        return Json(new { success = false, message = "Formato de fecha de fin inválido. Use YYYY-MM-DD." });
                    }
                }

                // Validaciones
                if (nuevaFechaInicio.HasValue && proyecto.fechaInicio.HasValue && nuevaFechaInicio < proyecto.fechaInicio)
                {
                    return Json(new { success = false, message = $"La fecha de inicio del elemento no puede ser anterior a la fecha de inicio del proyecto ({proyecto.fechaInicio:dd/MM/yyyy})." });
                }
                if (nuevaFechaFin.HasValue && proyecto.fechaFin.HasValue && nuevaFechaFin > proyecto.fechaFin)
                {
                    return Json(new { success = false, message = $"La fecha de fin del elemento no puede ser posterior a la fecha de fin del proyecto ({proyecto.fechaFin:dd/MM/yyyy})." });
                }
                if (nuevaFechaInicio.HasValue && nuevaFechaFin.HasValue && nuevaFechaFin < nuevaFechaInicio)
                {
                    return Json(new { success = false, message = "La fecha de fin del elemento no puede ser anterior a su fecha de inicio." });
                }
                if (nuevaFechaInicio.HasValue && !nuevaFechaFin.HasValue && proyectoElemento.fechaFin.HasValue && proyectoElemento.fechaFin < nuevaFechaInicio)
                {
                    return Json(new { success = false, message = "La nueva fecha de inicio no puede ser posterior a la fecha de fin existente del elemento." });
                }
                if (nuevaFechaFin.HasValue && !nuevaFechaInicio.HasValue && proyectoElemento.fechaInicio.HasValue && nuevaFechaFin < proyectoElemento.fechaInicio)
                {
                    return Json(new { success = false, message = "La nueva fecha de fin no puede ser anterior a la fecha de inicio existente del elemento." });
                }


                proyectoElemento.fechaInicio = nuevaFechaInicio;
                proyectoElemento.fechaFin = nuevaFechaFin;

                db.Entry(proyectoElemento).State = EntityState.Modified;
                db.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Fechas del elemento actualizadas.",
                    fechaInicioActualizada = nuevaFechaInicio?.ToString("dd/MM/yyyy"),
                    fechaFinActualizada = nuevaFechaFin?.ToString("dd/MM/yyyy")
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Error actualizando fechas de elemento: " + ex.ToString());
                return Json(new { success = false, message = "Error al actualizar las fechas del elemento: " + ex.Message });
            }
        }

        private void GenerateScrumTemplateData(tbProyectos proyecto)
        {
            var templateBacklogItems = new List<tbScrumBacklog>
            {
                new tbScrumBacklog { idProyecto = proyecto.idProyecto, descripcionBacklog = "Como usuario final, quiero poder registrarme...", prioridad = "alta" },
                new tbScrumBacklog { idProyecto = proyecto.idProyecto, descripcionBacklog = "Como usuario registrado, quiero poder iniciar sesión...", prioridad = "alta" },
                new tbScrumBacklog { idProyecto = proyecto.idProyecto, descripcionBacklog = "Como administrador, quiero poder gestionar usuarios...", prioridad = "media" },
                new tbScrumBacklog { idProyecto = proyecto.idProyecto, descripcionBacklog = "Como usuario, quiero poder ver mi perfil...", prioridad = "media" },
                new tbScrumBacklog { idProyecto = proyecto.idProyecto, descripcionBacklog = "Como usuario, quiero un panel de control principal...", prioridad = "baja" },
                new tbScrumBacklog { idProyecto = proyecto.idProyecto, descripcionBacklog = "Como usuario, quiero poder cerrar sesión...", prioridad = "baja" }
            };
            db.tbScrumBacklog.AddRange(templateBacklogItems);

            if (proyecto.fechaInicio.HasValue && proyecto.fechaFin.HasValue)
            {
                DateTime projectStartDate = proyecto.fechaInicio.Value;
                DateTime projectEndDate = proyecto.fechaFin.Value;
                if (projectEndDate < projectStartDate) { db.SaveChanges(); return; }

                TimeSpan projectDuration = projectEndDate - projectStartDate;
                int totalProjectDays = (int)projectDuration.TotalDays + 1;

                if (totalProjectDays <= 0)
                {
                    db.tbScrumSprints.Add(new tbScrumSprints { idProyecto = proyecto.idProyecto, nombreSprint = "Sprint Inicial", fechaInicio = projectStartDate, fechaFin = projectEndDate });
                }
                else if (totalProjectDays < 7)
                {
                    db.tbScrumSprints.Add(new tbScrumSprints { idProyecto = proyecto.idProyecto, nombreSprint = "Sprint Único", fechaInicio = projectStartDate, fechaFin = projectEndDate });
                }
                else
                {
                    int numberOfSprints = 3;
                    int baseDaysPerSprint = Math.Max(1, totalProjectDays / numberOfSprints);
                    int remainderDays = totalProjectDays % numberOfSprints;
                    DateTime currentSprintStartDate = projectStartDate;
                    for (int i = 1; i <= numberOfSprints; i++)
                    {
                        int sprintDays = baseDaysPerSprint;
                        if (i == numberOfSprints) sprintDays += remainderDays;
                        if (sprintDays <= 0) sprintDays = 1;
                        DateTime currentSprintEndDate = currentSprintStartDate.AddDays(sprintDays - 1);
                        if (i == numberOfSprints) currentSprintEndDate = projectEndDate;
                        if (currentSprintEndDate > projectEndDate) currentSprintEndDate = projectEndDate;
                        if (currentSprintEndDate < currentSprintStartDate) currentSprintEndDate = currentSprintStartDate;
                        db.tbScrumSprints.Add(new tbScrumSprints { idProyecto = proyecto.idProyecto, nombreSprint = $"Sprint {i}", fechaInicio = currentSprintStartDate, fechaFin = currentSprintEndDate });
                        if (i < numberOfSprints)
                        {
                            currentSprintStartDate = currentSprintEndDate.AddDays(1);
                            if (currentSprintStartDate > projectEndDate) break;
                        }
                    }
                }
            }
            db.SaveChanges();
        }

        public ActionResult Cronograma(int idProyecto)
        {
            var proyecto = db.tbProyectos.Find(idProyecto);
            if (proyecto == null || proyecto.idMetodologia != SCRUM_METHODOLOGY_ID)
            {
                TempData["ErrorMessage"] = "Proyecto SCRUM no encontrado o no válido.";
                return RedirectToAction("Index", "Proyecto");
            }
            ViewBag.Title = $"Cronograma: {proyecto.nombreProyecto}";
            ViewBag.CurrentProjectId = proyecto.idProyecto;
            ViewBag.CurrentUserRoleId = GetCurrentUserRoleId();
            var sprintsData = db.tbScrumSprints.Where(s => s.idProyecto == idProyecto).OrderBy(s => s.fechaInicio)
                .Select(s => new { id = s.idSprint, name = s.nombreSprint, start_date_dt = s.fechaInicio, end_date_dt = s.fechaFin }).ToList()
                .Select(s => new { s.id, s.name, start_date = s.start_date_dt?.ToString("yyyy-MM-dd"), end_date = s.end_date_dt?.ToString("yyyy-MM-dd") }).ToList();
            ViewBag.SprintsData = sprintsData;
            var backlogItemsData = db.tbScrumBacklog.Where(b => b.idProyecto == idProyecto)
                .Select(b => new { id = b.idBacklog, description = b.descripcionBacklog, priority = b.prioridad }).ToList();
            ViewBag.BacklogItemsData = backlogItemsData;
            return View(proyecto);
        }

        public ActionResult CronogramaElementos(int idProyecto)
        {
            var proyecto = db.tbProyectos.Find(idProyecto);
            if (proyecto == null || proyecto.idMetodologia != SCRUM_METHODOLOGY_ID)
            {
                TempData["ErrorMessage"] = "Proyecto SCRUM no encontrado o no válido.";
                return RedirectToAction("Index", "Proyecto");
            }
            ViewBag.Title = $"Cronograma de Elementos: {proyecto.nombreProyecto}";
            ViewBag.CurrentProjectId = proyecto.idProyecto;
            ViewBag.CurrentUserRoleId = GetCurrentUserRoleId();
            List<ProyectoElementoViewModel> proyectoElementosVM = db.tbProyectoElemento
                .Where(pe => pe.idProyecto == idProyecto && pe.idElemento.HasValue && pe.tbElementos != null)
                .Include(pe => pe.tbElementos)
                .OrderBy(pe => pe.fechaInicio).ToList()
                .Select(pe => new ProyectoElementoViewModel
                {
                    IdProyectoElemento = pe.idProyectoElemento,
                    IdElemento = pe.idElemento.Value,
                    ElementoNombre = pe.tbElementos.nombre ?? "Nombre no disponible",
                    FechaInicio = pe.fechaInicio?.ToString("yyyy-MM-dd"),
                    FechaFin = pe.fechaFin?.ToString("yyyy-MM-dd"),
                    FaseSprintIteracionMostrable = pe.FASE_SPRINT_ITERACION.HasValue ? pe.FASE_SPRINT_ITERACION.Value.ToString() : "N/A"
                }).ToList();
            ViewBag.ProyectoElementosData = proyectoElementosVM;
            return View(proyecto);
        }

        [HttpGet]
        public JsonResult GetSprintsForProject(int projectId)
        {
            var sprints = db.tbScrumSprints.Where(s => s.idProyecto == projectId).OrderBy(s => s.fechaInicio)
                .Select(s => new { id = s.idSprint, project_id = s.idProyecto, name = s.nombreSprint, start_date_dt = s.fechaInicio, end_date_dt = s.fechaFin }).ToList();
            var result = sprints.Select(s => new { s.id, s.project_id, s.name, start_date = s.start_date_dt?.ToString("yyyy-MM-dd"), end_date = s.end_date_dt?.ToString("yyyy-MM-dd") }).ToList();
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        public class SprintCreatePostModel { public int ProjectId { get; set; } public string Name { get; set; } public DateTime? Start_Date { get; set; } public DateTime? End_Date { get; set; } }
        [HttpPost]
        public JsonResult CreateSprint(SprintCreatePostModel sprintData)
        {
            if (string.IsNullOrWhiteSpace(sprintData.Name)) ModelState.AddModelError("Name", "Nombre es requerido.");
            if (sprintData.ProjectId <= 0) ModelState.AddModelError("ProjectId", "ID de proyecto inválido.");
            if (ModelState.IsValid)
            {
                tbScrumSprints newDbSprint = new tbScrumSprints { idProyecto = sprintData.ProjectId, nombreSprint = sprintData.Name, fechaInicio = sprintData.Start_Date, fechaFin = sprintData.End_Date };
                db.tbScrumSprints.Add(newDbSprint); db.SaveChanges();
                return Json(new { success = true, id = newDbSprint.idSprint, project_id = newDbSprint.idProyecto, name = newDbSprint.nombreSprint, start_date = newDbSprint.fechaInicio?.ToString("yyyy-MM-dd"), end_date = newDbSprint.fechaFin?.ToString("yyyy-MM-dd") });
            }
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return Json(new { success = false, message = "Datos inválidos.", errors = errors });
        }

        [HttpGet]
        public JsonResult GetBacklogItemsForProject(int projectId)
        {
            var items = db.tbScrumBacklog.Where(b => b.idProyecto == projectId)
                .Select(b => new { id = b.idBacklog, project_id = b.idProyecto, description = b.descripcionBacklog, priority = b.prioridad }).OrderBy(b => b.priority).ToList();
            return Json(items, JsonRequestBehavior.AllowGet);
        }
        public class BacklogItemCreatePostModel { public int ProjectId { get; set; } public string Description { get; set; } public string Priority { get; set; } }
        [HttpPost]
        public JsonResult CreateBacklogItem(BacklogItemCreatePostModel itemData)
        {
            if (string.IsNullOrWhiteSpace(itemData.Description)) ModelState.AddModelError("Description", "Descripción es requerida.");
            if (itemData.ProjectId <= 0) ModelState.AddModelError("ProjectId", "ID de proyecto inválido.");
            if (ModelState.IsValid)
            {
                tbScrumBacklog newDbItem = new tbScrumBacklog { idProyecto = itemData.ProjectId, descripcionBacklog = itemData.Description, prioridad = itemData.Priority };
                db.tbScrumBacklog.Add(newDbItem); db.SaveChanges();
                return Json(new { success = true, id = newDbItem.idBacklog, project_id = newDbItem.idProyecto, description = newDbItem.descripcionBacklog, priority = newDbItem.prioridad });
            }
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return Json(new { success = false, message = "Datos inválidos.", errors = errors });
        }

        [HttpPost]
        public JsonResult UpdateBacklogItemVisualState(int backlogId, int? targetSprintId, string targetVisualStatus)
        {
            var item = db.tbScrumBacklog.Find(backlogId);
            if (item == null) return Json(new { success = false, message = "Item no encontrado." });
            System.Diagnostics.Debug.WriteLine($"Item {backlogId} movido a Sprint {targetSprintId}, Estado {targetVisualStatus}. (No persistido)");
            return Json(new { success = true, message = "Cambio visual procesado (sin persistencia)." });
        }

        [HttpGet]
        public JsonResult GetDailiesForSprint(int sprintId)
        {
            var dailies = db.tbScrumDaily.Where(d => d.idSprint == sprintId).OrderByDescending(d => d.fechaDaily)
                .Select(d => new { id = d.idDaily, sprint_id = d.idSprint, date_dt = d.fechaDaily, observations = d.observaciones }).ToList();
            var result = dailies.Select(d => new { d.id, d.sprint_id, d.observations, date = d.date_dt.ToString("yyyy-MM-dd") }).ToList();
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        public class DailyBacklogUpdateModel { public int BacklogId { get; set; } public int UserId { get; set; } public string Comment { get; set; } }
        public class DailyCreatePostModel { public int SprintId { get; set; } public int ProjectId { get; set; } public DateTime Date { get; set; } public string Observations { get; set; } public List<DailyBacklogUpdateModel> BacklogUpdates { get; set; } }
        [HttpPost]
        public JsonResult CreateDaily(DailyCreatePostModel dailyData)
        {
            var sprint = db.tbScrumSprints.FirstOrDefault(s => s.idSprint == dailyData.SprintId && s.idProyecto == dailyData.ProjectId);
            if (sprint == null) return Json(new { success = false, message = "Sprint no encontrado." });
            if (dailyData.Date == DateTime.MinValue) ModelState.AddModelError("Date", "Fecha es requerida.");
            if (ModelState.IsValid)
            {
                tbScrumDaily newDbDaily = new tbScrumDaily { idSprint = dailyData.SprintId, fechaDaily = dailyData.Date, observaciones = dailyData.Observations };
                db.tbScrumDaily.Add(newDbDaily); db.SaveChanges();
                if (dailyData.BacklogUpdates != null)
                {
                    foreach (var bu in dailyData.BacklogUpdates.Where(b => b.BacklogId > 0 && b.UserId > 0 && !string.IsNullOrWhiteSpace(b.Comment)))
                    {
                        if (!db.tbScrumBacklog.Any(bi => bi.idBacklog == bu.BacklogId && bi.idProyecto == dailyData.ProjectId)) continue;
                        db.tbScrumDailyBacklog.Add(new tbScrumDailyBacklog { idDaily = newDbDaily.idDaily, idBacklog = bu.BacklogId, idUsuario = bu.UserId, comentarioActividad = bu.Comment });
                    }
                    db.SaveChanges();
                }
                return Json(new { success = true, id = newDbDaily.idDaily, sprint_id = newDbDaily.idSprint, date = newDbDaily.fechaDaily.ToString("yyyy-MM-dd"), observations = newDbDaily.observaciones });
            }
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return Json(new { success = false, message = "Datos inválidos.", errors = errors });
        }

        [HttpGet]
        public JsonResult GetDailyDetails(int dailyId, int projectId)
        {
            var daily = db.tbScrumDaily.Include(d => d.tbScrumSprints)
                .Include(d => d.tbScrumDailyBacklog.Select(sdb => sdb.tbScrumBacklog))
                .Include(d => d.tbScrumDailyBacklog.Select(sdb => sdb.tbUsuarios))
                .FirstOrDefault(d => d.idDaily == dailyId && d.tbScrumSprints.idProyecto == projectId);
            if (daily == null) return Json(new { success = false, message = "Daily no encontrado." }, JsonRequestBehavior.AllowGet);
            var result = new
            {
                id = daily.idDaily,
                sprint_id = daily.idSprint,
                date = daily.fechaDaily.ToString("yyyy-MM-dd"),
                observations = daily.observaciones,
                backlog_entries = daily.tbScrumDailyBacklog.Select(sdb => new { backlog_id = sdb.idBacklog, backlog_description = sdb.tbScrumBacklog?.descripcionBacklog, user_id = sdb.idUsuario, user_name = sdb.tbUsuarios?.nombreUsuario, comment = sdb.comentarioActividad }).ToList()
            };
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Equipo(int idProyecto)
        {
            var proyecto = db.tbProyectos.Include(p => p.tbProyectoUsuarios.Select(pu => pu.tbUsuarios))
                .Include(p => p.tbProyectoUsuarios.Select(pu => pu.tbRoles))
                .FirstOrDefault(p => p.idProyecto == idProyecto && p.idMetodologia == SCRUM_METHODOLOGY_ID);
            if (proyecto == null)
            {
                TempData["ErrorMessage"] = "Proyecto SCRUM no encontrado.";
                return RedirectToAction("Index", "Proyecto");
            }
            ViewBag.CurrentUserRoleId = GetCurrentUserRoleId();
            ViewBag.Title = $"Equipo del Proyecto: {proyecto.nombreProyecto}";
            return View(proyecto);
        }
        public ActionResult BurndownChart(int? idProyecto)
        {
            int proyectoIdReal;

            if (idProyecto.HasValue)
            {
                proyectoIdReal = idProyecto.Value;
            }
            else if (Session["CurrentProjectId"] != null && Session["CurrentProjectId"] is int sessProjectId)
            {
                proyectoIdReal = sessProjectId;
            }
            else
            {
                TempData["ErrorMessage"] = "ID del proyecto no especificado para el Burndown Chart.";
                return RedirectToAction("Index", "Proyecto");
            }

            var proyecto = db.tbProyectos.Find(proyectoIdReal);
            if (proyecto == null || proyecto.idMetodologia != SCRUM_METHODOLOGY_ID)
            {
                TempData["ErrorMessage"] = "Proyecto SCRUM no encontrado o no válido para ver Burndown Chart.";
                return RedirectToAction("Index", "Proyecto");
            }

            ViewBag.Title = $"Burndown Chart: {proyecto.nombreProyecto}";
            ViewBag.CurrentProjectId = proyecto.idProyecto;
            ViewBag.CurrentUserRoleId = GetCurrentUserRoleId();

            return View(proyecto);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) { db.Dispose(); }
            base.Dispose(disposing);
        }
    }
}