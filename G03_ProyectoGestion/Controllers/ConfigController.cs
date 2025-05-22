using System;
using System.Collections.Generic;
using System.Data.Entity; // Para Include
using System.Linq;
using System.Web.Mvc;
using G03_ProyectoGestion.Models;
using G03_ProyectoGestion.ViewModels;

namespace G03_ProyectoGestion.Controllers
{
    public class ConfigController : Controller
    {
        private g03_databaseEntities db = new g03_databaseEntities();

        private const int TARGET_METHODOLOGY_ID = 2; // RUP

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
            if (id == null)
            {
                TempData["ErrorMessage"] = "ID de proyecto no proporcionado.";
                return RedirectToAction("Index", "Proyecto");
            }

            int currentUserId = DEFAULT_USER_ID;
            if (currentUserId == 0)
            {
                TempData["ErrorMessage"] = "Sesión inválida o expirada. Por favor, inicie sesión.";
                // Considera redirigir a una página de login si la tienes.
                // Ejemplo: return RedirectToAction("Login", "Account"); 
                return RedirectToAction("Index", "Proyecto"); // O a una página de error/inicio
            }

            var projectEntity = db.tbProyectos
                                .Include(p => p.tbMetodologias)
                                .Include(p => p.tbProyectoRupFases.Select(prf => prf.tbRupFases))
                                .FirstOrDefault(p => p.idProyecto == id.Value &&
                                                p.tbProyectoUsuarios.Any(pu => pu.idUsuario == currentUserId));

            if (projectEntity == null)
            {
                TempData["ErrorMessage"] = "Proyecto no encontrado o no tienes acceso.";
                return RedirectToAction("Index", "Proyecto");
            }

            if (projectEntity.idMetodologia != TARGET_METHODOLOGY_ID)
            {
                TempData["ErrorMessage"] = "Este panel de configuración es solo para proyectos con metodología RUP.";
                return RedirectToAction("Details", "Proyecto", new { id = projectEntity.idProyecto });
            }

            var viewModel = new ConfigRupViewModel
            {
                Project = new ProjectViewModel
                {
                    id = projectEntity.idProyecto,
                    name = projectEntity.nombreProyecto,
                    scope = projectEntity.descripcionProyecto
                },
                ProjectId = projectEntity.idProyecto,
                ProjectStartDate = projectEntity.fechaInicio ?? DateTime.MinValue,
                ProjectEndDate = projectEntity.fechaFin ?? DateTime.MaxValue,
                RupPhases = db.tbRupFases.OrderBy(f => f.idFase).ToList(),
                ProjectPhaseDates = projectEntity.tbProyectoRupFases.Select(prf => new ProjectPhaseDatesViewModel
                {
                    PhaseId = prf.idFase,
                    PhaseName = prf.tbRupFases.nombre,
                    StartDate = prf.fechaInicio,
                    EndDate = prf.fechaFin
                }).OrderBy(pfd => pfd.PhaseId).ToList()
            };

            ViewBag.Title = $"Asignación Elementos RUP - {viewModel.Project.name}";
            return View(viewModel);
        }

        [HttpGet]
        public JsonResult GetElementosPorFase(int idProyecto, int idFase)
        {
            string faseDescripcion = $"FASE {idFase}";

            var elementos = db.tbProyectoElemento
                .Where(pe => pe.idProyecto == idProyecto &&
                             pe.tbElementos.tipo == "RUP" && // Asegura que el elemento es de tipo RUP
                             pe.tbElementos.descripcion == faseDescripcion)
                // Excluir elementos que ya tienen una asignación en tbRupActividadAsignaciones
                // Un elemento se considera asignado si existe ALGUNA entrada para su idElemento en tbRupActividadAsignaciones
                .Where(pe => !db.tbRupActividadAsignaciones.Any(aa => aa.idElemento == pe.idElemento))
                .Select(pe => new
                {
                    pe.idElemento,
                    nombre = pe.tbElementos.nombre,
                    fechaInicio = pe.fechaInicio, // Estas son las fechas del elemento en tbProyectoElemento
                    fechaFin = pe.fechaFin
                })
                .OrderBy(e => e.nombre)
                .ToList();

            // Formatear fechas para que el input date las tome bien (yyyy-MM-dd)
            var resultadoFormateado = elementos.Select(e => new {
                e.idElemento,
                e.nombre,
                fechaInicio = e.fechaInicio?.ToString("yyyy-MM-dd"),
                fechaFin = e.fechaFin?.ToString("yyyy-MM-dd")
            });

            return Json(resultadoFormateado, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetRolesEnProyecto(int idProyecto)
        {
            var roles = db.tbProyectoUsuarios
                .Where(pu => pu.idProyecto == idProyecto && pu.idRol != null)
                .Select(pu => pu.tbRoles)
                .Distinct()
                .Select(r => new { r.idRol, r.nombreRol })
                .OrderBy(r => r.nombreRol)
                .ToList();
            return Json(roles, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetUsuariosPorRolEnProyecto(int idProyecto, int idRol)
        {
            // Asegúrate que tbUsuarios tiene un campo como 'nombreCompleto' o 'nombreUsuario'
            // Aquí estoy asumiendo 'nombreUsuario', ajusta según tu modelo tbUsuarios
            var usuarios = db.tbProyectoUsuarios
                .Where(pu => pu.idProyecto == idProyecto && pu.idRol == idRol)
                .Select(pu => new
                {
                    pu.tbUsuarios.idUsuario,
                    // CAMBIAR 'nombreUsuario' SI TU CAMPO SE LLAMA DIFERENTE EN tbUsuarios
                    nombreUsuario = pu.tbUsuarios.nombreUsuario ?? ("Usuario " + pu.tbUsuarios.idUsuario)
                })
                .OrderBy(u => u.nombreUsuario)
                .ToList();
            return Json(usuarios, JsonRequestBehavior.AllowGet);
        }

        private bool FechasSeSolapan(DateTime inicio1, DateTime fin1, DateTime inicio2, DateTime fin2)
        {
            // (StartA <= EndB) and (EndA >= StartB)
            return inicio1 < fin2 && fin1 > inicio2;
        }

        [HttpPost]
        public JsonResult GuardarAsignaciones(int idProyecto, int idFaseSeleccionada, int idElemento,
                                              List<int> idUsuarios, string fechaInicioElementoStr, string fechaFinElementoStr)
        {
            try
            {
                if (idUsuarios == null || !idUsuarios.Any())
                {
                    return Json(new { success = false, message = "Debe seleccionar al menos un usuario." });
                }

                DateTime fechaInicioElemento;
                DateTime fechaFinElemento;

                if (!DateTime.TryParse(fechaInicioElementoStr, out fechaInicioElemento) ||
                    !DateTime.TryParse(fechaFinElementoStr, out fechaFinElemento))
                {
                    return Json(new { success = false, message = "Formato de fechas para el elemento inválido." });
                }

                if (fechaInicioElemento >= fechaFinElemento)
                {
                    return Json(new { success = false, message = "La fecha de inicio del elemento debe ser anterior a su fecha de fin." });
                }

                var faseProyecto = db.tbProyectoRupFases
                                     .FirstOrDefault(pf => pf.idProyecto == idProyecto && pf.idFase == idFaseSeleccionada);
                if (faseProyecto == null)
                {
                    return Json(new { success = false, message = "No se encontraron las fechas para la fase seleccionada del proyecto." });
                }

                if (fechaInicioElemento < faseProyecto.fechaInicio || fechaFinElemento > faseProyecto.fechaFin)
                {
                    return Json(new { success = false, message = $"Las fechas del elemento ({fechaInicioElemento:dd/MM/yyyy} - {fechaFinElemento:dd/MM/yyyy}) deben estar dentro del rango de la fase: {faseProyecto.tbRupFases.nombre} ({faseProyecto.fechaInicio:dd/MM/yyyy} - {faseProyecto.fechaFin:dd/MM/yyyy})." });
                }

                foreach (var idUsuario in idUsuarios)
                {
                    // Obtener todas las asignaciones del usuario para el proyecto actual.
                    // Las fechas del elemento provienen de tbProyectoElemento y el nombre del elemento de tbElementos.
                    var asignacionesUsuario = db.tbRupActividadAsignaciones
                        .Where(aa => aa.idUsuario == idUsuario && aa.idElemento.HasValue) // Asegura que el idElemento en la asignación no es nulo
                        .Join(
                            db.tbProyectoElemento.Where(pe => pe.idProyecto == idProyecto && pe.idElemento.HasValue), // Elementos del proyecto actual con idElemento no nulo
                            aa => aa.idElemento.Value, // Clave de join desde tbRupActividadAsignaciones (usar .Value por ser nullable)
                            pe => pe.idElemento.Value, // Clave de join desde tbProyectoElemento (usar .Value por ser nullable)
                            (aa, pe) => new { // 'aa' es tbRupActividadAsignaciones, 'pe' es tbProyectoElemento
                                FechaInicioElementoProyecto = pe.fechaInicio,
                                FechaFinElementoProyecto = pe.fechaFin,
                                // Acceder al nombre del elemento a través de la propiedad de navegación en tbProyectoElemento
                                // Asumiendo que tu clase EF tbProyectoElemento tiene una propiedad 'tbElementos' que referencia a la entidad tbElementos.
                                NombreDelElemento = (pe.tbElementos != null ? pe.tbElementos.nombre : "Elemento Desconocido")
                            }
                        )
                        .ToList();

                    foreach (var asignacionExistente in asignacionesUsuario)
                    {
                        // Solo verificar solapamiento si la asignación existente tiene fechas válidas.
                        if (asignacionExistente.FechaInicioElementoProyecto.HasValue && asignacionExistente.FechaFinElementoProyecto.HasValue)
                        {
                            if (FechasSeSolapan(fechaInicioElemento, fechaFinElemento, // Fechas del nuevo elemento/elemento a editar
                                                asignacionExistente.FechaInicioElementoProyecto.Value, asignacionExistente.FechaFinElementoProyecto.Value)) // Fechas de la asignación existente
                            {
                                var usuarioInfo = db.tbUsuarios.Find(idUsuario);
                                // CAMBIAR 'nombreUsuario' SI TU CAMPO SE LLAMA DIFERENTE EN tbUsuarios
                                string nombreUsuario = usuarioInfo?.nombreUsuario ?? $"ID:{idUsuario}"; // Asegúrate que tbUsuarios.nombreUsuario es el campo correcto
                                return Json(new { success = false, message = $"El usuario '{nombreUsuario}' ya tiene el elemento '{asignacionExistente.NombreDelElemento}' asignado ({asignacionExistente.FechaInicioElementoProyecto.Value:dd/MM/yy}-{asignacionExistente.FechaFinElementoProyecto.Value:dd/MM/yy}) que se solapa con las nuevas fechas ({fechaInicioElemento:dd/MM/yy}-{fechaFinElemento:dd/MM/yy})." });
                            }
                        }
                    }
                }


                var proyectoElemento = db.tbProyectoElemento.FirstOrDefault(pe => pe.idElemento == idElemento && pe.idProyecto == idProyecto);
                if (proyectoElemento == null)
                {
                    return Json(new { success = false, message = "Elemento del proyecto no encontrado." });
                }
                proyectoElemento.fechaInicio = fechaInicioElemento;
                proyectoElemento.fechaFin = fechaFinElemento;
                // Si el elemento en tbProyectoElemento tenía un idRol, lo dejamos, pues es una propiedad del elemento en el proyecto.
                // La asignación específica a un usuario con un rol se maneja en tbRupActividadAsignaciones.
                // proyectoElemento.idRol = idRolSeleccionado; // No es necesario cambiar el rol general del elemento aquí.

                // NOTA SOBRE idActividadAsignacion:
                // Tu DDL para tbRupActividadAsignaciones no indica que idActividadAsignacion sea IDENTITY.
                // Si NO es IDENTITY, esta parte fallará. Debes generar el ID manualmente o modificar la tabla.
                // Ejemplo si no es IDENTITY (NO RECOMENDADO PARA ALTA CONCURRENCIA SIN BLOQUEOS):
                // int nextId = (db.tbRupActividadAsignaciones.Max(a => (int?)a.idActividadAsignacion) ?? 0) + 1;

                foreach (var idUsuario in idUsuarios)
                {
                    // Verificar si ya existe esta asignación exacta para evitar duplicados (si es una preocupación)
                    bool yaExiste = db.tbRupActividadAsignaciones.Any(aa => aa.idElemento == idElemento && aa.idUsuario == idUsuario);
                    if (!yaExiste)
                    {
                        var nuevaAsignacion = new tbRupActividadAsignaciones
                        {
                            // idActividadAsignacion = nextId++, // Solo si NO es IDENTITY
                            idUsuario = idUsuario,
                            idElemento = idElemento
                            // No hay idProyecto aquí, se infiere
                        };
                        db.tbRupActividadAsignaciones.Add(nuevaAsignacion);
                    }
                }

                db.SaveChanges();
                return Json(new { success = true, message = "Asignación(es) guardada(s) correctamente." });
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                var errorMessages = ex.EntityValidationErrors
                    .SelectMany(x => x.ValidationErrors)
                    .Select(x => x.ErrorMessage);
                var fullErrorMessage = string.Join("; ", errorMessages);
                // Loggear el error ex y fullErrorMessage
                return Json(new { success = false, message = "Error de validación: " + fullErrorMessage });
            }
            catch (Exception ex)
            {
                // Loggear el error ex
                return Json(new { success = false, message = "Error al guardar la asignación: " + ex.InnerException?.Message ?? ex.Message });
            }
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