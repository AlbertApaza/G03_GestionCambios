using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using G03_GestionDeCambios.Models;
using G03_GestionDeCambios.ViewModels.DetallesViewModels;

namespace G03_GestionDeCambios.Service
{
    public class EstadisticasService : IDisposable
    {
        private readonly BD_GestionDeCambiosEntities _context;

        public EstadisticasService() { _context = new BD_GestionDeCambiosEntities(); }

        public EstadisticasViewModel GetEstadisticasProyecto(int idProyecto)
        {
            // Busca el proyecto y sus relaciones
            var proyecto = _context.tbProyectos
                .Include("tbProyectoElemento.tbTareas")
                .Include("tbProyectoUsuario.tbUsuarios.tbTareas.tbProyectoElemento")
                .Include("tbProyectoUsuario.tbUsuarios.tbDocumentos")
                .Include("tbDocumentos")
                .FirstOrDefault(p => p.idProyecto == idProyecto);

            if (proyecto == null) return null;

            var viewModel = new EstadisticasViewModel
            {
                IdProyecto = proyecto.idProyecto,
                NombreProyecto = proyecto.nombre
            };

            // 1. Calcular KPIs
            var tareasProyecto = proyecto.tbProyectoElemento.SelectMany(pe => pe.tbTareas).ToList();
            viewModel.TareasCompletadas = tareasProyecto.Count(t => t.estado == "Finalizado");
            viewModel.TareasPendientes = tareasProyecto.Count(t => t.estado != "Finalizado");

            viewModel.TotalMiembros = proyecto.tbProyectoUsuario.Count();
            viewModel.TotalDocumentos = proyecto.tbDocumentos.Count();

            // 2. Calcular Actividad por Miembro (para la tabla)
            viewModel.ActividadPorMiembro = proyecto.tbProyectoUsuario
                .Select(pu => new MiembroActividadViewModel
                {
                    NombreCompleto = pu.tbUsuarios.nombre + " " + pu.tbUsuarios.apellido,
                    TareasAsignadas = pu.tbUsuarios.tbTareas
                        .Count(t => t.tbProyectoElemento.idProyecto == idProyecto),
                    DocumentosSubidos = pu.tbUsuarios.tbDocumentos
                        .Count(d => d.idProyecto == idProyecto && d.idUsuarioSubida == pu.idUsuario)
                })
                .OrderByDescending(m => m.TareasAsignadas + m.DocumentosSubidos)
                .ToList();

            return viewModel;
        }

        // ===================================================================
        // === AGREGA ESTE NUEVO MÉTODO A TU SERVICIO ========================
        // ===================================================================
        public List<SolicitudListItemViewModel> GetSolicitudesProyecto(int idProyecto)
        {
            var solicitudes = _context.tbSolicitudesCambio
                .Where(s => s.idProyecto == idProyecto)
                .Include(s => s.tbUsuarios) // Para obtener el nombre del solicitante
                .OrderByDescending(s => s.fechaSolicitud)
                .Select(s => new SolicitudListItemViewModel
                {
                    IdSolicitudCambio = s.idSolicitudCambio,
                    // Acortamos la descripción para que no ocupe mucho en la tabla
                    DescripcionResumida = s.descripcionSolicitud.Length > 100
                                          ? s.descripcionSolicitud.Substring(0, 100) + "..."
                                          : s.descripcionSolicitud,
                    EstadoSolicitud = s.estadoSolicitud,
                    FechaSolicitud = s.fechaSolicitud ?? DateTime.MinValue,
                    NombreSolicitante = s.tbUsuarios.nombre + " " + s.tbUsuarios.apellido
                })
                .ToList();

            return solicitudes;
        }


        // === MODIFICA EL MÉTODO GetInformeEstadoData =======================
        // Ahora buscará por ID de solicitud, no por proyecto.
        public InformeEstadoViewModel GetInformeEstadoData(int idSolicitud) // <-- PARÁMETRO CAMBIADO
        {
            // La consulta ahora busca una solicitud específica por su ID
            var solicitud = _context.tbSolicitudesCambio
                              .Include(s => s.tbProyectos)
                              .Include(s => s.tbProyectoElemento.tbElementos)
                              .Include(s => s.tbUsuarios1) // Receptor del cambio
                              .FirstOrDefault(s => s.idSolicitudCambio == idSolicitud); // <-- LÓGICA CAMBIADA

            if (solicitud == null)
            {
                return null;
            }

            var responsable = solicitud.tbUsuarios1;
            var informeModel = new InformeEstadoViewModel
            {
                NumeroSolicitud = solicitud.idSolicitudCambio,
                FechaInforme = DateTime.Now,
                NombreProyecto = solicitud.tbProyectos.nombre,
                NombreDocumento = $"Documento de Solicitud {solicitud.codigoDocumentoSolicitd} – Primera Versión Aprobada",
                DescripcionCambio = solicitud.descripcionSolicitud,
                ElementoAfectado = solicitud.tbProyectoElemento?.tbElementos?.nombre ?? "No especificado",
                EstadoCambioSolicitado = "Aprobado",
                ResponsableDelCambio = responsable != null ? $"{responsable.nombre} {responsable.apellido}" : "No asignado",
                EstadoImplementacion = "Aprobado",
                ImpactoSistema = solicitud.impactoEstimado,
                Observaciones = solicitud.observaciones ?? "Sin observaciones."
            };

            return informeModel;
        }

        public void Dispose() { _context.Dispose(); }
    }
}