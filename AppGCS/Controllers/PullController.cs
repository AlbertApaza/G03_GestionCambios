using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace AppGCS.Controllers
{
    public class PullController : Controller
    {
        // Simulación de Pull Requests registrados
        private static List<PullRequest> PRRegistrados = new List<PullRequest>();

        private static List<string> RevisoresDisponibles = new List<string>
        {
            "QA_Jimenez", "QA_Rivera", "Dev_Aragon", "Responsable_Perez"
        };

        [HttpGet]
        public ActionResult Index()
        {
            ViewBag.Revisores = new MultiSelectList(RevisoresDisponibles);
            return View();
        }

        [HttpPost]
        public ActionResult Index(string nombreRama, string enlacePR, string estadoPR, string[] revisores)
        {
            ViewBag.Revisores = new MultiSelectList(RevisoresDisponibles, revisores);

            if (string.IsNullOrWhiteSpace(nombreRama) || string.IsNullOrWhiteSpace(enlacePR) || revisores.Length == 0)
            {
                ViewBag.Mensaje = "Todos los campos son obligatorios.";
                ViewBag.Estado = "alert-danger";
                return View();
            }

            if (!enlacePR.StartsWith("https://github.com/"))
            {
                ViewBag.Mensaje = "El enlace debe ser válido y comenzar con https://github.com/";
                ViewBag.Estado = "alert-warning";
                return View();
            }

            if (PRRegistrados.Exists(p => p.EnlacePR.Equals(enlacePR.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                ViewBag.Mensaje = "Este Pull Request ya fue registrado.";
                ViewBag.Estado = "alert-danger";
                return View();
            }

            // Agregamos el PR simulado
            PRRegistrados.Add(new PullRequest
            {
                NombreRama = nombreRama.Trim(),
                EnlacePR = enlacePR.Trim(),
                EstadoPR = estadoPR,
                Revisores = revisores
            });

            TempData["MensajeExito"] = "✔ Pull Request registrado correctamente.";
            return RedirectToAction("Listar");
        }

        [HttpGet]
        public ActionResult Listar()
        {
            return View(PRRegistrados);
        }

        // Clase interna para simular datos de un Pull Request
        public class PullRequest
        {
            public string NombreRama { get; set; }
            public string EnlacePR { get; set; }
            public string EstadoPR { get; set; }
            public string[] Revisores { get; set; }
        }
    }
}