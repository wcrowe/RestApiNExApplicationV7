using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RestApiNDxApiV6.Api.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class DDosAttackProtectedAttribute : Attribute
    {
    }
}
