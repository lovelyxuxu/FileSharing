using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FileSharing.Services
{
    public interface IOperation
    {
        /// <summary>
        /// 上传文件
        /// </summary>
        void UpFile();

        /// <summary>
        /// 下载文件
        /// </summary>
        void DownFile(string fileid);

        /// <summary>
        /// 查看文件
        /// </summary>
        string LookFile(string userid);

        /// <summary>
        /// 分享文件
        /// </summary>
        string ShareFile(string shareinfojson);

        /// <summary>
        /// 文件被下载次数
        /// </summary>
        int DownNumFile(string fileid);

        /// <summary>
        /// 文件被分享次数
        /// </summary>
        int ShareNumFile(string fileid);
        bool UserLogin(string strjson);
        bool UserRegister(string strjson);

        /// <summary>
        /// 用户分享总次数
        /// </summary>
        int UserShareFileNum(string userid);

        /// <summary>
        /// 用户下载总次数
        /// </summary>
        int UserDownFileNum(string userid);

        /// <summary>
        /// 获取总空间占用
        /// </summary>
        /// <returns></returns>
         string GetAllSize();

        string test();
        bool UserLogout();
        string RegistOrLoginTourist();
        bool DeleteOneFile(string fileid);
        void followUP(string id );
        List<string> LookFans(string upid);
        List<string> LookUp(string fansid);
        Dictionary<string, string> GetUserCenterInfo(string id);
        bool CheckCondition(string file);
        Dictionary<string, string> GetTheMostDownFlowFile();
        Dictionary<string, int> GetTheMostDownFileUser();
        string PublicFileLook(string userid);
    }
}
