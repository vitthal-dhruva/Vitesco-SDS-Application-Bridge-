using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hiemdall_bridge.Models
{
    public  class AppConfigModel
    {
        public string Connetionstring { get; set; } = string.Empty;
        public string OPCUrl { get; set; } = string.Empty;
        public string CommandID { get; set; } = string.Empty;
        public string PingNodeID { get; set; } = string.Empty;
        public string PingDatatype { get; set; } = string.Empty;
        public string Statusdatatype { get; set; } = string.Empty;
        public string  Status { get; set; }= string.Empty;
        public string pingtimer { get; set; }= string.Empty;
        public string pingtimedatatype { get; set; }= string.Empty;
    }
}
