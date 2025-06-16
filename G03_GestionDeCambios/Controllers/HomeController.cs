using System.Web.Mvc;
using G03_GestionDeCambios.Service;
using G03_GestionDeCambios.Models;

namespace G03_GestionDeCambios.Controllers
{
    public class HomeController : Controller
    {
        private readonly DashboardService _dashboardService;

        public HomeController()
        {
            _dashboardService = new DashboardService();
        }

        public ActionResult Index()
        {
            if (Session["idUsuario"] == null)
            {
                return RedirectToAction("Index", "Login");
            }

            int idUsuario = (int)Session["idUsuario"];

            var viewModel = _dashboardService.GetHomeData(idUsuario);

            return View(viewModel);
        }

        public ActionResult Dashboard()
        {
            if (Session["idUsuario"] == null)
            {
                return RedirectToAction("Index", "Login");
            }

            int idUsuarioAdmin = (int)Session["idUsuario"];
            var viewModel = _dashboardService.GetDashboardData(idUsuarioAdmin);
            return View(viewModel);
        }

        [HttpGet]
        public ActionResult GetDetalleUsuario(int id)
        {
            var detalleProyectos = _dashboardService.GetUsuarioProyectosTareas(id);
            return PartialView("_UsuarioProyectosTareas", detalleProyectos);
        }

        [HttpGet]
        public ActionResult GetProyectosDeMiembro(int id)
        {
            if (Session["idUsuario"] == null)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.Unauthorized);
            }
            int idUsuarioAdmin = (int)Session["idUsuario"];

            var proyectos = _dashboardService.GetProyectosDeMiembro(id, idUsuarioAdmin);
            return PartialView("_UsuarioProyectos", proyectos);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _dashboardService.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}