using G03_GestionDeCambios.Models; // Reemplaza con tu namespace de modelos de BD si es diferente
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System;

namespace G03_GestionDeCambios.Service
{
    public class SolicitudService
    {
        private readonly BD_GestionDeCambiosEntities _context;

        public SolicitudService()
        {
            _context = new BD_GestionDeCambiosEntities();
        }

        // Obtiene las solicitudes donde el usuario es solicitante o parte del proyecto.
        public List<SolicitudListadoViewModel> GetSolicitudesParaUsuario(int idUsuario)
        {
            // Primero, obtenemos los IDs de los proyectos en los que participa el usuario.
            var proyectosUsuario = _context.tbProyectoUsuario
                                           .Where(pu => pu.idUsuario == idUsuario)
                                           .Select(pu => pu.idProyecto)
                                           .ToList();

            // Ahora, buscamos solicitudes de esos proyectos.
            var solicitudes = _context.tbSolicitudesCambio
                .Where(s => proyectosUsuario.Contains(s.idProyecto))
                .Select(s => new SolicitudListadoViewModel
                {
                    IdSolicitudCambio = s.idSolicitudCambio,
                    // Formateamos el código para que sea más legible
                    CodigoSolicitud = s.codigoDocumentoSolicitd + "-" + s.idSolicitudCambio,
                    NombreProyecto = s.tbProyectos.nombre,
                    Objetivo = s.objetivoSolicitud,
                    FechaSolicitud = s.fechaSolicitud.Value,
                    Estado = s.estadoSolicitud,
                    PasoActualProceso = s.pasoActualProceso
                })
                .OrderByDescending(s => s.FechaSolicitud)
                .ToList();

            return solicitudes;
        }

        // Crea una nueva solicitud de cambio.
        public void CrearSolicitud(SolicitudCreacionViewModel model, int idUsuarioSolicitante)
        {
            var nuevaSolicitud = new tbSolicitudesCambio
            {
                // idSolicitudCambio es autoincremental, no se asigna aquí.
                codigoDocumentoSolicitd = "R-GCSW001", // Valor por defecto del PDF
                fechaSolicitud = DateTime.Now,
                idProyecto = model.IdProyecto.Value,
                idUsuarioSolicitante = idUsuarioSolicitante,
                objetivoSolicitud = model.ObjetivoSolicitud,
                descripcionSolicitud = model.DescripcionSolicitud,
                idElementoAfectado = model.IdElementoAfectado.Value,
                impactoEstimado = model.ImpactoEstimado,
                esfuerzoEstimado = model.EsfuerzoEstimado,
                estadoSolicitud = "Propuesto", // Estado inicial según el PDF y la BD
                pasoActualProceso = 1 // Estado inicial del proceso
            };

            _context.tbSolicitudesCambio.Add(nuevaSolicitud);
            _context.SaveChanges();
        }

        // Obtiene los elementos de configuración de un proyecto para un DropDownList
        public IEnumerable<SelectListItem> GetElementosConfiguracionPorProyecto(int idProyecto)
        {
            var elementos = _context.tbProyectoElemento
                .Where(pe => pe.idProyecto == idProyecto)
                .Select(pe => new SelectListItem
                {
                    Value = pe.idProyectoElemento.ToString(),
                    Text = pe.tbElementos.nombre + " (Ciclo: " + pe.codCiclo + ")"
                })
                .ToList();

            return new SelectList(elementos, "Value", "Text");
        }

        public int ObtenerPasoActualProceso(int idSolicitud)
        {
            return _context.tbSolicitudesCambio
                .Where(sc => sc.idSolicitudCambio == idSolicitud)
                .Select(sc => sc.pasoActualProceso)
                .FirstOrDefault();
        }
    }
}