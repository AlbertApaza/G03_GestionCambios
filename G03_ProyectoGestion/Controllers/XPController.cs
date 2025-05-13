using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using G03_ProyectoGestion.Models;

namespace G03_ProyectoGestion.Controllers
{
    public class XPController : Controller
    {
        private g03_databaseEntities _dbContext = new g03_databaseEntities();

        public ActionResult Index(int id)
        {
            var proyecto = _dbContext.tbProyectos.Find(id);
            if (proyecto == null)
                return HttpNotFound();

            return View(proyecto);
        }
    }
}