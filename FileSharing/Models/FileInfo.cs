using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace FileSharing.Models
{
    public class FileInfo
    {
        [Key]
        public Guid FileID { get; set; }
        public string UserID { get; set; }
        public string FileName { get; set; }
        public DateTime UpTime{ get; set; }
        public string Size { get; set; }
        public string Address { get; set; }
        public string Type { get; set; }
       


    }
}
