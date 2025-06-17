using System.Collections.Generic;

namespace G03_GestionDeCambios.Models
{
    // =======================================================================
    // MODELOS DE SOPORTE (DTOs)
    // =======================================================================

    public class FlagEvent
    {
        // CORRECCIÓN: Nombres de propiedades en minúscula para coincidir con Highcharts
        public long x { get; set; }
        public string title { get; set; }
        public string text { get; set; }
    }

    public class ChartDataViewModeld
    {
        public List<string> Labels { get; set; } = new List<string>();
        public List<int> Data { get; set; } = new List<int>();
    }

    public class UsuarioRankingViewModeld
    {
        public int IdUsuario { get; set; }
        public string NombreCompleto { get; set; }
        public int TareasAsignadas { get; set; }
    }

    public class ProyectoSolicitudesViewModel
    {
        public string NombreProyecto { get; set; }
        public int CantidadSolicitudes { get; set; }
    }

    public class ProyectoActividadViewModeld
    {
        public string NombreProyecto { get; set; }
        public int PuntajeActividad { get; set; }
    }


    // =======================================================================
    // EL VIEWMODEL PRINCIPAL
    // =======================================================================

    public class SuperAdminDashboardViewModel
    {
        public int TotalProyectos { get; set; }
        public int TotalUsuariosActivos { get; set; }
        public int TotalDocumentosSubidos { get; set; }
        public int TareasAtrasadas { get; set; }

        public List<object[]> ProyectosTimeline { get; set; }
        public List<object[]> UsuariosTimeline { get; set; }
        public List<FlagEvent> TimelineFlags { get; set; }

        public ChartDataViewModeld TareasPorEstadoChart { get; set; }
        public List<UsuarioRankingViewModeld> UsuariosConMasTareas { get; set; }
        public List<ProyectoSolicitudesViewModel> ProyectosConMasSolicitudes { get; set; }
        public List<ProyectoActividadViewModeld> ProyectosMasActivos { get; set; }

        public SuperAdminDashboardViewModel()
        {
            ProyectosTimeline = new List<object[]>();
            UsuariosTimeline = new List<object[]>();
            TimelineFlags = new List<FlagEvent>();
            TareasPorEstadoChart = new ChartDataViewModeld();
            UsuariosConMasTareas = new List<UsuarioRankingViewModeld>();
            ProyectosConMasSolicitudes = new List<ProyectoSolicitudesViewModel>();
            ProyectosMasActivos = new List<ProyectoActividadViewModeld>();
        }
    }
}