using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Showlist2026.Entities
{
    [Table("AppSetting")]
    public class AppSetting
    {
        [Key]
        [MaxLength(200)]
        public string Key { get; set; }

        public string Value { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
