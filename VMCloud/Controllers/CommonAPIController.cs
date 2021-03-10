using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using VMCloud.Utils;
using VMCloud.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VMCloud.Models.DAO;
using System.Web;
using System.Collections.Specialized;

namespace VMCloud.Controllers
{
    /// <summary>
    /// 公共部分API
    /// </summary>
    //[RoutePrefix("api")]
    public class CommonAPIController : ApiController
    {
        private RedisHelper redis;

        /// <summary>
        /// 初始化Helper
        /// </summary>
        public CommonAPIController()
        {
            redis = RedisHelper.GetRedisHelper();
        }
        /// <summary>
        /// 返回个人信息API by jyf
        /// 2019.7.13
        /// </summary>
        /// <returns>
        /// {
        ///     "code":,
        ///     "msg":"",
        ///     "data":{
        ///         "email":"",
        ///         "name":""
        ///     }   
        /// }
        /// </returns>
        [Route("getInfo"), HttpGet]
        public HttpResponseMessage GetInfo()
        {
            Dictionary<string, string> retData = new Dictionary<string, string>();
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
                if (user == null)
                {
                    return new Response(1001, "用户不存在").Convert();
                }

                retData.Add("email", user.email.ToString());
                retData.Add("name", user.name.ToString());
                return new Response(1001, "成功获取个人信息", retData).Convert();
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }

        /// <summary>
        /// 修改个人密码API by zzw
        /// 2019.7.14
        /// </summary>
        /// <param name="account">id,passwdOld,passwdNew</param>
        /// <returns>
        /// {
        ///     "code":,
        ///     "msg":""
        /// }
        /// </returns>
        [Route("changePwd"), HttpPost]
        public HttpResponseMessage ChangePwd([FromBody]JObject account)
        {
            Dictionary<string, string> retData = new Dictionary<string, string>();
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                var jsonParams = HttpUtil.Deserialize(account);
                string id = jsonParams.userId;
                string passwdNew = jsonParams.passwdNew;
                string passwdOld = jsonParams.passwdOld;
                bool login = redis.IsSet(signature);
                if (!login)
                {
                    return new Response(2001, "未登录账户").Convert();
                }

                string targetId = redis.Get<string>(signature);
                if (id == null)
                    id = targetId;
                User user = UserDao.GetUserById(id);
                User targetUser = UserDao.GetUserById(targetId);
                //权限判定
                if(targetUser.role == 4 && user.role == 4 && !id.Equals(targetId))
                {
                    //管理员不能修改其他管理员的信息
                    return new Response(2002, "无权限访问该用户的信息").Convert();
                }
                else if (targetUser.role == 3 && user.role == 3 && !id.Equals(targetId))
                {
                    //身份为部门管理员不能修改其他部门管理员的信息
                    return new Response(2002, "无权限访问该用户的信息").Convert();
                }
                else if(targetUser.role == 3 && user.department_id != targetUser.department_id)
                {
                    //身份为部门管理员但id不属于自己的学院
                    return new Response(2002, "无权限访问该用户的信息").Convert();
                }
                else if (targetUser.role == 2 && !id.Equals(targetId))
                {
                    //普通用户且id不相同
                    return new Response(2002, "无权限访问该用户的信息").Convert();
                }

                //如果id不同，则说明是权限高的修改权限低的，所以不需要判定passwordOld
                if (id.Equals(targetId))//如果是在修改自己的密码则需要判定原密码
                {
                    if (!passwdOld.Equals(user.passwd))
                    {
                        return new Response(1001, "密码不正确").Convert();
                    }
                }
                
                int res = UserDao.ChangePwd(id, passwdNew);
                if (res == 1)
                {
                    LogUtil.Log(Request, "修改密码", id, targetId, user.role);
                    return new Response(1001, "成功修改密码").Convert();
                }
                else if (res == 0)
                {
                    return new Response(1001, "不能与原密码相同").Convert();
                }
                throw new Exception("修改密码出现未知错误");
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return new Response(4001).Convert(); ;
            }
        }
        /// <summary>
        /// 修改个人信息API by xzy
        /// 2019.7.14
        /// </summary>
        /// <param name="account">id,name,email</param>
        /// <returns>
        /// {
        ///     "code":,
        ///     "msg":""
        /// }
        /// </returns>
        /// 
        [Route("changeInfo"), HttpPost]
        public HttpResponseMessage ChangeInfo([FromBody]JObject account)
        {
            Response ret = new Response();
            Dictionary<string, string> retData = new Dictionary<string, string>();
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
              
                var jsonParams = HttpUtil.Deserialize(account);

                string id = jsonParams.userId;
                string name = jsonParams.name;
                string email = jsonParams.email;
                User user = UserDao.GetUserById(id);
                bool login = redis.IsSet(signature);
                if (!login)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string targetId = redis.Get<string>(signature);
                if (id != targetId)
                {
                    return new Response(2002, "无权限访问该用户的信息").Convert();

                }
                UserDao.ChangeInfo(id, name, email);
                LogUtil.Log(Request, "修改个人信息", id, id, user.role);
                return new Response(1001, "成功修改个人信息").Convert();

            }
            catch (Exception e)
            {
                
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }

        }






        /// <summary>
        /// 通过学期和学生id获得课程列表 by zzw
        /// 2019.7.22
        /// </summary>
        /// <param name="stuIdAndTerm">term</param>
        /// <returns>
        /// {
        ///     "code":,
        ///     "msg":""
        ///     "data":{
        ///         'id': '',
        ///         'name': '',
        ///         'semester': '',
        ///         'teacher': '',
        ///         'department': ''
        ///     }
        /// }
        /// </returns>
        /// 
        [Route("getCourseListByStuIdAndTerm"), HttpGet]
        public HttpResponseMessage GetCourseListByStuIdAndTerm([FromBody]JObject stuIdAndTerm)
        {
            List<Dictionary<string, string>> retData = new List<Dictionary<string, string>>();
            Dictionary<string, string> courseInfo;
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                var jsonParams = Request.GetQueryNameValuePairs().ToDictionary(k => k.Key, v => v.Value);
                string term = jsonParams["term"];
                bool isLogin = redis.IsSet(signature);
                if (!isLogin)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string targetId = redis.Get<string>(signature);
                int termId = CourseDao.GetTermByName(term).id;
                List<Course> courseList = CourseDao.GetCoursesByStuIdAndTermId(targetId, termId).ToList();
                foreach (Course c in courseList)
                {
                    courseInfo = new Dictionary<string, string>
                    {
                        { "id", c.id.ToString() },
                        { "name", c.name },
                    };
                    if (c.term_id == null)
                    {
                        courseInfo.Add("semester", "");
                    }
                    else
                    {
                        courseInfo.Add("semester", CourseDao.GetTermById((int)c.term_id).name);
                    }
                    courseInfo.Add("teacher", UserDao.GetUserById(c.teacher_id).name);
                    courseInfo.Add("department", CourseDao.GetDepartmentById(c.department_id).name);
                    retData.Add(courseInfo);
                }
                return new Response(1001, "获取成功", retData).Convert();
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }

        /// <summary>
        /// 通过课程id获取学生列表 by zzw
        /// 2019.7.22
        /// </summary>
        /// <param name="account">courseId</param>
        /// <returns>
        /// {
        ///     "code":,
        ///     "msg":""
        ///     "data":{
        ///         'id': '',
        ///         'name': '',
        ///         'department': '',
        ///         'email': ''
        ///     }
        /// }
        /// </returns>
        /// 
        [Route("getStuListByCourseId"), HttpGet]
        public HttpResponseMessage GetStuListByCourseId([FromBody]JObject account)
        {
            List<Dictionary<string, string>> retData = new List<Dictionary<string, string>>();
            Dictionary<string, string> stuInfo;
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                var jsonParams = Request.GetQueryNameValuePairs().ToDictionary(k => k.Key, v => v.Value);
                int courseId = Convert.ToInt32(jsonParams["courseId"]);
                bool isLogin = redis.IsSet(signature);
                if (!isLogin)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string targetId = redis.Get<string>(signature);
                Course course = CourseDao.GetCourseInfoById(courseId);
                User user = UserDao.GetUserById(targetId);
                User professor = UserDao.GetUserById(course.teacher_id);

                Dictionary<string, string> department = new Dictionary<string, string>();
                string depart = null;
                if (user.role == 4 || (user.role ==3 && user.department_id == professor.department_id) || user.id == professor.id ||
                    (CourseDao.GetAssistantsByCourseId((courseId)).Where(a => a.student_id == user.id).Count() == 1))
                {
                    //如果是管理员、负责这个学院的部门管理员、课程对应的老师、课程对应的助教才有资格访问
                    List<User> stuList = CourseDao.GetStudentsById(courseId);
                    string temp = null;
                    foreach (User stu in stuList)
                    {
                        
                        if(department.ContainsKey(stu.department_id))
                        {
                            depart = department[stu.department_id];
                        }
                        else
                        {
                            temp = CourseDao.GetDepartmentById(stu.department_id).name;
                            department.Add(stu.department_id, temp);
                            depart = temp;
                        }
                        stuInfo = new Dictionary<string, string>
                        {
                            { "id", stu.id.ToString() },
                            { "name", stu.name },
                            { "department", depart },
                            { "email", stu.email }
                        };
                        retData.Add(stuInfo);
                    }
                    return new Response(1001, "获取成功", retData).Convert();
                }
                else
                {
                    return new Response(2002, "没有权限访问该信息").Convert();
                }
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }

        /// <summary>
        /// 获取没有虚拟机的学生列表
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        [Route("getStuListNoVM"), HttpGet]
        public HttpResponseMessage GetStuListNoVM([FromBody]JObject account)
        {
            List<Dictionary<string, string>> retData = new List<Dictionary<string, string>>();
            Dictionary<string, string> stuInfo;
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                var jsonParams = Request.GetQueryNameValuePairs().ToDictionary(k => k.Key, v => v.Value);
                int courseId = Convert.ToInt32(jsonParams["courseId"]);
                int expid= Convert.ToInt32(jsonParams["expId"]);
                bool isLogin = redis.IsSet(signature);
                if (!isLogin)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string targetId = redis.Get<string>(signature);
                Course course = CourseDao.GetCourseInfoById(courseId);
                User user = UserDao.GetUserById(targetId);
                User professor = UserDao.GetUserById(course.teacher_id);
                
                Dictionary<string, string> department = new Dictionary<string, string>();
                string depart = null;
                if (user.role == 4 || (user.role == 3 && user.department_id == professor.department_id) || user.id == professor.id ||
                    (CourseDao.GetAssistantsByCourseId((courseId)).Where(a => a.student_id == user.id).Count() == 1))
                {
                    //如果是管理员、负责这个学院的部门管理员、课程对应的老师、课程对应的助教才有资格访问
                    List<User> stuList = CourseDao.GetStudentsById(courseId);
                    string temp = null;
                    Experiment exp = ExperimentDao.GetExperimentById(expid);
                    List<VMConfig> virtuals = VMDao.GetVMsByVmName(exp.vm_name);
                    
                    foreach (User stu in stuList)
                    {
                        bool flag = true;
                        if (department.ContainsKey(stu.department_id))
                        {
                            depart = department[stu.department_id];
                        }
                        else
                        {
                            temp = CourseDao.GetDepartmentById(stu.department_id).name;
                            department.Add(stu.department_id, temp);
                            depart = temp;
                        }
                        foreach(VMConfig vm in virtuals)
                        {
                            if (vm.student_id == stu.id)
                                flag = false;
                        }
                        if (flag == false)
                            continue;



                        stuInfo = new Dictionary<string, string>
                        {
                            { "id", stu.id.ToString() },
                            { "name", stu.name }
                        };
                        retData.Add(stuInfo);
                    }
                    return new Response(1001, "获取成功", retData).Convert();
                }
                else
                {
                    return new Response(2002, "没有权限访问该信息").Convert();
                }
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }



        /// <summary>
        /// 修改课程信息API-教师 by zzw
        /// 2019.7.24
        /// </summary>
        /// <param name="Newcourse"></param>
        /// <returns>
        /// {
        ///     "code":,
        ///     "msg":""
        /// }
        /// </returns>
        [Route("changeCourseInfo"), HttpPost]
        public HttpResponseMessage ChangeCourseInfo([FromBody]JObject Newcourse)
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                Course course = new Course();
                QuickCopy.Copy<Course>(Newcourse, ref course);
                bool login = redis.IsSet(signature);
                if (!login)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string targetId = redis.Get<string>(signature);
                Course oldCourse = CourseDao.GetCourseInfoById(course.id);
                if(oldCourse == null || oldCourse.teacher_id != targetId)
                {
                    return new Response(2002, "无权限修改该课程的信息").Convert();
                }
                CourseDao.ChangeCourseInfo(course);
                LogUtil.Log(Request, "修改课程", course.id.ToString(), targetId,  UserDao.GetUserById(targetId).role);
                return new Response(1001, "修改课程信息成功").Convert();
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return new Response(4001).Convert(); ;
            }
        }

        /// <summary>
        /// 添加学期信息API-管理员 by zzw
        /// 2019.7.24
        /// </summary>
        /// <param name="newTerm">name</param>
        /// <returns>
        /// {
        ///     "code":,
        ///     "msg":""
        /// }
        /// </returns>
        [Route("addTerm"), HttpPost]
        public HttpResponseMessage AddTerm([FromBody]JObject newTerm)
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                Term term = new Term();
                QuickCopy.Copy<Term>(newTerm, ref term);
                bool login = redis.IsSet(signature);
                if (!login)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string targetId = redis.Get<string>(signature);
                if (UserDao.GetUserById(targetId).role != 4)
                {
                    return new Response(2002, "无权限添加学期信息").Convert();
                }
                else
                {
                    Term temp = CourseDao.GetTermByName(term.name);
                    if (temp != null)
                    {
                        return new Response(1002, "添加学期失败").Convert();
                    }
                    int res = UserDao.AddTerm(term);
                    if (res == 1)
                    {
                        Term t = CourseDao.GetTermByName(term.name);
                        LogUtil.Log(Request, "添加学期", t.id.ToString(), targetId, UserDao.GetUserById(targetId).role);
                        return new Response(1001, "添加学期成功").Convert();
                    }
                    return new Response(1001, "添加学期失败").Convert();
                }
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return new Response(4001).Convert(); ;
            }
        }

        /// <summary>
        /// 删除学期信息API-管理员 by zzw
        /// 2019.7.24
        /// </summary>
        /// <param name="terminfo">name</param>
        /// <returns>
        /// {
        ///     "code":,
        ///     "msg":""
        /// }
        /// </returns>
        [Route("deleteTerm"), HttpPost]
        public HttpResponseMessage DeleteTerm([FromBody]JObject terminfo)
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                var jsonParams = HttpUtil.Deserialize(terminfo);
                string termname = jsonParams.name;
                bool login = redis.IsSet(signature);
                if (!login)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string targetId = redis.Get<string>(signature);
                if (UserDao.GetUserById(targetId).role != 4)
                {
                    return new Response(2002, "无权限删除学期信息").Convert();
                }
                else
                {
                    Term t = CourseDao.GetTermByName(termname);
                    if (t != null)
                    {
                        if (CourseDao.GetAllCourse().Where(c => c.term_id == t.id).Count() > 0)
                            return new Response(1002, "该学期仍有课程，无法删除").Convert();
                        int res = UserDao.DeleteTerm(t);
                        if (res == 1)
                        {
                            LogUtil.Log(Request, "删除学期", t.id.ToString(), targetId, UserDao.GetUserById(targetId).role);
                            return new Response(1002, "删除学期成功").Convert();
                        }
                    }
                    return new Response(1002, "删除学期失败").Convert();
                }
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return new Response(4001).Convert(); ;
            }
        }

        /// <summary>
        /// 返回当前学期API by zzw
        /// 2019.7.24
        /// </summary>
        /// <param></param>
        /// <returns>
        /// {
        ///     "code":,
        ///     "msg":""
        /// }
        /// </returns>
        [Route("getNowTerm"), HttpGet]
        public HttpResponseMessage GetNowTerm()
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
                else
                {
                    Term term = UserDao.GetNowTerm();
                    return new Response(1001, "查询成功", term).Convert();
                }
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return new Response(4001).Convert(); ;
            }
        }





    }

    
}
