using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace G03_ProyectoGestion.Models
{
    public class ViewModelProyectoElementoClave
    {
        public string NombreElemento { get; set; }
        public string DescripcionElemento { get; set; } 
        public DateTime? FechaInicio { get; set; } 
        public DateTime? FechaFin { get; set; }   
        public int? FaseSprintIteracion { get; set; }
    }
}