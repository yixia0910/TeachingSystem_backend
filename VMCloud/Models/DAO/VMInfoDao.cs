using System.Collections.Generic;
using System.Linq;
using VMCloud.Utils;

namespace VMCloud.Models.DAO
{
    public class VMInfoDao
    {
        
    }

    public class SangforDao
    {
        /// <summary>
        /// 获取全部虚拟机id
        /// </summary>
        /// <returns>id列表</returns>
        public static List<string> GetAllId()
        {
            using (var dbContext = new DataModels())
            {
                List<SangforInfo> infos = dbContext.SangforInfos.ToList();
                List<string> ids = infos.Select(c => c.id).ToList();
                return ids;
            }
        }

        /// <summary>
        /// 根据虚拟机名称获取id
        /// </summary>
        /// <param name="names">虚拟机名称列表</param>
        /// <returns>id列表</returns>
        public static List<string> GetIdsByNames(List<string> names)
        {
            using (var dbContext = new DataModels())
            {
                List<SangforInfo> infos = dbContext.SangforInfos.ToList();
                List<string> ids = infos.Where(i => names.Contains(i.Name)).Select(c => c.id).ToList();
                return ids;
            }
        }

        /// <summary>
        /// 根据虚拟机id删除记录
        /// </summary>
        /// <param name="id">虚拟机id</param>
        public static void DeleteById(string id)
        {
            using (var dbContext = new DataModels())
            {
                SangforInfo info = dbContext.SangforInfos.Find(id);
                dbContext.SangforInfos.Remove(info);
                dbContext.SaveChanges();
            }
        }

        /// <summary>
        /// 增加虚拟机记录
        /// </summary>
        /// <param name="info">虚拟机记录</param>
        public static void Add(SangforInfo info)
        {
            using (var dbContext = new DataModels())
            {
                info = dbContext.SangforInfos.Add(info);
                dbContext.SaveChanges();
            }
            
        }

        /// <summary>
        /// 通过虚拟机名称获取虚拟机记录
        /// </summary>
        /// <param name="name">虚拟机名称</param>
        /// <returns></returns>
        public static SangforInfo GetSangforInfoByName(string name)
        {
            using (var dbContext = new DataModels())
            {
                List<string> id = dbContext.SangforInfos.Where(u => u.Name == name).Select(u => u.id).ToList();
                if (id.Count() == 0)
                    return null;
                SangforInfo info = dbContext.SangforInfos.Find(id[0]);
                return info;
            }
            return null;
        }

        /// <summary>
        /// 更改虚拟机类型
        /// </summary>
        /// <param name="vmName">虚拟机名称</param>
        /// <param name="operateType">操作方式</param>
        public static void ChangeVMTemplate(string vmName, int operateType)
        {
            SangforInfo info = GetSangforInfoByName(vmName);
            using (var dbContext = new DataModels())
            {
                info = dbContext.SangforInfos.Find(info.id);
                if (operateType == 0)
                {
                    info.IsTemplate = false;
                }
                else
                {
                    info.IsTemplate = true;
                }

                dbContext.SaveChanges();
            }
        }

        /// <summary>
        /// 合并数据
        /// </summary>
        /// <param name="vmConfigs">统一虚拟机数据</param>
        /// <returns></returns>
        public static List<VMConfig> MergeData(List<VMConfig> vmConfigs)
        {
            using (var dbContext = new DataModels())
            {
                RESTful restful = new RESTful();
                foreach (VMConfig config in vmConfigs)
                {
                    SangforInfo info = GetSangforInfoByName(config.Name);
                    config.admin_id = info.admin_id;
                    config.teacher_id = info.teacher_id;
                    config.student_id = info.student_id;
                    config.is_exp = info.is_exp;
                    config.is_exps = info.is_exps;
                    config.IsTemplate = info.IsTemplate;
                    config.TemplateName = info.TemplateName;
                    config.console_url = restful.OpenConsole(info.id);
                    
                    if (config.Status.PowerState.Equals("ACTIVE"))
                    {
                        config.Status.PowerState = "poweredOn";
                    }
                    else
                    {
                        config.Status.PowerState = "poweredOff";
                    }
                }
                List<SangforInfo> infos = dbContext.SangforInfos.ToList();
                infos = infos.Where(u => u.IsTemplate == true).ToList();
                foreach (SangforInfo info in infos)
                {
                    VMConfig config = new VMConfig();
                    config.admin_id = info.admin_id;
                    config.teacher_id = info.teacher_id;
                    config.student_id = info.student_id;
                    config.is_exp = info.is_exp;
                    config.is_exps = info.is_exps;
                    config.IsTemplate = info.IsTemplate;
                    config.TemplateName = info.TemplateName;
                    config.Name = info.Name;
                    vmConfigs.Add(config);
                }

            }
            return vmConfigs;
        }
        
    }
}