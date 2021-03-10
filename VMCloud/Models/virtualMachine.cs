/*
*	Author:       Xing Zhihuan
*   Email:        1@roycent.cn	
*	Created:      2019/7/21 19:01:29
*   Description:  
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace VMCloud.Models
{
    public class VirtualMachine
    {
        public string hostName { get; set; }
        public string vmName { get; set; }
        public bool isTemplate { get; set; }
        public long memoryByMb { get; set; }
        public int CPUByNum { get; set; }

        public string guestFullName { get; set; }
        public int DiskNum { get; set; }
        public string powerState { get; set; }
        public string runTimeState { get; set; }
        public long diskByB { get; set; }
        public string IPAddress { get; set; }
        public string adminID { get; set; }
        public string studentID { get; set; }
        public string teacherID { get; set; }
        public string isExperimental { get; set; }
    }
}