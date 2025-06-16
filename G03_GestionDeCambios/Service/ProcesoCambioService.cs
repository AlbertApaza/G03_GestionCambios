using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using G03_GestionDeCambios.Models;

namespace G03_GestionDeCambios.Service
{
    public class ProcesoCambioService
    {
        private readonly BD_GestionDeCambiosEntities _context;

        public ProcesoCambioService()
        {
            _context = new BD_GestionDeCambiosEntities();
        }
        public SolicitudDetalleViewModel GetSolicitudDetalle(int idSolicitud)
        {
            var solicitud = _context.tbSolicitudesCambio
                .Where(s => s.idSolicitudCambio == idSolicitud)
                .Select(s => new SolicitudDetalleViewModel
                {
                    // Encabezado
                    IdSolicitud = s.idSolicitudCambio,
                    FechaSolicitud = s.fechaSolicitud,

                    // Solicitante
                    SolicitadoPor = s.tbUsuarios.nombre + " " + s.tbUsuarios.apellido,
                    ProyectoProducto = s.tbProyectos.nombre,

                    // Solicitud
                    Objetivo = s.objetivoSolicitud,
                    DescripcionCambio = s.descripcionSolicitud,
                    // Navegación anidada para obtener el nombre del elemento
                    ElementoConfiguracion = s.tbProyectoElemento.tbElementos.nombre,
                    Impacto = s.impactoEstimado,
                    EsfuerzoEstimado = s.esfuerzoEstimado,

                    // Atención de la Solicitud (puede ser null)
                    RecibidoPor = s.idUsuarioReceptor != null ? s.tbUsuarios1.nombre + " " + s.tbUsuarios1.apellido : null,
                    FechaRecibido = s.fechaRecibida,
                    Estado = s.estadoSolicitud,
                    ImplementacionFecha = s.fechaInicioImplementacionCambio,
                    CierreCambioFecha = s.fechaCierreDelCambio,
                    Observaciones = s.observaciones

                    // Nota: Los otros campos de fecha (FechaEstado, GiroJefe, etc.)
                    // necesitarían sus propias columnas en la tabla tbSolicitudesCambio.
                    // Por ahora, se mostrarán en blanco.

                }).FirstOrDefault();

            return solicitud;
        }
    }
}