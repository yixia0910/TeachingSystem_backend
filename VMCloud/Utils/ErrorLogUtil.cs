/*
*	Author:       Xing Zhihuan
*   Email:        1@roycent.cn	
*	Created:      2019/7/11 18:58:02
*   Description:  
 */
using Quartz.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using VMCloud.Models.DAO;

namespace VMCloud.Utils
{
    public class ErrorLogUtil
    {
        /// <summary>
        /// 用于记录系统日志
        /// </summary>
        /// <param name="ex">发生的错误</param>
        /// <param name="request">发生错误的请求</param>
        static public void WriteLogToFile(Exception ex, HttpRequestMessage request)
        {
            IEnumerable<string> token;
            bool hasToken = request.Headers.TryGetValues("token", out token);

            HttpContextBase context = (HttpContextBase)request.Properties["MS_HttpContext"];
            HttpRequestBase httpRequest = context.Request;
            Stream stream = httpRequest.InputStream;
            string userId = "";
            string redisError = "";
            if(hasToken)
            {
                try
                {
                    RedisHelper redis = RedisHelper.GetRedisHelper();
                    userId = redis.Get<string>(token.First());
                    if (userId == null)
                    {
                        userId = "token无对应ID";
                    }
                }catch(Exception e)
                {
                    userId = "";
                    redisError = e.Message;
                }
            }

            while (ex.InnerException != null)
            {
                ex = ex.InnerException;
            }

            string userIP = httpRequest.Headers.Get("X-Forwarded-For");
            if (userIP.IsNullOrWhiteSpace())
                userIP = httpRequest.UserHostAddress.ToString();

            StreamReader reader = new StreamReader(stream);
            string text = reader.ReadToEnd();

            // 在出现未处理的错误时运行的代码
            StringBuilder _builder = new StringBuilder();
            _builder.Append("\r\n-------------  异常信息   ---------------------------------------------------------------");
            _builder.Append("\r\n发生时间：" + DateTime.Now.ToString());
            _builder.Append("\r\n发生异常页：" + httpRequest.Url.ToString());
            _builder.Append("\r\n异常信息：" + ex.Message + redisError);
            _builder.Append("\r\n用户ID（cookie）：" + userId);
            _builder.Append("\r\n用户代理标识：" + httpRequest.UserAgent);
            _builder.Append("\r\n用户请求方法：" + httpRequest.HttpMethod);
            _builder.Append("\r\n用户查询字符串：" + httpRequest.QueryString.ToString());
            _builder.Append("\r\n用户请求内容：\r\n" + text);
            _builder.Append("\r\n用户窗体变量：" + httpRequest.Form.ToString());
            _builder.Append("\r\n用户IP地址：" + userIP);
            _builder.Append("\r\n错误源：" + ex.Source);
            _builder.Append("\r\n堆栈信息：" + ex.StackTrace);
            _builder.Append("\r\n-----------------------------------------------------------------------------------------\r\n");
            //日志物理路径

            DateTime date = DateTime.Now;
            string path = httpRequest.MapPath(@"~/Log/");
            string month = date.ToString("yyyy-MM");
            if (!System.IO.Directory.Exists(path + month))
                System.IO.Directory.CreateDirectory(path + month);
            string currentDate = date.ToString("yyyy-MM-dd");
            string savePath = path + month + "/" + currentDate + ".log";
            System.IO.File.AppendAllText(savePath, _builder.ToString(), System.Text.Encoding.Default);
        }
    }
}
