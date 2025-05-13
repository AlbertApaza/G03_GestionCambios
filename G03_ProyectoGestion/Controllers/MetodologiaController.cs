using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using G03_ProyectoGestion.Models;

namespace G03_ProyectoGestion.Controllers
{
    public class MetodologiaController : Controller
    {
        private g03_databaseEntities _dbContext = new g03_databaseEntities();

        public ActionResult Detalles(int id)
        {
            var metodologia = _dbContext.tbMetodologias.Find(id);
            if (metodologia == null)
            {
                return HttpNotFound();
            }

            return View(metodologia); // Asegúrate de tener una vista llamada Detalles.cshtml
        }
    }

}