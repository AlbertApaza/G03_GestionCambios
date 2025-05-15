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

        // GET: XP/ObtenerIteraciones?idProyecto=#
        public ActionResult ObtenerIteraciones(int idProyecto)
        {
            var iteraciones = _dbContext.tbXpIteraciones
                .Where(i => i.idProyecto == idProyecto)
                .ToList() // ← Primero convierte los resultados a memoria
                .Select(i => new
                {
                    i.idIteracion,
                    i.nombre,
                    fechaInicio = i.fechaInicio?.ToString("yyyy-MM-dd") ?? "",
                    fechaFin = i.fechaFin?.ToString("yyyy-MM-dd") ?? "",
                    i.estado
                }).ToList();

            return Json(iteraciones, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult RegistrarIteracion(tbXpIteraciones iteracion)
        {
            if (ModelState.IsValid)
            {
                _dbContext.tbXpIteraciones.Add(iteracion);
                _dbContext.SaveChanges();
                return Json(new { success = true });
            }

            return Json(new { success = false, error = "Datos inválidos" });
        }
        [HttpPost]
        public ActionResult RegistrarHistoria(FormCollection form)
        {
            var historia = new tbXpHistoriasUsuario
            {
                idProyecto = Convert.ToInt32(form["idProyecto"]),
                titulo = form["titulo"],
                historia = form["historia"],
                criteriosAceptacion = form["criteriosAceptacion"]
            };

            _dbContext.tbXpHistoriasUsuario.Add(historia);
            _dbContext.SaveChanges();
            return Json(new { success = true });
        }

        [HttpPost]
        public ActionResult ActualizarHistoria(FormCollection form)
        {
            int id = Convert.ToInt32(form["idHistoria"]);
            var historia = _dbContext.tbXpHistoriasUsuario.Find(id);

            if (historia != null)
            {
                historia.titulo = form["titulo"];
                historia.historia = form["historia"];
                historia.criteriosAceptacion = form["criteriosAceptacion"];
                _dbContext.SaveChanges();
                return Json(new { success = true });
            }

            return Json(new { success = false, error = "Historia no encontrada" });
        }

        [HttpPost]
        public ActionResult EliminarHistoria(int id)
        {
            var historia = _dbContext.tbXpHistoriasUsuario.Find(id);
            if (historia != null)
            {
                _dbContext.tbXpHistoriasUsuario.Remove(historia);
                _dbContext.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        public ActionResult ListarHistorias(int idProyecto)
        {
            var historias = _dbContext.tbXpHistoriasUsuario
                .Where(h => h.idProyecto == idProyecto)
                .Select(h => new
                {
                    h.idHistoria,
                    h.titulo,
                    h.historia,
                    h.criteriosAceptacion
                }).ToList();

            return Json(historias, JsonRequestBehavior.AllowGet);
        }
    }

}