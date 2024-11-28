using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using DLL;

namespace DekstopApp
{
    [ServiceContract]
    public interface IJobServer
    {
        [OperationContract]
        Job GetJob();

        [OperationContract]
        void SubmitJobResult(string result,int jobId);

    }
}
