using Hiemdall_bridge.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SQLite;
using System.IO;

namespace Hiemdall_bridge
{
    public class dbLayer : IDbLayer, IDisposable
    {
        private readonly string _connectionString;

        public dbLayer(string? connectionString = null)
        {            
              string DbPath=ConfigurationManager.AppSettings["ConnectionString"];

                var exeDbPath = Path.Combine(DbPath, "VitescoDB.db");        
           

            // 3. Ensure directory exists
            var directory = Path.GetDirectoryName(exeDbPath);
            if (!Directory.Exists(directory))
            {
                
                throw new DirectoryNotFoundException($"Database directory not found at path: {directory}");
                
            }            
            _connectionString = $"Data Source={exeDbPath};Version=3;";
            


        }

        public int ExecSqlNonQuery(string sql, CommandType cmdType, List<SQLiteParameter>? parameters = null)
        {
            
            using var conn = new SQLiteConnection(_connectionString);
            using var cmd = new SQLiteCommand(sql, conn) { CommandType = cmdType, CommandTimeout = 120 };
            AddParameters(cmd, parameters);
            conn.Open();
            return cmd.ExecuteNonQuery();
        }

        public DataSet ExecSqlDataSet(string sql, CommandType cmdType, List<SQLiteParameter>? parameters = null)
        {
            var ds = new DataSet();

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open(); // ← YOU WERE MISSING THIS!

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.CommandType = cmdType;
                    cmd.CommandTimeout = 120;
                    AddParameters(cmd, parameters);

                    using (var adapter = new SQLiteDataAdapter(cmd))
                    {
                        adapter.Fill(ds);
                    }
                }
            }
            return ds;
          
        }

        private static void AddParameters(SQLiteCommand cmd, List<SQLiteParameter>? parameters)
        {
            if (parameters == null || parameters.Count == 0) return;
            foreach (var p in parameters) cmd.Parameters.Add(p);
        }

        public void Dispose() { /* if you later hold unmanaged resources */ }

        public String execsqldataReader(string sql, CommandType cmdType, List<SQLiteParameter>? parameters = null)
        {
            using var connection = new SQLiteConnection(_connectionString);
            {
                connection.Open();

                using var cmd = new SQLiteCommand(sql, connection);
                {
                    cmd.CommandType = cmdType;
                    cmd.CommandTimeout = 120;
                    AddParameters(cmd, parameters);
                    using var reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        string UserType = "User";
                        while (reader.Read())
                        {
                            UserType = reader.GetString(3);

                        }
                        return UserType;
                    }
                    else
                    {
                        return "Not Found";
                    }
                }
            }
        }

        public DataTable ExecSqlDataTable(string sql, CommandType cmdType, List<SQLiteParameter>? parameters = null)
        {
            var ds = new DataTable();

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open(); // ← YOU WERE MISSING THIS!

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.CommandType = cmdType;
                    cmd.CommandTimeout = 120;
                    AddParameters(cmd, parameters);

                    using (var adapter = new SQLiteDataAdapter(cmd))
                    {
                        adapter.Fill(ds);
                    }
                }
            }
            return ds;
        }

        public object ExecScalar(string sql, CommandType commandType, List<SQLiteParameter> parameters = null)
        {
            object result = null;

            using (SQLiteConnection con = new SQLiteConnection(_connectionString))
            {
                using (SQLiteCommand cmd = new SQLiteCommand(sql, con))
                {
                    cmd.CommandType = commandType;

                    // Add parameters
                    if (parameters != null && parameters.Count > 0)
                    {
                        cmd.Parameters.AddRange(parameters.ToArray());
                    }

                    con.Open();

                    result = cmd.ExecuteScalar();

                    con.Close();
                }
            }

            return result;
        }
    }
}
