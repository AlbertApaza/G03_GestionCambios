using System;
using System.ComponentModel.DataAnnotations;

namespace G03_GestionDeCambios.Models
{
    public class SolicitudDetalleViewModel
    {
        // Encabezado del Documento
        public string CodigoDocumento { get; set; } = "R-GCSW001";
        public int Version { get; set; } = 0;
        public string Pagina { get; set; } = "1";
        public int IdSolicitud { get; set; }

        [Display(Name = "No de Solicitud")]
        public string NoSolicitudCompleto => $"{CodigoDocumento}-{IdSolicitud}";

        [Display(Name = "Fecha")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime? FechaSolicitud { get; set; }

        // Sección SOLICITANTE
        [Display(Name = "Solicitada por")]
        public string SolicitadoPor { get; set; }

        [Display(Name = "Proyecto/Producto")]
        public string ProyectoProducto { get; set; }

        // Sección SOLICITUD
        [Display(Name = "Objetivo")]
        public string Objetivo { get; set; }

        [Display(Name = "Descripción del Cambio solicitado")]
        public string DescripcionCambio { get; set; }

        [Display(Name = "Elemento de Configuración del software afectado")]
        public string ElementoConfiguracion { get; set; }

        [Display(Name = "Impacto")]
        public string Impacto { get; set; }

        [Display(Name = "Esfuerzo estimado")]
        public string EsfuerzoEstimado { get; set; }

        // Sección ATENCIÓN DE LA SOLICITUD (se llenarán en pasos posteriores)
        [Display(Name = "Recibido por")]
        public string RecibidoPor { get; set; }

        [Display(Name = "Fecha")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime? FechaRecibido { get; set; }

        [Display(Name = "Estado")]
        public string Estado { get; set; }

        [Display(Name = "Fecha (Estado)")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime? FechaEstado { get; set; } // Podrías necesitar una columna para esto

        [Display(Name = "Giro a Jefe de Proyecto – Fecha")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime? GiroJefeProyectoFecha { get; set; } // Columna a agregar

        [Display(Name = "Implementación de cambio – Fecha")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime? ImplementacionFecha { get; set; }

        [Display(Name = "Modificación de la versión – Fecha")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime? ModificacionVersionFecha { get; set; } // Columna a agregar

        [Display(Name = "Cierre del Cambio – Fecha")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime? CierreCambioFecha { get; set; }

        [Display(Name = "Observaciones")]
        public string Observaciones { get; set; }
    }
}