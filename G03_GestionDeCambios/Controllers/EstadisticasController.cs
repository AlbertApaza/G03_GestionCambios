using System.Web.Mvc;
using G03_GestionDeCambios.Service;
using G03_GestionDeCambios.Models;

namespace G03_GestionDeCambios.Controllers
{
    public class EstadisticasController : Controller
    {
        private readonly EstadisticasService _estadisticasService;

        public EstadisticasController()
        {
            _estadisticasService = new EstadisticasService();
        }

        // Tu acción existente para la página de estadísticas
        public ActionResult Index(int id)
        {
            var viewModel = _estadisticasService.GetEstadisticasProyecto(id);
            if (viewModel == null)
            {
                return HttpNotFound("Proyecto no encontrado.");
            }
            return View(viewModel);
        }

        // --- ACCIÓN NUEVA AÑADIDA ---
        // ===================================================================
        public ActionResult ListaSolicitudes(int id) // 'id' es el idProyecto
        {
            var solicitudesViewModel = _estadisticasService.GetSolicitudesProyecto(id);
            // Pasamos el IdProyecto a la vista por si lo necesitamos para un título o un botón de "volver"
            ViewBag.ProjectId = id;
            return View(solicitudesViewModel);
        }


        // === MODIFICA LA ACCIÓN GenerarInformeEstado =======================
        // Ahora recibe el ID de la solicitud, no del proyecto.
        public ActionResult GenerarInformeEstado(int idSolicitud) // <-- PARÁMETRO CAMBIADO
        {
            // Llama al servicio con el id de la solicitud específica
            var viewModel = _estadisticasService.GetInformeEstadoData(idSolicitud); // <-- PARÁMETRO CAMBIADO

            if (viewModel == null)
            {
                return HttpNotFound("La solicitud de cambio especificada no fue encontrada.");
            }

            return View("InformeEstado", viewModel);
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