using System;
using System.Data;

namespace Hiemdall_bridge.Interface
{
    public interface IBusinessLayer
    {
        int DeleteOldMessageLogs();
        int InsertMessageValues(string type, string message);
        DataTable GetConfugurationValues();
        void UpdtateCinfugurationValues(string Station, string IPAddress, int port, int maxrows, int timer);
        DataSet GetAllCommand();
        DataTable GetAllCommandWithParameter(object CommandId);

        DataSet GetAllCommandParameter(object CommandId, object CommandType);
        DataSet GetCommandByID(int commandID);
        DataSet Getuser(object User, object Pass);
        string CheckUser(object User, object Pass);
        DataSet GetfilterLogs(DateTime fromDate, DateTime toDate);
        DataSet GetAllUsers();
        int InsertUSerValues(object username, object password, object role);
        int DeleteUser(object id);
        int UpdateUser(object id, object username, object password, object role);
        void UpdateParameter(object Id, object CommandId, object Commandtype, object ParamName, object DataType, object NodeID, object IsActive);
        void DeleteParameter(object Id);
        int InsertCommandValues(object CommandId, object Commandtype, object ParamName, object DataType, object NodeID, object IsActive);
    }
}