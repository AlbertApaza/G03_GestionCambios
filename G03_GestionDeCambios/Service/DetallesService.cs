using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;
using G03_GestionDeCambios.Models;
using G03_GestionDeCambios.ViewModels.DetallesViewModels;

namespace G03_GestionDeCambios.Service
{
    public class DetallesService : IDisposable
    {
        private readonly BD_GestionDeCambiosEntities _dbContext;

        public DetallesService()
        {
            _dbContext = new BD_GestionDeCambiosEntities();
        }

        // --- NUEVO MÉTODO PARA POBLAR EL DASHBOARD ---
        public DetallesIndexViewModel GetDetallesProyectoDashboard(int idProyecto)
        {
            var proyecto = _dbContext.tbProyectos.FirstOrDefault(p => p.idProyecto == idProyecto);
            if (proyecto == null) return null;

            var viewModel = new DetallesIndexViewModel
            {
                IdProyecto = proyecto.idProyecto,
                NombreProyecto = proyecto.nombre,
                CodCicloActual = proyecto.codCicloActual,
                NombreCicloActual = proyecto.tbCiclos?.nombre ?? "No definido"
            };

            if (proyecto.fechaFin.HasValue)
            {
                var totalDiasProyecto = (proyecto.fechaFin.Value - proyecto.fechaInicio).Days;
                viewModel.DiasRestantes = (proyecto.fechaFin.Value - DateTime.Now).Days;
                if (totalDiasProyecto > 0)
                {
                    viewModel.DiasTranscurridos = (DateTime.Now - proyecto.fechaInicio).Days;
                    viewModel.PorcentajeCompletado = (viewModel.DiasTranscurridos * 100) / totalDiasProyecto;
                    if (viewModel.PorcentajeCompletado > 100) viewModel.PorcentajeCompletado = 100;
                    if (viewModel.PorcentajeCompletado < 0) viewModel.PorcentajeCompletado = 0;
                }
            }
            else
            {
                viewModel.DiasTranscurridos = (DateTime.Now - proyecto.fechaInicio).Days;
            }

            var tareasProyecto = proyecto.tbProyectoElemento.SelectMany(pe => pe.tbTareas).ToList();
            viewModel.TareasTotales = tareasProyecto.Count;
            viewModel.TareasCompletadas = tareasProyecto.Count(t => t.estado == "Finalizado");
            viewModel.TareasPendientes = viewModel.TareasTotales - viewModel.TareasCompletadas;

            viewModel.TotalMiembros = proyecto.tbProyectoUsuario.Count;
            viewModel.MiembrosSinRol = proyecto.tbProyectoUsuario
                .Where(pu => pu.idRol == null)
                .Select(pu => pu.tbUsuarios.nombre + " " + pu.tbUsuarios.apellido)
                .ToList();

            viewModel.TotalElementosConfiguracion = proyecto.tbProyectoElemento.Count;
            viewModel.TotalDocumentos = proyecto.tbDocumentos.Count;
            viewModel.TotalSolicitudesCambio = proyecto.tbSolicitudesCambio.Count;

            viewModel.ActividadPorMiembro = proyecto.tbProyectoUsuario
                .Select(pu => new MiembroActividad2ViewModel
                {
                    NombreCompleto = pu.tbUsuarios.nombre + " " + pu.tbUsuarios.apellido,
                    TareasAsignadas = pu.tbUsuarios.tbTareas
                        .Count(t => t.tbProyectoElemento.idProyecto == idProyecto),
                    DocumentosSubidos = pu.tbUsuarios.tbDocumentos
                        .Count(d => d.idProyecto == idProyecto)
                })
                .OrderByDescending(m => m.TareasAsignadas + m.DocumentosSubidos)
                .ToList();

            viewModel.CiclosDisponiblesSelectList = GetCiclosPorMetodologiaProyecto(idProyecto);

            return viewModel;
        }

        // --- MÉTODOS EXISTENTES (NO SE TOCAN) ---
        public tbProyectos GetProyectoById(int idProyecto)
        {
            return _dbContext.tbProyectos.FirstOrDefault(p => p.idProyecto == idProyecto);
        }

        public List<SelectListItem> GetCiclosPorMetodologiaProyecto(int idProyecto)
        {
            var proyecto = _dbContext.tbProyectos.Find(idProyecto);
            if (proyecto == null)
            {
                return new List<SelectListItem>();
            }

            return _dbContext.tbCiclos
                .Where(c => c.idMetodologia == proyecto.idMetodologia)
                .OrderBy(c => c.orden)
                .Select(c => new SelectListItem
                {
                    Value = c.codCiclo,
                    Text = c.nombre
                }).ToList();
        }

        public bool ActualizarCicloActualProyecto(int idProyecto, string nuevoCodCiclo)
        {
            var proyecto = _dbContext.tbProyectos.Find(idProyecto);
            if (proyecto == null)
            {
                return false;
            }

            var cicloExiste = _dbContext.tbCiclos.Any(c => c.codCiclo == nuevoCodCiclo && c.idMetodologia == proyecto.idMetodologia);
            if (!cicloExiste)
            {
                return false;
            }

            proyecto.codCicloActual = nuevoCodCiclo;
            try
            {
                _dbContext.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al actualizar ciclo del proyecto: {ex.Message}");
                return false;
            }
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }
    }
}