using System.Web.Mvc;
using G03_GestionDeCambios.Service; // Asumiendo que tendrás un servicio para esto

namespace G03_GestionDeCambios.Controllers
{
    public class CronogramaController : Controller
    {
        private readonly CronogramaService _cronogramaService; 

        public CronogramaController()
        {
            _cronogramaService = new CronogramaService();
        }

        public ActionResult Proyecto(int id) // Nombre del parámetro 'id'
        {
            ViewBag.IdProyecto = id;
            ViewBag.ProjectId = id;
            var nombreProyecto = _cronogramaService.GetNombreProyecto(id);
            if (string.IsNullOrEmpty(nombreProyecto))
            {
                return HttpNotFound("Proyecto no encontrado.");
            }
            ViewBag.NombreProyecto = nombreProyecto;
            return View(); // Vista Proyecto.cshtml
        }

        [HttpGet]
        public JsonResult GanttData(int id) // Nombre del parámetro 'id'
        {
            var data = _cronogramaService.GetGanttDataParaProyecto(id);
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cronogramaService.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}