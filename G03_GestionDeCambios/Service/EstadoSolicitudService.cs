using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using G03_GestionDeCambios.Models;
using System.Data.Entity;

namespace G03_GestionDeCambios.Service
{
    public class EstadoSolicitudService
    {
        private readonly BD_GestionDeCambiosEntities _context;

        public EstadoSolicitudService()
        {
            _context = new BD_GestionDeCambiosEntities();
        }
        public int ObtenerPasoActualProceso(int idSolicitud)
        {
            return _context.tbSolicitudesCambio
                .Where(sc => sc.idSolicitudCambio == idSolicitud)
                .Select(sc => sc.pasoActualProceso)
                .FirstOrDefault();
        }
        private string ObtenerNombrePaso(int paso)
        {
            switch (paso)
            {
                case 1: return "Recepcion de Solicitud de Cambio";
                case 2: return "Análisis de Impacto";
                case 3: return "Aprobación por Comité/Líder de Proyecto";
                case 4: return "Asignacion de actividades";
                case 5: return "Desarrollo";
                case 6: return "Pruebas (QA)";
                case 7: return "Despliegue"; // NUEVO
                case 8: return "Cierre";     // NUEVO
                default: return "Paso Desconocido";
            }
        }

        public void SolicitudRecibida(int idSolicitud, int idUsuario)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // 1. Encontrar la solicitud específica
                    var solicitud = _context.tbSolicitudesCambio.FirstOrDefault(s => s.idSolicitudCambio == idSolicitud);

                    if (solicitud == null)
                    {
                        throw new Exception("La solicitud de cambio no fue encontrada.");
                    }

                    // 2. Condición principal: Ejecutar solo si no ha sido recibida/asignada previamente
                    if (solicitud.idUsuarioReceptor == null)
                    {
                        // 3. Actualizar los campos de la solicitud
                        solicitud.idUsuarioReceptor = idUsuario; // Asignar el usuario que la recibe
                        solicitud.fechaRecibida = DateTime.Now; // Marcar la fecha y hora de recepción

                        // 4. Crear el registro de auditoría en el historial
                        var historial = new tbSolicitudHistorial
                        {
                            idSolicitudCambio = idSolicitud,
                            pasoProceso = solicitud.pasoActualProceso, // La acción ocurre dentro del paso actual (debería ser el 1)
                            nombrePasoProceso = ObtenerNombrePaso(solicitud.pasoActualProceso),
                            fechaAccion = DateTime.Now,
                            idUsuarioAccion = idUsuario,
                            decision = "Recibida y Asignada", // Describe la acción realizada
                            comentarios = $"La solicitud ha sido recibida y asignada al usuario con ID {idUsuario}."
                        };

                        _context.tbSolicitudHistorial.Add(historial);

                        // 5. Guardar todos los cambios en la base de datos
                        _context.SaveChanges();

                        // 6. Confirmar la transacción
                        transaction.Commit();
                    }
                    // Si idUsuarioReceptor no es nulo, no se hace nada, ya que la solicitud ya fue tomada.
                    // La transacción simplemente se completará sin cambios que confirmar.
                }
                catch (Exception ex)
                {
                    // Si algo falla, revertir todo
                    transaction.Rollback();
                    throw new Exception("Ocurrió un error al marcar la solicitud como recibida.", ex);
                }
            }
        }
        public void RechazarSolicitud(int idSolicitud, int idUsuario, string comentarios)
        {
            SolicitudRecibida(idSolicitud, idUsuario);
            // Usamos una transacción para asegurar la integridad de los datos.
            // O se actualizan ambas tablas, o no se actualiza ninguna.
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // 1. Encontrar la solicitud a actualizar
                    var solicitud = _context.tbSolicitudesCambio.FirstOrDefault(s => s.idSolicitudCambio == idSolicitud);

                    if (solicitud == null)
                    {
                        // Si la solicitud no existe, no hacemos nada o lanzamos un error.
                        throw new Exception("La solicitud de cambio no fue encontrada.");
                    }

                    // Guardamos el paso actual antes de modificarlo para el historial.
                    int pasoActual = solicitud.pasoActualProceso;

                    // 2. Actualizar el estado y otros campos de la solicitud
                    // Nota: Tu constraint usa 'Cancelado', no 'Rechazado'.
                    solicitud.estadoSolicitud = "Cancelado";
                    solicitud.fechaEstado = DateTime.Now;
                    solicitud.observaciones = comentarios; // Podemos usar el campo observaciones para el motivo del rechazo.

                    // 3. Crear el registro de auditoría en el historial
                    var historial = new tbSolicitudHistorial
                    {
                        idSolicitudCambio = idSolicitud,
                        pasoProceso = pasoActual,
                        nombrePasoProceso = ObtenerNombrePaso(pasoActual), // Método auxiliar para el nombre legible
                        fechaAccion = DateTime.Now,
                        idUsuarioAccion = idUsuario,
                        decision = "Rechazado", // La acción que se tomó
                        comentarios = comentarios // El motivo
                    };

                    _context.tbSolicitudHistorial.Add(historial);

                    // 4. Guardar todos los cambios en la base de datos
                    _context.SaveChanges();

                    // 5. Si todo fue exitoso, confirmamos la transacción
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    // 6. Si algo falló, revertimos todos los cambios
                    transaction.Rollback();
                    // Opcional: Registrar el error en un log
                    // Lanza la excepción para que el controlador pueda manejarla
                    throw new Exception("Ocurrió un error al rechazar la solicitud.", ex);
                }
            }
        }

        public void EnviarAnalisis(int idSolicitud, int idUsuario, string comentarios)
        {
            SolicitudRecibida(idSolicitud, idUsuario);
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // 1. Encontrar la solicitud
                    var solicitud = _context.tbSolicitudesCambio.FirstOrDefault(s => s.idSolicitudCambio == idSolicitud);

                    if (solicitud == null)
                    {
                        throw new Exception("La solicitud de cambio no fue encontrada.");
                    }

                    // Guardamos el paso actual (que es 1) para el historial
                    int pasoDesdeDondeSeEnvia = solicitud.pasoActualProceso;

                    // 2. Actualizar la solicitud al siguiente paso
                    solicitud.pasoActualProceso = 2; // Actualizamos al paso de "Análisis"
                    solicitud.fechaEstado = DateTime.Now; // Actualizamos la fecha del último cambio de estado/paso

                    // Si tienes un campo de fecha específico para este giro, lo actualizas aquí
                    // Por ejemplo: solicitud.GiroJefeProyectoFecha = DateTime.Now;

                    // 3. Crear el registro de auditoría en el historial
                    var historial = new tbSolicitudHistorial
                    {
                        idSolicitudCambio = idSolicitud,
                        pasoProceso = pasoDesdeDondeSeEnvia, // El paso DESDE el que se realizó la acción
                        nombrePasoProceso = ObtenerNombrePaso(pasoDesdeDondeSeEnvia),
                        fechaAccion = DateTime.Now,
                        idUsuarioAccion = idUsuario,
                        decision = "Enviado a Análisis", // La decisión tomada
                        comentarios = comentarios
                    };

                    _context.tbSolicitudHistorial.Add(historial);

                    // 4. Guardar los cambios y confirmar la transacción
                    _context.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    // Si algo falla, revertir todo
                    transaction.Rollback();
                    throw new Exception("Ocurrió un error al enviar la solicitud a análisis.", ex);
                }
            }
        }

        // En Service/SolicitudService.cs

        public void AprobarAnalisis(int idSolicitud, int idUsuario, string comentarios)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var solicitud = _context.tbSolicitudesCambio.Find(idSolicitud);
                    if (solicitud == null || solicitud.pasoActualProceso != 2) throw new Exception("Acción no válida.");

                    // Actualizar solicitud
                    solicitud.pasoActualProceso = 3; // Mover al paso de Aprobación por Comité
                    solicitud.estadoSolicitud = "Aprobado"; // Estado intermedio
                    solicitud.fechaEstado = DateTime.Now;

                    // Registrar historial
                    _context.tbSolicitudHistorial.Add(new tbSolicitudHistorial
                    {
                        idSolicitudCambio = idSolicitud,
                        pasoProceso = 2,
                        nombrePasoProceso = "Análisis de Impacto",
                        fechaAccion = DateTime.Now,
                        idUsuarioAccion = idUsuario,
                        decision = "Análisis Aprobado",
                        comentarios = comentarios
                    });

                    _context.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    throw ex;
                }
            }
        }

        public void RechazarAnalisis(int idSolicitud, int idUsuario, string comentarios)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var solicitud = _context.tbSolicitudesCambio.Find(idSolicitud);
                    if (solicitud == null || solicitud.pasoActualProceso != 2) throw new Exception("Acción no válida.");

                    // Actualizar solicitud
                    solicitud.estadoSolicitud = "Cancelado";
                    solicitud.fechaEstado = DateTime.Now;
                    solicitud.observaciones = comentarios;

                    // Registrar historial
                    _context.tbSolicitudHistorial.Add(new tbSolicitudHistorial
                    {
                        idSolicitudCambio = idSolicitud,
                        pasoProceso = 2,
                        nombrePasoProceso = "Análisis de Impacto",
                        fechaAccion = DateTime.Now,
                        idUsuarioAccion = idUsuario,
                        decision = "Análisis Rechazado",
                        comentarios = comentarios
                    });

                    _context.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    throw ex;
                }
            }
        }

        public void SolicitarMasInformacion(int idSolicitud, int idUsuario, string comentarios)
        {
            // Esta acción no cambia el estado, solo añade un registro al historial
            // para que el gestor anterior lo vea.
            var solicitud = _context.tbSolicitudesCambio.Find(idSolicitud);
            if (solicitud == null || solicitud.pasoActualProceso != 2) throw new Exception("Acción no válida.");

            _context.tbSolicitudHistorial.Add(new tbSolicitudHistorial
            {
                idSolicitudCambio = idSolicitud,
                pasoProceso = 2,
                nombrePasoProceso = "Análisis de Impacto",
                fechaAccion = DateTime.Now,
                idUsuarioAccion = idUsuario,
                decision = "Solicitud de Información",
                comentarios = comentarios
            });
            _context.SaveChanges();
        }

        public void AprobarSolicitudFinal(int idSolicitud, int idUsuario, string comentarios)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var solicitud = _context.tbSolicitudesCambio.Find(idSolicitud);
                    if (solicitud == null || solicitud.pasoActualProceso != 3) throw new Exception("Acción no válida en el estado actual del proceso.");

                    // Actualizar la solicitud al estado "Planificado"
                    solicitud.pasoActualProceso = 4; // Avanza al siguiente paso (ej. Asignación/Desarrollo)
                    solicitud.estadoSolicitud = "Planificado";
                    solicitud.fechaEstado = DateTime.Now;
                    solicitud.fechaInicioImplementacionCambio = DateTime.Now; // Marcamos el inicio de la implementación

                    // Registrar historial
                    _context.tbSolicitudHistorial.Add(new tbSolicitudHistorial
                    {
                        idSolicitudCambio = idSolicitud,
                        pasoProceso = 3,
                        nombrePasoProceso = "Aprobación Final",
                        fechaAccion = DateTime.Now,
                        idUsuarioAccion = idUsuario,
                        decision = "Aprobado para Implementación",
                        comentarios = comentarios
                    });

                    _context.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    throw ex;
                }
            }
        }

        public void RechazarSolicitudFinal(int idSolicitud, int idUsuario, string comentarios)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var solicitud = _context.tbSolicitudesCambio.Find(idSolicitud);
                    if (solicitud == null || solicitud.pasoActualProceso != 3) throw new Exception("Acción no válida en el estado actual del proceso.");

                    // Actualizar solicitud para cancelarla definitivamente
                    solicitud.estadoSolicitud = "Cancelado";
                    solicitud.fechaEstado = DateTime.Now;
                    solicitud.observaciones = comentarios; // La razón del rechazo final

                    // Registrar historial
                    _context.tbSolicitudHistorial.Add(new tbSolicitudHistorial
                    {
                        idSolicitudCambio = idSolicitud,
                        pasoProceso = 3,
                        nombrePasoProceso = "Aprobación Final",
                        fechaAccion = DateTime.Now,
                        idUsuarioAccion = idUsuario,
                        decision = "Rechazado en Aprobación Final",
                        comentarios = comentarios
                    });

                    _context.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    throw ex;
                }
            }
        }

        public void AsignarTareasEIniciarImplementacion(AsignacionFormModel model, int idUsuarioAccion)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    if (model.NuevasTareas == null || !model.NuevasTareas.Any())
                    {
                        throw new Exception("Debe asignar al menos una tarea.");
                    }

                    var solicitud = _context.tbSolicitudesCambio.Find(model.IdSolicitud);
                    var elementoAfectado = _context.tbProyectoElemento.Find(solicitud.idElementoAfectado);

                    if (solicitud == null || elementoAfectado == null || solicitud.pasoActualProceso != 4)
                    {
                        throw new Exception("La solicitud no está en el paso correcto para la asignación de tareas.");
                    }

                    // 1. Crear las nuevas tareas
                    foreach (var tarea in model.NuevasTareas)
                    {
                        var nuevaTarea = new tbTareas
                        {
                            nombre = tarea.Nombre,
                            descripcion = tarea.Descripcion,
                            idUsuario = tarea.IdUsuarioAsignado,
                            idProyectoElemento = elementoAfectado.idProyectoElemento,
                            estado = "Pendiente" // Las tareas nacen como pendientes
                        };
                        _context.tbTareas.Add(nuevaTarea);
                    }

                    // 2. Actualizar el estado del elemento y el paso de la solicitud
                    elementoAfectado.estado = "En Proceso";
                    solicitud.pasoActualProceso = 5; // Mover a "Desarrollo"

                    // 3. Registrar la auditoría
                    _context.tbSolicitudHistorial.Add(new tbSolicitudHistorial
                    {
                        idSolicitudCambio = model.IdSolicitud,
                        pasoProceso = 4,
                        nombrePasoProceso = "Asignación",
                        fechaAccion = DateTime.Now,
                        idUsuarioAccion = idUsuarioAccion,
                        decision = "Tareas Asignadas",
                        comentarios = $"Se asignaron {model.NuevasTareas.Count} tareas. El cambio entra en fase de desarrollo."
                    });

                    _context.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    throw ex;
                }
            }
        }

        // En EstadoSolicitudService.cs

        public void EnviarAQA(int idSolicitud, int idUsuario, string comentarios)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var solicitud = _context.tbSolicitudesCambio.Find(idSolicitud);
                    if (solicitud == null || solicitud.pasoActualProceso != 5)
                    {
                        throw new Exception("La solicitud no se encuentra en la fase de Desarrollo.");
                    }

                    var tareasDesarrollo = _context.tbTareas
                        .Where(t => t.idProyectoElemento == solicitud.idElementoAfectado && !t.nombre.StartsWith("[DEFECTO]"));

                    if (tareasDesarrollo.Any(t => t.estado != "Finalizado"))
                    {
                        throw new Exception("No se puede enviar a QA. Aún hay tareas de desarrollo pendientes o en proceso.");
                    }

                    // --- LÓGICA CRÍTICA NUEVA: GENERAR TAREAS DE PRUEBA ---
                    // Por cada tarea de desarrollo finalizada, creamos una tarea de prueba correspondiente.
                    // En un sistema real, estas podrían venir de un plan de pruebas. Aquí las generamos.
                    var idUsuarioQA = 2; // Asumimos un ID de usuario de QA, en un caso real lo buscarías o asignarías.

                    foreach (var tareaDev in tareasDesarrollo)
                    {
                        var nuevaTareaQA = new tbTareas
                        {
                            nombre = $"[PRUEBA] - {tareaDev.nombre}",
                            descripcion = $"Verificar que la funcionalidad '{tareaDev.nombre}' se implementó correctamente según los criterios.",
                            idUsuario = idUsuarioQA, // Asignar a un tester
                            idProyectoElemento = solicitud.idElementoAfectado,
                            estado = "Pendiente" // ¡¡NACEN COMO PENDIENTES!!
                        };
                        _context.tbTareas.Add(nuevaTareaQA);
                    }
                    // --------------------------------------------------------

                    // 1. Actualizar el paso de la solicitud
                    solicitud.pasoActualProceso = 6; // Mover a "Pruebas (QA)"

                    // 2. Registrar en el historial
                    _context.tbSolicitudHistorial.Add(new tbSolicitudHistorial
                    {
                        idSolicitudCambio = idSolicitud,
                        pasoProceso = 5,
                        nombrePasoProceso = "Desarrollo",
                        fechaAccion = DateTime.Now,
                        idUsuarioAccion = idUsuario,
                        decision = "Enviado a QA",
                        comentarios = comentarios
                    });

                    _context.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    throw ex;
                }
            }
        }

        // En Service/SolicitudService.cs

        public void AprobarParaDespliegue(int idSolicitud, int idUsuario, string comentarios)
        {
            // Validaciones: Todas las pruebas deben estar pasadas y no debe haber incidencias abiertas.
            var solicitud = _context.tbSolicitudesCambio.Find(idSolicitud);
            var tareasQA = _context.tbTareas.Where(t => t.idProyectoElemento == solicitud.idElementoAfectado);

            if (tareasQA.Any(t => !t.nombre.StartsWith("[DEFECTO]") && t.estado != "Finalizado"))
                throw new Exception("No se puede aprobar. Aún hay casos de prueba sin finalizar.");
            if (tareasQA.Any(t => t.nombre.StartsWith("[DEFECTO]") && t.estado != "Finalizado")) // Asume que el dev lo marca como Finalizado
                throw new Exception("No se puede aprobar. Aún hay incidencias abiertas o sin corregir.");

            solicitud.pasoActualProceso = 7; // Mover a "Despliegue"
                                             // El estado de la solicitud sigue siendo "Planificado"

            _context.tbSolicitudHistorial.Add(new tbSolicitudHistorial
            {
                idSolicitudCambio = idSolicitud,
                pasoProceso = 6,
                nombrePasoProceso = "Pruebas (QA)",
                fechaAccion = DateTime.Now,
                idUsuarioAccion = idUsuario,
                decision = "QA Aprobado para Despliegue",
                comentarios = comentarios
            });
            _context.SaveChanges();
        }

        public void RetornarADesarrollo(int idSolicitud, int idUsuario, string comentarios)
        {
            // Validación: Debe haber al menos una incidencia abierta o una prueba fallida.
            var solicitud = _context.tbSolicitudesCambio.Find(idSolicitud);
            solicitud.pasoActualProceso = 5; // Devolver a "Desarrollo"

            _context.tbSolicitudHistorial.Add(new tbSolicitudHistorial
            {
                idSolicitudCambio = idSolicitud,
                pasoProceso = 6,
                nombrePasoProceso = "Pruebas (QA)",
                fechaAccion = DateTime.Now,
                idUsuarioAccion = idUsuario,
                decision = "Retornado a Desarrollo",
                comentarios = comentarios
            });
            _context.SaveChanges();
        }

        // Método para que QA reporte una incidencia
        public void RegistrarIncidenciaQA(int idSolicitud, string descripcion, string severidad, int idDevAsignado, int idUsuarioQA)
        {
            var solicitud = _context.tbSolicitudesCambio.Find(idSolicitud);
            var nuevaIncidencia = new tbTareas
            {
                nombre = $"[DEFECTO]: {severidad}",
                descripcion = descripcion,
                idUsuario = idDevAsignado, // Se asigna al desarrollador
                idProyectoElemento = solicitud.idElementoAfectado,
                estado = "Pendiente" // El desarrollador lo cambiará a "En Proceso"
            };
            _context.tbTareas.Add(nuevaIncidencia);
            _context.SaveChanges();
        }

        public void CrearNuevoDespliegue(int idSolicitud, int idEntorno, List<string> pasos, int idUsuario)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // 1. Crear el registro principal del despliegue
                    var nuevoDespliegue = new tbDespliegues
                    {
                        idSolicitudCambio = idSolicitud,
                        idEntorno = idEntorno,
                        idUsuarioInicio = idUsuario,
                        fechaInicio = DateTime.Now,
                        estado = "En Proceso" // Inicia en este estado
                    };
                    _context.tbDespliegues.Add(nuevoDespliegue);
                    _context.SaveChanges(); // Guardamos para obtener el ID

                    // 2. Crear cada uno de los pasos
                    for (int i = 0; i < pasos.Count; i++)
                    {
                        var nuevoPaso = new tbPasosDespliegue
                        {
                            idDespliegue = nuevoDespliegue.idDespliegue,
                            orden = i + 1,
                            descripcion = pasos[i],
                            estado = "Pendiente"
                        };
                        _context.tbPasosDespliegue.Add(nuevoPaso);
                    }

                    // 3. Registrar en el historial
                    var entornoNombre = _context.tbEntornosDespliegue.Find(idEntorno).nombre;
                    _context.tbSolicitudHistorial.Add(new tbSolicitudHistorial
                    {
                        idSolicitudCambio = idSolicitud,
                        pasoProceso = 7,
                        nombrePasoProceso = "Despliegue",
                        fechaAccion = DateTime.Now,
                        idUsuarioAccion = idUsuario,
                        decision = "Inicio de Despliegue",
                        comentarios = $"Se ha planificado e iniciado un despliegue en el entorno: {entornoNombre}."
                    });

                    _context.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    throw new Exception("Error al crear el plan de despliegue.", ex);
                }
            }
        }

        public bool CompletarPasoDespliegue(int idPaso, string notas, int idUsuario)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // Paso 1: Encuentra el paso y carga su despliegue padre y todos los pasos "hermanos".
                    // Esto trae todos los datos relevantes a la memoria para una verificación fiable.
                    var paso = _context.tbPasosDespliegue
                                       .Include(p => p.tbDespliegues.tbPasosDespliegue) // Carga el padre y todos sus hijos (los pasos)
                                       .FirstOrDefault(p => p.idPasoDespliegue == idPaso);

                    if (paso == null || paso.estado == "Completado")
                    {
                        // El paso no existe o ya fue completado.
                        return false;
                    }

                    // Paso 2: Actualiza el estado del paso actual en la memoria.
                    paso.estado = "Completado";
                    paso.idUsuarioCompletado = idUsuario;
                    paso.fechaCompletado = DateTime.Now;
                    paso.notas = notas;

                    var despliegue = paso.tbDespliegues;

                    // Paso 3: Verifica si TODOS los pasos de este despliegue están ahora "Completado".
                    // Esta comprobación es fiable porque opera sobre la colección en memoria que cargamos previamente.
                    bool todosCompletos = despliegue.tbPasosDespliegue.All(p => p.estado == "Completado");

                    if (todosCompletos)
                    {
                        // Paso 4: Si todos los pasos terminaron, actualiza el estado del despliegue padre.
                        despliegue.estado = "Completado";
                        despliegue.fechaFin = DateTime.Now;
                    }

                    // Paso 5: Guarda todos los cambios (el estado del paso y, si aplica, el del despliegue) en una sola transacción.
                    _context.SaveChanges();
                    transaction.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    // Opcional pero recomendado: registra el error 'ex' en un log.
                    transaction.Rollback();
                    return false;
                }
            }
        }


        // Sobrescribe el método anterior, ya que este es el verdadero final del proceso.
        public void FinalizarImplementacion(int idSolicitud, string comentarios, int idUsuario)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var solicitud = _context.tbSolicitudesCambio.Find(idSolicitud);
                    if (solicitud == null || solicitud.pasoActualProceso != 7)
                        throw new Exception("La solicitud no está en la fase de Despliegue para ser enviada a aceptación.");

                    bool hayDespliegueExitoso = _context.tbDespliegues.Any(d => d.idSolicitudCambio == idSolicitud && d.estado == "Completado");
                    if (!hayDespliegueExitoso)
                        throw new Exception("No se puede enviar a aceptación sin al menos un despliegue completado exitosamente.");

                    // 1. Actualizar la solicitud al paso de Aceptación
                    solicitud.pasoActualProceso = 8; // Mover al paso final de "Aceptación"

                    // 2. Registrar en el historial que está listo para la aceptación del cliente
                    _context.tbSolicitudHistorial.Add(new tbSolicitudHistorial
                    {
                        idSolicitudCambio = idSolicitud,
                        pasoProceso = 7,
                        nombrePasoProceso = "Despliegue",
                        fechaAccion = DateTime.Now,
                        idUsuarioAccion = idUsuario,
                        decision = "Enviado a Aceptación del Cliente",
                        comentarios = comentarios
                    });

                    _context.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    throw ex;
                }
            }
        }

        // NUEVO: Método para que el cliente acepte el cambio
        public void AceptarCambio(int idSolicitud, string comentarios, int idUsuario)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var solicitud = _context.tbSolicitudesCambio.Find(idSolicitud);
                    if (solicitud == null || solicitud.pasoActualProceso != 8)
                        throw new Exception("La solicitud no está en la fase de Aceptación.");

                    // Actualizar la solicitud al estado final y exitoso
                    solicitud.estadoSolicitud = "Implantado";
                    solicitud.fechaCierreDelCambio = DateTime.Now;
                    solicitud.observaciones = comentarios;

                    // Registrar el hito final en el historial
                    _context.tbSolicitudHistorial.Add(new tbSolicitudHistorial
                    {
                        idSolicitudCambio = idSolicitud,
                        pasoProceso = 8,
                        nombrePasoProceso = "Aceptación",
                        fechaAccion = DateTime.Now,
                        idUsuarioAccion = idUsuario,
                        decision = "Aceptado",
                        comentarios = comentarios
                    });

                    _context.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    throw ex;
                }
            }
        }

        // NUEVO: Método para que el cliente rechace el cambio
        public void RechazarCambio(int idSolicitud, string comentarios, int idUsuario)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var solicitud = _context.tbSolicitudesCambio.Find(idSolicitud);
                    if (solicitud == null || solicitud.pasoActualProceso != 8)
                        throw new Exception("La solicitud no está en la fase de Aceptación.");

                    // El cambio se cancela. Esto podría requerir un "rollback" manual.
                    solicitud.estadoSolicitud = "Cancelado";
                    solicitud.fechaCierreDelCambio = DateTime.Now; // Se cierra, pero como cancelado.
                    solicitud.observaciones = $"Rechazado por el cliente final: {comentarios}";

                    // Registrar en el historial
                    _context.tbSolicitudHistorial.Add(new tbSolicitudHistorial
                    {
                        idSolicitudCambio = idSolicitud,
                        pasoProceso = 8,
                        nombrePasoProceso = "Aceptación",
                        fechaAccion = DateTime.Now,
                        idUsuarioAccion = idUsuario,
                        decision = "Rechazado",
                        comentarios = comentarios
                    });

                    _context.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    throw ex;
                }
            }
        }
        public void ActualizarEstadoTareaQA(int idTarea, string nuevoEstado, int idUsuario)
        {
            var tarea = _context.tbTareas.Find(idTarea);
            if (tarea == null)
            {
                throw new Exception("La tarea no fue encontrada.");
            }

            // Validación para evitar estados no deseados
            var estadosValidos = new[] { "Passed", "Failed" };
            if (!estadosValidos.Contains(nuevoEstado))
            {
                throw new Exception("Estado no válido para esta acción.");
            }

            tarea.estado = nuevoEstado;
            // Opcional: Registrar quién y cuándo cambió el estado en un log o en la misma tarea si tienes campos para ello.

            _context.SaveChanges();
        }
    }
}