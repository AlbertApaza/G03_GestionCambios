using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace AppGCS.Controllers
{
    public class CambiosController : Controller
    {
        // Vista principal (tareas asignadas)
        public ActionResult Index()
        {
            ViewBag.TareaCompletada = TempData["completado"];
            return View();
        }

        // GET: Formulario de implementación
        [HttpGet]
        public ActionResult RegistrarImplementacion(string codigo)
        {
            ViewBag.CodigoSolicitud = codigo;
            return View();
        }

        // POST: Registro simulado de implementación
        [HttpPost]
        public ActionResult RegistrarImplementacion(FormCollection form)
        {
            //ViewBag.Mensaje = "✔ Implementación registrada correctamente (simulación)";
            //ViewBag.Codigo = form["codigoSolicitud"];
            //ViewBag.PR = form["pullRequestUrl"];
            //ViewBag.Notas = form["notasTecnicas"];
            //ViewBag.Entorno = form["entornoPruebas"];
            //ViewBag.Pruebas = form["pruebasExitosas"] == "on" ? "Sí" : "No";
            //ViewBag.Archivo = Request.Files["archivoCodigo"]?.FileName ?? "No se subió ningún archivo";

            // Marcar tarea como completada para vista Index
            ViewBag.MostrarMensaje = true;
            TempData["completado"] = true;

            return View();
        }
    }
}