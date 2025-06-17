using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace G03_GestionDeCambios.Models
{
    public class HomeViewModel
    {
        public string NombreCompleto { get; set; }
        public int CantidadMisProyectos { get; set; }
        public int CantidadMisTareasPendientes { get; set; }
        public bool EsAdminDeAlgunProyecto { get; set; }

        // --- AÑADIMOS LA LISTA DE TAREAS AGRUPADAS ---
        public List<ProyectoConTareasViewModel> ProyectosConTareas { get; set; }

        public HomeViewModel()
        {
            ProyectosConTareas = new List<ProyectoConTareasViewModel>();
        }
    }

    // --- VIEWMODELS DE SOPORTE (pueden estar en el mismo archivo o en otro) ---
    public class ProyectoConTareasViewModel
    {
        public int IdProyecto { get; set; }
        public string NombreProyecto { get; set; }
        public List<TareaViewModel> Tareas { get; set; }
    }

    public class TareaViewModel
    {
        public string NombreTarea { get; set; }
        public string Descripcion { get; set; }
        public string Estado { get; set; }
        public string NombreCiclo { get; set; }
        public string UsuarioAsignado { get; set; }
    }
}