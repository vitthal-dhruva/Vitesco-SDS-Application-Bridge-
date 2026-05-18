using System;

namespace Hiemdall_bridge.Interface
{
    public interface ILogger
    {
        void Info(string message);
        void Error(string message);
        
       
    }
}