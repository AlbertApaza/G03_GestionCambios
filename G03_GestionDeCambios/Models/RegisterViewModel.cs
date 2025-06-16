using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace G03_GestionDeCambios.Models
{
    public class RegisterViewModel
    {
        [Display(Name = "Nombre")]
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(50)]
        public string Nombre { get; set; }

        [Display(Name = "Apellido")]
        [StringLength(50)]
        public string Apellido { get; set; }

        [Display(Name = "Correo Electrónico")]
        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Formato de correo electrónico no válido.")]
        [StringLength(50)]
        public string Email { get; set; }

        [Display(Name = "Contraseña")]
        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [StringLength(50, ErrorMessage = "La {0} debe tener al menos {2} caracteres de longitud.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Contrasena { get; set; }

        [Display(Name = "Confirmar Contraseña")]
        [DataType(DataType.Password)]
        [Compare("Contrasena", ErrorMessage = "La contraseña y la confirmación de contraseña no coinciden.")]
        public string ConfirmarContrasena { get; set; }
    }
}