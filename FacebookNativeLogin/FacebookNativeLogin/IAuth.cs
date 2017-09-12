using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FacebookNativeLogin
{
    public interface IAuthentication
    {
        Task<LoginResult> Login();
        Task<UserInfo> GetInfo();
        void Logout();
    }
}
