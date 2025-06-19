// En Models/QAViewModel.cs
using System;
using System.Collections.Generic;

namespace G03_GestionDeCambios.Models
{
    public class QAViewModel
    {
        public int IdSolicitud { get; set; }
        public string ObjetivoSolicitud { get; set; }
        public string ResumenDesarrollo { get; set; } // Comentarios del desarrollador al enviar a QA
        public List<QATareaViewModel> PlanDePruebas { get; set; }
        public List<IncidenciaViewModel> IncidenciasRegistradas { get; set; }
        public List<MiembroAsignableViewModel> Desarrolladores { get; set; } // Para asignar correcciones
        public int ProgresoPruebas { get; set; }
        public int IncidenciasAbiertas { get; set; }
        public int PruebasFallidas { get; set; }

    }

    public class QATareaViewModel
    {
        public int IdTarea { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public string AsignadoA { get; set; }
        public string Estado { get; set; } // Pendiente, En Ejecución, Pasó, Falló
    }

    public class IncidenciaViewModel
    {
        public int IdTareaIncidencia { get; set; }
        public string Descripcion { get; set; }
        public string Severidad { get; set; } // Crítica, Alta, Media, Baja
        public string Estado { get; set; } // Abierta, En Corrección, Corregida
        public string ReportadoPor { get; set; }
        public string AsignadoA { get; set; } // Desarrollador que debe corregirla
    }
}