using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace G03_GestionDeCambios.Models
{
    using System.Collections.Generic;

    public class AdministradorDashboardViewModel
    {
        public List<tbUsuarios> Usuarios { get; set; }
        public List<tbProyectos> Proyectos { get; set; }
        public List<tbSolicitudesCambio> SolicitudesCambio { get; set; }
        public List<tbDocumentos> Documentos { get; set; }
        public List<tbTareas> Tareas { get; set; }
        public List<tbMetodologias> Metodologias { get; set; }
        public List<tbRoles> Roles { get; set; }
        public List<tbElementos> Elementos { get; set; }
        public List<tbCiclos> Ciclos { get; set; }

        public AdministradorDashboardViewModel()
        {
            Usuarios = new List<tbUsuarios>();
            Proyectos = new List<tbProyectos>();
            SolicitudesCambio = new List<tbSolicitudesCambio>();
            Documentos = new List<tbDocumentos>();
            Tareas = new List<tbTareas>();
            Metodologias = new List<tbMetodologias>();
            Roles = new List<tbRoles>();
            Elementos = new List<tbElementos>();
            Ciclos = new List<tbCiclos>();
        }
    }
}