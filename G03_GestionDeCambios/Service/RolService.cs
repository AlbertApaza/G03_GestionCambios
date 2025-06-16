using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using G03_GestionDeCambios.Models;

namespace G03_GestionDeCambios.Service
{
    public class RolService
    {
        public int? ObtenerRolProyecto(int idUsuario, int idProyecto)
        {
            try
            {
                using(var _dbContext = new BD_GestionDeCambiosEntities())
                {
                    return _dbContext.tbProyectoUsuario
                        .Where(up => up.idUsuario == idUsuario && up.idProyecto == idProyecto)
                        .Select(up => up.idRol)
                        .FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}