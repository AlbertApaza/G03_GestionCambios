using System;

namespace G03_GestionDeCambios.Models
{
    public class SolicitudListItemViewModel
    {
        public int IdSolicitudCambio { get; set; }
        public string DescripcionResumida { get; set; }
        public string EstadoSolicitud { get; set; }
        public DateTime FechaSolicitud { get; set; }
        public string NombreSolicitante { get; set; }
    }
}