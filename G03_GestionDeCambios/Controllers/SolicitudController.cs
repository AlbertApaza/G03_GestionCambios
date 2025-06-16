using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using G03_GestionDeCambios.Models;
using G03_GestionDeCambios.Service;

namespace G03_GestionDeCambios.Controllers
{
    public class SolicitudController : Controller
    {
        ProyectoService _proyectoService = new ProyectoService();
        SolicitudService _solicitudService = new SolicitudService();
        private int RetornarPasoActualdeSolicitud(int idSolicitud) { return 3; /*Por ejemplo*/ }

        // GET: ProcesoCambio
        public ActionResult Index()
        {
            // Es crucial verificar que el usuario ha iniciado sesión.
            if (Session["idUsuario"] == null)
            {
                // Redirigir a la página de login
                return RedirectToAction("Login", "Account");
            }
            var idUsuario = Convert.ToInt32(Session["idUsuario"]);

            var viewModel = new SolicitudIndexViewModel();

            // 1. Cargar la lista de solicitudes existentes
            viewModel.Solicitudes = _solicitudService.GetSolicitudesParaUsuario(idUsuario);

            // 2. Preparar el formulario de creación para el modal
            viewModel.FormularioCreacion.ProyectosDisponibles = _proyectoService.ProyectosUsuarioDropDown(idUsuario);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(SolicitudIndexViewModel model)
        {
            var idUsuario = Convert.ToInt32(Session["idUsuario"]);
            var formulario = model.FormularioCreacion;

            if (ModelState.IsValid)
            {
                try
                {
                    _solicitudService.CrearSolicitud(formulario, idUsuario);
                    TempData["SuccessMessage"] = "Solicitud de cambio creada exitosamente.";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    // Loggear el error 'ex'
                    ModelState.AddModelError("", "Ocurrió un error al guardar la solicitud. Por favor, intente de nuevo.");
                }
            }

            // Si el modelo no es válido, volvemos a cargar la vista Index
            // pero con los datos y errores existentes.
            var viewModel = new SolicitudIndexViewModel();
            viewModel.Solicitudes = _solicitudService.GetSolicitudesParaUsuario(idUsuario);
            viewModel.FormularioCreacion = formulario; // Mantiene los datos que el usuario ingresó
            viewModel.FormularioCreacion.ProyectosDisponibles = _proyectoService.ProyectosUsuarioDropDown(idUsuario);
            // Si ya se había seleccionado un proyecto, cargamos los elementos
            if (formulario.IdProyecto.HasValue)
            {
                viewModel.FormularioCreacion.ElementosDisponibles = _solicitudService.GetElementosConfiguracionPorProyecto(formulario.IdProyecto.Value);
            }

            // Indicamos a la vista que debe mostrar el modal con errores
            ViewBag.ShowModalOnError = true;

            return View("Index", viewModel);
        }

        // Acción para obtener elementos vía AJAX
        [HttpGet]
        public JsonResult GetElementosPorProyecto(int idProyecto)
        {
            var elementos = _solicitudService.GetElementosConfiguracionPorProyecto(idProyecto);
            return Json(elementos, JsonRequestBehavior.AllowGet);
        }
    }
}