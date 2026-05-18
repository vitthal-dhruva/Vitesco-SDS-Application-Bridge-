using Hiemdall_bridge.Models;

namespace Hiemdall_bridge.Interfaces
{
    public interface IAuthenticationService
    {
        UserModel Authenticate(string username, string password);
    }
}
