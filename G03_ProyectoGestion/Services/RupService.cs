using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using G03_ProyectoGestion.Models;

namespace G03_ProyectoGestion.Services
{
    public class RupService
    {
        // ELIMINADO: ObtenerIteracionesPorFase
        // ELIMINADO: CrearIteracion

        public bool ActualizarFaseDelProyecto(int projectId, int phaseId)
        {
            using (var db = new g03_databaseEntities())
            {
                var project = db.tbProyectos.FirstOrDefault(p => p.idProyecto == projectId && p.idMetodologia == 2); // 2 es RUP
                if (project == null) return false;

                var phaseExists = db.tbRupFases.Any(f => f.idFase == phaseId);
                if (!phaseExists) return false; // O manejar como error

                // Verificar si la fase existe en tbRupFases (si no, podrías necesitar crearla o devolver error)
                // Por ahora, se asume que phaseId es un ID válido de tbRupFases
                project.idFase = phaseId;
                db.SaveChanges();
                return true;
            }
        }

        public List<object> ObtenerActividadesPorFase(int projectId, int phaseId) // Cambiado de Iteracion a Fase
        {
            using (var db = new g03_databaseEntities())
            {
                var actividades = db.tbRupActividades
                    .Where(a => a.idProyecto == projectId && a.idFase == phaseId) // Cambiado
                    .Select(a => new
                    {
                        id = a.idActividad,
                        project_id = a.idProyecto, // Añadido para consistencia
                        phase_id = a.idFase,       // Añadido para consistencia
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
                    a.project_id,
                    a.phase_id,
                    a.description,
                    a.assigned_role,
                    a.status,
                    due_date = a.due_date?.ToString("yyyy-MM-dd"),
                    a.assigned_users
                }).ToList<object>();

                return result;
            }
        }

        public object CrearActividad(ActivityCreatePostModel activityData) // El modelo debe actualizarse
        {
            using (var db = new g03_databaseEntities())
            {
                if (activityData.AssignedUserIds == null || !activityData.AssignedUserIds.Any())
                {
                    return new { success = false, message = "Debe asignar al menos un usuario a la actividad." };
                }

                // Validar que el proyecto y la fase existen
                var projectPhaseExists = db.tbProyectos.Any(p => p.idProyecto == activityData.ProjectId && p.idFase == activityData.PhaseId);
                if (!projectPhaseExists) // Esta validación asume que la fase en el proyecto es la correcta.
                                         // O podrías validar activityData.PhaseId contra tbRupFases
                {
                    // Podrías verificar si la fase existe en tbRupFases de forma independiente
                    var phaseExistsInTable = db.tbRupFases.Any(f => f.idFase == activityData.PhaseId);
                    if (!phaseExistsInTable)
                    {
                        return new { success = false, message = "La fase especificada no existe." };
                    }
                    var projectExists = db.tbProyectos.Any(p => p.idProyecto == activityData.ProjectId);
                    if (!projectExists)
                    {
                        return new { success = false, message = "El proyecto especificado no existe." };
                    }
                    // Si ambos existen pero no coinciden con el proyecto.idFase, es decisión de negocio si permitirlo
                    // por ahora, asumimos que la actividad se crea para el proyecto y la fase dada.
                }


                foreach (var userId in activityData.AssignedUserIds)
                {
                    var userInProjectWithRole = db.tbProyectoUsuarios
                        .Any(pu => pu.idProyecto == activityData.ProjectId &&
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
                    idProyecto = activityData.ProjectId, // Cambiado
                    idFase = activityData.PhaseId,       // Cambiado
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
                    project_id = newDbActivity.idProyecto,
                    phase_id = newDbActivity.idFase,
                    description = newDbActivity.descripcion,
                    assigned_role = newDbActivity.idRol,
                    assigned_users = assignedUsers,
                    status = newDbActivity.estado,
                    due_date = newDbActivity.fechaLimite?.ToString("yyyy-MM-dd")
                };
            }
        }

        public List<object> ObtenerDocumentosPorFase(int projectId, int phaseId) // Cambiado de Iteracion a Fase
        {
            using (var db = new g03_databaseEntities())
            {
                var documentos = db.tbRupDocumentos
                    .Where(d => d.idProyecto == projectId && d.idFase == phaseId) // Cambiado
                    .Include(d => d.tbRupTiposDocumento) // Asegúrate que esta navegación existe y es correcta
                    .Select(d => new
                    {
                        id = d.idDocumento,
                        project_id = d.idProyecto, // Añadido
                        phase_id = d.idFase,       // Añadido
                        type = d.tbRupTiposDocumento.clave, // Asegúrate que tbRupTiposDocumento está bien referenciado
                        file_name = d.nombreArchivo,
                        version = d.Version,
                        status = d.Estado,
                        uploaded_at = d.FechaSubida
                    })
                    .ToList();

                var result = documentos.Select(d => new
                {
                    d.id,
                    d.project_id,
                    d.phase_id,
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