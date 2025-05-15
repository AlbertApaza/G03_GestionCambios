using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using G03_ProyectoGestion.Models;
using static G03_ProyectoGestion.Controllers.RUPController;

namespace G03_ProyectoGestion.Services
{
    public class RupService
    {

        public List<object> ObtenerIteracionesPorFase(int projectId, int phaseId)
        {
            using (var db = new g03_databaseEntities())
            {
                var iterations = db.tbRupIteraciones
                    .Where(i => i.idProyecto == projectId && i.idFase == phaseId)
                    .Select(i => new
                    {
                        id = i.idIteracion,
                        project_id = i.idProyecto,
                        phase_id = i.idFase,
                        name = i.nombre,
                        objective = i.objetivo,
                        start_date = i.fechaInicio,
                        end_date = i.fechaFin,
                        status = i.Estado
                    })
                    .ToList();

                var result = iterations.Select(i => new
                {
                    i.id,
                    i.project_id,
                    i.phase_id,
                    i.name,
                    i.objective,
                    start_date = i.start_date?.ToString("yyyy-MM-dd"),
                    end_date = i.end_date?.ToString("yyyy-MM-dd"),
                    i.status
                }).ToList<object>();

                return result;
            }
        }

        public bool ActualizarFaseDelProyecto(int projectId, int phaseId)
        {
            using (var db = new g03_databaseEntities())
            {
                var project = db.tbProyectos.FirstOrDefault(p => p.idProyecto == projectId && p.idMetodologia == 2);
                if (project == null) return false;

                var phaseExists = db.tbRupFases.Any(f => f.idFase == phaseId);
                if (!phaseExists) return false;

                project.idFase = phaseId;
                db.SaveChanges();
                return true;
            }
        }

        public object CrearIteracion(IterationCreatePostModel iterationData)
        {
            using (var db = new g03_databaseEntities())
            {
                var nuevaIteracion = new tbRupIteraciones
                {
                    idProyecto = iterationData.ProjectId,
                    idFase = iterationData.PhaseId,
                    nombre = iterationData.Name,
                    objetivo = iterationData.Objective,
                    fechaInicio = iterationData.Start_Date,
                    fechaFin = iterationData.End_Date,
                    Estado = "Planificada"
                };

                db.tbRupIteraciones.Add(nuevaIteracion);
                db.SaveChanges();

                return new
                {
                    success = true,
                    id = nuevaIteracion.idIteracion,
                    project_id = nuevaIteracion.idProyecto,
                    phase_id = nuevaIteracion.idFase,
                    name = nuevaIteracion.nombre,
                    objective = nuevaIteracion.objetivo,
                    start_date = nuevaIteracion.fechaInicio?.ToString("yyyy-MM-dd"),
                    end_date = nuevaIteracion.fechaFin?.ToString("yyyy-MM-dd"),
                    status = nuevaIteracion.Estado
                };
            }
        }

        public List<object> ObtenerActividadesPorIteracion(int iterationId)
        {
            using (var db = new g03_databaseEntities())
            {
                var actividades = db.tbRupActividades
                    .Where(a => a.idIteracion == iterationId)
                    .Select(a => new
                    {
                        id = a.idActividad,
                        iteration_id = a.idIteracion,
                        description = a.descripcion,
                        assigned_role = a.idRol,
                        status = a.estado,
                        due_date = a.fechaLimite,
                        assigned_users = db.tbRupActividadAsignaciones
                                           .Where(aa => aa.idActividad == a.idActividad)
                                           .Select(aa => new
                                           {
                                               id = aa.tbUsuarios.idUsuario,
                                               name = aa.tbUsuarios.nombreUsuario
                                           }).ToList()
                    })
                    .ToList();

                var result = actividades.Select(a => new
                {
                    a.id,
                    a.iteration_id,
                    a.description,
                    a.assigned_role,
                    a.status,
                    due_date = a.due_date?.ToString("yyyy-MM-dd"),
                    a.assigned_users
                }).ToList<object>();

                return result;
            }
        }
        public object CrearActividad(ActivityCreatePostModel activityData)
        {
            using (var db = new g03_databaseEntities())
            {
                if (activityData.AssignedUserIds == null || !activityData.AssignedUserIds.Any())
                {
                    return new { success = false, message = "Debe asignar al menos un usuario a la actividad." };
                }

                var iteration = db.tbRupIteraciones.Find(activityData.IterationId);
                if (iteration == null)
                {
                    return new { success = false, message = "Iteración no encontrada." };
                }

                int projectId = iteration.idProyecto;

                foreach (var userId in activityData.AssignedUserIds)
                {
                    var userInProjectWithRole = db.tbProyectoUsuarios
                        .Any(pu => pu.idProyecto == projectId &&
                                   pu.idUsuario == userId &&
                                   pu.idRol == activityData.ContextRoleId);

                    if (!userInProjectWithRole)
                    {
                        var user = db.tbUsuarios.Find(userId);
                        var role = db.tbRoles.Find(activityData.ContextRoleId);
                        return new
                        {
                            success = false,
                            message = $"El usuario '{user?.nombreUsuario ?? "Desconocido"}' no tiene el rol '{role?.nombreRol ?? "Desconocido"}' en este proyecto o no existe."
                        };
                    }
                }

                var newDbActivity = new tbRupActividades
                {
                    idIteracion = activityData.IterationId,
                    descripcion = activityData.Description,
                    idRol = activityData.ContextRoleId,
                    estado = string.IsNullOrEmpty(activityData.Status) ? "Pendiente" : activityData.Status,
                    fechaLimite = activityData.Due_Date
                };

                db.tbRupActividades.Add(newDbActivity);
                db.SaveChanges();

                foreach (var userId in activityData.AssignedUserIds)
                {
                    db.tbRupActividadAsignaciones.Add(new tbRupActividadAsignaciones
                    {
                        idActividad = newDbActivity.idActividad,
                        idUsuario = userId
                    });
                }

                db.SaveChanges();

                var assignedUsers = db.tbUsuarios
                    .Where(u => activityData.AssignedUserIds.Contains(u.idUsuario))
                    .Select(u => new { id = u.idUsuario, name = u.nombreUsuario })
                    .ToList();

                return new
                {
                    success = true,
                    id = newDbActivity.idActividad,
                    iteration_id = newDbActivity.idIteracion,
                    description = newDbActivity.descripcion,
                    assigned_role = newDbActivity.idRol,
                    assigned_users = assignedUsers,
                    status = newDbActivity.estado,
                    due_date = newDbActivity.fechaLimite?.ToString("yyyy-MM-dd")
                };
            }
        }
        public List<object> ObtenerDocumentosPorIteracion(int iterationId)
        {
            using (var db = new g03_databaseEntities())
            {
                var documentos = db.tbRupDocumentos
                    .Where(d => d.idIteracion == iterationId)
                    .Include(d => d.tbRupTiposDocumento)
                    .Select(d => new
                    {
                        id = d.idDocumento,
                        iteration_id = d.idIteracion,
                        type = d.tbRupTiposDocumento.clave,
                        file_name = d.nombreArchivo,
                        version = d.Version,
                        status = d.Estado,
                        uploaded_at = d.FechaSubida
                    })
                    .ToList();

                var result = documentos.Select(d => new
                {
                    d.id,
                    d.iteration_id,
                    d.type,
                    d.file_name,
                    d.version,
                    d.status,
                    uploaded_at = d.uploaded_at.ToString("o") // ISO 8601
                }).ToList<object>();

                return result;
            }
        }

        public List<object> ObtenerUsuariosPorRolEnProyecto(int projectId, int roleId)
        {
            using (var db = new g03_databaseEntities())
            {
                var usuarios = db.tbProyectoUsuarios
                    .Where(pu => pu.idProyecto == projectId && pu.idRol == roleId)
                    .Select(pu => new
                    {
                        id = pu.tbUsuarios.idUsuario,
                        name = pu.tbUsuarios.nombreUsuario
                    })
                    .ToList<object>();

                return usuarios;
            }
        }
    }
}