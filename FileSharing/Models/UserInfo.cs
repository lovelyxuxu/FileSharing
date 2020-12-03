using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace FileSharing.Models
{
    public class UserInfo
    {
        [Key]
        public Guid UserID { get; set; }
        public string   UserName{ get; set; }
        public string Password { get; set; }
        public int Age { get; set; }
        public bool Sex { get; set; }
        public DateTime Birthday { get; set; }
        public string IDCard { get; set; }
        public string  Address { get; set; }
        //[EmailAddress]
        public string Email { get; set; }
        public string ReallyName { get; set; }
       // [Phone]
        public string PhoneNum  { get; set; }

        public UserInfo()
        {
            this.Birthday = DateTime.Now;
            this.Age = 0;
            this.Sex = true;
            this.Email = "test@test.test";
            this.PhoneNum = "is null";
        }

    }
}
