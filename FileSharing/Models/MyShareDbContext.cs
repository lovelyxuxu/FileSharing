using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace FileSharing.Models
{
    public class MyShareDbContext : DbContext
    {
        public MyShareDbContext(DbContextOptions options)
             : base(options)
        {
        }

        public DbSet<UserInfo> userInfo { get; set; }
        public DbSet<UserUpDown> userUpDown { get; set; }
        public DbSet<FileInfo> fileInfo { get; set; }
        public DbSet<FileUpDown> fileUpDown { get; set; }
        public DbSet<UpAndFans> upAndFans { get; set; }
        public DbSet<UpInfo> upInfo { get; set; }
    }
}
