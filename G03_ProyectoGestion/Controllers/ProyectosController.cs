using G03_ProyectoGestion.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using G03_ProyectoGestion.Services;

namespace G03_ProyectoGestion.Controllers
{
    public class ProyectosController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }
    }
}
