using System.Collections.Generic;

namespace G03_GestionDeCambios.Models
{
    // ViewModel principal para la vista del dashboard con línea de tiempo
    public class AdminTimelineDashboardViewModel
    {
        // Propiedades existentes para las tarjetas de estadísticas
        public int TotalProyectos { get; set; }
        public int TotalUsuariosActivos { get; set; }
        public int TotalDocumentosSubidos { get; set; }
        public int TareasAtrasadas { get; set; }

        // Propiedades existentes para el gráfico de Highcharts
        public List<object[]> ProyectosTimeline { get; set; }
        public List<object[]> UsuariosTimeline { get; set; }
        // CAMBIO: Se usa la nueva clase con sufijo "Global"
        public List<TimelineGlobalFlag> TimelineFlags { get; set; }

        // --- PROPIEDADES ACTUALIZADAS PARA USAR LAS CLASES RENOMBRADAS ---
        // CAMBIO: Se usa la nueva clase con sufijo "Global"
        public List<UsuarioRankingGlobalViewModel> UsuariosConMasTareas { get; set; }
        public List<ProyectoCambiosGlobalViewModel> ProyectosConMasSolicitudes { get; set; }
        public List<ProyectoActividadGlobalViewModel> ProyectosMasActivos { get; set; }

        // Constructor para inicializar todas las listas y evitar errores
        public AdminTimelineDashboardViewModel()
        {
            ProyectosTimeline = new List<object[]>();
            UsuariosTimeline = new List<object[]>();
            // CAMBIO: Se inicializa la lista con el nuevo tipo
            TimelineFlags = new List<TimelineGlobalFlag>();
            // CAMBIO: Se inicializan las listas con los nuevos tipos
            UsuariosConMasTareas = new List<UsuarioRankingGlobalViewModel>();
            ProyectosConMasSolicitudes = new List<ProyectoCambiosGlobalViewModel>();
            ProyectosMasActivos = new List<ProyectoActividadGlobalViewModel>();
        }
    }

    // --- CLASES AUXILIARES RENOMBRADAS CON EL SUFIJO "Global" ---

    // CAMBIO: Clase renombrada de "TimelineFlag" a "TimelineGlobalFlag"
    public class TimelineGlobalFlag
    {
        public long x { get; set; } // Timestamp en milisegundos
        public string title { get; set; }
        public string text { get; set; }
    }

    // CAMBIO: Clase renombrada de "UsuarioRankingViewModel" a "UsuarioRankingGlobalViewModel"
    public class UsuarioRankingGlobalViewModel
    {
        public int IdUsuario { get; set; }
        public string NombreCompleto { get; set; }
        public int TareasAsignadas { get; set; }
    }

    // CAMBIO: Clase renombrada de "ProyectoCambiosViewModel" a "ProyectoCambiosGlobalViewModel"
    public class ProyectoCambiosGlobalViewModel
    {
        public string NombreProyecto { get; set; }
        public int CantidadSolicitudes { get; set; }
    }

    // CAMBIO: Clase renombrada de "ProyectoActividadViewModel" a "ProyectoActividadGlobalViewModel"
    public class ProyectoActividadGlobalViewModel
    {
        public string NombreProyecto { get; set; }
        public int PuntajeActividad { get; set; }
    }
}