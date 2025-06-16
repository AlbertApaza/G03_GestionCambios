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
        ProyectoService _proyectoService = new ProyectoService();
        SolicitudService _solicitudService= new SolicitudService();
        ProcesoCambioService _procesoCambioService= new ProcesoCambioService();
        private int RetornarPasoActualdeSolicitud(int idSolicitud) { return 3; /*Por ejemplo*/ }


        // id de Solicitd
        public ActionResult VerSolicitud(int idSolicitud)
        {
            var viewModel = _procesoCambioService.GetSolicitudDetalle(idSolicitud);
            if (viewModel == null)
            {
                return HttpNotFound(); // Si la solicitud no existe
            }

            ViewBag.IdSolicitudActual = idSolicitud;
            ViewBag.EstadoRealDelProceso = _solicitudService.ObtenerPasoActualProceso(idSolicitud);

            return View(viewModel);
        }
        // id de solicitd
        public ActionResult VerAnalisis(int idSolicitud)
        {
            var idPasoReal = _solicitudService.ObtenerPasoActualProceso(idSolicitud);
            ViewBag.IdSolicitudActual = idSolicitud;
            ViewBag.EstadoRealDelProceso = idPasoReal;
            return View();
        }

        public ActionResult VerAprobacion(int idSolicitud)
        {
            var idPasoReal = _solicitudService.ObtenerPasoActualProceso(idSolicitud);
            ViewBag.IdSolicitudActual = idSolicitud;
            ViewBag.EstadoRealDelProceso = idPasoReal;
            return View();
        }

    }
}

