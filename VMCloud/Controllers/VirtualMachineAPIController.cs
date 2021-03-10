using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using VMCloud.Models;
using VMCloud.Models.DAO;
using VMCloud.Utils;
using System.Threading;
using System.Threading.Tasks;
using System.Configuration;

namespace VMCloud.Controllers
{
    //[RoutePrefix("api")]
    public class VirtualMachineAPIController : ApiController
    {
        public RedisHelper redis;

        /// <summary>
        /// 初始化Helper
        /// </summary>
        public VirtualMachineAPIController()
        {
            redis = RedisHelper.GetRedisHelper();
        }

        private static dynamic getManager()
        {
            dynamic manager;
            string managerType = ConfigurationManager.AppSettings["VMManagerType"];
            if (managerType.Equals("VMware"))
                manager = new WebService();
            else if (managerType.Equals("Sangfor"))
                manager = new RESTful();
            else
                throw new Exception("No available service can be used for managing virtual machines. Please check your settings.");
            //manager = new WebService();
            return manager;
        }

        [Route("virtualMachine/expVMInfo"),HttpGet]
        public HttpResponseMessage ExpVMInfo([FromBody]JObject userId)
        {
            try
            {
                var jsonParams = HttpUtil.Deserialize(userId);
                string id = jsonParams.id;
                VCManagerService vms = new VCManagerService();
                string vmData = vms.GetVMList("", id, "", "true");
                return new Response(1001,"获取实验虚拟机信息成功",vmData).Convert();
            }catch(Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return new Response(4001).Convert();
            }
        }
        /// <summary>
        /// 管理员是否有没处理的申请记录
        /// </summary>
        /// <returns></returns>
        [Route("common/applynotfinish"), HttpGet]
        public HttpResponseMessage IsApplyNotFinish()
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                bool login = redis.IsSet(signature);
                if (!login)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string id = redis.Get<string>(signature);
                User user = UserDao.GetUserById(id);
                List<Apply_record> res = null;
                if (user.role == 4)
                {
                    res = VMDao.GetAllApplyRecord();
                    foreach(Apply_record apply in res)
                    {
                        if(apply.status==0)
                        {
                            return new Response(1001, "Success", 1).Convert();
                        }
                    }
                }
                return new Response(1001, "Success", 2).Convert();

            }
            catch (Exception ex)
            {
                ErrorLogUtil.WriteLogToFile(ex, Request);
                return Response.Error();
            }
        }


        /// <summary>
        /// 超级管理员查看所有申请记录  
        /// created by xzy
        /// 2019.9.21
        /// </summary>
        /// <param ></param>
        /// 用户查看自己所有申请记录
        /// 备注：直接返回状态码
        /// 
        /// <returns>
        ///  return 
        ///  {
        ///     "code":,
        ///     "msg":""
        ///     "data": { 
        ///             "id":"",
        ///             "sendor_id":"",
        ///             "apply_time":"",
        ///             "finish_time":"",
        ///             "status":"",
        ///             "vm_name":"",
        ///             "operate_type":"",
        ///             "detail":"",
        ///             "apply_msg":"",
        ///             "reply_msg":"",
        ///             }
        ///   }
        /// </returns>
        ///
        [Route("common/allapplicationInfo"), HttpGet]
        public HttpResponseMessage GetAllApplyRecord()
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                bool login = redis.IsSet(signature);
                if (!login)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string id = redis.Get<string>(signature);
                User user = UserDao.GetUserById(id);
                List<Apply_record> res = null;
                if (user.role <3)
                {
                    return new Response(2002, "无权限访问").Convert();
                }
                else if (user.role == 4)
                {
                    res = VMDao.GetAllApplyRecord();
                }
                else if(user.role == 3)
                {
                    List<string> ids = UserDao.GetUsersInDepart(user.department_id);
                    res = VMDao.GetAllApplyRecord().Where(ar => ids.Exists(_id => _id.Equals(ar.sender_id))).ToList();
                }
                var param = Request.GetQueryNameValuePairs().ToDictionary(k => k.Key, v => v.Value);
                if (param.Count > 0)
                {
                    int page = int.Parse(param["page"]);
                    int size = int.Parse(param["size"]);
                    int count;
                    using (var records = new DataModels())
                    {
                        var recordList = records.Apply_record.Pagination(
                            r => r.status == null ? int.MaxValue : (r.status == 0 ? 0 : 1),
                            page, size, out count,
                            r => -r.id).ToList();
                        Dictionary<string, object> ret = new Dictionary<string, object>();
                        ret.Add("data", AddUserNameIntoRecordList(recordList));
                        ret.Add("count", count);
                        return new Response(1001, "Success", ret).Convert();
                    }
                }
                return new Response(1001, "Success", AddUserNameIntoRecordList(res)).Convert();

            }
            catch (Exception ex)
            {
                ErrorLogUtil.WriteLogToFile(ex, Request);
                return Response.Error();
            }
        }

        private List<Apply_record> AddUserNameIntoRecordList(List<Apply_record> records)
        {
            Dictionary<string, string> nameParis = new Dictionary<string, string>();
            foreach(var r in records)
            {
                string name = "";
                if(!nameParis.TryGetValue(r.sender_id, out name))
                {
                    User u = UserDao.GetUserById(r.sender_id);
                    if(u != null)
                    {
                        name = u.name;
                        nameParis.Add(r.sender_id, name);
                    }
                }
                r.sender_id = r.sender_id + "(" + name + ")";
            }
            return records;
        }
        /// <summary>
        /// 查看申请记录  
        /// created by xzy
        /// 2019.8.18
        /// </summary>
        /// <param name="status">status</param>
        /// 用户查看自己所有申请记录
        /// 备注：直接返回状态码
        /// 
        /// <returns>
        ///  return 
        ///  {
        ///     "code":,
        ///     "msg":""
        ///     "data": { 
        ///             "id":"",
        ///             "sendor_id":"",
        ///             "apply_time":"",
        ///             "finish_time":"",
        ///             "status":"",
        ///             "vm_name":"",
        ///             "operate_type":"",
        ///             "detail":"",
        ///             "apply_msg":"",
        ///             "reply_msg":"",
        ///             }
        ///   }
        /// </returns>
        ///
        [Route("common/applicationInfo"), HttpGet]
        public HttpResponseMessage GetApplyRecord()
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                bool login = redis.IsSet(signature);
                if (!login)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string id = redis.Get<string>(signature);
                List <Apply_record> res = VMDao.GetApplyRecordsBySenderId(id);
                return new Response(1001, "Success", res).Convert();

            }
            catch (Exception ex)
            {
                ErrorLogUtil.WriteLogToFile(ex, Request);
                return Response.Error();
            }
        }
        /// <summary>
        /// 撤销申请记录  
        /// created by xzy
        /// 2019.8.19
        /// </summary>
        /// <param name="recordId">recordId</param>
        /// 用户撤销申请记录
        /// 
        /// <returns>
        ///  return 
        ///  {
        ///     "code":,
        ///     "msg":""
        ///    
        ///  }
        /// </returns>
        ///
        [Route("applyRecord/cancelApplyRecord"),HttpPost]
        public HttpResponseMessage CancelApplyRecord([FromBody]JObject recordId)
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                bool login = redis.IsSet(signature);
                if (!login)
                {
                    return new Response(2001, "未登录账户").Convert();
                }

                var jsonParams = HttpUtil.Deserialize(recordId);
                string userid = redis.Get<string>(signature);
                int recordid = Convert.ToInt32(jsonParams.recordId);
                Apply_record applyrecord = VMDao.GetApplyRecord(recordid);
                if (userid != applyrecord.sender_id)
                {
                    return new Response(2002, "无权限撤销该记录").Convert();
                }
                if (applyrecord.status != 0)
                {
                    return new Response(2003, "该申请已被处理，不允许撤销").Convert();
                }
                if(VMDao.ChangeApplyRecordStatus(recordid, -1,"已撤销") == "success")
                {
                    return new Response(1001, "撤销成功").Convert();
                }
                else
                {
                    throw new Exception("数据库操作异常");
                }
            }
            catch(Exception ex)
            {
                ErrorLogUtil.WriteLogToFile(ex, Request);
                return Response.Error();
            }
        }
        /// <summary>
        /// 拒绝申请 
        /// created by xzy
        /// 2019.8.19
        /// </summary>
        /// <param name="recordId">recordId，reason</param>
        /// 管理员/部门管理员拒绝申请记录
        /// 
        /// <returns>
        ///  return 
        ///  {
        ///     "code":,
        ///     "msg":""
        ///    
        ///  }
        /// </returns>
        ///
        [Route("applyRecord/refuseApplyRecord"), HttpPost]
        public HttpResponseMessage RefuseApplyRecord([FromBody]JObject recordId)
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                bool login = redis.IsSet(signature);
                if (!login)
                {
                    return new Response(2001, "未登录账户").Convert();
                }

                var jsonParams = HttpUtil.Deserialize(recordId);
                string userid = redis.Get<string>(signature);
               
                int recordid = Convert.ToInt32(jsonParams.recordId);
                string reason = jsonParams.reason;
                User user = UserDao.GetUserById(userid);
                if (user.role != 3 &&user.role != 4)
                {
                    return new Response(2002, "非管理员/部门管理员，无法进行此操作").Convert();
                }
                Apply_record applyrecord = VMDao.GetApplyRecord(recordid);
                if(VMDao.ChangeApplyRecordStatus(recordid,1, reason) == "success")
                {
                    return new Response(1001, "拒绝成功").Convert();
                }
                else
                {
                    throw new Exception("数据库操作异常");
                }
            }
            catch (Exception ex)
            {
                ErrorLogUtil.WriteLogToFile(ex, Request);
                return Response.Error();
            }
        }
        /// <summary>
        /// 申请创建自用虚拟机 
        /// created by xzy
        /// 2019.8.20
        /// </summary>
        /// <param name="applyInfo">apply_msg,vmconf,</param>
        /// 用户提交申请创建自用虚拟机
        /// 
        /// <returns>
        ///  return 
        ///  {
        ///     "code":,
        ///     "msg":""
        ///    
        ///  }
        /// </returns>
        ///
        [Route("common/privateVm"),HttpPost]
        public HttpResponseMessage ApplyForPrivateVM([FromBody]JObject applyInfo)
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                bool login = redis.IsSet(signature);
                if (!login)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                var jsonParams = HttpUtil.Deserialize(applyInfo);
                string sender_id = redis.Get<string>(signature);
                //string sender_id = jsonParams.sender_id;
                User user = UserDao.GetUserById(sender_id);
                if (VMDao.GetVMByName(jsonParams.vmconf.Name.ToString()) != null)
                {
                    return new Response(3001, "虚拟机已存在").Convert();
                }
                Apply_record newApply = new Apply_record();
                
                VMConfig vm = new VMConfig();
                QuickCopy.Copy(jsonParams.vmconf, ref vm);
                string json = JsonConvert.SerializeObject(vm);
                newApply.due_time = jsonParams.due_time;
                newApply.detail = json;               
                newApply.vm_name = jsonParams.vmconf.Name;
                newApply.apply_msg = jsonParams.apply_msg;                
                newApply.sender_id = sender_id;
                newApply.operate_type = 1;
                DateTime currentTime = DateTime.Now;
                newApply.apply_time = currentTime.ToString("yyyy/M/d HH:MM:ss");
                newApply.status = 0;

                VMDao.AddApplyRecord(newApply);
                LogUtil.Log(Request, "申请个人虚拟机", sender_id, sender_id, user.role);
                return new Response(1001,"申请成功").Convert();
            }
            catch(Exception ex)
            {
                ErrorLogUtil.WriteLogToFile(ex, Request);
                return Response.Error();
            }


        }
        /// <summary>
        /// 申请创建实验虚拟机
        /// created by xzy
        /// 2019.8.20
        /// </summary>
        /// <param name="applyInfo">expid,apply_msg,vmconf</param>
        /// 教师/助教提交申请创建实验虚拟机
        /// <returns>
        ///  return 
        ///  {
        ///     "code":,
        ///     "msg":""
        ///    
        ///  }
        /// </returns>
        ///
        [Route("applyRecord/ExperimentVm"), HttpPost]
        public HttpResponseMessage ApplyForExperimentVM([FromBody]JObject applyInfo)
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                bool login = redis.IsSet(signature);
                if (!login)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                var jsonParams = HttpUtil.Deserialize(applyInfo);
                string sender_id = redis.Get<string>(signature);
                //string sender_id = jsonParams.sender_id;
                int expid = Convert.ToInt32(jsonParams.expid);
                User user = UserDao.GetUserById(sender_id);

                Experiment exp = ExperimentDao.GetExperimentById(expid);
                Course course = CourseDao.GetCourseInfoById((int)exp.course_id);
                ///权限控制，该课程助教与老师可以访问
                if (CourseDao.GetAssistantsByCourseId(course.id).Where(a => a.student_id == sender_id).Count() == 1 || sender_id == course.teacher_id)
                {
                    Apply_record newApply = new Apply_record();

                    VMConfig vm = new VMConfig();
                    QuickCopy.Copy(jsonParams.vmconf, ref vm);
                    string json = JsonConvert.SerializeObject(vm);
                    newApply.detail = json;
                    
                    //新改的 by cyk
                    foreach(string sid in jsonParams.stulist)
                    {
                        newApply.reply_msg += sid + " ";
                    }
                    //
                    newApply.vm_name = jsonParams.vmconf.Name;
                    newApply.apply_msg = jsonParams.apply_msg;
                    newApply.due_time = jsonParams.due_time;
                    newApply.sender_id = sender_id;
                    newApply.operate_type = 2;
                    DateTime currentTime = DateTime.Now;
                    newApply.apply_time = currentTime.ToString("yyyy/M/d HH:MM:ss");
                    newApply.status = 0;

                    exp.vm_apply_id = VMDao.AddApplyRecord(newApply);
                    exp.vm_name = newApply.vm_name;
                    exp.vm_status = 0;
                    ExperimentDao.ChangeExperimentInfo(exp);

                    LogUtil.Log(Request, "批量申请虚拟机", sender_id, sender_id, user.role);
                    return new Response(1001, "申请成功").Convert();
                }
                else
                {
                    return new Response(2002, "无权限访问该实验相关信息").Convert();
                }
            }
            catch (Exception ex)
            {
                ErrorLogUtil.WriteLogToFile(ex, Request);
                return Response.Error();
            }


        }
        /// <summary>
        /// 修改虚拟机配置
        /// created by xzy
        /// 2019.8.22
        /// </summary>
        /// <param name="applyInfo">apply_msg,vmconf</param>
        /// 用户提交申请，修改虚拟机配置
        /// 
        /// <returns>
        ///  return 
        ///  {
        ///     "code":,
        ///     "msg":""
        ///    
        ///  }
        /// </returns>
        ///
        [Route("applyRecord/changeVMConfig"), HttpPost]
        public HttpResponseMessage ApplyChangeVMConfig([FromBody]JObject applyInfo)
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                bool login = redis.IsSet(signature);
                if (!login)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                var jsonParams = HttpUtil.Deserialize(applyInfo);
                string sender_id = redis.Get<string>(signature);
                string name = jsonParams.vmName;
                int cpuSize = jsonParams.cpu;
                long memorySize = jsonParams.memory;
                long diskSize = jsonParams.disk;
                //string sender_id = jsonParams.sender_id;
                User user = UserDao.GetUserById(sender_id);
                if (VMDao.GetVMByName(name) == null)
                {
                    return new Response(3001, "虚拟机不存在").Convert();
                }
                if (VMDao.GetPermissionForVM(name, sender_id) != 2)
                {
                    return new Response(2002, "无权修改虚拟机配置").Convert();
                }
                Apply_record newApply = new Apply_record();

                Dictionary<string, object> vm = new Dictionary<string, object>();
                vm.Add("Name", name);
                vm.Add("CPU", cpuSize);
                vm.Add("Memory", memorySize);
                vm.Add("Disk", diskSize);
                string json = JsonConvert.SerializeObject(vm);
                newApply.detail = json;

                newApply.sender_id = sender_id;
                newApply.apply_msg = jsonParams.apply_msg;
                newApply.operate_type = 3;
                DateTime currentTime = DateTime.Now;
                newApply.apply_time = currentTime.ToString("yyyy/MM/dd HH:MM:ss");
                newApply.status = 0;
                newApply.vm_name = name;

                VMDao.AddApplyRecord(newApply);
                LogUtil.Log(Request, "修改虚拟机配置", sender_id, sender_id, user.role);
                return new Response(1001, "申请成功").Convert();
            }
            catch (Exception ex)
            {
                ErrorLogUtil.WriteLogToFile(ex, Request);
                return Response.Error();
            }


        }
        [Route("applyRecord/changeDueTime"), HttpPost]
        public HttpResponseMessage ChangeDueTime([FromBody]JObject timeInfo)
        {

            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                bool login = redis.IsSet(signature);
                if (!login)
                {
                    return new Response(2001, "未登录账户").Convert();
                }

                var jsonParams = HttpUtil.Deserialize(timeInfo);
                int recordId = jsonParams.record_id;
                string time = jsonParams.due_time;
                
                string userId = redis.Get<string>(signature);
                User user = UserDao.GetUserById(userId);
                if (user.role < 4)
                {
                    if (user.role == 3)
                    {
                        Apply_record record = VMDao.GetApplyRecord(recordId);
                        User applyUser = UserDao.GetUserById(record.sender_id);
                        if (applyUser.department_id != user.department_id)
                            return new Response(2002, "无权限").Convert();
                    }
                    else
                        return new Response(2002, "无权限").Convert();
                }
                VMDao.ChangeApplyUseTime(recordId, time);
                return new Response(1001, "修改成功").Convert();
            }
            catch(Exception ex)
            {
                ErrorLogUtil.WriteLogToFile(ex, Request);
                return Response.Error();
            }
        }
        /// <summary>
        /// 直接删除虚拟机
        /// </summary>
        /// <param name="vmName">虚拟机名</param>
        /// <returns></returns>
        [Route("vm/deleteVM"),HttpPost]
        public HttpResponseMessage DeleteVM([FromBody]JObject vmName)
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                if(signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string name = HttpUtil.Deserialize(vmName).vmName;
                string id = redis.Get<string>(signature);
                User user = UserDao.GetUserById(id);
                if (VMDao.GetPermissionForVM(name, id) < 2)
                {
                    return new Response(2002, "无权直接删除该虚拟机").Convert();
                }
                else
                {
                    //VirtualMachine vm = VMDao.GetVMByName(name);
                    adminDeleteVM(name,id);
                    LogUtil.Log(Request, "直接删除虚拟机", name, id, user.role);
                    return new Response(1001, "成功执行删除操作").Convert();
                }
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }
        /// <summary>
        /// 直接创建虚拟机
        /// </summary>
        /// <param name="createInfo"></param>
        /// <returns></returns>
        [Route("vm/createVM"),HttpPost]
        public HttpResponseMessage CreateVM([FromBody]JObject createInfo)
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string id = redis.Get<string>(signature);
                User user = UserDao.GetUserById(id);
                if(user.role < 3)
                {
                    return new Response(2002).Convert();
                }
                
                var jsonParams = HttpUtil.Deserialize(createInfo);
                string userId = jsonParams.user_id;
                User vmUser = UserDao.GetUserById(userId);
                if (user.role == 3 && vmUser.department_id != user.department_id)
                    return new Response(2002).Convert();
                VMConfig config = new VMConfig(); 
                QuickCopy.Copy<VMConfig>(HttpUtil.Deserialize(jsonParams.vm_config), ref config);
                if (VMDao.GetVMByName(config.Name) != null)
                {
                    return new Response(1001, "虚拟机已存在").Convert();
                }
                VirtualMachine vm = new VirtualMachine
                {
                    apply_id = 0,
                    owner_id = userId,
                    user_id = userId,
                    course_id = null,
                    vm_name = config.Name,
                    due_time = config.due_time
                };
                createPersonVM(config, vm, userId);
                return new Response(1001).Convert();
            }catch(Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }


        }
        [Route("vm/changeConfig"), HttpPost]
        public HttpResponseMessage ChangeVMConfigAdmin([FromBody]JObject configInfo)
        {
            try
            {
                string managerType = ConfigurationManager.AppSettings["VMManagerType"];
                if (!managerType.Equals("VMware"))
                    return new Response(2002, "无法更改虚拟机配置").Convert();
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                var param = HttpUtil.Deserialize(configInfo);
                string name = param.vmName;
                int CPU = param.cpu;
                long memory = param.memory;
                long disk = param.disk;
                string id = redis.Get<string>(signature);
                User user = UserDao.GetUserById(id);
                if (VMDao.GetPermissionForVM(name, id) < 2)
                {
                    return new Response(2002, "无权更改该虚拟机配置").Convert();
                }
                if(CPU > 4 || memory > 8192 || disk > 204800)
                {
                    if (user.role < 3)
                        return new Response(2002, "无权直接更改该虚拟机配置").Convert();
                }
                VMConfig newConfig = new VMConfig
                {
                    Name = name,
                    CPU = CPU,
                    Memory = memory,
                    Disk = disk
                };
                changeConfig(newConfig, user);
                return new Response(1001, "请求提交成功，正在处理").Convert();
            }catch(Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }
        /// <summary>
        /// 申请删除单个虚拟机 
        /// created by xzy
        /// 2019.8.22
        /// </summary>
        /// <param name="applyInfo">vm_name,apply_msg</param>
        /// 用户提交申请，删除单个虚拟机
        /// 
        /// <returns>
        ///  return 
        ///  {
        ///     "code":,
        ///     "msg":""
        ///    
        ///  }
        /// </returns>
        ///
        [Route("common/deleteSingleVM"), HttpPost]
        public HttpResponseMessage DeleteSingleVM([FromBody]JObject applyInfo)
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                bool login = redis.IsSet(signature);
                if (!login)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                var jsonParams = HttpUtil.Deserialize(applyInfo);
                string sender_id = redis.Get<string>(signature);
                string vm_name = jsonParams.vm_name;
                //string sender_id = jsonParams.sender_id;
                User user = UserDao.GetUserById(sender_id);
                if (VMDao.GetVMByName(vm_name) == null)
                {
                    return new Response(3001, "虚拟机不存在").Convert();
                }
                System.Diagnostics.Debug.WriteLine(VMDao.GetPermissionForVM(vm_name, sender_id));
                if (VMDao.GetPermissionForVM(vm_name, sender_id) != 2)
                {
                    return new Response(2002, "无权删除该虚拟机").Convert();
                }
                Apply_record newApply = new Apply_record();
                
                newApply.sender_id = sender_id;
                newApply.vm_name = vm_name;
                newApply.apply_msg = jsonParams.apply_msg;
                newApply.operate_type = -1;
                DateTime currentTime = DateTime.Now;
                newApply.apply_time = currentTime.ToString("yyyy/M/d HH:MM:ss");
                newApply.status = 0;

                VMDao.AddApplyRecord(newApply);
                LogUtil.Log(Request, "申请删除虚拟机", sender_id, sender_id, user.role);
                return new Response(1001, "申请成功").Convert();
            }
            catch (Exception ex)
            {
                ErrorLogUtil.WriteLogToFile(ex, Request);
                return Response.Error();
            }


        }
        /// <summary>
        /// 申请批量删除虚拟机 
        /// created by xzy
        /// 2019.8.29
        /// </summary>
        /// <param name="applyInfo">vm_name,apply_msg</param>
        /// 
        /// 
        /// <returns>
        ///  return 
        ///  {
        ///     "code":,
        ///     "msg":""
        ///    
        ///  }
        /// </returns>
        ///
        [Route("common/deleteVMList"), HttpPost]
        public HttpResponseMessage DeleteVMList([FromBody]JObject applyInfo)
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                bool login = redis.IsSet(signature);
                if (!login)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                var jsonParams = HttpUtil.Deserialize(applyInfo);
                string sender_id = redis.Get<string>(signature);
                List<string> vmlist = jsonParams.vm_name.ToObject<List<String>>();
                //string sender_id = jsonParams.sender_id;
                
                User user = UserDao.GetUserById(sender_id);
                foreach(string vm in vmlist)
                {
                    if (VMDao.GetVMByName(vm) == null)
                    {
                        return new Response(3001, "虚拟机不存在").Convert();
                    }
                    if (VMDao.GetPermissionForVM(vm, sender_id) != 2)
                    {
                        return new Response(2002, "无权删除该虚拟机").Convert();
                    }
                }
                    
                Apply_record newApply = new Apply_record();               
                newApply.sender_id = sender_id;
                newApply.apply_msg = jsonParams.apply_msg;
                newApply.detail = String.Join(",", vmlist.ToArray());
                newApply.operate_type = -2;
                DateTime currentTime = DateTime.Now;
                newApply.apply_time = currentTime.ToString("yyyy/M/d HH:MM:ss");
                newApply.status = 0;

                VMDao.AddApplyRecord(newApply);
                LogUtil.Log(Request, "批量删除虚拟机", sender_id, sender_id, user.role);
                return new Response(1001, "申请成功").Convert();
            }
            catch (Exception ex)
            {
                ErrorLogUtil.WriteLogToFile(ex, Request);
                return Response.Error();
            }


        }

        /// <summary>
        /// 处理申请 
        /// created by xzy
        /// 2019.8.29
        /// </summary>
        /// <param name="applyRecord">id</param>
        /// 
        /// 
        /// <returns>
        ///  return 
        ///  {
        ///     "code":,
        ///     "msg":""
        ///    
        ///  }
        /// </returns>
        //[Route("applyRecord/accept"), HttpPost]
        //public HttpResponseMessage AcceptApplyRecord([FromBody]JObject applyRecord)
        //{
        //    try
        //    {
        //        string signature = HttpUtil.GetAuthorization(Request);
        //        if (signature == null || !redis.IsSet(signature))
        //        {
        //            return new Response(2001, "未登录账户").Convert();
        //        }
        //        var jsonParams = HttpUtil.Deserialize(applyRecord);
        //        string userid = redis.Get<string>(signature);
        //        int recordId = jsonParams.id;
        //        User user = UserDao.GetUserById(userid);
        //        if (user.role < 3)
        //        {
        //            return new Response(2002, "无权处理").Convert();
        //        }
        //        VMDao.ChangeApplyRecordStatus(recordId, 2, "您的请求已处理");

        //        Apply_record apply = VMDao.GetApplyRecord(recordId);
        //        VMConfig vmconf = new VMConfig();
        //        if (apply.detail != null)
        //        {
        //            JObject jo = (JObject)JsonConvert.DeserializeObject(apply.detail);
        //            QuickCopy.Copy(jo, ref vmconf);
        //        }
        //        switch (apply.operate_type)
        //        {
        //            case 1:
        //                VirtualMachine vm = new VirtualMachine();
        //                vm.apply_id = apply.id;
        //                vm.owner_id = apply.sender_id;
        //                vm.user_id = apply.sender_id;
        //                vm.vm_name = apply.vm_name;
        //                vm.due_time = vmconf.due_time;
        //                createPersonVM(vmconf, vm);
        //                //string ret = VMDao.CreateVM(vmconf, vm);
        //                //if (ret.StartsWith("success") || ret.StartsWith("error :null")){
        //                //    EmailUtil.SendEmail("创建虚拟机成功", "您的个人虚拟机:" + vm.vm_name + "已完成创建，请前往云平台查看",apply.sender_id);
        //                //    break;
        //                //}
        //                //else
        //                //{
        //                //    throw new Exception(ret);
        //                //}
        //                break;

        //            case 2:
        //                Experiment exp = ExperimentDao.GetExperimentByApplyId(recordId);
        //                exp.vm_status = 1;
        //                ExperimentDao.ChangeExperimentInfo(exp);
        //                //string expret= VMDao.CreateVMsForExp(vmconf, recordId);
        //                //if (expret.StartsWith("success") || expret.StartsWith("error :null"))
        //                //{
        //                //    EmailUtil.SendEmail("创建实验虚拟机成功", "您的实验:" +exp.name+ "批量创建虚拟机已完成，请前往云平台查看", apply.sender_id);
        //                //    exp.vm_status = 2;
        //                //    ExperimentDao.ChangeExperimentInfo(exp);
        //                //    break;
        //                //}
        //                //else
        //                //{
        //                //    exp.vm_status = -2;
        //                //    ExperimentDao.ChangeExperimentInfo(exp);
        //                //    throw new Exception(expret);
        //                //}
        //                createVMsForExp(vmconf, recordId, userid);
        //                break;

        //            case 3:
        //                dynamic vmConfig = JsonConvert.DeserializeObject(apply.detail);
        //                VMConfig newConfig = new VMConfig
        //                {
        //                    Name = vmConfig.Name,
        //                    CPU = vmConfig.CPU,
        //                    Memory = vmConfig.Memory,
        //                    Disk = vmConfig.Disk
        //                };
        //                changeConfig(newConfig, user, apply);
        //                break;
        //            case -1:
        //                deletePerVM(apply.vm_name, userid, apply);
        //                break;
        //            //if (delret.StartsWith("success") || delret.StartsWith("error :null"))
        //            //{
        //            //    EmailUtil.SendEmail("删除个人虚拟机成功", "您的虚拟机:" + apply.vm_name + "已删除", apply.sender_id);
        //            //    //VMDao.DeleteVMInDB(apply.vm_name);
        //            //    break;
        //            //}
        //            //else
        //            //{
        //            //    throw new Exception(delret);
        //            //}
        //            case -2:
        //                string[] list = apply.vm_name.Split(',');
        //                deleteVMs(list, userid, apply);
        //                //VMDao.DeleteVMInDB(list);
        //                EmailUtil.SendEmail("批量删除虚拟机", "您申请删除的虚拟机已删除。", apply.sender_id);
        //                break;

        //            default:
        //                return new Response(2003, "申请记录无效").Convert();
        //        }
        //        return new Response(1001, "处理完毕").Convert();
        //    }

        //    catch (Exception ex)
        //    {

        //        ErrorLogUtil.WriteLogToFile(ex, Request);
        //        return Response.Error();
        //    }

        //}
        [Route("applyRecord/accept"), HttpPost]
        public HttpResponseMessage AcceptApplyRecord([FromBody]JObject applyRecord)
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                var jsonParams = HttpUtil.Deserialize(applyRecord);
                string userid = redis.Get<string>(signature); 
                int recordId = jsonParams.id;
                User user = UserDao.GetUserById(userid);
                if (user.role <3)
                {
                    return new Response(2002, "无权处理").Convert();
                }              
                //VMDao.ChangeApplyRecordStatus(recordId, 2, "您的请求已处理");

                Apply_record apply = VMDao.GetApplyRecord(recordId);
                VMConfig vmconf = new VMConfig();
                if(apply.detail != null)
                {
                    JObject jo = (JObject)JsonConvert.DeserializeObject(apply.detail);
                    QuickCopy.Copy(jo, ref vmconf);
                }
                switch (apply.operate_type)
                {
                    case 1:
                        VirtualMachine vm = new VirtualMachine();
                        vm.apply_id = apply.id;
                        vm.owner_id = apply.sender_id;
                        vm.user_id = apply.sender_id;
                        vm.vm_name = apply.vm_name;
                        vm.due_time = vmconf.due_time;
                        createPersonVM(vmconf, vm);
                        //string ret = VMDao.CreateVM(vmconf, vm);
                        //if (ret.StartsWith("success") || ret.StartsWith("error :null")){
                        //    EmailUtil.SendEmail("创建虚拟机成功", "您的个人虚拟机:" + vm.vm_name + "已完成创建，请前往云平台查看",apply.sender_id);
                        //    break;
                        //}
                        //else
                        //{
                        //    throw new Exception(ret);
                        //}
                        break;
                        
                    case 2:
                        Experiment exp = ExperimentDao.GetExperimentByApplyId(recordId);
                        exp.vm_status = 1;
                        ExperimentDao.ChangeExperimentInfo(exp);
                        //string expret= VMDao.CreateVMsForExp(vmconf, recordId);
                        //if (expret.StartsWith("success") || expret.StartsWith("error :null"))
                        //{
                        //    EmailUtil.SendEmail("创建实验虚拟机成功", "您的实验:" +exp.name+ "批量创建虚拟机已完成，请前往云平台查看", apply.sender_id);
                        //    exp.vm_status = 2;
                        //    ExperimentDao.ChangeExperimentInfo(exp);
                        //    break;
                        //}
                        //else
                        //{
                        //    exp.vm_status = -2;
                        //    ExperimentDao.ChangeExperimentInfo(exp);
                        //    throw new Exception(expret);
                        //}
                        createVMsForExp(vmconf, recordId, userid,apply.reply_msg);
                        break;

                    case 3:
                        dynamic vmConfig = JsonConvert.DeserializeObject(apply.detail);
                        VMConfig newConfig = new VMConfig
                        {
                            Name = vmConfig.Name,
                            CPU = vmConfig.CPU,
                            Memory = vmConfig.Memory,
                            Disk = vmConfig.Disk
                        };
                        changeConfig(newConfig, user, apply);
                        break;
                    case -1:
                        deletePerVM(apply.vm_name, userid, apply);
                        break;
                    case -2:
                        string[] list = apply.vm_name.Split(',');
                        deleteVMs(list,userid,apply);
                        VMDao.DeleteVMInDB(list);
                        EmailUtil.SendEmail("批量删除虚拟机", "您申请删除的虚拟机已删除。", apply.sender_id);
                        break; 
                       
                    default:
                        VMDao.ChangeApplyRecordStatus(recordId, 2, "您的请求已处理");
                        return new Response(2003, "申请记录无效").Convert();
                }
                return new Response(1001, "处理完毕").Convert();
            }

            catch (Exception ex)
            {

                ErrorLogUtil.WriteLogToFile(ex, Request);
                return Response.Error();
            }

        }
        /// 
        /// <summary>
        /// 获取主机列表
        /// </summary>
        /// <returns>主机信息列表</returns>
        [Route("vm/getHosts"),HttpGet]
        public HttpResponseMessage GetHostInfo()
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                bool login = redis.IsSet(signature);
                if (!login)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string id = redis.Get<string>(signature);
                User user = UserDao.GetUserById(id);
                if (user.role != 4)
                {
                    return new Response(2002).Convert();
                }
                dynamic ws = getManager();
                return new Response(1001, "成功", ws.GetHosts()).Convert();
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }
        /// <summary>
        /// 获取某个虚拟机的快照
        /// </summary>
        /// <returns></returns>
        [Route("vm/getSnapshots"), HttpGet]
        public HttpResponseMessage GetSnapShots()
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                bool login = redis.IsSet(signature);
                if (!login)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string id = redis.Get<string>(signature);
                User user = UserDao.GetUserById(id);
                if(user.role < 2)
                {
                    return new Response(2002).Convert();
                }
                var jsonParams = Request.GetQueryNameValuePairs().ToDictionary(k => k.Key, v => v.Value);
                string vmName = jsonParams["vmName"];
                if(VMDao.GetPermissionForVM(vmName, id) < 2)
                {
                    return new Response(2002).Convert();
                }
                dynamic ws = getManager();
                return new Response(1001, "成功", ws.GetSnapshot(vmName)).Convert();
            }
            catch(Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }
        /// <summary>
        /// 移除指定快照
        /// </summary>
        /// <param name="snapshot">vmName snapshotName</param>
        /// <returns></returns>
        [Route("vm/removeSnapshot"),HttpPost]
        public HttpResponseMessage RemoveSnapshot([FromBody]JObject snapshot)
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                bool login = redis.IsSet(signature);
                if (!login)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string id = redis.Get<string>(signature);
                User user = UserDao.GetUserById(id);
                if (user.role < 2)
                {
                    return new Response(2002).Convert();
                }
                var jsonParams = HttpUtil.Deserialize(snapshot);
                string vmName = jsonParams.vmName;
                string snapshotName = jsonParams.snapshotName;
                if (VMDao.GetPermissionForVM(vmName, id) < 2)
                {
                    return new Response(2002).Convert();
                }
                dynamic ws = getManager();
                List<Snapshot> snapshots = ws.GetSnapshot(vmName);
                if( snapshots != null && snapshots.Where(s => s.Name.Equals(snapshotName)).Count()<1)
                {
                    return new Response(3001, "快照不存在").Convert();
                }
                string ret = ws.RemoveSnapshot(vmName, snapshotName);
                LogUtil.Log(Request, "删除快照", vmName + ": " + snapshotName, id, user.role);
                if (ret.StartsWith("success") || ret.StartsWith("error :null"))
                {
                    return new Response(1001,"success",ret).Convert();
                }
                else
                {
                    throw new Exception(ret);
                }
            }
            catch(Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }
        /// <summary>
        /// 创建快照
        /// </summary>
        /// <param name="snapshot">vmName snapshotName description</param>
        /// <returns></returns>
        [Route("vm/createSnapshot"), HttpPost]
        public HttpResponseMessage CreateSnapshot([FromBody]JObject snapshot)
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                bool login = redis.IsSet(signature);
                if (!login)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string id = redis.Get<string>(signature);
                User user = UserDao.GetUserById(id);
                if (user.role < 2)
                {
                    return new Response(2002).Convert();
                }
                var jsonParams = HttpUtil.Deserialize(snapshot);
                string vmName = jsonParams.vmName;
                string snapshotName = jsonParams.snapshotName;
                string description = jsonParams.description;
                if (VMDao.GetPermissionForVM(vmName, id) < 2)
                {
                    return new Response(2002).Convert();
                }
                dynamic ws = getManager();
                List<Snapshot> snapshots = ws.GetSnapshot(vmName);
                if (snapshots != null && snapshots.Count() > 2)
                {
                    return new Response(3001, "一台虚拟机最多创建3个快照").Convert();
                }
                string ret = ws.CreateSnapshot(vmName, snapshotName, description);
                LogUtil.Log(Request, "创建快照", vmName + ": " + snapshotName, id, user.role);
                if (ret.StartsWith("success") || ret.StartsWith("error :null"))
                {
                    return new Response(1001).Convert();
                }
                else
                {
                    throw new Exception(ret);
                }
            }catch(Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
         }
        /// <summary>
        /// 恢复快照
        /// </summary>
        /// <param name="snapshot">vmName snapshotName</param>
        /// <returns></returns>
        [Route("vm/revertSnapshot"),HttpPost]
        public HttpResponseMessage RevertSnapshot([FromBody]JObject snapshot)
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                bool login = redis.IsSet(signature);
                if (!login)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string id = redis.Get<string>(signature);
                User user = UserDao.GetUserById(id);
                if (user.role < 2)
                {
                    return new Response(2002).Convert();
                }
                var jsonParams = HttpUtil.Deserialize(snapshot);
                string vmName = jsonParams.vmName;
                string snapshotName = jsonParams.snapshotName;
                if (VMDao.GetPermissionForVM(vmName, id) < 2)
                {
                    return new Response(2002).Convert();
                }
                dynamic ws = getManager();
                List<Snapshot> snapshots = ws.GetSnapshot(vmName);
                if (snapshots != null && snapshots.Where(s => s.Name.Equals(snapshotName)).Count() < 1)
                {
                    return new Response(3001, "快照不存在").Convert();
                }
                string ret = ws.RevertSnapshot(vmName, snapshotName);
                LogUtil.Log(Request, "恢复快照", vmName + ": " + snapshotName, id, user.role);
                if (ret.StartsWith("success") || ret.StartsWith("error :null"))
                {
                    return new Response(1001).Convert();
                }
                else
                {
                    throw new Exception(ret);
                }
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }

        [Route("common/addTemplate"), HttpPost]
        public HttpResponseMessage AddTemplate([FromBody]JObject vmConfig)
        {
            try
            {
                var jsonParams = HttpUtil.Deserialize(vmConfig);
                SangforInfo info = new SangforInfo();
                info.Name = jsonParams.Name;
                info.image_disk = jsonParams.Disk;
                info.id = jsonParams.Id;
                info.IsTemplate = true;
                info.is_exp = false;
                info.is_exps = "False";
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                bool login = redis.IsSet(signature);
                if (!login)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string id = redis.Get<string>(signature);
                User user = UserDao.GetUserById(id);

                if (user.role == 2)
                {
                    info.student_id = id;
                    info.teacher_id = id;
                }
                else if (user.role == 3 || user.role == 4)
                {
                    info.student_id = "all";
                    info.teacher_id = "all";
                }
                else
                {
                    return new Response(2002).Convert();
                }
                SangforDao.Add(info);
                return new Response(1001).Convert();
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }

        /// <summary>
        /// 模板转化
        /// created by jyf
        /// 2019.8.31
        /// </summary>
        /// <param name="vmConfig">vmconfig</param>
        /// 用户提交申请，将特定虚拟机转化为模板
        /// 
        /// <returns>
        ///  return 
        ///  {
        ///     "code":,
        ///     "msg":""
        ///    
        ///  }
        /// </returns>
        ///
        [Route("common/makeTemplate"), HttpPost]
        public HttpResponseMessage MakeTemplate([FromBody]JObject vmConfig)
        {
            try
            {
                var jsonParams = HttpUtil.Deserialize(vmConfig);
                string vmName = jsonParams.vmName;
                
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                bool login = redis.IsSet(signature);
                if (!login)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string id = redis.Get<string>(signature);
                
                //string id = "15523523";
                User user = UserDao.GetUserById(id);
                if (user.role < 2)
                {
                    return new Response(2002).Convert();
                }

                if (VMDao.GetPermissionForVM(vmName, id) < 2)//一个人只能将自己能控制的vm设置为template
                {
                    return new Response(2002).Convert();
                }

                List<string> confs = new List<string>();
                confs.Add(vmName);
                List<VMConfig> vms = VMDao.GetMultiVMs(confs);

                if (vms.Count() == 0)
                {
                    return new Response(2002,"没有该虚拟机！").Convert();
                }
                VMConfig config = vms.First();
                if(config.IsTemplate == true)
                {
                    return new Response(2002,"该vm已经是模板").Convert();
                }
                if(config.Status.PowerState == "poweredOn")
                {
                    return new Response(2002, "该虚拟机需要预先关机").Convert();
                }

                int res = VMDao.ChangeVMTemplate(vmName, 1);
                LogUtil.Log(Request, "标记为模板", vmName, id, user.role);
                if (res != 0)
                {
                    return new Response(1001,"转化失败，指定虚拟机可能处于不可修改的状态").Convert();
                }
                return new Response(1001).Convert();
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }

        /// <summary>
        /// 模板转化
        /// created by jyf
        /// 2019.8.31
        /// </summary>
        /// <param name="vmConfig">vmconfig</param>
        /// 用户提交申请，将特定的模板取消
        /// 
        /// <returns>
        ///  return 
        ///  {
        ///     "code":,
        ///     "msg":""
        ///    
        ///  }
        /// </returns>
        ///
        [Route("vm/transferTemplate"), HttpPost]
        public HttpResponseMessage RemoveTemplate([FromBody]JObject vmConfig)
        {
            try
            {
                var jsonParams = HttpUtil.Deserialize(vmConfig);
                string vmName = jsonParams.templateName;

                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                bool login = redis.IsSet(signature);
                if (!login)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string id = redis.Get<string>(signature);
                User user = UserDao.GetUserById(id);
                if (user.role < 2)
                {
                    return new Response(2002).Convert();
                }

                if (VMDao.GetPermissionForVM(vmName, id) < 2)//一个人只能将自己能控制的vm设置为template
                {
                    return new Response(2002).Convert();
                }

                List<string> confs = new List<string>();
                confs.Add(vmName);
                List<VMConfig> vms = VMDao.GetMultiVMs(confs);

                if (vms.Count() == 0)
                {
                    return new Response(2002, "没有该虚拟机！").Convert();
                }

                int res = VMDao.ChangeVMTemplate(vmName, 0);
                LogUtil.Log(Request, "标记为虚拟机", vmName, id, user.role);
                if (res != 0)
                {
                    return new Response(1001, "转化失败").Convert();
                }
                return new Response(1001).Convert();
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }

        /// <summary>
        /// 查询正在用的虚拟机列表
        /// created by zzw
        /// 2019.9.4
        /// </summary>
        /// <param></param>
        /// 
        /// <returns>
        ///  return 
        ///  {
        ///     "code":"",
        ///     "msg":"",
        ///     "data": [
        ///    {
        ///        "Name": "person",
        ///        "CPU": 2,
        ///        "Memory": 2048,
        ///       "Disk": 102400,
        ///      "IsTemplate": false,
        ///        "GuestFullName": "CentOS 4/5/6/7 (64-bit)",
        ///        "TemplateName": null,
        ///        "AdvancedConfig": "",
        ///        "Status": {
        ///            "IPAddress": "10.251.254.127",
        ///            "HostName": "10.251.254.12",
        ///            "PowerState": "poweredOn",
        ///            "RunTimeState": "green"
        ///        }
        ///    }
        ///]
        ///    
        ///  }
        /// </returns>
        ///
        [Route("getUsingVMsById"), HttpGet]
        public HttpResponseMessage getUsingVMsById()
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                bool login = redis.IsSet(signature);
                if (!login)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string id = redis.Get<string>(signature);
                //List<VirtualMachine> vmlist = VMDao.GetUsingVMs(id);
                //User user = UserDao.GetUserById(id);
                //if (user.role == 1)
                //    vmlist = vmlist.Where(v => v.course_id != null).ToList();
                List<VMConfig> vMConfigs = VMDao.GetUsingVMs(id); //VMDao.GetVMInfo(vmlist);
                return new Response(1001, "获取成功", vMConfigs).Convert();
            }
            catch (Exception ex)
            {
                ErrorLogUtil.WriteLogToFile(ex, Request);
                return Response.Error();
            }
        }
        /// <summary>
        /// 查询个人虚拟机列表
        /// created by zzw
        /// 2019.9.27
        /// </summary>
        /// <param></param>
        /// 
        /// <returns>
        ///  return 
        ///  {
        ///     "code":"",
        ///     "msg":"",
        ///     "data": [
        ///    {
        ///        "Name": "person",
        ///        "CPU": 2,
        ///        "Memory": 2048,
        ///       "Disk": 102400,
        ///      "IsTemplate": false,
        ///        "GuestFullName": "CentOS 4/5/6/7 (64-bit)",
        ///        "TemplateName": null,
        ///        "AdvancedConfig": "",
        ///        "Status": {
        ///            "IPAddress": "10.251.254.127",
        ///            "HostName": "10.251.254.12",
        ///            "PowerState": "poweredOn",
        ///            "RunTimeState": "green"
        ///        }
        ///    }
        ///]
        ///    
        ///  }
        /// </returns>
        ///
        [Route("getPerVMsByUserId"), HttpGet]
        public HttpResponseMessage getPerVMsByUserId()
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                bool login = redis.IsSet(signature);
                if (!login)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string id = redis.Get<string>(signature);
                List<VMConfig> vMConfigs = VMDao.GetPersonVMsByUserId(id);
                if(vMConfigs != null)
                {
                    vMConfigs = vMConfigs.Where(v => v.IsTemplate == false).ToList();
                }
                return new Response(1001, "申请成功", vMConfigs).Convert();
            }
            catch (Exception ex)
            {
                ErrorLogUtil.WriteLogToFile(ex, Request);
                return Response.Error();
            }
        }
        /// <summary>
        /// 获取模板列表by用户id by zzw
        /// 2019.9.24
        /// </summary>
        /// <returns>
        /// {
        ///     "code":,
        ///     "msg":"",
        ///     "data":{
        ///     [
        ///         List vmconfig
        ///         "Name": "CentOS7-template",
        ///         "CPU": 2,
        ///         "Memory": 4096,
        ///         "Disk": 51200,
        ///         "IsTemplate": true,
        ///         "GuestFullName": "CentOS 4/5/6/7 (64-bit)",
        ///         "TemplateName": null,
        ///         "AdvancedConfig": "",
        ///         "Status": {
        ///         "IPAddress": "Unavailable",
        ///         "HostName": "10.251.254.9",
        ///         "PowerState": "poweredOff",
        ///         "RunTimeState": "green"
        ///         }
        ///     }
        ///     ]
        /// }
        /// </returns>
        /// 
        [Route("vm/getTemplatesByUserId"), HttpGet]
        public HttpResponseMessage GetTemplatesByUserId()
        {
            //try
            //{
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                bool isLogin = redis.IsSet(signature);
                if (!isLogin)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string targetId = redis.Get<string>(signature);
                List<VMConfig> templates = VMDao.GetTemplates(targetId);
                
                if (templates.Count == 0) return new Response(1001, "该用户没有模板").Convert();
                return new Response(1001, "获取成功", templates).Convert();
            //}
            //catch (Exception e)
            //{
            //    ErrorLogUtil.WriteLogToFile(e, Request);
            //    return Response.Error();
            //}
        }
        [Route("getTemplatesByUserId"),HttpGet]
        public HttpResponseMessage GetTemplatesByUserIdTemp()
        {
            return GetTemplatesByUserId();
        }
        /// <summary>
        /// 获取个人模板，不包括公共模板
        /// </summary>
        /// <returns></returns>
        [Route("vm/getPersonalTemplates"), HttpGet]
        public HttpResponseMessage GetPersonalTemplates()
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                bool isLogin = redis.IsSet(signature);
                if (!isLogin)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string targetId = redis.Get<string>(signature);
                List<VMConfig> templates = VMDao.GetTemplates(targetId, true);
                if (templates.Count == 0) return new Response(1001, "该用户没有模板").Convert();
                return new Response(1001, "获取成功", templates).Convert();
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }

        }
        /// <summary>
        /// 查询实验虚拟机列表by实验id
        /// created by zzw
        /// 2019.9.4
        /// </summary>
        /// <param name="expInfo">expId</param>
        /// 
        /// <returns>
        ///  return 
        ///  {
        ///     "code":,
        ///     "msg":"",
        ///     "data":"",
        ///    
        ///  }
        /// </returns>
        ///
        [Route("getExpVMsByExpId"), HttpGet]
        public HttpResponseMessage GetExpVMsByExpId([FromBody]JObject expInfo)
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                var jsonParams = Request.GetQueryNameValuePairs().ToDictionary(k => k.Key, v => v.Value);
                int expid = Convert.ToInt32(jsonParams["expId"]);
                bool login = redis.IsSet(signature);
                if (!login)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string id = redis.Get<string>(signature);
                User user = UserDao.GetUserById(id);
                Experiment experiment = ExperimentDao.GetExperimentById(expid);
                if ((user.role == 2 && CourseDao.GetCourseInfoById((int)experiment.course_id).teacher_id == user.id) || (user.role == 1 && CourseDao.GetAssistantsByCourseId((int)experiment.course_id).Where(a => a.student_id == user.id).Count() != 0))
                {
                    
                    List<VMConfig> vmlist= VMDao.GetAllExpVirtualMachine();
                    
                    List<VirtualMachine> virtualMachines = DataUtil.Transform(vmlist);

                    if (experiment.vm_name == null) 
                        return new Response(1001, "查询成功", null).Convert();
                    List<VMConfig> reslist = vmlist.Where(v => v.Name.StartsWith(experiment.vm_name + "_")).ToList();
                    System.Diagnostics.Debug.WriteLine(reslist.Count());
                    var query = reslist.Join(virtualMachines, conf => conf.Name, vm => vm.vm_name, (conf, vm) => new
                    {
                        
                        vm.owner_id,
                        vm.user_id,
                        vm.vm_name,
                        conf.AdvancedConfig,
                        conf.CPU,
                        conf.Disk,
                        conf.IsTemplate,
                        conf.Memory,
                        conf.GuestFullName,
                        conf.Status,
                        conf.TemplateName,
                        conf.console_url
                    });
                    return new Response(1001, "查询成功", query).Convert();
                }
                else
                {
                    return new Response(2002, "无权限查询信息").Convert();
                }

            }
            catch (Exception ex)
            {
                ErrorLogUtil.WriteLogToFile(ex, Request);
                return Response.Error();
            }
        }
        /// <summary>
        /// 查询指定id虚拟机列表
        /// created by xzy
        /// 2019.10.5
        /// </summary>
        /// <param></param>
        /// 
        /// <returns>
        ///  return 
        ///  {
        ///     "code":"",
        ///     "msg":"",
        ///     "data": [
        ///    {
        ///        "Name": "person",
        ///        "CPU": 2,
        ///        "Memory": 2048,
        ///       "Disk": 102400,
        ///      "IsTemplate": false,
        ///        "GuestFullName": "CentOS 4/5/6/7 (64-bit)",
        ///        "TemplateName": null,
        ///        "AdvancedConfig": "",
        ///        "Status": {
        ///            "IPAddress": "10.251.254.127",
        ///            "HostName": "10.251.254.12",
        ///            "PowerState": "poweredOn",
        ///            "RunTimeState": "green"
        ///        }
        ///    }
        ///]
        ///    
        ///  }
        /// </returns>
        ///
        [Route("getVMsByUserId"), HttpGet]
        public HttpResponseMessage getVMsByUserId()
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                bool login = redis.IsSet(signature);
                if (!login)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string id = redis.Get<string>(signature);
                //List<VirtualMachine> VMList= VMDao.GetOwningVMs(id);              
                List<VMConfig> vmconf =VMDao.GetOwningVMs(id);  //VMDao.GetVMInfo(VMList);              
                return new Response(1001, "获取成功", vmconf).Convert();
                
            }
            catch (Exception ex)
            {
                ErrorLogUtil.WriteLogToFile(ex, Request);
                return Response.Error();
            }
        }
        
        /// <summary>
        /// 创建实验虚拟机 -by zzw
        /// </summary>
        /// <param name="vm">stuId、expId</param>
        /// <returns></returns>
        [Route("virtualMachine/addVMForExp"), HttpPost]
        public HttpResponseMessage AddVMForExp([FromBody]JObject vm)
        {
            try
            {
                var jsonParams = HttpUtil.Deserialize(vm);
                Apply_record newApply = new Apply_record();
                VMConfig vm1 = new VMConfig();
                QuickCopy.Copy(jsonParams.vmconf, ref vm);
                string json = JsonConvert.SerializeObject(vm);
                newApply.detail = json;


                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                bool login = redis.IsSet(signature);
                if (!login)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string targetId = redis.Get<string>(signature);
                User user = UserDao.GetUserById(targetId);
                

                int expId = jsonParams.expId;
                Experiment experiment = ExperimentDao.GetExperimentById(expId);
                Course course = CourseDao.GetCourseInfoById((int)(experiment.course_id));
                int recordId;
                if (experiment.vm_apply_id != null)
                {
                    recordId = (int)ExperimentDao.GetExperimentById(expId).vm_apply_id;
                }
                else
                {
                    //没有applyId说明没有批量创建过、此时不应该有单独创建的权限
                    return new Response(2002, "没有权限创建实验虚拟机").Convert();
                }
                if ((user.role == 2 && course.teacher_id == user.id) || (user.role == 1 && CourseDao.GetAssistantsByCourseId((int)experiment.course_id).Where(a => a.student_id == user.id).Count() != 0))
                {
                    //if (CourseDao.GetMapByCourseId((int)experiment.course_id).Where(m => m.student_id == jsonParams.stuId).Count() == 0)
                    //{
                    //    return new Response(2002, "该课程没有这个学生").Convert();
                    //}
                    VirtualMachine vminfo = new VirtualMachine();
                    //Apply_record apply = VMDao.GetApplyRecord(recordId);
                    VMConfig vmconf = new VMConfig();
                    //JObject jo = (JObject)JsonConvert.DeserializeObject(newApply.detail);
                    //QuickCopy.Copy(jo, ref vmconf);
                    vminfo.apply_id = recordId;
                    vminfo.course_id = experiment.course_id;
                    vminfo.owner_id = course.teacher_id;
                    vminfo.due_time = jsonParams.vmconf.due_time;
                    vmconf.CPU = jsonParams.vmconf.CPU;
                    vmconf.TemplateName = jsonParams.vmconf.TemplateName;
                    vmconf.Memory = jsonParams.vmconf.Memory;
                    vmconf.Disk = jsonParams.vmconf.Disk;
                    vmconf.IsTemplate = false;

                    string stulist = "";
                    foreach (var sid in jsonParams.stulist)
                    {
                        stulist += sid;
                        stulist += " ";
                    }

                    Task<string> task = Task<string>.Run(() =>
                    {
                        return VMDao.CreateVMsForExp(vmconf, recordId, stulist);
                    });
                    
                    return new Response(1001, "操作完成,请等待虚拟机创建成功，这可能会需要一些时间").Convert();
                }
                else
                {
                    return new Response(2002, "没有权限创建实验虚拟机").Convert();
                }
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return new Response(4001).Convert();
            }
        }

        /// <summary>
        /// 删除实验虚拟机
        /// </summary>
        /// <param name="expInfo"></param>
        /// <returns></returns>
        [Route("virtualMachine/deleteVMForExp"), HttpPost]
        public HttpResponseMessage DeleteExpVm([FromBody] JObject expInfo)
        {

            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                var jsonParams = HttpUtil.Deserialize(expInfo);
                int expId = jsonParams.expId;
                Experiment exp = ExperimentDao.GetExperimentById(expId);
                if (exp == null || exp.vm_apply_id == null)
                    return new Response(3001, "实验虚拟机不存在").Convert();
                List<string> vmList = VMDao.GetVMsByVmName(exp.vm_name).Select(c => c.Name).ToList();
                Task<int> task = Task<int>.Run(() =>
                {
                    if (VMDao.DeleteVM(vmList.ToArray()) == 1)
                        VMDao.ChangeApplyRecordStatus((int)exp.vm_apply_id, -1, "");
                    return 1;
                });
                return new Response(1001).Convert();
            }
            catch (Exception ex)
            {
                ErrorLogUtil.WriteLogToFile(ex, Request);
                return Response.Error();
            }
        }
        
        //public HttpResponseMessage AddVMForExp([FromBody]JObject vm)
        //{
        //    try
        //    {
        //        string signature = HttpUtil.GetAuthorization(Request);
        //        if (signature == null || !redis.IsSet(signature))
        //        {
        //            return new Response(2001, "未登录账户").Convert();
        //        }
        //        bool login = redis.IsSet(signature);
        //        if (!login)
        //        {
        //            return new Response(2001, "未登录账户").Convert();
        //        }
        //        string targetId = redis.Get<string>(signature);
        //        User user = UserDao.GetUserById(targetId);
        //        var jsonParams = HttpUtil.Deserialize(vm);
        //        int expId = jsonParams.expId;
        //        Experiment experiment = ExperimentDao.GetExperimentById(expId);
        //        Course course = CourseDao.GetCourseInfoById((int)(experiment.course_id));
        //        int recordId;
        //        if (experiment.vm_apply_id != null)
        //        {
        //            recordId = (int)ExperimentDao.GetExperimentById(expId).vm_apply_id;
        //        }
        //        else
        //        {
        //            //没有applyId说明没有批量创建过、此时不应该有单独创建的权限
        //            return new Response(2002, "没有权限创建实验虚拟机").Convert();
        //        }
        //        if ((user.role == 2 && course.teacher_id == user.id) || (user.role == 1 && CourseDao.GetAssistantsByCourseId((int)experiment.course_id).Where(a => a.student_id == user.id).Count() != 0))
        //        {
        //            if (CourseDao.GetMapByCourseId((int)experiment.course_id).Where(m => m.student_id == jsonParams.stuId).Count() == 0)
        //            {
        //                return new Response(2002, "该课程没有这个学生").Convert();
        //            }
        //            VirtualMachine vminfo = new VirtualMachine();
        //            Apply_record apply = VMDao.GetApplyRecord(recordId);
        //            VMConfig vmconf = new VMConfig();
        //            JObject jo = (JObject)JsonConvert.DeserializeObject(apply.detail);
        //            QuickCopy.Copy(jo, ref vmconf);
        //            vminfo.apply_id = recordId;
        //            vminfo.course_id = experiment.course_id;
        //            vminfo.owner_id = course.teacher_id;
        //            vminfo.user_id = jsonParams.stuId;
        //            vminfo.vm_name = experiment.vm_name + "_" + vminfo.user_id;

        //            LogUtil.Log(Request, "新建单个实验虚拟机", vminfo.user_id, user.id, user.role, "running");
        //            createExpVM(vmconf, vminfo);
        //            return new Response(1001, "操作完成").Convert();
        //        }
        //        else
        //        {
        //            return new Response(2002, "没有权限创建实验虚拟机").Convert();
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        ErrorLogUtil.WriteLogToFile(e, Request);
        //        return new Response(4001).Convert();
        //    }
        //}



        /// <summary>
        /// 创建一个个人虚拟机 函数 -by zzw
        /// </summary>
        public async void createPersonVM(VMConfig vm , VirtualMachine vminfo, string sendTo = "")
        {
            string res = "";
            await Task.Run(() =>
            {
                res = VMDao.CreateVM(vm, vminfo);
                if (res.Contains("success"))
                {
                    VMDao.ChangeApplyRecordStatus((int)vminfo.apply_id, 2, "创建成功");
                    VMDao.AddVMInDB(vminfo);
                    EmailUtil.SendEmail("创建虚拟机成功", "您的个人虚拟机:" + vminfo.vm_name + "已完成创建，请前往云平台查看", sendTo.Equals("") ? VMDao.GetApplyRecord((int)vminfo.apply_id).sender_id : sendTo);

                    LogUtil.Log(Request, "新建个人虚拟机", vminfo.vm_name, vminfo.user_id, UserDao.GetUserById(vminfo.user_id).role, "success", res);
                }
                else
                {
                    LogUtil.Log(Request, "新建个人虚拟机", vminfo.vm_name, vminfo.user_id, UserDao.GetUserById(vminfo.user_id).role, "fail", res);
                }
            });
        }
        /// <summary>
        /// 创建一个实验虚拟机 函数 -by zzw
        /// </summary>
        public async void createExpVM(VMConfig vm, VirtualMachine vminfo)
        {
            string res = "";
            await Task.Run(() =>
            {
                res = VMDao.CreateVM(vm, vminfo);
                if (res.Contains("success"))
                {
                    LogUtil.Log(Request, "新建单个实验虚拟机", vminfo.vm_name, vminfo.owner_id, UserDao.GetUserById(vminfo.owner_id).role, "success", res);
                }
                else
                {
                    LogUtil.Log(Request, "新建单个实验虚拟机", vminfo.vm_name, vminfo.owner_id, UserDao.GetUserById(vminfo.owner_id).role, "fail", res);
                }
            });
        }
        /// <summary>
        /// 删除一个实验虚拟机 函数 -by zzw
        /// </summary>
        public async void deleteExpVM(string vmname,string operatorid)
        {
            string res = "";
            await Task.Run(() =>
            {
                res = VMDao.DeleteVM(vmname);
                if (res.Contains("success"))
                {
                    LogUtil.Log(Request, "删除单个实验虚拟机", vmname, operatorid, UserDao.GetUserById(operatorid).role, "success", res);
                }
                else
                {
                    LogUtil.Log(Request, "删除单个实验虚拟机", vmname, operatorid, UserDao.GetUserById(operatorid).role, "fail", res);
                }
            });
        }
        /// <summary>
        /// 删除一个个人虚拟机 函数 -by zzw
        /// </summary>
        public async void deletePerVM(string vmname, string operatorid,Apply_record apply)
        {
            string res = "";
            await Task.Run(() =>
            {
                res = VMDao.DeleteVM(vmname);
                if (res.Contains("success"))
                {
                    VMDao.ChangeApplyRecordStatus(apply.id, 2, "删除成功");
                    VMDao.DeleteVMInDB(vmname);
                    LogUtil.Log(Request, "删除个人虚拟机", vmname, operatorid, UserDao.GetUserById(operatorid).role, "success", res);
                    EmailUtil.SendEmail("删除个人虚拟机成功", "您的虚拟机:" + vmname + "已删除", apply.sender_id);
                }
                else
                {
                    LogUtil.Log(Request, "删除个人虚拟机", vmname, operatorid, UserDao.GetUserById(operatorid).role, "fail", res);
                }
            });
        }
        /// <summary>
        /// 批量删除虚拟机 函数 -by zzw
        /// </summary>
        public async void deleteVMs(string[] vmname, string operatorid, Apply_record apply)
        {
            int res = 0;
            await Task.Run(() =>
            {
                res = VMDao.DeleteVM(vmname);
                if (res!= 0)
                {
                    VMDao.ChangeApplyRecordStatus(apply.id, 2, "删除成功");
                    LogUtil.Log(Request, "批量删除虚拟机", apply.vm_name, operatorid, UserDao.GetUserById(operatorid).role, "success", "num="+(res.ToString()));
                    EmailUtil.SendEmail("批量删除虚拟机", "您申请删除的虚拟机已删除。", apply.sender_id);
                }
                else
                {
                    LogUtil.Log(Request, "删除个人虚拟机", apply.vm_name, operatorid, UserDao.GetUserById(operatorid).role, "fail");
                }
            });
        }
        /// <summary>
        /// 管理员删除虚拟机 函数 -by zzw
        /// </summary>
        public async void adminDeleteVM(string vmname,string adminid)
        {
            string res = "";
            await Task.Run(() =>
            {
                //VirtualMachine vm = VMDao.GetVMByName(vmname);
                res = VMDao.DeleteVM(vmname);
                if (res.Contains("success"))
                {
                    LogUtil.Log(Request, "删除个人虚拟机", vmname, adminid, UserDao.GetUserById(adminid).role, "success", res);
                    //EmailUtil.SendEmail("管理员删除您的虚拟机", "您的虚拟机:" + vmname + "已删除", vm.user_id);
                }
                else
                {
                    LogUtil.Log(Request, "删除个人虚拟机", vmname, adminid, UserDao.GetUserById(adminid).role, "fail", res);
                }
            });
        }
        /// <summary>
        /// 创建批量实验虚拟机 函数-by zzw
        /// </summary>
        //public async void createVMsForExp(VMConfig vm, int applyId, string targetId)
        //{
        //    string res = "";
        //    await Task.Run(() =>
        //    {
        //        Experiment exp = ExperimentDao.GetExperimentByApplyId(applyId);
        //        exp.vm_name = vm.Name;
        //        ExperimentDao.ChangeExperimentInfo(exp);
        //        Apply_record apply_Record = VMDao.GetApplyRecord(applyId);
        //        res = VMDao.CreateVMsForExp(vm, applyId);
        //        if (res.StartsWith("success") || res.StartsWith("error :null"))
        //        {
        //            EmailUtil.SendEmail("创建实验虚拟机成功", "您的实验:" + exp.name + "批量创建虚拟机已完成，请前往云平台查看", apply_Record.sender_id);
        //            LogUtil.Log(Request, "批量创建实验虚拟机", vm.Name, applyId.ToString(), UserDao.GetUserById(targetId).role, "success", res);
        //            exp.vm_status = 2;
        //            ExperimentDao.ChangeExperimentInfo(exp);
        //        }
        //        else
        //        {
        //            LogUtil.Log(Request, "批量创建实验虚拟机", vm.Name, applyId.ToString(), UserDao.GetUserById(targetId).role, "fail", res);
        //            exp.vm_status = -2;
        //            ExperimentDao.ChangeExperimentInfo(exp);
        //        }
        //    });
        //}
        public async void createVMsForExp(VMConfig vm, int applyId ,string targetId,string stulist)
        {
            string res = "";
            await Task.Run(() =>
            {
                Experiment exp = ExperimentDao.GetExperimentByApplyId(applyId);
                exp.vm_name = vm.Name;
                ExperimentDao.ChangeExperimentInfo(exp);
                Apply_record apply_Record = VMDao.GetApplyRecord(applyId);
                res = VMDao.CreateVMsForExp(vm , applyId,stulist);
                if (res.StartsWith("success") || res.StartsWith("error :null"))
                {
                    EmailUtil.SendEmail("创建实验虚拟机成功", "您的实验:" + exp.name + "批量创建虚拟机已完成，请前往云平台查看", apply_Record.sender_id);
                    LogUtil.Log(Request, "批量创建实验虚拟机", vm.Name, applyId.ToString(), UserDao.GetUserById(targetId).role, "success", res);
                    exp.vm_status = 2;
                    ExperimentDao.ChangeExperimentInfo(exp);
                }
                else
                {
                    LogUtil.Log(Request, "批量创建实验虚拟机", vm.Name, applyId.ToString(), UserDao.GetUserById(targetId).role, "fail", res);
                    exp.vm_status = -2;
                    ExperimentDao.ChangeExperimentInfo(exp);
                }
                VMDao.ChangeApplyRecordStatus(applyId, 2, "您的请求已处理");
            });
        }

        public async void changeConfig(VMConfig vmConfig, User user, Apply_record record = null)
        {
            string res = "";
            await Task.Run(() =>
            {
                dynamic ws = getManager();
                ws.PowerOption(vmConfig.Name, 2);
                string ret = ws.ChangeConfig(vmConfig.Name, vmConfig.CPU, vmConfig.Memory, vmConfig.Disk);
                if (ret.StartsWith("success"))
                {
                    if (record != null)
                    {
                        VMDao.ChangeApplyRecordStatus(record.id, 2, "修改成功");
                        EmailUtil.SendEmail("虚拟机配置修改成功", "您的虚拟机" + vmConfig.Name + "的修改配置申请已通过，请登录云平台使用"
                            , record.sender_id);
                    }
                    LogUtil.Log(Request, "修改虚拟机配置", vmConfig.Name, user.id, user.role, "success", "" , DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                }
                else
                {
                    if (record != null)
                    {
                        VMDao.ChangeApplyRecordStatus(record.id, 2, ret);
                    }
                    LogUtil.Log(Request, "修改虚拟机配置", vmConfig.Name, user.id, user.role, "fail", "", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                }
            });
        }

        /// <summary>
        /// 虚拟机电源管理
        /// </summary>
        /// <param name="vmInfo">vmName,option(int)[1-poweron|2-poweroff|3-reset]</param>
        /// <returns></returns>
        [Route("vm/powerManage"), HttpPost]
        public HttpResponseMessage PowerManage([FromBody]JObject vmInfo)
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                bool login = redis.IsSet(signature);
                if (!login)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                var param = HttpUtil.Deserialize(vmInfo);
                string userId = redis.Get<string>(signature);
                string vmName = param.vmName;
                int option = param.option;
                int permission = VMDao.GetPermissionForVM(vmName, userId);
                if(permission>0)
                {
                    dynamic ws = getManager();
                    ws.PowerOption(vmName, option);
                    return new Response(1001).Convert();
                }
                else
                {
                    return new Response(2002).Convert();
                }
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }

        /// <summary>
        /// 教师更改一个实验学生虚拟机的展示密码 by jyf
        /// </summary>
        /// <param name="vmInfo">experiment_id,password</param>
        /// <returns></returns>
        [Route("vm/changePassword"),HttpPost]
        public HttpResponseMessage VMChangePassword([FromBody]JObject vmInfo)
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                bool login = redis.IsSet(signature);
                if (!login)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string userId = redis.Get<string>(signature);
                User user = UserDao.GetUserById(userId);

                var JsonParams = HttpUtil.Deserialize(vmInfo);
                int experimentId = Convert.ToInt32(JsonParams.experiment_id);
                string password = JsonParams.password;

                Experiment experiment = ExperimentDao.GetExperimentById(experimentId);
                Course course = CourseDao.GetCourseInfoById((int)experiment.course_id);

                if (user.role < 2 && CourseDao.GetAssistantsByCourseId(course.id).Where(a => a.student_id == userId).Count() == 0)
                {
                    return new Response(2002, "没有相应权限").Convert();
                }
                if (user.role == 2 && userId != course.teacher_id)
                {
                    return new Response(2002, "没有相应权限").Convert();
                }
                if (user.role == 3 && user.department_id != course.department_id)
                {
                    return new Response(2002, "没有相应权限").Convert();
                }

                experiment.vm_passwd = password;
                ExperimentDao.ChangeExperimentInfo(experiment);

                LogUtil.Log(Request, "更改虚拟机密码", experiment.id.ToString(), user.id, user.role);
                return new Response(1001, "成功").Convert();
            }
            catch(Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }

        /// <summary>
        /// 管理员获取虚拟机
        /// </summary>
        /// <returns>
        /// List<Virtual_Machine>
        /// </returns>
        [Route("admin/getVM"),HttpGet]
        public HttpResponseMessage AdminGetVMs()
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                bool login = redis.IsSet(signature);
                if (!login)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string userId = redis.Get<string>(signature);
                
                //string userId = "admin";
                User user = UserDao.GetUserById(userId);
                

                if (user.role < 3)
                {
                    return new Response(2002, "无相应权限").Convert();
                }
                List<VirtualMachine> vms = new List<VirtualMachine>();
                List<VMConfig> confs = new List<VMConfig>();
                if (user.role == 3)
                {
                    confs = VMDao.GetDepartVMs(user.department_id);
                }
                else
                {
                    confs = VMDao.GetAllVMS();
                }
                vms = DataUtil.Transform(confs);
                confs = confs.Where(c => c.IsTemplate == false).ToList();
                var query = confs.Join(vms, conf => conf.Name, vm => vm.vm_name, (conf, vm) => new
                {
                    
                    vm.owner_id,
                    vm.user_id,
                    vm.vm_name,
                    conf.due_time,
                    
                    conf.AdvancedConfig,
                    conf.CPU,
                    conf.Disk,
                    conf.IsTemplate,
                    conf.Memory,
                    conf.GuestFullName,
                    conf.Status,
                    conf.console_url,
                    conf.TemplateName
                });
                return new Response(1001, "获取成功", query).Convert();
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }

        /// <summary>
        /// 管理员获取异常虚拟机
        /// </summary>
        /// <returns>
        /// list
        /// {owner_id, 拥有者id
        /// user_id, 使用者id
        /// vm_name, 虚拟机名称
        /// due_time, 到期时间
        /// AdvancedConfig, 
        /// CPU, cpu容量
        /// Disk, 磁盘容量
        /// Memory, 内存容量
        /// GuestFullName, 镜像名称
        /// Status:{
        /// IPAddress, IP地址
        /// HostName,
        /// PowerState, 电源状态
        /// RunTimeState,
        /// },
        /// errorType 异常类型}
        /// </returns>
        [Route("admin/getAbnormalVM"),HttpGet]
        public HttpResponseMessage AdminGetAbnormalVMs()
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                bool login = redis.IsSet(signature);
                if (!login)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string userId = redis.Get<string>(signature);
                User user = UserDao.GetUserById(userId);
                if (user.role < 3)
                {
                    return new Response(2002, "无相应权限").Convert();
                }
                List<VirtualMachine> vms = new List<VirtualMachine>();
                List<VMConfig> confs = new List<VMConfig>();
                if (user.role == 3)
                {
                    confs = VMDao.GetDepartVMs(user.department_id);
                }
                else
                {
                    confs = VMDao.GetAllVMS();
                }
                List<VMConfig> confs1 = new List<VMConfig>();
                foreach(VMConfig config in confs){
                    if(config.CPU > 8)
                        config.errorType += "CPU数过多 ";
                    if(config.Disk > 102400)
                        config.errorType += "磁盘过大 ";
                    if(config.Memory > 16*1024)
                        config.errorType += "内存过大 ";
                    if(config.errorType != null && !config.errorType.Equals("")) 
                        confs1.Add(config);
                }
                vms = DataUtil.Transform(confs1);
                confs1 = confs1.Where(c => c.IsTemplate == false).ToList();
                
                var query = confs1.Join(vms, conf1 => conf1.Name, vm => vm.vm_name, (conf1, vm) => new
                {
                    
                    vm.owner_id,
                    vm.user_id,
                    vm.vm_name,
                    vm.due_time,
                    
                    conf1.AdvancedConfig,
                    conf1.CPU,
                    conf1.Disk,
                    conf1.IsTemplate,
                    conf1.Memory,
                    conf1.GuestFullName,
                    conf1.Status,
                    conf1.TemplateName,
                    conf1.errorType
                });
                return new Response(1001, "获取成功", query).Convert();
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }

        /// <summary>
        /// 管理员获取虚拟机分析
        /// </summary>
        /// <returns>
        /// map数据
        /// { "experimentVm", experimentVm.ToString() },实验虚拟机数量
        /// { "personalVm", personalVm.ToString() },个人虚拟机数量
        /// { "cpuInfo", cpuInfos:list{size,number} },cpu信息
        /// { "memoryInfo",memoryInfos:list{size,number}} 内存信息
        /// </returns>
        [Route("admin/getAnalyzeVM"),HttpGet]
        public HttpResponseMessage AdminGetAnalyzeVMs()
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                bool login = redis.IsSet(signature);
                if (!login)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string userId = redis.Get<string>(signature);


                User user = UserDao.GetUserById(userId);


                if (user.role < 3)
                {
                    return new Response(2002, "无相应权限").Convert();
                }

                List<VMConfig> confs = new List<VMConfig>();
                if (user.role == 3)
                {
                    confs = VMDao.GetDepartVMs(user.department_id);
                }
                else
                {
                    confs = VMDao.GetAllVMS();
                }
                confs = confs.Where(c => c.IsTemplate == false).ToList();
                int experimentVm = 0;
                int personalVm = 0;
                Dictionary<int, int> retCpuData = new Dictionary<int, int>();
                Dictionary<long, int> retMemoryData = new Dictionary<long, int>();
                foreach(VMConfig config in confs){
                    if(config.is_exp == true)
                        experimentVm ++;
                    else 
                        personalVm ++;
                    if(retCpuData.ContainsKey(config.CPU) == true)
                    retCpuData[config.CPU] ++;
                    else retCpuData.Add(config.CPU,1);

                    if(retMemoryData.ContainsKey(config.Memory) == true)
                    retMemoryData[config.Memory] ++;
                    else retMemoryData.Add(config.Memory,1);    
                }
                List<CpuInfo> cpuInfos = new List<CpuInfo>();
                List<MemoryInfo> memoryInfos = new List<MemoryInfo>();
                foreach(int key in retCpuData.Keys){
                    CpuInfo cpuInfo = new CpuInfo();
                    cpuInfo.size = key;
                    cpuInfo.number = retCpuData[key];
                    cpuInfos.Add(cpuInfo);
                }
                foreach(long key in retMemoryData.Keys){
                    MemoryInfo memoryInfo = new MemoryInfo();
                    memoryInfo.size = key;
                    memoryInfo.number = retMemoryData[key];
                    memoryInfos.Add(memoryInfo);
                }

                Dictionary<string, Object> retData = new Dictionary<string, Object>
                {
                    { "experimentVm", experimentVm.ToString() },
                    { "personalVm", personalVm.ToString() },
                    { "cpuInfo", cpuInfos },
                    { "memoryInfo",memoryInfos}
                };
                return new Response(1001, "获取成功", retData).Convert();
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }

        /// <summary>
        /// 管理员获取异常用户
        /// </summary>
        /// <returns>
        /// list{
        /// name, 姓名
        /// id, 学号
        /// vmNumber, 虚拟机数量
        /// role 用户类型}
        /// </returns>
        [Route("admin/getAbnormalUser"),HttpGet]
        public HttpResponseMessage AdminGetAbnormalUsers()
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                bool login = redis.IsSet(signature);
                if (!login)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string userId = redis.Get<string>(signature);
                User user = UserDao.GetUserById(userId);


                if (user.role < 3)
                {
                    return new Response(2002, "无相应权限").Convert();
                }

                List<VMConfig> confs = new List<VMConfig>();
                if (user.role == 3)
                {
                    confs = VMDao.GetDepartVMs(user.department_id);
                }
                else
                {
                    confs = VMDao.GetAllVMS();
                }
                Dictionary<string, int> userDictionary = new Dictionary<string, int>();
                confs = confs.Where(c => c.IsTemplate == false).ToList();
                foreach(VMConfig config in confs){
                    if(userDictionary.ContainsKey(config.teacher_id) == true){
                        userDictionary[config.teacher_id] += 1;
                    }
                    else{
                        userDictionary.Add(config.teacher_id,1);
                    }
                }
                List<UserInfo> userList = new List<UserInfo>();
                User user1;
                foreach(string key in userDictionary.Keys){
                    user1 = UserDao.GetUserById(key);
                    if (user1 != null){
                        UserInfo userInfo = new UserInfo();
                        userInfo.id = user1.id;
                        userInfo.role = user1.role;
                        userInfo.name = user1.name;
                        userInfo.vmNumber = userDictionary[key];
                        userList.Add(userInfo);
                    }
                }
                return new Response(1001, "获取成功", userList).Convert();
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }


       

          


        /// <summary>
        /// 管理员获取模板
        /// </summary>
        /// <returns>
        /// List<Virtual_Machine>
        /// </returns>
        [Route("admin/getTemplate"), HttpGet]
        public HttpResponseMessage AdminGetTemplates()
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                bool login = redis.IsSet(signature);
                if (!login)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string userId = redis.Get<string>(signature);
                
                //string userId = "16211084";
                User user = UserDao.GetUserById(userId);


                if (user.role < 3)
                {
                    return new Response(2002, "无相应权限").Convert();
                }
                List<VirtualMachine> vms = new List<VirtualMachine>();
                List<VMConfig> confs = new List<VMConfig>();
                if (user.role == 3)
                {
                    confs = VMDao.GetDepartVMs(user.department_id);
                }
                else
                {
                    confs = VMDao.GetAllVMS();
                }
                vms = DataUtil.Transform(confs);
                confs = confs.Where(c => c.IsTemplate == true).ToList();
                var query = confs.Join(vms, conf => conf.Name, vm => vm.vm_name, (conf, vm) => new
                {
                    
                    vm.owner_id,
                    vm.user_id,
                    vm.vm_name,
                    conf.AdvancedConfig,
                    conf.CPU,
                    conf.Disk,
                    conf.IsTemplate,
                    conf.Memory,
                    conf.GuestFullName,
                    conf.Status,
                    conf.TemplateName
                });
                return new Response(1001, "获取成功", query).Convert();
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }
        /// <summary>
        /// 导入现有模板
        /// </summary>
        /// <param name="vmInfo"></param>
        /// <returns></returns>
        //[Route("vm/addExistingVM"), HttpPost]
        //public HttpResponseMessage AddExsitingVM([FromBody]JObject vmInfo)
        //{
        //    try
        //    {
                
        //        string signature = HttpUtil.GetAuthorization(Request);
        //        if (signature == null || !redis.IsSet(signature))
        //        {
        //            return new Response(2001, "未登录账户").Convert();
        //        }
        //        string id = redis.Get<string>(signature);
        //        if (signature == null)
        //            return new Response(2001).Convert();
        //        User user = UserDao.GetUserById(id);
        //        if (user.role < 4)
        //            return new Response(2002).Convert();
        //        VirtualMachine vm = new VirtualMachine();
        //        var jsonParams = HttpUtil.Deserialize(vmInfo);
                
        //        vm.vm_name = jsonParams.vm_name;
        //        vm.user_id = jsonParams.user_id;
        //        vm.owner_id = jsonParams.owner_id;
        //        if (VMDao.AddVM(vm))
        //            return new Response(1001).Convert();
        //        else
        //            return new Response(4001, "未知错误").Convert();
        //    }catch(Exception e)
        //    {
        //        ErrorLogUtil.WriteLogToFile(e, Request);
        //        return Response.Error();
        //    }
        //}

        [Route("vm/getConsole"), HttpGet]
        public HttpResponseMessage GetConsole([FromUri]string vmName)
        {
            try
            {
                string managerType = ConfigurationManager.AppSettings["VMManagerType"];
                if (!managerType.Equals("Sangfor"))
                    return new Response(2002).Convert();
                string token = HttpUtil.GetAuthorization(Request);
                if (token == null)
                    return new Response(2001).Convert();
                string id = redis.Get<string>(token);
                if (token == null)
                    return new Response(2001).Convert();
                if (VMDao.GetPermissionForVM(vmName, id) < 1)
                    return new Response(2002).Convert();
                //VirtualMachine vm = VMDao.GetVMByName(vmName);
                //if(vm != null & vm.uuid != null)
                //{
                    //RESTful rest = new RESTful();
                    //string consoleUrl = rest.OpenConsole(vm.uuid);
                    //return new Response(1001, "获取成功", consoleUrl).Convert();
               // }
                //else
                //{
                    return new Response(2002).Convert();
                //}
            }
            catch(Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }
    }
}




