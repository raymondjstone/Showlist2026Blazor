using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Showlist2026.Entities
{
    [Table("ShowUpdated")]
    public class ShowUpdated
    {
        [Key] public int Id { get; set; }

        public long showudatedid { get; set; }
        public long xshowid { get; set; }
        public long updatedTimeStamp { get; set; }
        public bool lastupdateprocessed { get; set; }




    }
}
