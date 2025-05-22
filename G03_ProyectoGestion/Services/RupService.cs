using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using G03_ProyectoGestion.Models;
using G03_ProyectoGestion.ViewModels; // Asegúrate que esta ruta es correcta

namespace G03_ProyectoGestion.Services
{
    public class RupService
    {
        // ELIMINADO: ObtenerIteracionesPorFase (ya estaba)
        // ELIMINADO: CrearIteracion (ya estaba)
        // ELIMINADO: ObtenerActividadesPorFase
        // ELIMINADO: CrearActividad
        // ELIMINADO: ObtenerUsuariosPorRolEnProyecto

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
    }
}