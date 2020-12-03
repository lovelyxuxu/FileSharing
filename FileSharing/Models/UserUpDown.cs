using Microsoft.Extensions.Configuration.UserSecrets;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace FileSharing.Models
{
    public class UserUpDown
    {
        [Key]
        public int UShareID { get; set; }
        public string  UserID { get; set; }
        public int ShareNum { get; set; }
        public int DownloadNum { get; set; }
        public UserUpDown()
        {
            this.ShareNum = 0;  //因为和文件不同文件不分享没法被下载
            this.DownloadNum = 0;//而人不分享能无限白嫖
        }
        public UserUpDown(string id)
        {
            this.UserID = id;
            this.ShareNum = 0;  //因为和文件不同文件不分享没法被下载
            this.DownloadNum = 0;//而人不分享能无限白嫖
        }
    }
}
