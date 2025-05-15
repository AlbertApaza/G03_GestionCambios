using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace G03_ProyectoGestion.Models
{
    public class SprintCreatePostModel
    {
        public int ProjectId { get; set; }
        public string Name { get; set; }
        public DateTime? Start_Date { get; set; }
        public DateTime? End_Date { get; set; }
    }

    public class BacklogItemCreatePostModel
    {
        public int ProjectId { get; set; }
        public string Description { get; set; }
        public string Priority { get; set; }
        // No hay IdUsuarioAsignado, SprintId, Status para enviar
    }

    public class DailyBacklogUpdateModel
    {
        public int BacklogId { get; set; }
        public int UserId { get; set; }
        public string Comment { get; set; }
    }
    public class DailyCreatePostModel
    {
        public int SprintId { get; set; }
        public int ProjectId { get; set; }
        public DateTime Date { get; set; }
        public string Observations { get; set; }
        public List<DailyBacklogUpdateModel> BacklogUpdates { get; set; }
    }

}