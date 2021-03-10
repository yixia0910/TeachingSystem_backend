
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using VMCloud.Models;
using VMCloud.Models.DAO;

namespace VMCloud.Utils
{
    public static class DataUtil
    {
        public static int getOnlineNumber()
        {
            WebService wb = new WebService();
            List<Host> vlist = wb.GetHosts();

            return vlist.Count();

        }

        public static String GetSumCpu()
        {
            
            List<VMConfig> confs = VMDao.getConfigs();
            int sumCpu = 0;
            foreach (VMConfig vm in confs)
            {

                sumCpu += vm.CPU;

            }
            return sumCpu.ToString();

        }
        public static String GetSumMemory()
        {
            
            List<VMConfig> confs = VMDao.getConfigs();
            long sumMemory = 0;
            foreach (VMConfig vm in confs)
            {

                sumMemory += vm.Memory;

            }
            return sumMemory.ToString();
        }

        public static String GetVmNumber()
        {

            
            List<VMConfig> confs = VMDao.getConfigs();
            return confs.Count().ToString();


        }

        public static String GetCourseNumber()
        {

            List<Course> courses = CourseDao.GetAllCourse();
            return courses.Count().ToString();

        }
        public static List<VirtualMachine> Transform(List<VMConfig> configs)
        {
            List<VirtualMachine> virtuals = new List<VirtualMachine>();
            foreach(VMConfig config in configs)
            {
                VirtualMachine machine = new VirtualMachine();
                machine.owner_id = config.teacher_id;
                machine.user_id = config.student_id;
                machine.vm_name = config.Name;
                virtuals.Add(machine);
            }

            return virtuals;

        }
        public static List<Data> GetDatas()
        {
            List<Data> datas = new List<Data>();
            Data data = new Data();
            data.Name = "CPU总核心数";
            data.number = GetSumCpu();
            datas.Add(data);
            data = new Data();
            data.Name = "总内存";
            data.number = GetSumMemory()+"MB";
            datas.Add(data);
            data = new Data();
            data.Name = "虚拟机总数";
            data.number = GetVmNumber() + "台";
            datas.Add(data);
            data = new Data();
            data.Name = "课程总数";
            data.number = GetCourseNumber();
            datas.Add(data);
            return datas;

        }




    }
}