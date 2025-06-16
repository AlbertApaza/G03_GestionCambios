using System.Web.Mvc;
using G03_GestionDeCambios.Service;
using G03_GestionDeCambios.Models; // <-- Necesita este using

namespace G03_GestionDeCambios.Controllers
{
    public class EstadisticasController : Controller
    {
        private readonly EstadisticasService _estadisticasService;

        public EstadisticasController()
        {
            _estadisticasService = new EstadisticasService();
        }

        public ActionResult Index(int id)
        {
            var viewModel = _estadisticasService.GetEstadisticasProyecto(id);
            if (viewModel == null)
            {
                return HttpNotFound("Proyecto no encontrado.");
            }
            return View(viewModel);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _estadisticasService.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}