using G03_ProyectoGestion.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity; // Necesario para .Include() si se usa

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
        [ValidateAntiForgeryToken] // Buena práctica añadir esto
        public ActionResult Crear(string nombreProyecto, string descripcionProyecto, DateTime fechaInicio, DateTime fechaFin, int idMetodologia, List<int> miembros, List<int> roles, List<int> selectedElementos) // Nuevo parámetro
        {
            if (ModelState.IsValid) // Buena práctica validar el modelo
            {
                int idUsuarioCreador = Convert.ToInt32(Session["idUsuario"]);

                var proyecto = new tbProyectos
                {
                    nombreProyecto = nombreProyecto,
                    descripcionProyecto = descripcionProyecto,
                    fechaInicio = fechaInicio,
                    fechaFin = fechaFin,
                    idUsuario = idUsuarioCreador,
                    idMetodologia = idMetodologia,
                    estado = 1, // Estado activo
                    idFase = idMetodologia == 2 ? 1 : (int?)null // Asignar fase inicial si es Rup (Asumo que 2 es RUP, ajusta si es necesario)
                };

                _dbContext.tbProyectos.Add(proyecto);
                _dbContext.SaveChanges();

                int idProyectoCreado = proyecto.idProyecto;

                if (miembros != null && roles != null && miembros.Count == roles.Count)
                {
                    for (int i = 0; i < miembros.Count; i++)
                    {
                        // Validar que el usuario y rol existan y no se asignen duplicados si es necesario
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

                // Guardar elementos de configuración seleccionados
                if (selectedElementos != null && selectedElementos.Any())
                {
                    foreach (var idElemento in selectedElementos)
                    {
                        var proyectoElemento = new tbProyectoElemento
                        {
                            idProyecto = idProyectoCreado,
                            idElemento = idElemento,
                            fechaInicio = fechaInicio, // Usamos las fechas del proyecto
                            fechaFin = fechaFin        // O podrías tener campos de fecha específicos para cada elemento
                        };
                        _dbContext.tbProyectoElemento.Add(proyectoElemento);
                    }
                    _dbContext.SaveChanges();
                }

                return RedirectToAction("Index");
            }

            // Si llegamos aquí, algo falló, volvemos a mostrar el formulario
            ViewBag.Usuarios = _dbContext.tbUsuarios.Where(u => u.estadoUsuario == "activo").ToList();
            ViewBag.Metodologias = _dbContext.tbMetodologias.ToList();
            ViewBag.Roles = _dbContext.tbRoles.ToList();
            // Podrías querer repoblar los campos con los valores que el usuario ya ingresó
            // usando los parámetros del método (nombreProyecto, descripcionProyecto, etc.)
            return View();
        }

        public ActionResult Index()
        {
            int idUsuario = Convert.ToInt32(Session["idUsuario"]);

            var proyectosViewModel = (from p in _dbContext.tbProyectos
                                      join pu in _dbContext.tbProyectoUsuarios on p.idProyecto equals pu.idProyecto
                                      where pu.idUsuario == idUsuario && p.estado == 1
                                      select new ProyectoCardViewModel
                                      {
                                          IdProyecto = p.idProyecto,
                                          NombreProyecto = p.nombreProyecto,
                                          DescripcionProyecto = p.descripcionProyecto,
                                          FechaInicio = p.fechaInicio,
                                          FechaFin = p.fechaFin,
                                          Metodologia = p.tbMetodologias != null ? p.tbMetodologias.nombreMetodologia : "No asignada"
                                      }).ToList();

            return View(proyectosViewModel);
        }

        public ActionResult Detalles(int id)
        {
            var proyecto = _dbContext.tbProyectos
                                     // Si necesitas cargar datos relacionados ansiosamente:
                                     // .Include(p => p.tbMetodologias)
                                     // .Include(p => p.tbProyectoUsuarios.Select(pu => pu.tbUsuarios))
                                     // .Include(p => p.tbProyectoUsuarios.Select(pu => pu.tbRoles))
                                     // .Include(p => p.tbProyectoElemento.Select(pe => pe.tbElementos))
                                     .FirstOrDefault(p => p.idProyecto == id);
            if (proyecto == null)
            {
                return HttpNotFound();
            }
            return View(proyecto);
        }

        // NUEVA ACCIÓN AJAX
        [HttpGet]
        public JsonResult ObtenerElementosPorMetodologia(int idMetodologia)
        {
            try
            {
                var metodologia = _dbContext.tbMetodologias.Find(idMetodologia);
                if (metodologia == null)
                {
                    return Json(new { success = false, message = "Metodología no encontrada" }, JsonRequestBehavior.AllowGet);
                }

                // Asumimos que el campo 'tipo' en tbElementos coincide con 'nombreMetodologia'
                // o algún identificador único de la metodología. Ajusta esto si es diferente.
                string tipoMetodologiaParaElementos = metodologia.nombreMetodologia;

                var elementos = _dbContext.tbElementos
                                          .Where(e => e.tipo.Equals(tipoMetodologiaParaElementos, StringComparison.OrdinalIgnoreCase))
                                          .Select(e => new { e.idElemento, e.nombre, e.descripcion })
                                          .ToList();

                return Json(new { success = true, data = elementos }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                // Loguear el error ex.Message
                return Json(new { success = false, message = "Error al obtener elementos: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _dbContext.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}