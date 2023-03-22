using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace RestApiNExApiV6.Entity
{
    /// <summary>
    /// A user attached to an account
    /// </summary>
    public class User : BaseEntity
    {
        [Required]
        [StringLength(20)]
        public string FirstName { get; set; }
        [Required]
        [StringLength(20)]
        public string LastName { get; set; }
        [StringLength(30)]
        public string UserName { get; set; }
        [Required]
        [StringLength(30)]
        public string Email { get; set; }
        [StringLength(255)]
        public string Description { get; set; }
        public bool IsAdminRole { get; set; }
        [StringLength(255)]
        public string Roles { get; set; }
        public bool IsActive { get; set; }
        [StringLength(255)]
        public string Password { get; set; }   //stored encrypted
        [StringLength(255)]
        public string RefreshToken { get; set; }
        public DateTime RefreshTokenExpiryDateTime { get; set; }
        [NotMapped]
        public string DecryptedPassword
        {
            get { return Decrypt(Password); }
            set { Password = Encrypt(value); }
        }
        public int AccountId { get; set; }


        public virtual Account Account { get; set; }

        private string Decrypt(string cipherText)
        {
            return EntityHelper.Decrypt(cipherText);
        }
        private string Encrypt(string clearText)
        {
            return EntityHelper.Encrypt(clearText);
        }
    }
}
