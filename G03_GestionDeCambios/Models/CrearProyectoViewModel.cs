using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace G03_GestionDeCambios.Models
{
    public class CrearProyectoViewModel
    {
        [Display(Name = "Nombre del Proyecto")]
        [Required(ErrorMessage = "El nombre del proyecto es obligatorio.")]
        public string Nombre { get; set; }

        [Display(Name = "Fecha de Inicio del Proyecto")]
        [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
        public DateTime FechaInicio { get; set; }
        [Display(Name = "Fecha de Fin del Proyecto")]
        public DateTime? FechaFin { get; set; }
        public int IdUsuarioCreador { get; set; }
        public int IdMetodologia { get; set; }
        //[Display(Name = "Ciclo Actual")]
        public string CodCicloActual { get; set; }


        public List<SelectListItem> Usuarios { get; set; }
        public List<SelectListItem> Roles { get; set; } 
        public List<SelectListItem> Metodologias { get; set; }
        [Display(Name = "Ciclo Actual")]
        public List<SelectListItem> Ciclos { get; set; } = new List<SelectListItem>();
    }

    public class ProyectoElementosViewModel
    {
        public int IdProyecto { get; set; }
        public string NombreProyecto { get; set; }
        public string NombreMetodologia { get; set; }

        [DataType(DataType.Date)]
        public DateTime FechaInicioProyecto { get; set; }
        [DataType(DataType.Date)]
        public DateTime? FechaFinProyecto { get; set; }

        public List<CicloGestionViewModel> CiclosDelProyecto { get; set; }
        public List<SelectListItem> TodosLosElementosDisponibles { get; set; }
        public List<SelectListItem> RolesDisponiblesParaElementos { get; set; }

        public ProyectoElementosViewModel()
        {
            CiclosDelProyecto = new List<CicloGestionViewModel>();
            TodosLosElementosDisponibles = new List<SelectListItem>();
            RolesDisponiblesParaElementos = new List<SelectListItem>();
        }
    }

    public class ElementoParaAsignarViewModel
    {
        public int IdElemento { get; set; }
        public string NombreElemento { get; set; }
        public bool Seleccionado { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public int? IdRol { get; set; } 

        public string CodCicloSeleccionado { get; set; }
    }

    public class ProyectoUsuariosViewModel
    {
        public int IdProyecto { get; set; }
        public string NombreProyecto { get; set; }
        public List<UsuarioAsignadoViewModel> UsuariosAsignados { get; set; }
        public List<SelectListItem> TodosLosUsuarios { get; set; }
        public int? UsuarioAAgregarId { get; set; }
        public List<SelectListItem> RolesDisponibles { get; set; }
        public int? RolParaNuevoUsuarioId { get; set; }

        public ProyectoUsuariosViewModel()
        {
            UsuariosAsignados = new List<UsuarioAsignadoViewModel>();
            TodosLosUsuarios = new List<SelectListItem>();
            RolesDisponibles = new List<SelectListItem>();
        }
    }

    public class UsuarioAsignadoViewModel
    {
        public int IdUsuario { get; set; }
        public string NombreCompletoUsuario { get; set; }
        public string EmailUsuario { get; set; }
        public int? IdRol { get; set; }
        public string NombreRol { get; set; }
    }


    public class ProyectoGestionElementosViewModel
    {
        public int IdProyecto { get; set; }
        public string NombreProyecto { get; set; }
        public string NombreMetodologia { get; set; }

        public List<CicloGestionViewModel> CiclosDelProyecto { get; set; }
        public List<SelectListItem> TodosLosElementosDisponibles { get; set; }

        public ProyectoGestionElementosViewModel()
        {
            CiclosDelProyecto = new List<CicloGestionViewModel>();
            TodosLosElementosDisponibles = new List<SelectListItem>();
        }
    }

    public class CicloGestionViewModel
    {
        public string CodCiclo { get; set; }
        public string NombreCiclo { get; set; }
        public int OrdenCiclo { get; set; }
        public int IdProyectoCiclo { get; set; }

        [Display(Name = "Inicio del Ciclo")]
        [DataType(DataType.Date)]
        public DateTime? FechaInicioCiclo { get; set; }

        [Display(Name = "Fin del Ciclo")]
        [DataType(DataType.Date)]
        public DateTime? FechaFinCiclo { get; set; }

        public List<ElementoAsignadoCicloViewModel> ElementosAsignados { get; set; }

        [Display(Name = "Elemento a Agregar")]
        public int? IdElementoAAgregar { get; set; }
        [Display(Name = "Fecha Inicio Elemento")]
        [DataType(DataType.Date)]
        public DateTime? FechaInicioNuevoElemento { get; set; }
        [Display(Name = "Fecha Fin Elemento")]
        [DataType(DataType.Date)]
        public DateTime? FechaFinNuevoElemento { get; set; }

        [Display(Name = "Rol encargado")]
        public int? IdRolNuevoElemento { get; set; }

        public CicloGestionViewModel()
        {
            ElementosAsignados = new List<ElementoAsignadoCicloViewModel>();
        }
    }

    public class ElementoAsignadoCicloViewModel
    {
        public int IdProyectoElemento { get; set; }
        public int IdElemento { get; set; }
        public string NombreElemento { get; set; }

        [Display(Name = "Fecha Inicio")]
        [DataType(DataType.Date)]
        public DateTime? FechaInicioElemento { get; set; }

        [Display(Name = "Fecha Fin")]
        [DataType(DataType.Date)]
        public DateTime? FechaFinElemento { get; set; }

        public int? IdRol { get; set; } 
        public string NombreRol { get; set; }

        public string CodCiclo { get; set; }
        public bool MarcadoParaEliminar { get; set; }
    }
}