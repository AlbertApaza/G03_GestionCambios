using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace G03_GestionDeCambios.Models
{
    // Modelo principal para la vista VerDespliegue
    public class DespliegueViewModel
    {
        public int IdSolicitud { get; set; }
        public string ObjetivoSolicitud { get; set; }

        // Resumen del paso anterior para dar contexto
        public ResumenQAViewModel ResumenQA { get; set; }

        // Para el formulario de nuevo despliegue
        public List<SelectListItem> EntornosDisponibles { get; set; }

        // Lista de despliegues ya iniciados o completados para esta solicitud
        public List<DespliegueActivoViewModel> DesplieguesActivos { get; set; }

        // Lógica de negocio: solo se puede cerrar si hay al menos un despliegue completado
        public bool PuedeCerrarSolicitud { get; set; }

        public DespliegueViewModel()
        {
            EntornosDisponibles = new List<SelectListItem>();
            DesplieguesActivos = new List<DespliegueActivoViewModel>();
        }
    }

    // Muestra el resumen de la aprobación de QA
    public class ResumenQAViewModel
    {
        public string AprobadoPor { get; set; }
        public DateTime FechaAprobacion { get; set; }
        public string ComentariosQA { get; set; }
    }

    // Representa un despliegue que está en curso o ya finalizó
    public class DespliegueActivoViewModel
    {
        public int IdDespliegue { get; set; }
        public string EntornoNombre { get; set; }
        public string Estado { get; set; } // Pendiente, En Proceso, Completado, Fallido
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public List<PasoDespliegueViewModel> Pasos { get; set; }
    }

    // Representa un paso individual dentro de un plan de despliegue
    public class PasoDespliegueViewModel
    {
        public int IdPaso { get; set; }
        public int Orden { get; set; }
        public string Descripcion { get; set; }
        public string Estado { get; set; } // Pendiente, Completado
        public string CompletadoPor { get; set; }
        public DateTime? FechaCompletado { get; set; }
        public string Notas { get; set; }
    }
}