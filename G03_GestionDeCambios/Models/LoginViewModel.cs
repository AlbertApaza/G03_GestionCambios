using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace G03_GestionDeCambios.Models
{
    public class LoginViewModel
    {
        [Display(Name = "Correo Electrónico")]
        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Formato de correo electrónico no válido.")]
        public string Email { get; set; } // Cambiado de Usuario a Email

        [Display(Name = "Contraseña")]
        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        public string Contrasena { get; set; }
    }
}