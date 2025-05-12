using G03_ProyectoGestion.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace G03_ProyectoGestion.Controllers
{
    public class ProyectoController : Controller
    {
        private g03_databaseEntities _dbContext = new g03_databaseEntities();

        public ActionResult Crear()
        {
            ViewBag.Usuarios = _dbContext.tbUsuarios.Where(u => u.estadoUsuario == "activo").ToList();
            ViewBag.Metodologias = _dbContext.tbMetodologias.ToList();
            ViewBag.Roles = _dbContext.tbRoles.ToList();
            return View();
        }

        [HttpPost]
        public ActionResult Crear(string nombreProyecto, string descripcionProyecto, DateTime fechaInicio, DateTime fechaFin, int idMetodologia, List<int> miembros, List<int> roles)
        {
            int idUsuarioCreador = Convert.ToInt32(Session["idUsuario"]);

            var proyecto = new tbProyectos
            {
                nombreProyecto = nombreProyecto,
                descripcionProyecto = descripcionProyecto,
                fechaInicio = fechaInicio,
                fechaFin = fechaFin,
                idUsuario = idUsuarioCreador,
                idMetodologia = idMetodologia
            };

            _dbContext.tbProyectos.Add(proyecto);
            _dbContext.SaveChanges();

            // Obtener el ID del proyecto recién creado
            int idProyectoCreado = proyecto.idProyecto;

            // Agregar miembros al proyecto
            if (miembros != null && roles != null && miembros.Count == roles.Count)
            {
                for (int i = 0; i < miembros.Count; i++)
                {
                    var miembroProyecto = new tbProyectoUsuarios
                    {
                        idProyecto = idProyectoCreado,
                        idUsuario = miembros[i],
                        idRol = roles[i]
                    };
                    _dbContext.tbProyectoUsuarios.Add(miembroProyecto);
                }
                _dbContext.SaveChanges();
            }

            return RedirectToAction("MisProyectos");
        }

        public ActionResult MisProyectos()
        {
            int idUsuario;
            if (Session["idUsuario"] == null || !int.TryParse(Session["idUsuario"].ToString(), out idUsuario))
            {
                // Redirigir a login o mostrar error si el idUsuario no está en sesión o no es válido
                return RedirectToAction("Login", "Account"); // Asumiendo que tienes una acción de Login
            }

            var proyectosViewModel = (from p in _dbContext.tbProyectos
                                      join pu in _dbContext.tbProyectoUsuarios on p.idProyecto equals pu.idProyecto
                                      where pu.idUsuario == idUsuario
                                      select new ProyectoCardViewModel // Proyecta al ViewModel
                                      {
                                          IdProyecto = p.idProyecto,
                                          NombreProyecto = p.nombreProyecto,
                                          DescripcionProyecto = p.descripcionProyecto,
                                          FechaInicio = p.fechaInicio,
                                          FechaFin = p.fechaFin,
                                          Metodologia = p.tbMetodologias != null ? p.tbMetodologias.nombreMetodologia : "No asignada"
                                      }).ToList();

            return View(proyectosViewModel); // Pasa la lista de ViewModels a la vista
        }

        public ActionResult Detalles(int id)
        {
            // Lógica para mostrar detalles del proyecto
            // Idealmente, también usarías un ViewModel aquí.
            var proyecto = _dbContext.tbProyectos.Find(id);
            if (proyecto == null)
            {
                return HttpNotFound();
            }
            // Aquí podrías mapear 'proyecto' a un 'ProyectoDetalleViewModel'
            return View(proyecto); // O un ViewModel específico para detalles
        }
    }
}
