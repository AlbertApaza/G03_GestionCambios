using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AppGCS.Models
{
    public class clsEquipo
    {
        public int IdEquipo { get; set; }
        public string NombreCreador {  get; set; }
        public List<clsIntegrante> Intengrantes { get; set; }
        public clsEquipo()
        {
            Intengrantes = new List<clsIntegrante>();
        }
    }
}