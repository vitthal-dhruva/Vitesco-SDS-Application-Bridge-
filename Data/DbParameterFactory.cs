using System;
using System.Data;
using System.Data.SQLite;

namespace Hiemdall_bridge
{
    public static class DbParameterFactory
    {
        public static SQLiteParameter Create(string name, DbType type, object? value) =>
            new SQLiteParameter(name, type) { Value = value ?? DBNull.Value };
    }
}