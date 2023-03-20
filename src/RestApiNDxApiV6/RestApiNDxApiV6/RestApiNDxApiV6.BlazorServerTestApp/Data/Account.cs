using System;

namespace RestApiNDxApiV6.BlazorServerTestApp.Data
{
    public class Account
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Description { get; set; }
        public bool IsTrial { get; set; }
        public bool IsActive { get; set; }
        public DateTime SetActive { get; set; }
        public byte[] RowVersion { get; set; }

    }
}
