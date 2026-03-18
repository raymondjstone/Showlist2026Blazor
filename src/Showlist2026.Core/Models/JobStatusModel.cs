using System;

namespace Showlist2026.Models
{
    public class JobStatusModel
    {
        public string JobName { get; set; } = "";
        public string Cron { get; set; } = "";
        public DateTime? LastExecution { get; set; }
        public DateTime? NextExecution { get; set; }
        public string LastStatus { get; set; } = "Unknown";
        public string? Error { get; set; }
    }
}
