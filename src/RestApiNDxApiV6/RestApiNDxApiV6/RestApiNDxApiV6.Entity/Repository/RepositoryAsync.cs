#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
using Dapper;
using RestApiNDxApiV6.Entity.Context;
using RestApiNDxApiV6.Entity.Queries;
using RestApiNDxApiV6.Entity.UnitofWork;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;


namespace RestApiNDxApiV6.Entity.Repository
{
    /// <summary>
    /// General repository class async
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RepositoryAsync<T> : IRepositoryAsync<T> where T : class
    {
        private readonly IDbConnection _connection;
        private IDbTransaction _transaction;
        private RestApiNDxApiV6Context _context;
        private IQuery _query;

        /// <summary>
        /// Connection and Transaction is per created unitofwork instance
        /// </summary>
        /// <param name="dbProvider"></param>
        public RepositoryAsync(RestApiNDxApiV6Context dbProvider)
        {
            _connection = dbProvider.Connection;
            if (_connection.State == ConnectionState.Closed) _connection.Open();
            _transaction = dbProvider.Transaction;
            _context = dbProvider;
            //build a query
            var type = typeof(T);
            _query = (new QueryBuilder()).Build(type);
        }


        public IDbConnection Connection { get => _connection; }
        public IDbTransaction Transaction { get => _transaction; set { _transaction = value; _context.Transaction = value; } }


        public async Task<IEnumerable<T>> GetAll()
        {
            return await _connection.QueryAsync<T>(_query.SelectAll);
        }
        public async Task<IEnumerable<T>> Get(Expression<Func<T, bool>> predicate)
        {
            IEnumerable<T> t = await _connection.QueryAsync<T>(_query.SelectAll, null);
            return t.AsQueryable().Where(predicate).AsEnumerable<T>();
        }

        public async Task<T> GetOne(Expression<Func<T, bool>> predicate)
        {
            IEnumerable<T> t = await _connection.QueryAsync<T>(_query.SelectAll, null);
            return t.AsQueryable().Where(predicate).FirstOrDefault();
        }
        public async Task<int> Insert(T entity)
        {
            if (entity == null)
                return -1;
            if (_transaction == null) Transaction = _connection.BeginTransaction();
            return (await _connection.QueryAsync<int>(_query.Insert, entity, _transaction)).Single();
        }
        public async Task<int> Update(object id, T entity)
        {
            int[] retval;
            if (entity == null)
                return 0;
            if (_transaction == null) Transaction = _connection.BeginTransaction();
            retval = (await _connection.QueryAsync<int>(_query.Update, entity, _transaction)).Cast<int>().ToArray();
            return retval[0];

        }
        public async Task<int> Delete(object id)
        {
            int[] retval;
            if (_transaction == null) Transaction = _connection.BeginTransaction();
            retval = (await _connection.QueryAsync<int>(_query.Delete, new { Id = id }, _transaction)).Cast<int>().ToArray();
            return retval[0];
        }

    }


}
