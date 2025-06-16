using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using G03_GestionDeCambios.Service;

namespace G03_GestionDeCambios.Models
{
    public class UsuarioViewModel
    {
        public int IdUsuario { get; set; }

        [Display(Name = "Correo Electrónico")]
        public string Email { get; set; }

        [Display(Name = "Fecha de Creación")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}", ApplyFormatInEditMode = true)]
        public DateTime? FechaCreacion { get; set; }

        public string MetodoRegistro { get; set; }

        // Esta propiedad almacenará el nombre del archivo o la URL completa de Google/link
        public string FotoPerfilAlmacenada { get; set; }

        // Esta propiedad se usará en las vistas para mostrar la imagen
        public string FotoPerfilUrlCompleta
        {
            get { return LoginService.GetFullPhotoUrl(FotoPerfilAlmacenada); }
        }


        [Display(Name = "Nombre de Usuario (Login)")]
        public string NombreUsuarioLogin { get; set; }

        [Display(Name = "Nombre Completo")]
        public string NombreCompletoDisplay { get; set; }

        [Display(Name = "Cantidad de Proyectos")]
        public int CantidadProyectos { get; set; }


        [Display(Name = "Nombre de Usuario (para login)")]
        [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
        [StringLength(50)]
        public string NombreUsuarioEditable { get; set; }

        [Display(Name = "Nombre(s)")]
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(50)]
        public string NombrePilaEditable { get; set; }

        [Display(Name = "Apellido(s)")]
        [StringLength(50)]
        public string ApellidoEditable { get; set; }

        [Display(Name = "Nueva Contraseña (dejar en blanco para no cambiar)")]
        [StringLength(50, ErrorMessage = "La {0} debe tener al menos {2} caracteres de longitud.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string NuevaContrasena { get; set; }

        [Display(Name = "Confirmar Nueva Contraseña")]
        [DataType(DataType.Password)]
        [Compare("NuevaContrasena", ErrorMessage = "La nueva contraseña y la confirmación no coinciden.")]
        public string ConfirmarNuevaContrasena { get; set; }

        [Display(Name = "Enlace a Foto de Perfil (URL)")]
        [Url(ErrorMessage = "Por favor, introduce una URL válida.")]
        public string LinkFotoPerfil { get; set; }

        [Display(Name = "Subir Nueva Foto de Perfil (opcional)")]
        public HttpPostedFileBase FotoSubida { get; set; }
    }
}