using G03_ProyectoGestion.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace G03_ProyectoGestion.Services
{
    public class ProyectoService
    {
        private readonly g03_databaseEntities _dbContext;
        public ProyectoService()
        {
            _dbContext = new g03_databaseEntities();
        }


        public void CrearProyecto(tbProyectos proyecto)
        {

        }
    }
}