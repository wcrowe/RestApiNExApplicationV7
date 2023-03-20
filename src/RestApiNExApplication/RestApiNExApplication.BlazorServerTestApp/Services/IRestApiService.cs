using RestApiNExApplication.BlazorServerTestApp.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RestApiNExApplication.BlazorServerTestApp.Services
{
    public interface IRestApiService
    {
        bool IsContainerized();
        Task<ICollection<Account>> GetAccountsAsync();
        Task<ICollection<User>> GetUsersAsync();

    }
}
