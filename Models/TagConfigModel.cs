using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hiemdall_bridge.Models
{//tab 2
    public class TagConfigModel
    {
        public int SrNo { get; set; }
        public int Id { get; set; }
        public string CommandId { get; set; }
        public string CommandType { get; set; }
        public string CommandName { get; set; }
        public string ParameterName { get; set; }
        public string DataType { get; set; }
        public string NodeID { get; set; }
        public bool IsActive { get; set; }
    }

}
