using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using G03_GestionDeCambios.Models;
using System.Data.Entity;


namespace G03_GestionDeCambios.Service
{
    public class ProyectoElementoService
    {
        public List<tbElementos> ObtenerTodosElementos()
        {
            try
            {
                using (var _dbContext = new BD_GestionDeCambiosEntities())
                {
                    return _dbContext.tbElementos.OrderBy(e => e.nombre).ToList();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener todos los elementos: " + ex.Message);
            }
        }


        public List<tbProyectoElemento> ObtenerElementosPorProyecto(int idProyecto)
        {
            try
            {
                using (var _dbContext = new BD_GestionDeCambiosEntities())
                {
                    return _dbContext.tbProyectoElemento
                                     .Where(pe => pe.idProyecto == idProyecto)
                                     .ToList();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener elementos asignados al proyecto: " + ex.Message);
            }
        }
        public List<tbProyectoElemento> ObtenerElementosPorProyectoConDetalles(int idProyecto)
        {
            using (var _dbContext = new BD_GestionDeCambiosEntities())
            {
                return _dbContext.tbProyectoElemento
                                 .Include(pe => pe.tbElementos)
                                 .Include(pe => pe.tbRoles) // NUEVO: Incluir Roles
                                 .Where(pe => pe.idProyecto == idProyecto)
                                 .ToList();
            }
        }

        public void GestionarElementosDeProyecto(int idProyecto, List<CicloGestionViewModel> ciclosConElementosVM)
        {
            using (var _dbContext = new BD_GestionDeCambiosEntities())
            {
                foreach (var cicloVM in ciclosConElementosVM)
                {
                    foreach (var elVM in cicloVM.ElementosAsignados.Where(e => e.MarcadoParaEliminar && e.IdProyectoElemento > 0))
                    {
                        var elDB = _dbContext.tbProyectoElemento.Find(elVM.IdProyectoElemento);
                        if (elDB != null)
                        {
                            _dbContext.tbProyectoElemento.Remove(elDB);
                        }
                    }
                }
                _dbContext.SaveChanges();


                foreach (var cicloVM in ciclosConElementosVM)
                {
                    if (cicloVM.IdElementoAAgregar.HasValue && cicloVM.IdElementoAAgregar > 0)
                    {
                        if (string.IsNullOrWhiteSpace(cicloVM.CodCiclo))
                        {
                            throw new Exception($"El ciclo para el nuevo elemento '{cicloVM.IdElementoAAgregar}' no puede estar vacío.");
                        }

                        int proximoIdProyectoElemento = 0;
                        if (!_dbContext.tbProyectoElemento.Any())
                        {
                            proximoIdProyectoElemento = 1;
                        }
                        else
                        {
                            proximoIdProyectoElemento = _dbContext.tbProyectoElemento.Max(p => p.idProyectoElemento) + 1;
                        }


                        var nuevoEl = new tbProyectoElemento
                        {
                            idProyectoElemento = proximoIdProyectoElemento, // Asegúrate de manejar esto correctamente
                            idProyecto = idProyecto,
                            idElemento = cicloVM.IdElementoAAgregar.Value,
                            codCiclo = cicloVM.CodCiclo,
                            fechaInicio = cicloVM.FechaInicioNuevoElemento,
                            fechaFin = cicloVM.FechaFinNuevoElemento,
                            idRol = cicloVM.IdRolNuevoElemento, // Se asigna el rol al crear
                            estado = "Pendiente"
                        };
                        _dbContext.tbProyectoElemento.Add(nuevoEl);
                    }

                    foreach (var elVM in cicloVM.ElementosAsignados.Where(e => !e.MarcadoParaEliminar && e.IdProyectoElemento > 0))
                    {
                        var elDB = _dbContext.tbProyectoElemento.Find(elVM.IdProyectoElemento);
                        if (elDB != null)
                        {
                            elDB.fechaInicio = elVM.FechaInicioElemento;
                            elDB.fechaFin = elVM.FechaFinElemento;
                            _dbContext.Entry(elDB).State = EntityState.Modified;
                        }
                    }
                }
                _dbContext.SaveChanges();
            }
        }

    }
}

