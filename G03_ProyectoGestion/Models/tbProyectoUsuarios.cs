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
    
    public partial class tbProyectoUsuarios
    {
        public int idProyecto { get; set; }
        public int idUsuario { get; set; }
        public Nullable<int> idRol { get; set; }
    
        public virtual tbProyectos tbProyectos { get; set; }
        public virtual tbRoles tbRoles { get; set; }
        public virtual tbUsuarios tbUsuarios { get; set; }
    }
}
