/*
*	Author:       Xing Zhihuan
*   Email:        1@roycent.cn	
*	Created:      2019/8/5 0:38:05
*   Description:  aCloudAPI相关
 */
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace VMCloud.Utils
{
    public class APIManager
    {
        //从配置文件Web.Config中读取地址、用户名以及密码信息
        private static string address = ConfigurationManager.AppSettings["AcloudAddress"];
        private static string userName = ConfigurationManager.AppSettings["AcloudUserName"];
        private static string password = ConfigurationManager.AppSettings["AcloudPassword"];

        //认证可能需要的信息
        private static string[] audit_ids;
        private static string token;
        private static DateTime expires = new DateTime();

        private static dynamic authInfo;



        /// <summary>
        /// API权限认证
        /// Create by xzh
        /// </summary>
        /// <returns>true:认证成功|false:认证失败</returns>
        private static bool auth()
        {
            //如果token不存在或未过期，重新POST登录数据
            if (isExpired())
            {
                Dictionary<string, object> data = new Dictionary<string, object>();
                /*
                 {
	                "auth": {
		                "tenantName": "api",
		                "passwordCredentials": {
			                "username": "api",
			                "password": "@BUAAsoft21"
		                }
	                }
                }

                */

                Dictionary<string, object> auth = new Dictionary<string, object>();
                Dictionary<string, string> passwordCredentials = new Dictionary<string, string>();
                passwordCredentials.Add("username", userName);
                passwordCredentials.Add("password", password);
                auth.Add("tenantName", userName);
                auth.Add("passwordCredentials", passwordCredentials);
                data.Add("auth", auth);

                HttpResponseMessage response = HttpUtil.Method(HttpMethod.Post, "openstack/identity/v2.0/tokens", null, data);
                //读取返回数据
                string retStr = response.Content.ReadAsStringAsync().Result;
                //将其转换为对象格式
                var ret = JsonConvert.DeserializeObject<dynamic>(retStr);
                if (ret.access != null)
                    authInfo = ret.access;
                //读取各种变量数据
                expires = DateTime.Parse(authInfo.token.expires.ToString());
                expires = expires.AddHours(8);//返回的是UTC时间，转换为北京时间
                token = authInfo.token.id.ToString();
                //id是个数组所以要特殊解析一下
                audit_ids = JsonConvert.DeserializeObject<string[]>(authInfo.token.audit_ids.ToString());

                return !isExpired();
            }
            return !isExpired();
        }

        public static string GetToken()
        {
            if (auth())
            {


                return token;
            }
            return null;


        }


        /// <summary>
        /// 获取镜像列表信息
        /// </summary>
        /// <returns>镜像列表信息(json)|No Authority|No Content</returns>
        public static string GetImages()
        {
            if (auth())
            {
                HttpResponseMessage httpResponseMessage = HttpUtil.Method(HttpMethod.Get, "openstack/image/v2/images", token);
                HttpContent httpContent = httpResponseMessage.Content;
                if (httpContent != null)
                {
                    Task<string> task = httpContent.ReadAsStringAsync();
                    return task.Result;
                }
                else return "No Content";

            }
            return "No Authority";
        }


        public static string GetHosts()
        {
            if (auth())
            {
                HttpResponseMessage httpResponseMessage = HttpUtil.Method(HttpMethod.Get,
                    "openstack/metric/v2/resource/generic/49c9a073-057d-4e84-9e5e-b82f1476b1fb/metric/cpu_util/measures",
                    token);
                HttpContent httpContent = httpResponseMessage.Content;
                if (httpContent != null)
                {
                    Task<string> task = httpContent.ReadAsStringAsync();
                    return task.Result;
                }
                else return "No Content";

            }
            return "No Authority";
        }
        /// <summary>
        /// 根据id查找虚拟机镜像
        /// </summary>
        /// <returns>镜像详情(json)|No Authority|No Content</returns>
        public static string GetImage(string id)
        {
            if (auth())
            {
                HttpResponseMessage httpResponseMessage = HttpUtil.Method(HttpMethod.Get, "openstack/image/v2/images/" + id, token);
                HttpContent httpContent = httpResponseMessage.Content;
                if (httpContent != null)
                {
                    Task<string> task = httpContent.ReadAsStringAsync();
                    return task.Result;
                }
                else return "No Content";

            }
            return "No Authority";
        }



        /// <summary>
        /// 获取云主机列表信息
        /// </summary>
        /// <returns>云主机列表信息(json)|No Authority|No Content</returns>
        public static string GetServers()
        {
            if (auth())
            {
                HttpResponseMessage httpResponseMessage = HttpUtil.Method(HttpMethod.Get, "openstack/compute/v2/servers/detail", token);
                HttpContent httpContent = httpResponseMessage.Content;
                if (httpContent != null)
                {
                    Task<string> task = httpContent.ReadAsStringAsync();
                    return task.Result;
                }
                else return "No Content";

            }
            return "No Authority";
        }


        /// <summary>
        /// 获取云主机列表
        /// </summary>
        /// <returns>云主机列表(json)|No Authority|No Content</returns>
        public static string GetServerList()
        {
            if (auth())
            {
                HttpResponseMessage httpResponseMessage = HttpUtil.Method(HttpMethod.Get, "openstack/compute/v2/servers", token);
                HttpContent httpContent = httpResponseMessage.Content;
                if (httpContent != null)
                {
                    Task<string> task = httpContent.ReadAsStringAsync();
                    return task.Result;
                }
                else return "No Content";
            }
            return "No Authority";
        }

        /// <summary>
        /// 获取规格详情列表信息
        /// </summary>
        /// <returns>规格详情列表信息(json)|No Authority|No Content</returns>
        public static string GetFlavors()
        {
            if (auth())
            {
                HttpResponseMessage httpResponseMessage = HttpUtil.Method(HttpMethod.Get, "openstack/compute/v2/flavors/detail", token);
                HttpContent httpContent = httpResponseMessage.Content;
                if (httpContent != null)
                {
                    Task<string> task = httpContent.ReadAsStringAsync();
                    return task.Result;
                }
                else return "No Content";
            }
            return "No Authority";
        }
        /// <summary>
        /// 根据id查找虚拟机规格
        /// </summary>
        /// <returns>规格详情(json)|No Authority|No Content</returns>
        public static string GetFlavor(string id)
        {
            if (auth())
            {
                HttpResponseMessage httpResponseMessage = HttpUtil.Method(HttpMethod.Get, "openstack/compute/v2/flavors/" + id, token);
                HttpContent httpContent = httpResponseMessage.Content;
                if (httpContent != null)
                {
                    Task<string> task = httpContent.ReadAsStringAsync();
                    return task.Result;
                }
                else return "No Content";
            }
            return "No Authority";
        }

        public static string CreateFlavor(Dictionary<string, object> data)
        {
            if (auth())
            {
                HttpResponseMessage httpResponseMessage = HttpUtil.Method(HttpMethod.Post, "openstack/compute/v2/flavors", token,data);
                HttpContent httpContent = httpResponseMessage.Content;
                if (httpContent != null)
                {
                    Task<string> task = httpContent.ReadAsStringAsync();
                    return task.Result;
                }
                else return "No Content";
            }
            return "No Authority";

        }

        public static string CreateServer()
        {
            if (auth())
            {
                Dictionary<string, object> data = new Dictionary<string, object>();
                Dictionary<string, object> server = new Dictionary<string, object>();
                //Dictionary<string, List<Dictionary<string, object>>> bdm = new Dictionary<string, List<Dictionary<string, object>>>();
                Dictionary<string, List<Dictionary<string, object>>> networks = new Dictionary<string, List<Dictionary<string, object>>>();
                List<Dictionary<string, object>> uuid = new List<Dictionary<string, object>>();
                List<Dictionary<string, object>> bdm = new List<Dictionary<string, object>>();
                data.Add("uuid", "820d5937-2878-4462-923a-b55356531dfb");
                uuid.Add(data);


                //networks.Add("uuid", uuid);
                data = new Dictionary<string, object>();
                data.Add("boot_index", 0);
                data.Add("uuid", "a0b2befe-12e9-4416-af17-56b5031df45b");
                data.Add("source_type", "image ");
                data.Add("destination_type", "volume");
                bdm.Add(data);
                data = new Dictionary<string, object>();


                data.Add("flavorRef", "3e5d0cc8-91de-4e54-8149-46541731c05c");
                data.Add("name", "CYK123");
                data.Add("networks", uuid);
                data.Add("block_device_mapping_v2", bdm);
                data.Add("availability_zone", "aCloud1资源池");
                data.Add("imageRef", "4b21226d-1332-4f93-b650-6c1ef280e5b6");
                server.Add("server", data);
                //return server["server"].ToString();
                return HttpUtil.Method(HttpMethod.Post, "openstack/compute/v2/servers", token, server).Content.ReadAsStringAsync().Result;
            }
            return "No Authority";



        }


        public static string CreateServer(JObject account)
        {
            if (auth())
            {

                return HttpUtil.Method(HttpMethod.Post, "openstack/compute/v2/servers", token, account).Content.ReadAsStringAsync().Result;
            }
            return "No Authority";



        }





        /// <summary>
        /// 创建虚拟机
        /// </summary>
        /// <param name="data">创建参数</param>
        /// <returns>创建结果(json)|No Authority|No Content</returns>
        public static string CreateServer(Dictionary<string, object> data)
        {
            if (auth())
            {

                HttpResponseMessage httpResponseMessage = HttpUtil.Method(HttpMethod.Post, "openstack/compute/v2/servers", token, data);
                HttpContent httpContent = httpResponseMessage.Content;
                if (httpContent != null)
                {
                    Task<string> task = httpContent.ReadAsStringAsync();
                    return task.Result;
                }
                else return "No Content";
            }
            return "No Authority";
        }


        /// <summary>
        /// 开启虚拟机
        /// </summary>
        /// <param name="id">虚拟机id</param>
        /// <returns>开机结果(json)|No Authority|No Content</returns>
        public static string OnServer(string id)
        {
            if (auth())
            {
                string os_start = null;
                Dictionary<string, object> data = new Dictionary<string, object>();
                data.Add("os-start", os_start);

                HttpResponseMessage httpResponseMessage = HttpUtil.Method(HttpMethod.Post, "openstack/compute/v2/servers/" + id + "/action", token, data);
                HttpContent httpContent = httpResponseMessage.Content;
                if (httpContent != null)
                {
                    Task<string> task = httpContent.ReadAsStringAsync();
                    return task.Result;
                }
                else return "No Content";
            }
            return "No Authority";
        }
        /// <summary>
        /// 关闭虚拟机
        /// </summary>
        /// <param name="id">虚拟机id</param>
        /// <returns>关机结果(json)|No Authority|No Content</returns>
        public static string OffServer(string id)//关机
        {
            if (auth())
            {
                string os_stop = null;
                Dictionary<string, object> data = new Dictionary<string, object>();
                data.Add("os-stop", os_stop);

                HttpResponseMessage httpResponseMessage = HttpUtil.Method(HttpMethod.Post, "openstack/compute/v2/servers/" + id + "/action", token, data);
                HttpContent httpContent = httpResponseMessage.Content;
                if (httpContent != null)
                {
                    Task<string> task = httpContent.ReadAsStringAsync();
                    return task.Result;
                }
                else return "No Content";
            }
            return "No Authority";
        }


        /// <summary>
        /// 获取单台云主机详情
        /// </summary>
        /// <param name="id">虚拟机id</param>
        /// <returns>虚拟机信息(json)|No Authority|No Content</returns>
        public static string GetServer(string id)//获取单台云主机详情
        {
            if (auth())
            {
                HttpResponseMessage httpResponseMessage = HttpUtil.Method(HttpMethod.Get, "openstack/compute/v2/servers/" + id, token);
                HttpContent httpContent = httpResponseMessage.Content;
                if (httpContent != null)
                {
                    Task<string> task = httpContent.ReadAsStringAsync();
                    return task.Result;
                }
                else return "No Content";

            }
            return "No Authority";
        }
        /// <summary>
        /// 删除云主机
        /// </summary>
        /// <param name="id">虚拟机id</param>
        /// <returns>删除结果(json)|No Authority|No Content</returns>
        public static string DeleteServer(string id)//删除虚拟机
        {
            if (auth())
            {
                HttpResponseMessage httpResponseMessage = HttpUtil.Method(HttpMethod.Delete, "openstack/compute/v2/servers/" + id, token);
                HttpContent httpContent = httpResponseMessage.Content;
                if (httpContent != null)
                {
                    Task<string> task = httpContent.ReadAsStringAsync();
                    return task.Result;
                }
                else return "No Content";

            }
            return "No Authority";
        }
        /// <summary>
        /// 打开控制台
        /// </summary>
        /// <param name="id">虚拟机id</param>
        /// <returns>控制台url(string)|No Authority|No Content</returns>
        public static string OpenConsole(string id)
        {
            if (auth())
            {
                string type = "novnc";
                Dictionary<string, object> os = new Dictionary<string, object>();
                Dictionary<string, object> data = new Dictionary<string, object>();
                os.Add("type", type);
                data.Add("os-getVNCConsole", os);
                HttpResponseMessage httpResponseMessage = HttpUtil.Method(HttpMethod.Post, "openstack/compute/v2/servers/" + id + "/action", token, data);
                HttpContent httpContent = httpResponseMessage.Content;
                if (httpContent != null)
                {
                    Task<string> task = httpContent.ReadAsStringAsync();
                    return task.Result;
                }
                else return "No Content";
            }
            return "No Authority";

        }



        private static bool isExpired()
        {
            return ((expires == null) || expires.CompareTo(DateTime.Now) <= 0);
        }
    }
}