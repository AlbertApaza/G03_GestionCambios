using System;
using System.Collections.Generic;

namespace G03_GestionDeCambios.ViewModels.TareasViewModels
{
    public class TareaUsuarioViewModel
    {
        public int IdTarea { get; set; }
        public string NombreTarea { get; set; }
        public string DescripcionTarea { get; set; }
        public string EstadoTarea { get; set; } // Pendiente, En Proceso, Finalizado

        public string NombreElementoAsociado { get; set; }
        public string CicloDelElemento { get; set; } // Para saber a qué ciclo pertenece

        public bool EsDelCicloActual { get; set; } // Para habilitar/deshabilitar
        public bool PuedeEditarEstado { get; set; } // (EsDelCicloActual && EstadoTarea != "Finalizado")

        public DateTime? FechaInicioElemento { get; set; }
        public DateTime? FechaFinElemento { get; set; }
    }

    public class TareasUsuarioIndexViewModel
    {
        public int IdProyecto { get; set; }
        public string NombreProyecto { get; set; }
        public string CicloActualProyecto { get; set; }
        public List<TareaUsuarioViewModel> Tareas { get; set; }

        public TareasUsuarioIndexViewModel()
        {
            Tareas = new List<TareaUsuarioViewModel>();
        }
    }
}