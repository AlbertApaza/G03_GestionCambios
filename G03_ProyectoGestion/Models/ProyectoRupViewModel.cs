using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace G03_ProyectoGestion.Models
{
    public class ProjectCreateViewModel
    {
        [Required(ErrorMessage = "El nombre del proyecto es obligatorio.")]
        [StringLength(100)]
        public string Name { get; set; }

        [Required(ErrorMessage = "El alcance es obligatorio.")]
        public string Scope { get; set; }

        [Required(ErrorMessage = "La fase inicial es obligatoria.")]
        public string Current_Phase { get; set; } // This will be 'inception', 'elaboration', etc.
    }

    public class IterationCreateViewModel
    {
        [Required]
        public int Project_Id { get; set; }
        [Required]
        public string Phase_Id { get; set; } // e.g. "inception"
        [Required]
        public string Name { get; set; }
        [Required]
        public string Objective { get; set; }
        public string Start_Date { get; set; } // Expect "yyyy-MM-dd" string
        public string End_Date { get; set; }   // Expect "yyyy-MM-dd" string
    }
}