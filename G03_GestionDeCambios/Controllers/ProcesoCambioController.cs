using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using G03_GestionDeCambios.Models;
using G03_GestionDeCambios.Service;

namespace G03_GestionDeCambios.Controllers
{
    public class ProcesoCambioController : Controller
    {
        private readonly BD_GestionDeCambiosEntities _context;

        public ProcesoCambioController()
        {
            _context = new BD_GestionDeCambiosEntities();
        }
        SolicitudService _solicitudService = new SolicitudService();
        ProcesoCambioService _procesoCambioService = new ProcesoCambioService();
        EstadoSolicitudService _estadoSolicitudService = new EstadoSolicitudService();
        private int RetornarPasoActualdeSolicitud(int idSolicitud) { return 3; /*Por ejemplo*/ }

        [HttpPost]
        [ValidateAntiForgeryToken] // Buena práctica para seguridad
        public ActionResult Rechazar(int idSolicitud, string comentarios)
        {
            var idUsuario = (int)Session["idUsuario"];
            _estadoSolicitudService.RechazarSolicitud(idSolicitud, idUsuario, comentarios);
            TempData["SuccessMessage"] = "La solicitud ha sido rechazada exitosamente.";
            return RedirectToAction("Index", "Solicitud");
        }
        // --- NUEVO ACTION PARA ENVIAR A ANÁLISIS ---
        [HttpPost]
        [ValidateAntiForgeryToken] // Buena práctica para seguridad
        public ActionResult EnviarAnalisis(int idSolicitud, string comentarios)
        {
            var idUsuario = (int)Session["idUsuario"];

            // Llamamos al servicio para que ejecute la lógica
            _estadoSolicitudService.EnviarAnalisis(idSolicitud, idUsuario, comentarios);

            TempData["SuccessMessage"] = "La solicitud fue enviada al área de Análisis correctamente.";

            // Redirigimos a la vista de análisis para continuar el proceso
            return RedirectToAction("VerAnalisis", new { idSolicitud = idSolicitud });
        }

        // id de Solicitd
        public ActionResult VerSolicitud(int idSolicitud)
        {
            var viewModel = _procesoCambioService.GetSolicitudDetalle(idSolicitud);
            if (viewModel == null)
            {
                return HttpNotFound(); // Si la solicitud no existe
            }

            ViewBag.IdSolicitudActual = idSolicitud;
            ViewBag.EstadoRealDelProceso = _estadoSolicitudService.ObtenerPasoActualProceso(idSolicitud);

            return View(viewModel);
        }




        // id de solicitd
        public ActionResult VerAnalisis(int idSolicitud)
        {
            var viewModel = _procesoCambioService.GetAnalisisViewModel(idSolicitud);
            if (viewModel == null)
            {
                return HttpNotFound();
            }

            var idPasoReal = _estadoSolicitudService.ObtenerPasoActualProceso(idSolicitud);
            if (idPasoReal != 2)
            {
                TempData["WarningMessage"] = "La solicitud no se encuentra en la fase de Análisis.";
                return RedirectToAction("VerSolicitud", new { idSolicitud = idSolicitud });
            }

            ViewBag.IdSolicitudActual = idSolicitud;
            ViewBag.EstadoRealDelProceso = idPasoReal;
            return View(viewModel);
        }


        // POST Actions for decisions
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AprobarDesdeAnalisis(int idSolicitud, string comentarios)
        {
            var idUsuario = (int)Session["idUsuario"];
            _estadoSolicitudService.AprobarAnalisis(idSolicitud, idUsuario, comentarios);
            TempData["SuccessMessage"] = "La solicitud fue aprobada y enviada al Comité de Cambios.";
            return RedirectToAction("VerAprobacion", new { idSolicitud = idSolicitud });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RechazarDesdeAnalisis(int idSolicitud, string comentarios)
        {
            var idUsuario = (int)Session["idUsuario"];
            _estadoSolicitudService.RechazarAnalisis(idSolicitud, idUsuario, comentarios);
            TempData["SuccessMessage"] = "La solicitud ha sido rechazada y el proceso ha finalizado.";
            return RedirectToAction("Index", "Solicitud");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PedirInformacion(int idSolicitud, string comentarios)
        {
            var idUsuario = (int)Session["idUsuario"];
            _estadoSolicitudService.SolicitarMasInformacion(idSolicitud, idUsuario, comentarios);
            TempData["InfoMessage"] = "Se ha enviado una solicitud de más información al gestor del proyecto.";
            return RedirectToAction("VerAnalisis", new { idSolicitud = idSolicitud });
        }



        public ActionResult VerAprobacion(int idSolicitud)
        {
            var viewModel = _procesoCambioService.GetAprobacionViewModel(idSolicitud);
            if (viewModel == null)
            {
                TempData["ErrorMessage"] = "No se encontró un análisis aprobado para esta solicitud. No puede continuar.";
                return RedirectToAction("Index", "Solicitud");
            }
            var idPasoReal = _estadoSolicitudService.ObtenerPasoActualProceso(idSolicitud);
            var pasoActual = _estadoSolicitudService.ObtenerPasoActualProceso(idSolicitud);
            if (pasoActual != 3)
            {
                TempData["WarningMessage"] = "La solicitud no se encuentra en la fase de Aprobación Final.";
                // Lo redirigimos a su paso correcto
                return RedirectToAction("VerSolicitud", new { idSolicitud = idSolicitud });
            }
            ViewBag.EstadoRealDelProceso = idPasoReal;
            ViewBag.IdSolicitudActual = idSolicitud;
            return View(viewModel);
        }

        // POST Actions
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AprobarFinal(int idSolicitud, string comentarios)
        {
            var idUsuario = (int)Session["idUsuario"];
            _estadoSolicitudService.AprobarSolicitudFinal(idSolicitud, idUsuario, comentarios);
            TempData["SuccessMessage"] = "¡Solicitud Aprobada! El cambio ha sido planificado para su implementación.";
            return RedirectToAction("VerAsignacion", new { idSolicitud = idSolicitud }); // O a un dashboard de tareas
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RechazarFinal(int idSolicitud, string comentarios)
        {
            var idUsuario = (int)Session["idUsuario"];
            _estadoSolicitudService.RechazarSolicitudFinal(idSolicitud, idUsuario, comentarios);
            TempData["SuccessMessage"] = "La solicitud ha sido rechazada y el proceso ha finalizado.";
            return RedirectToAction("Index", "Solicitud");
        }




        // GET
        public ActionResult VerAsignacion(int idSolicitud)
        {
            var idPasoReal = _estadoSolicitudService.ObtenerPasoActualProceso(idSolicitud);
            var pasoActual = _estadoSolicitudService.ObtenerPasoActualProceso(idSolicitud);
            if (pasoActual != 4)
            {
                TempData["WarningMessage"] = "La solicitud no se encuentra en la fase de Asignación.";
                return RedirectToAction("VerSolicitud", new { idSolicitud });
            }

            var viewModel = _procesoCambioService.GetAsignacionViewModel(idSolicitud);
            if (viewModel == null) return HttpNotFound();

            ViewBag.EstadoRealDelProceso = idPasoReal;
            ViewBag.IdSolicitudActual = idSolicitud;
            return View(viewModel);
        }

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AsignarTareas(AsignacionFormModel model)
        {
            if (!ModelState.IsValid || model.NuevasTareas == null || !model.NuevasTareas.Any())
            {
                TempData["ErrorMessage"] = "No se asignó ninguna tarea. Por favor, defina al menos una actividad.";
                return RedirectToAction("VerAsignacion", new { idSolicitud = model.IdSolicitud });
            }

            try
            {
                var idUsuario = (int)Session["idUsuario"];
                _estadoSolicitudService.AsignarTareasEIniciarImplementacion(model, idUsuario);
                TempData["SuccessMessage"] = "Tareas asignadas correctamente. La implementación ha comenzado.";
                return RedirectToAction("VerDesarrollo", new { idSolicitud = model.IdSolicitud }); // O a un dashboard
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al asignar tareas: " + ex.Message;
                return RedirectToAction("VerAsignacion", new { idSolicitud = model.IdSolicitud });
            }
        }

        // GET
        public ActionResult VerDesarrollo(int idSolicitud)
        {
            var pasoActual = _estadoSolicitudService.ObtenerPasoActualProceso(idSolicitud);
            if (pasoActual != 5)
            {
                TempData["WarningMessage"] = "La solicitud no se encuentra en la fase de Desarrollo.";
                return RedirectToAction("VerSolicitud", new { idSolicitud });
            }

            var viewModel = _procesoCambioService.GetDesarrolloViewModel(idSolicitud);
            if (viewModel == null) return HttpNotFound();

            ViewBag.EstadoRealDelProceso = pasoActual;
            ViewBag.IdSolicitudActual = idSolicitud;
            return View(viewModel);
        }

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EnviarAQA(int idSolicitud, string comentarios)
        {
            try
            {
                var idUsuario = (int)Session["idUsuario"];
                _estadoSolicitudService.EnviarAQA(idSolicitud, idUsuario, comentarios);
                TempData["SuccessMessage"] = "El desarrollo ha sido completado y enviado al equipo de QA para su validación.";
                return RedirectToAction("VerQA", new { idSolicitud });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error: " + ex.Message;
                return RedirectToAction("VerDesarrollo", new { idSolicitud });
            }
        }

        // GET
        public ActionResult VerQA(int idSolicitud)
        {
            var pasoActual = _estadoSolicitudService.ObtenerPasoActualProceso(idSolicitud);
            if (pasoActual != 6)
            {
                TempData["WarningMessage"] = "La solicitud no se encuentra en la fase de QA.";
                return RedirectToAction("VerSolicitud", new { idSolicitud });
            }
            var viewModel = _procesoCambioService.GetQAViewModel(idSolicitud);
            ViewBag.EstadoRealDelProceso = pasoActual;
            ViewBag.IdSolicitudActual = idSolicitud;
            return View(viewModel);
        }

        // POST para decisiones finales
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AprobarParaDespliegue(int idSolicitud, string comentarios)
        {
            try
            {
                _estadoSolicitudService.AprobarParaDespliegue(idSolicitud, (int)Session["idUsuario"], comentarios);
                TempData["SuccessMessage"] = "¡QA Aprobado! El cambio está listo para ser desplegado.";
                return RedirectToAction("VerDespliegue", new { idSolicitud }); // Al siguiente paso
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error: " + ex.Message;
                return RedirectToAction("VerQA", new { idSolicitud });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RetornarADesarrollo(int idSolicitud, string comentarios)
        {
            try
            {
                _estadoSolicitudService.RetornarADesarrollo(idSolicitud, (int)Session["idUsuario"], comentarios);
                TempData["WarningMessage"] = "El cambio ha sido devuelto a Desarrollo con las incidencias reportadas.";
                return RedirectToAction("VerDesarrollo", new { idSolicitud });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error: " + ex.Message;
                return RedirectToAction("VerQA", new { idSolicitud });
            }
        }

        // Endpoint AJAX para registrar una incidencia
        [HttpPost]
        public JsonResult RegistrarIncidencia(int idSolicitud, string descripcion, string severidad, int idDevAsignado)
        {
            try
            {
                _estadoSolicitudService.RegistrarIncidenciaQA(idSolicitud, descripcion, severidad, idDevAsignado, (int)Session["idUsuario"]);
                return Json(new { success = true, message = "Incidencia reportada correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET
        public ActionResult VerDespliegue(int idSolicitud)
        {
            var pasoActual = _estadoSolicitudService.ObtenerPasoActualProceso(idSolicitud);
            if (pasoActual != 7)
            {
                TempData["WarningMessage"] = "La solicitud no se encuentra en la fase de Despliegue.";
                return RedirectToAction("VerSolicitud", new { idSolicitud });
            }

            var viewModel = _procesoCambioService.GetDespliegueViewModel(idSolicitud);
            if (viewModel == null) return HttpNotFound();

            ViewBag.EstadoRealDelProceso = pasoActual;
            ViewBag.IdSolicitudActual = idSolicitud;
            return View(viewModel);
        }

        // POST para planificar un nuevo despliegue
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PlanificarDespliegue(int idSolicitud, int idEntorno, string pasosTexto)
        {
            if (string.IsNullOrWhiteSpace(pasosTexto))
            {
                TempData["ErrorMessage"] = "Debe definir al menos un paso para el despliegue.";
                return RedirectToAction("VerDespliegue", new { idSolicitud });
            }

            try
            {
                var pasos = pasosTexto.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                                      .ToList();
                var idUsuario = (int)Session["idUsuario"];
                _estadoSolicitudService.CrearNuevoDespliegue(idSolicitud, idEntorno, pasos, idUsuario);
                TempData["SuccessMessage"] = "Plan de despliegue creado e iniciado correctamente.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al planificar: " + ex.Message;
            }

            return RedirectToAction("VerDespliegue", new { idSolicitud });
        }

        // Endpoint AJAX para completar un paso
        [HttpPost]
        public JsonResult CompletarPaso(int idPaso, string notas)
        {
            try
            {
                var exito = _estadoSolicitudService.CompletarPasoDespliegue(idPaso, notas, (int)Session["idUsuario"]);
                if (exito)
                {
                    return Json(new { success = true, message = "Paso completado." });
                }
                return Json(new { success = false, message = "No se pudo completar el paso." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        // POST para finalizar y cerrar toda la solicitud de cambio
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult FinalizarSolicitud(int idSolicitud, string comentariosFinales)
        {
            try
            {
                // Este método ahora solo mueve la solicitud al paso 8 (Aceptación)
                _estadoSolicitudService.FinalizarImplementacion(idSolicitud, comentariosFinales, (int)Session["idUsuario"]);
                TempData["SuccessMessage"] = "El cambio ha sido enviado para la Aceptación final del cliente.";
                // Redirigir a la nueva vista de aceptación
                return RedirectToAction("VerAceptacion", new { idSolicitud });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al enviar a aceptación: " + ex.Message;
                return RedirectToAction("VerDespliegue", new { idSolicitud });
            }
        }

        // GET
        public ActionResult VerAceptacion(int idSolicitud)
        {
            var pasoActual = _estadoSolicitudService.ObtenerPasoActualProceso(idSolicitud);
            if (pasoActual < 8)
            {
                TempData["WarningMessage"] = "La solicitud aún no ha sido enviada a la fase de Aceptación.";
                return RedirectToAction("VerSolicitud", new { idSolicitud });
            }

            var viewModel = _procesoCambioService.GetAceptacionViewModel(idSolicitud);
            if (viewModel == null) return HttpNotFound();

            ViewBag.EstadoRealDelProceso = pasoActual;
            ViewBag.IdSolicitudActual = idSolicitud;
            return View(viewModel);
        }

        // POST para aceptar el cambio
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AceptarCambioFinal(int idSolicitud, string comentariosAceptacion)
        {
            try
            {
                _estadoSolicitudService.AceptarCambio(idSolicitud, comentariosAceptacion, (int)Session["idUsuario"]);
                TempData["SuccessMessage"] = "¡El cambio ha sido aceptado y la solicitud se ha cerrado exitosamente!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al aceptar el cambio: " + ex.Message;
            }
            return RedirectToAction("VerAceptacion", new { idSolicitud });
        }

        // POST para rechazar el cambio
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RechazarCambioFinal(int idSolicitud, string comentariosRechazo)
        {
            try
            {
                _estadoSolicitudService.RechazarCambio(idSolicitud, comentariosRechazo, (int)Session["idUsuario"]);
                TempData["WarningMessage"] = "El cambio ha sido rechazado. La solicitud se marcó como cancelada.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al rechazar el cambio: " + ex.Message;
            }
            return RedirectToAction("VerAceptacion", new { idSolicitud });
        }
        [HttpPost]
        public JsonResult ActualizarEstadoTareaQA(int idTarea, string nuevoEstado)
        {
            try
            {
                // Añadimos estados válidos para las pruebas de QA a la tabla Tareas
                _estadoSolicitudService.ActualizarEstadoTareaQA(idTarea, nuevoEstado, (int)Session["idUsuario"]);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpPost]
        public JsonResult ActualizarEstadoPrueba(int idTarea, string nuevoEstado)
        {
            try
            {
                var estadosValidos = new[] { "Finalizado", "En Proceso" };
                if (!estadosValidos.Contains(nuevoEstado))
                {
                    return Json(new { success = false, message = "Estado no válido para esta acción." });
                }

                // Accede al contexto a través del campo de la clase que ya tienes
                var tarea = _context.tbTareas.Find(idTarea);
                if (tarea == null)
                {
                    return Json(new { success = false, message = "Tarea no encontrada." });
                }

                tarea.estado = nuevoEstado;
                _context.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}

