using AutoMapper;
using RestApiNExApiV6.Entity;
using RestApiNExApiV6.Entity.UnitofWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace RestApiNExApiV6.Domain.Service
{
    public class AccountService<Tv, Te> : GenericService<Tv, Te>
                                        where Tv : AccountViewModel
                                        where Te : Account
    {
        //DI must be implemented in specific service as well beside GenericService constructor
        public AccountService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            if (_unitOfWork == null)
                _unitOfWork = unitOfWork;
            if (_mapper == null)
                _mapper = mapper;
        }

        public virtual IEnumerable<Tv> GetAll(Pagination pagination)
        {
            var queryable = _unitOfWork.Context.Accounts.AsQueryable();
            var entities = queryable.Paginate(pagination, out PaginationPagesCnt).ToList();
            return _mapper.Map<IEnumerable<Tv>>(source: entities);
        }

        //add here any custom service method or override generic service method
        public bool DoNothing()
        {
            return true;
        }
    }

}
