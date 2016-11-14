using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebToOneDriveBusiness
{
    interface IAuthProvider
    {
        Task<string> GetUserAccessTokenAsync();
    }
}
