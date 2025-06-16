using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using G03_GestionDeCambios.Models;

namespace G03_GestionDeCambios.Service
{
    public class ProyectoService
    {
        public List<SelectListItem> ProyectosUsuarioDropDown(int idUsuario)
        {
            try
            {
                using (var _dbContext = new BD_GestionDeCambiosEntities())
                {
                    return _dbContext.tbProyectos
                        .Where(p => p.tbProyectoUsuario.Any(u => u.idUsuario == idUsuario))
                        .Select(u => new SelectListItem
                        {
                            Value = u.idProyecto.ToString(),
                            Text = u.nombre
                        }).OrderBy(u => u.Text).ToList();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener todos los proyectos del usuario: " + ex.Message);
            }
        }
        public List<tbProyectos> ListarProyectos(int idUsuario)
        {
            try
            {
                using (var _dbContext = new BD_GestionDeCambiosEntities())
                {
                    // Obtener los proyectos del usuario autenticado
                    var proyectos = _dbContext.tbProyectos
                        .Include("tbMetodologias")
                        .Where(p => p.tbProyectoUsuario.Any(u => u.idUsuario == idUsuario))
                        .ToList();
                    return proyectos;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al listar proyectos: " + ex);
            }
        }
        public int CrearProyecto(tbProyectos proyecto)
        {
            try
            {
                using (var _dbContext = new BD_GestionDeCambiosEntities())
                {
                    proyecto.estado = 1;
                    _dbContext.tbProyectos.Add(proyecto);
                    _dbContext.SaveChanges();
                    return proyecto.idProyecto;

                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al crear el proyecto: " + ex);
            }
        }

        public CrearProyectoViewModel ObtenerMetodologias()
        {
            try
            {
                using (var _dbContext = new BD_GestionDeCambiosEntities())
                {
                    return new CrearProyectoViewModel
                    {
                        Metodologias = _dbContext.tbMetodologias
                            .Select(m => new SelectListItem
                            {
                                Value = m.idMetodologia.ToString(),
                                Text = m.nombre
                            }).ToList()
                    };
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener metodologías: " + ex.Message);
            }
        }
        public List<SelectListItem> ObtenerCicloPorMetodologia(int idMetodologia)
        {
            try
            {
                using (var _dbContext = new BD_GestionDeCambiosEntities())
                {
                    return _dbContext.tbCiclos
                        .Where(c => c.idMetodologia == idMetodologia)
                        .Select(c => new SelectListItem
                        {
                            Value = c.codCiclo,
                            Text = c.nombre
                        }).ToList();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener ciclos por metodología: " + ex.Message);
            }
        }

        public tbProyectos ObtenerProyectoPorId(int idProyecto)
        {
            try
            {
                using (var _dbContext = new BD_GestionDeCambiosEntities())
                {
                    return _dbContext.tbProyectos
                                     .Include("tbMetodologias") 
                                     .Include("tbCiclos")       
                                     .FirstOrDefault(p => p.idProyecto == idProyecto);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener el proyecto: " + ex.Message);
            }
        }

        public List<tbCiclos> ObtenerCiclosPorMetodologiaConOrden(int idMetodologia)
        {
            using (var _dbContext = new BD_GestionDeCambiosEntities())
            {
                return _dbContext.tbCiclos
                                 .Where(c => c.idMetodologia == idMetodologia)
                                 .OrderBy(c => c.orden) // Asegurar el orden
                                 .ToList();
            }
        }

        public tbProyectos ObtenerProyectoConMetodologia(int idProyecto)
        {
            using (var _dbContext = new BD_GestionDeCambiosEntities())
            {
                // Incluir la metodología para no hacer otra consulta luego
                return _dbContext.tbProyectos.Include("tbMetodologias")
                                 .FirstOrDefault(p => p.idProyecto == idProyecto);
            }
        }

        public List<tbProyectoCiclo> ObtenerProyectoCiclos(int idProyecto)
        {
            using (var _dbContext = new BD_GestionDeCambiosEntities())
            {
                return _dbContext.tbProyectoCiclo.Where(pc => pc.idProyecto == idProyecto).ToList();
            }
        }

        public void GuardarProyectoCiclos(int idProyecto, List<CicloGestionViewModel> ciclosVM)
        {
            using (var _dbContext = new BD_GestionDeCambiosEntities())
            {
                foreach (var cicloVM in ciclosVM)
                {
                    if (cicloVM.FechaInicioCiclo.HasValue && cicloVM.FechaFinCiclo.HasValue) // Solo guardar si hay fechas
                    {
                        var proyectoCicloDB = _dbContext.tbProyectoCiclo
                                                        .FirstOrDefault(pc => pc.idProyecto == idProyecto && pc.codCiclo == cicloVM.CodCiclo);

                        if (proyectoCicloDB == null) // No existe, crear
                        {
                            proyectoCicloDB = new tbProyectoCiclo
                            {
                                idProyecto = idProyecto,
                                codCiclo = cicloVM.CodCiclo,
                                inicioCiclo = cicloVM.FechaInicioCiclo,
                                finCiclo = cicloVM.FechaFinCiclo
                            };
                            _dbContext.tbProyectoCiclo.Add(proyectoCicloDB);
                        }
                        else // Existe, actualizar
                        {
                            proyectoCicloDB.inicioCiclo = cicloVM.FechaInicioCiclo;
                            proyectoCicloDB.finCiclo = cicloVM.FechaFinCiclo;
                            _dbContext.Entry(proyectoCicloDB).State = System.Data.Entity.EntityState.Modified;
                        }
                    }
                    else 
                    {
                        var proyectoCicloDB = _dbContext.tbProyectoCiclo
                                                       .FirstOrDefault(pc => pc.idProyecto == idProyecto && pc.codCiclo == cicloVM.CodCiclo);
                        if (proyectoCicloDB != null)
                        {
                            _dbContext.tbProyectoCiclo.Remove(proyectoCicloDB);
                        }
                    }
                }
                _dbContext.SaveChanges();
            }
        }
        public int ObtenerRol(int idUsuario, int idProyecto)
        {
            try
            {
                using (var _dbContext = new BD_GestionDeCambiosEntities())
                {
                    return _dbContext.tbProyectoUsuario
                        .Where(py => py.idProyecto == idProyecto && py.idUsuario == idUsuario)
                        .Select(py => py.idRol)
                        .FirstOrDefault()
                        .GetValueOrDefault(); 
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener el rol del usuario en el proyecto.", ex);
            }
        }

    }
}
