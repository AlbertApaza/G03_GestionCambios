using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using G03_ProyectoGestion.Models;

namespace YourProjectName.Controllers
{
    public class RUPController : Controller
    {
        private g03_databaseEntities db = new g03_databaseEntities();
        private const int RUP_METHODOLOGY_ID = 2; // From your DB script
        private const int DEFAULT_USER_ID = 1; // Placeholder - 'Albert' from your DB script

        // MAIN VIEW
        public ActionResult Index()
        {
            ViewBag.Title = "Gestor RUP";
            ViewBag.Phases = db.tbRupFases
                                .Select(p => new { id = p.idFase, name = p.nombre })
                                .ToList();
            ViewBag.Roles = db.tbRoles
                                .Select(r => new { id = r.idRol, name = r.nombreRol })
                                .ToList();
            ViewBag.DocumentTypes = db.tbRupTiposDocumento // EF might pluralize this to tbRupTiposDocumentoes
                                .Select(dt => new { id = dt.idTipoDocumento, name = dt.nombre, clave = dt.clave })
                                .ToList();
            return View();
        }

        // --- PROJECTS ---
        [HttpGet]
        public JsonResult GetProjects()
        {
            // Only fetch projects that use RUP methodology
            var projects = db.tbProyectos
                .Where(p => p.idMetodologia == RUP_METHODOLOGY_ID) // Filter for RUP projects
                .Select(p => new
                {
                    id = p.idProyecto,
                    name = p.nombreProyecto,
                    scope = p.descripcionProyecto,
                    current_phase = p.idFase // This is now an integer idFase
                })
                .ToList();
            return Json(projects, JsonRequestBehavior.AllowGet);
        }

        // ViewModel for Project Creation
        public class ProjectCreatePostModel
        {
            public string Name { get; set; }
            public string Scope { get; set; }
            public int InitialPhaseId { get; set; } // Expecting idFase
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
                    idMetodologia = RUP_METHODOLOGY_ID, // Hardcoded for RUP
                    idUsuario = DEFAULT_USER_ID, // Placeholder for logged-in user
                    fechaInicio = DateTime.Today // Example default
                    // fechaFin could be null or set
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
        public JsonResult UpdateProjectPhase(int projectId, int phaseId) // phaseId is now int (idFase)
        {
            var project = db.tbProyectos.FirstOrDefault(p => p.idProyecto == projectId && p.idMetodologia == RUP_METHODOLOGY_ID);
            if (project == null) return Json(new { success = false, message = "Proyecto RUP no encontrado." });

            var phaseExists = db.tbRupFases.Any(f => f.idFase == phaseId);
            if (!phaseExists) return Json(new { success = false, message = "Fase inválida." });

            project.idFase = phaseId;
            db.SaveChanges();
            return Json(new { success = true });
        }

        // --- ITERATIONS ---
        [HttpGet]
        public JsonResult GetIterationsForPhase(int projectId, int phaseId) // phaseId is int (idFase)
        {
            var iterations = db.tbRupIteraciones
                .Where(i => i.idProyecto == projectId && i.idFase == phaseId)
                .Select(i => new
                {
                    id = i.idIteracion, // for Alpine key
                    project_id = i.idProyecto,
                    phase_id = i.idFase, // Use phase_id consistently
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
            public int PhaseId { get; set; } // idFase
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
                tbRupIteraciones newDbIteration = new tbRupIteraciones // EF might use tbRupIteracione
                {
                    idProyecto = iterationData.ProjectId,
                    idFase = iterationData.PhaseId,
                    nombre = iterationData.Name,
                    objetivo = iterationData.Objective,
                    fechaInicio = iterationData.Start_Date,
                    fechaFin = iterationData.End_Date,
                    Estado = "Planificada" // Default status
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

        // --- ACTIVITIES ---
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
                    assigned_role = a.idRol, // This is now idRol (int)
                    status = a.estado,
                    due_date = a.fechaLimite
                })
                .ToList();

            var result = activities.Select(a => new {
                a.id,
                a.iteration_id,
                a.description,
                a.assigned_role,
                a.status,
                due_date = a.due_date?.ToString("yyyy-MM-dd")
            }).ToList();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public class ActivityCreatePostModel
        {
            public int IterationId { get; set; }
            public string Description { get; set; }
            public int AssignedRoleId { get; set; } // idRol
            public string Status { get; set; }
            public DateTime? Due_Date { get; set; }
        }

        [HttpPost]
        public JsonResult CreateActivity(ActivityCreatePostModel activityData)
        {
            if (ModelState.IsValid)
            {
                tbRupActividades newDbActivity = new tbRupActividades // EF might use tbRupActividade
                {
                    idIteracion = activityData.IterationId,
                    descripcion = activityData.Description,
                    idRol = activityData.AssignedRoleId,
                    estado = string.IsNullOrEmpty(activityData.Status) ? "Pendiente" : activityData.Status,
                    fechaLimite = activityData.Due_Date
                };
                db.tbRupActividades.Add(newDbActivity);
                db.SaveChanges();
                return Json(new
                {
                    success = true,
                    id = newDbActivity.idActividad,
                    iteration_id = newDbActivity.idIteracion,
                    description = newDbActivity.descripcion,
                    assigned_role = newDbActivity.idRol,
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

            activity.estado = status; // column name is 'estado'
            db.SaveChanges();
            return Json(new { success = true });
        }

        // --- DOCUMENTS ---
        [HttpGet]
        public JsonResult GetDocumentsForIteration(int iterationId)
        {
            var documents = db.tbRupDocumentos
                .Where(d => d.idIteracion == iterationId)
                .Include(d => d.tbRupTiposDocumento) // Include for accessing clave
                .Select(d => new
                {
                    id = d.idDocumento,
                    iteration_id = d.idIteracion,
                    type = d.tbRupTiposDocumento.clave, // Use the clave from related table
                    file_name = d.nombreArchivo,
                    version = d.Version,
                    status = d.Estado,
                    uploaded_at = d.FechaSubida
                })
                .ToList();
            return Json(documents, JsonRequestBehavior.AllowGet);
        }

        public class DocumentCreatePostModel
        {
            public int IterationId { get; set; }
            public string TypeClave { get; set; } // Frontend sends 'clave' (e.g., "vision")
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

            if (ModelState.IsValid)
            {
                var iteration = db.tbRupIteraciones.Find(documentData.IterationId);
                if (iteration == null) return Json(new { success = false, message = "Iteración no encontrada para el documento." });

                var fileName = Path.GetFileName(docFile.FileName);
                string projectUploadPath = Server.MapPath($"~/App_Data/RUP_Uploads/Project_{iteration.idProyecto}");
                string iterationUploadPath = Path.Combine(projectUploadPath, $"Iteration_{documentData.IterationId}");
                Directory.CreateDirectory(iterationUploadPath);

                var path = Path.Combine(iterationUploadPath, fileName);
                docFile.SaveAs(path);

                tbRupDocumentos newDbDocument = new tbRupDocumentos // EF might use tbRupDocumento
                {
                    idIteracion = documentData.IterationId,
                    idTipoDocumento = tipoDocumento.idTipoDocumento, // Use looked-up ID
                    nombreArchivo = fileName,
                    rutaArchivo = $"~/App_Data/RUP_Uploads/Project_{iteration.idProyecto}/Iteration_{documentData.IterationId}/{fileName}",
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
                    type = tipoDocumento.clave, // Send back the clave for consistency with GET
                    file_name = newDbDocument.nombreArchivo,
                    version = newDbDocument.Version,
                    status = newDbDocument.Estado,
                    uploaded_at = newDbDocument.FechaSubida.ToString("o")
                });
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