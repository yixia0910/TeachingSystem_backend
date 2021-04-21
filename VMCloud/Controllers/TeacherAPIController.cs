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
using System.Configuration;

namespace VMCloud.Controllers
{
    //[RoutePrefix("api")]
    public class TeacherAPIController : ApiController
    {

        private RedisHelper redis;

        /// <summary>
        /// 初始化Helper
        /// </summary>
        TeacherAPIController()
        {
            redis = RedisHelper.GetRedisHelper();
        }

        /// <summary>
        /// 文档第3项
        /// 教师添加实验/作业 api
        /// created by jyf 
        /// 2019.7.28
        /// </summary>
        /// <param name="account"></param>
        /// <returns>json</returns>
        /// {
        ///     "code":"",
        ///     "msg":""
        /// }
        [Route("teacher/addWorking"), HttpPost]
        public HttpResponseMessage AddExperiment([FromBody]JObject account)
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                var jsonParams = HttpUtil.Deserialize(account);
                int courseId = jsonParams.course_id;
                
                bool login = redis.IsSet(signature);
                if (!login)
                {
                    return new Response(2001, "未登录账户").Convert();
                }

                string id = redis.Get<string>(signature);
                
                //string id = "16211084";
                User user = UserDao.GetUserById(id);
                Course course = CourseDao.GetCourseInfoById(courseId);

                if (user.role != 4 && (user.role == 3 && user.department_id != course.department_id) && (user.role == 2 && user.id != course.teacher_id)&& (CourseDao.GetAssistantsByCourseId((courseId)).Where(a => a.student_id == user.id).Count() == 0))
                {
                    return new Response(2002, "无权限添加该实验/作业").Convert();
                }


                Experiment experiment = new Experiment();
                QuickCopy.Copy(account, ref experiment);
                experiment.create_time = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                experiment.peer_assessment_start = false;
                /**
                experiment.course_id = jsonParams.courseID;
                experiment.name = jsonParams.experimentName;
                experiment.start_time = jsonParams.beginTime;
                experiment.end_time = jsonParams.endTime;
                experiment.deadline = jsonParams.delayTime;
                experiment.detail = jsonParams.experimentDescription;
                experiment.is_peer_assessment = jsonParams.enablePeerReview;
                experiment.peer_assessment_deadline = jsonParams.peerReviewDeadline;
                experiment.peer_assessment_rules = jsonParams.peerReviewDescription;
                experiment.appeal_deadline = jsonParams.appealDeadline;
                experiment.create_time = DateTime.Now.ToString();
                experiment.peer_assessment_start = jsonParams.delayTime;//TODO：互评开始时间？先设置成ddl后了
                experiment.type = false;// TODO:实验是false？
                **/
                int res = ExperimentDao.AddExperiment(experiment);
                LogUtil.Log(Request, "添加实验", experiment.id.ToString(), id, user.role);
                if (res != 0)
                {
                    return new Response(1001, "添加信息成功").Convert();
                }
                else
                {
                    return new Response(1001, "添加信息失败").Convert();
                }

            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }
        /// <summary>
        /// 创建实验
        /// </summary>
        /// <returns></returns>
        [Route("teacher/createExp"), HttpPost]
        public HttpResponseMessage CreateExperiment()
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
                HttpContextBase context = (HttpContextBase)Request.Properties["MS_HttpContext"];
                HttpRequestBase httpRequest = context.Request;

                int courseId = int.Parse(httpRequest["course_id"]);
                Experiment newExp = new Experiment
                {
                    course_id = courseId,
                    name = httpRequest["name"],
                    vm_status = Convert.ToInt32(httpRequest["vm_status"]),
                    type = httpRequest["type"] == "true",
                    detail = httpRequest["detail"],
                    create_time = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
                    start_time = httpRequest["start_time"],
                    end_time = httpRequest["end_time"],
                    deadline = httpRequest["deadline"],
                    is_peer_assessment = httpRequest["is_peer_assessment"] == "true",
                    appeal_deadline = httpRequest["appeal_deadline"],
                    peer_assessment_deadline = httpRequest["peer_assessment_deadline"],
                    peer_assessment_rules = httpRequest["peer_assessment_rules"],
                    peer_assessment_start = false
                };
                User user = UserDao.GetUserById(id);
                Course course = CourseDao.GetCourseInfoById(courseId);

                if (user.role != 4 && (user.role == 3 && user.department_id != course.department_id) && (user.role == 2 && user.id != course.teacher_id) && (CourseDao.GetAssistantsByCourseId((courseId)).Where(a => a.student_id == user.id).Count() == 0))
                {
                    return new Response(2002, "无权限添加该实验/作业").Convert();
                }
                if (httpRequest.Files.Count > 0)
                {
                    string path = httpRequest.MapPath(@"~/Files/Resource/");
                    path = path + courseId + course.name;
                    if (!System.IO.Directory.Exists(path))
                        System.IO.Directory.CreateDirectory(path);
                    var postedFile = httpRequest.Files[0];

                    File newFile = HttpUtil.UploadFile(postedFile, id, "资源", path, Request);
                    AssignmentDao.AddFile(newFile);
                    newExp.resource = newFile.id;
                } else if (httpRequest["resource"] != null)
                    newExp.resource = httpRequest["resource"];

                int newExpId = ExperimentDao.AddExperiment(newExp);
                if (newExpId != 0)
                {
                    Experiment experiment = ExperimentDao.GetExperimentById(newExpId);
                    return new Response(1001, "", experiment).Convert();
                }
                else
                    throw new Exception("创建实验失败" + newExpId);
            }
            catch(Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }

        }
        /// <summary>
        /// 下载实验资源
        /// </summary>
        /// <returns></returns>
        [Route("downloadExpResource"), HttpGet]
        public HttpResponseMessage DownloadExpResource()
        {
            try
            {
                var jsonParams = Request.GetQueryNameValuePairs().ToDictionary(k => k.Key, v => v.Value);
                int expId = Convert.ToInt32(jsonParams["expId"]);
                Experiment exp = ExperimentDao.GetExperimentById(expId);
                if (exp == null)
                    return new Response(2002, "未找到该实验").Convert();
                File file = AssignmentDao.GetFileById(exp.resource);
                if (file == null)
                    return new Response(2002, "无资源文件").Convert();
                return HttpUtil.DownloadFile(file.path, file.name);
            }
            catch(Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }

        [Route("teacher/downloadAllAssignment"),HttpGet]
        public HttpResponseMessage DownloadAllAssignment()
        {
            try
            {
                var jsonParams = Request.GetQueryNameValuePairs().ToDictionary(k => k.Key, v => v.Value);
                string uuid = jsonParams["uuid"];
                if (!redis.IsSet(uuid))
                {
                    return new Response(2002, "文件下载请求不存在").Convert();
                }
                string jsonString = redis.Get<string>(uuid);
                dynamic file = JsonConvert.DeserializeObject(jsonString);

                HttpResponseMessage res = HttpUtil.DownloadFile(file.path.ToString(), file.name.ToString());
                //TODO: 将使用完的临时文件删除。使用后还会被占用，因此不能立刻删除
                //System.IO.File.Delete(file.path.ToString());
                //System.IO.Directory.Delete(file.directory.ToString(), true);
                redis.Delete(uuid);
                return res;
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }
        /// <summary>
        /// 获取作业打包下载uuid
        /// </summary>
        /// <returns></returns>
        [Route("teacher/tryDownloadAllAssignment"),HttpGet]
        public HttpResponseMessage TryDwonloadAllAssignment()
        {

            try
            {
                var jsonParams = Request.GetQueryNameValuePairs().ToDictionary(k => k.Key, v => v.Value);
                int expId = Convert.ToInt32(jsonParams["expId"]);
                int rename = jsonParams.ContainsKey("rename") ? Convert.ToInt32(jsonParams["rename"]) : 2; //0:不重命名 1:仅在文件名前添加学号姓名 2:完全重命名为学号_姓名_实验名
                List<Assignment> assignments = AssignmentDao.GetAssignmentsByExpId(expId);
                if (assignments.Count == 0)
                    return new Response(3001, "无作业").Convert();
                Experiment experiment = ExperimentDao.GetExperimentById(expId);
                List<User> users = CourseDao.GetStudentsById((int)experiment.course_id);
                HttpContextBase context = (HttpContextBase)Request.Properties["MS_HttpContext"];
                HttpRequestBase httpRequest = context.Request;
                string uuid = Guid.NewGuid().ToString();
                string tempZipFilePath = httpRequest.MapPath(@"~/Files/Temp/AssignmentZip/");
                if (!System.IO.Directory.Exists(tempZipFilePath + uuid))
                    System.IO.Directory.CreateDirectory(tempZipFilePath + uuid);
                tempZipFilePath = tempZipFilePath + uuid;
                string summary = "";
                foreach (var assignment in assignments)
                {
                    File file = AssignmentDao.GetFileById(assignment.file);
                    string stuName = users.Where(u => u.id.Equals(assignment.student_id)).First().name;
                    string fileName = "";
                    switch(rename)
                    {
                        case 0:
                            fileName = file.name;
                            break;
                        case 1:
                            fileName = file.uploader + "_" + stuName + "_" + file.name;
                            break;
                        case 2:
                        default:
                            fileName = file.uploader + "_" + stuName + "_" + experiment.name + "." + HttpUtil.GetFileExtensioName(file.name);
                            break;
                    }
                    System.IO.File.Copy(file.path, tempZipFilePath + "/" + fileName, true);
                    summary += "已提交\t" + file.uploader + "\t" + stuName + "\t" + file.upload_time + "\r\n";
                }
                List<User> noAssignment = new List<User>();
                foreach (var stu in users)
                {
                    if (assignments.Where(a => a.student_id == stu.id).FirstOrDefault() == null)
                    {
                        summary += "未提交\t" + stu.id + "\t" + stu.name + "\r\n";
                    }
                }
                HttpUtil.StringToFile(tempZipFilePath + "/提交情况.txt", summary);
                HttpUtil.CreateZip(tempZipFilePath, tempZipFilePath + "total.zip");
                //return HttpUtil.DownloadFile(tempZipFilePath + "/" + uuid + "total.zip", experiment.name + "作业提交汇总.zip");
                string downloadUuid = Guid.NewGuid().ToString();
                Dictionary<string, string> ret = new Dictionary<string, string>();
                ret.Add("path", tempZipFilePath + "total.zip");
                ret.Add("name", experiment.name + "_作业汇总.zip");
                ret.Add("directory", tempZipFilePath);
                redis.Set(downloadUuid, JsonConvert.SerializeObject(ret), 10);
                return new Response(1001, "获取成功", downloadUuid).Convert();
                //return HttpUtil.DownloadFile(tempZipFilePath + "/" + uuid + "total.zip", experiment.name + "_作业汇总.zip");

            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }
        /// <summary>
        /// 文档第4项
        /// 获取院系列表api
        /// created by jyf
        /// 2019.7.29
        /// </summary>
        /// <returns>json</returns>
        /// {
        ///     "code": "",
        ///     "msg": "",
        ///     "info":[
        ///         {
        ///             "name": "",
        ///             "number": ""
        ///         }
        ///     ]
        /// }
        [Route("teacher/listCollege")]
        public HttpResponseMessage ListCollege()
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

                return new Response(1001, "获取院系列表成功", CourseDao.GetDepartments()).Convert();
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }


        public class termRet
        {
            public string name;
            public int id;
            public int courseCount;
            public int expCount;
        }
        /// <summary>
        /// 文档第5项
        /// 获取学期列表api
        /// created by jyf
        /// 2019.7.30
        /// </summary>
        /// <returns>json</returns>
        /// {
        ///     "code": "",
        ///     "msg": "",
        ///     "info":[
        ///         {
        ///             "name": "",
        ///             "number": ""
        ///         }
        ///     ]
        /// }
        [Route("teacher/listTerm"),HttpGet]
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
                string id = redis.Get<string>(signature);
                User user = UserDao.GetUserById(id);
                var terms = CourseDao.GetTerms();
                terms = terms.OrderByDescending(t => t.id).ToList();
                if (user.role > 3)
                {
                    List<termRet> ret = new List<termRet>();
                    List<Course> courses = CourseDao.GetAllCourse();

                    foreach(var t in terms)
                    {
                        termRet tr = new termRet
                        {
                            name = t.name,
                            id = t.id,
                            courseCount = courses.Where(c => c.term_id == t.id).Count()
                        };
                        var exps = ExperimentDao.GetExperimentByTermId(t.id);
                        tr.expCount = exps.Count;
                        ret.Add(tr);
                    }
                    return new Response(1001, "获取学期成功", ret).Convert();
                }
                return new Response(1001, "获取学期成功", terms).Convert();
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }

        /// <summary>
        /// 获取教师课程by教师id by zzw
        /// 2019.7.22
        /// </summary>
        /// <returns>
        /// {
        ///     "code":,
        ///     "msg":"",
        ///     "data":{
        ///         'id': '',
        ///         'name': '',
        ///         'semester': '',
        ///         'department': ''
        ///     }
        /// }
        /// </returns>
        /// 
        [Route("teacher/courseInfoByProId"), HttpGet]
        public HttpResponseMessage GetCourseListByProId()
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
                bool isLogin = redis.IsSet(signature);
                if (!isLogin)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string targetId = redis.Get<string>(signature);
                List<Course> courseList = CourseDao.GetCoursesByProfessorId(targetId).ToList();
                List<Assistant> assistant = CourseDao.GetAssistantsByStuId(targetId).ToList();
                foreach(Assistant a in assistant)
                {
                    Course c = new Course();
                    c = CourseDao.GetCourseInfoById(a.course_id);
                    courseList.Add(c);
                }
                foreach (Course c in courseList)
                {
                    courseInfo = new Dictionary<string, string>
                    {
                        { "id", c.id.ToString() },
                        { "name", c.name },
                    };
                    if (c.term_id == null)
                    {
                        courseInfo.Add("term", "");
                        courseInfo.Add("term_id", "");
                    }
                    else
                    {
                        courseInfo.Add("term", CourseDao.GetTermById((int)c.term_id).name);
                        courseInfo.Add("term_id", c.term_id.ToString());
                    }
                    courseInfo.Add("department", CourseDao.GetDepartmentById(c.department_id).name);
                    courseInfo.Add("department_id", c.department_id.ToString());
                    retData.Add(courseInfo);
                }
                retData.Sort((x, y) => -int.Parse(x["id"]).CompareTo(int.Parse(y["id"])));
                return new Response(1001, "获取成功", retData).Convert();
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }

        /// <summary>
        /// 添加课程 by zzw
        /// 2019.8.1
        /// </summary>
        /// <param name="NewCourse">name、department_id、term_id</param>
        /// <returns>
        /// {
        ///     "code":,
        ///     "msg":"",
        /// }
        /// </returns>
        /// 
        [Route("teacher/addCourse"), HttpPost]
        public HttpResponseMessage AddCourse([FromBody]JObject NewCourse)
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                var jsonParams = HttpUtil.Deserialize(NewCourse);
                bool isLogin = redis.IsSet(signature);
                if (!isLogin)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string targetId = redis.Get<string>(signature);
                if(UserDao.GetUserById(targetId).role != 2)
                {
                    return new Response(2002, "无权限访问").Convert();
                }
                Course course = new Course();
                QuickCopy.Copy(NewCourse, ref course);
                course.teacher_id = targetId;
                if(CourseDao.GetCoursesByProfessorId(targetId).Where(c => c.name.Equals(course.name)&&c.term_id == course.term_id).ToList().Count != 0)
                {
                    return new Response(1001, "课程已经存在").Convert();
                }
                if(course.department_id == null)
                {
                    User user = UserDao.GetUserById(targetId);
                    course.department_id = user.department_id;
                }
                course.create_time = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                CourseDao.AddCourse(course);
                LogUtil.Log(Request, "添加课程", course.id.ToString(), targetId, 2);
                return new Response(1001, "添加课程成功").Convert();
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }
        /// <summary>
        /// 删除课程 by zzw
        /// 2019.8.1
        /// </summary>
        /// <param name="account">courseID</param>
        /// <returns>
        /// {
        ///     "code":,
        ///     "msg":"",
        /// }
        /// </returns>
        /// 
        [Route("teacher/deleteCourse"), HttpPost]
        public HttpResponseMessage DeleteCourse([FromBody]JObject account)
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                var jsonParams = HttpUtil.Deserialize(account);
                int courseId = jsonParams.courseID;
                bool isLogin = redis.IsSet(signature);
                if (!isLogin)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string targetId = redis.Get<string>(signature);
                Course course = CourseDao.GetCourseInfoById(courseId);
                if (course.teacher_id != targetId )
                {
                    return new Response(2002, "无权限删除").Convert();
                }
                List<Experiment> expertmentlist = ExperimentDao.GetExperimentByCourseId(courseId);
                if (expertmentlist.Count != 0)
                {
                    return new Response(1002, "该课程尚存实验，无法删除",expertmentlist).Convert();
                }
                if (CourseDao.GetStudentsNumByCourseId(courseId) !=0)
                {
                    return new Response(1003, "该课程尚存学生，无法删除").Convert();
                }
                if (VMDao.GetCourseVMs(courseId).Count != 0)
                {
                    return new Response(1004, "该课程尚存虚拟机，无法删除").Convert();
                }
                CourseDao.DeleteCourse(courseId);
                LogUtil.Log(Request, "删除课程", courseId.ToString(), targetId,  2);
                return new Response(1001, "删除课程成功").Convert();
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }


        /// <summary>
        /// 教师查看实验 api (目前依照老项目按courseid)(老项目termid没用)
        /// created by jyf 
        /// 2019.7.24
        /// </summary>
        /// <param name="account"></param>
        /// <returns>json</returns>
        /// TODO:具体是啥日后再说
        [Route("teacher/getExperiment"), HttpGet]
        public HttpResponseMessage GetExperiment([FromBody]JObject account)
        {
            try
            {
                var jsonParams = Request.GetQueryNameValuePairs().ToDictionary(k => k.Key, v => v.Value);
                int courseId = Convert.ToInt32(jsonParams["course_id"]);
                
                
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                //var jsonParams = HttpUtil.Deserialize(account);
                //int termId = Convert.ToInt32(jsonParams.termId);
                
                
                bool isLogin = redis.IsSet(signature);
                if (!isLogin)
                {
                    return new Response(2001, "未登录账户").Convert();
                }

                string id = redis.Get<string>(signature);
                
                //string id = "16211084";
                User user = UserDao.GetUserById(id);
                Course course = CourseDao.GetCourseInfoById(courseId);

                if(course == null)
                {
                    return new Response(2002, "无权限查看该实验/作业").Convert();
                }

                if (user.role != 4 && (user.role == 3 && user.department_id != course.department_id) && (user.role == 2 && user.id != course.teacher_id) && (CourseDao.GetAssistantsByCourseId((courseId)).Where(a => a.student_id == user.id).Count() == 0))
                {
                    return new Response(2002, "无权限查看该实验/作业").Convert();
                }
                var allExperiment = ExperimentDao.GetExperimentByCourseId(courseId);

                if (allExperiment.Count() == 0)
                {
                    return new Response(1001, "该课程下无实验").Convert();
                }

                List<ExperimentDao.expReturn> ret = ExperimentDao.GetExpRet(allExperiment);
                List<Dictionary<string, string>> returns = new List<Dictionary<string, string>>();
                Dictionary<string, string> temp;
                var props =  ret.First().GetType().GetProperties();
                foreach (ExperimentDao.expReturn r in ret)
                {
                    temp = new Dictionary<string, string>();
                    foreach(var pi in props)
                    {
                        var v = r.GetType().GetProperty(pi.Name).GetValue(r,null);
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
                    int con = CourseDao.GetStudentsById(course.id).Count();
                    temp.Add("assignment", AssignmentDao.GetAssignmentsByExpId(r.id).Count().ToString() + "/" + con.ToString());
                    if (r.is_peer_assessment == true)
                    {
                        //TODO:
                        //互评的完成情况dao
                        temp.Add("peerAssessment", "");

                    }
                    else
                    {
                        temp.Add("peerAssessment", "-");
                    }
                    temp.Add("vms", "无");
                    temp.Add("peerStarted", r.peer_assessment_start == true ? Convert.ToString(true) : Convert.ToString(false));
                    returns.Add(temp);
                }
                //TODO:确定返回参数
                return new Response(1001, "获取成功",returns).Convert();


            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }
        
        /// <summary>
        /// 教师查看学生分数 api (目前依照老项目按courseid)(老项目termid没用)
        /// created by yixia
        /// 2021.4.10
        /// </summary>
        /// <returns>json</returns>
        [Route("teacher/getStudentScore"), HttpGet]
        public HttpResponseMessage GetStudentScore([FromBody]JObject account)
        {
            try
            {
                var jsonParams = Request.GetQueryNameValuePairs().ToDictionary(k => k.Key, v => v.Value);
                int courseId = Convert.ToInt32(jsonParams["course_id"]);
                string studentId = jsonParams["student_id"];
                int expId = -1;
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

                string id = redis.Get<string>(signature);
                
                //string id = "16211084";
                User user = UserDao.GetUserById(id);
                Course course = CourseDao.GetCourseInfoById(courseId);

                if(course == null)
                {
                    return new Response(2002, "无权限查看该实验/作业").Convert();
                }

                if (user.role != 4 && (user.role == 3 && user.department_id != course.department_id) && (user.role == 2 && user.id != course.teacher_id) && (CourseDao.GetAssistantsByCourseId((courseId)).Where(a => a.student_id == user.id).Count() == 0))
                {
                    return new Response(2002, "无权限查看该实验/作业").Convert();
                }
                var allExperiment = ExperimentDao.GetExperimentByCourseId(courseId);

                if (allExperiment.Count() == 0)
                {
                    return new Response(1001, "该课程下无实验").Convert();
                }

                List<ExperimentDao.expReturn> ret = ExperimentDao.GetExpRet(allExperiment);
                List<Dictionary<string, string>> returns = new List<Dictionary<string, string>>();
                Dictionary<string, string> temp;
                var props =  ret.First().GetType().GetProperties();
                foreach (ExperimentDao.expReturn r in ret)
                {
                    temp = new Dictionary<string, string>();
                    
                    foreach(var pi in props)
                    {
                        var v = r.GetType().GetProperty(pi.Name).GetValue(r,null);
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
                        if (pi.Name == "id")
                        {
                            expId = Int32.Parse(value);
                        }
                    }

                    if (expId != -1)
                    {
                        Peer_assessment Pa = PeerAssessmentDao.getPeerAssessment(studentId, id, expId);
                        if(Pa != null)
                            temp.Add("score",Pa.origin_score.ToString());
                        
                    }
                    
                    int con = CourseDao.GetStudentsById(course.id).Count();
                    temp.Add("assignment", AssignmentDao.GetAssignmentsByExpId(r.id).Count().ToString() + "/" + con.ToString());
                    if (r.is_peer_assessment == true)
                    {
                        //TODO:
                        //互评的完成情况dao
                        temp.Add("peerAssessment", "");

                    }
                    else
                    {
                        temp.Add("peerAssessment", "-");
                    }
                    temp.Add("vms", "无");
                    temp.Add("peerStarted", r.peer_assessment_start == true ? Convert.ToString(true) : Convert.ToString(false));
                    returns.Add(temp);
                }
                //TODO:确定返回参数
                return new Response(1001, "获取成功",returns).Convert();


            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }

        /// <summary>
        /// 教师修改实验信息  
        /// created by jyf
        /// 2019.7.24
        /// </summary>
        /// <param name="expInfo"></param>
        /// <returns></returns>
        /// TODO:返回日后再说
        [Route("teacher/changeExperimentInfo"), HttpPost]
        public HttpResponseMessage ChangeExperimentInfo()
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

                string id = redis.Get<string>(signature);
                
                //string id = "admin";
                HttpContextBase context = (HttpContextBase)Request.Properties["MS_HttpContext"];
                HttpRequestBase httpRequest = context.Request;

                int courseId = int.Parse(httpRequest["course_id"]);
                Experiment newExp = new Experiment
                {
                    id = Convert.ToInt32(httpRequest["id"]),
                    course_id = courseId,
                    name = httpRequest["name"],
                    type = httpRequest["type"] == "true",
                    vm_status = Convert.ToInt32(httpRequest["vm_status"]),
                    detail = httpRequest["detail"],
                    create_time = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
                    start_time = httpRequest["start_time"],
                    end_time = httpRequest["end_time"],
                    deadline = httpRequest["deadline"],
                    is_peer_assessment = httpRequest["is_peer_assessment"] == "true",
                    appeal_deadline = httpRequest["appeal_deadline"],
                    peer_assessment_deadline = httpRequest["peer_assessment_deadline"],
                    peer_assessment_rules = httpRequest["peer_assessment_rules"],
                    peer_assessment_start = false
                };
                User user = UserDao.GetUserById(id);
                Course course = CourseDao.GetCourseInfoById(courseId);

                if (user.role != 4 && (user.role == 3 && user.department_id != course.department_id) && (user.role == 2 && user.id != course.teacher_id) && (CourseDao.GetAssistantsByCourseId((courseId)).Where(a => a.student_id == user.id).Count() == 0))
                {
                    return new Response(2002, "无权限添加该实验/作业").Convert();
                }
                //if (httpRequest.Files.Count > 0)
                //{
                //    string path = httpRequest.MapPath(@"~/Resource/");
                //    if (!System.IO.Directory.Exists(path + courseId))
                //        System.IO.Directory.CreateDirectory(path + courseId);
                //    var postedFile = httpRequest.Files[0];

                //    File newFile = HttpUtil.UploadFile(postedFile, id, "资源", path + courseId, Request);
                //    AssignmentDao.AddFile(newFile);
                //    newExp.resource = newFile.id;
                //}
                if (httpRequest["resource"] != null)
                    newExp.resource = httpRequest["resource"];
                ExperimentDao.ChangeExperimentInfo(newExp);
                LogUtil.Log(Request, "修改实验/作业", newExp.id.ToString(), id, user.role);
                return new Response(1001, "修改成功").Convert();


            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }
        
        /// <summary>
        /// 教师设置参考分数 
        /// created by yixia
        /// 2021.4.12
        /// </summary>
        /// <param name="expInfo"></param>
        /// <returns></returns>
        /// TODO:返回日后再说
        [Route("teacher/seScore2"), HttpPost]
        public HttpResponseMessage SetScore2()
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

                string id = redis.Get<string>(signature);
                
                //string id = "admin";
                HttpContextBase context = (HttpContextBase)Request.Properties["MS_HttpContext"];
                HttpRequestBase httpRequest = context.Request;

                int courseId = int.Parse(httpRequest["course_id"]);
                Experiment newExp = new Experiment
                {
                    id = Convert.ToInt32(httpRequest["id"]),
                    course_id = null,
                    name = null,
                    type = null,
                    vm_status = null,
                    vm_name = httpRequest["reason"],
                    vm_apply_id = Convert.ToInt32(httpRequest["score2"]),
                    detail = null,
                    create_time = null,
                    start_time = null,
                    end_time = null,
                    deadline = null,
                    is_peer_assessment = null,
                    appeal_deadline = null,
                    peer_assessment_deadline = null,
                    peer_assessment_rules = null,
                    peer_assessment_start = false
                };
                User user = UserDao.GetUserById(id);
                Course course = CourseDao.GetCourseInfoById(courseId);

                if (user.role != 4 && (user.role == 3 && user.department_id != course.department_id) && (user.role == 2 && user.id != course.teacher_id) && (CourseDao.GetAssistantsByCourseId((courseId)).Where(a => a.student_id == user.id).Count() == 0))
                {
                    return new Response(2002, "无权限添加该实验/作业").Convert();
                }
                if (httpRequest["resource"] != null)
                    newExp.resource = httpRequest["resource"];
                ExperimentDao.ChangeExperimentInfo(newExp);
                LogUtil.Log(Request, "修改实验/作业", newExp.id.ToString(), id, user.role);
                return new Response(1001, "修改成功").Convert();


            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }

        /// <summary>
        /// 教师删除实验 api
        /// created by jyf 
        /// 2019.7.28
        /// </summary>
        /// <param name="account"></param>
        /// <returns>json</returns>
        /// TODO:具体是啥日后再说
        [Route("teacher/deleteExperimentById"), HttpPost]
        public HttpResponseMessage DeleteExperimentById([FromBody]JObject account)
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                var jsonParams = HttpUtil.Deserialize(account);
                int experimentId = Convert.ToInt32(jsonParams.id);

                bool login = redis.IsSet(signature);
                if (!login)
                {
                    return new Response(2001, "未登录账户").Convert();
                }

                string id = redis.Get<string>(signature);
                User user = UserDao.GetUserById(id);
                Experiment experiment = ExperimentDao.GetExperimentById(experimentId);
                Course course = CourseDao.GetCourseInfoById((int)experiment.course_id);
                if (user.role != 4 && (user.role == 3 && user.department_id != course.department_id) && (user.role == 2 && user.id != course.teacher_id) && (CourseDao.GetAssistantsByCourseId((course.id)).Where(a => a.student_id == user.id).Count() == 0))
                {
                    return new Response(2002, "无权限修改该实验/作业").Convert();
                }

                int res = ExperimentDao.DeleteExperimentById(experimentId);
                LogUtil.Log(Request, "删除实验/作业", experimentId.ToString(), id, user.role);
                return new Response(1001, "删除成功").Convert();


            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }

        /// <summary>
        /// 将不存在系统的学生添加进来
        /// </summary>
        /// <param name="addInfo">学生id</param>
        /// <returns>json</returns>
        [Route("teacher/addStuToSystem"),HttpPost]
        public HttpResponseMessage AddStuToSystem([FromBody]JObject addInfo)
        {
            try
            {

                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                var jsonParams = HttpUtil.Deserialize(addInfo);
                int courseId = jsonParams.courseID;
                
                bool login = redis.IsSet(signature);
                if (!login)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string id = redis.Get<string>(signature);
                User user = UserDao.GetUserById(id);
                Course course = CourseDao.GetCourseInfoById(courseId);
                if ((user.role == 2 && user.id == course.teacher_id) || (user.role == 1 && CourseDao.GetAssistantsByCourseId((courseId)).Where(a => a.student_id == user.id).Count() != 0))
                {
                    JArray tempJArray = (JArray)jsonParams.sid;
                    List<string> stuIdList = tempJArray.ToObject<List<string>>();
                    List<User> toAddUsers = new List<User>();
                    foreach (var sid in stuIdList)
                    {
                        User newUser = new User
                        {
                            id = sid,
                            name = "待激活用户",
                            email = "waitinput",
                            department_id = user.department_id,
                            role = 1,
                            passwd = ConfigurationManager.AppSettings["DefaultUserPasswd"]
                        };
                        toAddUsers.Add(newUser);
                    }

                    UserDao.AddUser(toAddUsers);
                    Course_student_mapping map = new Course_student_mapping();
                    int tempres = 0;
                    int totalres = 0;
                    List<string> repeatedList = new List<string>();
                    foreach(var sid in stuIdList)
                    {
                        map.course_id = courseId;
                        map.student_id = sid;
                        if (UserDao.GetUserById(map.student_id) == null || UserDao.GetUserById(map.student_id).role != 1)
                        {
                            string ret = map.student_id.ToString();

                            return new Response(1001, "添加信息失败", ret).Convert();
                        }
                        tempres = CourseDao.AddMap(map);
                        if (tempres == 1)
                        {
                            totalres++;
                        }
                        else if (tempres == 0)
                        {
                            repeatedList.Add(map.student_id);
                        }

                    }
                    if (totalres != 0)
                    {
                        string content = "成功添加" + totalres + "个学生";
                        if (repeatedList.Count != 0)
                        {
                            string repeat = "";
                            for (int i = 0; i < repeatedList.Count - 1; i++)
                            {
                                repeat += repeatedList[i] + "，";
                            }
                            repeat += repeatedList.Last() + "。";
                            content += "," + "重复添加的学生有" + repeat;
                        }
                        LogUtil.Log(Request, "添加学生到课程", courseId.ToString(), id, user.role, "success", content);
                        return new Response(1001, "添加信息成功", content).Convert();
                    }
                    else
                    {
                        return new Response(1001, "添加信息失败").Convert();
                    }
                }
                else
                {
                    return new Response(2002, "无权限添加该实验/作业").Convert();
                }
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }

        }
        /// <summary>
        /// 教师助教添加学生进课程 api
        /// created by zzw 
        /// 2019.8.27
        /// </summary>
        /// <param name="addInfo">courseID,stuidList</param>
        /// <returns>json</returns>
        /// {
        ///     "code":"",
        ///     "msg":""
        /// }
        [Route("teacher/addStusToCourse"), HttpPost]
        public HttpResponseMessage AddStusToCourse([FromBody]JObject addInfo)
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                var jsonParams = HttpUtil.Deserialize(addInfo);
                int courseId = jsonParams.courseID;
                dynamic list = jsonParams.stuidList;
                List<string> stuidList = list.ToObject<List<string>>();

                bool login = redis.IsSet(signature);
                if (!login)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string id = redis.Get<string>(signature);
                User user = UserDao.GetUserById(id);
                Course course = CourseDao.GetCourseInfoById(courseId);
                if ((user.role == 2 && user.id == course.teacher_id) || CourseDao.GetAssistantsByCourseId((courseId)).Where(a => a.student_id == user.id).Count() != 0)
                {
                    Course_student_mapping map = new Course_student_mapping();
                    int tempres = 0;
                    int totalres = 0;
                    List<string> repeatedList = new List<string>();
                    List<string> notInSystemList = new List<string>();
                    string content = "";
                    for (int i = 0; i < stuidList.Count(); i++)
                    {
                        map.course_id = courseId;
                        map.student_id = stuidList[i];
                        if(UserDao.GetUserById(map.student_id) == null|| UserDao.GetUserById(map.student_id).role != 1)
                        {
                            notInSystemList.Add(map.student_id);
                            //string ret = map.student_id.ToString();
                            //return new Response(1001, "添加信息失败" ,ret).Convert();
                        }
                        if (CourseDao.AddMap(map) == 1)
                            totalres++;
                        else
                            repeatedList.Add(map.student_id);
                    }
                    if (totalres != 0)
                    {
                        content = "成功添加" + totalres + "个学生";
                        if (repeatedList.Count != 0)
                        {
                            string repeat = "";
                            for (int i = 0; i < repeatedList.Count - 1; i++)
                            {
                                repeat += repeatedList[i] + "，";
                            }
                            repeat += repeatedList.Last() + "。";
                            content += "," + "重复添加的学生有" + repeat;
                        }
                    }
                    if (notInSystemList.Count > 0)
                    {
                        return new Response(1001, "部分添加失败。" + content, notInSystemList).Convert();
                    } else if (totalres > 0)
                    {
                        LogUtil.Log(Request, "添加学生到课程", courseId.ToString(), id, user.role, "success", content);
                        return new Response(1001, "添加信息成功", content).Convert();
                    }
                    else
                    {
                        return new Response(1001, "添加信息失败").Convert();
                    }
                }
                else
                {
                    return new Response(2002, "无权限添加该实验/作业").Convert();
                }

            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }

        //[Route("teacher/addStusToCourse"), HttpPost]
        //public HttpResponseMessage AddStusToCourse([FromBody]JObject addInfo)
        //{
        //    try
        //    {
        //        string signature = HttpUtil.GetAuthorization(Request);
        //        if (signature == null || !redis.IsSet(signature))
        //        {
        //            return new Response(2001, "未登录账户").Convert();
        //        }
        //        var jsonParams = HttpUtil.Deserialize(addInfo);
        //        int courseId = jsonParams.courseID;
        //        dynamic list = jsonParams.stuidList;
        //        List<string> stuidList = list.ToObject<List<string>>();

        //        bool login = redis.IsSet(signature);
        //        if (!login)
        //        {
        //            return new Response(2001, "未登录账户").Convert();
        //        }
        //        string id = redis.Get<string>(signature);
        //        User user = UserDao.GetUserById(id);
        //        Course course = CourseDao.GetCourseInfoById(courseId);
        //        if ((user.role == 2 && user.id == course.teacher_id) || (user.role == 1 && CourseDao.GetAssistantsByCourseId((courseId)).Where(a => a.student_id == user.id).Count() != 0))
        //        {
        //            Course_student_mapping map = new Course_student_mapping();
        //            int tempres = 0;
        //            int totalres = 0;
        //            List<string> repeatedList = new List<string>();
        //            for (int i = 0; i < stuidList.Count(); i++)
        //            {
        //                map.course_id = courseId;
        //                map.student_id = stuidList[i];
        //                if (UserDao.GetUserById(map.student_id) == null || UserDao.GetUserById(map.student_id).role != 1)
        //                {
        //                    string ret = "学号为" + map.student_id.ToString() + "的学生不存在";
        //                    return new Response(1001, "添加信息失败", ret).Convert();
        //                }
        //                tempres = CourseDao.AddMap(map);
        //                if (tempres == 1)
        //                {
        //                    totalres++;
        //                }
        //                else if (tempres == 0)
        //                {
        //                    repeatedList.Add(map.student_id);
        //                }

        //            }
        //            if (totalres != 0)
        //            {
        //                string content = "成功添加" + totalres + "个学生";
        //                if (repeatedList.Count != 0)
        //                {
        //                    string repeat = "";
        //                    for (int i = 0; i < repeatedList.Count - 1; i++)
        //                    {
        //                        repeat += repeatedList[i] + "，";
        //                    }
        //                    repeat += repeatedList.Last() + "。";
        //                    content += "," + "重复添加的学生有" + repeat;
        //                }
        //                LogUtil.Log(Request, "添加学生到课程", courseId.ToString(), id, user.role, "success", content);
        //                return new Response(1001, "添加信息成功", content).Convert();
        //            }
        //            else
        //            {
        //                return new Response(1001, "添加信息失败").Convert();
        //            }
        //        }
        //        else
        //        {
        //            return new Response(2002, "无权限添加该实验/作业").Convert();
        //        }

        //    }
        //    catch (Exception e)
        //    {
        //        ErrorLogUtil.WriteLogToFile(e, Request);
        //        return Response.Error();
        //    }
        //}
        /// <summary>
        /// 删除某课程的学生 api
        /// created by zzw 
        /// 2019.9.23
        /// </summary>
        /// <param name="map">courseID,stuID</param>
        /// <returns>json</returns>
        /// {
        ///     "code":"",
        ///     "msg":""
        /// }
        [Route("teacher/deleteStuFromCourse"), HttpPost]
        public HttpResponseMessage DeleteStuFromCourse([FromBody]JObject map)
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                var jsonParams = HttpUtil.Deserialize(map);
                int courseId = Convert.ToInt32(jsonParams.courseID);
                string stuid = jsonParams.stuID;
                bool login = redis.IsSet(signature);
                if (!login)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string id = redis.Get<string>(signature);
                User user = UserDao.GetUserById(id);
                Course course = CourseDao.GetCourseInfoById(courseId);
                if (user.role != 4 && (user.role == 3 && user.department_id != course.department_id) && (user.role == 2 && user.id != course.teacher_id) && (CourseDao.GetAssistantsByCourseId((course.id)).Where(a => a.student_id == user.id).Count() == 0))
                {
                    return new Response(2002, "无权删除该课程的学生").Convert();
                }
                if(CourseDao.GetStudentsById(courseId).Exists(s=>s.id == stuid))
                {
                    CourseDao.DeleteMapByStudentIdAndCourseId(stuid, courseId);
                    LogUtil.Log(Request, "删除学生",stuid , id, user.role);
                    return new Response(1001, "删除成功").Convert();
                }
                else
                {
                    return new Response(1002, "该课程不存在这个学生").Convert();
                }
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }

        /// <summary>
        /// 删除某课程的全部学生 api
        /// created by zzw 
        /// 2019.9.23
        /// </summary>
        /// <param name="courseinfo">courseID</param>
        /// <returns>json</returns>
        /// {
        ///     "code":"",
        ///     "msg":""
        /// }
        [Route("teacher/deleteAllStuFromCourse"), HttpPost]
        public HttpResponseMessage DeleteAllStuFromCourse([FromBody]JObject courseinfo)
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                var jsonParams = HttpUtil.Deserialize(courseinfo);
                int courseId = Convert.ToInt32(jsonParams.courseID);
                bool login = redis.IsSet(signature);
                if (!login)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string id = redis.Get<string>(signature);
                User user = UserDao.GetUserById(id);
                Course course = CourseDao.GetCourseInfoById(courseId);
                if (user.role != 4 && (user.role == 3 && user.department_id != course.department_id) && (user.role == 2 && user.id != course.teacher_id) && (CourseDao.GetAssistantsByCourseId((course.id)).Where(a => a.student_id == user.id).Count() == 0))
                {
                    return new Response(2002, "无权删除该课程的学生").Convert();
                }
                int res = CourseDao.DeleteMapByCourseId(courseId);
                LogUtil.Log(Request, "删除课程的所有学生", courseId.ToString(), id, user.role);
                return new Response(1001, "删除成功").Convert();
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }
        /// <summary>
        /// 教师为课程添加助教 api
        /// created by zzw 
        /// 2019.8.27
        /// </summary>
        /// <param name="addInfo">courseID,stuid</param>
        /// <returns>json</returns>
        /// {
        ///     "code":"",
        ///     "msg":""
        /// }
        [Route("teacher/addAstToCourse"), HttpPost]
        public HttpResponseMessage AddAstToCourse([FromBody]JObject addInfo)
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                var jsonParams = HttpUtil.Deserialize(addInfo);
                int courseId = jsonParams.courseID;
                string stuid = jsonParams.stuid;
                bool login = redis.IsSet(signature);
                if (!login)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string id = redis.Get<string>(signature);
                User user = UserDao.GetUserById(id);
                Course course = CourseDao.GetCourseInfoById(courseId);
                if (user.id == course.teacher_id || (user.role == 2 && CourseDao.GetAssistantsByCourseId((courseId)).Where(a => a.student_id == user.id).Count() != 0))
                {
                    if (UserDao.GetUserById(stuid) == null)
                        return new Response(1002, "学工号不存在").Convert();
                    Assistant ast = new Assistant();
                    ast.course_id = courseId;
                    ast.student_id = stuid;
                    ast.create_time = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                    int res = UserDao.AddAstToCourse(ast);
                    if (res == 1)
                    {
                        return new Response(1001, "添加成功").Convert();
                    }
                    else
                    {
                        return new Response(1001, "无需重复添加").Convert();
                    }
                }
                else
                {
                    return new Response(2002, "无权限为该课程添加助教").Convert();
                }
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }
        /// <summary>
        /// 教师为课程删除助教 api
        /// created by zzw 
        /// 2019.9.28
        /// </summary>
        /// <param name="deleteInfo">courseID,stuid</param>
        /// <returns>json</returns>
        /// {
        ///     "code":"",
        ///     "msg":""
        /// }
        [Route("teacher/deleteAstFromCourse"), HttpPost]
        public HttpResponseMessage DeleteAstFromCourse([FromBody]JObject deleteInfo)
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                var jsonParams = HttpUtil.Deserialize(deleteInfo);
                int courseId = jsonParams.courseID;
                string stuid = jsonParams.stuid;
                bool login = redis.IsSet(signature);
                if (!login)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string id = redis.Get<string>(signature);
                User user = UserDao.GetUserById(id);
                Course course = CourseDao.GetCourseInfoById(courseId);
                if (user.id == course.teacher_id || (user.role == 2 && CourseDao.GetAssistantsByCourseId((courseId)).Where(a => a.student_id == user.id).Count() != 0))
                {
                    User toDelete = UserDao.GetUserById(stuid);
                    if (toDelete.role > 1)
                    {
                        if (user.id != course.teacher_id)
                            return new Response(2002, "无权限管理其他指导教师").Convert();
                    }
                    Assistant ast = new Assistant();
                    ast.course_id = courseId;
                    ast.student_id = stuid;
                    int res = UserDao.DeleteAstFromCourse(ast);
                    if (res == 1)
                    {
                        return new Response(1001, "删除助教成功").Convert();
                    }
                    else
                    {
                        return new Response(1001, "所删除的助教不存在").Convert();
                    }
                }
                else
                {
                    return new Response(2002, "无权限为该课程添加助教").Convert();
                }
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }
        /// <summary>
        /// 教师获取助教列表 by jyf
        /// </summary>
        /// <returns>json
        /// :code,msg,data["stulsit","teaList"]{id,name,course,term}
        /// </returns>
        /// <returns></returns>
        [Route("teacher/astInfo"),HttpGet]
        public HttpResponseMessage GetAssistant()
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
                List<Dictionary<string, string>> retStu = new List<Dictionary<string, string>>();
                List<Dictionary<string, string>> retTea = new List<Dictionary<string, string>>();
                Dictionary<string, List<Dictionary<string, string>>> ret = new Dictionary<string, List<Dictionary<string, string>>>();
                Dictionary<string, string> retData;
                User ast;
                if (user.role < 2)
                {
                    return new Response(2002, "无权限查看助教").Convert();
                }
                List<Course> courses = CourseDao.GetCoursesByProfessorId(id);
                List<Assistant> teacher_assistants = CourseDao.GetAssistantsByStuId(id);
                foreach(var a in teacher_assistants)
                {
                    Course c = CourseDao.GetCourseInfoById(a.course_id);
                    if (c != null)
                        courses.Add(CourseDao.GetCourseInfoById(a.course_id));
                }
                foreach (Course c in courses)
                {
                    List<Assistant> assistants = CourseDao.GetAssistantsByCourseId(c.id);
                    foreach (Assistant a in assistants)
                    {
                        ast = UserDao.GetUserById(a.student_id);
                        retData = new Dictionary<string, string>();
                        if (ast != null && ast.role == 1)
                        {
                            retData.Add("id", a.student_id);
                            retData.Add("name", ast.name);
                            retData.Add("course", c.name);
                            retData.Add("term", CourseDao.GetTermById((int)c.term_id).name);
                            retData.Add("create_time", a.create_time);
                            retData.Add("course_id", c.id.ToString());
                            retStu.Add(retData);
                        }
                        else if (ast != null && ast.role == 2)
                        {
                            retData = new Dictionary<string, string>();
                            retData.Add("id", a.student_id);
                            retData.Add("name", ast.name);
                            retData.Add("course", c.name);
                            retData.Add("term", CourseDao.GetTermById((int)c.term_id).name);
                            retData.Add("create_time", a.create_time);
                            retData.Add("course_id", c.id.ToString());
                            retTea.Add(retData);
                        }
                    }
                }
                ret.Add("stulsit", retStu);
                ret.Add("teaList", retTea);
                return new Response(1001, "成功", ret).Convert();
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }
        /// <summary>
        /// 获取当前登录学生账号的助教课程列表
        /// </summary>
        /// <returns></returns>
        [Route("student/astInfo"),HttpGet]
        public HttpResponseMessage GetAssistantCourse()
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
                List<int> courseIds = CourseDao.GetAssistantsByStuId(id).Select(a => a.course_id).ToList();
                if(courseIds.Count < 1)
                {
                    return new Response(2002).Convert();
                }
                List<Course> courses = new List<Course>();
                List<Dictionary<string, string>> retData = new List<Dictionary<string, string>>();
                Dictionary<string, string> astCourse;
                Dictionary<string, string> idNamePairs = new Dictionary<string, string>();
                Dictionary<int, string> idTermPairs = new Dictionary<int, string>();
                foreach(var cid in courseIds)
                {
                    try
                    {
                        Course c = CourseDao.GetCourseInfoById(cid);
                        Term t = CourseDao.GetTermById((int)c.term_id);
                        Department d = CourseDao.GetDepartmentById(c.department_id);
                        User teacher = UserDao.GetUserById(c.teacher_id);
                        astCourse = new Dictionary<string, string>
                        {
                            {"id", cid.ToString() },
                            {"teacher_name", teacher.name},
                            {"name", c.name },
                            {"term_id", t.id.ToString() },
                            {"term_name", t.name },
                            {"department_id", d.id },
                            {"department_name", d.name }
                        };
                        retData.Add(astCourse);
                    }
                    catch(Exception e)
                    {
                        ErrorLogUtil.WriteLogToFile(e, Request);
                    }
                }
                return new Response(1001, "获取成功", retData).Convert();
            }
            catch(Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }
        /// <summary>
        /// 教师查看某学生全部互评信息 by xzy
        /// </summary>
        /// <param name="info">stuid,expid</param>
        /// <returns>json
        /// :code,msg,data{id,name,course,term}
        /// </returns>
        /// <returns></returns>
        [Route("teacher/getSinglePeerDetail"), HttpGet]
        public HttpResponseMessage GetetSinglePeerDetail([FromBody]JObject info)
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

                var jsonParams = Request.GetQueryNameValuePairs().ToDictionary(k => k.Key, v => v.Value);
                int expid = Convert.ToInt32(jsonParams["expid"]);
                string stuid = jsonParams["stuid"];
                //string id = "16211084";
                User user = UserDao.GetUserById(id);                            
                Experiment exp = ExperimentDao.GetExperimentById(expid);
                Course course = CourseDao.GetCourseInfoById((int)exp.course_id);
                ///权限控制，该课程助教与老师可以访问
                if (CourseDao.GetAssistantsByCourseId(course.id).Where(a => a.student_id == id).Count() == 1 || id == course.teacher_id)
                {
                    List<Peer_assessment> peer_Assessments = PeerAssessmentDao.getPeerAssessmentByExpId(stuid, expid);
                    List<Dictionary<string, string>> retData = new List<Dictionary<string, string>>();
                    Dictionary<string, string> temp;
                    var props = peer_Assessments.First().GetType().GetProperties();
                    foreach (Peer_assessment pr in peer_Assessments)
                    {
                        temp = new Dictionary<string, string>();
                        foreach (var pi in props)
                        {
                            var v = pr.GetType().GetProperty(pi.Name).GetValue(pr, null);
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
                        User student = UserDao.GetUserById(pr.assessor_id);
                        temp.Add("assessor_name", student.name.ToString());                        
                        retData.Add(temp);
                    }
                    return new Response(1001, "成功", retData).Convert();
                }
                else
                {
                    return new Response(2002, "无权查看互评信息").Convert();           
                }                 
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }
    }
}