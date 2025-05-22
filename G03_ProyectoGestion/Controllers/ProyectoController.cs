using G03_ProyectoGestion.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity; // Necesario para .Include() y para transacciones explícitas si se usan

namespace G03_ProyectoGestion.Controllers
{
    public class ProyectoController : Controller
    {
        private g03_databaseEntities _dbContext = new g03_databaseEntities();

        public ActionResult Crear()
        {
            ViewBag.Usuarios = _dbContext.tbUsuarios.Where(u => u.estadoUsuario == "activo").ToList();
            ViewBag.Metodologias = _dbContext.tbMetodologias.ToList();
            ViewBag.Roles = _dbContext.tbRoles.ToList();
            // Pasar el nombre de la metodología RUP a la vista para la validación de JS
            var rupMetodologia = _dbContext.tbMetodologias.FirstOrDefault(m => m.nombreMetodologia.Equals("RUP", StringComparison.OrdinalIgnoreCase));
            ViewBag.RupMetodologiaNombre = rupMetodologia?.nombreMetodologia; // Será "RUP" o null
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(string nombreProyecto, string descripcionProyecto, DateTime fechaInicio, DateTime fechaFin, int idMetodologia, List<int> miembros, List<int> roles, List<int> selectedElementos)
        {
            var metodologiaSeleccionada = _dbContext.tbMetodologias.Find(idMetodologia);
            bool esRup = metodologiaSeleccionada != null && metodologiaSeleccionada.nombreMetodologia.Equals("RUP", StringComparison.OrdinalIgnoreCase);

            // 1. Validación de Fechas
            if (fechaFin < fechaInicio)
            {
                ModelState.AddModelError("fechaFin", "La fecha de fin no puede ser anterior a la fecha de inicio.");
            }

            if (esRup)
            {
                // RUP debe durar al menos 3 meses
                if (fechaFin < fechaInicio.AddMonths(3))
                {
                    // Ajuste para ser más preciso con el "mínimo 3 meses"
                    // Si fechaInicio es 01-Jan y fechaFin es 30-Mar, AddMonths(3) sería 01-Apr.
                    // Una forma más simple es verificar si el día de fin es al menos el mismo día de inicio 3 meses después.
                    // O, que la diferencia en días sea aproximadamente 90.
                    // Para este ejemplo, AddMonths(3) es una buena aproximación.
                    // Si fechaFin es 31-Marzo y fechaInicio 01-Enero, fechaInicio.AddMonths(3) es 01-Abril.
                    // fechaFin (31-Mar) < fechaInicio.AddMonths(3) (01-Abr) -> True, pero casi 3 meses.
                    // Consideremos que AddMonths(3) nos da el inicio del 4to mes. Así que fechaFin debe ser >= a eso.
                    // Para ser exactos: (fechaFin - fechaInicio).TotalDays < 89 (aproximadamente, puede variar)
                    // Usemos AddMonths para simplificar, pero ten en cuenta sus peculiaridades.
                    // Si el proyecto empieza el 15/01, 3 meses después sería el 15/04.
                    // fechaFin debe ser >= 15/04
                    var fechaMinimaFinRup = fechaInicio.AddMonths(3);
                    if (fechaFin < fechaMinimaFinRup)
                    {
                        ModelState.AddModelError("fechaFin", "Para la metodología RUP, el proyecto debe durar como mínimo 3 meses.");
                    }
                }
            }

            if (ModelState.IsValid)
            {
                int idUsuarioCreador = Convert.ToInt32(Session["idUsuario"]);

                var proyecto = new tbProyectos
                {
                    nombreProyecto = nombreProyecto,
                    descripcionProyecto = descripcionProyecto,
                    fechaInicio = fechaInicio,
                    fechaFin = fechaFin,
                    idUsuario = idUsuarioCreador,
                    idMetodologia = idMetodologia,
                    estado = 1,
                    // Si es RUP, la fase inicial del proyecto es 1. Para otras metodologías, no se asigna fase inicial al proyecto.
                    idFase = esRup ? _dbContext.tbRupFases.FirstOrDefault(f => f.nombre.Contains("Inicio") || f.idFase == 1)?.idFase : (int?)null // Asume que fase 1 es Inicio
                };

                _dbContext.tbProyectos.Add(proyecto);
                _dbContext.SaveChanges(); // Guardar proyecto para obtener idProyectoCreado

                int idProyectoCreado = proyecto.idProyecto;

                // Guardar Miembros
                if (miembros != null && roles != null && miembros.Count == roles.Count)
                {
                    for (int i = 0; i < miembros.Count; i++)
                    {
                        var miembroProyecto = new tbProyectoUsuarios
                        {
                            idProyecto = idProyectoCreado,
                            idUsuario = miembros[i],
                            idRol = roles[i]
                        };
                        _dbContext.tbProyectoUsuarios.Add(miembroProyecto);
                    }
                    _dbContext.SaveChanges();
                }

                List<tbProyectoRupFases> fasesDelProyectoRup = new List<tbProyectoRupFases>();

                // 2. División Automática de Fases RUP
                if (esRup)
                {
                    var rupFasesDefinicion = _dbContext.tbRupFases.OrderBy(f => f.idFase).ToList(); // Asume idFase 1,2,3,4
                    if (rupFasesDefinicion.Count >= 4) // Asegurarse que tenemos al menos 4 fases definidas para RUP
                    {
                        double totalDiasProyecto = (fechaFin - fechaInicio).TotalDays + 1; // +1 para incluir el día de fin
                        int numeroDeFasesRup = 4;

                        // Distribuir días equitativamente
                        int[] diasPorFase = new int[numeroDeFasesRup];
                        double diasBasePorFase = totalDiasProyecto / numeroDeFasesRup;

                        for (int i = 0; i < numeroDeFasesRup; i++)
                        {
                            diasPorFase[i] = (int)Math.Floor(diasBasePorFase);
                        }

                        int diasRestantes = (int)totalDiasProyecto - diasPorFase.Sum();
                        for (int i = 0; i < diasRestantes; i++) // Distribuir los días sobrantes
                        {
                            diasPorFase[i % numeroDeFasesRup]++;
                        }

                        DateTime fechaInicioFaseActual = fechaInicio;
                        for (int i = 0; i < numeroDeFasesRup; i++)
                        {
                            // Usar el idFase de la definición de tbRupFases
                            int idFaseDb = rupFasesDefinicion[i].idFase;

                            DateTime fechaFinFaseActual = fechaInicioFaseActual.AddDays(diasPorFase[i] - 1);
                            // Asegurar que la última fase termine exactamente en la fechaFin del proyecto
                            if (i == numeroDeFasesRup - 1)
                            {
                                fechaFinFaseActual = fechaFin;
                            }


                            var proyectoRupFase = new tbProyectoRupFases
                            {
                                idProyecto = idProyectoCreado,
                                idFase = idFaseDb, // Usar el id de la tabla tbRupFases
                                fechaInicio = fechaInicioFaseActual,
                                fechaFin = fechaFinFaseActual
                            };
                            _dbContext.tbProyectoRupFases.Add(proyectoRupFase);
                            fasesDelProyectoRup.Add(proyectoRupFase); // Guardar para usarla con los elementos

                            fechaInicioFaseActual = fechaFinFaseActual.AddDays(1);
                        }
                        _dbContext.SaveChanges();
                    }
                    else
                    {
                        // Log o manejo de error: No se encontraron las 4 fases RUP definidas
                        ModelState.AddModelError("", "No se pudieron definir las fases RUP. Faltan definiciones de fases.");
                        // Repoblar ViewBags y retornar vista
                        ViewBag.Usuarios = _dbContext.tbUsuarios.Where(u => u.estadoUsuario == "activo").ToList();
                        ViewBag.Metodologias = _dbContext.tbMetodologias.ToList();
                        ViewBag.Roles = _dbContext.tbRoles.ToList();
                        ViewBag.RupMetodologiaNombre = metodologiaSeleccionada?.nombreMetodologia;
                        return View(new { nombreProyecto, descripcionProyecto, fechaInicio, fechaFin, idMetodologia, miembros, roles, selectedElementos });
                    }
                }

                // 3. División Automática de Elementos RUP por Fase (o asignación normal)
                if (selectedElementos != null && selectedElementos.Any())
                {
                    if (selectedElementos != null && selectedElementos.Any())
                    {
                        if (esRup && fasesDelProyectoRup.Any()) // fasesDelProyectoRup ya contiene las fases calculadas para ESTE proyecto
                        {
                            // Necesitamos los detalles de los elementos seleccionados, especialmente su descripción (que indica la fase RUP)
                            var elementosSeleccionadosConDetalles = _dbContext.tbElementos
                                .Where(e => selectedElementos.Contains(e.idElemento) && e.tipo.Equals("RUP", StringComparison.OrdinalIgnoreCase))
                                .ToList();

                            // Necesitamos un mapeo de nombre de fase (ej. "FASE 1") a idFase de tbRupFases
                            // y las fechas de inicio/fin de cada fase para este proyecto específico.
                            // fasesDelProyectoRup ya tiene idFase, fechaInicio, fechaFin para este proyecto.
                            // tbRupFases tiene idFase y nombreFase (que podría ser "Inicio", "Elaboración", etc.)
                            // Asumiremos que tbElementos.descripcion ("FASE 1", "FASE 2") coincide con alguna parte
                            // identificable de tbRupFases.nombreFase o que tenemos una forma de mapear.

                            // Vamos a simplificar y asumir que "FASE X" en tbElementos.descripcion
                            // corresponde al número X de la fase RUP.
                            // Ejemplo: "FASE 1" -> idFase = 1 (de tbRupFases)
                            // Ejemplo: "FASE 2" -> idFase = 2 (de tbRupFases)

                            foreach (var elementoInfo in elementosSeleccionadosConDetalles)
                            {
                                int? idFaseDelElemento = null;
                                if (!string.IsNullOrWhiteSpace(elementoInfo.descripcion))
                                {
                                    // Extraer el número de la fase de la descripción "FASE X"
                                    string faseNumeroStr = elementoInfo.descripcion.ToUpper().Replace("FASE", "").Trim();
                                    if (int.TryParse(faseNumeroStr, out int numeroFase))
                                    {
                                        // Buscar la definición de esta fase en tbRupFases para obtener su idFase REAL
                                        // Esto es crucial si los idFase en tbRupFases no son simplemente 1,2,3,4
                                        // o si los nombres son más descriptivos (ej. "Inicio", "Elaboración")
                                        // y tenemos una forma de mapearlos al número.
                                        // Por ahora, si tbRupFases tiene idFase = 1, 2, 3, 4 y éstos corresponden
                                        // secuencialmente a Fase 1, Fase 2, etc., podemos buscar por el número.

                                        // Opción A: Si tbRupFases.idFase es directamente 1, 2, 3, 4
                                        // var faseDefinicion = _dbContext.tbRupFases.FirstOrDefault(f => f.idFase == numeroFase);

                                        // Opción B: Si tbRupFases.nombreFase contiene "Fase X" o similar
                                        // var faseDefinicion = _dbContext.tbRupFases
                                        //    .FirstOrDefault(f => f.nombreFase.Contains(numeroFase.ToString()));

                                        // Para tu caso, donde tbElementos.descripcion ES "FASE X",
                                        // y queremos mapearlo a las fases calculadas en fasesDelProyectoRup.
                                        // fasesDelProyectoRup está ordenado por idFase (1,2,3,4)
                                        // y tiene las fechas correctas.
                                        // Necesitamos encontrar el tbProyectoRupFases que corresponde al 'numeroFase'.
                                        // Si los idFase de tbRupFases son 1,2,3,4 y corresponden a FASE 1, FASE 2...
                                        // entonces el idFase que buscamos en fasesDelProyectoRup es 'numeroFase'.

                                        var faseCorrespondienteEnProyecto = fasesDelProyectoRup
                                                                        .FirstOrDefault(f => f.idFase == numeroFase); // Asumiendo que idFase de tbRupFases son 1,2,3,4...

                                        if (faseCorrespondienteEnProyecto != null)
                                        {
                                            idFaseDelElemento = faseCorrespondienteEnProyecto.idFase;

                                            var proyectoElemento = new tbProyectoElemento
                                            {
                                                idProyecto = idProyectoCreado,
                                                idElemento = elementoInfo.idElemento,
                                                fechaInicio = faseCorrespondienteEnProyecto.fechaInicio,
                                                fechaFin = faseCorrespondienteEnProyecto.fechaFin,
                                                FASE_SPRINT_ITERACION = idFaseDelElemento
                                            };
                                            _dbContext.tbProyectoElemento.Add(proyectoElemento);
                                        }
                                        else
                                        {
                                            // El elemento indica una fase (ej. "FASE 5") que no existe en las 4 fases del proyecto.
                                            // ¿Qué hacer? Omitir, asignar a una por defecto, o dar error.
                                            // Por ahora, lo omitiremos, pero podrías loggear un warning.
                                            System.Diagnostics.Debug.WriteLine($"Advertencia: El elemento '{elementoInfo.nombre}' (ID: {elementoInfo.idElemento}) indica la fase '{elementoInfo.descripcion}' que no se pudo mapear a una fase del proyecto RUP ID: {idProyectoCreado}.");
                                            // O podrías asignarlo al proyecto general sin fase específica si eso tiene sentido:
                                            /*
                                            var proyectoElemento = new tbProyectoElemento
                                            {
                                                idProyecto = idProyectoCreado,
                                                idElemento = elementoInfo.idElemento,
                                                fechaInicio = fechaInicio, // Fecha inicio del proyecto
                                                fechaFin = fechaFin,       // Fecha fin del proyecto
                                                FASE_SPRINT_ITERACION = null
                                            };
                                            _dbContext.tbProyectoElemento.Add(proyectoElemento);
                                            */
                                        }
                                    }
                                    else
                                    {
                                        // La descripción del elemento RUP no sigue el formato "FASE X" esperado.
                                        System.Diagnostics.Debug.WriteLine($"Advertencia: El elemento RUP '{elementoInfo.nombre}' (ID: {elementoInfo.idElemento}) tiene una descripción '{elementoInfo.descripcion}' no interpretable como fase.");
                                        // Podrías asignarlo al proyecto sin fase específica.
                                    }
                                }
                                else
                                {
                                    // Elemento RUP sin descripción de fase. ¿Cómo tratarlo?
                                    // Podrías asignarlo a la primera fase por defecto, o al proyecto sin fase.
                                    System.Diagnostics.Debug.WriteLine($"Advertencia: El elemento RUP '{elementoInfo.nombre}' (ID: {elementoInfo.idElemento}) no tiene descripción de fase.");
                                    // Ejemplo: asignar a la primera fase
                                    /*
                                    if (fasesDelProyectoRup.Any()) {
                                        var primeraFase = fasesDelProyectoRup.First();
                                        var proyectoElemento = new tbProyectoElemento
                                        {
                                            idProyecto = idProyectoCreado,
                                            idElemento = elementoInfo.idElemento,
                                            fechaInicio = primeraFase.fechaInicio,
                                            fechaFin = primeraFase.fechaFin,
                                            FASE_SPRINT_ITERACION = primeraFase.idFase
                                        };
                                        _dbContext.tbProyectoElemento.Add(proyectoElemento);
                                    }
                                    */
                                }
                            }
                        }
                        else // No es RUP o no se pudieron crear las fases RUP
                        {
                            // Lógica original para metodologías no-RUP o si RUP falló en la creación de fases
                            var elementosNoRupSeleccionados = _dbContext.tbElementos
                                .Where(e => selectedElementos.Contains(e.idElemento)) // Podrías filtrar aquí si solo quieres los que NO son RUP, o si RUP falló.
                                .ToList();

                            foreach (var elementoInfo in elementosNoRupSeleccionados)
                            {
                                var proyectoElemento = new tbProyectoElemento
                                {
                                    idProyecto = idProyectoCreado,
                                    idElemento = elementoInfo.idElemento,
                                    fechaInicio = fechaInicio, // Fecha inicio del proyecto
                                    fechaFin = fechaFin,       // Fecha fin del proyecto
                                    FASE_SPRINT_ITERACION = null // No aplica fase específica aquí
                                };
                                _dbContext.tbProyectoElemento.Add(proyectoElemento);
                            }
                        }
                        _dbContext.SaveChanges(); // Guardar los tbProyectoElemento
                    }
                    else // No es RUP o no se pudieron crear las fases RUP
                    {
                        foreach (var idElemento in selectedElementos)
                        {
                            var proyectoElemento = new tbProyectoElemento
                            {
                                idProyecto = idProyectoCreado,
                                idElemento = idElemento,
                                fechaInicio = fechaInicio, // Fecha inicio del proyecto
                                fechaFin = fechaFin,       // Fecha fin del proyecto
                                FASE_SPRINT_ITERACION = null // No aplica fase específica aquí
                            };
                            _dbContext.tbProyectoElemento.Add(proyectoElemento);
                        }
                    }
                    _dbContext.SaveChanges();
                }

                return RedirectToAction("Index");
            }

            // Si ModelState no es válido, repoblar ViewBags y retornar la vista
            ViewBag.Usuarios = _dbContext.tbUsuarios.Where(u => u.estadoUsuario == "activo").ToList();
            ViewBag.Metodologias = _dbContext.tbMetodologias.ToList();
            ViewBag.Roles = _dbContext.tbRoles.ToList();
            ViewBag.RupMetodologiaNombre = metodologiaSeleccionada?.nombreMetodologia;
            // Devolver los datos ingresados para que el usuario no los pierda
            // Necesitarías un ViewModel para pasar estos datos de forma estructurada.
            // Por ahora, los pongo en ViewBag o los pasas directamente al View(modelo) si tienes uno.
            ViewBag.NombreProyectoIngresado = nombreProyecto;
            ViewBag.DescripcionProyectoIngresado = descripcionProyecto;
            // ... y así sucesivamente para otros campos si quieres repoblarlos.
            return View(); // Aquí deberías pasar un modelo con los datos si es necesario.
        }


        public ActionResult Index()
        {
            int idUsuario = Convert.ToInt32(Session["idUsuario"]);
            var proyectosViewModel = (from p in _dbContext.tbProyectos
                                      join pu in _dbContext.tbProyectoUsuarios on p.idProyecto equals pu.idProyecto
                                      where pu.idUsuario == idUsuario && p.estado == 1
                                      select new ProyectoCardViewModel // Asegúrate que este ViewModel exista y tenga las propiedades
                                      {
                                          IdProyecto = p.idProyecto,
                                          NombreProyecto = p.nombreProyecto,
                                          DescripcionProyecto = p.descripcionProyecto,
                                          FechaInicio = p.fechaInicio,
                                          FechaFin = p.fechaFin,
                                          Metodologia = p.tbMetodologias != null ? p.tbMetodologias.nombreMetodologia : "No asignada"
                                      }).ToList();
            return View(proyectosViewModel);
        }

        public ActionResult Detalles(int id)
        {
            // Incluir fases RUP si es un proyecto RUP
            var proyecto = _dbContext.tbProyectos
                                     .Include(p => p.tbMetodologias) // Para nombre de metodología
                                     .Include(p => p.tbProyectoUsuarios.Select(pu => pu.tbUsuarios)) // Miembros y sus usuarios
                                     .Include(p => p.tbProyectoUsuarios.Select(pu => pu.tbRoles))    // Roles de los miembros
                                     .Include(p => p.tbProyectoElemento.Select(pe => pe.tbElementos)) // Elementos del proyecto
                                     .Include(p => p.tbProyectoRupFases.Select(prf => prf.tbRupFases)) // Fases RUP del proyecto
                                     .FirstOrDefault(p => p.idProyecto == id);
            if (proyecto == null) return HttpNotFound();

            // Podrías pasar información adicional a la vista a través de ViewBag o un ViewModel más complejo
            // ViewBag.EsRup = proyecto.tbMetodologias?.nombreMetodologia.Equals("RUP", StringComparison.OrdinalIgnoreCase) ?? false;
            // ViewBag.FasesRupInfo = proyecto.tbProyectoRupFases.OrderBy(f => f.idFase).ToList();

            return View(proyecto);
        }


        [HttpGet]
        public JsonResult ObtenerElementosPorMetodologia(int idMetodologia)
        {
            try
            {
                var metodologia = _dbContext.tbMetodologias.Find(idMetodologia);
                if (metodologia == null)
                {
                    return Json(new { success = false, message = "Metodología no encontrada" }, JsonRequestBehavior.AllowGet);
                }

                string nombreMetodologia = metodologia.nombreMetodologia;
                bool esRup = nombreMetodologia.Equals("RUP", StringComparison.OrdinalIgnoreCase);

                var elementosQuery = _dbContext.tbElementos
                                          .Where(e => e.tipo.Equals(nombreMetodologia, StringComparison.OrdinalIgnoreCase));

                if (esRup)
                {
                    // Para RUP, la 'descripcion' del elemento es la Fase ("FASE 1", "FASE 2", etc.)
                    // Esta lógica agrupa los elementos por su 'descripcion' que actúa como nombre de fase.
                    var elementosAgrupados = elementosQuery
                        .ToList()
                        .GroupBy(e => e.descripcion)
                        .OrderBy(g => g.Key)
                        .Select(g => new
                        {
                            faseNombre = g.Key,
                            elementos = g.Select(e => new { e.idElemento, e.nombre, e.descripcion }).ToList()
                        })
                        .ToList();

                    if (!elementosAgrupados.Any())
                    {
                        return Json(new { success = true, isRup = true, data = new List<object>(), message = "No hay elementos de configuración para RUP." }, JsonRequestBehavior.AllowGet);
                    }
                    return Json(new { success = true, isRup = true, data = elementosAgrupados }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    var elementosSimples = elementosQuery
                        .Select(e => new { e.idElemento, e.nombre, e.descripcion })
                        .ToList();

                    if (!elementosSimples.Any())
                    {
                        return Json(new { success = true, isRup = false, data = new List<object>(), message = $"No hay elementos de configuración para {nombreMetodologia}." }, JsonRequestBehavior.AllowGet);
                    }
                    return Json(new { success = true, isRup = false, data = elementosSimples }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                // Loguear el error ex.ToString() para obtener más detalles, incluyendo stack trace
                System.Diagnostics.Debug.WriteLine("Error en ObtenerElementosPorMetodologia: " + ex.ToString());
                return Json(new { success = false, message = "Error al obtener elementos. " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _dbContext.Dispose();
            base.Dispose(disposing);
        }
    }
}