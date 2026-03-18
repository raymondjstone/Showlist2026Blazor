using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire;
using Hangfire.Storage;
using Showlist2026.Models;
using Showlist2026.Services;

namespace Showlist2026.Web.Services
{
    public class JobStatusService : IJobStatusService
    {
        public List<JobStatusModel> GetRecurringJobStatuses()
        {
            try
            {
                using var connection = JobStorage.Current.GetConnection();
                var recurringJobs = connection.GetRecurringJobs();

                return recurringJobs.Select(job => new JobStatusModel
                {
                    JobName = job.Id,
                    Cron = job.Cron,
                    LastExecution = job.LastExecution,
                    NextExecution = job.NextExecution,
                    LastStatus = job.LastJobState ?? "Never Run",
                    Error = job.Error
                }).ToList();
            }
            catch
            {
                return new List<JobStatusModel>();
            }
        }
    }
}
