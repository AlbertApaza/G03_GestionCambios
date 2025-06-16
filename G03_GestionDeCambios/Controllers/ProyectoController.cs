using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.ModelBinding;
using System.Web.Mvc;
using G03_GestionDeCambios.Models;
using G03_GestionDeCambios.Service;

namespace G03_GestionDeCambios.Controllers
{
    public class ProyectoController : Controller
    {
        ProyectoService _proyectoService = new ProyectoService();
        ProyectoUsuarioService _proyectoUsuarioService = new ProyectoUsuarioService();
        ProyectoElementoService _proyectoElementoService = new ProyectoElementoService();


        // GET: Proyecto
        //Listar Proyectos
        public ActionResult Index()
        {
            
            var Proyectos = _proyectoService.ListarProyectos(Convert.ToInt32(Session["idUsuario"]));
            return View(Proyectos);
        }
        public ActionResult Crear()
        {
            var model = _proyectoService.ObtenerMetodologias();
            return View(model);
        }
        public JsonResult ObtenerCiclos(int idMetodologia)
        {
            try
            {
                var ciclos = _proyectoService.ObtenerCicloPorMetodologia(idMetodologia);
                return Json(ciclos, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        public ActionResult Crear(CrearProyectoViewModel model)
        {
            try
            {
                int idProyecto = _proyectoService.CrearProyecto(new tbProyectos
                {
                    nombre = model.Nombre,
                    fechaInicio = model.FechaInicio,
                    fechaFin = model.FechaFin,
                    idUsuarioCreador = Session["idUsuario"] != null ? (int)Session["idUsuario"] : 1,
                    idMetodologia = model.IdMetodologia,
                    codCicloActual = model.CodCicloActual
                });
                ViewBag.ProjectId = idProyecto;

                _proyectoUsuarioService.AgregarUsuarioAProyecto(
                    idProyecto,
                    (int)Session["idUsuario"],
                    model.IdMetodologia == 1 ? 31 : model.IdMetodologia == 2 ? 32 : 33
                );
                return RedirectToAction("Elementos", "Proyecto", new { idProyecto });
            }
            catch (Exception ex)
            {
                throw;
            }
        }


        // --- GESTIÓN DE USUARIOS ---
        public ActionResult Usuarios(int idProyecto)
        {
            ViewBag.ProjectId = idProyecto;
            var proyecto = _proyectoService.ObtenerProyectoPorId(idProyecto);


            var viewModel = new ProyectoUsuariosViewModel
            {
                IdProyecto = proyecto.idProyecto,
                NombreProyecto = proyecto.nombre,
                UsuariosAsignados = _proyectoUsuarioService.ObtenerUsuariosPorProyecto(idProyecto),
                TodosLosUsuarios = _proyectoUsuarioService.ObtenerTodosLosUsuariosParaDropdown(),
                RolesDisponibles = _proyectoUsuarioService.ObtenerRolesPorMetodologia(proyecto.idMetodologia) 
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AgregarUsuario(ProyectoUsuariosViewModel model)
        {
            try
            {

                _proyectoUsuarioService.AgregarUsuarioAProyecto(
                    model.IdProyecto,
                    model.UsuarioAAgregarId.Value,
                    model.RolParaNuevoUsuarioId.Value // Pasar el idRol
                );
                TempData["Exito"] = "Usuario agregado al proyecto correctamente con su rol.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al agregar usuario: " + ex.Message;
            }

            return RedirectToAction("Usuarios", new { idProyecto = model.IdProyecto });
        }
        
        public ActionResult Elementos(int idProyecto)
        {
            ViewBag.ProjectId = idProyecto;
            var proyecto = _proyectoService.ObtenerProyectoConMetodologia(idProyecto);
            if (proyecto == null) return HttpNotFound("Proyecto no encontrado");

            var ciclosMetodologia = _proyectoService.ObtenerCiclosPorMetodologiaConOrden(proyecto.idMetodologia);
            var proyectoCiclosDB = _proyectoService.ObtenerProyectoCiclos(idProyecto);
            var proyectoElementosDB = _proyectoElementoService.ObtenerElementosPorProyectoConDetalles(idProyecto);

            var todosElementosMaestros = _proyectoElementoService.ObtenerTodosElementos()
                                           .Select(e => new SelectListItem { Value = e.idElemento.ToString(), Text = e.nombre })
                                           .ToList();
            todosElementosMaestros.Insert(0, new SelectListItem { Value = "", Text = "-- Seleccione Elemento --" });

            // Obtener roles para la metodología del proyecto (para el dropdown de NUEVO elemento)
            var rolesParaDropdown = _proyectoUsuarioService.ObtenerRolesPorMetodologia(proyecto.idMetodologia);
            // Asegúrate que _proyectoUsuarioService.ObtenerRolesPorMetodologia DEVUELVE una lista de SelectListItem
            // y que los Value son strings.
            // Si devuelve List<tbRoles>, necesitas convertir:
            // var rolesParaDropdown = _proyectoUsuarioService.ObtenerRolesPorMetodologia(proyecto.idMetodologia)
            //                              .Select(r => new SelectListItem { Value = r.idRol.ToString(), Text = r.nombre})
            //                              .ToList();
            rolesParaDropdown.Insert(0, new SelectListItem { Value = "", Text = "-- Seleccione Rol --" });


            var viewModel = new ProyectoElementosViewModel
            {
                IdProyecto = proyecto.idProyecto,
                NombreProyecto = proyecto.nombre,
                NombreMetodologia = proyecto.tbMetodologias.nombre,
                FechaInicioProyecto = proyecto.fechaInicio,
                FechaFinProyecto = proyecto.fechaFin,
                TodosLosElementosDisponibles = todosElementosMaestros,
                RolesDisponiblesParaElementos = rolesParaDropdown, // Para el dropdown del nuevo elemento
                CiclosDelProyecto = new List<CicloGestionViewModel>()
            };

            foreach (var cicloMet in ciclosMetodologia.OrderBy(c => c.orden))
            {
                var proyectoCicloActual = proyectoCiclosDB.FirstOrDefault(pc => pc.codCiclo == cicloMet.codCiclo);
                var cicloVM = new CicloGestionViewModel
                {
                    CodCiclo = cicloMet.codCiclo,
                    NombreCiclo = cicloMet.nombre,
                    OrdenCiclo = cicloMet.orden ?? 0,
                    IdProyectoCiclo = proyectoCicloActual?.idProyectoCiclo ?? 0,
                    FechaInicioCiclo = proyectoCicloActual?.inicioCiclo,
                    FechaFinCiclo = proyectoCicloActual?.finCiclo,
                    ElementosAsignados = proyectoElementosDB
                                            .Where(pe => pe.codCiclo == cicloMet.codCiclo)
                                            .Select(peDB => new ElementoAsignadoCicloViewModel
                                            {
                                                IdProyectoElemento = peDB.idProyectoElemento,
                                                IdElemento = (int)peDB.idElemento,
                                                NombreElemento = peDB.tbElementos?.nombre ?? "N/A",
                                                FechaInicioElemento = peDB.fechaInicio,
                                                FechaFinElemento = peDB.fechaFin,
                                                CodCiclo = peDB.codCiclo,
                                                IdRol = peDB.idRol, // Mantenemos para la lógica interna si es necesario
                                                NombreRol = peDB.tbRoles?.nombre ?? "No asignado" // Esto se mostrará
                                            }).ToList()
                };
                viewModel.CiclosDelProyecto.Add(cicloVM);
            }
            return View("Elementos", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Elementos(ProyectoElementosViewModel model)
        {
            //var proyectoDB = _proyectoService.ObtenerProyectoPorId(model.IdProyecto);
            var proyectoDB = _proyectoService.ObtenerProyectoConMetodologia(model.IdProyecto); // Necesitamos la metodología para los roles
            if (proyectoDB == null) return HttpNotFound();

            model.NombreProyecto = proyectoDB.nombre;
            model.NombreMetodologia = proyectoDB.tbMetodologias.nombre ?? "N/A";
            model.FechaInicioProyecto = proyectoDB.fechaInicio;
            model.FechaFinProyecto = proyectoDB.fechaFin;
            model.TodosLosElementosDisponibles = _proyectoElementoService.ObtenerTodosElementos()
                                           .Select(e => new SelectListItem { Value = e.idElemento.ToString(), Text = e.nombre })
                                           .ToList();
            model.TodosLosElementosDisponibles.Insert(0, new SelectListItem { Value = "", Text = "-- Seleccione Elemento --" });



            var rolesDisponibles = _proyectoUsuarioService.ObtenerRolesPorMetodologia(proyectoDB.idMetodologia);
            rolesDisponibles.Insert(0, new SelectListItem { Value = "", Text = "-- Seleccione Rol --" });
            model.RolesDisponiblesParaElementos = rolesDisponibles;





            DateTime? fechaFinCicloAnteriorValidada = null;

            for (int i = 0; i < model.CiclosDelProyecto.Count; i++)
            {
                var cicloVM = model.CiclosDelProyecto[i];

                if (cicloVM.FechaInicioCiclo.HasValue || cicloVM.FechaFinCiclo.HasValue)
                {
                    if (!cicloVM.FechaInicioCiclo.HasValue)
                        ModelState.AddModelError($"CiclosDelProyecto[{i}].FechaInicioCiclo", $"Inicio del ciclo '{cicloVM.NombreCiclo}' es requerido si se define una fecha fin.");
                    if (!cicloVM.FechaFinCiclo.HasValue)
                        ModelState.AddModelError($"CiclosDelProyecto[{i}].FechaFinCiclo", $"Fin del ciclo '{cicloVM.NombreCiclo}' es requerido si se define una fecha inicio.");

                    if (cicloVM.FechaInicioCiclo.HasValue)
                    {
                        if (cicloVM.FechaInicioCiclo < model.FechaInicioProyecto)
                            ModelState.AddModelError($"CiclosDelProyecto[{i}].FechaInicioCiclo", $"Ciclo '{cicloVM.NombreCiclo}' no puede iniciar antes que el proyecto ({model.FechaInicioProyecto:yyyy-MM-dd}).");
                        if (model.FechaFinProyecto.HasValue && cicloVM.FechaInicioCiclo > model.FechaFinProyecto.Value)
                            ModelState.AddModelError($"CiclosDelProyecto[{i}].FechaInicioCiclo", $"Ciclo '{cicloVM.NombreCiclo}' no puede iniciar después del fin del proyecto ({model.FechaFinProyecto.Value:yyyy-MM-dd}).");
                        if (fechaFinCicloAnteriorValidada.HasValue && cicloVM.FechaInicioCiclo < fechaFinCicloAnteriorValidada.Value)
                            ModelState.AddModelError($"CiclosDelProyecto[{i}].FechaInicioCiclo", $"Ciclo '{cicloVM.NombreCiclo}' debe iniciar el mismo día o después que finaliza el ciclo anterior ({fechaFinCicloAnteriorValidada.Value:yyyy-MM-dd}).");
                    }

                    if (cicloVM.FechaFinCiclo.HasValue)
                    {
                        if (cicloVM.FechaInicioCiclo.HasValue && cicloVM.FechaFinCiclo < cicloVM.FechaInicioCiclo)
                            ModelState.AddModelError($"CiclosDelProyecto[{i}].FechaFinCiclo", $"Fin del ciclo '{cicloVM.NombreCiclo}' debe ser posterior o igual a su inicio.");
                        if (model.FechaFinProyecto.HasValue && cicloVM.FechaFinCiclo > model.FechaFinProyecto.Value)
                            ModelState.AddModelError($"CiclosDelProyecto[{i}].FechaFinCiclo", $"Ciclo '{cicloVM.NombreCiclo}' no puede finalizar después del fin del proyecto ({model.FechaFinProyecto.Value:yyyy-MM-dd}).");
                    }

                    if (ModelState.IsValidField($"CiclosDelProyecto[{i}].FechaInicioCiclo") && ModelState.IsValidField($"CiclosDelProyecto[{i}].FechaFinCiclo") && cicloVM.FechaInicioCiclo.HasValue && cicloVM.FechaFinCiclo.HasValue)
                    {
                        fechaFinCicloAnteriorValidada = cicloVM.FechaFinCiclo;

                        for (int j = 0; j < cicloVM.ElementosAsignados.Count; j++)
                        {
                            var elVM = cicloVM.ElementosAsignados[j];
                            if (!elVM.MarcadoParaEliminar && (elVM.FechaInicioElemento.HasValue || elVM.FechaFinElemento.HasValue))
                            {
                                if (!elVM.FechaInicioElemento.HasValue) ModelState.AddModelError($"CiclosDelProyecto[{i}].ElementosAsignados[{j}].FechaInicioElemento", "Inicio es requerido.");
                                else if (elVM.FechaInicioElemento < cicloVM.FechaInicioCiclo.Value) ModelState.AddModelError($"CiclosDelProyecto[{i}].ElementosAsignados[{j}].FechaInicioElemento", "No antes del ciclo.");
                                else if (elVM.FechaInicioElemento > cicloVM.FechaFinCiclo.Value) ModelState.AddModelError($"CiclosDelProyecto[{i}].ElementosAsignados[{j}].FechaInicioElemento", "No después del ciclo.");

                                if (!elVM.FechaFinElemento.HasValue) ModelState.AddModelError($"CiclosDelProyecto[{i}].ElementosAsignados[{j}].FechaFinElemento", "Fin es requerido.");
                                else if (elVM.FechaInicioElemento.HasValue && elVM.FechaFinElemento < elVM.FechaInicioElemento) ModelState.AddModelError($"CiclosDelProyecto[{i}].ElementosAsignados[{j}].FechaFinElemento", "Fin >= Inicio.");
                                else if (elVM.FechaFinElemento > cicloVM.FechaFinCiclo.Value) ModelState.AddModelError($"CiclosDelProyecto[{i}].ElementosAsignados[{j}].FechaFinElemento", "No después del ciclo.");
                                // Validaciones de Rol eliminadas

                                
                            }
                        }
                        if (cicloVM.IdElementoAAgregar.HasValue && cicloVM.IdElementoAAgregar > 0)
                        {
                            if (!cicloVM.FechaInicioNuevoElemento.HasValue) ModelState.AddModelError($"CiclosDelProyecto[{i}].FechaInicioNuevoElemento", "Inicio nuevo elem. requerido.");
                            else if (cicloVM.FechaInicioNuevoElemento < cicloVM.FechaInicioCiclo.Value) ModelState.AddModelError($"CiclosDelProyecto[{i}].FechaInicioNuevoElemento", "No antes del ciclo.");
                            else if (cicloVM.FechaInicioNuevoElemento > cicloVM.FechaFinCiclo.Value) ModelState.AddModelError($"CiclosDelProyecto[{i}].FechaInicioNuevoElemento", "No después del ciclo.");

                            if (!cicloVM.FechaFinNuevoElemento.HasValue) ModelState.AddModelError($"CiclosDelProyecto[{i}].FechaFinNuevoElemento", "Fin nuevo elem. requerido.");
                            else if (cicloVM.FechaInicioNuevoElemento.HasValue && cicloVM.FechaFinNuevoElemento < cicloVM.FechaInicioNuevoElemento) ModelState.AddModelError($"CiclosDelProyecto[{i}].FechaFinNuevoElemento", "Fin >= Inicio.");
                            else if (cicloVM.FechaFinNuevoElemento > cicloVM.FechaFinCiclo.Value) ModelState.AddModelError($"CiclosDelProyecto[{i}].FechaFinNuevoElemento", "No después del ciclo.");

                            // NUEVO: Validación de Rol para nuevo elemento
                            if (!cicloVM.IdRolNuevoElemento.HasValue) // Asumiendo que el rol es obligatorio para nuevos elementos
                                ModelState.AddModelError($"CiclosDelProyecto[{i}].IdRolNuevoElemento", "Rol para nuevo elemento es requerido.");
                            else if (!model.RolesDisponiblesParaElementos.Any(r => r.Value == cicloVM.IdRolNuevoElemento.ToString()))
                                ModelState.AddModelError($"CiclosDelProyecto[{i}].IdRolNuevoElemento", "Rol seleccionado no es válido.");
                        }
                    }
                    else
                    {
                        fechaFinCicloAnteriorValidada = null;
                    }
                }
                else
                {
                    fechaFinCicloAnteriorValidada = null;
                }
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Existen errores de validación. Por favor, corríjalos.";
                return View("Elementos", model);
            }

            try
            {
                _proyectoService.GuardarProyectoCiclos(model.IdProyecto, model.CiclosDelProyecto);
                _proyectoElementoService.GestionarElementosDeProyecto(model.IdProyecto, model.CiclosDelProyecto);

                TempData["Exito"] = "Gestión de elementos del proyecto guardada correctamente.";
                return RedirectToAction("Elementos", new { idProyecto = model.IdProyecto });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al guardar la gestión de elementos: " + ex.Message);
                TempData["Error"] = "Error al guardar: " + ex.Message;
                return View("Elementos", model);
            }
        }
    }
}