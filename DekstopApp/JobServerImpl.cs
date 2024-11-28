using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using DLL;

namespace DekstopApp
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
    public class JobServerImpl: IJobServer
    {
        
        public Job GetJob() {

            Job job = JobManager.RequestJob();
            if (!(job == null))
            {
                return job;
            }
            return null;
        }

        public void SubmitJobResult(string result,int jobId)
        {
            JobManager.SubmitJob(result,jobId);
        }
    }
}
