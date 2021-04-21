/*
*	Author:       Xing Zhihuan
*   Email:        1@roycent.cn	
*	Created:      2019/9/5 21:08:35
*   Description:  
 */
using Newtonsoft.Json;
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

namespace VMCloud.Controllers
{
    /// <summary>
    /// 作业管理API
    /// </summary>
    //[RoutePrefix("api")]
    public class AssignmentAPIController : ApiController
    {
        private RedisHelper redis;

        /// <summary>
        /// 初始化Helper
        /// </summary>
        public AssignmentAPIController()
        {
            redis = RedisHelper.GetRedisHelper();
        }

        /// <summary>
        /// 上传作业,网盘用
        /// </summary>
        /// <param name="assignmentInfo">包括file以及exp_id</param>
        /// <returns></returns>
        [Route("assignment/uploadViaNetdisk"),HttpPost]
        public HttpResponseMessage UploadAssignment([FromBody]JObject assignmentInfo)
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
                var jsonParams = HttpUtil.Deserialize(assignmentInfo);
                int expID = jsonParams.exp_id;
                string file = jsonParams.file;
                Experiment experiment = ExperimentDao.GetExperimentById(expID);
                List<Course_student_mapping> csm = CourseDao.GetMapByStudentId(id).Where(m => m.course_id == experiment.course_id).ToList();
                if(csm.Count>0)
                {
                    Assignment assignment = new Assignment
                    {
                        experiment_id = experiment.id,
                        student_id = id,
                        submit_time = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
                        is_standard = 0,
                        file = file
                    };
                    Assignment oldAssignment = AssignmentDao.GetAssignmentsByStuIdAndExpId(id, expID);
                    if ( oldAssignment != null)
                    {
                        LogUtil.Log(Request, "重新提交作业", expID.ToString(), id, 1, "", "", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                        AssignmentDao.DeleteAssignment(oldAssignment.id);
                        AssignmentDao.AddAssignment(assignment);
                        return new Response(1001, "重新提交作业成功").Convert();
                    }
                    else
                    {
                        LogUtil.Log(Request, "提交作业", expID.ToString(), id, 1, "", "", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                        AssignmentDao.AddAssignment(assignment);
                        return new Response(1001, "提交作业成功").Convert();
                    }
                }
                return new Response(2002).Convert();
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }
        /// <summary>
        /// 上传作业/重新上传作业
        /// </summary>
        /// <param name="expId">实验id，form-data</param>
        /// <param name="file">文件，form-data</param>
        /// <returns></returns>
        [Route("assignment/upload"), HttpPost]
        public HttpResponseMessage UploadAssignment()
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

                int expId = int.Parse(httpRequest["expId"]);


                Experiment exp = ExperimentDao.GetExperimentById(expId);
                if (!HttpUtil.IsTimeLater(exp.deadline))
                {
                    return new Response(2002, "已过提交截止时间").Convert();
                }
                if (exp == null)
                {
                    return new Response(3001).Convert();
                }
                Course_student_mapping csm = CourseDao.GetMapByStudentId(id).Where(m => m.course_id == exp.course_id).First();
                if (csm == null)
                {
                    return new Response(2002).Convert();
                }

                string fileCode = null;
                if (httpRequest.Files.Count > 0)
                {
                    string path = httpRequest.MapPath(@"~/Files/Assignment/" + expId + "/");
                    if (!System.IO.Directory.Exists(path))
                        System.IO.Directory.CreateDirectory(path);
                    var uploadedFiles = httpRequest.Files;
                    if (uploadedFiles.Count != 1)
                        return new Response(2002, "仅支持单个文件").Convert();
                    var uploadedFile = uploadedFiles[0];
                    Models.File file = HttpUtil.UploadFile(uploadedFile, id, "作业", path, Request);
                    AssignmentDao.AddFile(file);
                    fileCode = file.id;

                } else if (httpRequest["file"] != null)
                {
                    fileCode = httpRequest["file"].ToString();
                }
                Assignment assignment = AssignmentDao.GetAssignmentsByStuIdAndExpId(id, expId);
                if (assignment == null)
                {
                    assignment = new Assignment
                    {
                        student_id = id,
                        experiment_id = expId,
                        submit_time = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
                        file = fileCode,
                        is_standard = 0
                    };
                    AssignmentDao.AddAssignment(assignment);
                    LogUtil.Log(Request, "提交作业", expId.ToString(), id, 1);
                }
                else
                {
                    string oldFile = assignment.file;
                    assignment.file = fileCode;
                    assignment.submit_time = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                    AssignmentDao.ChangeAssignmentInfo(assignment);
                    LogUtil.Log(Request, "重新提交作业", expId.ToString(), id, 1, "", "替换的作业: " + oldFile);
                }

                return new Response(1001).Convert();
            }
            catch(Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }
        /// <summary>
        /// 下载作业
        /// </summary>
        /// <param name="uuid"></param>
        /// <returns>文件</returns>
        [Route("assignment/download"),HttpGet]
        public HttpResponseMessage DownloadAssignment()
        {
            try
            {
                var jsonParams = Request.GetQueryNameValuePairs().ToDictionary(k => k.Key, v => v.Value);
                string uuid = jsonParams["uuid"];
                if (!redis.IsSet(uuid))
                {
                    return new Response(2002, "文件下载请求不存在").Convert();
                }
                string fileId = redis.Get<string>(uuid);

                File file = AssignmentDao.GetFileById(fileId);
                HttpResponseMessage res = HttpUtil.DownloadFile(file.path.ToString(), file.name.ToString());
                redis.Delete(uuid);
                return res;
            }
            catch(Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }
        /// <summary>
        /// 获取作业单独下载uuid
        /// </summary>
        /// <returns></returns>
        [Route("assignment/tryDownload"),HttpGet]
        public HttpResponseMessage TryDownloadAssignment()
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
                var jsonParams = Request.GetQueryNameValuePairs().ToDictionary(k => k.Key, v => v.Value);
                int expId = Convert.ToInt32(jsonParams["expId"]);
                string stuId = jsonParams["stuId"];
                Assignment assignment = AssignmentDao.GetAssignmentsByStuIdAndExpId(stuId, expId);
                if(assignment == null)
                {
                    return new Response(2002, "未找到作业").Convert();
                }
                string fileId = assignment.file;
                File file = AssignmentDao.GetFileById(fileId);
                if (file == null)
                {
                    return new Response(2002, "未找到作业").Convert();
                }
                string uuid = Guid.NewGuid().ToString();
                redis.Set(uuid, file.id, 10);
                return new Response(1001, "获取成功", uuid).Convert();

            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }
        /// <summary>
        /// 下载作业
        /// </summary>
        /// <param name="assignmentInfo">包括user_id,exp_id</param>
        /// <returns></returns>
        [Route("assignment/downloadViaNetdisk"),HttpPost]
        public HttpResponseMessage DownloadAssignment([FromBody]JObject assignmentInfo)
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
                var jsonParams = HttpUtil.Deserialize(assignmentInfo);
                int expID = int.Parse(jsonParams.exp_id.ToString());
                string user_id = jsonParams.user_id;
                Assignment assignment = AssignmentDao.GetAssignmentsByStuIdAndExpId(user_id, expID);
                if (assignment == null)
                    return new Response(3001, "未找到该作业").Convert();
                Experiment exp = ExperimentDao.GetExperimentById((int)assignment.experiment_id);
                if (assignment.student_id == id || CourseDao.GetAssistantsByCourseId((int)exp.course_id).Where(a => a.student_id == id).ToList().Count > 0 || CourseDao.GetProfessorById((int)exp.course_id).id == id)
                {
                    Dictionary<string, string> ret = new Dictionary<string, string>();
                    ret.Add("file", assignment.file);
                    return new Response(1001, "成功", ret).Convert();
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
        /// 获取预览
        /// </summary>
        /// <returns></returns>
        [Route("preview"),HttpGet]
        public HttpResponseMessage PreviewFile()
        {
            try
            {
                var jsonParams = Request.GetQueryNameValuePairs().ToDictionary(k => k.Key, v => v.Value);
                string id = jsonParams["id"];
                File file = AssignmentDao.GetFileById(id);
                if(file.preview == null)
                {
                    return new Response(3001, "无法生成预览").Convert();
                }
                return HttpUtil.DownloadFile(file.preview, "preview.pdf", false);
            }
            catch(Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }

        /// <summary>
        /// 教师获取一个实验的学生提交
        /// </summary>
        /// <param name="experiment_id">虚拟机id</param>
        /// <returns>List<Assignment></returns>
        [Route("teacher/getAssignments"),HttpGet]
        public HttpResponseMessage GetAssignments()
        {
            try
            {
                var jsonParams = Request.GetQueryNameValuePairs().ToDictionary(k => k.Key, v => v.Value);
                int expid = Convert.ToInt32(jsonParams["experiment_id"]);

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

                User user = UserDao.GetUserById(id);
                Experiment exp = ExperimentDao.GetExperimentById(expid);
                Course course = CourseDao.GetCourseInfoById((int)exp.course_id);

                if(user.role < 2 && CourseDao.GetAssistantsByCourseId(course.id).Where(a => a.student_id == id).Count() == 0)
                {
                    return new Response(2002, "无权访问").Convert();
                }
                if(user.role==2 && id != course.teacher_id)
                {
                    return new Response(2002, "无权访问").Convert();
                }
                if(user.role == 3 && user.department_id != course.department_id)
                {
                    return new Response(2002, "无权访问").Convert();
                }

                List<Assignment> assignments = AssignmentDao.GetAssignmentsByExpId(expid);
                return new Response(1001, "成功", assignments).Convert();
            }
            catch(Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }

        /// <summary>
        /// 教师查看作业/实验的提交情况 by jyf
        /// </summary>
        /// <param name="expInfo">id</param>
        /// <returns>
        ///  json:{code,msg,data:{stuId,stuName,fileName,homeworkStatus,uploadTime}}
        /// </returns>
        [Route("assignment/studentHomework"),HttpGet]
        public HttpResponseMessage GetHomeworkStatus([FromBody]JObject expInfo)
        {
            try
            {
                var jsonParams = Request.GetQueryNameValuePairs().ToDictionary(k => k.Key, v => v.Value);
                int experimentId = Convert.ToInt32(jsonParams["id"]);
                
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
                Experiment experiment = ExperimentDao.GetExperimentById(experimentId);
                Course course = CourseDao.GetCourseInfoById((int)experiment.course_id);

                //if (user.role < 2 && CourseDao.GetAssistantsByCourseId(course.id).Where(a => a.student_id == id).ToList().Count() == 0)
                //{
                //    return new Response(2002, "无权限查看助教").Convert();
                //}
                //if (user.role == 3 && user.department_id != course.department_id)
                //{
                //    return new Response(2002, "无权限查看助教").Convert();
                //}

                List<Dictionary<string, string>> ret = new List<Dictionary<string, string>>();
                Dictionary<string, string> retData;

                List<User> students = CourseDao.GetStudentsById(course.id);
                List<Assignment> assignments = AssignmentDao.GetAssignmentsByExpId(experiment.id);
                string fileId = "";
                string homeworkStatus = "";
                string uploadTime = "";

                foreach (User stu in students)
                {
                    retData = new Dictionary<string, string>();
                    Assignment assignment = assignments.Where(a => a.student_id == stu.id).SingleOrDefault();
                    if (assignment == null)
                    {
                        fileId = "";
                        homeworkStatus = "未提交";
                        uploadTime = "";
                    }
                    else
                    {
                        fileId = assignment.file;
                        homeworkStatus = "已提交";
                        uploadTime = assignment.submit_time;
                    }
                    retData.Add("stuId", stu.id);
                    retData.Add("stuName", stu.name);
                    retData.Add("fileID", fileId == null?  "" : fileId);
                    retData.Add("homeworkStatus", homeworkStatus);
                    retData.Add("uploadTime", uploadTime);
                    retData.Add("score", assignment == null ? "" : assignment.score.ToString());
                    ret.Add(retData);
                }
                return new Response(1001, "成功", ret).Convert();
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }
        /// <summary>
        /// 教师抽取打分作业 by xzy
        /// </summary>
        /// <params=expId>expid</params>
        /// <returns>json
        /// :code,msg,data{id,student_id,experiment_id,submit_time,file,is_standard,score}
        /// </returns>
        [Route("teacher/getstandardHW"), HttpGet]
        public HttpResponseMessage GetstandardHW([FromBody]JObject expId)
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
                //string id = jsonParams["id"];
                int expid = Convert.ToInt32(jsonParams["expid"]);
                User user = UserDao.GetUserById(id);
                Experiment exp = ExperimentDao.GetExperimentById(expid);
                Course course = CourseDao.GetCourseInfoById((int)exp.course_id);
                ///权限控制，该课程助教与老师可以访问
                if (CourseDao.GetAssistantsByCourseId(course.id).Where(a => a.student_id == id).Count() == 1 || id == course.teacher_id)
                {
                    List<Assignment> assignments = AssignmentDao.GetAssignmentsToMarkByExpId(expid);
                    
                    if (assignments.Count() < 6)
                    {
                        assignments = AssignmentDao.GenAssignmentsToMark(expid);
                        if (assignments == null)
                        {
                            return new Response(3001, "作业数量不足6份，无法抽取").Convert();
                        }
                    }

                    List<Dictionary<string, string>> retData = new List<Dictionary<string, string>>();
                    Dictionary<string, string> temp;
                    var props = assignments.First().GetType().GetProperties();
                    int cnt = 0;
                    foreach (Assignment hw in assignments)
                    {
                        temp = new Dictionary<string, string>();
                        foreach (var pi in props)
                        {
                            var v = hw.GetType().GetProperty(pi.Name).GetValue(hw, null);
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
                        User student = UserDao.GetUserById(hw.student_id);
                        temp.Add("student_name",student.name.ToString());
                        if (hw.score != null)
                        {
                            cnt++;
                        }
                        retData.Add(temp);
                    }                  
                    if (cnt == 6)
                    {
                        if (exp.peer_assessment_start == true)
                        {
                            return new Response(1001, "互评已开始", retData).Convert();
                        }
                        else
                        return new Response(1001, "评分已完成，请开始互评", retData).Convert();
                    }
                    return new Response(1001, "success", retData).Convert();

                }
                else
                {
                    return new Response(2002, "无权抽取作业").Convert();
                }
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }
        /// <summary>
        /// 作业打分 by xzy
        /// </summary>
        /// <params=hw>hwid,grade,
        /// </params>
        /// <returns></returns>
        [Route("teacher/markAssignment"), HttpPost]
        public HttpResponseMessage MarkAssignment([FromBody]JObject hw)
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
                var jsonParams = HttpUtil.Deserialize(hw);
               // string id = jsonParams.id;
                int hwid = Convert.ToInt32(jsonParams.hwid);
                float grade = Convert.ToSingle(jsonParams.grade);
                User user = UserDao.GetUserById(id);
                Assignment assignment = AssignmentDao.GetAssignmentById(hwid);
                Experiment exp = ExperimentDao.GetExperimentById((int)assignment.experiment_id);
                Course course = CourseDao.GetCourseInfoById((int)exp.course_id);
                ///权限控制，该课程助教与老师可以访问
                if (CourseDao.GetAssistantsByCourseId(course.id).Where(a => a.student_id == id).Count() == 1 || id == course.teacher_id)
                {
                    AssignmentDao.ModifyScore(hwid, grade);

                    return new Response(1001, "评分成功").Convert();

                }
                else
                {
                    return new Response(2002, "无权打分").Convert();
                }
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }
        /// <summary>
        /// 生成标准作业，开始互评 by xzy
        /// </summary>
        /// 
        /// <param name="expId">expid</param>
        /// <returns></returns>
        [Route("teacher/startPeerAssessment"), HttpPost]
        public HttpResponseMessage StartPeerAssessment([FromBody]JObject expId)
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

                var jsonParams = HttpUtil.Deserialize(expId);
                //string id = "16211084";
                int expid = Convert.ToInt32(jsonParams.expid);
                User user = UserDao.GetUserById(id);
                Experiment exp = ExperimentDao.GetExperimentById(expid);
                Course course = CourseDao.GetCourseInfoById((int)exp.course_id);
                if (exp.peer_assessment_start == true)
                {
                    return new Response(3001, "互评已开启，请勿重复操作").Convert();
                }
                ///权限控制，该课程助教与老师可以访问               
                if (CourseDao.GetAssistantsByCourseId(course.id).Where(a => a.student_id == id).Count() == 1 || id == course.teacher_id)
                {
                    
                    AssignmentDao.SetStandardAssignment(expid, 4);
                    AssignmentDao.AssignPeerAsssessment(expid);
                    exp.peer_assessment_start = true;
                    ExperimentDao.ChangeExperimentInfo(exp);
                    return new Response(1001, "开启成功").Convert();
                }
                else
                {
                    return new Response(2002, "无权开启互评").Convert();
                }
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }
        /// <summary>
        /// 互评分数结算 by xzy
        /// </summary>
        /// <param name="expId">expid</param>
        /// <returns></returns>
        [Route("teacher/exportPeerScore"), HttpPost]
        public HttpResponseMessage ExportPeerScore([FromBody]JObject expId)
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

                var jsonParams = HttpUtil.Deserialize(expId);
                //string id = "16211084";
                int expid = Convert.ToInt32(jsonParams.expid);
                User user = UserDao.GetUserById(id);
                Experiment exp = ExperimentDao.GetExperimentById(expid);
                Course course = CourseDao.GetCourseInfoById((int)exp.course_id);
                ///权限控制，该课程助教与老师可以访问
                if (CourseDao.GetAssistantsByCourseId(course.id).Where(a => a.student_id == id).Count() == 1 || id == course.teacher_id)
                {
                    List<Assignment> assignments = AssignmentDao.GetAssignmentsByExpId(expid);
                    List<PeerResult> prResults = new List<PeerResult>();
                    foreach(var hw in assignments)
                    {
                        PeerAssessmentDao.CorrectScore(hw.student_id, expid);
                        PeerAssessmentDao.ComputeFinalScore(hw.student_id, expid);
                        try
                        {
                            prResults.Add(new PeerResult
                            {
                                id = hw.student_id,
                                name = UserDao.GetUserById(hw.student_id).name,
                                score = hw.score
                            });
                        }catch(Exception e){}
                    }
                    return new Response(1001, "导出成功", prResults).Convert();
                }
                else
                {
                    return new Response(2002, "无权进行操作").Convert();
                }
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }
    }

    public class PeerResult
    {
        public string id { get; set; }
        public string name { get; set; }
        public float? score { get; set; }
    }
}