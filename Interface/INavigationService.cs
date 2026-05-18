using Hiemdall_bridge.Models;

namespace Hiemdall_bridge.Interfaces
{
    public interface INavigationService
    {
        void OpenMainWindow(UserModel user);
        void OpenLogin();
    }
}
