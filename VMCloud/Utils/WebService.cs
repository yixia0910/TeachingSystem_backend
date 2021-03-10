/*
*	Author:       Xing Zhihuan
*   Email:        1@roycent.cn	
*	Created:      2019/7/24 16:38:12
*   Description:  
 */
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using VMCloud.Models;

namespace VMCloud.Utils
{
    public class WebService : IVirtualMachineManager
    {
        private VCManagerService vms;
        private string ipPrefix;

        public WebService()
        {
            vms = new VCManagerService();
            ipPrefix = ConfigurationManager.AppSettings["UsefulIPPrefix"];
        }
        /// <summary>
        /// 进行虚拟机电源操作
        /// </summary>
        /// <param name="vmName"></param>
        /// <param name="op">
        /// 1-poweron|2-poweroff|3-reset
        /// </param>
        /// <returns>
        /// Wrong option|success...|error...
        /// </returns>
        public string PowerOption(string vmName, int option)
        {
            string op;
            switch (option)
            {
                case 1:
                    op = "poweron";
                    break;
                case 2:
                    op = "poweroff";
                    break;
                case 3:
                    op = "reset";
                    break;
                default:
                    return "Wrong option";

            }
            VCManagerService vms = new VCManagerService();
            return vms.BasicOps(vmName, op);
        }
        /// <summary>
        /// 删除虚拟机
        /// </summary>
        /// <param name="vmName"></param>
        /// <returns>
        /// success...|error...
        /// </returns>
        public string Delete(string vmName)
        {
            return vms.BasicOps(vmName, "delete");
        }
        /// <summary>
        /// 批量删除虚拟机
        /// </summary>
        /// <param name="vmName"></param>
        /// <returns>
        /// 删除虚拟机的个数
        /// </returns>
        public int Delete(List<string> vmName)
        {
            string ret;
            int num = 0;
            foreach(var vm in vmName)
            {
                ret = vms.BasicOps(vm, "delete");
                if (ret.StartsWith("success"))
                    num++;
            }
            return num;
        }
        /// <summary>
        /// 创建虚拟机
        /// </summary>
        /// <param name="config">虚拟机配置</param>
        /// <param name="vmNameList">虚拟机名列表</param>
        /// <param name="hostName">创建位置</param>
        /// <returns></returns>
        public string Create(VMConfig config, List<string> vmNameList, List<string> IDList, string teacherId, string hostName="")
        {
            string[] vmNames = vmNameList.ToArray();
            string[] IDs = IDList.ToArray();
            return vms.CreateFromTemplate(config.TemplateName, vmNames, "0", IDs, teacherId, "true", config.CPU.ToString(), config.Memory.ToString(), config.Disk.ToString(), hostName, "");
        }

        
        public string Create(VMConfig config, string vmName, string ID, string hostName = "")
        {
            VCManagerService vms = new VCManagerService();
            string[] vmNameList = { vmName };
            string[] IDList = { ID };
            return vms.CreateFromTemplate(config.TemplateName, vmNameList, "0", IDList, ID, "false", config.CPU.ToString(), config.Memory.ToString(), config.Disk.ToString(), hostName, "");
        }


        public string Create(VMConfig config, string vmName, string ID,string teacher_id, string hostName = "")
        {
            VCManagerService vms = new VCManagerService();
            string[] vmNameList = { vmName };
            string[] IDList = { ID };
            return vms.CreateFromTemplate(config.TemplateName, vmNameList, "0", IDList, teacher_id, "true", config.CPU.ToString(), config.Memory.ToString(), config.Disk.ToString(), hostName, "");
        }
        /// <summary>
        /// 获取主机信息
        /// </summary>
        /// <returns></returns>
        public List<Host> GetHosts()
        {
            string hostInfo = vms.GetHostInfo();
            JArray list = JArray.Parse(hostInfo);
            List<Host> hosts = new List<Host>();
            foreach(var host in list)
            {
                Host h = new Host();
                h.HostName = host["Host Name"].ToString();
                h.IsConnected = host["Is Connected"].ToString();
                h.RunTimeState = host["Run Time State"].ToString();
                h.CPUTotal = long.Parse(host["CPU Total By Mhz"].ToString());
                h.CPUUsed = long.Parse(host["CPU Used By Mhz"].ToString());
                h.MemoryTotal = long.Parse(host["Memory Size By B"].ToString()) / 1024 / 1024;
                h.MemoryUsed = long.Parse(host["Memory Used By Mb"].ToString());
                string vmList = host["Virtual Machine List"].ToString();
                if (vmList != null)
                {
                    h.VirtualMachineList = JsonConvert.DeserializeObject<List<string>>(vmList);
                }
                hosts.Add(h);
            }
            return hosts;
        }
        /// <summary>
        /// 获取指定虚拟机的信息
        /// </summary>
        /// <param name="vmName"></param>
        /// <returns>
        /// {
		///"CPU": 12,
		///"Memory": 65536,
		///"Disk": 250032,
		///"IsTemplate": false,
		///"GuestFullName": "Microsoft Windows Server Threshold (64-bit)",
		///"TemplateName": null,
		///"AdvancedConfig": "",
		///"Status": {
		///	"IPAddress": "10.251.254.1",
		///	"HostName": "10.251.254.1",
		///	"PowerState": "poweredOn",
		///	"RunTimeState": "green"
		///}
        /// </returns>
        public VMConfig GetVMInfo(string vmName)
        {
            string wsRet = vms.GetVMInfo(vmName);
            List<VMConfig> ret = Deserialize(JsonConvert.DeserializeObject<List<dynamic>>(wsRet));
            return ret.First();
        }
        /// <summary>
        /// 通过名字获取所有模板虚拟机名
        /// 虚拟机命名格式:
        /// Cloud2019-Exp19211001|VM
        /// CentOS7-Template|TP
        /// </summary>
        /// <returns></returns>
        public List<string> GetTemplatesNames()
        {
            string allVMList = vms.GetVMList("", "", "", "");
            JArray list = JArray.Parse(allVMList);
            List<dynamic> vmList = list.ToObject<List<dynamic>>();
            List<dynamic> templateList = vmList.Where(v => bool.Parse(v.isTemplate.ToString()) == true).Select(v => v.vmName.ToString()).ToList();
            //List<dynamic> templateList = vmList.Where(v => v.vmName.ToString().EndsWith("|TP")).Select(v => v.vmName.ToString()).ToList();
            string name;
            return templateList.ConvertAll(v => name = v);
        }
        public List<VMConfig> GetTemplates()
        {
            string allVMList = vms.GetVMList("", "", "", "");
            JArray list = JArray.Parse(allVMList);
            List<dynamic> vmList = list.ToObject<List<dynamic>>();
            List<dynamic> templateList = vmList.Where(v => bool.Parse(v.isTemplate.ToString()) == true).Select(v => v.vmName.ToString()).ToList();
            return Deserialize(templateList);

        }
        /// <summary>
        /// 按名字列表获取虚拟机配置及状态信息
        /// </summary>
        /// <param name="vmNames">虚拟机名列表</param>
        /// <returns>虚拟机列表</returns>
        public List<VMConfig> GetVMList(List<string> vmNames)
        {
            string allVMList = vms.GetVMList("", "", "", "");
            JArray list = JArray.Parse(allVMList);
            List<dynamic> vmList = list.ToObject<List<dynamic>>();
            List<dynamic> templateList = vmList.Where(v => vmNames.Contains(v.vmName.ToString())).ToList();
            return Deserialize(templateList);
        }

        public List<VMConfig> GetVMList()
        {
            string allVMList = vms.GetVMList("", "", "", "");
            JArray list = JArray.Parse(allVMList);
            List<dynamic> vmList = list.ToObject<List<dynamic>>();
            return Deserialize(vmList);
        }
        public List<VMConfig> GetVMList(string isex)
        {
            string allVMList = vms.GetVMList("", "", "", isex);
            JArray list = JArray.Parse(allVMList);
            List<dynamic> vmList = list.ToObject<List<dynamic>>();
            return Deserialize(vmList);
        }
        public string GetVMList1()
        {
            string allVMList = vms.GetVMList("", "", "", "");
            //JArray list = JArray.Parse(allVMList);
            //List<dynamic> vmList = list.ToObject<List<dynamic>>();
            return allVMList;
        }

        public List<VMConfig> GetVMListByStuId(string stuid)
        {
            string allVMList = vms.GetVMList("", stuid, "", "");
            JArray list = JArray.Parse(allVMList);
            List<dynamic> vmList = list.ToObject<List<dynamic>>();
            return Deserialize(vmList);
        }

        public VMConfig GetVMByName(string name)
        {
            List<VMConfig> configs = GetVMList();
            foreach (VMConfig config in configs)
                if (config.Name.Equals(name) == true)
                    return config;
            return null;
        }

        /// <summary>
        /// 更改虚拟机名称
        /// </summary>
        /// <param name="vmName"></param>
        /// <param name="newName"></param>
        /// <returns>
        /// success...|error...
        /// </returns>
        public string Rename(string vmName, string newName)
        {
            return vms.RenameVirtualMachine(vmName, newName);
        }
        /// <summary>
        /// 将虚拟机标记为模板
        /// </summary>
        /// <param name="vmName"></param>
        /// <returns>
        /// success...|error...
        /// </returns>
        public string ConvertToTemplate(string vmName)
        {
            string ret = vms.ConvertToTemplate(vmName);
            return ret;
        }
        /// <summary>
        /// 将模板标记为虚拟机
        /// </summary>
        /// <param name="vmName"></param>
        /// <returns>
        /// success...|error...
        /// </returns>
        public string ConvertToVM(string vmName)
        {
            string ret = vms.ConvertToVirtualMachine(vmName, "", "");
            return ret;
        }
        /// <summary>
        /// 创建快照
        /// </summary>
        /// <param name="vmName"></param>
        /// <param name="snapshotName"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public string CreateSnapshot(string vmName, string snapshotName, string description)
        {
            return vms.SnapShotOps(vmName, "create", snapshotName, description, "0");
        }
        /// <summary>
        /// 获取快照
        /// </summary>
        /// <param name="vmName"></param>
        /// <returns></returns>
        public List<Snapshot> GetSnapshot(string vmName)
        {
            string ssList = vms.SnapShotOps(vmName, "list", "", "", "0");
            string[] ssSplit = ssList.Split('|');
            if (ssSplit.Count() < 3)
                return null;
            List<Snapshot> snapshotList = new List<Snapshot>();
            for(int i = 0; i < ssSplit.Count() - 3; i = i + 3)
            {
                Snapshot s = new Snapshot
                {
                    Name = ssSplit[i],
                    Description = ssSplit[i + 1],
                    CreateTime = DateTime.Parse(ssSplit[i + 2]).AddHours(8).ToString()
                };
                snapshotList.Add(s);
            }
            return snapshotList;
        }
        /// <summary>
        /// 删除快照
        /// </summary>
        /// <param name="vmName"></param>
        /// <param name="snapshotName"></param>
        /// <returns></returns>
        public string RemoveSnapshot(string vmName, string snapshotName)
        {
            return vms.SnapShotOps(vmName, "remove", snapshotName, "", "1");
        }
        /// <summary>
        /// 恢复快照
        /// </summary>
        /// <param name="vmName"></param>
        /// <param name="snapshotName"></param>
        /// <returns></returns>
        public string RevertSnapshot(string vmName, string snapshotName)
        {
            return vms.SnapShotOps(vmName, "revert", snapshotName, "", "1");
        }
        /// <summary>
        /// 修改虚拟机配置
        /// </summary>
        /// <param name="vmName">虚拟机名</param>
        /// <param name="CPU">CPU数量</param>
        /// <param name="memory">内存（MB）</param>
        /// <param name="disk">硬盘（MB）</param>
        /// <returns>success...|error...</returns>
        public string ChangeConfig(string vmName, int CPU, long memory, long disk)
        {
            return vms.ChangeConfig(vmName, CPU.ToString(), memory.ToString(), "", disk.ToString(), "", null, null, null, null);
        }

        private List<VMConfig> Deserialize(List<dynamic> vmList)
        {
            List<VMConfig> ret = new List<VMConfig>();
            foreach(var vmInfo in vmList)
            {
                string ip = vmInfo.IPAddress.ToString();
                if (ip != null && !ip.Equals("Unavailable"))
                {
                    List<string> ips = ip
                        .Replace("[", "").Replace("]","").Replace("\"", "").Replace("\r","").Replace("\n","")
                        .Replace(" ","")
                        .Split(',')
                        .Where(s => s.StartsWith(ipPrefix)).ToList();
                    ip = string.Join(",", ips);
                }
                VMStatus status = new VMStatus
                {
                    IPAddress = ip,
                    HostName = vmInfo.hostName.ToString(),
                    PowerState = vmInfo.powerState.ToString(),
                    RunTimeState = vmInfo.runTimeState.ToString()
                };

                VMConfig vm = new VMConfig
                {
                    Name = vmInfo.vmName.ToString(),
                    CPU = int.Parse(vmInfo.CPUByNum.ToString()),
                    Memory = long.Parse(vmInfo.memoryByMb.ToString()),
                    Disk = long.Parse(vmInfo.diskByB.ToString()) / 1024 / 1024,
                    IsTemplate = bool.Parse(vmInfo.isTemplate.ToString()),
                    GuestFullName = vmInfo.guestFullName.ToString(),
                    AdvancedConfig = "",
                    Status = status,
                    student_id = vmInfo.studentID.ToString(),
                    teacher_id = vmInfo.teacherID.ToString(),
                    admin_id = vmInfo.adminID.ToString(),
                    is_exps = vmInfo.isExperimental.ToString()
                };
                if (vm.is_exps != null && vm.is_exps.Equals("True")) vm.is_exp = true;
                else if (vm.is_exps != null && vm.is_exps.Equals("False")) vm.is_exp = false;
                else vm.is_exp = false;
                ret.Add(vm);
            }
            return ret;
        }

        public void ReadVMInfo()
        {
            string vmListStr = vms.GetVMList("", "", "", "");
            JArray list = JArray.Parse(vmListStr);
            List<dynamic> vmList = list.ToObject<List<dynamic>>();
            using (var vmContext = new VMContext())
            {
                foreach (var vm in vmList)
                {
                    /*

                    VirtualMachine virtualMachine = vmContext.VirtualMachines.Find(vm.vmName.ToString());
                    if (virtualMachine == null)
                    {
                        virtualMachine = new VirtualMachine();
                        virtualMachine.apply_id = 0;
                    }*/
                    VirtualMachine virtualMachine = new VirtualMachine();
                    if(!vm.isExperimental.ToString().Equals("default") && (bool)vm.isExperimental)
                    {
                        virtualMachine.owner_id = vm.teacherID.ToString();
                        virtualMachine.user_id = vm.studentID.ToString();
                    }
                    else
                    {
                        virtualMachine.owner_id = vm.adminID.ToString();
                        virtualMachine.user_id = vm.teacherID.ToString();
                    }
                    virtualMachine.vm_name = vm.vmName.ToString();
                    /**
                    if(vm.isTemplate=="true" && !virtualMachine.vm_name.EndsWith("|TP"))
                    {
                        vms.RenameVirtualMachine(virtualMachine.vm_name, virtualMachine.vm_name + "|TP");
                        virtualMachine.vm_name = virtualMachine.vm_name + "|TP";
                    }
                    **/
                    vmContext.VirtualMachines.Add(virtualMachine);
                }
                vmContext.SaveChanges();
            }
        }
    }
}