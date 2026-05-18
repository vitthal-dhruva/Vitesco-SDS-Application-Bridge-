namespace Hiemdall_bridge.Models
{
    public class TagModel
    {
        public int Id { get; set; }
        public string CommandName { get; set; }
        public string CommandType { get; set; }
        public string ParamName { get; set; }
        public string DataType { get; set; }
        public string NodeID { get; set; } // OPC UA NodeIDs are usually strings
        public bool IsActive { get; set; }
    }
}