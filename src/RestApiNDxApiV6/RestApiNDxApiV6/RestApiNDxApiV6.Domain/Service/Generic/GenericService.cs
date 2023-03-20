using AutoMapper;
using RestApiNDxApiV6.Entity;
using RestApiNDxApiV6.Entity.UnitofWork;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace RestApiNDxApiV6.Domain.Service
{
    public class GenericService<Tv, Te> : IService<Tv, Te> where Tv : BaseDomain
                                      where Te : BaseEntity
    {

        protected IUnitOfWork _unitOfWork;
        protected IMapper _mapper;
        public GenericService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public GenericService()
        {
        }

        public virtual IEnumerable<Tv> GetAll()
        {
            var entities = _unitOfWork.GetRepository<Te>()
            .GetAll();
            return _mapper.Map<IEnumerable<Tv>>(source: entities);
        }
        public virtual Tv GetOne(int id)
        {
            var entity = _unitOfWork.GetRepository<Te>()
                .GetOne(predicate: x => x.Id == id);
            return _mapper.Map<Tv>(source: entity);
        }

        public virtual int Add(Tv view)
        {
            var entity = _mapper.Map<Te>(source: view);
            int id = _unitOfWork.GetRepository<Te>().Insert(entity);
            _unitOfWork.Save();
            return id;
        }

        public virtual int Update(Tv view)
        {
            int retval = _unitOfWork.GetRepository<Te>().Update(view.Id, _mapper.Map<Te>(source: view));
            _unitOfWork.Save();
            return retval;
        }


        public virtual int Remove(int id)
        {
            int retval = _unitOfWork.GetRepository<Te>().Delete(id);
            _unitOfWork.Save();
            return retval;
        }

        public virtual IEnumerable<Tv> Get(Expression<Func<Te, bool>> predicate)
        {
            var entities = _unitOfWork.GetRepository<Te>().Get(predicate);
            return _mapper.Map<IEnumerable<Tv>>(source: entities);
        }
    }
}
