using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DLL
{
    public class Job
    {
        public int JobId { get; set; }
        public string PythonScript { get; set; }
        public bool IsCompleted { get; set; }
        public string JobResult { get; set; }
        public bool InProgress { get; set; }
        public string ScriptHash { get; set; }

    }
}
