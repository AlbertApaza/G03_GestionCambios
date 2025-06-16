using System;
using System.Collections.Generic;
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

        public void Dispose() { _context.Dispose(); }
    }
}