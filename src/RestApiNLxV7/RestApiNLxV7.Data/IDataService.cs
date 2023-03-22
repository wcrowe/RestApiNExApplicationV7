using RestApiNLxV7.Data.Entities;
using System.Collections.Generic;

namespace RestApiNLxV7.Data
{
    public interface IDataService
    {
        Account GetAccount(int id);
        List<Account> GetAccounts();
        void AddAccount(Account account);
        void UpdateAccount(Account account);
        void DeleteAccount(int id);
    }
}
