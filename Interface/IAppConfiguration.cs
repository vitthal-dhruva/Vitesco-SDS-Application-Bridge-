// File: IAppConfiguration.cs
using Hiemdall_bridge.Models;

namespace Hiemdall_bridge.Interface
{
    public interface IAppConfiguration
    {
        AppConfigModel Current { get; }
        AppConfigModel ReadConfig();
    }
}