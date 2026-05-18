using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hiemdall_bridge.Models
{
    public class LogItem
    {
        public int SrNo { get; set; }
        public DateTime DateTime { get; set; }
        public string? State { get; set; }   // SEND / RECEIVE / CONNECT
        public string? Message { get; set; }
    }

    // TabTiem2 Code
    public enum CMDType
    {
        Request,
        Response
    }

}
