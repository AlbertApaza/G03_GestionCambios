using System.Collections.Generic;

namespace G03_GestionDeCambios.Models
{
    // Este ViewModel es solo para la prueba.
    public class TestGraphViewModel
    {
        public List<object[]> ProyectosTimeline { get; set; }
        public List<FlagEvent> TimelineFlags { get; set; }

        public TestGraphViewModel()
        {
            ProyectosTimeline = new List<object[]>();
            TimelineFlags = new List<FlagEvent>();
        }
    }
}