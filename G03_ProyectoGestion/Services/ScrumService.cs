using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using G03_ProyectoGestion.Models;
using System.Data.Entity;


namespace G03_ProyectoGestion.Services
{
    public class ScrumService
    {

        public (bool success, tbProyectos proyecto, List<string> errorMessage) ObtenerProyecto(int idProyecto)
        {
            using (var db = new g03_databaseEntities())
            {
                var proyecto = db.tbProyectos
                                 .FirstOrDefault(p => p.idProyecto == idProyecto && p.idMetodologia == 1);

                if (proyecto == null)
                {
                    return (false, null, new List<string> { "Proyecto SCRUM no encontrado o no válido." });
                }

                return (true, proyecto, null);
            }
        }

        public List<object> ObtenerUsuariosActivos()
        {
            using (var db = new g03_databaseEntities())
            {
                var usuarios = db.tbUsuarios
                                 .Where(u => u.estadoUsuario == "activo") // Asumiendo campo 'activo'
                                 .Select(u => new { id = u.idUsuario, name = u.nombreUsuario })
                                 .OrderBy(u => u.name)
                                 .ToList();

                return usuarios.Cast<object>().ToList();
            }
        }


        public List<object> ObtenerSprintsPorProyecto(int idProyecto)
        {
            using (var db = new g03_databaseEntities())
            {
                return db.tbScrumSprints
                        .Where(s => s.idProyecto == idProyecto)
                        .OrderBy(s => s.fechaInicio)
                        .ToList() // <-- ejecuta la consulta y trae los datos a memoria
                        .Select(s => new
                        {
                            id = s.idSprint,
                            project_id = s.idProyecto,
                            name = s.nombreSprint,
                            start_date = s.fechaInicio.HasValue ? s.fechaInicio.Value.ToString("yyyy-MM-dd") : null,
                            end_date = s.fechaFin.HasValue ? s.fechaFin.Value.ToString("yyyy-MM-dd") : null
                        })
                        .Cast<object>()
                        .ToList();
            }
        }

        public (bool success, tbScrumSprints sprint, List<string> errors) CrearSprint(SprintCreatePostModel sprintData)
        {
            var errores = new List<string>();

            if (string.IsNullOrWhiteSpace(sprintData.Name))
                errores.Add("El nombre del sprint es requerido.");
            if (sprintData.ProjectId <= 0)
                errores.Add("ID de proyecto inválido.");

            if (errores.Any())
                return (false, null, errores);

            using (var db = new g03_databaseEntities())
            {
                var nuevoSprint = new tbScrumSprints
                {
                    idProyecto = sprintData.ProjectId,
                    nombreSprint = sprintData.Name,
                    fechaInicio = sprintData.Start_Date,
                    fechaFin = sprintData.End_Date
                };

                db.tbScrumSprints.Add(nuevoSprint);
                db.SaveChanges();

                return (true, nuevoSprint, null);
            }
        }

        public List<object> ObtenerBacklogPorProyecto(int idProyecto)
        {
            using (var db = new g03_databaseEntities())
            {
                return db.tbScrumBacklog
                    .Where(b => b.idProyecto == idProyecto)
                    .OrderBy(b => b.prioridad)
                    .Select(b => new
                    {
                        id = b.idBacklog,
                        project_id = b.idProyecto,
                        description = b.descripcionBacklog,
                        priority = b.prioridad
                    })
                    .ToList<object>();
            }
        }

        public (bool success, tbScrumBacklog item, List<string> errors) CrearBacklogItem(BacklogItemCreatePostModel itemData)
        {
            var errores = new List<string>();

            if (string.IsNullOrWhiteSpace(itemData.Description))
                errores.Add("La descripción del item es requerida.");
            if (itemData.ProjectId <= 0)
                errores.Add("ID de proyecto inválido.");

            if (errores.Any())
                return (false, null, errores);

            using (var db = new g03_databaseEntities())
            {
                var nuevoItem = new tbScrumBacklog
                {
                    idProyecto = itemData.ProjectId,
                    descripcionBacklog = itemData.Description,
                    prioridad = itemData.Priority
                };

                db.tbScrumBacklog.Add(nuevoItem);
                db.SaveChanges();

                return (true, nuevoItem, null);
            }
        }
        public bool ActualizarEstadoVisualBacklog(int backlogId, int? targetSprintId, string targetVisualStatus)
        {
            using (var db = new g03_databaseEntities())
            {
                var item = db.tbScrumBacklog.Find(backlogId);
                if (item == null) return false;

                // Si lo necesitas, puedes agregar lógica adicional para cambiar estados visuales

                System.Diagnostics.Debug.WriteLine($"Item {backlogId} visualmente movido a Sprint {targetSprintId}, Estado {targetVisualStatus}. No hay persistencia directa en tbScrumBacklog.");

                return true;
            }
        }

        public List<object> ObtenerDailiesPorSprint(int sprintId)
        {
            using (var db = new g03_databaseEntities())
            {
                return db.tbScrumDaily
                    .Where(d => d.idSprint == sprintId)
                    .OrderByDescending(d => d.fechaDaily)
                    .Select(d => new
                    {
                        id = d.idDaily,
                        sprint_id = d.idSprint,
                        date = d.fechaDaily,
                        observations = d.observaciones
                    })
                    .ToList<object>();
            }
        }

        public (bool success, tbScrumDaily daily, List<string> errors) CrearDaily(DailyCreatePostModel dailyData)
        {
            var errores = new List<string>();

            using (var db = new g03_databaseEntities()) // Aseguramos que db se declare aquí
            {
                var sprint = db.tbScrumSprints
                    .FirstOrDefault(s => s.idSprint == dailyData.SprintId && s.idProyecto == dailyData.ProjectId);
                if (sprint == null)
                {
                    errores.Add("Sprint no encontrado o no pertenece al proyecto actual.");
                }

                if (dailyData.Date == DateTime.MinValue)
                {
                    errores.Add("La fecha del daily es requerida.");
                }

                if (errores.Any())
                {
                    return (false, null, errores);
                }

                var nuevoDaily = new tbScrumDaily
                {
                    idSprint = dailyData.SprintId,
                    fechaDaily = dailyData.Date,
                    observaciones = dailyData.Observations
                };
                db.tbScrumDaily.Add(nuevoDaily);
                db.SaveChanges();

                if (dailyData.BacklogUpdates != null)
                {
                    foreach (var bu in dailyData.BacklogUpdates.Where(b => b.BacklogId > 0 && b.UserId > 0))
                    {
                        var backlogItemExistsInProject = db.tbScrumBacklog
                            .Any(bi => bi.idBacklog == bu.BacklogId && bi.idProyecto == dailyData.ProjectId);
                        if (!backlogItemExistsInProject) continue;

                        db.tbScrumDailyBacklog.Add(new tbScrumDailyBacklog
                        {
                            idDaily = nuevoDaily.idDaily,
                            idBacklog = bu.BacklogId,
                            idUsuario = bu.UserId,
                            comentarioActividad = bu.Comment
                        });
                    }
                    db.SaveChanges();
                }

                return (true, nuevoDaily, null);
            }
        }

        public object ObtenerDetallesDaily(int dailyId, int projectId)
        {
            using (var db = new g03_databaseEntities())
            {
                var daily = db.tbScrumDaily
                    .Include(d => d.tbScrumSprints)
                    .Include(d => d.tbScrumDailyBacklog.Select(sdb => sdb.tbScrumBacklog))
                    .Include(d => d.tbScrumDailyBacklog.Select(sdb => sdb.tbUsuarios))
                    .FirstOrDefault(d => d.idDaily == dailyId && d.tbScrumSprints.idProyecto == projectId);

                if (daily == null)
                {
                    return new { success = false, message = "Daily no encontrado para este proyecto." };
                }

                var result = new
                {
                    id = daily.idDaily,
                    sprint_id = daily.idSprint,
                    date = daily.fechaDaily.ToString("yyyy-MM-dd"),
                    observations = daily.observaciones,
                    backlog_entries = daily.tbScrumDailyBacklog.Select(sdb => new
                    {
                        backlog_id = sdb.idBacklog,
                        backlog_description = sdb.tbScrumBacklog?.descripcionBacklog,
                        user_id = sdb.idUsuario,
                        user_name = sdb.tbUsuarios?.nombreUsuario,
                        comment = sdb.comentarioActividad
                    }).ToList()
                };

                return result;
            }
        }

        public (bool success, tbProyectos project, string errorMessage) ObtenerProyectoConEquipo(int projectId)
        {
            using (var db = new g03_databaseEntities())
            {
                var project = db.tbProyectos
                    .Include(p => p.tbProyectoUsuarios.Select(pu => pu.tbUsuarios))
                    .Include(p => p.tbProyectoUsuarios.Select(pu => pu.tbRoles))
                    .FirstOrDefault(p => p.idProyecto == projectId && p.idMetodologia == 1);

                if (project == null)
                {
                    return (false, null, "Proyecto SCRUM no encontrado o no válido.");
                }

                return (true, project, null);
            }
        }
    }
}