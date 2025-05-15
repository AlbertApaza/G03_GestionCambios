using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using G03_ProyectoGestion.Models;

namespace G03_ProyectoGestion.Services
{
    public class RupService
    {
        public List<object> Listar(int idUsuario)
        {
            using(var _dbContext = new g03_databaseEntities())
            {
                var projects = _dbContext.tbProyectoUsuarios
                .Where(p => p.tbProyectos.idMetodologia == 2 && p.idUsuario == idUsuario)
                .Select(p => new
                {
                    id = p.tbProyectos.idProyecto,
                    name = p.tbProyectos.nombreProyecto,
                    scope = p.tbProyectos.descripcionProyecto,
                    current_phase = p.tbProyectos.idFase
                })
                .ToList<object>();
                return projects;
            }
        }
    }
}