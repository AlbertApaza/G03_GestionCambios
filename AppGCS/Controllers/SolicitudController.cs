using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace AppGCS.Controllers
{
    public class SolicitudController : Controller
    {
        // GET: Solicitud
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult FormularioSolicitud()
        {
            return View();
        }
        public ActionResult InformeSolicitudPdf()
        {
            return View();
        }
    }
}