using System.Collections.Generic;

namespace G03_GestionDeCambios.Models
{
    // --- ViewModel Principal para la página de Estadísticas ---
    public class EstadisticasViewModel
    {
        public int IdProyecto { get; set; }
        public string NombreProyecto { get; set; }

        // --- Propiedades para los KPIs (las tarjetas de resumen) ---
        public int TareasCompletadas { get; set; }
        public int TareasPendientes { get; set; }
        public int TotalMiembros { get; set; }
        public int TotalDocumentos { get; set; }

        // --- Propiedad para la Tabla de Actividad por Miembro ---
        public List<MiembroActividadViewModel> ActividadPorMiembro { get; set; }

        // --- Propiedades para los Gráficos ---
        public ChartDataViewModel TareasPorEstadoChart { get; set; }
        public ChartDataViewModel ActividadPorMiembroChart { get; set; }

        public EstadisticasViewModel()
        {
            // Inicializamos todas las colecciones
            ActividadPorMiembro = new List<MiembroActividadViewModel>();
            TareasPorEstadoChart = new ChartDataViewModel();
            ActividadPorMiembroChart = new ChartDataViewModel();
        }
    }

    // --- Definición para la Tabla de Actividad por Miembro ---
    public class MiembroActividadViewModel
    {
        public string NombreCompleto { get; set; }
        public int TareasAsignadas { get; set; }
        public int DocumentosSubidos { get; set; }
    }

    // --- Clases de Soporte para los Gráficos (si decides usarlos) ---
    public class ChartDataViewModel
    {
        public List<string> Labels { get; set; }
        public List<ChartDataset> Datasets { get; set; }

        public ChartDataViewModel()
        {
            Labels = new List<string>();
            Datasets = new List<ChartDataset>();
        }
    }

    public class ChartDataset
    {
        public string Label { get; set; }
        public List<int> Data { get; set; }
        public object BackgroundColor { get; set; }
        public object BorderColor { get; set; }
        public bool? Fill { get; set; }
        public double? Tension { get; set; } = 0.1;
    }
}
