// EN TU CARPETA ViewModels o similar
// TuProyecto.ViewModels;

using System.Collections.Generic;
using System;
namespace G03_ProyectoGestion.ViewModels
{

    public class ProjectTimelineViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string StartDate { get; set; } // Formato ISO: "yyyy-MM-dd"
        public string EndDate { get; set; }   // Formato ISO: "yyyy-MM-dd"
    }

    public class RupPhaseViewModel
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public string TitleColorClass { get; set; }
        public string TimelineBarColorClass { get; set; }
        public List<string> DefaultActivities { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
    }

    public class RupActivityViewModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string PhaseKey { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int DurationDays { get; set; }
        public string Status { get; set; }
        public string RoleJsKey { get; set; }
        public int RoleDbId { get; set; }
        public List<UserViewModel> Assignees { get; set; }
        public int DbId { get; set; }
    }

    public class UserViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class RoleViewModel
    {
        public string Id { get; set; } // jsKey
        public string Name { get; set; }
        public List<UserViewModel> Members { get; set; }
        public int DbId { get; set; }
    }

    public class CronogramaActivityCreateModel
    {
        public int ProjectId { get; set; }
        public string Name { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public string PhaseKey { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public DateTime StartDate { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public DateTime EndDate { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public string Status { get; set; }
        public string RoleJsKey { get; set; }
        public List<int> AssigneeUserIds { get; set; }
    }

    // Modelo para la pestaña de documentos Alpine (si se mantiene la lógica original)
    public class ProjectViewModel // Usado por Alpine
    {
        public int id { get; set; }
        public string name { get; set; }
        public string scope { get; set; }
        public int current_phase { get; set; }
    }

    public class ActivityCreatePostModel // Usado por Alpine si se mantiene CreateActivityOriginal
    {
        public int ProjectId { get; set; }
        public int PhaseId { get; set; }
        public string Description { get; set; }
        public int ContextRoleId { get; set; }
        public List<int> AssignedUserIds { get; set; }
        public string Status { get; set; }
        public DateTime? Due_Date { get; set; } // Podría ser StartDate
                                                // public DateTime? End_Date { get; set; } // Si se añade EndDate
    }

    public class DocumentCreatePostModel // Usado por Alpine
    {
        public int ProjectId { get; set; }
        public int PhaseId { get; set; }
        public string TypeClave { get; set; }
        public string Version { get; set; }
    }

}