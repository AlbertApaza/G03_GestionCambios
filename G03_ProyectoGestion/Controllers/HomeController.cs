using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using G03_ProyectoGestion.Services;
using G03_ProyectoGestion.Models;

namespace G03_ProyectoGestion.Controllers
{
    public class HomeController : Controller
    {
        private UsuarioService usuarioService = new UsuarioService();
        // GET: Home
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Login(tbUsuarios usuario)
        {
            // Validamos si el usuario existe con el nombre de usuario y la contraseña
            var usuarioModelo = usuarioService.Login(usuario);

            if (usuarioModelo != null)
            {
                // Si el usuario es encontrado, se guarda el idUsuario en la sesión
                Session["idUsuario"] = usuarioModelo.idUsuario;

                // Redirigimos al "Index" o a una página principal
                return RedirectToAction("Index");
            }
            else
            {
                // Si no existe el usuario, regresamos al login con un mensaje de error
                ModelState.AddModelError("", "Usuario o contraseña incorrectos.");
                return View(usuario);
            }
        }
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Register(tbUsuarios usuario)
        {
            if (ModelState.IsValid)
            {
                // Registramos al nuevo usuario
                usuarioService.Registro(usuario);

                // Redirigimos a la página de login después del registro
                return RedirectToAction("Login");
            }

            // Si el modelo no es válido, volvemos a mostrar la vista con los errores de validación
            return View(usuario);
        }
    }
}