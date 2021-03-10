/*
*	Author:       Xing Zhihuan
*   Email:        1@roycent.cn	
*	Created:      2019/9/5 15:32:17
*   Description:  
 */
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Web;
using VMCloud.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VMCloud.Models.DAO;

namespace VMCloud.Utils
{
    public class RESTful : IVirtualMachineManager
    {


        /// <summary>
        /// 开启关闭虚拟机
        /// </summary>
        /// <param name="vmName">虚拟机名称</param>
        /// <param name="option">1为开机 2为关机</param>
        /// <returns>success|error:错误信息</returns>
        public string PowerOption(string id, int option)
        {
            //string id = MysqlManager.FindServer(vmName);
            string info = "";

            //处理传参是name的情况
            SangforInfo sang = SangforDao.GetSangforInfoByName(id);
            if (sang != null)
                id = sang.id;


            if (option == 2)
            {
                info = APIManager.OffServer(id);

            }
            else if (option == 1)
            {
                info = APIManager.OnServer(id);
            }
            if (info == "") return "success";
            else if(info.Equals("No Content"))
            {
                return "error:No Content";
            }
            else if(info.Equals("|No Authority"))
            {
                return "error:No Authority";
            }
            else
            {
                return "error:"+info;
            }
            
        }


        /// <summary>
        /// 删除虚拟机
        /// </summary>
        /// <param name="vmName">虚拟机名称</param>
        /// <returns>success|error:错误信息</returns>
        public string Delete(string id)
        {
                    
            string info = "";
            
            APIManager.OffServer(id);
            info = APIManager.DeleteServer(id);
            
            if (info=="")
            {
                SangforDao.DeleteById(id);
                return "success";
            }
            else if (info.Equals("No Content"))
            {
                return "error:No Content";
            }
            else if (info.Equals("|No Authority"))
            {
                return "error:No Authority";
            }
            else
            {
                //var param = HttpUtil.Deserialize(JObject.Parse(info));
                //return "error:"+param["error"]["message"];
                return "error:" + info;
            }
        }
        /// <summary>
        /// 批量删除虚拟机
        /// </summary>
        /// <param name="vmNames">虚拟机名称列表</param>
        /// <returns>删除的台数(int)</returns>
        public int Delete(List<string> ids)
        {
            int i = 0;
            foreach (string id in ids)
            {
                
                string info = "";
               
                APIManager.OffServer(id);
                info = APIManager.DeleteServer(id);
                if (info=="")
                {
                    SangforDao.DeleteById(id);
                    i++;
                }
            }
            return i;
        }
        ///// <summary>
        ///// 批量创建虚拟机
        ///// </summary>
        ///// <param name="config">虚拟机配置</param>
        ///// <param name="vmNameList">虚拟机名称</param>
        ///// <param name="IDList">未知参数</param>
        ///// <param name="hostName">未知参数</param>
        ///// <returns></returns>
        public string Create(VMConfig config, List<string> vmNameList, List<string> IDList, string teacherId = "", string hostName = "")
        {
        //    string vmName = vmNameList[0];
        //    string flavorRef = MessageManager.FindFlavor(config.CPU, config.Memory, config.Disk);
        //    string uuid = MysqlManager.FindNetworks();
        //    string zone = MysqlManager.FindZone();
            string info = "";
        //    if (MysqlManager.FindServer(vmName).Equals("Not Found") == false)
        //        return "Used Name";
        //    if (MysqlManager.IsImage(config.TemplateName))
        //    {
        //        string imgRef = MysqlManager.FindServer(config.TemplateName);
        //        Dictionary<string, object> data = MessageManager.PackageCreateJsonByImage(flavorRef, uuid, vmName, zone, imgRef,2);
                
        //        info = APIManager.CreateServer(data);
        //    }
        //    else
        //    {
        //        string serverId = MysqlManager.FindServer(config.TemplateName);
        //        string volumeId = MessageManager.FindVolume(serverId);
        //        Dictionary<string, object> data = MessageManager.PackageCreateJsonByVolume(flavorRef, uuid, vmName, zone, volumeId,2);
        //        info = APIManager.CreateServer(data);
        //    }
        //    /*string id = MessageManager.AnalyzeCreateInfo(info, vmName);
        //    if (id.Equals("error") == false)
        //    {
        //        MysqlManager.AddServer(id, vmName);
        //        return "Create Success";
        //    }*/
            return info;
            
        }

        /// <summary>
        /// 采用新逻辑批量创建虚拟机，用于优化多台创建的情况
        /// </summary>
        public void CreateNewLogic(VMConfig config, List<SangforInfo> infos)
        {
            List<string> nameList = new List<string>();
            List<string> newNameList = new List<string>();
            //nameList.Add(config.Name);
            newNameList.Add(config.TemplateName);

            int num = 0;

            while (true)
            {
                List<string> idList = new List<string>();
                nameList = new List<string>();
                foreach (string name in newNameList)
                {
                    nameList.Add(name);
                   
                }
                if (num == infos.Count())
                    break;
                
                foreach (string name in nameList)
                {
                    config.TemplateName = name;
                    string id = Create(config, infos[num].Name, null, null);
                    
                    if (!id.StartsWith("error"))
                    {
                        infos[num].id = id;
                        idList.Add(id);
                        SangforDao.Add(infos[num]);
                        newNameList.Add(infos[num].Name);
                    }
                    num++;
                    if (num == infos.Count())
                        break;
                }
                Thread.Sleep(180000);
                foreach(string id in idList)
                {
                    PowerOption(id, 2);
                }
                Thread.Sleep(120000);
            }
        }


        /// <summary>
        /// 创建虚拟机
        /// </summary>
        /// <param name="config">虚拟机配置</param>
        /// <param name="vmName">虚拟机名称</param>
        /// <param name="ID">模板id</param>
        /// <param name="hostName">未知参数</param>
        /// <returns>success|error:错误信息</returns>
        public string Create(VMConfig config, string vmName, string ID, string hostName = "")
        {
            SangforInfo info1 = SangforDao.GetSangforInfoByName(config.TemplateName);
            config.Disk = Convert.ToInt64(info1.image_disk);
            string flavorRef = MessageManager.FindFlavor(config.CPU, config.Memory, config.Disk);
            if (flavorRef.Equals("Not Found"))
            {
                flavorRef = MessageManager.CreateFlavor(config);
            }
            
            string uuid = ConfigurationManager.AppSettings["AcloudNetworks"]; 
            string zone = ConfigurationManager.AppSettings["AcloudAvailability_zone"]; 
            string info = "";
            
            //if (info1 == null)
            //{
            //   return "error";
            //}
            //string serverId = info1.id; 
            //PowerOption(serverId, 2);
            //string volumeId = MessageManager.FindVolume(serverId);
            //Dictionary<string, object> data =
            //MessageManager.PackageCreateJsonByVolume(flavorRef, uuid, vmName, zone, volumeId);
            Dictionary<string, object> data =
                MessageManager.PackageCreateJsonByImage(flavorRef, uuid, vmName, zone, info1.id);
            info = APIManager.CreateServer(data);
            string id= MessageManager.AnalyzeCreateInfo(info);
            if (id.Equals("error") == false)
            {
                return id;
            }
            else if (info.Equals("No Content"))
            {
                return "error:No Content";
            }
            else if (info.Equals("|No Authority"))
            {
                return "error:No Authority";
            }
            else
                return "error"+info;
        }

        public List<Host> GetHosts()
        {
            return null;
        }
        /// <summary>
        /// 获取虚拟机信息
        /// </summary>
        /// <param name="vmName">虚拟机名称</param>
        /// <returns>虚拟机信息(VMConfig)|null</returns>
        public VMConfig GetVMInfo(string id)
        {
            //string id = MysqlManager.FindServer(vmName);
            if (id.Equals("Not Found")) return null;
            VMConfig vMConfig = MessageManager.GetServerDetail(id);
            return vMConfig;
        }

        public List<VMConfig> GetTemplates()
        {


            return null;
        }

        /// <summary>
        /// 批量获取虚拟机信息
        /// </summary>
        /// <param name="vmNameList">虚拟机名称列表</param>
        /// <returns>虚拟机信息列表(List<VMConfig>)</returns>
        public List<VMConfig> GetVMList(List<string> idList)
        {
            List<VMConfig> vMConfigs = new List<VMConfig>();
            foreach (string id in idList)
            {
                if (id == null) continue;
                VMConfig vMConfig = MessageManager.GetServerDetail(id);
                if (vMConfig == null)
                    continue;
                vMConfigs.Add(vMConfig);

            }


            return vMConfigs;
        }

        public string Rename(string vmName, string newName)
        {
            return null;
        }

        public string ConvertToTemplate(string vmName)
        {

            return null;
        }

        public string OpenConsole(string id)
        {

            string info=APIManager.OpenConsole(id);
            if (info.Equals("No Content"))
            {
                return "error:No Content";
            }
            else if (info.Equals("|No Authority"))
            {
                return "error:No Authority";
            }
            else
            {
                var param = HttpUtil.Deserialize(JObject.Parse(info));
                return param["console"]["url"].ToString();
            }
            

        }


    }
}