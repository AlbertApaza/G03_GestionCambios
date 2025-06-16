using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace G03_GestionDeCambios.Models
{
    public class DocumentoInfoViewModel
    {
        public int IdDocumento { get; set; }
        public string NombreArchivoOriginal { get; set; }
        public string NombreArchivoEnServidor { get; set; } // El nombre con el que se guarda en el VPS
        public string Version { get; set; }
        public string Estado { get; set; }
        public DateTime FechaSubida { get; set; }
        public string Comentarios { get; set; }
        public string NombreUsuarioSubida { get; set; }
        public string UrlDescarga { get; set; } // Se construirá para el cliente
    }

    public class DocumentosIndexViewModel
    {
        public int IdProyecto { get; set; }
        public string NombreProyecto { get; set; }
        public string CodCicloActual { get; set; }
        public string NombreCicloActual { get; set; }
        public List<DocumentoInfoViewModel> Documentos { get; set; }

        [Display(Name = "Archivo a Subir")]
        [Required(ErrorMessage = "Debe seleccionar un archivo.")]
        public HttpPostedFileBase ArchivoSubido { get; set; }

        [Display(Name = "Versión (ej. 1.0)")]
        [StringLength(10)]
        public string VersionDocumento { get; set; }

        [Display(Name = "Comentarios (opcional)")]
        [DataType(DataType.MultilineText)]
        public string ComentariosDocumento { get; set; }

        public bool PuedeSubirDocumentos { get; set; } // Para controlar si se muestra el form de subida

        public DocumentosIndexViewModel()
        {
            Documentos = new List<DocumentoInfoViewModel>();
        }
    }
}