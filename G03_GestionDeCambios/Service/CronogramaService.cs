// ~/Service/CronogramaService.cs (o el nombre que tenías para dhtmlxGantt)
using System;
using System.Collections.Generic;
using System.Linq;
using G03_GestionDeCambios.Models;
using System.Data.Entity;

namespace G03_GestionDeCambios.Service
{
    public class GanttTaskViewModel
    {
        public string id { get; set; }
        public string text { get; set; }
        public string start_date { get; set; } // Formato: "dd-MM-yyyy"
        public int duration { get; set; }
        public decimal progress { get; set; }
        public string parent { get; set; }
        public string type { get; set; } // "project", "task", "milestone"
        public bool open { get; set; } = true;

        public string estado { get; set; }
        public string responsable { get; set; }
        public string rol { get; set; }
    }

    public class GanttLinkViewModel { /* ... como antes ... */ }

    public class GanttDataResponse
    {
        public List<GanttTaskViewModel> data { get; set; }
        public List<GanttLinkViewModel> links { get; set; }
        public GanttDataResponse() { data = new List<GanttTaskViewModel>(); links = new List<GanttLinkViewModel>(); }
    }

    public class CronogramaService // O el nombre original que usaste para dhtmlxGantt
    {
        private readonly BD_GestionDeCambiosEntities _dbContext;
        private const string DateFormat = "dd-MM-yyyy"; // Definir formato una vez

        public CronogramaService()
        {
            _dbContext = new BD_GestionDeCambiosEntities();
        }

        public string GetNombreProyecto(int idProyecto)
        {
            return _dbContext.tbProyectos
                .Where(p => p.idProyecto == idProyecto)
                .Select(p => p.nombre)
                .FirstOrDefault();
        }

        public GanttDataResponse GetGanttDataParaProyecto(int idProyecto)
        {
            var response = new GanttDataResponse();
            var proyecto = _dbContext.tbProyectos.Include(p => p.tbUsuarios).FirstOrDefault(p => p.idProyecto == idProyecto);

            if (proyecto == null) return response;

            // ID del proyecto raíz
            string proyectoRootId = $"proj_{proyecto.idProyecto}";

            response.data.Add(new GanttTaskViewModel
            {
                id = proyectoRootId,
                text = proyecto.nombre,
                start_date = proyecto.fechaInicio.ToString(DateFormat),
                duration = proyecto.fechaFin.HasValue && proyecto.fechaFin.Value >= proyecto.fechaInicio ? (proyecto.fechaFin.Value - proyecto.fechaInicio).Days + 1 : 1,
                progress = CalcularProgresoProyecto(proyecto.idProyecto),
                type = "project",
                open = true,
                estado = TraducirEstadoProyecto(proyecto.estado),
                responsable = proyecto.tbUsuarios?.nombre + " " + proyecto.tbUsuarios?.apellido
            });

            var ciclosDelProyecto = _dbContext.tbProyectoCiclo
                .Where(pc => pc.idProyecto == idProyecto)
                .Include(pc => pc.tbCiclos)
                .OrderBy(pc => pc.tbCiclos.orden)
                .ToList();

            foreach (var pc in ciclosDelProyecto)
            {
                var ciclo = pc.tbCiclos;
                if (ciclo == null) continue;

                string cicloId = $"ciclo_{ciclo.codCiclo}_{idProyecto}";
                DateTime cicloStartDate = pc.inicioCiclo ?? proyecto.fechaInicio;
                int cicloDuration = (pc.finCiclo.HasValue && pc.finCiclo.Value >= cicloStartDate) ? (pc.finCiclo.Value - cicloStartDate).Days + 1 : 1;
                if (cicloDuration <= 0) cicloDuration = 1; // Duración mínima 1

                response.data.Add(new GanttTaskViewModel
                {
                    id = cicloId,
                    text = ciclo.nombre,
                    start_date = cicloStartDate.ToString(DateFormat),
                    duration = cicloDuration,
                    progress = CalcularProgresoCiclo(idProyecto, ciclo.codCiclo),
                    parent = proyectoRootId,
                    type = "project",
                    open = true,
                    estado = "En progreso" // O calcular
                });

                var elementosDelCiclo = _dbContext.tbProyectoElemento
                    .Where(pe => pe.idProyecto == idProyecto && pe.codCiclo == ciclo.codCiclo && pe.fechaInicio.HasValue) // Solo con fecha de inicio
                    .Include(pe => pe.tbElementos)
                    .Include(pe => pe.tbRoles)
                    .ToList();

                foreach (var pe in elementosDelCiclo)
                {
                    string elementoId = $"elem_{pe.idProyectoElemento}";
                    int elementoDuration = (pe.fechaFin.HasValue && pe.fechaFin.Value >= pe.fechaInicio.Value) ? (pe.fechaFin.Value - pe.fechaInicio.Value).Days + 1 : 1;
                    if (elementoDuration <= 0) elementoDuration = 1;

                    response.data.Add(new GanttTaskViewModel
                    {
                        id = elementoId,
                        text = pe.tbElementos.nombre,
                        start_date = pe.fechaInicio.Value.ToString(DateFormat),
                        duration = elementoDuration,
                        progress = (pe.estado == "Finalizado" ? 1m : (pe.estado == "En Proceso" ? 0.5m : 0m)),
                        parent = cicloId,
                        type = "task", 
                        open = true,
                        estado = pe.estado,
                        rol = pe.tbRoles?.nombre
                    });

                    var tareasDelElemento = _dbContext.tbTareas
                        .Where(t => t.idProyectoElemento == pe.idProyectoElemento)
                        .Include(t => t.tbUsuarios)
                        .ToList();

                    foreach (var tarea in tareasDelElemento)
                    {
                        response.data.Add(new GanttTaskViewModel
                        {
                            id = $"tarea_{tarea.idTareas}",
                            text = tarea.nombre,
                            start_date = pe.fechaInicio.Value.ToString(DateFormat), 
                            duration = 2, // Tareas individuales podrían durar 1 día o ser milestones
                            progress = (tarea.estado == "Finalizado" ? 1m : (tarea.estado == "En Proceso" ? 0.5m : 0m)),
                            parent = elementoId, // Anidada al elemento
                            type = "task", // O "milestone" si duration es 0 (ajustar start_date si es milestone)
                            estado = tarea.estado,
                            responsable = tarea.tbUsuarios?.nombre + " " + tarea.tbUsuarios?.apellido
                        });
                    }
                }
            }
            return response;
        }

        private decimal CalcularProgresoProyecto(int idProyecto)
        {
            var elementosProyecto = _dbContext.tbProyectoElemento.Where(pe => pe.idProyecto == idProyecto).ToList();
            if (!elementosProyecto.Any()) return 0;
            var finalizados = elementosProyecto.Count(pe => pe.estado == "Finalizado");
            return (decimal)finalizados / elementosProyecto.Count();
        }
        private decimal CalcularProgresoCiclo(int idProyecto, string codCiclo)
        {
            var elementosCiclo = _dbContext.tbProyectoElemento.Where(pe => pe.idProyecto == idProyecto && pe.codCiclo == codCiclo).ToList();
            if (!elementosCiclo.Any()) return 0;
            var finalizados = elementosCiclo.Count(pe => pe.estado == "Finalizado");
            return (decimal)finalizados / elementosCiclo.Count();
        }
        private string TraducirEstadoProyecto(int estadoId)
        {
            switch (estadoId)
            {
                case 1: return "Activo";
                case 0: return "Inactivo";
                default: return "Desconocido";
            }
        }
        public void Dispose()
        {
            _dbContext.Dispose();
        }
    }
}