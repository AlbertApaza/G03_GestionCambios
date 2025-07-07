using System;
using System.Collections.Generic;

namespace G03_GestionDeCambios.Models
{
    // Modelo principal para la vista VerAceptacion
    public class AceptacionViewModel
    {
        // Información básica
        public int IdSolicitud { get; set; }
        public SolicitudDetalleViewModel SolicitudInfo { get; set; } // Reutilizamos el VM que ya tienes

        // Resumen del ciclo de vida del cambio
        public List<HistorialViewModel.HistorialEntry> HistorialCompleto { get; set; }

        // Detalle de las tareas de implementación
        public List<TareaDetalleViewModel2> TareasImplementacion { get; set; }

        // Detalle de las pruebas de QA
        public QASummaryViewModel ResumenQA { get; set; }

        // Detalle de los despliegues realizados
        public List<DespliegueActivoViewModel> DesplieguesRealizados { get; set; }

        // Información de cierre (si ya fue cerrado)
        public CierreInfoViewModel InfoCierre { get; set; }

        public AceptacionViewModel()
        {
            HistorialCompleto = new List<HistorialViewModel.HistorialEntry>();
            TareasImplementacion = new List<TareaDetalleViewModel2>();
            DesplieguesRealizados = new List<DespliegueActivoViewModel>();
        }
    }

    // Un resumen de los resultados de QA
    public class QASummaryViewModel
    {
        public int PruebasEjecutadas { get; set; }
        public int IncidenciasReportadas { get; set; }
        public string AprobadoPorQA { get; set; }
        public DateTime FechaAprobacionQA { get; set; }
        public string ComentariosQA { get; set; }
    }

    // Información de cierre de la solicitud
    public class CierreInfoViewModel
    {
        public string CerradoPor { get; set; }
        public DateTime FechaCierre { get; set; }
        public string ComentariosFinales { get; set; }
        public string DecisionFinal { get; set; } // Aceptado, Rechazado
    }
}

//revisado.
