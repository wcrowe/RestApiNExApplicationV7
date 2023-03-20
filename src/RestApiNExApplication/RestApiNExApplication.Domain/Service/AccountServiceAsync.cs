using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RestApiNExApplication.Entity;
using RestApiNExApplication.Entity.UnitofWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace RestApiNExApplication.Domain.Service
{
    public class AccountServiceAsync<Tv, Te> : GenericServiceAsync<Tv, Te>
                                        where Tv : AccountViewModel
                                        where Te : Account
    {
        //DI must be implemented specific service as well beside GenericAsyncService constructor
        public AccountServiceAsync(IUnitOfWork unitOfWork, IMapper mapper)
        {
            if (_unitOfWork == null)
                _unitOfWork = unitOfWork;
            if (_mapper == null)
                _mapper = mapper;
        }

        public virtual async Task<IEnumerable<Tv>> GetAll(Pagination pagination)
        {
            var queryable = await Task.FromResult(_unitOfWork.Context.Accounts.AsQueryable());
            var entities = await queryable.Paginate(pagination, out PaginationPagesCnt).ToListAsync();
            return _mapper.Map<IEnumerable<Tv>>(source: entities);
        }
        //add here any custom service method or override genericasync service method
        //...
    }

}
