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
    
    public partial class tbProyectoElemento
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public tbProyectoElemento()
        {
            this.tbRupActividadAsignaciones = new HashSet<tbRupActividadAsignaciones>();
        }
    
        public int idProyectoElemento { get; set; }
        public Nullable<int> idProyecto { get; set; }
        public Nullable<int> idElemento { get; set; }
        public Nullable<System.DateTime> fechaInicio { get; set; }
        public Nullable<System.DateTime> fechaFin { get; set; }
        public Nullable<int> FASE_SPRINT_ITERACION { get; set; }
        public Nullable<int> idRol { get; set; }
        public string estado { get; set; }
    
        public virtual tbElementos tbElementos { get; set; }
        public virtual tbProyectos tbProyectos { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<tbRupActividadAsignaciones> tbRupActividadAsignaciones { get; set; }
        public virtual tbRoles tbRoles { get; set; }
    }
}
