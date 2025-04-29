using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AppGCS.Models
{
    public class clsProyecto
    {
        public int IdProyecto { get; set; }
        public string NombreProyecto { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string Metodologia { get; set; }

        public int IdEquipo { get; set; }

        public clsProyecto()
        {
            this.FechaCreacion = DateTime.Now;
        }
    }
}