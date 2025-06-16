using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace G03_GestionDeCambios.Models
{
    // Modelo principal para la vista Index. Contiene la lista y el formulario.
    public class SolicitudIndexViewModel
    {
        public List<SolicitudListadoViewModel> Solicitudes { get; set; }
        public SolicitudCreacionViewModel FormularioCreacion { get; set; }

        public SolicitudIndexViewModel()
        {
            Solicitudes = new List<SolicitudListadoViewModel>();
            FormularioCreacion = new SolicitudCreacionViewModel();
        }
    }

    // Modelo para mostrar cada fila en la tabla de solicitudes.
    public class SolicitudListadoViewModel
    {
        public int IdSolicitudCambio { get; set; }
        [Display(Name = "N° Solicitud")]
        public string CodigoSolicitud { get; set; } // Formateado como R-GCSW001-ID
        [Display(Name = "Proyecto")]
        public string NombreProyecto { get; set; }
        [Display(Name = "Objetivo")]
        public string Objetivo { get; set; }
        [Display(Name = "Fecha Solicitud")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime FechaSolicitud { get; set; }
        [Display(Name = "Estado")]
        public string Estado { get; set; }
        public int PasoActualProceso { get; set; }
    }

    // Modelo para el formulario de creación en el modal.
    public class SolicitudCreacionViewModel
    {
        [Required(ErrorMessage = "Debe seleccionar un Proyecto.")]
        [Display(Name = "Proyecto/Producto")]
        public int? IdProyecto { get; set; }

        [Required(ErrorMessage = "El objetivo de la solicitud es obligatorio.")]
        [Display(Name = "Objetivo")]
        [DataType(DataType.MultilineText)]
        public string ObjetivoSolicitud { get; set; }

        [Required(ErrorMessage = "La descripción del cambio es obligatoria.")]
        [Display(Name = "Descripción del Cambio solicitado")]
        [DataType(DataType.MultilineText)]
        public string DescripcionSolicitud { get; set; }

        [Required(ErrorMessage = "Debe seleccionar el elemento de configuración afectado.")]
        [Display(Name = "Elemento de Configuración del software afectado")]
        public int? IdElementoAfectado { get; set; } // Esto es idProyectoElemento

        [Required(ErrorMessage = "El impacto estimado es obligatorio.")]
        [Display(Name = "Impacto")]
        [DataType(DataType.MultilineText)]
        public string ImpactoEstimado { get; set; }

        [Required(ErrorMessage = "El esfuerzo estimado es obligatorio.")]
        [Display(Name = "Esfuerzo estimado")]
        [DataType(DataType.MultilineText)]
        public string EsfuerzoEstimado { get; set; }

        // Para llenar los DropDownList
        public IEnumerable<SelectListItem> ProyectosDisponibles { get; set; }
        public IEnumerable<SelectListItem> ElementosDisponibles { get; set; }

        public SolicitudCreacionViewModel()
        {
            ProyectosDisponibles = new List<SelectListItem>();
            ElementosDisponibles = new List<SelectListItem>();
        }
    }
}