using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using G03_ProyectoGestion.Models;

namespace G03_ProyectoGestion.Controllers
{
    public class SCRUMController : Controller
    {
        private g03_databaseEntities db = new g03_databaseEntities();
        private const int SCRUM_METHODOLOGY_ID = 1;

        // Helper methods to get current user's info
        // REPLACE WITH YOUR ACTUAL AUTHENTICATION/SESSION LOGIC
        private int GetCurrentUserRoleId()
        {
            if (Session["CurrentUserRoleId"] != null && Session["CurrentUserRoleId"] is int)
            {
                return (int)Session["CurrentUserRoleId"];
            }
            // Default to Admin/Full access for testing if not set or if developing locally without full auth
            // For production, you might throw an error or redirect to login if role is not found
            return 1; // Example: 1 for Admin, 6 for Collaborator, 7 for Client
        }

        private int GetCurrentUserId()
        {
            if (Session["CurrentUserId"] != null && Session["CurrentUserId"] is int)
            {
                return (int)Session["CurrentUserId"];
            }
            // Default for testing
            return 1; // Example: default user ID
        }

        // Renamed from Index to TableroScrum for clarity, or keep as Index if preferred
        // GET: SCRUM/Index/{id} or SCRUM/TableroScrum/{id}
        public ActionResult Index(int id) // 'id' is idProyecto
        {
            var proyecto = db.tbProyectos.Find(id);
            if (proyecto == null || proyecto.idMetodologia != SCRUM_METHODOLOGY_ID)
            {
                TempData["ErrorMessage"] = "Proyecto SCRUM no encontrado o no es de tipo SCRUM.";
                return RedirectToAction("Index", "Proyecto");
            }

            // --- START: New Project Templating Logic ---
            bool hasSprints = db.tbScrumSprints.Any(s => s.idProyecto == proyecto.idProyecto);
            bool hasBacklogItems = db.tbScrumBacklog.Any(b => b.idProyecto == proyecto.idProyecto);

            if (!hasSprints && !hasBacklogItems)
            {
                // Project is empty of SCRUM artifacts, attempt to populate with template
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
                            TempData["SuccessMessage"] = "Proyecto inicializado con plantilla SCRUM (historias de usuario y sprints de ejemplo).";
                        }
                        catch (Exception ex)
                        {
                            // Log the exception ex
                            TempData["ErrorMessage"] = "Error al generar la plantilla SCRUM para el proyecto. " + ex.Message;
                        }
                    }
                }
                else
                {
                    TempData["WarningMessage"] = "El proyecto no tiene fechas de inicio y fin definidas. No se pudo generar la plantilla SCRUM.";
                }
            }
            // --- END: New Project Templating Logic ---


            int currentUserRoleId = GetCurrentUserRoleId();
            int currentUserId = GetCurrentUserId();

            ViewBag.CurrentUserRoleId = currentUserRoleId;
            ViewBag.CurrentUserId = currentUserId;

            ViewBag.Title = $"Tablero SCRUM: {proyecto.nombreProyecto}";
            ViewBag.CurrentProjectId = proyecto.idProyecto;

            if (currentUserRoleId != 7)
            {
                ViewBag.Usuarios = db.tbUsuarios
                                    .Where(u => u.estadoUsuario == "activo")
                                    .Select(u => new { id = u.idUsuario, name = u.nombreUsuario })
                                    .OrderBy(u => u.name)
                                    .ToList();
                ViewBag.PrioridadesBacklog = new List<string> { "alta", "media", "baja" };
                ViewBag.EstadosBacklogVisual = new List<string> { "Por Hacer", "En Progreso", "Terminado" };

                var projectUsersAndRoles = db.tbProyectoUsuarios
                    .Where(up => up.idProyecto == id)
                    .Select(up => new
                    {
                        userName = up.tbUsuarios.nombreUsuario,
                        roleName = up.tbRoles.nombreRol
                    }).ToList<dynamic>();
                ViewBag.ProjectUsersAndRoles = projectUsersAndRoles;
            }

            return View("TableroScrum", proyecto);
        }

        private void GenerateScrumTemplateData(tbProyectos proyecto)
        {
            // --- 1. Add Template Backlog Items ---
            var templateBacklogItems = new List<tbScrumBacklog>
    {
        new tbScrumBacklog { idProyecto = proyecto.idProyecto, descripcionBacklog = "Como usuario final, quiero poder registrarme en el sistema para acceder a sus funcionalidades.", prioridad = "alta" },
        new tbScrumBacklog { idProyecto = proyecto.idProyecto, descripcionBacklog = "Como usuario registrado, quiero poder iniciar sesión con mis credenciales para usar la aplicación.", prioridad = "alta" },
        new tbScrumBacklog { idProyecto = proyecto.idProyecto, descripcionBacklog = "Como administrador, quiero poder gestionar usuarios (crear, editar, eliminar) para controlar el acceso.", prioridad = "media" },
        new tbScrumBacklog { idProyecto = proyecto.idProyecto, descripcionBacklog = "Como usuario, quiero poder ver mi perfil y editar mi información personal.", prioridad = "media" },
        new tbScrumBacklog { idProyecto = proyecto.idProyecto, descripcionBacklog = "Como usuario, quiero un panel de control principal que muestre información relevante al iniciar sesión.", prioridad = "baja" },
        new tbScrumBacklog { idProyecto = proyecto.idProyecto, descripcionBacklog = "Como usuario, quiero poder cerrar sesión de forma segura.", prioridad = "baja" }
        // Add more generic user stories as needed
    };
            db.tbScrumBacklog.AddRange(templateBacklogItems);

            // --- 2. Add Template Sprints ---
            DateTime projectStartDate = proyecto.fechaInicio.Value;
            DateTime projectEndDate = proyecto.fechaFin.Value;
            TimeSpan projectDuration = projectEndDate - projectStartDate;
            int totalProjectDays = (int)projectDuration.TotalDays + 1; // Inclusive of end date

            if (totalProjectDays < 3) // Not enough days for 3 meaningful sprints
            {
                // Create a single sprint spanning the project duration
                db.tbScrumSprints.Add(new tbScrumSprints
                {
                    idProyecto = proyecto.idProyecto,
                    nombreSprint = "Sprint Único",
                    fechaInicio = projectStartDate,
                    fechaFin = projectEndDate
                });
            }
            else
            {
                int numberOfSprints = 3;
                // Calculate approximate days per sprint, distribute remainder to the last sprint.
                int baseDaysPerSprint = totalProjectDays / numberOfSprints;
                int remainderDays = totalProjectDays % numberOfSprints;

                DateTime currentSprintStartDate = projectStartDate;

                for (int i = 1; i <= numberOfSprints; i++)
                {
                    int sprintDays = baseDaysPerSprint;
                    if (i == numberOfSprints) // Last sprint gets any remainder
                    {
                        sprintDays += remainderDays;
                    }

                    // Ensure sprint doesn't end before it starts if duration is too short (e.g. 0 days allocated after division)
                    if (sprintDays <= 0) sprintDays = 1;


                    DateTime currentSprintEndDate = currentSprintStartDate.AddDays(sprintDays - 1);

                    // Adjust last sprint's end date to exactly match project end date
                    if (i == numberOfSprints)
                    {
                        currentSprintEndDate = projectEndDate;
                    }
                    // Ensure sprint end date does not exceed project end date
                    if (currentSprintEndDate > projectEndDate)
                    {
                        currentSprintEndDate = projectEndDate;
                    }
                    // Ensure sprint end date is not before sprint start date if project duration is very short
                    if (currentSprintEndDate < currentSprintStartDate)
                    {
                        currentSprintEndDate = currentSprintStartDate;
                    }


                    db.tbScrumSprints.Add(new tbScrumSprints
                    {
                        idProyecto = proyecto.idProyecto,
                        nombreSprint = $"Sprint {i}",
                        fechaInicio = currentSprintStartDate,
                        fechaFin = currentSprintEndDate
                    });

                    if (i < numberOfSprints) // Prepare for next sprint
                    {
                        currentSprintStartDate = currentSprintEndDate.AddDays(1);
                        // If next sprint start date goes beyond project end, break (shouldn't happen with prior checks but good safeguard)
                        if (currentSprintStartDate > projectEndDate) break;
                    }
                }
            }
            db.SaveChanges();
        }

        // New Action for the Cronograma View
        // GET: SCRUM/Cronograma/{idProyecto}
        public ActionResult Cronograma(int idProyecto)
        {
            var proyecto = db.tbProyectos.Find(idProyecto);
            if (proyecto == null || proyecto.idMetodologia != SCRUM_METHODOLOGY_ID)
            {
                TempData["ErrorMessage"] = "Proyecto SCRUM no encontrado o no válido.";
                return RedirectToAction("Index", "Proyecto"); // Ajusta "Proyecto" si tu controlador de listado de proyectos se llama diferente
            }

            ViewBag.Title = $"Cronograma: {proyecto.nombreProyecto}";
            ViewBag.CurrentProjectId = proyecto.idProyecto;
            ViewBag.CurrentUserRoleId = GetCurrentUserRoleId(); // Para consistencia en el encabezado/navegación

            // Sprints para el cronograma
            var sprintsData = db.tbScrumSprints
                .Where(s => s.idProyecto == idProyecto)
                .OrderBy(s => s.fechaInicio)
                .Select(s => new
                {
                    id = s.idSprint,
                    name = s.nombreSprint,
                    start_date = s.fechaInicio,
                    end_date = s.fechaFin
                })
                .ToList() // Materializa antes de formatear fechas
                .Select(s => new { // Formatea fechas después de traer los datos de la BD
                    s.id,
                    s.name,
                    start_date = s.start_date?.ToString("yyyy-MM-dd"),
                    end_date = s.end_date?.ToString("yyyy-MM-dd")
                }).ToList();
            ViewBag.SprintsData = sprintsData;

            // Backlog Items (Tareas) para el proyecto.
            var backlogItemsData = db.tbScrumBacklog
                .Where(b => b.idProyecto == idProyecto)
                .Select(b => new
                {
                    id = b.idBacklog,
                    description = b.descripcionBacklog,
                    priority = b.prioridad
                    // No tenemos b.idSprint en tu modelo actual de tbScrumBacklog
                })
                .ToList();
            ViewBag.BacklogItemsData = backlogItemsData;

            return View(proyecto); // La vista es Cronograma.cshtml
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
                .ToList(); // Fetch data from DB

            // Format dates after fetching
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
            public DateTime? Start_Date { get; set; } // Ensure name matches JS
            public DateTime? End_Date { get; set; }   // Ensure name matches JS
        }

        [HttpPost]
        public JsonResult CreateSprint(SprintCreatePostModel sprintData)
        {
            // Basic server-side validation (client-side also exists)
            if (string.IsNullOrWhiteSpace(sprintData.Name))
                ModelState.AddModelError("Name", "El nombre del sprint es requerido.");
            if (sprintData.ProjectId <= 0)
                ModelState.AddModelError("ProjectId", "ID de proyecto inválido.");
            // Add date validations if necessary (e.g., End_Date >= Start_Date, within project bounds)
            // This is partly handled client-side but good to have server-side too.

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
            // For Role 6 (Collaborator), you might want to filter by assigned user here
            // if tbScrumBacklog has an idUsuarioAsignado field.
            // int currentUserId = GetCurrentUserId();
            // int currentUserRoleId = GetCurrentUserRoleId();
            // var query = db.tbScrumBacklog.Where(b => b.idProyecto == projectId);
            // if (currentUserRoleId == 6) {
            //    query = query.Where(b => b.idUsuarioAsignado == currentUserId);
            // }
            // For now, returning all items for the project. Alpine.js will handle visibility.

            var items = db.tbScrumBacklog
                .Where(b => b.idProyecto == projectId)
                .Select(b => new
                {
                    id = b.idBacklog,
                    project_id = b.idProyecto,
                    description = b.descripcionBacklog,
                    priority = b.prioridad,
                    // If you add idUsuarioAsignado to tbScrumBacklog, include it here:
                    // assigned_user_id = b.idUsuarioAsignado 
                })
                .OrderBy(b => b.priority) // Example ordering
                .ToList();
            return Json(items, JsonRequestBehavior.AllowGet);
        }

        public class BacklogItemCreatePostModel
        {
            public int ProjectId { get; set; }
            public string Description { get; set; }
            public string Priority { get; set; }
            // public int? AssignedUserId { get; set; } // If you add assignment
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
                tbScrumBacklog newDbItem = new tbScrumBacklog
                {
                    idProyecto = itemData.ProjectId,
                    descripcionBacklog = itemData.Description,
                    prioridad = itemData.Priority,
                    // idUsuarioCreador = GetCurrentUserId(), // Example
                    // idUsuarioAsignado = itemData.AssignedUserId, // If you add assignment
                    // estado = "Por Hacer" // Default state if you have one
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
                    // assigned_user_id = newDbItem.idUsuarioAsignado // If added
                });
            }
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return Json(new { success = false, message = "Datos inválidos para crear item de backlog.", errors = errors });
        }

        [HttpPost]
        public JsonResult UpdateBacklogItemVisualState(int backlogId, int? targetSprintId, string targetVisualStatus)
        {
            // IMPORTANT: This current implementation DOES NOT PERSIST the visual state to the database.
            // For a real application, you'd update tbScrumBacklog with idSprint and a status field.
            var item = db.tbScrumBacklog.Find(backlogId);
            if (item == null) return Json(new { success = false, message = "Item del backlog no encontrado." });

            // Example of how you MIGHT persist (requires tbScrumBacklog to have these fields)
            // item.idSprintActual = targetSprintId;
            // item.estadoVisual = targetVisualStatus; // Map targetVisualStatus to your DB states
            // db.Entry(item).State = EntityState.Modified;
            // db.SaveChanges();

            System.Diagnostics.Debug.WriteLine($"Item {backlogId} visualmente movido a Sprint {targetSprintId}, Estado {targetVisualStatus}. Persistencia NO implementada en este ejemplo.");
            return Json(new { success = true, message = "Cambio visual procesado (sin persistencia en DB para este ejemplo)." });
        }

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
                .ToList(); // Fetch data

            var result = dailies.Select(d => new { // Format date after fetching
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
            public int ProjectId { get; set; } // Good to have for validation
            public DateTime Date { get; set; }
            public string Observations { get; set; }
            public List<DailyBacklogUpdateModel> BacklogUpdates { get; set; }
        }

        [HttpPost]
        public JsonResult CreateDaily(DailyCreatePostModel dailyData)
        {
            // Validate sprint belongs to project
            var sprint = db.tbScrumSprints.FirstOrDefault(s => s.idSprint == dailyData.SprintId && s.idProyecto == dailyData.ProjectId);
            if (sprint == null)
            {
                return Json(new { success = false, message = "Sprint no encontrado o no pertenece al proyecto actual." });
            }

            if (dailyData.Date == DateTime.MinValue) // Simple date validation
                ModelState.AddModelError("Date", "La fecha del daily es requerida.");
            // You might want to validate the date is within the sprint's dates or project dates.

            if (ModelState.IsValid)
            {
                tbScrumDaily newDbDaily = new tbScrumDaily
                {
                    idSprint = dailyData.SprintId,
                    fechaDaily = dailyData.Date,
                    observaciones = dailyData.Observations
                    // idUsuarioReporta = GetCurrentUserId() // Example
                };
                db.tbScrumDaily.Add(newDbDaily);
                db.SaveChanges(); // Save daily first to get its ID

                if (dailyData.BacklogUpdates != null)
                {
                    foreach (var bu in dailyData.BacklogUpdates.Where(b => b.BacklogId > 0 && b.UserId > 0 && !string.IsNullOrWhiteSpace(b.Comment)))
                    {
                        // Ensure backlog item exists and belongs to the project
                        var backlogItemExistsInProject = db.tbScrumBacklog.Any(bi => bi.idBacklog == bu.BacklogId && bi.idProyecto == dailyData.ProjectId);
                        if (!backlogItemExistsInProject)
                        {
                            // Log this or handle as an error? For now, skipping.
                            System.Diagnostics.Debug.WriteLine($"Skipping daily backlog update for non-existent/mismatched project backlog item ID: {bu.BacklogId}");
                            continue;
                        }

                        db.tbScrumDailyBacklog.Add(new tbScrumDailyBacklog
                        {
                            idDaily = newDbDaily.idDaily,
                            idBacklog = bu.BacklogId,
                            idUsuario = bu.UserId,
                            comentarioActividad = bu.Comment
                        });
                    }
                    db.SaveChanges(); // Save backlog entries
                }

                return Json(new
                {
                    success = true,
                    id = newDbDaily.idDaily,
                    sprint_id = newDbDaily.idSprint,
                    date = newDbDaily.fechaDaily.ToString("yyyy-MM-dd"), // Consistent date format
                    observations = newDbDaily.observaciones
                });
            }
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return Json(new { success = false, message = "Datos inválidos para crear daily.", errors = errors });
        }

        [HttpGet]
        public JsonResult GetDailyDetails(int dailyId, int projectId) // Added projectId for security
        {
            var daily = db.tbScrumDaily
                .Include(d => d.tbScrumSprints) // To verify project ID
                .Include(d => d.tbScrumDailyBacklog.Select(sdb => sdb.tbScrumBacklog)) // Eager load backlog item
                .Include(d => d.tbScrumDailyBacklog.Select(sdb => sdb.tbUsuarios))   // Eager load user
                .FirstOrDefault(d => d.idDaily == dailyId && d.tbScrumSprints.idProyecto == projectId);

            if (daily == null)
            {
                return Json(new { success = false, message = "Daily no encontrado para este proyecto." }, JsonRequestBehavior.AllowGet);
            }

            var result = new
            {
                id = daily.idDaily,
                sprint_id = daily.idSprint,
                date = daily.fechaDaily.ToString("yyyy-MM-dd"),
                observations = daily.observaciones,
                backlog_entries = daily.tbScrumDailyBacklog.Select(sdb => new {
                    backlog_id = sdb.idBacklog,
                    backlog_description = sdb.tbScrumBacklog?.descripcionBacklog, // Null check if item deleted
                    user_id = sdb.idUsuario,
                    user_name = sdb.tbUsuarios?.nombreUsuario, // Null check if user deleted/inactive
                    comment = sdb.comentarioActividad
                }).ToList()
            };
            return Json(result, JsonRequestBehavior.AllowGet);
        }


        // GET: SCRUM/Equipo?idProyecto=5
        public ActionResult Equipo(int idProyecto)
        {
            var proyecto = db.tbProyectos
                            .Include(p => p.tbProyectoUsuarios.Select(pu => pu.tbUsuarios))
                            .Include(p => p.tbProyectoUsuarios.Select(pu => pu.tbRoles))
                            .FirstOrDefault(p => p.idProyecto == idProyecto && p.idMetodologia == SCRUM_METHODOLOGY_ID);

            if (proyecto == null)
            {
                TempData["ErrorMessage"] = "Proyecto SCRUM no encontrado o no válido.";
                return RedirectToAction("Index", "Proyecto");
            }

            // Ensure role is passed for consistent header/navigation if you have a _Layout that uses it
            ViewBag.CurrentUserRoleId = GetCurrentUserRoleId();

            ViewBag.Title = $"Equipo del Proyecto: {proyecto.nombreProyecto}";
            return View(proyecto);
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