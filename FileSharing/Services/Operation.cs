using FileSharing.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Internal;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http.Results;
using StackExchange.Redis;
using Microsoft.Extensions.Hosting;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Newtonsoft.Json.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace FileSharing.Services
{
    public class Operation : IOperation
    {
        #region 属性和依赖
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IDatabase _redis;
        private readonly MyShareDbContext _dbContext;
        private readonly IHostEnvironment _hostEnvironment;
        public Operation(IHostEnvironment hostEnvironment, IHttpContextAccessor httpContextAccessor, MyShareDbContext dbContext, IConnectionMultiplexer connectionMultiplexer)
        {
            _httpContextAccessor = httpContextAccessor;
            _dbContext = dbContext;
            _redis = connectionMultiplexer.GetDatabase();
            _hostEnvironment = hostEnvironment;
        }
        #endregion

        #region 测试IHostingEnvironment 可删除
        public string  test()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(_hostEnvironment.ApplicationName);//FileSharing
            sb.AppendLine(_hostEnvironment.ContentRootPath);//E:\study\FileSharing\FileSharing
            sb.AppendLine(_hostEnvironment.EnvironmentName);//Development
            sb.AppendLine("********************");
            sb.AppendLine(Directory.GetCurrentDirectory());//E:\study\FileSharing\FileSharing
            return sb.ToString();
        }
        #endregion

        #region 上传文件
        public void UpFile() //上传文件 可上传多个
        {
            //获得传的文件
            IFormFileCollection formFiles = _httpContextAccessor.HttpContext.Request.Form.Files;
            SaveAllSize(formFiles.Sum(f => f.Length),'+');

            foreach (IFormFile ff in formFiles)
            {
                string filename = Path.GetFileName(ff.FileName);
                string ext = Path.GetExtension(filename);
                //前端判断文件大小，这里就不判断了

                string path = _hostEnvironment.ContentRootPath+"/Upload/";//路径  要保存的主文件夹
                //按类型分文件夹
                string type = GetDirNameByExt(ext);  //获得的不仅仅是文件夹名字也是要存储的type数据
                string path1 = type + "/";
                //把上传的文件进行分类  用文件夹进行分类
                if (!Directory.Exists(path + path1))  //判断文件夹是否存在 
                {
                    Directory.CreateDirectory(path + path1);//如果不存在 创建一个
                }
                string address = "/Upload/"+ path1 + Guid.NewGuid().ToString().Substring(0, 8) + filename;
                //防止文件重名  但是存储的时候还是原来文件名;
                using (var stream = new FileStream(address, FileMode.Create))
                {
                    ff.CopyTo(stream);
                }

                Models.FileInfo fileInfo = new Models.FileInfo();
                fileInfo.FileID = Guid.NewGuid();
                fileInfo.UserID = _httpContextAccessor.HttpContext.Request.Cookies["touristid"];
                fileInfo.FileName = filename;
                fileInfo.UpTime = DateTime.Now;
                fileInfo.Size = ff.Length.ToString();//还是要存字节  因为要还要减。
                fileInfo.Address = address;
                fileInfo.Type = type;
                _dbContext.fileInfo.Add(fileInfo);
            }
            _dbContext.SaveChanges();
        }
        #endregion
        #region 统计总空间占用
        public string GetAllSize()
        {
            return GetSize(Convert.ToInt64(_redis.StringGet("ZX_FileSharing")));
        }
        #endregion
        #region 私有的用redis更新服务器中文件总字节
        private void SaveAllSize(long size,char oper)
        {
            if(oper == '+')
                _redis.StringSet("ZX_FileSharing", Convert.ToInt64(_redis.StringGet("ZX_FileSharing")) + size);
            else if (oper == '-')
                _redis.StringSet("ZX_FileSharing", Convert.ToInt64(_redis.StringGet("ZX_FileSharing")) - size);
        }
        #endregion
        #region 私有的返回文件类型
        private string GetDirNameByExt(string extAa)
        {
            string ext= extAa.ToLower();
            string s="" ;
            string[] Image = { ".jpg", ".jpeg", ".png", ".gif", ".bmp" ,".psd",".pdd"};
            string[] Video = { ".avi", ".mp4", ".rm", ".mov", ".rmvb",".flv","3gp" ,".asf",".navi",".mpeg",".divx"};
            string[] Audio = { ".mp3", ".wma", ".acm", ".cda", ".wav",".ra",".midi",".ogg",".ape",".flac",".acc" };
            string[] Text = { ".txt", ".doc",".docx",".pptx", ".ppt", ".xlsx", ".pdf",".md" };
            //注意大小写
            if (Array.IndexOf(Image,ext)>=0)//图片
            {
                s = "Image";
            }
            else if (Array.IndexOf(Video, ext) >= 0)//视频
            {
                s = "Video";
            }
            else if (Array.IndexOf(Audio, ext) >= 0)//音频
            {
                s = "Audio";
            }
            else if (Array.IndexOf(Text, ext) >= 0)//文本
            {
                s = "Text";
            }
            else//其他
            {
                s = "Other";
            }
            return s;
        }
        #endregion
        #region 私有的获取把字节变换单位
        private string GetSize(long bytesize)
        {
            string s = "";
            if (bytesize < 1024)  //b
            {
                s = string.Format("{0:F}B",bytesize);
            }
            else if (bytesize<1024*1024) //kb
            {
                s = string.Format("{0:F}KB", bytesize/1024.0);
            }
            else if(bytesize < 1024 *1024* 1024)//mb
            {
                s = string.Format("{0:F}MB", bytesize/1024.0/1024);
            }
            else //GB
            {
                s = string.Format("{0:F}GB", bytesize / 1024.0 /1024/ 1024);
            }
            return s;
        }
        #endregion

        #region 下载文件
        public void DownFile(string fileid) //下载文件
        {
            //如果是自己的文件并没有分享则不操作
            FileUpDown fileUp = _dbContext.fileUpDown.Where(c => c.FileID == fileid).FirstOrDefault();
            if (fileUp != null)
            {
                string userid=_httpContextAccessor.HttpContext.Request.Cookies["touristid"];
                if (!string.IsNullOrEmpty(userid))
                {  //如果下载的是用户，用户下载量也++
                    UserUpDown userUp = _dbContext.userUpDown.Where(c => c.UserID == userid).FirstOrDefault();
                    userUp.DownloadNum++;
                    _dbContext.Attach<UserUpDown>(userUp);
                    _dbContext.Entry<UserUpDown>(userUp).Property(c => c.DownloadNum).IsModified = true;
                }
                fileUp.DownloadNum++;
                _dbContext.Attach<FileUpDown>(fileUp);
                _dbContext.Entry<FileUpDown>(fileUp).Property(c => c.DownloadNum).IsModified = true;
                //分享的不管哪种方式下载的，文件的下载量肯定要++
                _dbContext.SaveChanges();
            }
        }
        #endregion

     

        #region 查看文件 并返回json字符串  未做分页查询
        public string LookFile(string userid) //查看文件
        {

            var q= _dbContext.fileInfo.Where(c => c.UserID == userid).ToList();
            string strjson= JsonConvert.SerializeObject(q);
            return strjson;
        }
        #endregion

        #region 私有查询单个文件返回FileInfo对象
        private Models.FileInfo GetOneFile(string fileid)
        {
            return _dbContext.fileInfo.Where(c => c.FileID.ToString() == fileid).FirstOrDefault();
        }
        #endregion
        #region 删除文件
        public bool DeleteOneFile(string fileid)
        {
            Models.FileInfo delfile = GetOneFile(fileid);
            string delfileid = delfile.FileID.ToString();
            FileUpDown fff = _dbContext.fileUpDown.Where(c => c.FileID == fileid).FirstOrDefault();
            if (fff != null)
            {
                _dbContext.fileUpDown.Remove(fff);
            }
            SaveAllSize(Convert.ToInt64(delfile.Size), '-') ;
            _dbContext.fileInfo.Remove(delfile);
            _dbContext.SaveChanges();
            return false;
        }
        #endregion

        #region 分享文件
        public string ShareFile(string shareinfojson) //分享文件
        {
            //分享应该就是分享链接，然后下载
            //区别就是增加了自定义的判断条件
            // 参数有 fileid  expirtime  指定的desuser 
            JObject shareinfo = (JObject)JsonConvert.DeserializeObject(shareinfojson);
            string fileid = shareinfo["id"].ToString().ToUpper();
            string expirtime = shareinfo["expirtime"].ToString();
            string desuser = shareinfo["desuser"].ToString();
            if (string.IsNullOrEmpty(expirtime))
            {
                expirtime = "2999-08-16"; 
                //都不能为空，否则找的时候报错 设置为固定日期 谁能撑到2999年(滑稽)
            }
            if (string.IsNullOrEmpty(desuser))
            {
                desuser = "0";//表示共享
            }
            //先查询是否分享过  如果分享过 分享次数+1  过期时间指定user进行修改
            FileUpDown sharefile= _dbContext.fileUpDown.Where(c => c.FileID == fileid).FirstOrDefault();
            if (sharefile != null)
            {
                sharefile.ShareNum = sharefile.ShareNum + 1;
                sharefile.ExpirTime = DateTime.Parse(expirtime); 
                sharefile.DesUser = desuser;
                _dbContext.fileUpDown.Attach(sharefile);
                _dbContext.Entry(sharefile).Property(u => u.ShareNum).IsModified = true;
                _dbContext.Entry(sharefile).Property(u => u.ExpirTime).IsModified = true;
                _dbContext.Entry(sharefile).Property(u => u.DesUser).IsModified = true;
                _dbContext.Entry<FileUpDown>(sharefile).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            }
            else
            {
                sharefile = new FileUpDown(fileid,DateTime.Parse( expirtime), desuser);
                _dbContext.Entry<FileUpDown>(sharefile).State = Microsoft.EntityFrameworkCore.EntityState.Added;
            }

            string userid = _dbContext.fileInfo.Where(c => c.FileID.ToString() == fileid).Select(c => c.UserID).FirstOrDefault().ToString();
            UserUpDown useri = _dbContext.userUpDown.Where(c => c.UserID == userid).FirstOrDefault();
            if (useri != null)
            {
                useri.ShareNum = useri.ShareNum + 1;
                _dbContext.userUpDown.Attach(useri);
                _dbContext.Entry<UserUpDown>(useri).Property(u => u.ShareNum).IsModified = true;
            }
            else
            {
                useri = new UserUpDown(userid);
                useri.ShareNum = useri.ShareNum + 1;
                _dbContext.Entry<UserUpDown>(useri).State = Microsoft.EntityFrameworkCore.EntityState.Added;
            }

            _dbContext.SaveChanges();

            //查询address 
            return getLink(fileid);

        }
        #endregion
        #region 私有查询分享链接
        private string getLink(string id)
        {
            string fileaddress=_dbContext.fileInfo.Where(c => c.FileID.ToString() == id).Select(c => c.Address).ToString();
            return string.Format("http:XXXXX{0}",fileaddress);
        }
        #endregion

        #region 文件被分享次数
        public int ShareNumFile(string fileid) //文件被分享次数
        {
            //1 获取文件ID
            //string fileid = null;
            //2 查看
            int q =Convert.ToInt32( _dbContext.fileUpDown.Where(c => c.FileID == fileid).Select(c=>c.ShareNum));
            return q;
        }
        #endregion
        #region 文件被下载次数
        public int DownNumFile(string fileid)//文件被下载次数
        {
            //1 获取文件ID
            //string fileid = null;
            //2 查看
            int q = Convert.ToInt32(_dbContext.fileUpDown.Where(c => c.FileID == fileid).Select(c => c.DownloadNum));
            return q;
        }
        #endregion
        #region 用户总下载次数
        public int UserDownFileNum(string userid)//用户总下载次数
        {
            //1 获取用户ID
            //string userid = null;
            //2 查看
            int q = Convert.ToInt32(_dbContext.userUpDown.Where(c => c.UserID == userid).Select(c => c.DownloadNum));
            return q;
        }
        #endregion
        #region 用户总分享次数
        public int UserShareFileNum(string userid)//用户总分享次数
        {
            //1 获取用户ID
            //string userid = null;
            //2 查看
            int q = Convert.ToInt32(_dbContext.userUpDown.Where(c => c.UserID == userid).Select(c => c.ShareNum));
            return q;
        }
        #endregion

        #region 用户登录
        public bool UserLogin(string strjson)
        {
            //传入json字符串  传的是id 
            string id = "";
            JObject loginfo = (JObject)JsonConvert.DeserializeObject(strjson);
            if (loginfo.Count>1)//用户登录
            {
                string username = loginfo["username"].ToString();
                string password = loginfo["password"].ToString();
                id = LookUserID( username,  password);
                if (string.IsNullOrEmpty(id))
                {
                    return false;
                }
            }
            else
            {
                id = loginfo["touristid"].ToString();
                if (LookUserById(id) == null)
                {
                    return false;
                }
            }
            AddCookie(id);
            
            return true;
        }
        #endregion
        #region 查找id
        private string LookUserID(string username,string password)
        {
            return _dbContext.userInfo.Where(c => c.UserName == username && c.Password == password ).Select(c => c.UserID).FirstOrDefault().ToString();
        }
        #endregion

        #region 查找用户
        private UserInfo LookUserById(string userid)
        {
            return _dbContext.userInfo.Where(c => c.UserID.ToString() ==userid).FirstOrDefault();

        }
        #endregion

        #region 用户注册
        public bool UserRegister(string strjson)
        {
            JObject jObject = (JObject)JsonConvert.DeserializeObject(strjson);
            UserInfo user = new UserInfo();
            user.UserID = Guid.NewGuid();
            user.UserName = jObject["username"].ToString();
            user.Password = jObject["password"].ToString();
            if (!string.IsNullOrEmpty(jObject["sex"].ToString()))
            {
                user.Sex = jObject["sex"].ToString() == "true" ? true : false;
            }
            if(!string.IsNullOrEmpty(jObject["birthday"].ToString()))
            {
                user.Birthday = DateTime.Parse(jObject["birthday"].ToString());
            }
            if (!string.IsNullOrEmpty(jObject["age"].ToString()))
            {
                user.Age = Convert.ToInt32(jObject["age"].ToString());
            }
            _dbContext.userInfo.Add(user);

            UserUpDown userUpDown = new UserUpDown();
            userUpDown.UserID = user.UserID.ToString();
            userUpDown.ShareNum = 0;
            userUpDown.DownloadNum = 0;
            _dbContext.userUpDown.Add(userUpDown);

            UpInfo upInfo = new UpInfo();
            upInfo.UserID = user.UserID.ToString();

            //查询upandfans表查看曾经是否关注过其他up  修改follownum
            int follownum= _dbContext.upAndFans.Where(c => c.fansID == upInfo.UserID).Count();
             if (follownum > 0)
            {
                upInfo.FollowNum = follownum;
            }
            //查询fileinfo表，看曾经是否上传过文件。修改tweet
            int tweetnum = _dbContext.fileInfo.Where(c => c.UserID == upInfo.UserID).Count();
            if (tweetnum > 0)
            {
                upInfo.TweetNum = tweetnum;
            }
            _dbContext.upInfo.Add(upInfo);

            _dbContext.SaveChanges();
            return true;
            
        }
        #endregion
        #region 用户注销
        public bool UserLogout()
        {
            //把cookie设置为空
            AddCookie("");
            return true;
        }
        #endregion

        #region 游客Log or reg
        public string RegistOrLoginTourist()
        {
            string s = "";
            if (CheckCookie())  //如果此浏览器没游客注册
            {
                UserInfo tourist = new UserInfo();
                tourist.UserID = Guid.NewGuid();
                tourist.Birthday = DateTime.Now;
                s = tourist.UserID.ToString();
                AddCookie(s);
                _dbContext.userInfo.Add(tourist);

                UserUpDown userUpDown = new UserUpDown();
                userUpDown.UserID = tourist.UserID.ToString();
                userUpDown.ShareNum = 0;
                userUpDown.DownloadNum = 0;
                _dbContext.userUpDown.Add(userUpDown);

                _dbContext.SaveChanges();
                AddCookie(tourist.UserID.ToString());
            }
            //登录 返回id
            return s;
        }
        #endregion
        #region 判断是否有cookie
        private bool CheckCookie()
        {
            bool flag = true; //添加cookie  没有cookie
            string touristid = _httpContextAccessor.HttpContext.Request.Cookies["touristid"];
            if (touristid != null)
            {
                flag = false;
            }
            return flag;
        }
        #endregion
        #region 添加Cookie
        private void AddCookie(string touristid)
        {
             _httpContextAccessor.HttpContext.Response.Cookies.Append("touristid", touristid, new CookieOptions
            {
                Expires = DateTime.Now.AddYears(1),
                SameSite = SameSiteMode.Lax,
                Domain = "localhost"
            });
        }
        #endregion

        #region 关注UP

        public void followUP(string id)
        {
            UpAndFans model = new UpAndFans();
            JObject shareinfo = (JObject)JsonConvert.DeserializeObject(id);
            model.upID = shareinfo["upId"].ToString();
            model.fansID = shareinfo["fansId"].ToString();
            _dbContext.upAndFans.Add(model);

            //up粉丝加一  粉丝关注加1  都自动创建的 不会为null
            UpInfo up = GetUpByID(model.upID);
            up.FansNum = up.FansNum + 1;
            _dbContext.upInfo.Attach(up);
            _dbContext.Entry<UpInfo>(up).Property(c => c.FansNum).IsModified = true;

            UpInfo fans = GetUpByID(model.fansID);
            fans.FansNum = fans.FollowNum + 1;
            _dbContext.upInfo.Attach(fans);
            _dbContext.Entry<UpInfo>(fans).Property(c => c.FansNum).IsModified = true;

            _dbContext.SaveChanges();
        }
        #endregion
        #region 通过id查看获取up
        private UpInfo GetUpByID(string id)
        {
            return _dbContext.upInfo.Where(c => c.UserID == id).FirstOrDefault();
        }
        #endregion


        #region 查看粉丝
        public List<string> LookFans(string upid)
        {
            List<string> fanslist= _dbContext.upAndFans.Where(c=>c.upID==upid).Select(c=>c.fansID).ToList();
            return fanslist;
        }
        #endregion
        #region 查看Up
        public List<string> LookUp(string fansid)
        {
            List<string> uplist = _dbContext.upAndFans.Where(c => c.fansID == fansid).Select(c => c.upID).ToList();
            return uplist;
        }
        #endregion

        #region 获取用户中心信息
        public Dictionary<string, string> GetUserCenterInfo(string id)
        {
            UserInfo user = _dbContext.userInfo.Where(c => c.UserID.ToString() == id).FirstOrDefault();
            if (user != null)
            {

            }
            UpInfo upInfo = _dbContext.upInfo.Where(c => c.UserID == id).FirstOrDefault();
            Dictionary<string, string> dic = new Dictionary<string, string>();
            if (user.UserName != null)
            {
                dic.Add("name", user.UserName);
                dic.Add("sex", user.Sex ? "男" : "女");
            }
            else
            {
                dic.Add("name", user.UserID.ToString());
                dic.Add("sex", "无");
            }
            dic.Add("birthday", user.Birthday.ToString("yyyyMMdd"));
            dic.Add("fans",upInfo.FansNum.ToString());
            dic.Add("follow", upInfo.FollowNum.ToString());
            dic.Add("tweet", upInfo.TweetNum.ToString());
        
            return dic;
        }
        #endregion

        #region 查看是否满足下载条件
        public bool CheckCondition(string fileid)
        {

            //判断是不是公开的文件，因为可能有人不是用户获得的连接。
            Models.FileUpDown file = _dbContext.fileUpDown.Where(c => c.FileID == fileid).FirstOrDefault();
            int result= DateTime.Now.CompareTo(file.ExpirTime);
            if (result > 0)
            {
                //过期了
                return false;
            }
            if (file.DesUser =="0")
            {
                //表示公开
                return true;
            }

            //下边表示不公开的文件
            if (CheckCookie())
            {
                //没cookie  (上边写的有点不顺  这个确实是没有cookie为true)
                return false;  //
            }
            string touristid = _httpContextAccessor.HttpContext.Request.Cookies["touristid"];

            if (touristid == file.DesUser)
            {
                return true; //如果为被指定的人，下载
            }

            int isHave = _dbContext.fileInfo.Where(c => c.FileID.ToString() == fileid && c.UserID == touristid).Count();
            if (isHave>0)
            {//表示是自己的文件
                return true;
            }
            return false;
        }
        #endregion

        #region 获取下载流量最大的文件
        public Dictionary<string, string> GetTheMostDownFlowFile()
        {
            //获取 文件的大小 和下载数量  两个相乘
            var list = (from f in _dbContext.fileInfo
                        from fu in _dbContext.fileUpDown
                        where f.FileID.ToString() == fu.FileID
                        select new { fileid= f.FileID.ToString(),size=Convert.ToInt64( f.Size), fu.DownloadNum }).ToList();
            // 要是弄成下边这种就可以了  
            //string json = JsonConvert.SerializeObject(list);  //前端输出这种

            //使用匿名类

            Dictionary<string, long> dic = new Dictionary<string, long>();
            foreach (var l in list)
            {
                dic[l.fileid] = l.size * l.DownloadNum;
            }

            //  KeyValuePair<string,int> a = dic.OrderBy(c => c.Value).FirstOrDefault();  //去一个的情况下

            //但是可能不止一个最高的
            
            Dictionary<string, string> dic2 = new Dictionary<string, string>();
            long i = 0;
            //dic.Add("aaa", 99999999);
            //dic.Add("bbb", 99);
            //IEnumerable<KeyValuePair<string, long>> a=dic.OrderByDescending(c => c.Value);
            foreach (KeyValuePair<string, long> d in dic.OrderByDescending(c => c.Value))//从大到小的 
            {
                if (d.Value > i)
                {
                    dic2.Clear();
                    dic2.Add(d.Key, GetSize(d.Value)); //转换为 。。。MB  。。K之类的
                    i = d.Value;
                }
                else if (d.Value == i)
                {
                    dic2.Add(d.Key, GetSize(d.Value));
                }
                else
                {
                    break; //如果小于就不用比了  后边肯定都小
                }
            }
           // { 86C5820D - CD09 - 47A6 - 845D - 5824F7C1343F: "0.20MB", 45A0CB18 - F8D2 - 432B - 9104 - B5354EC5BCE0: "0.00B"}
            return dic2;
        }
        #endregion

        #region 分享的文件被下载最多的用户
        public Dictionary<string, int> GetTheMostDownFileUser()
        {
            //方法一
            var list = (from f in _dbContext.fileInfo
                     from fu in _dbContext.fileUpDown
                     where f.UserID != null && f.FileID.ToString() == fu.FileID
                     select new { userid = f.UserID, downnum = fu.DownloadNum }).ToList();
            Dictionary<string, int> dic = new Dictionary<string, int>();
            foreach(var l in list)
            {
                if (dic.ContainsKey(l.userid))
                {
                    dic[l.userid] += l.downnum;
                }
                else
                {
                    dic.Add(l.userid, l.downnum);
                }
            }
            
            //  KeyValuePair<string,int> a = dic.OrderBy(c => c.Value).FirstOrDefault();  //去一个的情况下

            //但是可能不止一个最高的

            Dictionary<string, int> dic2 = new Dictionary<string, int>();
            int i = 0;
            foreach (KeyValuePair<string, int> d in dic.OrderByDescending(c=>c.Value))//从大到小的 
            {
                if (d.Value > i)
                {
                    dic2.Clear();
                    dic2.Add(d.Key,d.Value);
                    i = d.Value;
                }
                else if(d.Value==i)
                {
                    dic2.Add(d.Key, d.Value);
                }
                else
                {
                    break; //如果小于就不用比了  后边肯定都小
                }
            }
            //{ a671eb9f - f860 - 4cc4 - 813b - 9260c04ea1cf: 100}


            //方法二
            //sql中 创建一个视图  为fileinfo表和fileupdown表
            //里边有 userid(去掉为空的user)  文件id  文件的下载量  三个字段

            //按userid划分小组，用sum把每组的下载量放在一起 （这个是当前所有的用户，以后如果需要可以截出来）

            //取最大的

            return dic2;
        }
        #endregion

        #region 查看下载社区的文件
        public string PublicFileLook(string userid)
        {

            var q = from f in _dbContext.fileInfo
                    from fu in _dbContext.fileUpDown
                    where f.FileID.ToString() == fu.FileID
                    select new {f.FileID,f.UserID,f.FileName,f.UpTime,f.Size,f.Address,f.Type,fu.ShareNum,fu.DownloadNum,fu.ExpirTime };
            string strjson = JsonConvert.SerializeObject(q);
            return strjson;
        }
        #endregion

    }
}
