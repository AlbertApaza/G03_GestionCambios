using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using G03_ProyectoGestion.Models;
using G03_ProyectoGestion.Services; // Asegúrate que tus modelos están aquí

namespace G03_ProyectoGestion.Controllers
{
    public class SCRUMController : Controller
    {
        ScrumService _scrumService = new ScrumService();

        private g03_databaseEntities db = new g03_databaseEntities();


        public ActionResult Index(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "ID de proyecto no proporcionado.";
                return RedirectToAction("Index", "Proyecto");
            }

            var (success, proyecto, errorMessage) = _scrumService.ObtenerProyecto(id.Value);
            if (!success)
            {
                TempData["ErrorMessage"] = errorMessage;
                return RedirectToAction("Index", "Proyecto");
            }

            var usuarios = _scrumService.ObtenerUsuariosActivos();

            ViewBag.Title = $"Tablero SCRUM: {proyecto.nombreProyecto}";
            ViewBag.CurrentProjectId = proyecto.idProyecto;
            ViewBag.Usuarios = usuarios;
            ViewBag.PrioridadesBacklog = new List<string> { "alta", "media", "baja" };
            ViewBag.EstadosBacklogVisual = new List<string> { "Por Hacer", "En Progreso", "Terminado" };

            return View(proyecto);
        }

        // --- ACCIONES JSON PARA SPRINTS ---
        [HttpGet]
        public JsonResult GetSprintsForProject(int projectId)
        {
            var sprints = _scrumService.ObtenerSprintsPorProyecto(projectId);
            return Json(sprints, JsonRequestBehavior.AllowGet);
        }



        [HttpPost]
        public JsonResult CreateSprint(SprintCreatePostModel sprintData)
        {
            var (success, sprint, errors) = _scrumService.CrearSprint(sprintData);

            if (success)
            {
                return Json(new
                {
                    success = true,
                    id = sprint.idSprint,
                    project_id = sprint.idProyecto,
                    name = sprint.nombreSprint,
                    start_date = sprint.fechaInicio?.ToString("yyyy-MM-dd"),
                    end_date = sprint.fechaFin?.ToString("yyyy-MM-dd")
                });
            }

            return Json(new
            {
                success = false,
                message = "Datos inválidos para crear sprint.",
                errors = errors
            });
        }

        // --- ACCIONES JSON PARA PRODUCT BACKLOG ITEMS ---
        [HttpGet]
        public JsonResult GetBacklogItemsForProject(int projectId)
        {
            var items = _scrumService.ObtenerBacklogPorProyecto(projectId);
            return Json(items, JsonRequestBehavior.AllowGet);
        }



        [HttpPost]
        public JsonResult CreateBacklogItem(BacklogItemCreatePostModel itemData)
        {
            var (success, item, errors) = _scrumService.CrearBacklogItem(itemData);

            if (success)
            {
                return Json(new
                {
                    success = true,
                    id = item.idBacklog,
                    project_id = item.idProyecto,
                    description = item.descripcionBacklog,
                    priority = item.prioridad
                });
            }

            return Json(new
            {
                success = false,
                message = "Datos inválidos para crear item de backlog.",
                errors = errors
            });
        }


        [HttpPost]
        public JsonResult UpdateBacklogItemVisualState(int backlogId, int? targetSprintId, string targetVisualStatus)
        {
            var success = _scrumService.ActualizarEstadoVisualBacklog(backlogId, targetSprintId, targetVisualStatus);

            if (success)
            {
                return Json(new { success = true, message = "Cambio visual procesado (sin persistencia directa en backlog)." });
            }

            return Json(new { success = false, message = "Item del backlog no encontrado." });
        }


        // --- ACCIONES JSON PARA DAILY SCRUMS ---
        [HttpGet]
        public JsonResult GetDailiesForSprint(int sprintId)
        {
            var dailies = _scrumService.ObtenerDailiesPorSprint(sprintId);
            return Json(dailies, JsonRequestBehavior.AllowGet);
        }



        [HttpPost]
        public JsonResult CreateDaily(DailyCreatePostModel dailyData)
        {
            var (success, daily, errors) = _scrumService.CrearDaily(dailyData);

            if (success)
            {
                return Json(new
                {
                    success = true,
                    id = daily.idDaily,
                    sprint_id = daily.idSprint,
                    date = daily.fechaDaily.ToString("yyyy-MM-dd"),
                    observations = daily.observaciones
                });
            }

            return Json(new
            {
                success = false,
                message = "Datos inválidos para crear daily.",
                errors = errors
            });
        }

        [HttpGet]
        public JsonResult GetDailyDetails(int dailyId, int projectId)
        {
            var result = _scrumService.ObtenerDetallesDaily(dailyId, projectId);
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
        public ActionResult Equipo(int idProyecto)
        {
            var (success, proyecto, errorMessage) = _scrumService.ObtenerProyectoConEquipo(idProyecto);

            if (!success)
            {
                TempData["ErrorMessage"] = errorMessage;
                return RedirectToAction("Index", "Proyecto");
            }

            ViewBag.Title = $"Equipo del Proyecto: {proyecto.nombreProyecto}";
            return View(proyecto);
        }
    }
}