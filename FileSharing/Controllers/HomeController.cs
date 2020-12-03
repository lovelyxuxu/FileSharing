using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FileSharing.Services;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

namespace FileSharing.Controllers
{
    [Route("api/[controller]")]
    [EnableCors("all")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        #region 字段和依赖
        private readonly IOperation _operation;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHostEnvironment _hostEnvironment;
        public HomeController(IOperation operation,IHttpContextAccessor httpContextAccessor, IHostEnvironment hostEnvironment)
        {
            _operation = operation;
            _httpContextAccessor = httpContextAccessor;
            _hostEnvironment = hostEnvironment;
        }
        #endregion


        #region 1 登录  
        [HttpPost]
        [Route("Login")]
        public IActionResult Login()
        {
            //分为游客和用户注册  想要上传文件需要游客或者用户登录
            string strjson = _httpContextAccessor.HttpContext.Request.Form["strjson"];
            return Ok(_operation.UserLogin(strjson));
        }
        #endregion
        #region 1.1注册  
        [HttpPost]
        [Route("Register")]
        public IActionResult Register()
        {
            string strjson = _httpContextAccessor.HttpContext.Request.Form["strjson"];
            //分为游客和用户注册  想要上传文件需要游客或者用户登录
            return Ok(_operation.UserRegister(strjson));
        }
        #endregion 
        #region 1.2 自动注册登录游客 并且存入cookie中  每次访问自动登录
        [HttpPost]
        [Route("RegLogTourist")]
        public IActionResult RegLogTourist()
        {
            //分为游客和用户注册  想要上传文件需要游客或者用户登录
            string touid =_operation.RegistOrLoginTourist();
            return Ok(touid); 
        }
        #endregion
        #region 2 注销
        [HttpPost]
        [Route("Logout")]
        public IActionResult Logout()
        {
            //分为游客和用户注册  想要上传文件需要游客或者用户登录
            return Ok(_operation.UserLogout());
        }
        #endregion


        #region 3 上传文件
        [HttpPost]
        [Route("FileUpload")]
        public IActionResult PostFileUpload()
        {
            
            _operation.UpFile();
            return Ok("ok");
        }
        #endregion
        #region 4 下载文件
        [HttpGet]
        [Route("FileDown")]
        public  IActionResult GetFileDown(string file)
        {
            string fileid = _httpContextAccessor.HttpContext.Request.Query["fileid"];//可以获得
            //还要获得文件的id
            if (string.IsNullOrEmpty(file))
            {
                return NotFound("空链接");
            }
            // 下载的时候需要看看是否这位用户是否满足条件。
            if (!_operation.CheckCondition(fileid))
            {
                return NotFound("没有权限");
            }


            string path = Path.GetFullPath(_hostEnvironment.ContentRootPath + file);
            var memoryStream = new MemoryStream();
            using (var stream = new FileStream(path, FileMode.Open))
            {
                stream.CopyTo(memoryStream);
            }
            memoryStream.Seek(0, SeekOrigin.Begin);
            string filename = Path.GetFileName(file);
            //文件名必须编码，否则会有特殊字符(如中文)无法在此下载。
            string encodeFilename =System.Web.HttpUtility.UrlEncode(filename, System.Text.Encoding.GetEncoding("UTF-8"));
            Response.Headers.Add("Content-Disposition", "attachment; filename=" + encodeFilename);
            Response.Headers.Add("myfileurl", "aaaaa");
            FileStreamResult fileresult= new FileStreamResult(memoryStream, "application/octet-stream");//文件流方式，指定文件流对应的ContenType。
            fileresult.FileDownloadName = filename.Substring(8);
            
            //增加用户下载次数和文件被下载次数
            _operation.DownFile(fileid);
            
            return fileresult;

        }
        #endregion



        #region 生成链接
        [HttpGet]
        [Route("DownOrShare")]
        public IActionResult DownOrShare(string file)
        {
            string fileid = _httpContextAccessor.HttpContext.Request.Query["fileid"];
            //最原始的方法，拼接字符串
            // https://localhost:44387/api/Home/FileDown?file=%2FUpload%2FOther%2Fc97b4e300002019%E5%B9%B4%E5%BA%A6%E6%95%99%E8%82%B2%E9%83%A8%E8%BF%94%E5%9B%9E%E6%88%90%E7%BB%A9%E8%A1%A81.18.xls
            string encodeFilename = System.Web.HttpUtility.UrlEncode(file, System.Text.Encoding.GetEncoding("UTF-8"));
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("https://localhost:44387/api/Home/FileDown?");
            sb.Append("fileid=");
            sb.Append(fileid);
            sb.Append("&file=");
            sb.Append(encodeFilename);
            //如果是下载 就再调用一下这个链接  如果是分享就返回字符串不调用这个链接 调用分享之后的方法
            return Ok(sb.ToString());
        }
        #endregion

        #region 5 查看下载社区的文件
        [HttpGet]
        [Route("PublicFileLook")]
        public IActionResult GetPublicFileLook(string userid)
        {
            string strjson = _operation.PublicFileLook(userid);
            return Ok(strjson);
        }
        #endregion

        #region 5 查看文件
        [HttpGet]
        [Route("FileLook")]
        public IActionResult GetFileLook( string userid)
        {
            string strjson= _operation.LookFile( userid);
            return Ok(strjson);
        }
        #endregion
        #region 6 分享文件
        [HttpPost]
        [Route("FileShare")]
        public IActionResult PostFileShare()
        {
            string shareinfojson = _httpContextAccessor.HttpContext.Request.Form["shareinfojson"];
            return Ok(_operation.ShareFile(shareinfojson));
        }
        #endregion
        #region 删除文件
        [HttpGet]
        [Route("FileDelete")]
        public IActionResult GetDeleteFile(string fileid)
        {
            bool flag= _operation.DeleteOneFile(fileid);
            return Ok(flag);
        }
        #endregion


        #region 7 获取文件的下载次数
        [HttpGet]
        [Route("FileDownNum")]
        public IActionResult GetFileDownNum(string fileid)
        {
            int num=_operation.DownNumFile(fileid);
            return Ok(num);
        }
        #endregion
        #region 8 获取文件的分享次数
        [HttpGet]
        [Route("FileShareNum")]
        public IActionResult GetFileShareNum(string fileid)
        {
            int num = _operation.ShareNumFile(fileid);
            return Ok(num);
        }
        #endregion
        #region 9 获取用户分享次数
        [HttpGet]
        [Route("UserShareNum")]
        public IActionResult GetUserShareNum(string userid)
        {
            int num = _operation.UserShareFileNum(userid);
            return Ok(num);
        }
        #endregion
        #region 10 获取用户下载次数
        [HttpGet]
        [Route("UserDownNum")]
        public IActionResult GetUserDownNum(string userid)
        {
            int num = _operation.UserDownFileNum(userid);
            return Ok(num);
        }
        #endregion
        #region 11 获取总空间占用
        [HttpGet]
        [Route("SizeUsed")]
        public IActionResult GetSizeUsed()
        {
            string allsize = _operation.GetAllSize();  //前端完善界面  所以这里只传值
            return Ok(allsize);
        }
        #endregion

        #region 测试IHostEnvironment可删除
        [HttpGet]
        [Route("Text")]
        public IActionResult GetText()
        {
            return Ok(_operation.test());
        }
        #endregion


        #region 关注UP
        [HttpPost]
        [Route("Follow")]
        public IActionResult PostFollow()
        {
            string id = _httpContextAccessor.HttpContext.Request.Form["id"];
            _operation.followUP(id);
            return Ok();
        }
        #endregion
        #region 查看全部粉丝
        [HttpPost]
        [Route("LookFans")]
        public IActionResult PostLookFans([FromBody] string upid)
        {
            List<string> list = _operation.LookFans(upid);
            return Ok(list);
        }
        #endregion
        #region 查看全部UP
        [HttpPost]
        [Route("LookUp")]
        public IActionResult PostLoolUp( [FromBody] string fansid)
        {
            List<string> list =_operation.LookUp(fansid);
            return Ok(list);
        }
        #endregion

        #region 获取用户中心信息
        [HttpGet]
        [Route("UserCenterInfo")]
        public IActionResult GetUserCenterInfo(string userid)
        {
            Dictionary<string, string> dic =  _operation.GetUserCenterInfo(userid);
            string json = JsonConvert.SerializeObject(dic);
            return Ok(json);
        }
        #endregion

        #region 获取下载流量最大的文件
        [HttpGet]
        [Route("TheMostDownFlowFile")]
        public IActionResult TheMostDownFlowFile()
        {
            return Ok(_operation.GetTheMostDownFlowFile());
        }
        #endregion

        #region 分享的文件被下载最多的用户  (可能是个集合  返回的是字典)
        [HttpGet]
        [Route("TheMostDownFileUser")]
        public IActionResult TheMostDownFileUser()
        {
            return Ok(_operation.GetTheMostDownFileUser());
        }
        #endregion

    }
}
