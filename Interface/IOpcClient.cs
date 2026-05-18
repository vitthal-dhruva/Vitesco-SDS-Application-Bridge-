using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hiemdall_bridge.Interface
{
    public interface IOpcClient
    {
        
        event Action<bool>? OnConnectionChange;
        event Action<string>? OnCommandReceived;
        // Task ConnectOPCSession(string endpointUrl,string Eventnode);
        Task ConnectAsync(string endpointUrl,string Eventnode);
        
        Task DisconnectOPCSessionAsync();
        Task<bool> WriteValuesCollection(Dictionary<string, object> values);
       // Task<bool> WriteValue(Dictionary<string, string> dict, string rootName);
        Task<Dictionary<string, object>> ReadValuesCollection(List<string> nodeIds);
    }
}