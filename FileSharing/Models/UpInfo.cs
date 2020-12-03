using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace FileSharing.Models
{
    public class UpInfo
    {
        [Key]
        public int Id { get; set; }
        public string UserID { get; set; }
        public int FansNum { get; set; }
        public int FollowNum { get; set; }
        public int TweetNum { get; set; }
        public DateTime BeginTime { get; set; }

        public UpInfo()
        {
            this.FansNum = 0;
            this.FollowNum = 0;
            this.TweetNum = 0;
            this.BeginTime = DateTime.Now;
        }
    }
}
