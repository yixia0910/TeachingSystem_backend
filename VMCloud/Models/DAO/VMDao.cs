/*
*	Author:       Xing Zhihuan
*   Email:        1@roycent.cn	
*	Created:      2019/7/24 15:20:06
*   Description:  数据库访问类，虚拟机相关
 */
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Web;
using VMCloud.Utils;

namespace VMCloud.Models.DAO
{
    public static class VMDao
    {
        public static string ManagerType;
        private static WebService manager;
        private static RESTful restful;
        

        static VMDao()
        {
            ManagerType = ConfigurationManager.AppSettings["VMManagerType"];
            if (ManagerType.Equals("Sangfor"))
            {
                restful = new RESTful();
            }
            else
            {
                manager = new WebService();
            }
        }

        /// <summary>
        /// 获取全部虚拟机
        /// </summary>
        /// <returns></returns>
        private static List<VMConfig> GetAllVm()
        {
            List<VMConfig> configs;
            if (ManagerType.Equals("Sangfor"))
            {
                configs = restful.GetVMList(SangforDao.GetAllId());
                configs = SangforDao.MergeData(configs);
            }
            else
            {
                configs = manager.GetVMList();
                using (var context = new DataModels())
                {
                    List<VirtualMachine> virtualMachines = context.VirtualMachines.ToList();
                    foreach (var vm in virtualMachines)
                    {
                        var conf = configs.FirstOrDefault(c => c.Name.Equals(vm.vm_name));
                        if (conf != null)
                            conf.due_time = vm.due_time;
                    }
                }
            }
            return configs;
        }

        /// <summary>
        /// 获取用户对某虚拟机的权限
        /// </summary>
        /// <param name="vmName">虚拟机名称</param>
        /// <param name="userId">id</param>
        /// <returns>
        /// 0:无权限
        /// 1:仅有开关机权限
        /// 2:有完全权限
        /// </returns>
        public static int GetPermissionForVM(string vmName, string userId)
        {
            VMConfig config = null;
            if (ManagerType.Equals("Sangfor"))
            {
                List<VMConfig> configs = GetAllVm();
                configs = configs.Where(c => c.Name == vmName).ToList();
                if (configs.Count() != 0)
                    config = configs[0];
            }
            else
            {
                config = manager.GetVMByName(vmName);
            }
            
            if (config == null) return 0;
            if (config.student_id == null) return 2;
            User user = UserDao.GetUserById(userId);
            if (user.role == 4) return 2;
            if(config.is_exps.Equals("False"))
            {
                if (config.student_id.Equals(userId) || config.teacher_id.Equals(userId))
                    return 2;
                if (user.role == 3)
                {
                    User owner = UserDao.GetUserById(config.student_id);
                    if (owner == null) return 0;
                    if (owner.department_id.Equals(user.department_id) == true)
                        return 2;
                    else return 0;
                }
                return 0;
            }
            else
            {
                if (config.teacher_id == userId)
                    return 2;
                if (config.student_id == userId)
                    return 1;
                if (user.role == 3)
                {
                    User owner = UserDao.GetUserById(config.teacher_id);
                    if (owner == null) return 0;
                    if (owner.department_id.Equals(user.department_id) == true)
                        return 2;
                    else return 0;
                }
            }
            return 0;
        }
        /// <summary>
        /// 获取可使用的虚拟机列表
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static List<VMConfig> GetUsingVMs(string userId)
        {
            List<VMConfig> configs = GetAllVm();
            List<VMConfig> configs1 = new List<VMConfig>();
            configs1 = configs.Where(c => c.is_exps.Equals("True")).ToList();
            
            User user = UserDao.GetUserById(userId);
            if (user == null) return null;
            if(user.role==1)
            {
                configs = configs1;
                configs1 = new List<VMConfig>();
                foreach(VMConfig fig in configs)
                {
                    if (fig.student_id != null && fig.student_id.Equals(user.id))
                    {
                        configs1.Add(fig);
                    }
                }
                var expList = ExperimentDao.GetExperimentByStuId(user.id).Where(e => e.vm_passwd != null).ToList();
                foreach(var e in expList)
                {
                    var expVM = configs1.Where(c => c.Name.StartsWith(e.vm_name)).ToList();
                    expVM.ForEach(v => v.AdvancedConfig = e.vm_passwd);
                }
                return configs1;
            }
            else if(user.role==4)
            {
                return configs1;
            }
            else if(user.role==2)
            {
                configs = configs1;
                configs1 = new List<VMConfig>();
                foreach (VMConfig config in configs)
                {
                    User user1 = UserDao.GetUserById(config.student_id);
                    if (user1.department_id.Equals(user.department_id))
                        configs1.Add(config);
                }
                return configs1;
            }
            return null;
        }
        /// <summary>
        /// 获取拥有的虚拟机列表,包括私有模板以及公有模板
        /// </summary>
        /// <param name="userId">id</param>
        /// <returns></returns>
        public static List<VMConfig> GetOwningVMs(string userId)
        {
            List<VMConfig> configs = GetAllVm();
            
            List<VMConfig> configs1 = new List<VMConfig>();
            foreach (VMConfig config in configs)
            {
                
                if (config.student_id == null || config.student_id.Length == 0 || userId.Equals(config.student_id))
                    configs1.Add(config);
            }
            return configs1;
        }
        /// <summary>
        /// 获取某部门所有虚拟机列表
        /// </summary>
        /// <param name="departId"></param>
        /// <returns></returns>
        public static List<VMConfig> GetDepartVMs(string departId)
        {
            List<string> ids = UserDao.GetUsersInDepart(departId);
            List<VMConfig> configs = GetAllVm();
            List<VMConfig> configs1 = new List<VMConfig>();
            foreach(VMConfig config in configs)
            {
               
                    if(config.is_exp==true)
                    {
                        User user = UserDao.GetUserById(config.teacher_id);
                        if (user!=null&&user.department_id.Equals(departId))
                            configs1.Add(config);
                    }
                    else
                    {
                        User user = UserDao.GetUserById(config.student_id);
                        if (user != null && user.department_id.Equals(departId))
                            configs1.Add(config);
                    }
                
            }
            return configs1;
        }
        /// <summary>
        /// 获取某课程所有虚拟机列表 by zzw
        /// </summary>
        /// <param name="courseId"></param>
        /// <returns></returns>
        public static List<VMConfig> GetCourseVMs(int courseId)
        {
            List<Experiment> experiment = ExperimentDao.GetExperimentByCourseId(courseId);
            List<VMConfig> configs = GetAllVm();
            List<VMConfig> configs1 = new List<VMConfig>();
            List<string> apply_s = new List<string>();
            foreach (Experiment experiment1 in experiment)
                apply_s.Add(experiment1.vm_name);
            foreach(VMConfig config in configs)
            {
                String name = config.Name;
                if (name == null) continue;
                foreach(string apply in apply_s)
                {
                    if(name.StartsWith(apply+"_"))
                    {
                        configs1.Add(config);
                        break;
                    }
                }
            }
            return configs1;
        }
        /// <summary>
        /// 通过实验虚拟机名获取虚拟机列表 zzw
        /// </summary>
        /// <param name="vmName"></param>
        /// <returns></returns>
        public static List<VMConfig> GetVMsByVmName(string vmName)
        {
            List<VMConfig> configs = GetAllVm();
            List<VMConfig> configs1 = new List<VMConfig>();
            foreach (VMConfig config in configs)
            {
                String name = config.Name;
                if (name == null) continue;
                if (name.StartsWith(vmName + "_"))
                {
                    configs1.Add(config);
                }
                
            }
            return configs1;
        }
        /// <summary>
        /// 获取所有实验虚拟机zzw
        /// </summary>
        /// <param></param>
        /// <returns></returns>
        public static List<VMConfig> GetAllExpVirtualMachine()
        {
            List<VMConfig> configs = GetAllVm();
            List<VMConfig> configs1 = configs.Where(c => c.is_exps.Equals("True")).ToList();
            
            return configs1;
        }
        


        
        /// <summary>
        /// 通过虚拟机名列表获取虚拟机列表
        /// </summary>
        /// <param name="vmNames"></param>
        /// <returns></returns>
        public static List<VMConfig> GetMultiVMs(List<string> vmNames)
        {
            
            List<VMConfig> configList = GetAllVm();
            List<VMConfig> configList1 = new List<VMConfig>();
            foreach (VMConfig config in configList)
            {
                if (config.Name == null) continue;
                foreach (string name in vmNames)
                {
                    if (config.Name.Equals(name))
                    {
                        configList1.Add(config);
                        break;
                    } 
                }
            }

            return configList1;
        }
        public static List<VMConfig> GetTemplates(string userId, bool onlyMine = false)
        {
            List<VMConfig> configs = GetAllVm();
            configs = configs.Where(c => c.IsTemplate == true).ToList();
            if (onlyMine)
                configs = configs.Where(c => c.student_id.Equals(userId) || c.teacher_id.Equals(userId)).ToList();
            else
                configs = configs.Where(c =>
                        c.teacher_id.Equals("all") || c.student_id.Equals(userId) || c.teacher_id.Equals(userId))
                    .ToList();
            return configs;
            
        }
        /// <summary>
        /// 获取某个学生的所有实验虚拟机以及详细信息
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static List<VMConfig> GetStuExpVM(string userId)
        {
            List<VMConfig> configList = GetAllVm();
            List<VMConfig> configList1 = new List<VMConfig>();
            foreach (VMConfig config in configList)
                if (config.is_exp == true && config.student_id != null && config.student_id.Equals(userId))
                    configList1.Add(config);
            return configList1;
            
        }
        //public static List<VMConfig> GetTeaExpVM(string userId)
        //{
        //    User user = UserDao.GetUserById(userId);
        //    List<VirtualMachine> vms = null;
        //    using (var vmContext = new VMContext())
        //    {
        //        if (user.role == 2)
        //            vms = vmContext.VirtualMachines.Where(v => v.owner_id == userId&&v.course_id!=null).ToList();
        //        else if(user.role == 1)
        //        {
        //            List<Assistant> assistants = CourseDao.GetAssistantsByStuId(userId);
        //            if (assistants.Count == 0)
        //                return null;
        //            else
        //            {
        //                vms = vmContext.VirtualMachines.Where(v => assistants.Exists(a => a.course_id == v.course_id)).ToList();
        //            }
        //        }
        //    }
        //    if (vms != null)
        //        return manager.GetVMList(vms.Select(v => v.vm_name).ToList());
        //    return null;
        //}
        public static List<VMConfig> GetAllExpVM()
        {
            List<VMConfig> configList = GetAllVm();
            List<VMConfig> configList1 = new List<VMConfig>();
            configList = manager.GetVMList();
            foreach (VMConfig config in configList)
                if (config.is_exp == true)
                    configList1.Add(config);
            return configList1;

        }
        /// <summary>
        /// 获取非实验虚拟机通过用户id
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static List<VMConfig> GetPersonVMsByUserId(string userId)
        {
            
            List<VirtualMachine> vms = new List<VirtualMachine>();
            List<VMConfig> configList = GetAllVm();
            List<VMConfig> configList1 = new List<VMConfig>();
            configList1 = configList.Where(c => !c.is_exps.Equals("True"))
                .Where(c => c.teacher_id.Equals(userId) || c.student_id.Equals(userId)).ToList();
            vms = DataUtil.Transform(configList1);
            var list = configList1.Join(vms, conf => conf.Name, vm => vm.vm_name, (conf, vm) => new VMConfig
            {
                Name = vm.vm_name,
                CPU = conf.CPU,
                Memory = conf.Memory,
                Disk = conf.Disk,
                IsTemplate = conf.IsTemplate,
                GuestFullName = conf.GuestFullName,
                TemplateName = conf.TemplateName,
                AdvancedConfig = conf.AdvancedConfig,
                Status = conf.Status,
                console_url = conf.console_url,
                due_time = conf.due_time
            }).ToList<VMConfig>();
            return list;
        }
        /// <summary>
        /// 创建一个虚拟机
        /// </summary>
        /// <param name="vmConfig">虚拟机配置信息</param>
        /// <param name="vm">虚拟机权限信息</param>
        /// <returns></returns>
        public static string CreateVM(VMConfig vmConfig, VirtualMachine vm)
        {

            string res="success";
            if (ManagerType.Equals("Sangfor"))
            {
                SangforInfo info = new SangforInfo();
                info.student_id = vm.owner_id;
                info.teacher_id = vm.owner_id;
                info.IsTemplate = false;
                info.is_exp = false;
                info.is_exps = "False";
                info.Name = vm.vm_name;
                info.TemplateName = vmConfig.TemplateName;
                //vmConfig.Disk = vmConfig.Disk / 1024;

                string result = restful.Create(vmConfig, info.Name, null, null);
                if (!result.StartsWith("error"))
                {
                    info.id = result;
                    SangforDao.Add(info);
                }
            }
            else
            {
                res = manager.Create(vmConfig, vm.vm_name, vm.owner_id);
            }
            return res;

        }
        /// <summary>
        /// 为实验创建虚拟机
        /// </summary>
        /// <param name="vmConfig">虚拟机配置</param>
        /// <param name="vmName">虚拟机名</param>
        /// <param name="expId">实验id</param>
        /// <returns></returns>
        public static string CreateVMsForExp(VMConfig vmConfig, int applyId,string stulist)
        {
            Experiment exp = ExperimentDao.GetExperimentByApplyId(applyId);

            Course course = CourseDao.GetCourseInfoById((int)exp.course_id);
            vmConfig.Name = exp.vm_name;
            string teacherId = course.teacher_id;
            string[] ids = stulist.Split(' ');
            List<string> stuIds = new List<string>();
            foreach (string id in ids)
            {
                if(id != null&&id.Length!=0)
                  stuIds.Add(id);
            }
            string[] names = new string[stuIds.Count()];
            string res = "success";
            
            
            for (int i = 0; i < stuIds.Count(); ++i)
            {
                names[i] = vmConfig.Name + "_" + stuIds[i];
            }
            if (ManagerType.Equals("Sangfor"))
            {
                List<SangforInfo> infoList = new List<SangforInfo>();
                for (int i = 0; i < stuIds.Count(); ++i)
                {
                    SangforInfo info = new SangforInfo();
                    info.student_id = stuIds[i];
                    info.teacher_id = teacherId;
                    info.IsTemplate = false;
                    info.is_exp = true;
                    info.is_exps = "True";
                    info.Name = names[i];
                    info.TemplateName = vmConfig.TemplateName;
                    infoList.Add(info);
                }
                //vmConfig.Disk = vmConfig.Disk / 1024;
               
                foreach (SangforInfo info in infoList)
                {
                    string result = restful.Create(vmConfig, info.Name, null, null);
                    if (!result.StartsWith("error"))
                    {
                        info.id = result;
                        SangforDao.Add(info);
                    }
                }
                // 使用新逻辑创建，优化创建时间
                //restful.CreateNewLogic(vmConfig,infoList);
            }
            else
            {
                res = manager.Create(vmConfig, names.ToList(), stuIds.ToList(), teacherId);
            }

            return res;
        }
        /// <summary>
        /// 删除虚拟机
        /// </summary>
        /// <param name="vmName"></param>
        /// <returns></returns>
        public static string DeleteVM(string vmName)
        {
            if (ManagerType.Equals("Sangfor"))
            {
                SangforInfo info = SangforDao.GetSangforInfoByName(vmName);
                if(info!=null)
                  restful.Delete(info.id);
                return "";
            }
            else
            {
                return manager.Delete(vmName);
            }
        }
        /// <summary>
        /// 删除多个虚拟机
        /// </summary>
        /// <param name="vmNames"></param>
        /// <returns></returns>
        public static int DeleteVM(string[] vmNames)
        {
            if (ManagerType.Equals("Sangfor"))
            {
                restful.Delete(SangforDao.GetIdsByNames(vmNames.ToList()));
            }
            else
            {
                manager.Delete(vmNames.ToList());
            }
            return 1;
        }
        /// <summary>
        /// 按applyId删除虚拟机
        /// </summary>
        /// <param name="applyId"></param>
        /// <returns></returns>
        public static int DeleteVMByApplyId(int applyId)
        {
            List<VMConfig> configList;
            List<string> nameList = new List<string>();
            configList = GetAllVm();
            string vmname = VMDao.GetApplyRecord(applyId).vm_name;
            foreach(VMConfig config in configList)
            {
                if (config.Name.StartsWith(vmname))
                    nameList.Add(config.Name);
            }
            int count;
            if (ManagerType.Equals("Sangfor"))
            {
                count = restful.Delete(SangforDao.GetIdsByNames(nameList));
            }
            else
            {
                count = manager.Delete(nameList);
            }

            return count ;
        }
        public static List<Apply_record> GetAllApplyRecord()
        {
            using (var context = new DataModels())
            {
                return context.Apply_record.ToList();
            }
        }
        //返回添加的记录id
        public static int AddApplyRecord(Apply_record record)
        {
            using (var context = new DataModels())
            {
                context.Apply_record.Add(record);
                context.SaveChanges();
                return record.id;
            }
            
        }

        public static Apply_record GetApplyRecord(int recordId)
        {
            using (var context = new DataModels())
            {
                return context.Apply_record.Find(recordId);
            }
        }
        public static List<Apply_record> GetApplyRecordsBySenderId(string senderId)
        {
            using (var context=new DataModels())
            {                
                List<Apply_record> res = new List<Apply_record>();
                res = context.Apply_record.Where(ar => ar.sender_id == senderId).ToList();
                return res;
            }
        }
        public static List<Apply_record> GetPendingApplyRecordsBySenderId(string senderId)
        {
            using (var context = new DataModels())
            {
                List<Apply_record> res = new List<Apply_record>();
                res = context.Apply_record.Where(ar => ar.sender_id == senderId&&ar.status==0).ToList();
                return res;
            }
        }
        public static string ChangeApplyRecordStatus(int recordId,int status,string replyMsg="")
        {
            using (var context=new DataModels())
            {            
                Apply_record edit_record = context.Apply_record.Find(recordId);
                DateTime currentTime = DateTime.Now;
                edit_record.finish_time = currentTime.ToString("yyyy/M/d HH:MM:ss");
                edit_record.status = status;
                edit_record.reply_msg = replyMsg;
                context.SaveChanges();
                return "success";           
            }
        }
        public static Apply_record GetLastApplyRecordbySenderID(string senderID)
        {
            using (var context = new DataModels())
            {
                return context.Apply_record.Where(r=>r.sender_id==senderID).Last();
            }
        }
        public static string ChangeApplyUseTime(int recordId, string dueTime)
        {
            using (var context=new DataModels())
            {
                Apply_record edit_record = context.Apply_record.Find(recordId);
                if (edit_record == null)
                    throw new Exception("record not found");
                edit_record.due_time = dueTime;
                if (context.SaveChanges() > 0)
                    return "success";
                throw new Exception("error");
            }
        }

        /// <summary>
        /// 修改虚拟机：模板转化 0-变成虚拟机 1-变成模板
        /// </summary>
        /// <param name="vmName"></param>
        /// <returns></returns>
        public static int ChangeVMTemplate(string vmName, int operateType)
        {
            if (ManagerType.Equals("Sangfor"))
            {
                //restful.PowerOption(vmName,2);
                //SangforDao.ChangeVMTemplate(vmName,operateType);
            }
            else
            {
                string res;
                if (operateType == 0)
                {
                    res = manager.ConvertToVM(vmName);
                }
                else
                {
                    res = manager.ConvertToTemplate(vmName);
                }

                if (!res.StartsWith("success"))
                {
                    throw new Exception(res);
                }
            }

            return 0;

        }
        /// <summary>
        /// 删除数据库虚拟机表项
        /// 
        /// </summary>
        /// <param name="vmName"></param>
        /// 
        /// <returns></returns>
        public static void DeleteVMInDB(string vmName)
        {
            using (var dbcontext = new DataModels())
            {
                VirtualMachine vm = dbcontext.VirtualMachines.Find(vmName);
                if (vm != null)
                {
                    dbcontext.VirtualMachines.Remove(vm);
                    dbcontext.SaveChanges();
                }
            }
        }
        /// <summary>
        /// 删除数据库虚拟机表项
        /// 
        /// </summary>
        /// <param name = "vmNames" ></ param >
        ///
        /// < returns ></ returns >
        public static void DeleteVMInDB(string[] vmNames)
        {
            using (var dbcontext = new DataModels())
            {
                foreach (string vmName in vmNames)
                {
                    VirtualMachine vm = dbcontext.VirtualMachines.Find(vmName);
                    if (vm != null)
                    {
                        dbcontext.VirtualMachines.Remove(vm);

                    }
                }
                dbcontext.SaveChanges();

            }
        }
        /// <summary>
        /// 将虚拟机添加至数据库记录中
        /// </summary>
        /// <param name="vm"></param>
        /// <returns></returns>
        public static int AddVMInDB(VirtualMachine vm)
        {
            using (var dbContext = new DataModels())
            {
                dbContext.VirtualMachines.Add(vm);
                return dbContext.SaveChanges();
            }
        }
        /// <summary>
        /// 将虚拟机添加至数据库记录中
        /// </summary>
        /// <param name="vms"></param>
        /// <returns></returns>
        public static int AddVMInDB(List<VirtualMachine> vms)
        {
            using (var dbContext = new DataModels())
            {
                dbContext.VirtualMachines.AddRange(vms);
                return dbContext.SaveChanges();
            }
        }
        
        /// <summary>
        /// 根据名称查找虚拟机
        /// 
        /// </summary>
        /// <param name="vmName"></param>
        /// 
        /// <returns></returns>
        public static VMConfig GetVMByName(string vmName)
        {
            List<VMConfig> vmConfigs = GetAllVm();
            foreach (VMConfig config in vmConfigs)
                if (config.Name.Equals(vmName) == true)
                    return config;
            return null;

            //using (var dbcontext = new DataModels())
            //{
                //return dbcontext.VirtualMachines.Find(vmName);
               
            //}
        }

        public static List<VMConfig> GetAllVMS(int department_id = 0)
        {
            List<VMConfig> configs = GetAllVm();
            return configs;
            //using (var dbcontext = new DataModels())
            //{
            //    var vms = dbcontext.VirtualMachines.ToList();
            //    var vmNames = vms.Select(v => v.vm_name).ToList();
            //    var vmIds=vms.Select(v => v.uuid).ToList();
            //    List<VMConfig> configs;
                
            //        configs = manager.GetVMList(vmNames);
                
            //    var query = configs.Join(vms, conf => conf.Name, vm => vm.vm_name, (conf, vm) => new
            //    {
            //        vm,
            //        conf
            //    });
            //    return vms;
            //}
        }

        
        public static List<VMConfig> getConfigs(List<string> vmNames)
        {
            List<VMConfig> configs = GetAllVm();
            return configs.Where(c => vmNames.Contains(c.Name)).ToList();
        }

        public static List<VMConfig> getConfigs()
        {
            return GetAllVm();
        }

        public static void AddWarnTimes(string vmNames)
        {
            using (var dbContext = new DataModels())
            {
                var vm = dbContext.VirtualMachines.Find(vmNames);
                if (vm != null)
                {
                    vm.warn_times++;
                    dbContext.SaveChanges();
                }
            }
        }

        public static List<VirtualMachine> GetVMNearDue()
        {
            using (var dbContext = new DataModels())
            {
                var vmList = dbContext.VirtualMachines
                    .Where(v => v.warn_times < 3).ToList()
                    .Where(v => v.due_time != null)
                    .Where(v => !HttpUtil.IsTimeLater(v.due_time, v.warn_times))
                    .ToList();
                return vmList;
            }
        }
    }
}