using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DLL;
namespace DekstopApp
{
    public static class JobManager
    {
        public static List<Job> JobQueue = new List<Job>();

        public static Job RequestJob()
        {
            Job job = JobQueue.FirstOrDefault(j => !j.IsCompleted && !j.InProgress);

            if (job != null)
            {
                job.InProgress = true;
                Console.WriteLine("Returning job");
                return job;
            }

            return null;
        }

        public static void SubmitJob(string result, int jobId)
        {
            Job job = JobQueue.First(j => j.JobId == jobId);
            job.JobResult = result;
            job.InProgress = false;
            job.IsCompleted = true; 
        }
    }
}
