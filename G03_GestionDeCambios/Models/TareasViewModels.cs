using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace G03_GestionDeCambios.ViewModels.TareasViewModels
{
    public class ProyectoElementoViewModel
    {
        public int IdProyectoElemento { get; set; }
        public string NombreElemento { get; set; }
        public DateTime FechaInicio { get; set; } // Asumo que no será null para un elemento elegible
        public DateTime? FechaFin { get; set; }
        public string EstadoElemento { get; set; }
        public string NombreRolAsignadoAlElemento { get; set; }
        public int? IdRolAsignadoAlElemento { get; set; }
    }


    public class UsuarioDisponibleViewModel
    {
        public int IdUsuario { get; set; }
        public string NombreCompletoUsuario { get; set; }
    }


    public class CrearTareaViewModel
    {
        [Required]
        public int IdProyecto { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un elemento de configuración.")]
        [Display(Name = "Elemento de Configuración")]
        public int IdProyectoElemento { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un usuario.")]
        [Display(Name = "Usuario Asignado")]
        public int IdUsuario { get; set; }

        [Required(ErrorMessage = "El nombre de la tarea es obligatorio.")]
        [StringLength(50, ErrorMessage = "El nombre no puede exceder los 50 caracteres.")]
        [Display(Name = "Nombre de la Tarea")]
        public string NombreTarea { get; set; } 

        [Display(Name = "Descripción")]
        [DataType(DataType.MultilineText)]
        public string DescripcionTarea { get; set; }

        // Para pasar datos a la vista
        public string NombreProyecto { get; set; }
        public string NombreElementoSeleccionado { get; set; }
        public string NombreUsuarioSeleccionado { get; set; }
    }


    public class TareaDetalleViewModel
    {
        public int IdTarea { get; set; }
        public string NombreTarea { get; set; }
        public string DescripcionTarea { get; set; }
        public string EstadoTarea { get; set; }

        public int IdUsuarioAsignado { get; set; } // IMPORTANTE
        public string NombreUsuarioAsignado { get; set; }

        public int IdProyectoElemento { get; set; } // IMPORTANTE
        public string NombreElementoAsociado { get; set; }
        public DateTime? FechaInicioElemento { get; set; }
        public DateTime? FechaFinElemento { get; set; }
        public string EstadoElemento { get; set; }
        public string RolUsuarioEnElemento { get; set; } // Rol requerido por el elemento
    }

    public class TareasIndexViewModel
    {
        public int IdProyecto { get; set; }
        public string NombreProyecto { get; set; }
        public string CicloActual { get; set; }
        public List<ProyectoElementoViewModel> ElementosConfiguracion { get; set; }
        public CrearTareaViewModel FormularioCrearTarea { get; set; }

        // Para los dropdowns en el formulario de creación
        public IEnumerable<SelectListItem> ElementosSelectList { get; set; }
        public IEnumerable<SelectListItem> UsuariosSelectList { get; set; }

        //  Lista de tareas existentes
        public List<TareaDetalleViewModel> TareasExistentes { get; set; }

        public TareasIndexViewModel()
        {
            ElementosConfiguracion = new List<ProyectoElementoViewModel>();
            FormularioCrearTarea = new CrearTareaViewModel();
            ElementosSelectList = new List<SelectListItem>();
            UsuariosSelectList = new List<SelectListItem>();
            TareasExistentes = new List<TareaDetalleViewModel>(); // Inicializar
        }
    }

}