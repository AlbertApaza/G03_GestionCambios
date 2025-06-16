using System;
using System.Collections.Generic;
using System.Linq;
using G03_GestionDeCambios.Models;

namespace G03_GestionDeCambios.Service
{
    public class DashboardService : IDisposable
    {
        private readonly BD_GestionDeCambiosEntities _context;

        public DashboardService()
        {
            _context = new BD_GestionDeCambiosEntities();
        }

        public AdminDashboardViewModel GetDashboardData(int idUsuarioAdmin)
        {
            List<int> proyectosAdministradosIds = _context.tbProyectoUsuario
                .Where(pu => pu.idUsuario == idUsuarioAdmin && pu.tbRoles.nombre.ToUpper() == "ADMIN")
                .Select(pu => pu.idProyecto.Value)
                .ToList();

            if (!proyectosAdministradosIds.Any())
            {
                return new AdminDashboardViewModel();
            }

            var viewModel = new AdminDashboardViewModel
            {
                TotalProyectos = proyectosAdministradosIds.Count(),
                TotalDocumentosSubidos = _context.tbDocumentos
                    .Count(d => d.idProyecto.HasValue && proyectosAdministradosIds.Contains(d.idProyecto.Value)),
                TotalUsuariosActivos = _context.tbProyectoUsuario
                    .Where(pu => pu.idProyecto.HasValue && proyectosAdministradosIds.Contains(pu.idProyecto.Value))
                    .Select(pu => pu.idUsuario)
                    .Distinct()
                    .Count(),
                TareasAtrasadas = _context.tbTareas
                    .Count(t => proyectosAdministradosIds.Contains(t.tbProyectoElemento.idProyecto.Value) &&
                                t.estado != "Finalizado" &&
                                t.tbProyectoElemento.fechaFin < DateTime.Now)
            };

            var tareasData = _context.tbTareas
                .Where(t => t.tbProyectoElemento.idProyecto.HasValue && proyectosAdministradosIds.Contains(t.tbProyectoElemento.idProyecto.Value))
                .GroupBy(t => t.estado)
                .Select(g => new { Estado = g.Key ?? "Sin Estado", Cantidad = g.Count() })
                .ToList();
            viewModel.TareasPorEstadoChart.Labels = tareasData.Select(d => d.Estado).ToList();
            viewModel.TareasPorEstadoChart.Data = tareasData.Select(d => d.Cantidad).ToList();

            var metodologiaData = _context.tbProyectos
                .Where(p => proyectosAdministradosIds.Contains(p.idProyecto))
                .GroupBy(p => p.tbMetodologias.nombre)
                .Select(g => new { Metodologia = g.Key, Cantidad = g.Count() })
                .ToList();
            viewModel.MetodologiaChart.Labels = metodologiaData.Select(d => d.Metodologia).ToList();
            viewModel.MetodologiaChart.Data = metodologiaData.Select(d => d.Cantidad).ToList();

            viewModel.UsuariosConMasTareas = _context.tbTareas
                .Where(t => t.tbProyectoElemento.idProyecto.HasValue && proyectosAdministradosIds.Contains(t.tbProyectoElemento.idProyecto.Value))
                .Where(t => t.idUsuario != null)
                .GroupBy(t => t.tbUsuarios)
                .Select(g => new { Usuario = g.Key, TotalTareas = g.Count() })
                .OrderByDescending(x => x.TotalTareas)
                .Select(x => new UsuarioRankingViewModel
                {
                    IdUsuario = x.Usuario.idUsuario,
                    NombreCompleto = x.Usuario.nombre + " " + x.Usuario.apellido,
                    TareasAsignadas = x.TotalTareas
                })
                .ToList();

            viewModel.ProyectosConMasSolicitudes = _context.tbSolicitudesCambio
                .Where(sc => proyectosAdministradosIds.Contains(sc.idProyecto))
                .GroupBy(sc => sc.tbProyectos.nombre)
                .Select(g => new { NombreProyecto = g.Key, Cantidad = g.Count() })
                .OrderByDescending(x => x.Cantidad)
                .Select(x => new ProyectoCambiosViewModel
                {
                    NombreProyecto = x.NombreProyecto,
                    CantidadSolicitudes = x.Cantidad
                })
                .ToList();

            var asignacionesMiembros = _context.tbProyectoUsuario
                .Where(pu => proyectosAdministradosIds.Contains(pu.idProyecto.Value) && pu.idUsuario != idUsuarioAdmin)
                .Select(pu => new
                {
                    Usuario = pu.tbUsuarios,
                    Proyecto = pu.tbProyectos
                })
                .ToList();

            var asignacionesInactivas = asignacionesMiembros
                .Where(asig => !_context.tbTareas.Any(t =>
                    t.idUsuario == asig.Usuario.idUsuario &&
                    t.tbProyectoElemento.idProyecto == asig.Proyecto.idProyecto))
                .ToList();

            viewModel.MiembrosConProyectosInactivos = asignacionesInactivas
                .GroupBy(a => a.Usuario)
                .Select(grupo => new MiembroConProyectosInactivosViewModel
                {
                    NombreCompleto = grupo.Key.nombre + " " + grupo.Key.apellido,
                    ProyectosSinTareas = grupo.Select(g => g.Proyecto.nombre).ToList()
                })
                .ToList();

            viewModel.ProyectosMasActivos = _context.tbProyectos
                .Where(p => proyectosAdministradosIds.Contains(p.idProyecto))
                .Select(p => new ProyectoActividadViewModel
                {
                    NombreProyecto = p.nombre,
                    PuntajeActividad =
                        p.tbProyectoElemento.Count() +
                        p.tbProyectoUsuario.Count() +
                        p.tbDocumentos.Count() +
                        p.tbSolicitudesCambio.Count() +
                        p.tbProyectoElemento.SelectMany(pe => pe.tbTareas).Count()
                })
                .OrderByDescending(p => p.PuntajeActividad)
                .ToList();

            return viewModel;
        }
        public HomeViewModel GetHomeData(int idUsuario)
        {
            var usuario = _context.tbUsuarios.Find(idUsuario);
            if (usuario == null)
            {
                return new HomeViewModel();
            }

            var viewModel = new HomeViewModel
            {
                NombreCompleto = usuario.nombre + " " + usuario.apellido,
                CantidadMisProyectos = _context.tbProyectoUsuario.Count(pu => pu.idUsuario == idUsuario),
                CantidadMisTareasPendientes = _context.tbTareas.Count(t => t.idUsuario == idUsuario && t.estado != "Finalizado"),
                EsAdminDeAlgunProyecto = _context.tbProyectoUsuario.Any(pu => pu.idUsuario == idUsuario && pu.tbRoles.nombre.ToUpper() == "ADMIN")
            };

            // --- NUEVA LÓGICA PARA OBTENER LAS TAREAS ---
            var tareasDelUsuario = _context.tbTareas
                .Where(t => t.idUsuario == idUsuario)
                .Select(t => new
                {
                    Proyecto = t.tbProyectoElemento.tbProyectos,
                    TareaNombre = t.nombre,
                    TareaDescripcion = t.descripcion,
                    TareaEstado = t.estado,
                    CicloNombre = t.tbProyectoElemento.tbCiclos.nombre
                })
                .ToList();

            // Agrupamos en memoria y poblamos el ViewModel
            viewModel.ProyectosConTareas = tareasDelUsuario
                .GroupBy(t => t.Proyecto)
                .Select(g => new ProyectoConTareasViewModel
                {
                    IdProyecto = g.Key.idProyecto,
                    NombreProyecto = g.Key.nombre,
                    Tareas = g.Select(tarea => new TareaViewModel
                    {
                        NombreTarea = tarea.TareaNombre,
                        Descripcion = tarea.TareaDescripcion,
                        Estado = tarea.TareaEstado,
                        NombreCiclo = tarea.CicloNombre ?? "Sin Etapa"
                    }).ToList()
                })
                .OrderBy(p => p.NombreProyecto)
                .ToList();

            return viewModel;
        }


        public List<UserProjectDetailViewModel> GetUsuarioProyectosTareas(int idUsuario)
        {
            var tareasPorProyecto = _context.tbTareas
                .Where(t => t.idUsuario == idUsuario)
                .GroupBy(t => t.tbProyectoElemento.tbProyectos)
                .ToList();

            var resultado = new List<UserProjectDetailViewModel>();

            foreach (var grupoProyecto in tareasPorProyecto)
            {
                resultado.Add(new UserProjectDetailViewModel
                {
                    IdProyecto = grupoProyecto.Key.idProyecto,
                    NombreProyecto = grupoProyecto.Key.nombre,
                    Tareas = grupoProyecto.Select(tarea => new UserTaskDetailViewModel
                    {
                        NombreTarea = tarea.nombre,
                        EstadoTarea = tarea.estado,
                        NombreCiclo = tarea.tbProyectoElemento.tbCiclos.nombre ?? "Sin Etapa"
                    }).ToList()
                });
            }
            return resultado;
        }

        public List<UserProjectAssignmentViewModel> GetProyectosDeMiembro(int idMiembro, int idUsuarioAdmin)
        {
            List<int> proyectosDelAdminIds = _context.tbProyectoUsuario
                .Where(pu => pu.idUsuario == idUsuarioAdmin && pu.tbRoles.nombre.ToUpper() == "ADMIN")
                .Select(pu => pu.idProyecto.Value)
                .ToList();

            return _context.tbProyectoUsuario
                .Where(pu => pu.idUsuario == idMiembro && proyectosDelAdminIds.Contains(pu.idProyecto.Value))
                .Select(pu => new UserProjectAssignmentViewModel
                {
                    NombreProyecto = pu.tbProyectos.nombre
                })
                .OrderBy(p => p.NombreProyecto)
                .ToList();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}