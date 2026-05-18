using Hiemdall_bridge.Interfaces;
using Hiemdall_bridge.Models;

namespace Hiemdall_bridge.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly BusinessLayer _layer;

        public AuthenticationService()
        {
            _layer = new BusinessLayer();
        }

        public UserModel Authenticate(string username, string password)
        {
            string role = _layer.CheckUser(username, password);

            if (role == "Admin" || role == "User")
            {
                return new UserModel
                {
                    Username = username,
                    Role = role,
                    Password = password

                };
            }

            return null;
        }
    }
}
