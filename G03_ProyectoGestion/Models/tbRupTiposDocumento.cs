//------------------------------------------------------------------------------
// <auto-generated>
//     Este código se generó a partir de una plantilla.
//
//     Los cambios manuales en este archivo pueden causar un comportamiento inesperado de la aplicación.
//     Los cambios manuales en este archivo se sobrescribirán si se regenera el código.
// </auto-generated>
//------------------------------------------------------------------------------

namespace G03_ProyectoGestion.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class tbRupTiposDocumento
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public tbRupTiposDocumento()
        {
            this.tbRupDocumentos = new HashSet<tbRupDocumentos>();
        }
    
        public int idTipoDocumento { get; set; }
        public string nombre { get; set; }
        public string clave { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<tbRupDocumentos> tbRupDocumentos { get; set; }
    }
}
