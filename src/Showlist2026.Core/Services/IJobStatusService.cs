using System.Collections.Generic;
using Showlist2026.Models;

namespace Showlist2026.Services
{
    public interface IJobStatusService
    {
        List<JobStatusModel> GetRecurringJobStatuses();
    }
}
