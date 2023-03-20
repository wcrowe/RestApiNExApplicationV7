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
    /// General repository class
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly IDbConnection _connection;
        private IDbTransaction _transaction;
        private RestApiNDxApiV6Context _context;
        private IQuery _query;

        /// <summary>
        /// Connection and Transaction is per created unitofwork instance
        /// </summary>
        /// <param name="dbProvider"></param>
        public Repository(RestApiNDxApiV6Context dbProvider)
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


        public IEnumerable<T> GetAll()
        {
            return _connection.Query<T>(_query.SelectAll);
            //example= how to use sp
            //string readSp = "GetAllAccounts";
            //return conn.Query<Account>(readSp, commandType: CommandType.StoredProcedure).ToList();
        }
        public IEnumerable<T> Get(Expression<Func<T, bool>> predicate)
        {
            return _connection.Query<T>(_query.SelectAll, null).AsQueryable().Where(predicate).AsEnumerable<T>();
        }
        public T GetOne(Expression<Func<T, bool>> predicate)
        {
            return _connection.Query<T>(_query.SelectAll, null).AsQueryable().Where(predicate).FirstOrDefault();
        }
        public int Insert(T entity)
        {
            if (entity == null)
                return -1;
            if (_transaction == null) Transaction = _connection.BeginTransaction();
            return _connection.Query<int>(_query.Insert, entity, _transaction).Single();

        }
        public int Update(object id, T entity)
        {
            int[] retval;
            if (entity == null)
                return 0;
            if (_transaction == null) Transaction = _connection.BeginTransaction();
            retval = _connection.Query<int>(_query.Update, entity, _transaction).Cast<int>().ToArray();
            return retval[0];
        }
        public int Delete(object id)
        {
            int[] retval;
            if (_transaction == null) Transaction = _connection.BeginTransaction();
            retval = _connection.Query<int>(_query.Delete, new { Id = id }, _transaction).Cast<int>().ToArray();
            return retval[0];
        }

    }

}
