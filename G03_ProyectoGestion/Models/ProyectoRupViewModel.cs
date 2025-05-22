// EN TU CARPETA ViewModels o similar
// TuProyecto.ViewModels;

using System.Collections.Generic;
using System;
using G03_ProyectoGestion.Models;
namespace G03_ProyectoGestion.ViewModels
{

    // ViewModels (puedes ponerlos en un archivo separado o al inicio del controlador)
    public class ActivityCreatePostModel
    {
        public int ProjectId { get; set; } // Cambiado
        public int PhaseId { get; set; }   // Cambiado
        public string Description { get; set; }
        public int ContextRoleId { get; set; }
        public List<int> AssignedUserIds { get; set; }
        public string Status { get; set; }
        public DateTime? Due_Date { get; set; }
    }

    public class DocumentCreatePostModel
    {
        public int ProjectId { get; set; } // Cambiado
        public int PhaseId { get; set; }   // Cambiado
        public string TypeClave { get; set; }
        public string Version { get; set; }
        // El archivo HttpPostedFileBase se maneja por separado
    }

    public class ProjectViewModel
    {
        public int id { get; set; }
        public string name { get; set; }
        public string scope { get; set; }
        public int current_phase { get; set; } // Eliminamos esta, se maneja dinámicamente
        public System.DateTime? projectStartDate { get; set; } // Añadido
        public System.DateTime? projectEndDate { get; set; }   // Añadido
    }

    // En G03_ProyectoGestion.ViewModels
    public class ConfigRupViewModel
    {
        public ProjectViewModel Project { get; set; }
        public List<tbRupFases> RupPhases { get; set; }
        public List<ProjectPhaseDatesViewModel> ProjectPhaseDates { get; set; }
        public DateTime ProjectStartDate { get; set; }
        public DateTime ProjectEndDate { get; set; }
        public int ProjectId { get; set; }
    }

    public class ProjectPhaseDatesViewModel
    {
        public int PhaseId { get; set; }
        public string PhaseName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

}