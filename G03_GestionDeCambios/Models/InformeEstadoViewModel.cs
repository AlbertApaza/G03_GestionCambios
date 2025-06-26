using System;

namespace G03_GestionDeCambios.Models
{
    public class InformeEstadoViewModel
    {
        // Encabezado del documento
        public string CodigoDocumento { get; set; } = "R-GCSW004";
        public string Version { get; set; } = "0";
        public string Pagina { get; set; } = "1-1";

        // Datos del informe
        public int NumeroSolicitud { get; set; }
        public DateTime FechaInforme { get; set; }
        public string NombreProyecto { get; set; }
        public string NombreDocumento { get; set; }
        public string DescripcionCambio { get; set; }
        public string ElementoAfectado { get; set; }
        public string EstadoCambioSolicitado { get; set; }
        public string ResponsableDelCambio { get; set; }
        public string EstadoImplementacion { get; set; }
        public string ImpactoSistema { get; set; }
        public string Observaciones { get; set; }
    }
}