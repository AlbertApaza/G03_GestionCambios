using System;
using System.Data.Entity;
using System.Linq;
using G03_GestionDeCambios.Models;

namespace G03_GestionDeCambios.Service
{
    public class SuperAdminDashboardService : IDisposable
    {
        private readonly BD_GestionDeCambiosEntities _context;
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public SuperAdminDashboardService()
        {
            _context = new BD_GestionDeCambiosEntities();
        }

        public SuperAdminDashboardViewModel GetSuperAdminDashboardData()
        {
            var viewModel = new SuperAdminDashboardViewModel();

            // --- 1. DATOS PARA LAS TARJETAS DE ESTADÍSTICAS ---
            viewModel.TotalProyectos = _context.tbProyectos.Count(p => p.estado == 1);
            viewModel.TotalDocumentosSubidos = _context.tbDocumentos.Count();
            viewModel.TotalUsuariosActivos = _context.tbUsuarios.Count(u => u.estado == 1);
            viewModel.TareasAtrasadas = _context.tbTareas
                .Count(t => t.estado != "Finalizado" &&
                            t.tbProyectoElemento.fechaFin < DateTime.Now);

            // --- 2. DATOS PARA EL GRÁFICO DE LÍNEA DE TIEMPO Y EVENTOS ---
            var projectGrowthData = _context.tbProyectos
                .GroupBy(p => DbFunctions.TruncateTime(p.fechaInicio))
                .Select(g => new { Date = g.Key.Value, Count = g.Count() })
                .OrderBy(x => x.Date)
                .ToList();

            long cumulativeProjects = 0;
            int milestoneTarget = 5;

            // Generamos la línea de tiempo y los flags en el mismo bucle.
            foreach (var item in projectGrowthData)
            {
                long previousTotal = cumulativeProjects;
                cumulativeProjects += item.Count;

                viewModel.ProyectosTimeline.Add(new object[] { ToJsTimestamp(item.Date), cumulativeProjects });

                // Lógica para los hitos de proyectos
                if (cumulativeProjects >= milestoneTarget && previousTotal < milestoneTarget)
                {
                    viewModel.TimelineFlags.Add(new FlagEvent
                    {
                        x = ToJsTimestamp(item.Date),
                        title = $"P{milestoneTarget}",
                        text = $"Hito: {milestoneTarget} proyectos creados"
                    });

                    milestoneTarget += 5;
                }
            }

            var userGrowthData = _context.tbUsuarios
                .Where(u => u.fechaCreacion.HasValue)
                .GroupBy(u => DbFunctions.TruncateTime(u.fechaCreacion.Value))
                .Select(g => new { Date = g.Key.Value, Count = g.Count() })
                .OrderBy(x => x.Date)
                .ToList();

            long cumulativeUsers = 0;
            viewModel.UsuariosTimeline = userGrowthData.Select(item =>
            {
                cumulativeUsers += item.Count;
                return new object[] { ToJsTimestamp(item.Date), cumulativeUsers };
            }).ToList();


            // --- 3. DATOS PARA LAS TABLAS ---
            var tareasData = _context.tbTareas
                .GroupBy(t => t.estado)
                .Select(g => new { Estado = g.Key ?? "Sin Estado", Cantidad = g.Count() })
                .ToList();

            viewModel.TareasPorEstadoChart.Labels = tareasData.Select(d => d.Estado).ToList();
            viewModel.TareasPorEstadoChart.Data = tareasData.Select(d => d.Cantidad).ToList();

            viewModel.UsuariosConMasTareas = _context.tbUsuarios
                .Where(u => u.estado == 1)
                .Select(u => new UsuarioRankingViewModeld
                {
                    IdUsuario = u.idUsuario,
                    NombreCompleto = u.nombre + " " + u.apellido,
                    TareasAsignadas = u.tbTareas.Count()
                })
                .Where(u => u.TareasAsignadas > 0)
                .OrderByDescending(u => u.TareasAsignadas)
                .Take(5)
                .ToList();

            var proyectosActivos = _context.tbProyectos
                .Where(p => p.estado == 1)
                .Select(p => new
                {
                    p.idProyecto,
                    p.nombre,
                    CantidadSolicitudes = _context.tbSolicitudesCambio
                                                  .Count(sc => sc.tbProyectoElemento.idProyecto == p.idProyecto),
                    CantidadTareas = p.tbProyectoElemento.SelectMany(pe => pe.tbTareas).Count()
                })
                .ToList();

            viewModel.ProyectosMasActivos = proyectosActivos
                .Select(p => new ProyectoActividadViewModeld
                {
                    NombreProyecto = p.nombre,
                    PuntajeActividad = p.CantidadTareas + p.CantidadSolicitudes
                })
                .Where(p => p.PuntajeActividad > 0)
                .OrderByDescending(p => p.PuntajeActividad)
                .Take(5)
                .ToList();

            viewModel.ProyectosConMasSolicitudes = proyectosActivos
                .Where(p => p.CantidadSolicitudes > 0)
                .Select(p => new ProyectoSolicitudesViewModel
                {
                    NombreProyecto = p.nombre,
                    CantidadSolicitudes = p.CantidadSolicitudes
                })
                .OrderByDescending(p => p.CantidadSolicitudes)
                .Take(5)
                .ToList();

            return viewModel;
        }

        public static long ToJsTimestamp(DateTime dateTime)
        {
            var specifiedDateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Local);
            return (long)(specifiedDateTime.ToUniversalTime() - UnixEpoch).TotalMilliseconds;
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}