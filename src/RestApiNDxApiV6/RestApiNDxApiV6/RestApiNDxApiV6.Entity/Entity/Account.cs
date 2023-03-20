using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace RestApiNDxApiV6.Entity
{
    /// <summary>
    /// A account with users
    /// </summary>
    public class Account : BaseEntity
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Description { get; set; }
        public bool IsTrial { get; set; }
        public bool IsActive { get; set; }
        public DateTime SetActive { get; set; }

        public virtual ICollection<User> Users { get; set; }

    }




}
