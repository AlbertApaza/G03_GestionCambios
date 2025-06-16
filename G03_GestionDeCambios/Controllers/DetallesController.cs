using System.Web.Mvc;
using G03_GestionDeCambios.Service;
using G03_GestionDeCambios.ViewModels.DetallesViewModels;

namespace G03_GestionDeCambios.Controllers
{
    public class DetallesController : Controller
    {
        private readonly DetallesService _detallesService;

        public DetallesController()
        {
            _detallesService = new DetallesService();
        }

        // --- CAMBIO DENTRO DE LA ACCIÓN INDEX ---
        public ActionResult Index(int idProyecto)
        {
            // La acción ahora llama al nuevo método del servicio que devuelve el ViewModel completo.
            var viewModel = _detallesService.GetDetallesProyectoDashboard(idProyecto);
            if (viewModel == null)
            {
                return HttpNotFound("Proyecto no encontrado.");
            }

            return View(viewModel);
        }

        // --- EL RESTO DEL CONTROLADOR NO CAMBIA ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CambiarCiclo(int idProyecto, string CodCicloActual)
        {
            if (idProyecto <= 0)
            {
                TempData["ErrorMessage"] = "ID de Proyecto inválido.";
                return RedirectToAction("Index", "Proyecto");
            }

            if (string.IsNullOrEmpty(CodCicloActual))
            {
                TempData["ErrorMessage"] = "No se seleccionó un ciclo válido.";
                return RedirectToAction("Index", new { idProyecto = idProyecto });
            }

            bool actualizado = _detallesService.ActualizarCicloActualProyecto(idProyecto, CodCicloActual);

            if (actualizado)
            {
                TempData["SuccessMessage"] = "Ciclo del proyecto actualizado correctamente.";
            }
            else
            {
                TempData["ErrorMessage"] = "Error al actualizar el ciclo del proyecto.";
            }

            return RedirectToAction("Index", new { idProyecto = idProyecto });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _detallesService.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}