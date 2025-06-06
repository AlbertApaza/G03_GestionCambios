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
    
    public partial class tbXpHistoriasUsuario
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public tbXpHistoriasUsuario()
        {
            this.tbXpParejasProgramacion = new HashSet<tbXpParejasProgramacion>();
            this.tbXpPlanningGame = new HashSet<tbXpPlanningGame>();
            this.tbXpPruebasAceptacion = new HashSet<tbXpPruebasAceptacion>();
        }
    
        public int idHistoria { get; set; }
        public int idProyecto { get; set; }
        public string titulo { get; set; }
        public string historia { get; set; }
        public string criteriosAceptacion { get; set; }
        public int idIteracion { get; set; }
    
        public virtual tbProyectos tbProyectos { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<tbXpParejasProgramacion> tbXpParejasProgramacion { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<tbXpPlanningGame> tbXpPlanningGame { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<tbXpPruebasAceptacion> tbXpPruebasAceptacion { get; set; }
        public virtual tbXpIteraciones tbXpIteraciones { get; set; }
    }
}
