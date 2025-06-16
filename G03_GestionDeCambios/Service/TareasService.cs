using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using G03_GestionDeCambios.Models; 
using G03_GestionDeCambios.ViewModels.TareasViewModels;
using System.Diagnostics; 

namespace G03_GestionDeCambios.Service
{
    public class TareasService
    {
        private readonly BD_GestionDeCambiosEntities _dbContext;

        public TareasService()
        {
            _dbContext = new BD_GestionDeCambiosEntities();
        }

        public TareasService(BD_GestionDeCambiosEntities context) 
        {
            _dbContext = context;
        }

        public tbProyectos GetProyectoById(int idProyecto)
        {
            return _dbContext.tbProyectos.FirstOrDefault(p => p.idProyecto == idProyecto);
        }

        public List<ProyectoElementoViewModel> GetElementosConfiguracionParaAsignacion(int idProyecto)
        {
            var proyecto = _dbContext.tbProyectos
                                     .Include(p => p.tbCiclos) // Para codCicloActual
                                     .FirstOrDefault(p => p.idProyecto == idProyecto);

            if (proyecto == null || proyecto.codCicloActual == null)
            {
                return new List<ProyectoElementoViewModel>(); // O lanzar excepción
            }

            return _dbContext.tbProyectoElemento
                .Where(pe => pe.idProyecto == idProyecto && pe.codCiclo == proyecto.codCicloActual && pe.estado != "Finalizado") 
                .Include(pe => pe.tbElementos) 
                .Include(pe => pe.tbRoles) 
                .Select(pe => new ProyectoElementoViewModel
                {
                    IdProyectoElemento = pe.idProyectoElemento,
                    NombreElemento = pe.tbElementos.nombre,
                    FechaInicio = pe.fechaInicio.Value, 
                    FechaFin = pe.fechaFin,
                    EstadoElemento = pe.estado,
                    NombreRolAsignadoAlElemento = pe.tbRoles != null ? pe.tbRoles.nombre : "Rol no definido",
                    IdRolAsignadoAlElemento = pe.idRol
                }).ToList();
        }

        public tbProyectoElemento GetProyectoElementoById(int idProyectoElemento)
        {
            return _dbContext.tbProyectoElemento
                             .Include(pe => pe.tbProyectos) 
                             .Include(pe => pe.tbRoles)     
                             .FirstOrDefault(pe => pe.idProyectoElemento == idProyectoElemento);
        }


        public List<UsuarioDisponibleViewModel> GetUsuariosDisponiblesParaElemento(int idProyectoElemento)
        {
            var elementoSeleccionado = _dbContext.tbProyectoElemento
                                            .Include(pe => pe.tbProyectos) // Para el idProyecto
                                            .FirstOrDefault(pe => pe.idProyectoElemento == idProyectoElemento);

            if (elementoSeleccionado == null || !elementoSeleccionado.idRol.HasValue || !elementoSeleccionado.fechaInicio.HasValue)
            {
                return new List<UsuarioDisponibleViewModel>();
            }

            int idProyecto = elementoSeleccionado.idProyecto.Value;
            int idRolRequerido = elementoSeleccionado.idRol.Value;
            DateTime inicioElementoActual = elementoSeleccionado.fechaInicio.Value;
            DateTime? finElementoActual = elementoSeleccionado.fechaFin;

            var usuariosConRolEnProyecto = _dbContext.tbProyectoUsuario
                .Where(pu => pu.idProyecto == idProyecto && pu.idRol == idRolRequerido && pu.tbUsuarios.estado == 1)
                .Select(pu => pu.tbUsuarios)
                .Distinct()
                .ToList();

            var usuariosDisponibles = new List<UsuarioDisponibleViewModel>();

            foreach (var usuario in usuariosConRolEnProyecto)
            {
                var tareasDelUsuario = _dbContext.tbTareas
                    .Where(t => t.idUsuario == usuario.idUsuario && t.estado != "Finalizado") 
                    .Include(t => t.tbProyectoElemento) 
                    .ToList();

                bool estaOcupadoEnFechas = false;
                if (tareasDelUsuario.Any()) 
                {
                    foreach (var tareaExistente in tareasDelUsuario)
                    {
                        var peExistente = tareaExistente.tbProyectoElemento;
                        if (peExistente != null && peExistente.fechaInicio.HasValue)
                        {
                            DateTime inicioExistente = peExistente.fechaInicio.Value;
                            DateTime? finExistente = peExistente.fechaFin;

                            bool haySolapamiento =
                                (inicioElementoActual < (finExistente ?? DateTime.MaxValue)) &&
                                ((finElementoActual ?? DateTime.MaxValue) > inicioExistente);

                            if (haySolapamiento)
                            {
                                estaOcupadoEnFechas = true;
                                break; 
                            }
                        }
                    }
                }


                if (!estaOcupadoEnFechas)
                {
                    usuariosDisponibles.Add(new UsuarioDisponibleViewModel
                    {
                        IdUsuario = usuario.idUsuario,
                        NombreCompletoUsuario = usuario.nombre + " " + usuario.apellido
                    });
                }
            }

            return usuariosDisponibles.OrderBy(u => u.NombreCompletoUsuario).ToList();
        }

        public void CrearNuevaTarea(int idProyectoElemento, int idUsuario, string nombreTarea, string descripcionTarea)
        {
            if (string.IsNullOrWhiteSpace(nombreTarea))
            {
                throw new ArgumentException("El nombre de la tarea no puede estar vacío.", nameof(nombreTarea));
            }

            var tarea = new tbTareas
            {
                idProyectoElemento = idProyectoElemento,
                idUsuario = idUsuario,
                nombre = nombreTarea,
                descripcion = descripcionTarea,
                estado = "Pendiente" // Estado inicial por defecto
            };

            _dbContext.tbTareas.Add(tarea);
            _dbContext.SaveChanges();
        }

        public string GetNombreProyecto(int idProyecto)
        {
            return _dbContext.tbProyectos
                             .Where(p => p.idProyecto == idProyecto)
                             .Select(p => p.nombre)
                             .FirstOrDefault();
        }

        public string GetNombreElemento(int idProyectoElemento)
        {
            return _dbContext.tbProyectoElemento
                             .Where(pe => pe.idProyectoElemento == idProyectoElemento)
                             .Select(pe => pe.tbElementos.nombre) 
                             .FirstOrDefault();
        }
        public string GetNombreUsuario(int idUsuario)
        {
            return _dbContext.tbUsuarios
                             .Where(u => u.idUsuario == idUsuario)
                             .Select(u => u.nombre + " " + u.apellido)
                             .FirstOrDefault();
        }

        public List<TareaDetalleViewModel> GetTareasDetalladasPorProyectoYCiclo(int idProyecto)
        {
            var proyecto = _dbContext.tbProyectos
                                     .Include(p => p.tbCiclos)
                                     .FirstOrDefault(p => p.idProyecto == idProyecto);

            if (proyecto == null || string.IsNullOrEmpty(proyecto.codCicloActual))
            {
                return new List<TareaDetalleViewModel>();
            }

            var codCicloActual = proyecto.codCicloActual;

            var tareas = _dbContext.tbTareas
                .Where(t => t.tbProyectoElemento.idProyecto == idProyecto && t.tbProyectoElemento.codCiclo == codCicloActual)
                .Include(t => t.tbProyectoElemento.tbElementos) 
                .Include(t => t.tbProyectoElemento.tbRoles)   
                .Include(t => t.tbUsuarios)                   
                .Select(t => new TareaDetalleViewModel
                {
                    IdTarea = t.idTareas,
                    NombreTarea = t.nombre,
                    DescripcionTarea = t.descripcion,
                    EstadoTarea = t.estado,

                    IdUsuarioAsignado = t.idUsuario.Value, 
                    IdProyectoElemento = t.idProyectoElemento.Value, 

                    NombreElementoAsociado = t.tbProyectoElemento.tbElementos.nombre,
                    FechaInicioElemento = t.tbProyectoElemento.fechaInicio,
                    FechaFinElemento = t.tbProyectoElemento.fechaFin,
                    EstadoElemento = t.tbProyectoElemento.estado,

                    NombreUsuarioAsignado = t.tbUsuarios.nombre + " " + t.tbUsuarios.apellido,
                    RolUsuarioEnElemento = t.tbProyectoElemento.tbRoles != null ? t.tbProyectoElemento.tbRoles.nombre : "N/A"
                })
                .OrderByDescending(t => t.IdTarea) // O por la fecha 
                .ToList();

            return tareas;
        }

        public List<TareaUsuarioViewModel> GetTareasParaUsuarioEnProyecto(int idUsuario, int idProyecto)
        {
            var proyecto = _dbContext.tbProyectos
                                     .Include(p => p.tbCiclos) // Para obtener codCicloActual y su nombre
                                     .FirstOrDefault(p => p.idProyecto == idProyecto);

            if (proyecto == null)
            {
                return new List<TareaUsuarioViewModel>();
            }

            string codCicloActualProyecto = proyecto.codCicloActual;

            var tareasUsuario = _dbContext.tbTareas
                .Where(t => t.idUsuario == idUsuario && t.tbProyectoElemento.idProyecto == idProyecto)
                .Include(t => t.tbProyectoElemento.tbElementos) 
                .Include(t => t.tbProyectoElemento.tbCiclos)   
                .OrderByDescending(t => t.tbProyectoElemento.fechaInicio) // O por idTarea
                .ThenBy(t => t.nombre)
                .Select(t => new TareaUsuarioViewModel
                {
                    IdTarea = t.idTareas,
                    NombreTarea = t.nombre,
                    DescripcionTarea = t.descripcion,
                    EstadoTarea = t.estado,
                    NombreElementoAsociado = t.tbProyectoElemento.tbElementos.nombre,
                    CicloDelElemento = t.tbProyectoElemento.codCiclo, 
                    FechaInicioElemento = t.tbProyectoElemento.fechaInicio,
                    FechaFinElemento = t.tbProyectoElemento.fechaFin,
                })
                .ToList(); 

            foreach (var tareaVm in tareasUsuario)
            {
                tareaVm.EsDelCicloActual = tareaVm.CicloDelElemento == codCicloActualProyecto;
                tareaVm.PuedeEditarEstado = tareaVm.EsDelCicloActual && tareaVm.EstadoTarea != "Finalizado";
            }

            return tareasUsuario
                .OrderByDescending(t => t.EsDelCicloActual && t.EstadoTarea != "Finalizado") // Prioridad 1: Activas y editables
                .ThenByDescending(t => t.EsDelCicloActual) // Prioridad 2: Del ciclo actual (ya finalizadas)
                .ThenBy(t => t.EstadoTarea == "Finalizado") // Prioridad 3: Finalizadas después de las no finalizadas
                .ThenByDescending(t => t.FechaInicioElemento ?? DateTime.MinValue) // Luego por fecha del elemento
                .ThenBy(t => t.NombreTarea)
                .ToList();
        }

        public bool ActualizarEstadoTarea(int idTarea, string nuevoEstado, int idUsuarioActual)
        {
            var tarea = _dbContext.tbTareas.FirstOrDefault(t => t.idTareas == idTarea);

            if (tarea == null)
            {
                Debug.WriteLine($"Service: Tarea con ID {idTarea} no encontrada.");
                return false; // Tarea no encontrada
            }

            if (tarea.idUsuario != idUsuarioActual)
            {
                Debug.WriteLine($"Service: Usuario {idUsuarioActual} no autorizado para cambiar tarea {idTarea} (pertenece a {tarea.idUsuario}).");
                return false; // No es el usuario asignado
            }

            if (tarea.estado == "Finalizado")
            {
                Debug.WriteLine($"Service: Tarea {idTarea} ya está finalizada. No se puede cambiar estado.");
                return false; // Ya está finalizada, no se puede cambiar
            }

            List<string> estadosPermitidos = new List<string> { "Pendiente", "En Proceso", "Finalizado" };
            if (!estadosPermitidos.Contains(nuevoEstado))
            {
                Debug.WriteLine($"Service: Estado '{nuevoEstado}' no es válido para tarea {idTarea}.");
                return false; // Estado no válido
            }

            var proyectoElemento = _dbContext.tbProyectoElemento
                                        .Include(pe => pe.tbProyectos)
                                        .FirstOrDefault(pe => pe.idProyectoElemento == tarea.idProyectoElemento);

            if (proyectoElemento == null || proyectoElemento.tbProyectos == null)
            {
                Debug.WriteLine($"Service: No se pudo encontrar el proyecto elemento o el proyecto asociado para la tarea {idTarea}.");
                return false; // No se pudo verificar el ciclo
            }

            if (proyectoElemento.codCiclo != proyectoElemento.tbProyectos.codCicloActual)
            {
                Debug.WriteLine($"Service: Tarea {idTarea} no pertenece al ciclo actual del proyecto. No se puede cambiar estado.");
                return false; // No es del ciclo actual
            }


            tarea.estado = nuevoEstado;
            _dbContext.SaveChanges();
            Debug.WriteLine($"Service: Tarea {idTarea} actualizada a estado '{nuevoEstado}' exitosamente.");
            return true;
        }

        public bool ActualizarTareaDetallada(int idTarea, string nombre, string descripcion, string estado, int idUsuarioQueActualiza, out string mensajeError)
        {
            mensajeError = string.Empty;
            var tarea = _dbContext.tbTareas
                                  .Include(t => t.tbProyectoElemento) // Necesario si quieres verificar algo del elemento
                                  .FirstOrDefault(t => t.idTareas == idTarea);

            if (tarea == null)
            {
                Debug.WriteLine($"Service: Tarea con ID {idTarea} no encontrada para actualizar (admin).");
                mensajeError = "Tarea no encontrada.";
                return false;
            }


            List<string> estadosPermitidos = new List<string> { "Pendiente", "En Proceso", "Finalizado" };
            if (!estadosPermitidos.Contains(estado))
            {
                Debug.WriteLine($"Service: Estado '{estado}' no es válido para tarea {idTarea} (admin).");
                mensajeError = $"El estado '{estado}' no es válido.";
                return false;
            }


            tarea.nombre = nombre;
            tarea.descripcion = descripcion;
            tarea.estado = estado;

            try
            {
                _dbContext.SaveChanges();
                Debug.WriteLine($"Service (Admin): Tarea {idTarea} actualizada exitosamente (sin cambiar usuario).");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Service (Admin): Error al guardar cambios para tarea {idTarea}: {ex.ToString()}");
                mensajeError = "Error al guardar los cambios en la base de datos.";
                return false;
            }
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }
    }
}