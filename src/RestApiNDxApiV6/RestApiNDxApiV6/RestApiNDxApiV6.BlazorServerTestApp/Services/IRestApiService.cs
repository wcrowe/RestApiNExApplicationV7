using RestApiNDxApiV6.BlazorServerTestApp.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RestApiNDxApiV6.BlazorServerTestApp.Services
{
    public interface IRestApiService
    {
        Task<ICollection<Account>> GetAccountsAsync();
        Task<ICollection<User>> GetUsersAsync();

    }
}
