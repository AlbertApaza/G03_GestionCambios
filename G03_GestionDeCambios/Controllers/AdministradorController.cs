using G03_GestionDeCambios.Models;
using G03_GestionDeCambios.Service;
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace G03_GestionDeCambios.Controllers
{
    public class AdministradorController : Controller
    {
        private readonly AdministradorService _adminService = new AdministradorService();
        private readonly BD_GestionDeCambiosEntities db = new BD_GestionDeCambiosEntities();
        private readonly DashboardService _dashboardService = new DashboardService();
        private readonly SuperAdminDashboardService _superAdminDashboardService = new SuperAdminDashboardService();



        private bool IsAdmin()
        {
            if (Session["idUsuario"] == null)
            {
                return false;
            }
            int idUsuario = (int)Session["idUsuario"];
            var usuario = db.tbUsuarios.Find(idUsuario);

            return usuario != null && usuario.adminRol == 1;
        }

        public ActionResult Index()
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Login");
            var viewModel = _adminService.GetDashboardData();
            return View(viewModel);
        }

        public ActionResult Dashboard()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Login");
            }

            // Usamos el nuevo servicio para obtener los datos de TODO el sistema
            var viewModel = _superAdminDashboardService.GetSuperAdminDashboardData();

            // La vista se seguirá buscando en /Views/Administrador/Dashboard.cshtml
            return View(viewModel);
        }


        // --- GESTIÓN DE USUARIOS ---

        public ActionResult DetailsUsuario(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Login");
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            tbUsuarios tbUsuarios = db.tbUsuarios.Find(id);
            if (tbUsuarios == null) return HttpNotFound();
            return View(tbUsuarios);
        }

        public ActionResult CreateUsuario()
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Login");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateUsuario([Bind(Include = "usuario,contrasena,nombre,apellido,email,estado,adminRol")] tbUsuarios tbUsuarios)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Login");
            if (ModelState.IsValid)
            {
                tbUsuarios.fechaCreacion = System.DateTime.Now;
                tbUsuarios.metodo_registro = "Credenciales";
                db.tbUsuarios.Add(tbUsuarios);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(tbUsuarios);
        }

        public ActionResult EditUsuario(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Login");
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            tbUsuarios tbUsuarios = db.tbUsuarios.Find(id);
            if (tbUsuarios == null) return HttpNotFound();
            return View(tbUsuarios);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditUsuario([Bind(Include = "idUsuario,usuario,contrasena,nombre,apellido,email,fechaCreacion,estado,metodo_registro,foto_perfil,adminRol")] tbUsuarios tbUsuarios)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Login");
            if (ModelState.IsValid)
            {
                db.Entry(tbUsuarios).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(tbUsuarios);
        }

        public ActionResult DeleteUsuario(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Login");
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            tbUsuarios tbUsuarios = db.tbUsuarios.Find(id);
            if (tbUsuarios == null) return HttpNotFound();
            return View(tbUsuarios);
        }

        [HttpPost, ActionName("DeleteUsuario")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteUsuarioConfirmed(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Login");
            tbUsuarios tbUsuarios = db.tbUsuarios.Find(id);
            db.tbUsuarios.Remove(tbUsuarios);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // --- GESTIÓN DE PROYECTOS ---

        public ActionResult DetailsProyecto(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Login");
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            tbProyectos tbProyectos = db.tbProyectos.Find(id);
            if (tbProyectos == null) return HttpNotFound();
            return View(tbProyectos);
        }

        public ActionResult CreateProyecto()
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Login");
            ViewBag.idUsuarioCreador = new SelectList(db.tbUsuarios, "idUsuario", "nombre");
            ViewBag.idMetodologia = new SelectList(db.tbMetodologias, "idMetodologia", "nombre");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateProyecto([Bind(Include = "nombre,fechaInicio,fechaFin,idUsuarioCreador,idMetodologia,estado")] tbProyectos tbProyectos)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Login");
            if (ModelState.IsValid)
            {
                db.tbProyectos.Add(tbProyectos);
                db.SaveChanges();
                int idRolAdminProyecto;
                switch (tbProyectos.idMetodologia)
                {
                    case 1: idRolAdminProyecto = 31; break;
                    case 2: idRolAdminProyecto = 32; break;
                    case 3: idRolAdminProyecto = 33; break;
                    default: idRolAdminProyecto = 31; break;
                }
                tbProyectoUsuario proyectoUsuario = new tbProyectoUsuario { idProyecto = tbProyectos.idProyecto, idUsuario = tbProyectos.idUsuarioCreador, idRol = idRolAdminProyecto };
                db.tbProyectoUsuario.Add(proyectoUsuario);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.idUsuarioCreador = new SelectList(db.tbUsuarios, "idUsuario", "nombre", tbProyectos.idUsuarioCreador);
            ViewBag.idMetodologia = new SelectList(db.tbMetodologias, "idMetodologia", "nombre", tbProyectos.idMetodologia);
            return View(tbProyectos);
        }

        public ActionResult EditProyecto(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Login");
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            tbProyectos tbProyectos = db.tbProyectos.Find(id);
            if (tbProyectos == null) return HttpNotFound();
            var ciclosDisponibles = db.tbCiclos.Where(c => c.idMetodologia == tbProyectos.idMetodologia).ToList();
            ViewBag.CiclosDisponibles = new SelectList(ciclosDisponibles, "codCiclo", "nombre", tbProyectos.codCicloActual);
            return View(tbProyectos);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditProyecto([Bind(Include = "idProyecto,nombre,codCicloActual,estado")] tbProyectos projectData)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Login");
            if (ModelState.IsValid)
            {
                var proyectoOriginal = db.tbProyectos.Find(projectData.idProyecto);
                if (proyectoOriginal == null) return HttpNotFound();
                proyectoOriginal.nombre = projectData.nombre;
                proyectoOriginal.codCicloActual = projectData.codCicloActual;
                proyectoOriginal.estado = projectData.estado;
                db.Entry(proyectoOriginal).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            var ciclosDisponibles = db.tbCiclos.Where(c => c.idMetodologia == projectData.idMetodologia).ToList();
            ViewBag.CiclosDisponibles = new SelectList(ciclosDisponibles, "codCiclo", "nombre", projectData.codCicloActual);
            return View(projectData);
        }

        public ActionResult DeleteProyecto(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Login");
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            tbProyectos tbProyectos = db.tbProyectos.Find(id);
            if (tbProyectos == null) return HttpNotFound();
            return View(tbProyectos);
        }

        [HttpPost, ActionName("DeleteProyecto")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteProyectoConfirmed(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Login");
            tbProyectos tbProyectos = db.tbProyectos.Find(id);
            db.tbProyectos.Remove(tbProyectos);
            db.SaveChanges();
            return RedirectToAction("Index");
        }
       

        // --- GESTIÓN DE METODOLOGIAS ---
        public ActionResult CreateMetodologia() { if (!IsAdmin()) return RedirectToAction("Index", "Login"); return View(); }
        [HttpPost][ValidateAntiForgeryToken] public ActionResult CreateMetodologia([Bind(Include = "idMetodologia,nombre")] tbMetodologias metodologia) { if (!IsAdmin()) return RedirectToAction("Index", "Login"); if (ModelState.IsValid) { db.tbMetodologias.Add(metodologia); db.SaveChanges(); return RedirectToAction("Index"); } return View(metodologia); }
        public ActionResult EditMetodologia(int? id) { if (!IsAdmin()) return RedirectToAction("Index", "Login"); if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest); tbMetodologias metodologia = db.tbMetodologias.Find(id); if (metodologia == null) return HttpNotFound(); return View(metodologia); }
        [HttpPost][ValidateAntiForgeryToken] public ActionResult EditMetodologia(tbMetodologias metodologia) { if (!IsAdmin()) return RedirectToAction("Index", "Login"); if (ModelState.IsValid) { db.Entry(metodologia).State = EntityState.Modified; db.SaveChanges(); return RedirectToAction("Index"); } return View(metodologia); }
        public ActionResult DeleteMetodologia(int? id) { if (!IsAdmin()) return RedirectToAction("Index", "Login"); if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest); tbMetodologias metodologia = db.tbMetodologias.Find(id); if (metodologia == null) return HttpNotFound(); return View(metodologia); }
        [HttpPost, ActionName("DeleteMetodologia")][ValidateAntiForgeryToken] public ActionResult DeleteMetodologiaConfirmed(int id) { if (!IsAdmin()) return RedirectToAction("Index", "Login"); tbMetodologias metodologia = db.tbMetodologias.Find(id); db.tbMetodologias.Remove(metodologia); db.SaveChanges(); return RedirectToAction("Index"); }

        // --- GESTIÓN DE ROLES ---
        public ActionResult CreateRol() { if (!IsAdmin()) return RedirectToAction("Index", "Login"); ViewBag.idMetodologia = new SelectList(db.tbMetodologias, "idMetodologia", "nombre"); return View(); }
        [HttpPost][ValidateAntiForgeryToken] public ActionResult CreateRol([Bind(Include = "idRol,nombre,idMetodologia")] tbRoles rol) { if (!IsAdmin()) return RedirectToAction("Index", "Login"); if (ModelState.IsValid) { db.tbRoles.Add(rol); db.SaveChanges(); return RedirectToAction("Index"); } ViewBag.idMetodologia = new SelectList(db.tbMetodologias, "idMetodologia", "nombre", rol.idMetodologia); return View(rol); }
        public ActionResult EditRol(int? id) { if (!IsAdmin()) return RedirectToAction("Index", "Login"); if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest); tbRoles rol = db.tbRoles.Find(id); if (rol == null) return HttpNotFound(); ViewBag.idMetodologia = new SelectList(db.tbMetodologias, "idMetodologia", "nombre", rol.idMetodologia); return View(rol); }
        [HttpPost][ValidateAntiForgeryToken] public ActionResult EditRol(tbRoles rol) { if (!IsAdmin()) return RedirectToAction("Index", "Login"); if (ModelState.IsValid) { db.Entry(rol).State = EntityState.Modified; db.SaveChanges(); return RedirectToAction("Index"); } ViewBag.idMetodologia = new SelectList(db.tbMetodologias, "idMetodologia", "nombre", rol.idMetodologia); return View(rol); }
        public ActionResult DeleteRol(int? id) { if (!IsAdmin()) return RedirectToAction("Index", "Login"); if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest); tbRoles rol = db.tbRoles.Find(id); if (rol == null) return HttpNotFound(); return View(rol); }
        [HttpPost, ActionName("DeleteRol")][ValidateAntiForgeryToken] public ActionResult DeleteRolConfirmed(int id) { if (!IsAdmin()) return RedirectToAction("Index", "Login"); tbRoles rol = db.tbRoles.Find(id); db.tbRoles.Remove(rol); db.SaveChanges(); return RedirectToAction("Index"); }

        // --- GESTIÓN DE ELEMENTOS ---
        public ActionResult CreateElemento() { if (!IsAdmin()) return RedirectToAction("Index", "Login"); return View(); }
        [HttpPost][ValidateAntiForgeryToken] public ActionResult CreateElemento([Bind(Include = "idElemento,nombre,descripcion")] tbElementos elemento) { if (!IsAdmin()) return RedirectToAction("Index", "Login"); if (ModelState.IsValid) { db.tbElementos.Add(elemento); db.SaveChanges(); return RedirectToAction("Index"); } return View(elemento); }
        public ActionResult EditElemento(int? id) { if (!IsAdmin()) return RedirectToAction("Index", "Login"); if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest); tbElementos elemento = db.tbElementos.Find(id); if (elemento == null) return HttpNotFound(); return View(elemento); }
        [HttpPost][ValidateAntiForgeryToken] public ActionResult EditElemento(tbElementos elemento) { if (!IsAdmin()) return RedirectToAction("Index", "Login"); if (ModelState.IsValid) { db.Entry(elemento).State = EntityState.Modified; db.SaveChanges(); return RedirectToAction("Index"); } return View(elemento); }
        public ActionResult DeleteElemento(int? id) { if (!IsAdmin()) return RedirectToAction("Index", "Login"); if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest); tbElementos elemento = db.tbElementos.Find(id); if (elemento == null) return HttpNotFound(); return View(elemento); }
        [HttpPost, ActionName("DeleteElemento")][ValidateAntiForgeryToken] public ActionResult DeleteElementoConfirmed(int id) { if (!IsAdmin()) return RedirectToAction("Index", "Login"); tbElementos elemento = db.tbElementos.Find(id); db.tbElementos.Remove(elemento); db.SaveChanges(); return RedirectToAction("Index"); }

        // --- GESTIÓN DE CICLOS ---
        public ActionResult CreateCiclo() { if (!IsAdmin()) return RedirectToAction("Index", "Login"); ViewBag.idMetodologia = new SelectList(db.tbMetodologias, "idMetodologia", "nombre"); return View(); }
        [HttpPost][ValidateAntiForgeryToken] public ActionResult CreateCiclo([Bind(Include = "codCiclo,nombre,orden,idMetodologia")] tbCiclos ciclo) { if (!IsAdmin()) return RedirectToAction("Index", "Login"); if (ModelState.IsValid) { db.tbCiclos.Add(ciclo); db.SaveChanges(); return RedirectToAction("Index"); } ViewBag.idMetodologia = new SelectList(db.tbMetodologias, "idMetodologia", "nombre", ciclo.idMetodologia); return View(ciclo); }
        public ActionResult EditCiclo(string id) { if (!IsAdmin()) return RedirectToAction("Index", "Login"); if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest); tbCiclos ciclo = db.tbCiclos.Find(id); if (ciclo == null) return HttpNotFound(); ViewBag.idMetodologia = new SelectList(db.tbMetodologias, "idMetodologia", "nombre", ciclo.idMetodologia); return View(ciclo); }
        [HttpPost][ValidateAntiForgeryToken] public ActionResult EditCiclo(tbCiclos ciclo) { if (!IsAdmin()) return RedirectToAction("Index", "Login"); if (ModelState.IsValid) { db.Entry(ciclo).State = EntityState.Modified; db.SaveChanges(); return RedirectToAction("Index"); } ViewBag.idMetodologia = new SelectList(db.tbMetodologias, "idMetodologia", "nombre", ciclo.idMetodologia); return View(ciclo); }
        public ActionResult DeleteCiclo(string id) { if (!IsAdmin()) return RedirectToAction("Index", "Login"); if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest); tbCiclos ciclo = db.tbCiclos.Find(id); if (ciclo == null) return HttpNotFound(); return View(ciclo); }
        [HttpPost, ActionName("DeleteCiclo")][ValidateAntiForgeryToken] public ActionResult DeleteCicloConfirmed(string id) { if (!IsAdmin()) return RedirectToAction("Index", "Login"); tbCiclos ciclo = db.tbCiclos.Find(id); db.tbCiclos.Remove(ciclo); db.SaveChanges(); return RedirectToAction("Index"); }

        public ActionResult TestGraph()
        {
            // 1. Crear el ViewModel de prueba.
            var viewModel = new TestGraphViewModel();

            // 2. Crear datos de prueba (Hardcoded).
            // Usaremos una función local para convertir fechas a timestamps.
            long ToJsTimestamp(DateTime dt) => (long)(dt.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;

            var hoy = DateTime.Today;
            var timestampHoy = ToJsTimestamp(hoy);
            var timestampAyer = ToJsTimestamp(hoy.AddDays(-1));
            var timestampSemanaPasada = ToJsTimestamp(hoy.AddDays(-7));

            // Datos para la línea del gráfico
            viewModel.ProyectosTimeline.Add(new object[] { timestampSemanaPasada, 5 });
            viewModel.ProyectosTimeline.Add(new object[] { timestampAyer, 8 });
            viewModel.ProyectosTimeline.Add(new object[] { timestampHoy, 10 });

            // Datos para los "flags" o eventos en la línea
            viewModel.TimelineFlags.Add(new FlagEvent
            {
                x = timestampAyer, // El flag aparecerá en el punto de "ayer"
                title = "A",
                text = "Evento de prueba A"
            });

            viewModel.TimelineFlags.Add(new FlagEvent
            {
                x = timestampHoy, // El flag aparecerá en el punto de "hoy"
                title = "B",
                text = "Evento de prueba B"
            });


            // 3. Devolver la vista de prueba con los datos.
            return View("TestGraph", viewModel);
        }
        public ActionResult DemoGraph()
        {
            return View();
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}