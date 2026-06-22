using Hiemdall_bridge.Interface;
using Hiemdall_bridge.Models;
using System;
using System.Configuration;


namespace Hiemdall_bridge.Services
{
    public class AppConfiguration : IAppConfiguration
    {
        // Properties
        public AppConfigModel Current { get; private set; } = new AppConfigModel();

        public AppConfigModel ReadConfig()
        {
            try
            {
                // Reading from App.config keys
                Current.Connetionstring = ConfigurationManager.AppSettings["ConnectionString"] ?? "Server=.;Database=Hiemdall;Trusted_Connection=True;";
                Current.OPCUrl = ConfigurationManager.AppSettings["OPCConnection"] ?? "opc.tcp://127.0.0.1:4840";

                // Parsing Integers
                Current.CommandID = ConfigurationManager.AppSettings["EventNode"];
                Current.Status = ConfigurationManager.AppSettings["StatusNode"];
                Current.PingNodeID = ConfigurationManager.AppSettings["PingNode"];
                Current.PingDatatype = ConfigurationManager.AppSettings["PingDatatype"];
                Current.Statusdatatype = ConfigurationManager.AppSettings["StatusNodeDatatype"];
                Current.pingtimer = ConfigurationManager.AppSettings["PingTimer"];
                Current.pingtimedatatype = ConfigurationManager.AppSettings["PingTimerDatatype"];

            }
            catch (Exception ex)
            {
                // Fallback to hardcoded defaults if config is broken
                SetDefaultValues();
                // Optional: Log error here using your ILogger
            }

            return Current;
        }

        private void SetDefaultValues()
        {
            Current.Connetionstring = "Default_Conn_String";
            Current.OPCUrl = "opc.tcp://localhost:4840";
          
        }
    }

  
}