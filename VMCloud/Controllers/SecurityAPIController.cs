using System;
using System.Collections.Generic;
using System.Linq;
//using System.Net;
using System.Net.Http;
using System.Web.Http;
using VMCloud.Utils;
using VMCloud.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VMCloud.Models.DAO;
using System.Web;
using System.Text.RegularExpressions;
using System.Collections.Specialized;
using System.Configuration;
using System.Web.Http;
using Quartz.Util;
using System.Text;

namespace VMCloud.Controllers
{
    /// <summary>
    /// 账号相关API
    /// </summary>
    //[RoutePrefix("api")]
    public class SecurityAPIController : ApiController
    {
        private RedisHelper redis;
        private static readonly string SSOServer = ConfigurationManager.AppSettings["SSOServer"];
        private static readonly string MyServer = ConfigurationManager.AppSettings["MyServer"];

        /// <summary>
        /// 初始化两个Helper
        /// </summary>
        SecurityAPIController()
        {
            redis = RedisHelper.GetRedisHelper();
        }

        /// <summary>
        /// 注册账户
        /// </summary>
        /// <param name="account">id,name,password</param>
        /// <returns>token</returns>
        [Route("security/register"),HttpPost]
        public HttpResponseMessage Register([FromBody]JObject account)
        {
            try
            {
                var jsonParams = HttpUtil.Deserialize(account);

                string id = jsonParams.id;
                string pwd = ConfigurationManager.AppSettings["DefaultUserPasswd"];
                string name = jsonParams.name;
                string email = jsonParams.email;

                pwd = HttpUtil.Encrypt(pwd, System.Text.Encoding.UTF8);

                User user = UserDao.GetUserById(id.ToLower());
                if (user == null)
                {
                    return new Response(1001, "用户不存在").Convert();
                }
                if(user.is_accept!=null)
                {
                    return new Response(1001, "该用户已经激活").Convert();
                }
                user.passwd = pwd;
                user.name = name;
                user.email = jsonParams.email;
                user.is_accept = false;
                UserDao.ChangeInfo(user);

                if (user.email != null)
                {
                    Dictionary<string, string> retData = new Dictionary<string, string>();
                    string uuid = System.Guid.NewGuid().ToString();
                    redis.Set(uuid, id, 60);
                    retData.Add("id", id);
                    retData.Add("token", uuid);
                    string href = ConfigurationManager.AppSettings["webBase"] + "/security/activateAccount" + "?id=" + user.id + "&token=" + uuid;
                    string res = EmailUtil.SendEmail("账户激活", "请点击以下网址来激活用户", user.id, 1, href);
                    //todo: 发送激活邮件
                    return new Response(1001, "发送激活邮件", retData).Convert();
                }
                return new Response(1001, "成功激活").Convert();
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return new Response(4001).Convert();
            }



        }

        /// <summary>
        /// 登录API by XZH 
        /// 2019.7.11
        /// </summary>
        /// <param name="account">id，password，code以及token</param>
        /// <returns>token</returns>
        [Route("security/login"),HttpPost]
        public HttpResponseMessage Login([FromBody]JObject account)
        {
            try
            {
                
                var jsonParams = HttpUtil.Deserialize(account);
                
                string id = jsonParams.id;
                string pwd = jsonParams.password;
                string code = jsonParams.code;
                string auth = jsonParams.token;   //DONE:字段名更新
                string service = jsonParams.service;
                //TODO: 请求地址验证（白名单）

                User user = UserDao.GetUserById(id.ToLower());
                if(user == null)
                {
                    user = UserDao.GetUserByNickName(id);
                    if(user==null)
                        return new Response(1001, "用户不存在").Convert();
                    id = user.id;
                }
                if (pwd.Equals(user.passwd))
                {
                    if(!redis.IsSet(auth))
                    {
                        return new Response(1001, "验证码不存在").Convert();
                    }
                    string corrCode = redis.Get<string>(auth);
                    if (!(corrCode.ToUpper().Equals(code.ToUpper())))
                    {
                        redis.Delete(auth);
                        return new Response(1001, "验证码错误").Convert();
                    }

                    bool login = redis.IsSet(id);
                    login = false;
                    if (login)
                    {
                        string expiredToken = redis.Get<string>(id);
                        redis.Delete(expiredToken);
                        redis.Delete(id);
                    }
                    string uuid = System.Guid.NewGuid().ToString();
                    redis.Set(uuid, id, 60);
                    redis.Set(id, uuid, 60);
                    Dictionary<string, string> retData = new Dictionary<string, string>
                    {
                        { "authorization", uuid },
                        { "role", user.role.ToString() },
                        { "name", user.name },
                        { "userid",user.id},
                        { "is_accept", user.is_accept.ToString() }
                    };
                    redis.Delete(auth);
                    if (service != null)
                    {
                        retData.Add("service", service);
                        LogUtil.Log(Request, "登录", service, id, user.role);
                        return new Response(1003, "登录成功", retData).Convert();
                    }
                    LogUtil.Log(Request, "登录", id, id, user.role);
                    
                    return new Response(1001, "登录成功", retData).Convert(); ;
                }
                else
                {
                    redis.Delete(auth);
                    return new Response(1001, "密码错误").Convert();
                }
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return new Response(4001).Convert();
            }
        }

        /// <summary>
        /// 注销接口
        /// </summary>
        /// <returns>2001未登录账户|1001注销成功</returns>
        [Route("security/logout"),HttpPost]
        public HttpResponseMessage Logout()
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string id = redis.Get<string>(signature);
                redis.Delete(id);
                redis.Delete(signature);
                return new Response(1001, "注销成功").Convert();
            }catch(Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }
        /// <summary>
        /// SSO认证接口
        /// </summary>
        /// <param name="ticketInfo">ticket:""</param>
        /// <returns></returns>
        [Route("security/ssoLogin"),HttpPost]
        public HttpResponseMessage SSOLogin(JObject ticketInfo)
        {
            var jsonParams = HttpUtil.Deserialize(ticketInfo);
            string ticket = jsonParams.ticket;
            
            string url = SSOServer + "serviceValidate?ticket=" + ticket + "&" +
                        "service=" + MyServer;
            System.IO.StreamReader Reader = new System.IO.StreamReader(new System.Net.WebClient().OpenRead(url));
            string resp = Reader.ReadToEnd();
            Reader.Close();

            System.Xml.NameTable nt = new System.Xml.NameTable();
            System.Xml.XmlNamespaceManager nsmgr = new System.Xml.XmlNamespaceManager(nt);
            System.Xml.XmlParserContext context = new System.Xml.XmlParserContext(null, nsmgr, null, System.Xml.XmlSpace.None);
            System.Xml.XmlTextReader reader1 = new System.Xml.XmlTextReader(resp, System.Xml.XmlNodeType.Element, context);
            string netid = null;
            string debugMsg = "";

            while (reader1.Read())
            {
                debugMsg += reader1.LocalName + reader1.ReadString();
                if (reader1.IsStartElement())
                {
                    string tag = reader1.LocalName;
                    
                    if (tag == "employeeNumber")
                        netid = reader1.ReadString();
                }
            }
            if(netid==null)
            {
                LogUtil.Log(Request, "登录", "Unknown", "Unknown", 0, "Fail", debugMsg, DateTime.Now.ToString());
                return new Response(2002, "请重试").Convert();
            }
            User user = UserDao.GetUserById(netid.ToUpper());
            if (user!=null)
            {
                bool login = redis.IsSet(user.id);
                if (login)
                {
                    string expiredToken = redis.Get<string>(user.id);
                    redis.Delete(expiredToken);
                    redis.Delete(user.id);
                }
                string uuid = System.Guid.NewGuid().ToString();
                redis.Set(uuid, user.id, 15);
                redis.Set(user.id, uuid, 15);
                Dictionary<string, string> retData = new Dictionary<string, string>
                {
                    { "authorization", uuid },
                    { "userId", user.id },
                    { "role", user.role.ToString() },
                    { "name", user.name },
                    { "is_accept", user.is_accept.ToString() }
                };
                LogUtil.Log(Request, "登录", user.id, user.id, user.role, "", "SSO登录" + SSOServer, DateTime.Now.ToString());
                return new Response(1001, "登录成功", retData).Convert();
            }
            return new Response(2002, "ID不存在").Convert();
        }

        /// <summary>
        /// 获取验证码API by jyf
        /// 2019.7.14
        /// </summary>
        /// <param name="account">无</param>
        /// <returns>{
        /// "code":,
        ///  "msg":"",
        ///  "data":{
        ///    "base64":""
        ///    "token":""    
        ///  }
        /// }
        /// </returns>
        [Route("security/code"), HttpGet]
        public HttpResponseMessage Code()
        {
            Dictionary<string, string> retData = new Dictionary<string, string>();
            try
            {
                //创建验证码字符串及图片
                string code = CodeManageUtil.GetCode();
                string pic = CodeManageUtil.GetCodePic(code);
                //添加验证键值对
                string uuid = System.Guid.NewGuid().ToString();
                redis.Set(uuid, code, 5);

                retData.Add("base64", pic);
                retData.Add("token", uuid);
                return new Response(1001, "验证码获取成功", retData).Convert();
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }


        ///目前假定发送邮件API为SendEmail
        /// <summary>
        /// 重置密码API--生成请求 by jyf
        /// 2019.7.20
        /// </summary>
        /// <returns>
        /// 暂时未定
        /// </returns>
        [Route("security/resetPasswordRequest"), HttpPost]
        public HttpResponseMessage ResetPasswordRequest([FromBody]JObject account)
        {
            Dictionary<string, string> retData = new Dictionary<string, string>();
            try
            {
                var jsonParams = HttpUtil.Deserialize(account);
                string id = jsonParams.id;
                string code = jsonParams.code;
                string auth = jsonParams.token;

                if(!redis.IsSet(auth))
                {
                    return new Response(1001, "验证码不存在").Convert();
                }
                string corrCode = redis.Get<string>(auth);
                if (!(corrCode.ToUpper().Equals(code.ToUpper())))
                {
                    redis.Delete(auth);
                    return new Response(1001, "验证码错误").Convert();
                }
                redis.Delete(auth);

                string uuid = System.Guid.NewGuid().ToString();
                redis.Set(uuid, id, 60);
                retData.Add("id", id);
                retData.Add("token", uuid);

                //TODO：增加发送邮件函数
                EmailUtil.SendEmail("重置密码","要重置密码，请点击以下链接\n若非本人操作，请忽视该邮件",id,1, ConfigurationManager.AppSettings["webBase"]+"/security/findPwd"+"?id="+id+"&token="+uuid);

                return new Response(1001, "请求成功", retData).Convert();
            }
            catch(Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }

        ///目前假定发送邮件API为SendEmail
        /// <summary>
        /// 重置密码API--验证重置请求（链接） by jyf
        /// 2019.7.20
        /// </summary>
        /// <returns>
        /// 暂时未定
        /// </returns>
        /// 目前假定发送链接邮件
        /// 链接目前包含token一个字段，对应修重置密码ID
        [Route("security/resetPasswordVerify"), HttpGet]
        public HttpResponseMessage ResetPasswordVerify()
        {
            Dictionary<string, string> retData = new Dictionary<string, string>();
            try
            {
                var jsonParams = Request.GetQueryNameValuePairs().ToDictionary(k => k.Key, v => v.Value);
                string auth = jsonParams["token"];

                bool exist = redis.IsSet(auth);
                if (exist)
                {
                    retData.Add("id", redis.Get<string>(auth));
                    retData.Add("token", auth);
                    return new Response(1001, "验证成功", retData).Convert();
                }
                else
                {
                    return new Response(1001,"请求不存在").Convert();
                }
                
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }


        ///目前假定发送邮件API为SendEmail
        /// <summary>
        /// 重置密码API--完成修改 by jyf
        /// 2019.7.20
        /// </summary>
        /// <param name="account"></param>
        /// <returns>
        /// 暂时未定
        /// </returns>
        /// 
        [Route("security/resetPasswordRequestChange"), HttpPost]
        public HttpResponseMessage ResetPasswordChange([FromBody]JObject account)
        {
            Dictionary<string, string> retData = new Dictionary<string, string>();
            try
            {
                var jsonParams = HttpUtil.Deserialize(account);
                string id = jsonParams.id;
                string auth = jsonParams.token;
                string password = jsonParams.password;

                if (!redis.IsSet(auth))
                {
                    return new Response(3001, "请求不存在").Convert();
                }

                if(!(id == redis.Get<string>(auth)))
                {
                    return new Response(2001, "无权修改此账户的密码").Convert();
                }

                User user = UserDao.GetUserById(id);
                if (user == null)
                {
                    return new Response(1001, "用户不存在").Convert();
                }

                redis.Delete(auth);
                int res = UserDao.ChangePwd(id, password);
                if (res == 1)
                {
                    LogUtil.Log(Request, "修改密码", id, id, user.role);
                    return new Response(1001, "成功修改密码").Convert();
                }
                else
                {
                    return new Response(1001, "不能与原密码相同").Convert();
                }
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }

        ///目前假定发送邮件API为SendEmail
        /// <summary>
        /// 激活账户-发送邮件 by zzw
        /// 2019.7.22
        /// </summary>
        /// <returns>
        /// 暂时未定
        /// </returns>
        /// 
        [Route("security/activateAccount"), HttpGet]
        public HttpResponseMessage ActivateAccount()
        {
            Dictionary<string, string> retData = new Dictionary<string, string>();
            try
            {
                var jsonParams = Request.GetQueryNameValuePairs().ToDictionary(k => k.Key, v => v.Value);
                string email = jsonParams["email"];

                var tmp = Request.Headers;
                
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string targetId = redis.Get<string>(signature);
                bool login = redis.IsSet(signature);
                if (!login)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                
                User user = UserDao.GetUserById(targetId);
                if (user == null)
                {
                    return new Response(1001, "用户不存在").Convert();
                }
                
                UserDao.ChangeInfo(user.id,user.name,email);
                string uuid = System.Guid.NewGuid().ToString();
                redis.Set(uuid, targetId, 60);
                retData.Add("id", targetId);
                retData.Add("token", uuid);
                string href = ConfigurationManager.AppSettings["webBase"] + "/security/activateAccount" + "?id=" + user.id + "&token=" + uuid;
                string res = EmailUtil.SendEmail("账户激活", "请点击以下网址来激活用户", user.id, 1, href);
                //todo: 发送激活邮件
                return new Response(1001, "发送激活邮件", retData).Convert();
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }
        ///目前假定发送邮件API为SendEmail
        /// <summary>
        /// 激活用户--验证激活邮件并激活 by zzw
        /// 2019.7.22
        /// </summary>
        /// <param name="accountInfo"></param>
        /// <returns>
        /// 暂时未定
        /// </returns>
        /// 
        [Route("security/activateAccountVerify"), HttpPost]
        public HttpResponseMessage ActivateAccountVerify([FromBody]JObject accountInfo)
        {
            try
            {
                var jsonParams = HttpUtil.Deserialize(accountInfo);
                string id = jsonParams.id;
                string auth = jsonParams.token;
                string password = jsonParams.password;
                if (!redis.IsSet(auth))
                {
                    return new Response(3001, "请求不存在").Convert();
                }
                if (!(id == redis.Get<string>(auth)))
                {
                    return new Response(2002, "无权修改此账户的密码").Convert();
                }

                User user = UserDao.GetUserById(id);
                if (user == null)
                {
                    return new Response(1001, "用户不存在").Convert();
                }

                if(user.passwd == password)
                {
                    return new Response(1001, "不能与默认密码相同").Convert();
                }

                int res1 = UserDao.ChangePwd(id, password);
                if (res1 == 1)
                {
                    int res2 = UserDao.ChangeIsAccept(id);
                    if (res2 == 1)
                    {
                        LogUtil.Log(Request, "激活账户", id, id, user.role);
                        return new Response(1001, "成功激活账户").Convert();
                    }
                    else
                    {
                        return new Response(1001, "账户已激活").Convert();
                    }
                }
                else
                {
                    return new Response(1001, "不能与默认密码相同").Convert();
                }
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }
        /// <summary>
        /// 验证token是否合法，并返回id
        /// </summary>
        /// <param name="token">待验证的token</param>
        /// <param name="service">服务地址</param>
        /// <returns></returns>
        [Route("security/tokenVerify"), HttpPost]
        public HttpResponseMessage TokenVerify([FromBody]JObject param)
        {
            //TODO: 验证请求发出的地址
            try
            {
                var jsonParams = HttpUtil.Deserialize(param);
                string tokenValue = jsonParams.token;
                string service = jsonParams.service;
                if (tokenValue.IsNullOrWhiteSpace()|| !redis.IsSet(tokenValue))
                {
                    return new Response(2001, service + "Token错误", null).Convert();
                }
                else
                {
                    string id = redis.Get<string>(tokenValue);
                    User user = UserDao.GetUserById(id);
                    if (user == null)
                        return new Response(2001, service + "Token错误", null).Convert();
                    else
                    {
                        Dictionary<string, string> ret = new Dictionary<string, string>();
                        ret.Add("id", id);
                        ret.Add("role", user.role.ToString());
                        ret.Add("service", service);
                        return new Response(1003, service + "验证成功", ret).Convert();
                    }
                }
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }
    }
}
