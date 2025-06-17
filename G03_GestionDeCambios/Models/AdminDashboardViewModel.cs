using System.Collections.Generic;
using System.Linq;

namespace G03_GestionDeCambios.Models
{
    public class AdminDashboardViewModel
    {
        public int TotalProyectos { get; set; }
        public int TotalUsuariosActivos { get; set; }
        public int TotalDocumentosSubidos { get; set; }
        public int TareasAtrasadas { get; set; }
        public List<UsuarioRankingViewModel> UsuariosConMasTareas { get; set; }
        public List<ProyectoCambiosViewModel> ProyectosConMasSolicitudes { get; set; }
        public List<MiembroConProyectosInactivosViewModel> MiembrosConProyectosInactivos { get; set; }
        public List<ProyectoActividadViewModel> ProyectosMasActivos { get; set; }
        public ChartData TareasPorEstadoChart { get; set; }
        public ChartData MetodologiaChart { get; set; }
        public int TotalTareas => TareasPorEstadoChart?.Data.Sum() ?? 0;

        public AdminDashboardViewModel()
        {
            UsuariosConMasTareas = new List<UsuarioRankingViewModel>();
            ProyectosConMasSolicitudes = new List<ProyectoCambiosViewModel>();
            MiembrosConProyectosInactivos = new List<MiembroConProyectosInactivosViewModel>();
            ProyectosMasActivos = new List<ProyectoActividadViewModel>();
            TareasPorEstadoChart = new ChartData();
            MetodologiaChart = new ChartData();
        }
    }

    public class ChartData
    {
        public List<string> Labels { get; set; } = new List<string>();
        public List<int> Data { get; set; } = new List<int>();
    }

    public class UsuarioRankingViewModel
    {
        public int IdUsuario { get; set; }
        // FIX: Corrected property name from "NombreCompletos" to "NombreCompleto"
        public string NombreCompleto { get; set; }
        public int TareasAsignadas { get; set; }
    }

    public class ProyectoCambiosViewModel
    {
        public string NombreProyecto { get; set; }
        public int CantidadSolicitudes { get; set; }
    }

    public class MiembroConProyectosInactivosViewModel
    {
        public string NombreCompleto { get; set; }
        public List<string> ProyectosSinTareas { get; set; }
    }

    public class UserProjectDetailViewModel
    {
        public int IdProyecto { get; set; }
        public string NombreProyecto { get; set; }
        public List<UserTaskDetailViewModel> Tareas { get; set; }
    }

    public class UserTaskDetailViewModel
    {
        public string NombreTarea { get; set; }
        public string EstadoTarea { get; set; }
        public string NombreCiclo { get; set; }
    }

    public class UserProjectAssignmentViewModel
    {
        public string NombreProyecto { get; set; }
    }

    public class ProyectoActividadViewModel
    {
        public string NombreProyecto { get; set; }
        public int PuntajeActividad { get; set; }
    }
}