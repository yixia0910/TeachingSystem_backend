using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using VMCloud.Models;

namespace VMCloud.Utils
{
    public class MessageManager//用来处理接受回来的信息的类
    {
        /// <summary>
        /// 通过镜像名字找到镜像id
        /// </summary>
        /// <param name="imageName">镜像名字</param>
        /// <returns>镜像id(string)|No Authority|No Content|Not Found</returns>
        public static string FindImage(string imageName)
        {
            var param = HttpUtil.Deserialize(JObject.Parse(APIManager.GetImages()));
            if (param.Equals("No Content")) return "No Content";
            else if (param.Equals("No Authority")) return "No Authority";
            else
            {
                for (int i = 0; param["images"][i] != null; i++)
                {
                    if (param["images"][i]["name"].ToString().Equals(imageName))
                        return param["images"][i]["id"];
                }
                return "Not Found";
            }

        }
        /// <summary>
        /// 通过CPU，内存，磁盘查找规格id
        /// </summary>
        /// <param name="cpu">CPU数目</param>
        /// <param name="memory">内存大小</param>
        /// <param name="disk">磁盘数目</param>
        /// <returns>规格id(string)|No Authority|No Content|Not Found</returns>
        public static string FindFlavor(int cpu, long memory, long disk)
        {
            var param = HttpUtil.Deserialize(JObject.Parse(APIManager.GetFlavors()));
            if (param.Equals("No Content")) return "No Content";
            else if (param.Equals("No Authority")) return "No Authority";
            else
            {
                for (int i = 0;; i++)
                {
                    try
                    {
                        if (param["flavors"][i]["vcpus"].ToString().Equals(cpu.ToString()) 
                            && param["flavors"][i]["ram"].ToString().Equals(memory.ToString())
                            && param["flavors"][i]["disk"].ToString().Equals(disk.ToString()))
                            return param["flavors"][i]["id"];
                    }
                    catch
                    {
                        break;
                    }
                }
                return "Not Found";
            }

        }
        /// <summary>
        /// 通过虚拟机id查找挂载卷id
        /// </summary>
        /// <param name="serverId">虚拟机id</param>
        /// <returns>卷id(string)|No Authority|No Content|Not Found</returns>
        public static string FindVolume(string serverId)
        {
            var param = HttpUtil.Deserialize(JObject.Parse(APIManager.GetServer(serverId)));
            if (param.Equals("No Content")) return "No Content";
            else if (param.Equals("No Authority")) return "No Authority";
            else
            {
                if (param["server"]["os-extended-volumes:volumes_attached"].ToString().Equals("[]") == false)
                    return param["server"]["os-extended-volumes:volumes_attached"][0]["id"].ToString();
                return "Not Found";
            }
        }





        /// <summary>
        /// 根据所给参数，封装参数以创建虚拟机(镜像)
        /// </summary>
        /// <param name="flavorRef">规格id</param>
        /// <param name="uid">网络id</param>
        /// <param name="name">虚拟机名字</param>
        /// <param name="availability_zone">可用区</param>
        /// <param name="imgRef">镜像id</param>
        /// <returns>参数(Dictionary)</returns>
        public static Dictionary<string, object> PackageCreateJsonByImage(string flavorRef, string uid, string name, string availability_zone, string imgRef)
        {
            Dictionary<string, object> data = new Dictionary<string, object>();
            Dictionary<string, object> server = new Dictionary<string, object>();
            Dictionary<string, object> uuid = new Dictionary<string, object>();
            List<Dictionary<string, object>> networks = new List<Dictionary<string, object>>();
            uuid.Add("uuid", uid);
            networks.Add(uuid);
            data.Add("flavorRef", flavorRef);
            data.Add("name", name);
            data.Add("networks", networks);
            data.Add("availability_zone", availability_zone);
            data.Add("imageRef", imgRef);
            server.Add("server", data);
            return server;
        }

       


        /// <summary>
        /// 根据所给参数，封装参数以创建虚拟机(卷)
        /// </summary>
        /// <param name="flavorRef">规格id</param>
        /// <param name="uid">网络id</param>
        /// <param name="name">虚拟机名字</param>
        /// <param name="availability_zone">可用区</param>
        /// <param name="volumeId">卷id</param>
        /// <returns>参数(Dictionary)</returns>
        public static System.Collections.Generic.Dictionary<string, object> PackageCreateJsonByVolume(string flavorRef, string uid, string name, string availability_zone, string volumeId)
        {
            Dictionary<string, object> data = new Dictionary<string, object>();
            Dictionary<string, object> server = new Dictionary<string, object>();
            Dictionary<string, object> uuid = new Dictionary<string, object>();
            List<Dictionary<string, object>> networks = new List<Dictionary<string, object>>();
            uuid.Add("uuid", uid);
            networks.Add(uuid);
            data.Add("flavorRef", flavorRef);
            data.Add("name", name);
            data.Add("networks", networks);
            data.Add("availability_zone", availability_zone);
            List<Dictionary<string, object>> bdm = new List<Dictionary<string, object>>();
            Dictionary<string, object> bdmdata = new Dictionary<string, object>();
            bdmdata.Add("boot_index", 0);
            bdmdata.Add("uuid", volumeId);
            bdmdata.Add("source_type", "volume");
            bdmdata.Add("destination_type", "volume");
            bdm.Add(bdmdata);
            data.Add("block_device_mapping_v2",bdm);
            server.Add("server", data);
            return server;
        }

        public static string CreateFlavor(VMConfig config)
        {
            Dictionary<string, object> data = new Dictionary<string, object>();
            Dictionary<string, object> flavor = new Dictionary<string, object>();
            flavor.Add("name", config.Name + "_" + config.Memory.ToString() + "_" + config.CPU.ToString() + "_" + config.Disk.ToString());
            flavor.Add("ram", config.Memory);
            flavor.Add("vcpus", config.CPU);
            flavor.Add("disk", config.Disk);
            data.Add("flavor",flavor);
            string result = APIManager.CreateFlavor(data);
            var param = HttpUtil.Deserialize(JObject.Parse(result));
            if (param == null)
                return null;
            return param["flavor"]["id"];
        }


        



        /// <summary>
        /// 获取虚拟机信息并封装
        /// </summary>
        /// <param name="id">虚拟机id</param>
        /// <returns>虚拟机信息(VMConfig)</returns>
        public static VMConfig GetServerDetail(string id)
        {
            try
            {
                VMConfig vMConfig = new VMConfig();
                var param = HttpUtil.Deserialize(JObject.Parse(APIManager.GetServer(id)));
                if (param == null||param["error"]!=null)
                    return null;
                vMConfig.Name = param["server"]["name"];
                vMConfig.Status = new VMStatus();
                vMConfig.Status.PowerState = param["server"]["status"].ToString();
                if (param["server"]["addresses"].ToString().Equals("{}") == false)
                {
                    vMConfig.Status.IPAddress = param["server"]["addresses"]["子网1"][0]["addr"].ToString();
                }
                string flavorRef = param["server"]["flavor"]["id"];
                //if (param["server"]["image"] != null)
                //{
                //    string imageRef = param["server"]["image"]["id"];
                //    param = HttpUtil.Deserialize(JObject.Parse(APIManager.GetImage(imageRef)));
                //    vMConfig.GuestFullName = param["image"]["name"];
                //}

                param = HttpUtil.Deserialize(JObject.Parse(APIManager.GetFlavor(flavorRef)));
                vMConfig.Memory = param["flavor"]["ram"];
                vMConfig.CPU = param["flavor"]["vcpus"];
                vMConfig.Disk = param["flavor"]["disk"];
                vMConfig.Disk = vMConfig.Disk * 1024;
                return vMConfig;
            }
           catch
            {
                return null;
            }
        }
        /// <summary>
        /// 解析创建后的虚拟机信息
        /// </summary>
        /// <param name="info">虚拟机创建后的信息</param>
        /// <returns>id(int)|error</returns>
        public static string AnalyzeCreateInfo(string info)
        {
            var param = HttpUtil.Deserialize(JObject.Parse(info));
            if (param["server"] != null)
            {
                string id = param["server"]["id"].ToString();
                
                return id;
            }
            else return "error";

        }







    }
}