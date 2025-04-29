using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AppGCS.Models
{
    public enum Rol
    {
        LiderDeProyecto,
        ProjectOwner,
        Desarrollador
    }

    public class Integrante
    {
        public string Nombre { get; set; }
        public Rol Rol { get; set; }
    }

    public class Proyecto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Metodologia { get; set; }
        public DateTime FechaCreacion { get; set; }
        public List<Integrante> Integrantes { get; set; } = new List<Integrante>();
    }
}