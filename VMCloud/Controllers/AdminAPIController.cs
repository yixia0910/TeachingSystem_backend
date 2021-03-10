/*
*	Author:       Xing Zhihuan
*   Email:        1@roycent.cn	
*	Created:      2019/8/30 1:24:30
*   Description:  管理员相关功能接口
 */
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using VMCloud.Models;
using VMCloud.Models.DAO;
using VMCloud.Utils;
using Spire.Xls;
using System.IO;
using System.Web.Security;
using System.Configuration;

namespace VMCloud.Controllers
{
    /// <summary>
    /// 管理员管理相关接口
    /// </summary>
    //[RoutePrefix("api")]
    public class AdminAPIController : ApiController
    {

        private RedisHelper redis;

        /// <summary>
        /// 初始化Helper
        /// </summary>
        public AdminAPIController()
        {
            redis = RedisHelper.GetRedisHelper();
        }
        /// <summary>
        /// 获取服务器日志 支持分页(page、size)
        /// </summary>
        /// <returns></returns>
        [Route("admin/getLogs"), HttpGet]
        public HttpResponseMessage GetLogs()
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
                List<System_log> logList;
                var jsonParams = Request.GetQueryNameValuePairs().ToDictionary(k => k.Key, v => v.Value);
                using (var logs = new Logger())
                {
                    if (jsonParams.Count > 0)
                    {
                        int page = int.Parse(jsonParams["page"]);
                        int size = int.Parse(jsonParams["size"]);
                        int count;
                        logList = logs.Logs.Pagination(l => (-l.id), page, size, out count).ToList();
                        Dictionary<string, object> ret = new Dictionary<string, object>();
                        ret.Add("data", AddUserNameIntoLogList(logList));
                        ret.Add("count", count);
                        return new Response(1001, "成功", ret).Convert();
                    }
                    else
                    {
                        logList = logs.Logs.ToList();
                        return new Response(1001, "成功", AddUserNameIntoLogList(logList)).Convert();
                    }
                }
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }

        private List<System_log> AddUserNameIntoLogList(List<System_log> logs)
        {
            Dictionary<string, string> namePairs = new Dictionary<string, string>();
            foreach(var log in logs)
            {
                string name = "";
                if (log.operator_id != null)
                {
                    if (!namePairs.TryGetValue(log.operator_id, out name))
                    {
                        User u = UserDao.GetUserById(log.operator_id);
                        if (u != null)
                        {
                            name = u.name;
                            namePairs.Add(log.operator_id, name);
                        }
                    }
                }
                log.operator_id = log.operator_id + "(" + name + ")";
            }
            return logs;
        }

        /// <summary>
        /// 超级管理员获取院系管理员
        /// </summary>
        /// <returns></returns>
        [Route("admin/getDepartmentAdmin")]
        public HttpResponseMessage GetDepartmentAdmin()
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

                List<User> users = UserDao.GetUserByRole(3);
                List<Dictionary<string, string>> ret = new List<Dictionary<string, string>>();
                Dictionary<string, string> temp;
                var props = users.First().GetType().GetProperties();
                List<Department> departments = CourseDao.GetDepartments();
                foreach (User u in users)
                {
                    temp = new Dictionary<string, string>();
                    foreach (var pi in props)
                    {
                        var v = u.GetType().GetProperty(pi.Name).GetValue(u, null);
                        string value;
                        if (v != null)
                        {
                            value = v.ToString();
                        }
                        else
                        {
                            value = "";
                        }
                        temp.Add(pi.Name, value);
                    }
                    Department department = departments.Find(d => d.id.Equals(u.department_id));
                    if (department == null)
                        temp.Add("department_name", "无");
                    else
                        temp.Add("department_name", department.name);
                    ret.Add(temp);
                }

                return new Response(1001, "获取成功", ret).Convert();

                //return new Response(1001, "获取成功", UserDao.GetUserByRole(3)).Convert();
            }
            catch(Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
            
        }

        /// <summary>
        /// 超级管理员,系管理员获取教师
        /// </summary>
        /// <returns></returns>
        [Route("admin/getTeacher")]
        public HttpResponseMessage GetTeacher()
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
                if (user.role != 4 && user.role!=3)
                {
                    return new Response(2002).Convert();
                }

                List<User> users = UserDao.GetUserByRole(2, user.role == 3 ? user.department_id : "");
                if (users.Count() == 0)
                {
                    return new Response(1001, "获取成功").Convert();
                }
                List<Dictionary<string, string>> ret = new List<Dictionary<string, string>>();
                Dictionary<string, string> temp;
                var props = users.First().GetType().GetProperties();
                List<Department> departments = CourseDao.GetDepartments();
                foreach (User u in users)
                {
                    temp = new Dictionary<string, string>();
                    foreach (var pi in props)
                    {
                        var v = u.GetType().GetProperty(pi.Name).GetValue(u, null);
                        string value;
                        if (v != null)
                        {
                            value = v.ToString();
                        }
                        else
                        {
                            value = "";
                        }
                        temp.Add(pi.Name, value);
                    }
                    Department department = departments.Find(d => d.id.Equals(u.department_id));
                    if (department == null)
                        temp.Add("department_name", "无");
                    else
                        temp.Add("department_name", department.name);
                    ret.Add(temp);
                }

                return new Response(1001, "获取成功", ret).Convert();

                //return new Response(1001, "获取成功", UserDao.GetUserByRole(2, user.role==3? user.department_id:"")).Convert();
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }

        }

        /// <summary>
        /// 超级管理员,系管理员获取学生
        /// </summary>
        /// <returns></returns>
        [Route("admin/getStudent")]
        public HttpResponseMessage GetStudent()
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
                
                //string id = "admin";
                User user = UserDao.GetUserById(id);
                if (user.role != 4 && user.role != 3)
                {
                    return new Response(2002).Convert();
                }

                List<User> users = UserDao.GetUserByRole(1, user.role == 3 ? user.department_id : "");
                if(users.Count() == 0)
                {
                    return new Response(1001, "获取成功").Convert();
                }
                List<Dictionary<string, string>> ret = new List<Dictionary<string, string>>();
                Dictionary<string, string> temp;
                var props = users.First().GetType().GetProperties();
                List<Department> departments = CourseDao.GetDepartments();
                foreach (User u in users)
                {
                    temp = new Dictionary<string, string>();
                    foreach (var pi in props)
                    {
                        var v = u.GetType().GetProperty(pi.Name).GetValue(u, null);
                        string value;
                        if (v != null)
                        {
                            value = v.ToString();
                        }
                        else
                        {
                            value = "";
                        }
                        temp.Add(pi.Name, value);
                    }
                    Department department = departments.Find(d => d.id.Equals(u.department_id));
                    if (department == null)
                        temp.Add("department_name", "无");
                    else
                        temp.Add("department_name", department.name);
                    ret.Add(temp);
                }

                return new Response(1001, "获取成功", ret).Convert();
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }

        }

        /// <summary>
        /// 增加学生用户（超管,系统管理员）
        /// </summary>
        /// <returns></returns>
        [Route("admin/addStudent")]
        public HttpResponseMessage AddStudent([FromBody]JObject studentInfo)
        {
            try
            {
                var jsonParams = HttpUtil.Deserialize(studentInfo);

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


                if (user.role != 4 && user.role != 3)
                {
                    return new Response(2002).Convert();
                }

                User toAdd = new User();
                QuickCopy.Copy(studentInfo, ref toAdd);
                toAdd.name = toAdd.name.ToLower();
                toAdd.is_accept = false;
                toAdd.nick_name = toAdd.name;
                toAdd.role = 1;
                toAdd.passwd = ConfigurationManager.AppSettings["DefaultUserPasswd"];


                if (user.role == 3 && toAdd.department_id != null && user.department_id != toAdd.department_id)
                {
                    return new Response(2002,"不能添加其他系的学生").Convert();
                }
                if(user.role == 4 && toAdd.department_id == null)
                {
                    return new Response(3001, "院系不能为空").Convert();
                }
                if(user.role == 3)
                {
                    toAdd.department_id = user.department_id;
                }

                List<User> users = new List<User>();
                users.Add(toAdd);
                UserDao.AddUser(users);
                return new Response(1001, "添加成功").Convert();

            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }

        }

        /// <summary>
        /// 增加教师用户（超管,系统管理员）
        /// </summary>
        /// <returns></returns>
        [Route("admin/addTeacher")]
        public HttpResponseMessage AddTeacher([FromBody]JObject teacherInfo)
        {
            try
            {
                var jsonParams = HttpUtil.Deserialize(teacherInfo);
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


                if (user.role != 4 && user.role != 3)
                {
                    return new Response(2002).Convert();
                }

                User toAdd = new User();
                QuickCopy.Copy(teacherInfo, ref toAdd);
                toAdd.name = toAdd.name.ToLower();
                toAdd.is_accept = false;
                toAdd.nick_name = toAdd.name;
                toAdd.role = 2;
                toAdd.passwd = ConfigurationManager.AppSettings["DefaultUserPasswd"]; 


                if (user.role == 3 && toAdd.department_id != null && user.department_id != toAdd.department_id)
                {
                    return new Response(2002, "不能添加其他系的教师").Convert();
                }
                if (user.role == 4 && toAdd.department_id == null)
                {
                    return new Response(3001, "院系不能为空").Convert();
                }
                if (user.role == 3)
                {
                    toAdd.department_id = user.department_id;
                }

                List<User> users = new List<User>();
                users.Add(toAdd);
                UserDao.AddUser(users);
                return new Response(1001, "添加成功").Convert();

            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }

        }


        /// <summary>
        /// 增加系管理员用户（超管）
        /// </summary>
        /// <returns></returns>
        [Route("admin/addDepartmentAdmin")]
        public HttpResponseMessage AddDepartmentAdmin([FromBody]JObject AdminInfo)
        {
            try
            {
                var jsonParams = HttpUtil.Deserialize(AdminInfo);
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


                if (user.role != 4 )
                {
                    return new Response(2002).Convert();
                }

                User toAdd = new User();
                QuickCopy.Copy(AdminInfo, ref toAdd);
                toAdd.name = toAdd.name.ToLower();
                toAdd.is_accept = false;
                toAdd.nick_name = toAdd.name;
                toAdd.role = 3; 
                toAdd.passwd = ConfigurationManager.AppSettings["DefaultUserPasswd"];

                if (user.role == 4 && toAdd.department_id == null)
                {
                    return new Response(3001, "院系不能为空").Convert();
                }


                List<User> users = new List<User>();
                users.Add(toAdd);
                UserDao.AddUser(users);
                return new Response(1001, "添加成功").Convert();

            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }

        }


        /// <summary>
        /// 工具函数
        /// </summary>
        /// <param name="admin"></param>
        /// <param name="file"></param>
        /// <param name="role"></param>
        /// <returns></returns>
        public int batchCreateUsers(User admin, HttpPostedFileBase file, int role)
        {
            var severPath = System.Web.Hosting.HostingEnvironment.MapPath("/ExcelFiles/");
            if (!Directory.Exists(severPath))
            {
                Directory.CreateDirectory(severPath);
            }
            var savePath = Path.Combine(severPath, file.FileName);
            User user = null;
            Workbook workbook = new Workbook();
            Worksheet sheet = null;
            string s = admin.id;

            if (string.Empty.Equals(file.FileName) || (".xls" != Path.GetExtension(file.FileName) && ".xlsx" != Path.GetExtension(file.FileName)))
            {
                return 4;
            }

            file.SaveAs(savePath);
            workbook.LoadFromFile(savePath);
            sheet = workbook.Worksheets[0];
            List<User> userList = new List<User>();
            int row = sheet.Rows.Length;//获取不为空的行数
            int col = sheet.Columns.Length;//获取不为空的列数
            int i;
            string tempId;
            string tempName;
            string tempEmail;
            string tempDepartment;
            int idcol = -11;
            int namecol = -11;
            int emailcol = -11;
            int idrow = -11;
            int departmentcol = -11;
            CellRange[] cellrange = sheet.Cells;
            int rangelength = cellrange.Length;
            for (i = 0; i < row; i++)
            {
                for (int j = 0; j < col; j++)
                {
                    tempId = cellrange[i * col + j].Value;
                    if (tempId.Equals("学号") || tempId.Equals("工号"))
                    {
                        idcol = j;
                        idrow = i + 1;
                    }
                    if (tempId.Equals("姓名"))
                    {
                        namecol = j;
                    }

                    if (tempId.Equals("Email")||tempId.Equals("邮箱"))
                    {
                        emailcol = j;
                    }
                    if (tempId.Equals("所在院系"))
                    {
                        departmentcol = j;
                    }
                }
                if (idcol >= 0 && namecol >= 0)
                {
                    break;
                }
            }

            if (idcol < 0 || namecol < 0)
            {
                return 1;
            }
            if (departmentcol < 0 && admin.role == 4)
            {
                return 2;
            }
            if (departmentcol >= 0 && admin.role == 3)
            {
                for (i = idrow; i < row; i++)
                {
                    tempDepartment = cellrange[i * col + departmentcol].Value;
                    if (tempDepartment != null && tempDepartment != "" && tempDepartment != admin.department_id)
                    {
                        return 3;
                    }
                }
            }
            if (role == 3 && admin.role == 3)
            {
                return 5;
            }

            string departmentId;
            if (admin.role == 3)
            {
                departmentId = admin.department_id;
            }
            else
            {
                departmentId = cellrange[idrow * col + departmentcol].Value.Split(' ')[0];
            }

            string defaultPasswd = ConfigurationManager.AppSettings["DefaultUserPasswd"];

            for (i = idrow; i < row; i++)
            {
                tempId = cellrange[i * col + idcol].Value;
                tempName = cellrange[i * col + namecol].Value;
                tempEmail = cellrange[i * col + emailcol].Value;
                if (tempName != "")
                {
                    user = new User();
                    user.id = tempId;
                    user.name = tempName.ToLower();
                    user.nick_name = null;
                    user.email = tempEmail;
                    user.passwd = defaultPasswd;
                    user.department_id = departmentId;
                    user.role = role;
                    user.is_accept = false;

                    userList.Add(user);
                }
            }

            UserDao.AddUser(userList);
            return 0;
        }

        /// <summary>
        /// 批量增加学生用户（超管，系统管理员）
        /// </summary>
        /// <returns></returns>
        [Route("admin/batchAddStudent")]
        public HttpResponseMessage BatchAddStudent()
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
                
                //string id = "admin";
                User user = UserDao.GetUserById(id);
                if (user.role != 4 && user.role != 3)
                {
                    return new Response(2002).Convert();
                }

                HttpFileCollection files = HttpContext.Current.Request.Files;
                HttpPostedFile file = files[0];

                int result = batchCreateUsers(user, new HttpPostedFileWrapper(file) as HttpPostedFileBase, 1);

                if (result != 0)
                {
                    return new Response(2002,"文件可能有不是xls，缺少id/名字列，缺少所在院系等问题").Convert();
                }

                return new Response(1001, "添加成功").Convert();

            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }

        }

        /// <summary>
        /// 批量增加教师用户（超管，系统管理员）
        /// </summary>
        /// <returns></returns>
        [Route("admin/batchAddTeacher")]
        public HttpResponseMessage BatchAddTeacher()
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
                if (user.role != 4 && user.role != 3)
                {
                    return new Response(2002).Convert();
                }

                HttpFileCollection files = HttpContext.Current.Request.Files;
                HttpPostedFile file = files[0];

                int result = batchCreateUsers(user, new HttpPostedFileWrapper(file) as HttpPostedFileBase, 2);

                if (result != 0)
                {
                    return new Response(2002, "文件可能有不是xls，缺少id/名字列，缺少所在院系等问题").Convert();
                }

                return new Response(1001, "添加成功").Convert();

            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }

        }

        /// <summary>
        /// 批量增加系管理员用户（超管）
        /// </summary>
        /// <returns></returns>
        [Route("admin/batchAddDepartmentAdmin")]
        public HttpResponseMessage BatchAddDepartmentAdmin()
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
                if (user.role != 4 && user.role != 3)
                {
                    return new Response(2002).Convert();
                }

                HttpFileCollection files = HttpContext.Current.Request.Files;
                HttpPostedFile file = files[0];

                int result = batchCreateUsers(user, new HttpPostedFileWrapper(file) as HttpPostedFileBase, 3);

                if (result != 0)
                {
                    return new Response(2002, "文件可能有不是xls，缺少id/名字列，缺少所在院系等问题").Convert();
                }

                return new Response(1001, "添加成功").Convert();

            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }

        }

        /// <summary>
        /// 删除用户
        /// </summary>
        /// <param name="userAccount"></param>
        /// <returns></returns>
        [Route("admin/deleteUsers"),HttpPost]
        public HttpResponseMessage DeleteUser([FromBody]JObject userAccount)
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
                
                //string id = "16211084";
                User user = UserDao.GetUserById(id);
                if (user.role != 3 && user.role != 4)
                {
                    return new Response(2002).Convert();
                }

                var jsonParams = HttpUtil.Deserialize(userAccount);
                List<string> userIds = jsonParams.userid.ToObject<List<string>>();

                List<User> users = UserDao.FindUserByIds(userIds);

                if (user.role == 3)//院系管理员不能删院系管理员，系外教师和学生
                {
                    if (users.Exists(u => u.role >= 3))
                    {
                        return new Response(2002).Convert();
                    }
                    if (users.Exists(u => (u.role <= 2 && u.department_id != user.department_id)))
                    {
                        return new Response(2002).Convert();
                    }
                }

                UserDao.DeleteUser(users);
                return new Response(1001, "删除成功").Convert();
                
            }
            catch(Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
                //return new Response(4001, e.Message).Convert();
            }
        }

        /// <summary>
        /// 管理员更改用户信息 by jyf
        /// </summary>
        /// <param name="account">带用户id的用户信息，参数名参照数据库</param>
        /// <returns></returns>
        [Route("admin/changeUserInfo"),HttpPost]
        public HttpResponseMessage AdminChangeUserInfo([FromBody]JObject account)
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

                //string id = "16211084";
                User user = UserDao.GetUserById(id);
                if (user.role != 3 && user.role != 4)
                {
                    return new Response(2002).Convert();
                }

                User toAdd = new User();
                QuickCopy.Copy(account, ref toAdd);

                User target = UserDao.GetUserById(toAdd.id);

                if(user.role == 3 && (target.role >= 3 || user.department_id != target.department_id))
                {
                    return new Response(2002).Convert();
                }

                UserDao.ChangeInfo(toAdd);
                return new Response(1001, "修改成功").Convert();
            }
            catch(Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }

        /// <summary>
        /// 管理员更改用户密码 by jyf
        /// </summary>
        /// <param name="account">id,passwd，参数名参照数据库</param>
        /// <returns></returns>
        [Route("admin/changeUserPassword"), HttpPost]
        public HttpResponseMessage AdminChangeUserPassword([FromBody]JObject account)
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
                
                //string id = "16211084";
                User user = UserDao.GetUserById(id);
                if (user.role != 3 && user.role != 4)
                {
                    return new Response(2002).Convert();
                }

                User toAdd = new User();
                QuickCopy.Copy(account, ref toAdd);

                User target = UserDao.GetUserById(toAdd.id);

                if (user.role == 3 && (target.role >= 3 || user.department_id != target.department_id))
                {
                    return new Response(2002).Convert();
                }

                UserDao.ChangeInfo(toAdd);
                return new Response(1001, "修改成功").Convert();
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }

        [Route("admin/listData"), HttpGet]
        public HttpResponseMessage ListTerm()
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
                



                return new Response(1001, "获取数据成功", DataUtil.GetDatas()).Convert();
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }


    }
}