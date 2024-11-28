using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DLL
{
    public class Client
    {
        public int ClientId { get; set; }
        public string IPAddress { get; set; }
        public int Port { get; set; }
        public DateTime LastActiveTime { get; set; }
        public int NoOfCompletedJobs { get; set; }
    }
}
