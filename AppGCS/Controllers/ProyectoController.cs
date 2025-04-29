using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using AppGCS.Models;

namespace AppGCS.Controllers
{
    public class ProyectoController : Controller
    {
        // GET: Equipo

        public ActionResult FormCrearProyecto()
        {
            //formulario para crear nuevo proyecto y luego enviarselos a la vista addproyecto
            return View();
        }

        public ActionResult AddProyecto(Proyecto objProyecto)
        {
            //crear el proyecto en sesion, luego pasaria a un formulario de crear equipo para ese proyecto, ahí agregaría a los nuevos intengrantes

            var proyectos = Session["Proyectos"] as List<Proyecto> ?? new List<Proyecto>();
            proyectos.Add(objProyecto);
            Session["Proyectos"] = proyectos;

            return View(objProyecto);
        }

        public ActionResult FormCrearEquipo()
        {
            //aqui es una vista para agregar a los integrantes del equipo, escribiendo sus nombres
            return View();
        }

        public ActionResult AddEquipo(Equipo objEquipo)
        {
            var equipos = Session["Equipos"] as List<Equipo> ?? new List<Equipo>();
            equipos.Add(objEquipo);
            Session["Equipos"] = equipos;

            return View(objEquipo);
        }
    }
}