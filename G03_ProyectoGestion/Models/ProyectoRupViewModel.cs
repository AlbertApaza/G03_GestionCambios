using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace G03_ProyectoGestion.Models
{
    public class ProjectViewModel
    {
        public int id { get; set; }
        public string name { get; set; }
        public string scope { get; set; }
        public int current_phase { get; set; } // O Nullable<int> si puede ser null
    }
    public class ProjectCreatePostModel
    {
        public string Name { get; set; }
        public string Scope { get; set; }
        public int InitialPhaseId { get; set; }
    }
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

    public class IterationCreatePostModel
        {
            public int ProjectId { get; set; }
            public int PhaseId { get; set; }
            public string Name { get; set; }
            public string Objective { get; set; }
            public DateTime? Start_Date { get; set; }
            public DateTime? End_Date { get; set; }
        }

    public class ActivityCreatePostModel
    {
        public int ProjectId { get; set; }
        public int PhaseId { get; set; }
        public string Description { get; set; }
        public int ContextRoleId { get; set; } // El rol general de la actividad
        public List<int> AssignedUserIds { get; set; } // IDs de los usuarios asignados
        public string Status { get; set; }
        public DateTime? Due_Date { get; set; }
    }

    public class DocumentCreatePostModel
    {
        public int ProjectId { get; set; }
        public int PhaseId { get; set; }
        public string TypeClave { get; set; }
        public string Version { get; set; }
    }

}