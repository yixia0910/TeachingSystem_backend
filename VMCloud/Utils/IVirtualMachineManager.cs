/*
*	Author:       Xing Zhihuan
*   Email:        1@roycent.cn	
*	Created:      2019/8/1 10:42:43
*   Description:  
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using VMCloud.Models;

namespace VMCloud.Utils
{
    interface IVirtualMachineManager
    {
        string PowerOption(string vmName, int option);
        string Delete(string vmName);
        int Delete(List<string> vmNames);
        string Create(VMConfig config, List<string> vmNameList, List<string> IDList, string teacherId="", string hostName = "");
        string Create(VMConfig config, string vmName, string ID, string hostName = "");
        List<Host> GetHosts();
        VMConfig GetVMInfo(string vmName);
        List<VMConfig> GetTemplates();
        List<VMConfig> GetVMList(List<string> vmNameList);
        string Rename(string vmName, string newName);
        string ConvertToTemplate(string vmName);
    }
}