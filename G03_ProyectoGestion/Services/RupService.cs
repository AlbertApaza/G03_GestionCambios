using System;
using System.Collections.Generic;
using System.Data.Entity; // Necesario si usas Include y otras funciones de EF aquí
using System.Linq;
using System.Web;
using G03_ProyectoGestion.Models;
using G03_ProyectoGestion.ViewModels; // Asegúrate que tus modelos EF y ViewModels estén aquí o referenciados
namespace G03_ProyectoGestion.Services
{
    public class RupService
    {
        // Helper para obtener la clave JS del rol (duplicado del controlador para independencia del servicio)
        private string GetRoleJsKeyFromDb_Service(tbRoles role)
        {
            if (role == null) return $"unknown_role_{Guid.NewGuid().ToString().Substring(0, 4)}";
            return role.nombreRol.Trim().ToLowerInvariant()
            .Replace(" ", "_")
            .Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u")
            .Replace("ñ", "n");
        }
        public bool ActualizarFaseDelProyecto(int projectId, int phaseId)
        {
            using (var db = new g03_databaseEntities())
            {
                var project = db.tbProyectos.FirstOrDefault(p => p.idProyecto == projectId && p.idMetodologia == 2); // 2 es RUP
                if (project == null) return false;

                var phaseExists = db.tbRupFases.Any(f => f.idFase == phaseId);
                if (!phaseExists) return false;

                project.idFase = phaseId;
                db.SaveChanges();
                return true;
            }
        }

        // Este método es para la pestaña de Documentos/Alpine.js si usa su propio sistema de actividades.
        // El `ActivityCreatePostModel` es el que usabas originalmente con Alpine.
        // Ahora tbRupActividades tiene fechaInicio.
        public object CrearActividad(ActivityCreatePostModel activityData)
        {
            using (var db = new g03_databaseEntities())
            {
                if (activityData.AssignedUserIds == null || !activityData.AssignedUserIds.Any())
                {
                    return new { success = false, message = "Debe asignar al menos un usuario a la actividad." };
                }

                var projectPhaseExists = db.tbProyectos.Any(p => p.idProyecto == activityData.ProjectId && p.idFase == activityData.PhaseId);
                if (!projectPhaseExists)
                {
                    var phaseExistsInTable = db.tbRupFases.Any(f => f.idFase == activityData.PhaseId);
                    if (!phaseExistsInTable) return new { success = false, message = "La fase especificada no existe." };
                    var projectExists = db.tbProyectos.Any(p => p.idProyecto == activityData.ProjectId);
                    if (!projectExists) return new { success = false, message = "El proyecto especificado no existe." };
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
                        return new { success = false, message = $"El usuario '{user?.nombreUsuario ?? "Desconocido"}' no tiene el rol '{role?.nombreRol ?? "Desconocido"}' en este proyecto o no existe." };
                    }
                }

                var newDbActivity = new tbRupActividades
                {
                    // idActividad se autogenera
                    idProyecto = activityData.ProjectId,
                    idFase = activityData.PhaseId,
                    descripcion = activityData.Description,
                    idRol = activityData.ContextRoleId, // idRol es NOT NULL
                    estado = string.IsNullOrEmpty(activityData.Status) ? "Pendiente" : activityData.Status,
                    // Asumiendo que Due_Date en ActivityCreatePostModel ahora representa la fecha límite.
                    // Si necesitas fechaInicio también para estas actividades, ActivityCreatePostModel debe incluirla.
                    fechaInicio = activityData.Due_Date, // O una nueva propiedad StartDate en el modelo
                    fechaLimite = activityData.Due_Date
                };

                db.tbRupActividades.Add(newDbActivity);
                db.SaveChanges(); // Para obtener newDbActivity.idActividad

                foreach (var userId in activityData.AssignedUserIds)
                {
                    db.tbRupActividadAsignaciones.Add(new tbRupActividadAsignaciones
                    {
                        // idActividadAsignacion se autogenera
                        idActividad = newDbActivity.idActividad,
                        idUsuario = userId
                    });
                }
                db.SaveChanges();

                var assignedUsers = db.tbUsuarios
                    .Where(u => activityData.AssignedUserIds.Contains(u.idUsuario))
                    .Select(u => new { id = u.idUsuario, name = u.nombreUsuario }) // Coincide con el formato esperado por Alpine
                    .ToList();

                // Devolver un objeto que Alpine pueda usar para actualizar su lista local.
                // Este formato debe coincidir con lo que `fetchActivitiesForCurrentPhase` en Alpine espera.
                return new
                {
                    success = true,
                    id = newDbActivity.idActividad, // El ID de la BD
                    project_id = newDbActivity.idProyecto,
                    phase_id = newDbActivity.idFase,
                    description = newDbActivity.descripcion,
                    assigned_role = newDbActivity.idRol, // ID de BD del rol
                    assigned_users = assignedUsers, // Lista de {id, name}
                    status = newDbActivity.estado,
                    due_date = newDbActivity.fechaLimite?.ToString("yyyy-MM-dd") // Fecha límite
                                                                                 // Si Alpine también necesita fechaInicio, añádela aquí
                                                                                 // start_date = newDbActivity.fechaInicio?.ToString("yyyy-MM-dd")
                };
            }
        }

        public List<object> ObtenerDocumentosPorFase(int projectId, int phaseId)
        {
            using (var db = new g03_databaseEntities())
            {
                var documentos = db.tbRupDocumentos
                    .Where(d => d.idProyecto == projectId && d.idFase == phaseId)
                    .Include(d => d.tbRupTiposDocumento)
                    .Select(d => new
                    {
                        id = d.idDocumento,
                        project_id = d.idProyecto,
                        phase_id = d.idFase,
                        type = d.tbRupTiposDocumento.clave,
                        file_name = d.nombreArchivo,
                        version = d.Version,
                        status = d.Estado,
                        uploaded_at = d.FechaSubida
                    })
                    .ToList();

                // Formatear para el cliente (Alpine JS)
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
                    .Select(pu => new // Formato esperado por Alpine { id, name }
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