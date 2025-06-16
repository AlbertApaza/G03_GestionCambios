using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using G03_GestionDeCambios.Models;

namespace G03_GestionDeCambios.Service
{
    public class ProyectoUsuarioService
    {
        public List<UsuarioAsignadoViewModel> ObtenerUsuariosPorProyecto(int idProyecto)
        {
            try
            {
                using (var _dbContext = new BD_GestionDeCambiosEntities())
                {
                    return _dbContext.tbProyectoUsuario
                        .Where(pu => pu.idProyecto == idProyecto)
                        .Select(pu => new UsuarioAsignadoViewModel
                        {
                            IdUsuario = pu.tbUsuarios.idUsuario,
                            NombreCompletoUsuario = pu.tbUsuarios.nombre + " " + pu.tbUsuarios.apellido,
                            EmailUsuario = pu.tbUsuarios.email,
                            IdRol = pu.idRol, // Nuevo
                            NombreRol = pu.tbRoles != null ? pu.tbRoles.nombre : "No asignado" // Nuevo, asumiendo navegación tbRoles
                        }).ToList();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener usuarios del proyecto: " + ex.Message);
            }
        }

        public List<SelectListItem> ObtenerTodosLosUsuariosParaDropdown()
        {
            try
            {
                using (var _dbContext = new BD_GestionDeCambiosEntities())
                {
                    return _dbContext.tbUsuarios
                        .Where(u => u.estado == 1) 
                        .Select(u => new SelectListItem
                        {
                            Value = u.idUsuario.ToString(),
                            Text = u.nombre + " " + u.apellido + " (" + u.usuario + ")"
                        }).OrderBy(u => u.Text).ToList();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener todos los usuarios: " + ex.Message);
            }
        }

        public void AgregarUsuarioAProyecto(int idProyecto, int idUsuario, int idRol) 
        {
            try
            {
                using (var _dbContext = new BD_GestionDeCambiosEntities())
                {
                    var nuevaAsignacion = new tbProyectoUsuario
                    {
                        idProyecto = idProyecto,
                        idUsuario = idUsuario,
                        idRol = idRol // Nuevo
                    };
                    _dbContext.tbProyectoUsuario.Add(nuevaAsignacion);
                    _dbContext.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al agregar usuario al proyecto: " + ex.Message);
            }
        }
        public List<SelectListItem> ObtenerRolesPorMetodologia(int idMetodologia)
        {
            try
            {
                using (var _dbContext = new BD_GestionDeCambiosEntities())
                {
                    return _dbContext.tbRoles
                        .Where(r => r.idMetodologia == idMetodologia)
                        .Select(r => new SelectListItem
                        {
                            Value = r.idRol.ToString(),
                            Text = r.nombre
                        }).OrderBy(r => r.Text).ToList();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener roles por metodología: " + ex.Message);
            }
        }
    }
}