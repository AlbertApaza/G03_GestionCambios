using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AppGCS.Models
{
    public class ClsUsuario
    {
        public int IdUsuario { get; set; }
        public string Usuario { get; set; }
        public string Contrasena { get; set; }
        public int IdRol { get; set; }
        public bool Estado { get; set; }
    }
}