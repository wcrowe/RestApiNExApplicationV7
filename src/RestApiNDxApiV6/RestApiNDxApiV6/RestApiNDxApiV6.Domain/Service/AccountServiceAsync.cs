﻿using AutoMapper;
using RestApiNDxApiV6.Entity;
using RestApiNDxApiV6.Entity.UnitofWork;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace RestApiNDxApiV6.Domain.Service
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

        //add here any custom service method or override genericasync service method
        //...
    }

}
