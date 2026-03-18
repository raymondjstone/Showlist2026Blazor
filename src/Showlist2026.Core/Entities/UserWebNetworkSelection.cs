using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Showlist2026.Entities
{
    [Table("UserWebNetworkSelection")]
    public class UserWebNetworkSelection
    {
        [Key] public int Id { get; set; }

        public bool include { get; set; }


        public virtual WebNetwork webnetwork { get; set; }

    }
}
