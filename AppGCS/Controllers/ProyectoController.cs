using AppGCS.Models;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Linq;
using System;

public class ProyectoController : Controller
{
    private List<Proyecto> ObtenerProyectos()
    {
        if (Session["Proyectos"] == null)
        {
            var proyectos = new List<Proyecto>
            {
                new Proyecto
                {
                    Id = 1,
                    Nombre = "Proyecto Alpha",
                    Metodologia = "Scrum",
                    FechaCreacion = DateTime.Now.AddDays(-5),
                    Integrantes = new List<Integrante>
                    {
                        new Integrante { Nombre = "Ana", Rol = Rol.LiderDeProyecto },
                        new Integrante { Nombre = "Luis", Rol = Rol.Desarrollador }
                    }
                },
                new Proyecto
                {
                    Id = 2,
                    Nombre = "Proyecto Beta",
                    Metodologia = "RUP",
                    FechaCreacion = DateTime.Now.AddDays(-2),
                    Integrantes = new List<Integrante>
                    {
                        new Integrante { Nombre = "Carlos", Rol = Rol.ProjectOwner },
                        new Integrante { Nombre = "Sofía", Rol = Rol.Desarrollador }
                    }
                }
            };
            Session["Proyectos"] = proyectos;
        }
        return (List<Proyecto>)Session["Proyectos"];
    }

    public ActionResult Index()
    {
        var proyectos = ObtenerProyectos().OrderByDescending(p => p.FechaCreacion).ToList();
        return View(proyectos);
    }

    public ActionResult Crear()
    {
        return View();
    }

    [HttpPost]
    public ActionResult Crear(string nombre, string metodologia)
    {
        var proyectos = ObtenerProyectos();
        int nuevoId = proyectos.Max(p => p.Id) + 1;

        var nuevoProyecto = new Proyecto
        {
            Id = nuevoId,
            Nombre = nombre,
            Metodologia = metodologia,
            FechaCreacion = DateTime.Now
        };

        proyectos.Add(nuevoProyecto);
        Session["Proyectos"] = proyectos;

        return RedirectToAction("AgregarIntegrantes", new { id = nuevoId });
    }

    public ActionResult AgregarIntegrantes(int id)
    {
        var proyecto = ObtenerProyectos().FirstOrDefault(p => p.Id == id);
        if (proyecto == null) return HttpNotFound();
        return View(proyecto);
    }

    [HttpPost]
    public ActionResult AgregarIntegrantes(int id, string nombreIntegrante, Rol rol)
    {
        var proyectos = ObtenerProyectos();
        var proyecto = proyectos.FirstOrDefault(p => p.Id == id);
        if (proyecto != null)
        {
            proyecto.Integrantes.Add(new Integrante { Nombre = nombreIntegrante, Rol = rol });
        }
        Session["Proyectos"] = proyectos;
        return RedirectToAction("AgregarIntegrantes", new { id });
    }

    public ActionResult Detalle(int id)
    {
        var proyecto = ObtenerProyectos().FirstOrDefault(p => p.Id == id);
        if (proyecto == null) return HttpNotFound();
        return View(proyecto);
    }

    [HttpPost]
    public ActionResult EditarRol(int id, string nombre, Rol nuevoRol)
    {
        var proyectos = ObtenerProyectos();
        var proyecto = proyectos.FirstOrDefault(p => p.Id == id);
        var integrante = proyecto?.Integrantes.FirstOrDefault(i => i.Nombre == nombre);
        if (integrante != null)
        {
            integrante.Rol = nuevoRol;
        }
        Session["Proyectos"] = proyectos;
        return RedirectToAction("Detalle", new { id });
    }

    [HttpPost]
    public ActionResult AgregarNuevoIntegrante(int id, string nombre, Rol rol)
    {
        var proyectos = ObtenerProyectos();
        var proyecto = proyectos.FirstOrDefault(p => p.Id == id);
        if (proyecto != null)
        {
            proyecto.Integrantes.Add(new Integrante { Nombre = nombre, Rol = rol });
        }
        Session["Proyectos"] = proyectos;
        return RedirectToAction("Detalle", new { id });
    }
}
