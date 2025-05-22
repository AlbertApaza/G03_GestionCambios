using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace G03_ProyectoGestion.Models
{
    public class ProyectoElementoViewModel
    {
        public int IdProyectoElemento { get; set; }
        public int IdElemento { get; set; }
        public string ElementoNombre { get; set; }
        public string FechaInicio { get; set; }
        public string FechaFin { get; set; }
        public string FaseSprintIteracionMostrable { get; set; }
        public string FechaInicioEditable { get; set; }
        public string FechaFinEditable { get; set; }
    }
}