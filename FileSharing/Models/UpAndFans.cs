using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace FileSharing.Models
{
    public class UpAndFans
    {
        [Key]
        public  int Id { get; set; }
        public string  upID { get; set; }
        public string  fansID { get; set; }
    }
}
