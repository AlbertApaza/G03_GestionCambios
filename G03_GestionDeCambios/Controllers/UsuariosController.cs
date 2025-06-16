using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using G03_GestionDeCambios.Models;
using G03_GestionDeCambios.Service;

namespace G03_GestionDeCambios.Controllers
{
    public class UsuariosController : Controller
    {
        ProyectoUsuarioService _pyUsuarioService = new ProyectoUsuarioService();
        ProyectoService _proyectoService = new ProyectoService();
        LoginService _loginService = new LoginService();

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Usuarios(int idProyecto)
        {
            ViewBag.ProjectId = idProyecto;
            var proyecto = _proyectoService.ObtenerProyectoPorId(idProyecto);
            if (proyecto == null)
            {
                TempData["Error"] = "Proyecto no encontrado.";
                return RedirectToAction("Index", "Home");
            }
            var viewModel = new ProyectoUsuariosViewModel
            {
                IdProyecto = proyecto.idProyecto,
                NombreProyecto = proyecto.nombre,
                UsuariosAsignados = _pyUsuarioService.ObtenerUsuariosPorProyecto(idProyecto),
                TodosLosUsuarios = _pyUsuarioService.ObtenerTodosLosUsuariosParaDropdown(),
                RolesDisponibles = _pyUsuarioService.ObtenerRolesPorMetodologia(proyecto.idMetodologia)
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AgregarUsuario(ProyectoUsuariosViewModel model)
        {
            if (!model.UsuarioAAgregarId.HasValue || !model.RolParaNuevoUsuarioId.HasValue)
            {
                TempData["Error"] = "Debe seleccionar un usuario y un rol.";
                return RedirectToAction("Usuarios", new { idProyecto = model.IdProyecto });
            }
            try
            {
                _pyUsuarioService.AgregarUsuarioAProyecto(
                    model.IdProyecto,
                    model.UsuarioAAgregarId.Value,
                    model.RolParaNuevoUsuarioId.Value
                );
                TempData["Exito"] = "Usuario agregado al proyecto correctamente con su rol.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al agregar usuario: " + ex.Message;
            }
            return RedirectToAction("Usuarios", new { idProyecto = model.IdProyecto });
        }

        public ActionResult Profile()
        {
            if (Session["idUsuario"] == null)
            {
                return RedirectToAction("Index", "Login", new { returnUrl = Url.Action("Profile", "Usuarios") });
            }
            int idUsuarioLogueado = (int)Session["idUsuario"];
            var usuarioDb = _loginService.ObtenerUsuarioPorId(idUsuarioLogueado);
            if (usuarioDb == null)
            {
                TempData["ErrorMessage"] = "No se pudo cargar la información del perfil.";
                Session.Clear();
                return RedirectToAction("Index", "Login");
            }
            var viewModel = new UsuarioViewModel
            {
                IdUsuario = usuarioDb.idUsuario,
                NombreUsuarioLogin = usuarioDb.usuario,
                NombreCompletoDisplay = $"{usuarioDb.nombre} {usuarioDb.apellido}".Trim(),
                Email = usuarioDb.email,
                FotoPerfilAlmacenada = usuarioDb.foto_perfil, // Asigna el valor almacenado
                FechaCreacion = usuarioDb.fechaCreacion,
                MetodoRegistro = usuarioDb.metodo_registro,
                CantidadProyectos = _loginService.ContarProyectosDeUsuario(idUsuarioLogueado)
            };
            return View(viewModel);
        }

        public ActionResult Settings()
        {
            if (Session["idUsuario"] == null)
            {
                return RedirectToAction("Index", "Login", new { returnUrl = Url.Action("Settings", "Usuarios") });
            }
            int idUsuarioLogueado = (int)Session["idUsuario"];
            var usuarioDb = _loginService.ObtenerUsuarioPorId(idUsuarioLogueado);
            if (usuarioDb == null)
            {
                TempData["ErrorMessage"] = "No se pudo cargar la configuración del perfil.";
                Session.Clear();
                return RedirectToAction("Index", "Login");
            }
            var viewModel = new UsuarioViewModel
            {
                IdUsuario = usuarioDb.idUsuario,
                NombreUsuarioEditable = usuarioDb.usuario,
                NombrePilaEditable = usuarioDb.nombre,
                ApellidoEditable = usuarioDb.apellido,
                Email = usuarioDb.email,
                FotoPerfilAlmacenada = usuarioDb.foto_perfil, // Asigna el valor almacenado
                FechaCreacion = usuarioDb.fechaCreacion,
                MetodoRegistro = usuarioDb.metodo_registro
            };
            if (TempData["SuccessMessage"] != null) ViewBag.SuccessMessage = TempData["SuccessMessage"];
            if (TempData["ErrorMessage"] != null) ViewBag.ErrorMessage = TempData["ErrorMessage"];
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Settings(UsuarioViewModel model)
        {
            if (Session["idUsuario"] == null || (int)Session["idUsuario"] != model.IdUsuario)
            {
                return new HttpUnauthorizedResult();
            }

            if (model.FotoSubida != null && model.FotoSubida.ContentLength > 0)
            {
                if (model.FotoSubida.ContentLength > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("FotoSubida", "El archivo es demasiado grande (máximo 5MB).");
                }
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(model.FotoSubida.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("FotoSubida", "Formato de archivo no permitido. Solo se aceptan JPG, PNG, GIF.");
                }
            }

            if (model.MetodoRegistro == "Google" && !string.IsNullOrWhiteSpace(model.NuevaContrasena))
            {
                ModelState.AddModelError("NuevaContrasena", "No puedes cambiar la contraseña para cuentas registradas con Google.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    bool success = _loginService.ActualizarPerfilUsuario(
                        model.IdUsuario,
                        model.NombrePilaEditable,
                        model.ApellidoEditable,
                        model.NombreUsuarioEditable,
                        (model.MetodoRegistro == "Credenciales" ? model.NuevaContrasena : null),
                        model.LinkFotoPerfil,
                        model.FotoSubida,
                        model.MetodoRegistro
                    );

                    if (success)
                    {
                        TempData["SuccessMessage"] = "Perfil actualizado correctamente.";
                        Session["usuario"] = model.NombreUsuarioEditable;
                        Session["nombreCompleto"] = $"{model.NombrePilaEditable} {model.ApellidoEditable}".Trim();
                        var usuarioActualizado = _loginService.ObtenerUsuarioPorId(model.IdUsuario);
                        if (usuarioActualizado != null)
                        {
                            // Actualizar la sesión con el nombre de archivo/URL de Google que está en la BD
                            Session["fotoPerfil"] = usuarioActualizado.foto_perfil;
                        }
                        return RedirectToAction("Settings");
                    }
                    else
                    {
                        ModelState.AddModelError("", "No se pudo actualizar el perfil. Inténtalo de nuevo.");
                    }
                }
                catch (InvalidOperationException exOp)
                {
                    ModelState.AddModelError("", exOp.Message);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Ocurrió un error inesperado al actualizar el perfil: " + ex.Message);
                }
            }
            var usuarioDbError = _loginService.ObtenerUsuarioPorId(model.IdUsuario);
            model.FotoPerfilAlmacenada = usuarioDbError?.foto_perfil;
            model.Email = usuarioDbError?.email;
            model.FechaCreacion = usuarioDbError?.fechaCreacion;
            // model.MetodoRegistro ya está en el modelo y no debería cambiar en un POST fallido
            return View(model);
        }
    }
}