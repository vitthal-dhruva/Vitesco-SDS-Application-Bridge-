using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;

namespace Hiemdall_bridge
{
    public interface IDbLayer
    {
        int ExecSqlNonQuery(string sql, CommandType cmdType, List<SQLiteParameter>? parameters = null);
        DataSet ExecSqlDataSet(string sql, CommandType cmdType, List<SQLiteParameter>? parameters = null);
        string execsqldataReader(string sql, CommandType cmdType, List<SQLiteParameter>? parameters = null);
        DataTable ExecSqlDataTable(string sql, CommandType text, List<SQLiteParameter>? parameters = null);
        object ExecScalar(string sql, CommandType commandType, List<SQLiteParameter> parameters = null);
    }
}