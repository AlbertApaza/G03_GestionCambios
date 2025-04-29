using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AppGCS.Models
{
    public class Equipo
    {
        public int IdEquipo { get; set; }
        public string NombreCreador {  get; set; }
        public List<string> NombresIntengrantes { get; set; }
        public Equipo()
        {
            NombresIntengrantes = new List<string>();
        }
    }
}