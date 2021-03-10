/*
*	Author:       Xing Zhihuan
*   Email:        1@roycent.cn	
*	Created:      2019/7/24 17:03:42
*   Description:  
 */
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace VMCloud.Models
{
    public class VMConfig
    {
        public string Name { get; set; }//虚拟机名
        public int CPU { get; set; }//个
        public long Memory { get; set; }//MB
        public long Disk { get; set; }//MB
        public bool IsTemplate { get; set; }//是否为模板
        public string GuestFullName { get; set; }//OS名
        public string TemplateName { get; set; }//模板名
        public string TemplateId { get; set; }//模板id
        public string AdvancedConfig { get; set; }//高阶配置
        public VMStatus Status { get; set; }
        public string due_time { get; set; }
        public string student_id { get; set; }
        public string admin_id { get; set; }
        public string teacher_id { get; set; }
        public bool is_exp { get; set; }
        public string errorType {get; set; }
        public string is_exps { get; set; }

        public string console_url { get; set; }
    }
    
    [Table("fxcloud.sangforInfo")]
    public class SangforInfo
    {
        [StringLength(100)]
        public string Name { get; set; }//虚拟机名
        
        [StringLength(100)]
        public string id { get; set; }
        
        public bool IsTemplate { get; set; }//是否为模板
        
        [StringLength(100)]
        public string TemplateName { get; set; }//模板名
        
        [StringLength(100)]
        public string TemplateId { get; set; }//模板id
        
        [StringLength(100)]
        public string student_id { get; set; }
        
        [StringLength(100)]
        public string admin_id { get; set; }
        
        [StringLength(100)]
        public string teacher_id { get; set; }
        public bool is_exp { get; set; }
        public string is_exps { get; set; }
        public string image_disk { get; set; }
    }

    public class Host
    {
        public string HostName { get; set; }
        public string IsConnected { get; set; }
        public string RunTimeState { get; set; }
        public long CPUTotal { get; set; }
        public long CPUUsed { get; set; }
        public long MemoryTotal { get; set; }
        public long MemoryUsed { get; set; }
        public List<string> VirtualMachineList { get; set; }
    }

    public class VMStatus
    {
        public string IPAddress { get; set; }
        public string HostName { get; set; }
        public string PowerState { get; set; }
        public string RunTimeState { get; set; }
    }

    public class Snapshot
    {
        public string Name { get; set; }
        public string CreateTime { get; set; }
        public string Description { get; set; }
    }

    public class Data
    {
        public string Name { get; set; }
        public string number { get; set; }



    }

    public class CpuInfo
    {
        public int number { get; set; }
        public int size { get; set; }
    }

    public class MemoryInfo
    {
        public int number { get; set; }
        public long size { get; set; }
    }

    public class UserInfo
    {
        public string id { get; set; }
        public string name { get; set; }
        public int? role { get; set; }
        public int vmNumber { get; set; }

    }
}