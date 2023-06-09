﻿using AutoMapper;
using RestApiNDxApiV6.Entity;
using RestApiNDxApiV6.Entity.UnitofWork;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace RestApiNDxApiV6.Domain.Service
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

        //add here any custom service method or override generic service method
        public bool DoNothing()
        {
            return true;
        }
    }

}
