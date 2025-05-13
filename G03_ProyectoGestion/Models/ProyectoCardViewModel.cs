using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace G03_ProyectoGestion.Models
{
    public class ProyectoCardViewModel
    {
        public int IdProyecto { get; set; }
        public string NombreProyecto { get; set; }
        public string DescripcionProyecto { get; set; }
        public DateTime? FechaInicio { get; set; } // Usa Nullable DateTime si pueden ser null en BD
        public DateTime? FechaFin { get; set; }    // Usa Nullable DateTime si pueden ser null en BD
        public string Metodologia { get; set; }
        public int IdMetodologia { get; set; } // 

    }
}