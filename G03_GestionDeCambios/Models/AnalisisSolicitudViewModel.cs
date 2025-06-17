using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace G03_GestionDeCambios.Models
{
    public class AnalisisSolicitudViewModel
    {
        public int IdSolicitud { get; set; }
        public SolicitudDetalleViewModel SolicitudInfo { get; set; } // Reutilizamos el ViewModel anterior
        public HistorialViewModel HistorialReciente { get; set; }
        public ProyectoContextoViewModel ProyectoContexto { get; set; }
        public ElementoContextoViewModel ElementoAfectado { get; set; }
        public EquipoViewModel EquipoProyecto { get; set; }
    }

    public class HistorialViewModel
    {
        public string ComentarioPasoAnterior { get; set; }
        public string UsuarioPasoAnterior { get; set; }
        public List<HistorialEntry> Entradas { get; set; }
        public class HistorialEntry
        {
            public DateTime Fecha { get; set; }
            public string Usuario { get; set; }
            public string Decision { get; set; }
            public string Comentarios { get; set; }
            public string PasoProceso { get; set; }
        }
    }

    public class ProyectoContextoViewModel
    {
        public string NombreProyecto { get; set; }
        public string Metodologia { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public string CicloActual { get; set; }
    }

    public class ElementoContextoViewModel
    {
        public string NombreElemento { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public List<TareaViewModel> TareasAsociadas { get; set; }
    }

    //public class TareaViewModel
    //{
    //    public string NombreTarea { get; set; }
    //    public string UsuarioAsignado { get; set; }
    //    public string Estado { get; set; }
    //}

    public class EquipoViewModel
    {
        public List<MiembroEquipo> Miembros { get; set; }
    }

    public class MiembroEquipo
    {
        public string NombreCompleto { get; set; }
        public string Rol { get; set; }
        public string Disponibilidad { get; set; } // "Disponible" o "En Tarea"
    }
    public class AprobacionViewModel
    {
        public int IdSolicitud { get; set; }
        public SolicitudDetalleViewModel SolicitudInfo { get; set; }
        public ResumenAnalisisViewModel ResumenAnalisis { get; set; }
        public HistorialViewModel HistorialCompleto { get; set; } // Reutilizamos el ViewModel de historial
    }

    public class ResumenAnalisisViewModel
    {
        public string AnalistaNombre { get; set; }
        public DateTime FechaAnalisis { get; set; }
        public string JustificacionAnalisis { get; set; }
    }

    public class AsignacionViewModel
    {
        public int IdSolicitud { get; set; }
        public SolicitudDetalleViewModel SolicitudInfo { get; set; }
        public ResumenAnalisisViewModel ResumenAnalisis { get; set; }
        public string ElementoAfectadoNombre { get; set; }
        public int IdProyectoElemento { get; set; }
        public List<MiembroAsignableViewModel> EquipoElegible { get; set; }
        public List<TareaViewModel> TareasYaAsignadas { get; set; }
    }

    public class MiembroAsignableViewModel
    {
        public int IdUsuario { get; set; }
        public string NombreCompleto { get; set; }
        public string Rol { get; set; }
        public int TareasActivas { get; set; }
    }

    // Modelo para recibir los datos del formulario de asignación
    public class AsignacionFormModel
    {
        public int IdSolicitud { get; set; }
        public List<NuevaTareaModel> NuevasTareas { get; set; }
    }

    public class NuevaTareaModel
    {
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public int IdUsuarioAsignado { get; set; }
    }
    public class DesarrolloViewModel
    {
        public int IdSolicitud { get; set; }
        public string ObjetivoSolicitud { get; set; }
        public string ElementoAfectadoNombre { get; set; }
        public List<TareaDetalleViewModel2> Tareas { get; set; }
        public int Progreso { get; set; } // Porcentaje de 0 a 100
    }

    public class TareaDetalleViewModel2
    {
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public string AsignadoA { get; set; }
        public string Estado { get; set; }
    }
}