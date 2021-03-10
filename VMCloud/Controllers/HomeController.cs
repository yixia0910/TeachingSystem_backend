using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using VMCloud.Utils;
using VMCloud.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VMCloud.Models.DAO;
using System.Web;
using System.Collections.Specialized;
using System.Reflection;
using System.Configuration;

namespace VMCloud.Controllers
{
    //[RoutePrefix("api")]
    public class HomeController : ApiController
    {
        
        [Route(""), HttpGet]
        public HttpResponseMessage HelloWorld()
        {
            return new HttpResponseMessage{Content = new StringContent("Start successfully, see \"/status\" for more information.")};
        }
        [Route("status"), HttpGet]
        public HttpResponseMessage HelloMessage()
        {
            string managerType = ConfigurationManager.AppSettings["VMManagerType"];
            string versionMsg = "";
            string redisMsg = "Redis: ERROR\n";
            string mySQLMsg = "MySQL: ERROR\n";
            string vmMsg = "Web Service: ERROR\n";
            string errorMsg = "Error Message:\n";
            string netdiskMsg = "Netdisk: OK\n";
            string publishTime = System.IO.File.GetLastWriteTime(this.GetType().Assembly.Location).ToString();
            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            HttpResponseMessage res;
            try
            {
#if DEBUG
                versionMsg = "Development Environment ";
#else
                versionMsg = "Production Environment";
#endif
                RedisHelper redis = RedisHelper.GetRedisHelper();
                redis.Set("cloud.beihangsoft.cn", "backend", 99999);
                if (redis.IsSet("cloud.beihangsoft.cn"))
                    redisMsg = "Redis: OK\n";
            }
            catch (Exception e)
            {
                errorMsg += e.Message + "\n";
                errorMsg += e.StackTrace + "\n";
            }
            try
            {
                using (var context = new DataModels())
                {
                    int userCount = context.Users.Count();
                    if (userCount > 0)
                        mySQLMsg = $"MySQL: OK. Users:  {userCount}\n";
                }
            }
            catch (Exception e)
            {
                errorMsg += e.Message + "\n";
            }
            try
            {
                if (managerType.Equals("VMware"))
                {
                    WebService ws = new WebService();
                    int hostCount = ws.GetHosts().Count();
                    if (hostCount > 0)
                        vmMsg = $"WebService: OK. Available hosts: {hostCount}\n";
                    else
                        vmMsg = "WebService: ERROR\n";
                }
                else
                {
                    RESTful rest = new RESTful();
                    vmMsg = "Using Sangfor REST api.\n";
                }
            }
            catch (Exception e)
            {
                errorMsg = e.Message + "\n";
            }
            try
            {
                var ret = HttpUtil.Method(HttpMethod.Get, ConfigurationManager.AppSettings["MyServer"] + "/netdisk/status");
                if (!ret.StatusCode.Equals(HttpStatusCode.OK))
                    throw new Exception("Netdisk service error, " + ret.StatusCode);
            }
            catch (Exception e)
            {
                errorMsg = e.Message + "\n";
            }
            finally
            {
                if (errorMsg.Equals("Error Message:\n"))
                    errorMsg = "ERVERY THING IS OK!\n";
                res = new HttpResponseMessage
                {
                    Content = new StringContent
                    (
                    $"cloud.beihangsoft.cn {versionMsg}\n" +
                    $"Last updated at {publishTime}\n"+
                    $"Version {version}\n"+
                    $"{redisMsg}" +
                    $"{mySQLMsg}" +
                    $"{vmMsg}" +
                    $"{netdiskMsg}"+
                    $"{errorMsg}" +
                    $"{DateTime.Now}\n" +
                    $"Copyright 2016-{DateTime.Now.Year} College of Software, BUAA"
                    )
                };
                res.Headers.Add("Collaborators", "xzh,jyf,xzy,zzw,cyk");
            }
            return res;
        }
    }
}
