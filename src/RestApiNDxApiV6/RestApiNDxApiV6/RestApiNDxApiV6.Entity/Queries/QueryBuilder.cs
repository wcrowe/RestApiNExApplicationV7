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

namespace RestApiNDxApiV6.Entity.Queries
{
    public partial class QueryBuilder
    {

        private IQuery _query;
        public IQuery Build(Type type)
        {
            switch (type.ToString())
            {
                case "RestApiNDxApiV6.Entity.Account":
                    _query = new AccountQuery();
                    break;
                case "RestApiNDxApiV6.Entity.User":
                    _query = new UserQuery();
                    break;
                default:
                    break;
            }
            if (_query == null)
                SearchAdditionalEntityTypes(type, ref _query);
            return _query;
        }

        //extend for additional entity types checks for T4 code 
        partial void SearchAdditionalEntityTypes(Type type, ref IQuery query);

    }
}
