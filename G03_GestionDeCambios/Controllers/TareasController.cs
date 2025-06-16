// ~/Controllers/TareasController.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;
using G03_GestionDeCambios.Models;
using G03_GestionDeCambios.Service;
using G03_GestionDeCambios.ViewModels.TareasViewModels;

namespace G03_GestionDeCambios.Controllers
{
    public class TareasController : Controller
    {
        private readonly TareasService _tareasService;
        private readonly RolService _rolService;

        public TareasController()
        {
            _tareasService = new TareasService();
            _rolService = new RolService();
        }

        // GET: Tareas
        public ActionResult Tareas(int idProyecto) // Cambiado de Index a Tareas como en tu ejemplo
        {
            if (Session["idUsuario"] == null)
            {
                TempData["ErrorMessage"] = "Debe iniciar sesión para ver sus tareas.";
                return RedirectToAction("Index", "Login");
            }
            var idUsuario = Convert.ToInt32(Session["idUsuario"]);
            ViewBag.ProjectId = idProyecto;

            var proyecto = _tareasService.GetProyectoById(idProyecto);
            if (proyecto == null)
            {
                return HttpNotFound("Proyecto no encontrado.");
            }

            var viewModel = new TareasUsuarioIndexViewModel
            {
                IdProyecto = idProyecto,
                NombreProyecto = proyecto.nombre,
                CicloActualProyecto = proyecto.tbCiclos?.nombre ?? "N/A (Proyecto sin ciclo actual)",
                Tareas = _tareasService.GetTareasParaUsuarioEnProyecto(idUsuario, idProyecto)
            };

            return View(viewModel); 
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ActualizarEstadoTarea(int idTarea, string nuevoEstado, int idProyecto)
        {
            if (Session["idUsuario"] == null)
            {
                return Json(new { success = false, message = "Sesión expirada. Por favor, inicie sesión nuevamente." });
            }
            var idUsuario = Convert.ToInt32(Session["idUsuario"]);

            Debug.WriteLine($"Controller: Intentando actualizar tarea ID: {idTarea} a estado: {nuevoEstado} para proyecto ID: {idProyecto} por usuario: {idUsuario}");

            try
            {
                bool actualizado = _tareasService.ActualizarEstadoTarea(idTarea, nuevoEstado, idUsuario);
                if (actualizado)
                {
                    TempData["SuccessMessage"] = "Estado de la tarea actualizado correctamente.";
                    // Para AJAX response
                    if (Request.IsAjaxRequest())
                    {
                        return Json(new { success = true, message = "Estado actualizado.", nuevoEstado = nuevoEstado, idTarea = idTarea });
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "No se pudo actualizar el estado de la tarea. Puede que no pertenezca al ciclo actual, ya esté finalizada o no tenga permisos.";
                    if (Request.IsAjaxRequest())
                    {
                        return Json(new { success = false, message = "Error al actualizar. La tarea podría no ser del ciclo actual, estar finalizada, o no tener permisos." });
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al actualizar estado: " + ex.Message;
                Debug.WriteLine($"Controller EXCEPTION: {ex.Message}");
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = "Error del servidor: " + ex.Message });
                }
            }

            return RedirectToAction("Tareas", new { idProyecto = idProyecto });
        }



        // En la acción GET: Crear(int idProyecto)
        public ActionResult Crear(int idProyecto)
        {
            int? idRolProyecto = _rolService.ObtenerRolProyecto(Convert.ToInt32(Session["idUsuario"]), idProyecto);
            Debug.WriteLine($"Rol del usuario en el proyecto: {idRolProyecto}");
            if (idRolProyecto != 31 && idRolProyecto != 32 && idRolProyecto != 33)
            {
                Debug.WriteLine("No tiene permisos");
                TempData["ErrorMessagePermiso"] = "No tiene permisos para crear tareas en este proyecto.";
                return RedirectToAction("Tareas", new { idProyecto = idProyecto });
            }

            ViewBag.ProjectId = idProyecto;
            var proyecto = _tareasService.GetProyectoById(idProyecto);
            if (proyecto == null)
            {
                return HttpNotFound("Proyecto no encontrado.");
            }

            var viewModel = new TareasIndexViewModel
            {
                IdProyecto = idProyecto,
                NombreProyecto = proyecto.nombre,
                CicloActual = proyecto.tbCiclos?.nombre ?? "N/A",
                ElementosConfiguracion = _tareasService.GetElementosConfiguracionParaAsignacion(idProyecto),
                FormularioCrearTarea = new CrearTareaViewModel { IdProyecto = idProyecto },
                // POBLAR TAREAS EXISTENTES
                TareasExistentes = _tareasService.GetTareasDetalladasPorProyectoYCiclo(idProyecto)
            };

            viewModel.ElementosSelectList = new SelectList(
                viewModel.ElementosConfiguracion,
                nameof(ProyectoElementoViewModel.IdProyectoElemento),
                nameof(ProyectoElementoViewModel.NombreElemento)
            );
            viewModel.UsuariosSelectList = new SelectList(Enumerable.Empty<SelectListItem>());

            return View(viewModel);
        }

        // GET: Tareas/GetUsuariosDisponiblesJson
        [HttpGet]
        public JsonResult GetUsuariosDisponiblesJson(int idProyectoElemento)
        {
            try
            {
                var usuarios = _tareasService.GetUsuariosDisponiblesParaElemento(idProyectoElemento);
                var selectListItems = usuarios.Select(u => new SelectListItem
                {
                    Value = u.IdUsuario.ToString(),
                    Text = u.NombreCompletoUsuario
                }).ToList();
                return Json(selectListItems, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                // Log ex
                return Json(new { error = "Error al obtener usuarios: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // GET: Tareas/GetElementoDetallesJson
        [HttpGet]
        public JsonResult GetElementoDetallesJson(int idProyectoElemento)
        {
            try
            {
                var elemento = _tareasService.GetProyectoElementoById(idProyectoElemento);
                if (elemento == null)
                {
                    return Json(new { error = "Elemento no encontrado" }, JsonRequestBehavior.AllowGet);
                }
                return Json(new
                {
                    nombreElemento = elemento.tbElementos?.nombre, 
                    fechaInicio = elemento.fechaInicio?.ToString("dd/MM/yyyy"),
                    fechaFin = elemento.fechaFin?.ToString("dd/MM/yyyy") ?? "Abierto",
                    rolRequerido = elemento.tbRoles?.nombre 
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = "Error al obtener detalles del elemento: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(TareasIndexViewModel viewModelConFormulario)
        {
            CrearTareaViewModel model = viewModelConFormulario.FormularioCrearTarea;
            Debug.WriteLine("--- INICIO MÉTODO CREAR (POST) ---");
            Debug.WriteLine($"Valor recibido para IdProyecto: {model.IdProyecto}");
            Debug.WriteLine($"Valor recibido para IdProyectoElemento: {model.IdProyectoElemento}");
            Debug.WriteLine($"Valor recibido para IdUsuario: {model.IdUsuario}");
            Debug.WriteLine($"Valor recibido para NombreTarea: '{model.NombreTarea}'");
            Debug.WriteLine($"Valor recibido para DescripcionTarea: '{model.DescripcionTarea}'");

            if (!ModelState.IsValid)
            {
                Debug.WriteLine("ModelState NO es válido. Errores:");
                foreach (var key in ModelState.Keys)
                {
                    var state = ModelState[key];
                    if (state.Errors.Any())
                    {
                        Debug.WriteLine($"Campo: {key}");
                        foreach (var error in state.Errors)
                        {
                            Debug.WriteLine($"  - Error: {error.ErrorMessage}");
                            if (error.Exception != null)
                            {
                                Debug.WriteLine($"    - Excepción: {error.Exception.Message}");
                            }
                        }
                    }
                }
            }
            else
            {
                Debug.WriteLine("ModelState ES válido.");
            }


            if (ModelState.IsValid)
            {
                try
                {
                    Debug.WriteLine("Intentando crear la tarea en el servicio...");
                    _tareasService.CrearNuevaTarea(
                        model.IdProyectoElemento,
                        model.IdUsuario,
                        model.NombreTarea,
                        model.DescripcionTarea
                    );
                    TempData["SuccessMessage"] = "Tarea creada y asignada exitosamente.";
                    Debug.WriteLine("Tarea creada exitosamente. Redirigiendo...");
                    return RedirectToAction("Crear", new { idProyecto = model.IdProyecto });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"EXCEPCIÓN al llamar a _tareasService.CrearNuevaTarea: {ex.Message}");
                    Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                    if (ex.InnerException != null)
                    {
                        Debug.WriteLine($"InnerException: {ex.InnerException.Message}");
                    }
                    ModelState.AddModelError("", "Error al crear la tarea: " + ex.Message);
                }
            }

            Debug.WriteLine("Recargando la vista debido a errores o ModelState inválido.");

            var proyecto = _tareasService.GetProyectoById(model.IdProyecto);
            var viewModelParaVista = new TareasIndexViewModel
            {
                IdProyecto = model.IdProyecto, // Usa model.IdProyecto
                NombreProyecto = proyecto?.nombre,
                CicloActual = proyecto?.tbCiclos?.nombre ?? "N/A",
                ElementosConfiguracion = _tareasService.GetElementosConfiguracionParaAsignacion(model.IdProyecto), 
                FormularioCrearTarea = model, 
                TareasExistentes = _tareasService.GetTareasDetalladasPorProyectoYCiclo(model.IdProyecto)
            };

            viewModelParaVista.ElementosSelectList = new SelectList(
                viewModelParaVista.ElementosConfiguracion,
                nameof(ProyectoElementoViewModel.IdProyectoElemento),
                nameof(ProyectoElementoViewModel.NombreElemento),
                model.IdProyectoElemento
            );

            if (model.IdProyectoElemento > 0)
            {
                var usuariosDisponibles = _tareasService.GetUsuariosDisponiblesParaElemento(model.IdProyectoElemento);
                viewModelParaVista.UsuariosSelectList = new SelectList(
                    usuariosDisponibles,
                    nameof(UsuarioDisponibleViewModel.IdUsuario),
                    nameof(UsuarioDisponibleViewModel.NombreCompletoUsuario),
                    model.IdUsuario
                );
            }
            else
            {
                viewModelParaVista.UsuariosSelectList = new SelectList(Enumerable.Empty<SelectListItem>());
            }

            if (!ModelState.IsValid) 
            {
                
                if (TempData["ErrorMessage"] == null)
                {
                    TempData["ErrorMessage"] = "No se pudo crear la tarea. Revise los errores de validación.";
                }
            }
            else if (TempData["ErrorMessage"] == null) 
            {
                TempData["ErrorMessage"] = "Ocurrió un error inesperado al intentar crear la tarea.";
            }


            Debug.WriteLine("--- FIN MÉTODO CREAR (POST) ---");
            return View("Crear", viewModelParaVista);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ActualizarTareaAdmin(int idTarea, string nombreTarea, string descripcionTarea, string estadoTarea, int idProyecto)
        {
            var idUsuarioActual = Convert.ToInt32(Session["idUsuario"]); // O como obtengas el ID del admin

            if (string.IsNullOrWhiteSpace(nombreTarea))
            {
                return Json(new { success = false, message = "El nombre de la tarea no puede estar vacío." });
            }

            string mensajeServicio;
            bool actualizado = false;

            try
            {
                Debug.WriteLine($"Controller (Admin): Actualizando Tarea ID: {idTarea}, Nombre: {nombreTarea}, Estado: {estadoTarea}");
                actualizado = _tareasService.ActualizarTareaDetallada(idTarea, nombreTarea, descripcionTarea, estadoTarea, idUsuarioActual, out mensajeServicio);

                if (actualizado)
                {

                    if (string.IsNullOrEmpty(mensajeServicio))
                    {
                        mensajeServicio = "Tarea actualizada correctamente.";
                    }
                }
                else if (string.IsNullOrEmpty(mensajeServicio))
                {
                    mensajeServicio = "No se pudo actualizar la tarea.";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Controller EXCEPTION ActualizarTareaAdmin: {ex.Message}");
                mensajeServicio = "Error en el servidor: " + ex.Message;
                actualizado = false;
            }

            return Json(new
            {
                success = actualizado,
                message = mensajeServicio,
                idTarea = idTarea,
                nombreTarea = actualizado ? nombreTarea : null,
                descripcionTarea = actualizado ? descripcionTarea : null,
                estadoTarea = actualizado ? estadoTarea : null
            });
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _tareasService.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}