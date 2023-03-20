using System;
using System.Collections.Generic;
using System.Text;

namespace RestApiNDxApiV6.Entity.Queries
{
    public interface IQuery          //<T> where T : class
    {
        string SelectAll { get; }
        string SelectOne { get; }
        string Insert { get; }
        string Update { get; }
        string Delete { get; }
    }
}
