using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Showlist2026.Entities
{
    [Table("ShowLink")]
    public class ShowLink
    {
        [Key] public int Id { get; set; }

        public int PredecessorShowId { get; set; }
        public Show? PredecessorShow { get; set; }

        public int SuccessorShowId { get; set; }
        public Show? SuccessorShow { get; set; }
    }
}
