using Hiemdall_bridge.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;

namespace Hiemdall_bridge
{
    public class BusinessLayer : IBusinessLayer
    {
        private readonly IDbLayer _dbl;
        public BusinessLayer(IDbLayer? dbl = null) => _dbl = dbl ?? new dbLayer();

        public int InsertMessageValues(string type, string message)
        {
            const string sql = "INSERT INTO Message_Log (Type, Message, CreatedAt) VALUES (@Type,@Message,@Timestamp)";
            var ps = new List<SQLiteParameter>
            {
                DbParameterFactory.Create("@Type", DbType.String, type),
                DbParameterFactory.Create("@Message", DbType.String, message),
                DbParameterFactory.Create("@Timestamp", DbType.DateTime, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
            };
            return _dbl.ExecSqlNonQuery(sql, CommandType.Text, ps);
        }
        public int DeleteOldMessageLogs()
        {

            string sql = @"DELETE FROM Message_Log WHERE CreatedAt < datetime('now','-4 months')";

            return _dbl.ExecSqlNonQuery(sql, CommandType.Text);

        }
        public void UpdtateCinfugurationValues(string Station, string IPAddress, int port, int maxrows, int timer)
        {
            const string sql = "update MESConfiguration set StationName=@Station, IpAddress=@IPAddress, PortNO=@port, RecordDisplay=@maxrows, TimerTime=@timer where id=1";
            var ps = new List<SQLiteParameter>
                  {

                      DbParameterFactory.Create("@Station", DbType.String, Station),
                      DbParameterFactory.Create("@IPAddress", DbType.String, IPAddress),
                      DbParameterFactory.Create("@port", DbType.Int32, port),
                      DbParameterFactory.Create("@maxrows", DbType.Int32, maxrows),
                      DbParameterFactory.Create("@timer", DbType.Int32, timer)
                  };
            _dbl.ExecSqlNonQuery(sql, CommandType.Text, ps);
        }
        public DataTable GetConfugurationValues()
        {
            const string sql = "select * from MESConfiguration where id=1";
            return _dbl.ExecSqlDataTable(sql, CommandType.Text);
        }

        public DataTable GetAllCommandWithParameter(object CommandId)
        {
            return _dbl.ExecSqlDataTable("SELECT b.Id, a.CommandId, b.CommandName,b.RootElement,a.CommandType, a.ParamName,a.DataType,a.NodeID,a.IsActive FROM CommandparameterST1 a INNER JOIN MESCommand b ON a.CommandId = b.Id WHERE a.CommandId IN(@commandID, 12) AND a.IsActive = 1 ORDER BY a.CommandId ASC",

                CommandType.Text,
                new List<SQLiteParameter>
                {
                    new SQLiteParameter("@commandID", DbType.Int64) { Value = CommandId },

                    new SQLiteParameter("@IsActive", DbType.Boolean) { Value = true }
                });
        }

        public DataSet GetCommandByID(int commandID)
        {
            return _dbl.ExecSqlDataSet(
                "SELECT CommandName FROM MESCommand WHERE Id = @commandID",
                CommandType.Text,
                new List<SQLiteParameter>
                {
                    new SQLiteParameter("@commandID", DbType.Int32) { Value = commandID }
                });
        }

        public DataSet Getuser(object User, object Pass)
        {
            return _dbl.ExecSqlDataSet("SELECT * FROM User where Username=@User and Password=@Pass", CommandType.Text,
                 new List<SQLiteParameter>
                {
                    new SQLiteParameter("@User", DbType.String) { Value = User },

                new SQLiteParameter("@Pass", DbType.String) { Value = Pass }
                }
                );
        }
        public string CheckUser(object User, object Pass)
        {
            return _dbl.execsqldataReader("SELECT * FROM User where Username=@User and Password=@Pass", CommandType.Text,
                 new List<SQLiteParameter>
                {
                    new SQLiteParameter("@User", DbType.String) { Value = User },

                new SQLiteParameter("@Pass", DbType.String) { Value = Pass }
                }
                );
        }

        public int InsertCommandValues(object CommandId, object Commandtype, object ParamName, object DataType, object NodeID, object IsActive)
        {
            return _dbl.ExecSqlNonQuery(
                $"INSERT INTO CommandParameterST1 (CommandId, CommandType, ParamName,DataType,NodeID,IsActive) VALUES (@CommandId,@Commandtype,@ParamName,@DataType,@NodeID,@IsActive)",
                CommandType.Text,
                new List<SQLiteParameter>
                {
                    new SQLiteParameter("@CommandId", DbType.Int64) { Value = CommandId ?? DBNull.Value },
                    new SQLiteParameter("@Commandtype", DbType.String) { Value = Commandtype ?? DBNull.Value },
                    new SQLiteParameter("@ParamName", DbType.String) { Value = ParamName ?? DBNull.Value },
                    new SQLiteParameter("@DataType", DbType.String) { Value = DataType ?? DBNull.Value },
                    new SQLiteParameter("@NodeID", DbType.String) { Value = NodeID ?? DBNull.Value },
                    new SQLiteParameter("@IsActive", DbType.Boolean) { Value = IsActive ?? DBNull.Value }
                });
        }

        public DataSet GetAllCommand()
        {
            return _dbl.ExecSqlDataSet("SELECT * FROM MESCommand", CommandType.Text);
        }

        public DataSet GetAllCommandParameter(object CommandId, object Commandtype)
        {
            return _dbl.ExecSqlDataSet(
                "SELECT * FROM CommandParameterST1 a inner join MESCommand b on a.CommandId=b.Id where a.CommandId=@commandID and a.CommandType=@Commandtype",
                CommandType.Text,
                new List<SQLiteParameter>
                {
                    new SQLiteParameter("@commandID", DbType.Int64) { Value = CommandId },
                    new SQLiteParameter("@Commandtype", DbType.String) { Value = Commandtype ?? DBNull.Value },
                    new SQLiteParameter("@IsActive", DbType.Boolean) { Value = true }
                });
        }

        public void UpdateParameter(object Id, object CommandId, object Commandtype, object ParamName, object NodeID, object DataType, object IsActive)
        {
            string sql = @"UPDATE CommandParameterST1
                           SET CommandId = @CommandId,
                               CommandType = @Commandtype,
                               ParamName = @ParamName,
                               NodeID = @NodeID,
                               DataType = @DataType,
                               IsActive=@IsActive
                           WHERE Id = @Id";

            _dbl.ExecSqlNonQuery(sql, CommandType.Text,
                new List<SQLiteParameter>
                {
                    new SQLiteParameter("@CommandId", DbType.Int64) { Value = CommandId ?? DBNull.Value },
                    new SQLiteParameter("@Commandtype", DbType.String) { Value = Commandtype ?? DBNull.Value },
                    new SQLiteParameter("@ParamName", DbType.String) { Value = ParamName ?? DBNull.Value },
                    new SQLiteParameter("@DataType", DbType.String) { Value = DataType ?? DBNull.Value },
                    new SQLiteParameter("@NodeID", DbType.String) { Value = NodeID ?? DBNull.Value },
                    new SQLiteParameter("@IsActive", DbType.Boolean) { Value = IsActive ?? DBNull.Value },
                    new SQLiteParameter("@Id", DbType.Int64) { Value = Id ?? DBNull.Value }
                });
        }

        public void DeleteParameter(object Id)
        {
            string sql = "delete from CommandParameterST1 WHERE Id = @Id";
            _dbl.ExecSqlNonQuery(sql, CommandType.Text, new List<SQLiteParameter>
            {
                new SQLiteParameter("@Id", DbType.Int64) { Value = Id }
            });
        }

        public DataSet GetfilterLogs(DateTime fromDate, DateTime toDate)
        {
                         return _dbl.ExecSqlDataSet(
                 @"SELECT *
                   FROM Message_Log
                   WHERE CreatedAt >= @FromDate 
                   AND CreatedAt <= @ToDate
                   ORDER BY CreatedAt DESC",
                 CommandType.Text,
                 new List<SQLiteParameter>
                 {
                     new SQLiteParameter("@FromDate", DbType.DateTime)
                     {
                         Value = fromDate
                     },
                     new SQLiteParameter("@ToDate", DbType.DateTime)
                     {
                         Value = toDate
                     }
                 });
        }

        // Add these methods to the existing BusinessLayer class (place anywhere inside the class)
        public DataSet GetAllUsers()
        {
            return _dbl.ExecSqlDataSet("SELECT ID, UserName, UserType  FROM User", CommandType.Text);
        }

        public int InsertUSerValues(object username, object password, object role)
        {
            const string sql = "INSERT INTO User (UserName, PassWord, UserType) VALUES (@Username, @Password, @RoleType)";
            var ps = new List<SQLiteParameter>
    {
        new SQLiteParameter("@Username", DbType.String) { Value = username ?? DBNull.Value },
        new SQLiteParameter("@Password", DbType.String) { Value = password ?? DBNull.Value },
        new SQLiteParameter("@RoleType", DbType.String) { Value = role ?? DBNull.Value },

    };
            return _dbl.ExecSqlNonQuery(sql, CommandType.Text, ps);
        }

        public int DeleteUser(object id)
        { // Check if selected user is Admin
            const string checkUserSql =
                "SELECT UserType FROM User WHERE ID = @Id";

            var checkParams = new List<SQLiteParameter>
    {
        new SQLiteParameter("@Id", DbType.Int64)
        {
            Value = id ?? DBNull.Value
        }
    };

            object roleObj = _dbl.ExecScalar(
                checkUserSql,
                CommandType.Text,
                checkParams);

            string roleType = roleObj?.ToString();

            // If user is Admin, check admin count
            if (roleType == "Admin")
            {
                const string adminCountSql =
                    "SELECT COUNT(*) FROM User WHERE UserType = 'Admin'";

                object countObj = _dbl.ExecScalar(
                    adminCountSql,
                    CommandType.Text,
                    null);

                int adminCount = Convert.ToInt32(countObj);

                // Prevent deleting last admin
                if (adminCount <= 1)
                {
                    return 0;
                }
            }
            const string sql = "DELETE FROM User WHERE ID = @Id";
            var ps = new List<SQLiteParameter>
    {
        new SQLiteParameter("@Id", DbType.Int64) { Value = id ?? DBNull.Value }
    };
            return _dbl.ExecSqlNonQuery(sql, CommandType.Text, ps);
        }


        // Add this method inside the existing BusinessLayer class

        public int UpdateUser(object id, object username, object password, object role)
        {
            const string sql = @"UPDATE User
                         SET Username = @Username,
                             Password = @Password,
                             UserType = @RoleType
                             
                         WHERE Id = @Id";

            var ps = new List<SQLiteParameter>
    {
        new SQLiteParameter("@Username", DbType.String) { Value = username ?? DBNull.Value },
        new SQLiteParameter("@Password", DbType.String) { Value = password ?? DBNull.Value },
        new SQLiteParameter("@RoleType", DbType.String) { Value = role ?? DBNull.Value },
        //new SQLiteParameter("@IsActive", DbType.Boolean) { Value = isActive ?? DBNull.Value },
        new SQLiteParameter("@Id", DbType.Int64) { Value = id ?? DBNull.Value }
    };

            return _dbl.ExecSqlNonQuery(sql, CommandType.Text, ps);
        }
    }
}
