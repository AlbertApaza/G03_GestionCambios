using System.Collections.Generic;
using System.Web.Mvc;

namespace G03_GestionDeCambios.ViewModels.DetallesViewModels
{
    // --- ViewModel Principal ---
    public class DetallesIndexViewModel
    {
        // Información básica y para el formulario
        public int IdProyecto { get; set; }
        public string NombreProyecto { get; set; }
        public string CodCicloActual { get; set; }
        public string NombreCicloActual { get; set; }
        public IEnumerable<SelectListItem> CiclosDisponiblesSelectList { get; set; }

        // KPIs de Tiempo y Progreso
        public int? DiasRestantes { get; set; }
        public int DiasTranscurridos { get; set; }
        public int PorcentajeCompletado { get; set; }

        // KPIs de Tareas
        public int TareasTotales { get; set; }
        public int TareasCompletadas { get; set; }
        public int TareasPendientes { get; set; }

        // KPIs de Equipo
        public int TotalMiembros { get; set; }
        public List<string> MiembrosSinRol { get; set; }

        // KPIs de Contenido
        public int TotalElementosConfiguracion { get; set; }
        public int TotalDocumentos { get; set; }
        public int TotalSolicitudesCambio { get; set; }

        // Ranking de Actividad
        public List<MiembroActividad2ViewModel> ActividadPorMiembro { get; set; }

        public DetallesIndexViewModel()
        {
            MiembrosSinRol = new List<string>();
            ActividadPorMiembro = new List<MiembroActividad2ViewModel>();
        }
    }

    // --- ViewModel de Soporte para la tabla de actividad ---
    public class MiembroActividad2ViewModel
    {
        public string NombreCompleto { get; set; }
        public int TareasAsignadas { get; set; }
        public int DocumentosSubidos { get; set; }
    }
}