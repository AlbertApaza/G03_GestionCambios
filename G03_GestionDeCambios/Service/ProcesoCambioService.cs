using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using G03_GestionDeCambios.Models;
using System.Data.Entity;
using System.Web.Mvc; // Para usar .Include()
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
                    Observaciones = s.observaciones,
                    FechaEstado = s.fechaEstado

                    // Nota: Los otros campos de fecha (FechaEstado, GiroJefe, etc.)
                    // necesitarían sus propias columnas en la tabla tbSolicitudesCambio.
                    // Por ahora, se mostrarán en blanco.

                }).FirstOrDefault();

            return solicitud;
        }

        public AnalisisSolicitudViewModel GetAnalisisViewModel(int idSolicitud)
        {
            var solicitud = _context.tbSolicitudesCambio
                .Include(s => s.tbProyectos)
                .Include(s => s.tbProyectos.tbMetodologias)
                .Include(s => s.tbProyectoElemento)
                .Include(s => s.tbProyectoElemento.tbElementos)
                .Include(s => s.tbUsuarios)
                .FirstOrDefault(s => s.idSolicitudCambio == idSolicitud);

            if (solicitud == null) return null;

            var viewModel = new AnalisisSolicitudViewModel
            {
                IdSolicitud = idSolicitud,
                // Reutiliza la lógica que ya tienes para los detalles básicos si es posible
                SolicitudInfo = GetSolicitudDetalle(idSolicitud),

                // 1. Contexto del Proyecto
                ProyectoContexto = new ProyectoContextoViewModel
                {
                    NombreProyecto = solicitud.tbProyectos.nombre,
                    Metodologia = solicitud.tbProyectos.tbMetodologias.nombre,
                    FechaInicio = solicitud.tbProyectos.fechaInicio,
                    FechaFin = solicitud.tbProyectos.fechaFin,
                    CicloActual = solicitud.tbProyectos.codCicloActual
                },

                // 2. Contexto del Elemento Afectado
                ElementoAfectado = new ElementoContextoViewModel
                {
                    NombreElemento = solicitud.tbProyectoElemento.tbElementos.nombre,
                    FechaInicio = solicitud.tbProyectoElemento.fechaInicio,
                    FechaFin = solicitud.tbProyectoElemento.fechaFin,
                    TareasAsociadas = _context.tbTareas
                        .Where(t => t.idProyectoElemento == solicitud.idElementoAfectado)
                        .Select(t => new TareaViewModel
                        {
                            NombreTarea = t.nombre,
                            UsuarioAsignado = t.tbUsuarios.nombre + " " + t.tbUsuarios.apellido,
                            Estado = t.estado
                        }).ToList()
                },

                // 3. Historial Reciente
                HistorialReciente = new HistorialViewModel
                {
                    // Busca el comentario específico del paso anterior
                    ComentarioPasoAnterior = _context.tbSolicitudHistorial
                        .Where(h => h.idSolicitudCambio == idSolicitud && h.decision == "Enviado a Análisis")
                        .OrderByDescending(h => h.fechaAccion)
                        .Select(h => h.comentarios)
                        .FirstOrDefault() ?? "No se dejaron comentarios adicionales.",
                    UsuarioPasoAnterior = _context.tbSolicitudHistorial
                        .Where(h => h.idSolicitudCambio == idSolicitud && h.decision == "Enviado a Análisis")
                        .OrderByDescending(h => h.fechaAccion)
                        .Select(h => h.tbUsuarios.nombre + " " + h.tbUsuarios.apellido)
                        .FirstOrDefault() ?? "N/A",
                    Entradas = _context.tbSolicitudHistorial
                        .Where(h => h.idSolicitudCambio == idSolicitud)
                        .OrderByDescending(h => h.fechaAccion)
                        .Select(h => new HistorialViewModel.HistorialEntry
                        {
                            Fecha = h.fechaAccion,
                            Usuario = h.tbUsuarios.nombre + " " + h.tbUsuarios.apellido,
                            Decision = h.decision,
                            Comentarios = h.comentarios
                        }).ToList()
                },

                // 4. Disponibilidad del Equipo
                EquipoProyecto = GetEquipoViewModel(solicitud.idProyecto)
            };

            return viewModel;
        }
        private EquipoViewModel GetEquipoViewModel(int idProyecto)
        {
            // Usuarios ocupados (en tareas activas de este proyecto)
            var usuariosOcupadosIds = _context.tbTareas
                .Where(t => t.tbProyectoElemento.idProyecto == idProyecto && t.estado == "En Proceso")
                .Select(t => t.idUsuario)
                .Distinct()
                .ToList();

            var miembros = _context.tbProyectoUsuario
                .Where(pu => pu.idProyecto == idProyecto)
                .Select(pu => new MiembroEquipo
                {
                    NombreCompleto = pu.tbUsuarios.nombre + " " + pu.tbUsuarios.apellido,
                    Rol = pu.tbRoles.nombre,
                    // Determina la disponibilidad
                    Disponibilidad = usuariosOcupadosIds.Contains(pu.idUsuario) ? "En Tarea" : "Disponible"
                }).ToList();

            return new EquipoViewModel { Miembros = miembros };
        }
        public AprobacionViewModel GetAprobacionViewModel(int idSolicitud)
        {
            // Buscamos el registro de historial donde el analista aprobó el paso anterior
            var analisisAprobado = _context.tbSolicitudHistorial
                .Include(h => h.tbUsuarios)
                .Where(h => h.idSolicitudCambio == idSolicitud && h.decision == "Análisis Aprobado")
                .OrderByDescending(h => h.fechaAccion)
                .FirstOrDefault();

            if (analisisAprobado == null)
            {
                // Esto indica que la solicitud no llegó aquí por el camino correcto.
                // Podrías manejarlo como un error o devolver null.
                return null;
            }

            // Historial completo
            var historialCompleto = _context.tbSolicitudHistorial
                        .Include(h => h.tbUsuarios)
                        .Where(h => h.idSolicitudCambio == idSolicitud)
                        .OrderByDescending(h => h.fechaAccion)
                        .Select(h => new HistorialViewModel.HistorialEntry
                        {
                            Fecha = h.fechaAccion,
                            Usuario = h.tbUsuarios.nombre + " " + h.tbUsuarios.apellido,
                            Decision = h.decision,
                            Comentarios = h.comentarios
                        }).ToList();

            var viewModel = new AprobacionViewModel
            {
                IdSolicitud = idSolicitud,
                SolicitudInfo = GetSolicitudDetalle(idSolicitud), // Reutilizamos el método que ya tienes
                ResumenAnalisis = new ResumenAnalisisViewModel
                {
                    AnalistaNombre = analisisAprobado.tbUsuarios.nombre + " " + analisisAprobado.tbUsuarios.apellido,
                    FechaAnalisis = analisisAprobado.fechaAccion,
                    JustificacionAnalisis = analisisAprobado.comentarios
                },
                HistorialCompleto = new HistorialViewModel { Entradas = historialCompleto }
            };

            return viewModel;
        }
        public AsignacionViewModel GetAsignacionViewModel(int idSolicitud)
        {
            var solicitud = _context.tbSolicitudesCambio
                .Include(s => s.tbProyectoElemento.tbElementos)
                .FirstOrDefault(s => s.idSolicitudCambio == idSolicitud);

            if (solicitud == null || !solicitud.idElementoAfectado.HasValue) return null;

            var idProyecto = solicitud.idProyecto;
            var idProyectoElemento = solicitud.idElementoAfectado.Value;

            // El rol requerido para este elemento específico
            var idRolRequerido = solicitud.tbProyectoElemento.idRol;

            // Obtener usuarios del proyecto que tienen el rol requerido para este elemento
            var usuariosElegiblesIds = _context.tbProyectoUsuario
                .Where(pu => pu.idProyecto == idProyecto && pu.idRol == idRolRequerido)
                .Select(pu => pu.idUsuario)
                .ToList();

            var equipoElegible = _context.tbUsuarios
                .Where(u => usuariosElegiblesIds.Contains(u.idUsuario))
                .Select(u => new MiembroAsignableViewModel
                {
                    IdUsuario = u.idUsuario,
                    NombreCompleto = u.nombre + " " + u.apellido,
                    // Obtenemos el rol desde la tabla de asignación del proyecto
                    Rol = _context.tbProyectoUsuario
                              .FirstOrDefault(pu => pu.idProyecto == idProyecto && pu.idUsuario == u.idUsuario).tbRoles.nombre,
                    // Calculamos la carga de trabajo (tareas no finalizadas)
                    TareasActivas = _context.tbTareas.Count(t => t.idUsuario == u.idUsuario && t.estado != "Finalizado")
                }).ToList();

            // Obtener tareas ya asignadas a este elemento
            var tareasYaAsignadas = _context.tbTareas
                .Where(t => t.idProyectoElemento == idProyectoElemento)
                .Select(t => new TareaViewModel
                {
                    NombreTarea = t.nombre,
                    UsuarioAsignado = t.tbUsuarios.nombre + " " + t.tbUsuarios.apellido,
                    Estado = t.estado
                }).ToList();

            var viewModel = new AsignacionViewModel
            {
                IdSolicitud = idSolicitud,
                SolicitudInfo = GetSolicitudDetalle(idSolicitud), // Reutilizar
                ResumenAnalisis = GetAprobacionViewModel(idSolicitud)?.ResumenAnalisis, // Reutilizar
                ElementoAfectadoNombre = solicitud.tbProyectoElemento.tbElementos.nombre,
                IdProyectoElemento = idProyectoElemento,
                EquipoElegible = equipoElegible,
                TareasYaAsignadas = tareasYaAsignadas
            };

            return viewModel;
        }
        public DesarrolloViewModel GetDesarrolloViewModel(int idSolicitud)
        {
            var solicitud = _context.tbSolicitudesCambio
                .Include(s => s.tbProyectoElemento.tbElementos)
                .FirstOrDefault(s => s.idSolicitudCambio == idSolicitud);

            if (solicitud == null || !solicitud.idElementoAfectado.HasValue) return null;

            var tareas = _context.tbTareas
                .Where(t => t.idProyectoElemento == solicitud.idElementoAfectado)
                .Select(t => new TareaDetalleViewModel2
                {
                    Nombre = t.nombre,
                    Descripcion = t.descripcion,
                    AsignadoA = t.tbUsuarios.nombre + " " + t.tbUsuarios.apellido,
                    Estado = t.estado
                }).ToList();

            if (!tareas.Any())
            {
                // Si no hay tareas, el progreso es 0 o 100 dependiendo de la regla de negocio.
                // Asumimos 0 para evitar pasar a QA sin trabajo definido.
                return new DesarrolloViewModel
                {
                    IdSolicitud = idSolicitud,
                    ObjetivoSolicitud = solicitud.objetivoSolicitud,
                    ElementoAfectadoNombre = solicitud.tbProyectoElemento.tbElementos.nombre,
                    Tareas = new List<TareaDetalleViewModel2>(),
                    Progreso = 0
                };
            }

            // Calcular el progreso
            int tareasCompletadas = tareas.Count(t => t.Estado == "Finalizado");
            int progreso = (int)Math.Round((double)tareasCompletadas / tareas.Count * 100);

            var viewModel = new DesarrolloViewModel
            {
                IdSolicitud = idSolicitud,
                ObjetivoSolicitud = solicitud.objetivoSolicitud,
                ElementoAfectadoNombre = solicitud.tbProyectoElemento.tbElementos.nombre,
                Tareas = tareas,
                Progreso = progreso
            };

            return viewModel;
        }
        // En ProcesoCambioService.cs

        public QAViewModel GetQAViewModel(int idSolicitud)
        {
            var solicitud = _context.tbSolicitudesCambio.Find(idSolicitud);
            if (solicitud == null || !solicitud.idElementoAfectado.HasValue) return null;

            var resumenDev = _context.tbSolicitudHistorial
                .Where(h => h.idSolicitudCambio == idSolicitud && h.decision == "Enviado a QA")
                .OrderByDescending(h => h.fechaAccion)
                .Select(h => h.comentarios)
                .FirstOrDefault() ?? "No se dejaron comentarios.";

            var todasLasTareasDelElemento = _context.tbTareas
                .Where(t => t.idProyectoElemento == solicitud.idElementoAfectado)
                .Include(t => t.tbUsuarios)
                .ToList();

            // Tareas de Prueba: Las que empiezan con "[PRUEBA]"
            var planDePruebas = todasLasTareasDelElemento
                .Where(t => t.nombre.StartsWith("[PRUEBA]"))
                .Select(t => new QATareaViewModel
                {
                    IdTarea = t.idTareas,
                    Nombre = t.nombre.Replace("[PRUEBA] - ", ""), // Mostramos un nombre limpio
                    Descripcion = t.descripcion,
                    AsignadoA = t.tbUsuarios?.nombre + " " + t.tbUsuarios?.apellido,
                    Estado = t.estado
                }).ToList();

            // Incidencias: Las que empiezan con "[DEFECTO]"
            var incidencias = todasLasTareasDelElemento
                .Where(t => t.nombre.StartsWith("[DEFECTO]"))
                .Select(t => new IncidenciaViewModel
                {
                    IdTareaIncidencia = t.idTareas,
                    Descripcion = t.descripcion,
                    Severidad = t.nombre.Contains(":") ? t.nombre.Split(':')[1].Trim() : "No especificada",
                    Estado = t.estado,
                    AsignadoA = t.tbUsuarios?.nombre + " " + t.tbUsuarios?.apellido,
                    ReportadoPor = "Equipo de QA"
                }).ToList();

            var desarrolladores = _context.tbProyectoUsuario
                .Where(pu => pu.idProyecto == solicitud.idProyecto && pu.tbRoles.nombre == "Desarrollador")
                .Select(pu => new MiembroAsignableViewModel
                {
                    IdUsuario = pu.idUsuario.Value,
                    NombreCompleto = pu.tbUsuarios.nombre + " " + pu.tbUsuarios.apellido
                }).ToList();

            // CÁLCULOS CORRECTOS BASADOS EN EL NUEVO FLUJO
            int pruebasEjecutadas = planDePruebas.Count(p => p.Estado != "Pendiente");
            int progreso = planDePruebas.Any() ? (int)Math.Round((double)pruebasEjecutadas / planDePruebas.Count * 100) : 100; // Si no hay pruebas, está 100% listo

            int pruebasFallidas = planDePruebas.Count(p => p.Estado == "En Proceso"); // "En Proceso" es nuestra bandera de "Falló"
            int incidenciasAbiertas = incidencias.Count(i => i.Estado != "Finalizado");

            return new QAViewModel
            {
                IdSolicitud = idSolicitud,
                ObjetivoSolicitud = solicitud.objetivoSolicitud,
                ResumenDesarrollo = resumenDev,
                PlanDePruebas = planDePruebas,
                IncidenciasRegistradas = incidencias,
                Desarrolladores = desarrolladores,
                ProgresoPruebas = progreso,
                IncidenciasAbiertas = incidenciasAbiertas,
                PruebasFallidas = pruebasFallidas,
            };
        }

        public DespliegueViewModel GetDespliegueViewModel(int idSolicitud)
        {
            var solicitud = _context.tbSolicitudesCambio.Find(idSolicitud);
            if (solicitud == null) return null;

            // 1. Obtener resumen de la aprobación de QA
            var aprobacionQA = _context.tbSolicitudHistorial
                .Where(h => h.idSolicitudCambio == idSolicitud && h.decision == "QA Aprobado para Despliegue")
                .OrderByDescending(h => h.fechaAccion)
                .Select(h => new ResumenQAViewModel
                {
                    AprobadoPor = h.tbUsuarios.nombre + " " + h.tbUsuarios.apellido,
                    FechaAprobacion = h.fechaAccion,
                    ComentariosQA = h.comentarios
                }).FirstOrDefault();

            // 2. Obtener entornos de despliegue disponibles
            var entornos = _context.tbEntornosDespliegue
                .Select(e => new SelectListItem
                {
                    Value = e.idEntorno.ToString(),
                    Text = e.nombre
                }).ToList();

            // 3. Obtener despliegues existentes para esta solicitud
            var desplieguesActivos = _context.tbDespliegues
                .Where(d => d.idSolicitudCambio == idSolicitud)
                .OrderByDescending(d => d.fechaInicio)
                .Select(d => new DespliegueActivoViewModel
                {
                    IdDespliegue = d.idDespliegue,
                    EntornoNombre = d.tbEntornosDespliegue.nombre,
                    Estado = d.estado,
                    FechaInicio = d.fechaInicio,
                    FechaFin = d.fechaFin,
                    Pasos = _context.tbPasosDespliegue
                        .Where(p => p.idDespliegue == d.idDespliegue)
                        .OrderBy(p => p.orden)
                        .Select(p => new PasoDespliegueViewModel
                        {
                            IdPaso = p.idPasoDespliegue,
                            Orden = p.orden,
                            Descripcion = p.descripcion,
                            Estado = p.estado,
                            CompletadoPor = p.idUsuarioCompletado != null ? p.tbUsuarios.nombre + " " + p.tbUsuarios.apellido : "N/A",
                            FechaCompletado = p.fechaCompletado,
                            Notas = p.notas
                        }).ToList()
                }).ToList();

            var viewModel = new DespliegueViewModel
            {
                IdSolicitud = idSolicitud,
                ObjetivoSolicitud = solicitud.objetivoSolicitud,
                ResumenQA = aprobacionQA,
                EntornosDisponibles = entornos,
                DesplieguesActivos = desplieguesActivos,
                // La solicitud se puede cerrar si al menos un despliegue ha sido completado con éxito
                PuedeCerrarSolicitud = desplieguesActivos.Any(d => d.Estado == "Completado")
            };

            return viewModel;
        }
        public AceptacionViewModel GetAceptacionViewModel(int idSolicitud)
        {
            var solicitud = _context.tbSolicitudesCambio.Find(idSolicitud);
            if (solicitud == null) return null;

            // 1. Información básica de la solicitud (reutilizando)
            var solicitudInfo = GetSolicitudDetalle(idSolicitud);

            // 2. Historial completo (reutilizando)
            var historial = _context.tbSolicitudHistorial
                .Where(h => h.idSolicitudCambio == idSolicitud)
                .OrderBy(h => h.fechaAccion)
                .Select(h => new HistorialViewModel.HistorialEntry
                {
                    Fecha = h.fechaAccion,
                    Usuario = h.tbUsuarios.nombre + " " + h.tbUsuarios.apellido,
                    Decision = h.decision,
                    Comentarios = h.comentarios,
                    PasoProceso = h.nombrePasoProceso
                }).ToList();

            // 3. Tareas de implementación
            var tareas = _context.tbTareas
                .Where(t => t.idProyectoElemento == solicitud.idElementoAfectado)
                .Select(t => new TareaDetalleViewModel2
                {
                    Nombre = t.nombre,
                    Descripcion = t.descripcion,
                    AsignadoA = t.tbUsuarios.nombre + " " + t.tbUsuarios.apellido,
                    Estado = t.estado
                }).ToList();

            // 4. Resumen de QA
            var aprobacionQA = _context.tbSolicitudHistorial
                .Where(h => h.idSolicitudCambio == idSolicitud && h.decision == "QA Aprobado para Despliegue")
                .OrderByDescending(h => h.fechaAccion)
                .FirstOrDefault();

            var resumenQA = new QASummaryViewModel
            {
                PruebasEjecutadas = _context.tbTareas.Count(t => t.idProyectoElemento == solicitud.idElementoAfectado && !t.nombre.StartsWith("[DEFECTO]")),
                IncidenciasReportadas = _context.tbTareas.Count(t => t.idProyectoElemento == solicitud.idElementoAfectado && t.nombre.StartsWith("[DEFECTO]")),
                AprobadoPorQA = aprobacionQA?.tbUsuarios.nombre + " " + aprobacionQA?.tbUsuarios.apellido,
                FechaAprobacionQA = aprobacionQA?.fechaAccion ?? DateTime.MinValue,
                ComentariosQA = aprobacionQA?.comentarios
            };

            // 5. Despliegues realizados (reutilizando)
            var despliegues = _context.tbDespliegues
                .Where(d => d.idSolicitudCambio == idSolicitud)
                .OrderBy(d => d.fechaInicio)
                .Select(d => new DespliegueActivoViewModel
                {
                    IdDespliegue = d.idDespliegue,
                    EntornoNombre = d.tbEntornosDespliegue.nombre,
                    Estado = d.estado,
                    FechaInicio = d.fechaInicio,
                    FechaFin = d.fechaFin
                }).ToList();

            // 6. Información de cierre (si ya existe)
            var cierreHistorial = _context.tbSolicitudHistorial
                .Where(h => h.idSolicitudCambio == idSolicitud && h.pasoProceso == 8)
                .OrderByDescending(h => h.fechaAccion)
                .FirstOrDefault();

            var infoCierre = cierreHistorial != null ? new CierreInfoViewModel
            {
                CerradoPor = cierreHistorial.tbUsuarios.nombre + " " + cierreHistorial.tbUsuarios.apellido,
                FechaCierre = cierreHistorial.fechaAccion,
                ComentariosFinales = cierreHistorial.comentarios,
                DecisionFinal = cierreHistorial.decision
            } : null;

            // Ensamblar el ViewModel final
            var viewModel = new AceptacionViewModel
            {
                IdSolicitud = idSolicitud,
                SolicitudInfo = solicitudInfo,
                HistorialCompleto = historial,
                TareasImplementacion = tareas,
                ResumenQA = resumenQA,
                DesplieguesRealizados = despliegues,
                InfoCierre = infoCierre
            };

            return viewModel;
        }

    }
}