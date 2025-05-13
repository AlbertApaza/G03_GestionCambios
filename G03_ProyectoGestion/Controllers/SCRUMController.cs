using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using G03_ProyectoGestion.Models; // Asegúrate que tus modelos están aquí

namespace G03_ProyectoGestion.Controllers
{
    public class SCRUMController : Controller
    {
        private g03_databaseEntities db = new g03_databaseEntities();
        private const int SCRUM_METHODOLOGY_ID = 1; // <<<--- AJUSTA ESTE ID
        // private const int DEFAULT_USER_ID = 1;    // Quitado, ya que no podemos usar idUsuarioCreador directamente

        // GET: SCRUM/Detalles/{idProyecto}

        public ActionResult Index(int id) // 'id' es idProyecto
        {
            var proyecto = db.tbProyectos.Find(id);
            if (proyecto == null || proyecto.idMetodologia != SCRUM_METHODOLOGY_ID)
            {
                TempData["ErrorMessage"] = "Proyecto SCRUM no encontrado o no válido.";
                return RedirectToAction("Index", "Proyecto");
            }

            ViewBag.Title = $"Tablero SCRUM: {proyecto.nombreProyecto}";
            ViewBag.CurrentProjectId = proyecto.idProyecto;

            ViewBag.Usuarios = db.tbUsuarios
                                .Where(u => u.estadoUsuario == "activo") // Asumo campo 'activo'
                                .Select(u => new { id = u.idUsuario, name = u.nombreUsuario })
                                .OrderBy(u => u.name)
                                .ToList();
            ViewBag.PrioridadesBacklog = new List<string> { "alta", "media", "baja" };
            // Estados para el tablero visual en cliente (no se guardan en tbScrumBacklog.Estado)
            ViewBag.EstadosBacklogVisual = new List<string> { "Por Hacer", "En Progreso", "Terminado" };

            return View(proyecto);
        }

        // --- ACCIONES JSON PARA SPRINTS ---
        [HttpGet]
        public JsonResult GetSprintsForProject(int projectId)
        {
            var sprints = db.tbScrumSprints
                .Where(s => s.idProyecto == projectId)
                .OrderBy(s => s.fechaInicio)
                .Select(s => new
                {
                    id = s.idSprint,
                    project_id = s.idProyecto,
                    name = s.nombreSprint,
                    start_date = s.fechaInicio,
                    end_date = s.fechaFin
                })
                .ToList();
            var result = sprints.Select(s => new {
                s.id,
                s.project_id,
                s.name,
                start_date = s.start_date?.ToString("yyyy-MM-dd"),
                end_date = s.end_date?.ToString("yyyy-MM-dd")
            }).ToList();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public class SprintCreatePostModel
        {
            public int ProjectId { get; set; }
            public string Name { get; set; }
            public DateTime? Start_Date { get; set; }
            public DateTime? End_Date { get; set; }
        }

        [HttpPost]
        public JsonResult CreateSprint(SprintCreatePostModel sprintData)
        {
            if (string.IsNullOrWhiteSpace(sprintData.Name))
                ModelState.AddModelError("Name", "El nombre del sprint es requerido.");
            if (sprintData.ProjectId <= 0)
                ModelState.AddModelError("ProjectId", "ID de proyecto inválido.");

            if (ModelState.IsValid)
            {
                tbScrumSprints newDbSprint = new tbScrumSprints
                {
                    idProyecto = sprintData.ProjectId,
                    nombreSprint = sprintData.Name,
                    fechaInicio = sprintData.Start_Date,
                    fechaFin = sprintData.End_Date
                };
                db.tbScrumSprints.Add(newDbSprint);
                db.SaveChanges();
                return Json(new
                {
                    success = true,
                    id = newDbSprint.idSprint,
                    project_id = newDbSprint.idProyecto,
                    name = newDbSprint.nombreSprint,
                    start_date = newDbSprint.fechaInicio?.ToString("yyyy-MM-dd"),
                    end_date = newDbSprint.fechaFin?.ToString("yyyy-MM-dd")
                });
            }
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return Json(new { success = false, message = "Datos inválidos para crear sprint.", errors = errors });
        }

        // --- ACCIONES JSON PARA PRODUCT BACKLOG ITEMS ---
        [HttpGet]
        public JsonResult GetBacklogItemsForProject(int projectId)
        {
            // Ya que tbScrumBacklog no tiene idUsuarioCreador/Asignado, los quitamos del select
            var items = db.tbScrumBacklog
                .Where(b => b.idProyecto == projectId)
                .Select(b => new
                {
                    id = b.idBacklog,
                    project_id = b.idProyecto,
                    description = b.descripcionBacklog,
                    priority = b.prioridad
                    // No hay sprint_id ni status en el modelo tbScrumBacklog
                })
                .OrderBy(b => b.priority) // O algún otro orden
                .ToList();
            return Json(items, JsonRequestBehavior.AllowGet);
        }

        public class BacklogItemCreatePostModel
        {
            public int ProjectId { get; set; }
            public string Description { get; set; }
            public string Priority { get; set; }
            // No hay IdUsuarioAsignado, SprintId, Status para enviar
        }

        [HttpPost]
        public JsonResult CreateBacklogItem(BacklogItemCreatePostModel itemData)
        {
            if (string.IsNullOrWhiteSpace(itemData.Description))
                ModelState.AddModelError("Description", "La descripción del item es requerida.");
            if (itemData.ProjectId <= 0)
                ModelState.AddModelError("ProjectId", "ID de proyecto inválido.");

            if (ModelState.IsValid)
            {
                // No podemos guardar idUsuarioCreador ni idUsuarioAsignado porque no están en el modelo
                tbScrumBacklog newDbItem = new tbScrumBacklog
                {
                    idProyecto = itemData.ProjectId,
                    descripcionBacklog = itemData.Description,
                    prioridad = itemData.Priority
                };
                db.tbScrumBacklog.Add(newDbItem);
                db.SaveChanges();

                return Json(new
                {
                    success = true,
                    id = newDbItem.idBacklog,
                    project_id = newDbItem.idProyecto,
                    description = newDbItem.descripcionBacklog,
                    priority = newDbItem.prioridad
                    // No hay más campos para devolver relacionados con usuarios o sprint/status
                });
            }
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return Json(new { success = false, message = "Datos inválidos para crear item de backlog.", errors = errors });
        }

        // Esta acción es conceptualmente para el drag-and-drop.
        // Como tbScrumBacklog no tiene idSprint ni Estado, esta acción no puede persistir esos cambios.
        // Podría usarse para registrar la intención en tbScrumDailyBacklog si se quisiera.
        [HttpPost]
        public JsonResult UpdateBacklogItemVisualState(int backlogId, int? targetSprintId, string targetVisualStatus)
        {
            var item = db.tbScrumBacklog.Find(backlogId);
            if (item == null) return Json(new { success = false, message = "Item del backlog no encontrado." });

            // No hay campos en tbScrumBacklog para guardar targetSprintId o targetVisualStatus.
            // La lógica de persistencia de esto se haría a través de tbScrumDailyBacklog,
            // por ejemplo, al registrar un comentario en un daily que indique que este item
            // se movió a X sprint o cambió a Y estado.

            // Por ahora, esta acción solo confirma que recibió la petición.
            // El estado real se maneja en el cliente (Alpine.js).
            System.Diagnostics.Debug.WriteLine($"Item {backlogId} visualmente movido a Sprint {targetSprintId}, Estado {targetVisualStatus}. No hay persistencia directa en tbScrumBacklog.");
            return Json(new { success = true, message = "Cambio visual procesado (sin persistencia directa en backlog)." });
        }

        // No podemos tener AssignBacklogItem si no hay idUsuarioAsignado en tbScrumBacklog

        // --- ACCIONES JSON PARA DAILY SCRUMS ---
        [HttpGet]
        public JsonResult GetDailiesForSprint(int sprintId)
        {
            var dailies = db.tbScrumDaily
                .Where(d => d.idSprint == sprintId)
                .OrderByDescending(d => d.fechaDaily)
                .Select(d => new
                {
                    id = d.idDaily,
                    sprint_id = d.idSprint,
                    date = d.fechaDaily,
                    observations = d.observaciones
                })
                .ToList();
            var result = dailies.Select(d => new {
                d.id,
                d.sprint_id,
                d.observations,
                date = d.date.ToString("yyyy-MM-dd")
            }).ToList();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public class DailyBacklogUpdateModel
        {
            public int BacklogId { get; set; }
            public int UserId { get; set; }
            public string Comment { get; set; }
        }
        public class DailyCreatePostModel
        {
            public int SprintId { get; set; }
            public int ProjectId { get; set; }
            public DateTime Date { get; set; }
            public string Observations { get; set; }
            public List<DailyBacklogUpdateModel> BacklogUpdates { get; set; }
        }

        [HttpPost]
        public JsonResult CreateDaily(DailyCreatePostModel dailyData)
        {
            var sprint = db.tbScrumSprints.FirstOrDefault(s => s.idSprint == dailyData.SprintId && s.idProyecto == dailyData.ProjectId);
            if (sprint == null) return Json(new { success = false, message = "Sprint no encontrado o no pertenece al proyecto actual." });

            if (dailyData.Date == DateTime.MinValue) // Validación simple de fecha
                ModelState.AddModelError("Date", "La fecha del daily es requerida.");

            if (ModelState.IsValid)
            {
                tbScrumDaily newDbDaily = new tbScrumDaily
                {
                    idSprint = dailyData.SprintId,
                    fechaDaily = dailyData.Date,
                    observaciones = dailyData.Observations
                };
                db.tbScrumDaily.Add(newDbDaily);
                db.SaveChanges();

                if (dailyData.BacklogUpdates != null)
                {
                    foreach (var bu in dailyData.BacklogUpdates.Where(b => b.BacklogId > 0 && b.UserId > 0))
                    {
                        var backlogItemExistsInProject = db.tbScrumBacklog.Any(bi => bi.idBacklog == bu.BacklogId && bi.idProyecto == dailyData.ProjectId);
                        if (!backlogItemExistsInProject) continue;

                        db.tbScrumDailyBacklog.Add(new tbScrumDailyBacklog
                        {
                            idDaily = newDbDaily.idDaily,
                            idBacklog = bu.BacklogId,
                            idUsuario = bu.UserId,
                            comentarioActividad = bu.Comment
                        });
                    }
                    db.SaveChanges();
                }

                return Json(new
                {
                    success = true,
                    id = newDbDaily.idDaily,
                    sprint_id = newDbDaily.idSprint,
                    date = newDbDaily.fechaDaily.ToString("yyyy-MM-dd"),
                    observations = newDbDaily.observaciones
                });
            }
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return Json(new { success = false, message = "Datos inválidos para crear daily.", errors = errors });
        }

        [HttpGet]
        public JsonResult GetDailyDetails(int dailyId, int projectId)
        {
            var daily = db.tbScrumDaily
                .Include(d => d.tbScrumSprints)
                .Include(d => d.tbScrumDailyBacklog.Select(sdb => sdb.tbScrumBacklog))
                .Include(d => d.tbScrumDailyBacklog.Select(sdb => sdb.tbUsuarios))
                .FirstOrDefault(d => d.idDaily == dailyId && d.tbScrumSprints.idProyecto == projectId);

            if (daily == null) return Json(new { success = false, message = "Daily no encontrado para este proyecto." }, JsonRequestBehavior.AllowGet);

            var result = new
            {
                id = daily.idDaily,
                sprint_id = daily.idSprint,
                date = daily.fechaDaily.ToString("yyyy-MM-dd"),
                observations = daily.observaciones,
                backlog_entries = daily.tbScrumDailyBacklog.Select(sdb => new {
                    backlog_id = sdb.idBacklog,
                    backlog_description = sdb.tbScrumBacklog?.descripcionBacklog,
                    user_id = sdb.idUsuario,
                    user_name = sdb.tbUsuarios?.nombreUsuario,
                    comment = sdb.comentarioActividad
                }).ToList()
            };
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