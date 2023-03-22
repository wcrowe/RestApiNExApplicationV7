using RestApiNLxV7.Data.Context;
using RestApiNLxV7.Data.Entities;
using System.Collections.Generic;
using System.Linq;

namespace RestApiNLxV7.Data
{
    public class DataService : IDataService
    {
        protected DataContext _context;
        public DataService(DataContext context)
        {
            _context = context;
        }

        //// stored procedure example
        //public Account GetAccount(int accountId)
        //{
        //    var accountp= new SqlParameter("@id", accountId);
        //    var account = _context.Accounts
        //        .FromSqlRaw("exec prGetAccount @id", accountp)
        //        .FirstOrDefault();
        //    return account;
        //}

        public List<Account> GetAccounts()
        {
            var accounts = _context.Accounts.ToList();
            return accounts;
        }

        public Account GetAccount(int accountId)
        {
            var account = _context.Accounts.Where(acc => acc.Id == accountId).SingleOrDefault();
            return account;
        }

        public void AddAccount(Account account)
        {
            _context.Accounts.Add(account);
            _context.SaveChangesAsync();
        }

        public void UpdateAccount(Account account)
        {
            _context.Accounts.Update(account);
            _context.SaveChangesAsync();
        }

        public void DeleteAccount(int accountId)
        {
            var account = _context.Accounts.Where(acc => acc.Id == accountId).SingleOrDefault();
            _context.Accounts.Remove(account);
            _context.SaveChangesAsync();
        }

    }
}
