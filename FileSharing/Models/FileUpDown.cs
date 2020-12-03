using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace FileSharing.Models
{
    public class FileUpDown
    {
        [Key]
        public int FShareID { get; set; }
        public string FileID { get; set; }
        public int   DownloadNum { get; set; }
        public int ShareNum { get; set; }
        public DateTime ExpirTime { get; set; }
        public string DesUser { get; set; }

        public FileUpDown()
        {
            this.FileID = "000";//以后遇见000的都是无效的，删除
            this.DownloadNum = 0;
            this.ShareNum = 0;
            this.ExpirTime = DateTime.Parse("2999-08-16");
            //都不能为空，否则找的时候报错 设置为固定日期 谁能撑到2999年(滑稽)
            this.DesUser = "0";//表示共享
        }
        public FileUpDown(string ID,DateTime date,string desuser)
        {
            this.FileID = ID;
            this.DownloadNum = 0;
            this.ShareNum = 1; //如果进表肯定被分享了一次
            this.ExpirTime = date;
            this.DesUser = desuser;
        }

    }
}
