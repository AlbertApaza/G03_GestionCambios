namespace G03_GestionDeCambios.Service
{
    using G03_GestionDeCambios.Models;
    using System.Data.Entity;
    using System.Linq;

    public class AdministradorService
    {
        private readonly BD_GestionDeCambiosEntities db = new BD_GestionDeCambiosEntities();

        public AdministradorDashboardViewModel GetDashboardData()
        {
            var viewModel = new AdministradorDashboardViewModel
            {
                Usuarios = db.tbUsuarios.ToList(),
                Proyectos = db.tbProyectos.Include(p => p.tbUsuarios).Include(p => p.tbMetodologias).ToList(),
                SolicitudesCambio = db.tbSolicitudesCambio.Include(s => s.tbProyectos).Include(s => s.tbUsuarios).ToList(),
                Documentos = db.tbDocumentos.Include(d => d.tbProyectos).Include(d => d.tbUsuarios).ToList(),
                Tareas = db.tbTareas.Include(t => t.tbUsuarios).Include(t => t.tbProyectoElemento).ToList(),
                Metodologias = db.tbMetodologias.ToList(),
                Roles = db.tbRoles.Include(r => r.tbMetodologias).ToList(), // Corregido aquí
                Elementos = db.tbElementos.ToList(),
                Ciclos = db.tbCiclos.Include(c => c.tbMetodologias).ToList() // Y corregido aquí
            };

            return viewModel;
        }

        public tbUsuarios GetUserById(int id)
        {
            return db.tbUsuarios.Find(id);
        }
    }
}