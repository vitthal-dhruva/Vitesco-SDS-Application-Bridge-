using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hiemdall_bridge.Models
{
    public class ConfigurationModel
    {
        public string StationName { get; set; }
        public string IPAddress { get; set; }
        public int Port { get; set; }
        public int Timer { get; set; }
        public int MaxRows { get; set; }
    }
}
