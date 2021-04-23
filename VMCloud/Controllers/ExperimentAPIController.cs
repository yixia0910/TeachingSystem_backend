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
    //[RoutePrefix("api")]
    public class ExperimentAPIController: ApiController
    {
        private RedisHelper redis;

        /// <summary>
        /// 初始化Helper
        /// </summary>
        public ExperimentAPIController()
        {
            redis = RedisHelper.GetRedisHelper();
        }

        

        /// <summary>
        /// 学生提交互评  
        /// created by xzy
        /// 2019.7.28
        /// </summary>
        /// <param name="peerAssessment"></param>
        /// <returns>
        ///  return 
        ///  {
        ///     "code":,
        ///     "msg":""
        ///     "data": '' 
        ///  }
        /// </returns>
        ///
        [Route("peer/update"), HttpPost]
        public HttpResponseMessage AddPeerAssessment([FromBody]JObject peerAssessment)
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                var jsonParams = HttpUtil.Deserialize(peerAssessment);
                bool isLogin = redis.IsSet(signature);
                if (!isLogin)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string id = redis.Get<string>(signature);
                string stuid = jsonParams.student_id;
                int expid = jsonParams.experiment_id;
                Peer_assessment peerAssessment2 = new Peer_assessment();
                peerAssessment2.assessor_id = id;
                QuickCopy.Copy(peerAssessment, ref peerAssessment2);
                Peer_assessment OldPa = PeerAssessmentDao.getPeerAssessment(stuid, id, expid);
                if (OldPa != null)
                {
                    if (PeerAssessmentDao.ChangePeerAssessmentInfo(peerAssessment2) == 1)
                    {
                        return new Response(1001, "Success").Convert();
                    }
                    else
                    {
                        return new Response(1001, "数据未变").Convert();
                    }
                }
                
                Peer_assessment peer_Assessment = new Peer_assessment();
                peer_Assessment.assessor_id = id;             
                QuickCopy.Copy(peerAssessment, ref peer_Assessment);
                Experiment exp = ExperimentDao.GetExperimentById(peer_Assessment.experiment_id);
                //if (!HttpUtil.IsTimeLater(exp.peer_assessment_deadline))
                    //return new Response(2002, "互评已结束").Convert();
                PeerAssessmentDao.AddPeerAssessment(peer_Assessment);
                LogUtil.Log(Request, "作业评分", peer_Assessment.student_id, peer_Assessment.assessor_id, 1, "", "", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                return new Response(1001, "Success").Convert();
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }
        /// <summary>
        /// 老师提交课程总分 
        /// created by yixia
        /// 2021.4.22
        /// </summary>
        /// <param name="peerAssessment"></param>
        /// <returns>
        ///  return 
        ///  {
        ///     "code":,
        ///     "msg":""
        ///     "data": '' 
        ///  }
        /// </returns>
        ///
        [Route("commitScore"), HttpPost]
        public HttpResponseMessage CommitScore([FromBody]JObject peerAssessment)
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                var jsonParams = HttpUtil.Deserialize(peerAssessment);
                bool isLogin = redis.IsSet(signature);
                if (!isLogin)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string id = jsonParams.student_id;
                string stuid = jsonParams.student_id;
                int expid = jsonParams.experiment_id;
                Peer_assessment peerAssessment2 = new Peer_assessment();
                peerAssessment2.assessor_id = id;
                QuickCopy.Copy(peerAssessment, ref peerAssessment2);
                Peer_assessment OldPa = PeerAssessmentDao.getPeerAssessment(stuid, id, expid);
                if (OldPa != null)
                {
                    if (PeerAssessmentDao.ChangePeerAssessmentInfo(peerAssessment2) == 1)
                    {
                        return new Response(1001, "Success").Convert();
                    }
                    else
                    {
                        return new Response(1001, "数据未变").Convert();
                    }
                }
                
                Peer_assessment peer_Assessment = new Peer_assessment();
                peer_Assessment.assessor_id = id;             
                QuickCopy.Copy(peerAssessment, ref peer_Assessment);
                Experiment exp = ExperimentDao.GetExperimentById(peer_Assessment.experiment_id);
                PeerAssessmentDao.AddPeerAssessment(peer_Assessment);
                LogUtil.Log(Request, "作业评分", peer_Assessment.student_id, peer_Assessment.assessor_id, 1, "", "", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                return new Response(1001, "Success").Convert();
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }
        /// <summary>
        /// 修改互评  
        /// created by xzy
        /// 2019.7.28
        /// </summary>
        /// <param name="paInfo">stuid,expid,</param>
        /// Todo:传参，需要作业拥有者id,实验expid,修改者userid,
        /// <returns>
        ///  return 
        ///  {
        ///     "code":,
        ///     "msg":""
        ///     "data": '' 
        ///  }
        /// </returns>
        ///
        [Route("ChangePeerAssessmentInfo"), HttpPost]
        public HttpResponseMessage ChangePeerAssessmentInfo([FromBody]JObject paInfo)
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                var jsonParams = HttpUtil.Deserialize(paInfo);
                bool isLogin = redis.IsSet(signature);
                if (!isLogin)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string stuid = jsonParams.student_id;
                string userid = redis.Get<string>(signature);
                int expid = jsonParams.experiment_id;
                Peer_assessment peer_Assessment = new Peer_assessment();
                QuickCopy.Copy(paInfo, ref peer_Assessment);
                Peer_assessment OldPa = PeerAssessmentDao.getPeerAssessment(stuid, userid, expid);
                if (OldPa == null)
                {
                    return new Response(2002, "无权访问").Convert();
                }
                if (PeerAssessmentDao.ChangePeerAssessmentInfo(peer_Assessment) == 1)
                {
                    return new Response(1001, "Success").Convert();
                }
                else
                {
                    throw new Exception("数据库操作异常");
                }

            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }
        /// <summary>
        /// 查找分数
        /// created by yixia
        /// 2021.3.12
        /// </summary>
        /// <param name="paInfo">stuid,expid,</param>
        /// Todo:传参，需要作业拥有者id,实验expid,userid,
        /// <returns>
        ///  return 
        ///  {
        ///     "code":,
        ///     "msg":""
        ///     "data": '' 
        ///  }
        /// </returns>
        ///
        [Route("FindScore"), HttpGet]
        public HttpResponseMessage FindScore()
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                var jsonParams = Request.GetQueryNameValuePairs().ToDictionary(k => k.Key, v => v.Value);
                bool isLogin = redis.IsSet(signature);
                if (!isLogin)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string stuid = jsonParams["stuid"];
                string userid = jsonParams["userid"];
                int expid = Convert.ToInt32(jsonParams["expid"]);
                Peer_assessment OldPa = PeerAssessmentDao.getPeerAssessment(stuid, userid, expid);
                if (OldPa == null)
                {
                    return new Response(1001, "暂无分数", -1).Convert();
                }
                
                return new Response(1001, "成功", OldPa.origin_score).Convert();
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }

        /// <summary>
        /// 查找权重/总分
        /// created by yixia
        /// 2021.4.22
        /// </summary>
        /// <param name="paInfo">stuid,expid,</param>
        /// <returns>
        ///  return 
        ///  {
        ///     "code":,
        ///     "msg":""
        ///     "data": '' 
        ///  }
        /// </returns>
        ///
        [Route("FindWeight"), HttpGet]
        public HttpResponseMessage FindWeight()
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                //var jsonParams = HttpUtil.Deserialize(paInfo);
                var jsonParams = Request.GetQueryNameValuePairs().ToDictionary(k => k.Key, v => v.Value);
                //Console.WriteLine(jsonParams);
                bool isLogin = redis.IsSet(signature);
                if (!isLogin)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string stuid = jsonParams["stuid"];
                string userid = jsonParams["userid"];
                int expid = Convert.ToInt32(jsonParams["expid"]);
                Peer_assessment OldPa = PeerAssessmentDao.getPeerAssessment(stuid, userid, expid);
                if (OldPa == null)
                {
                    return new Response(1001, "暂无分数", -1).Convert();
                }
                return new Response(1001, "成功", OldPa.score).Convert();
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }

        /// <summary>
        /// 查找评语
        /// created by yixia
        /// 2021.3.12
        /// </summary>
        /// <param name="paInfo">stuid,expid,</param>
        /// Todo:传参，需要作业拥有者id,实验expid,userid,
        /// <returns>
        ///  return 
        ///  {
        ///     "code":,
        ///     "msg":""
        ///     "data": '' 
        ///  }
        /// </returns>
        ///
        [Route("FindComment"), HttpGet]
        public HttpResponseMessage FindComment([FromBody]JObject paInfo)
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                //var jsonParams = HttpUtil.Deserialize(paInfo);
                var jsonParams = Request.GetQueryNameValuePairs().ToDictionary(k => k.Key, v => v.Value);
                //Console.WriteLine(jsonParams);
                bool isLogin = redis.IsSet(signature);
                if (!isLogin)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string stuid = jsonParams["stuid"];
                string userid = redis.Get<string>(signature);
                int expid = Convert.ToInt32(jsonParams["expid"]);;
                Peer_assessment peerAssessment = new Peer_assessment();
                Peer_assessment OldPa = PeerAssessmentDao.getPeerAssessment(stuid, userid, expid);
                if (OldPa == null)
                {
                    return new Response(1001, "暂无评价").Convert();
                }
                
                return new Response(1001, "成功", OldPa.reason).Convert();
                
                //if (PeerAssessmentDao.getPeerAssessment(peer_Assessment) == null)
                //{
                    //return new Response(1002, "失败").Convert();
                //}
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }
        
        /// <summary>
        /// 互评申诉  
        /// created by xzy
        /// 2019.7.28
        /// </summary>
        /// <param name="appealInfo">expid,reason</param>
        /// Todo:传参，需要作业拥有者id,实验expid,申诉原因reason
        /// <returns>
        ///  return 
        ///  {
        ///     "code":,
        ///     "msg":""
        ///     "data": '' 
        ///  }
        /// </returns>
        ///
        [Route("peer/appeal"), HttpPost]
        public HttpResponseMessage AppealPeerAssessment([FromBody]JObject appealInfo)
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                var jsonParams = HttpUtil.Deserialize(appealInfo);
                bool isLogin = redis.IsSet(signature);
                if (!isLogin)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string stuid = redis.Get<string>(signature);
                //string stuid = jsonParams.stuid;
                int expid = jsonParams.experiment_id;
                string reason = jsonParams.reason;
                string assessorid = jsonParams.assessor_id;
                User user = UserDao.GetUserById(stuid);
                PeerAssessmentDao.AppealPeerAssessment(stuid, assessorid,expid,1, reason);
                LogUtil.Log(Request, "互评申诉", stuid, stuid, user.role, "", "", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                return new Response(1001, "申诉成功").Convert();

            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }
        /// <summary>
        /// 处理申诉
        /// created by xzy
        /// 2019.9.27
        /// </summary>
        /// <param name="paInfo">stuid,asessor_id,expid,score</param>
        /// 
        /// <returns>
        ///  return 
        ///  {
        ///     "code":,
        ///     "msg":""
        ///     "data": '' 
        ///  }
        /// </returns>
        ///
        [Route("teacher/dealAppeal"), HttpPost]
        public HttpResponseMessage dealAppeal([FromBody]JObject paInfo)
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
                var jsonParams = HttpUtil.Deserialize(paInfo);
                //string id = jsonParams.id;
                string student_id = jsonParams.student_id;               
                string assessor_id = jsonParams.assessor_id;
                int experiment_id = jsonParams.experiment_id;
                float score = Convert.ToSingle(jsonParams.score);

                User user = UserDao.GetUserById(id);
                Experiment exp = ExperimentDao.GetExperimentById(experiment_id);
                Course course = CourseDao.GetCourseInfoById((int)exp.course_id);
                ///权限控制，该课程助教与老师可以访问
                if (CourseDao.GetAssistantsByCourseId(course.id).Where(a => a.student_id == id).Count() == 1 || id == course.teacher_id)
                {
                    Peer_assessment peer_Assessment = new Peer_assessment();
                    QuickCopy.Copy(paInfo, ref peer_Assessment);
                    peer_Assessment.appeal_status = 2;
                    Peer_assessment OldPa = PeerAssessmentDao.getPeerAssessment(student_id, assessor_id, experiment_id);
                    if (OldPa == null)
                    {
                        return new Response(3001, "参数无效").Convert();
                    }
                    if (PeerAssessmentDao.ChangePeerAssessmentInfo(peer_Assessment) == 1)
                    {
                        return new Response(1001, "Success").Convert();
                    }
                    else
                    {
                        throw new Exception("数据库操作异常");
                    }                   
                }
                else
                {
                    return new Response(2002, "无权访问").Convert();
                }
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }
        /// <summary>
        /// 查看收到的互评结果
        /// created by xzy
        /// 2019.7.28
        /// </summary>
        /// <param name="peerDetail">expid</param>
        /// 学生查看某作业收到的所有互评分数，
        /// 2019.8.27
        /// update:标准作业只返回部分互评分数
        /// 
        /// <returns>
        ///  return 
        ///  {
        ///     "code":,
        ///     "msg":""
        ///     "data": '' 
        ///  }
        /// </returns>
        ///
        [Route("peer/details"), HttpGet]
        public HttpResponseMessage PeerAssessmentDetails([FromBody]JObject peerDetail)
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                //var jsonParams = HttpUtil.Deserialize(peerDetail);
                var jsonParams = Request.GetQueryNameValuePairs().ToDictionary(k => k.Key, v => v.Value);
                List<Dictionary<string, string>> retData = new List<Dictionary<string, string>>();
                Dictionary<string, string> PaInfo = new Dictionary<string, string>();
                bool isLogin = redis.IsSet(signature);
                if (!isLogin)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string userid = redis.Get<string>(signature);// jsonParams.stuid;//
                int expid = Convert.ToInt32(jsonParams["expid"]);
                Assignment assignment = AssignmentDao.GetAssignmentsByStuIdAndExpId(userid, expid);
                int cnt = 0;
                List<Peer_assessment> peer_Assessments = PeerAssessmentDao.getPeerAssessmentByExpId(userid, expid);
                foreach (Peer_assessment pa in peer_Assessments)
                {
                    PaInfo = new Dictionary<string, string>
                    {
                        { "assessorId", pa.assessor_id.ToString() },
                        { "score", pa.origin_score.ToString() },
                        { "reason", pa.appeal_reason },
                        { "appealStatus", pa.appeal_status.ToString() },
                    };
                    retData.Add(PaInfo);
                    cnt++;
                    if (cnt >=3 && assignment.is_standard == 2)
                    {
                        break;
                    }
                    
                }
                return new Response(1001, "Success", retData).Convert();
                //todo:body.id
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }
        /// <summary>
        /// 学生互评页面  
        /// created by xzy
        /// 2019.7.28
        /// </summary>
        /// <param name="peerInfo">expid</param>
        /// 查看某个作业该学生要完成的全部互评
        ///
        /// 
        /// <returns>
        ///  return 
        ///  {
        ///     "code":,
        ///     "msg":""
        ///     "data": {"studentId": "",
        //              "status": "",
        //              "originScore": "",
        //              "reason": ""
        ///             }
        ///  }
        /// </returns>
        ///
        [Route("peer/info"), HttpGet]
        public HttpResponseMessage PeerAssessmentInfo([FromBody]JObject peerInfo)
        {
            try
            {
                List<Dictionary<string, string>> retData = new List<Dictionary<string, string>>();
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                //var jsonParams = HttpUtil.Deserialize(peerInfo);
                var jsonParams = Request.GetQueryNameValuePairs().ToDictionary(k => k.Key, v => v.Value);
                bool isLogin = redis.IsSet(signature);
                Dictionary<string, string> PaInfo;
                if (!isLogin)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string userid = redis.Get<string>(signature);
                int expid = Convert.ToInt32(jsonParams["expid"]);
                List<Peer_assessment> peer_Assessments = PeerAssessmentDao.getPeerAssessmentByAssessorId(userid, expid);
                foreach (Peer_assessment pa in peer_Assessments)
                {
                    PaInfo = new Dictionary<string, string>
                    {
                        { "studentId", pa.student_id.ToString() },
                        { "status", pa.appeal_status.ToString() },
                        { "originScore", pa.origin_score.ToString() },
                        { "reason", pa.reason },
                        
                    };
                    retData.Add(PaInfo);

                }

                return new Response(1001, "Success", retData).Convert();
                //todo:body.id
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }
        /// <summary>
        /// 互评信息表格
        /// created by xzy
        /// 2019.8.18
        /// </summary>
        /// <param name="peerInfo">expid</param>
        /// 教师/助教查看某实验全部互评信息
        ///
        /// 
        /// <returns>
        ///  return 
        ///  {
        ///     "code":,
        ///     "msg":""
        ///     "data": '' 
        ///  }
        /// </returns>
        ///
        [Route("peer/allpeerinfo"), HttpGet]
        public HttpResponseMessage getAllPeerAssessmentInfo([FromBody]JObject peerInfo)
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
                string userid = redis.Get<string>(signature);
                var jsonParams = Request.GetQueryNameValuePairs().ToDictionary(k => k.Key, v => v.Value);                
                List<Dictionary<string, string>> retData = new List<Dictionary<string, string>>();
                Dictionary<string, string> temp;
               // string userid = jsonParams["id"];
                int expid = Convert.ToInt32(jsonParams["expid"]);

                Experiment exp = ExperimentDao.GetExperimentById(expid);
                Course course = CourseDao.GetCourseInfoById((int)exp.course_id);
                ///权限控制，该课程助教与老师可以访问
                if (CourseDao.GetAssistantsByCourseId(course.id).Where(a => a.student_id == userid).Count() == 1 || userid == course.teacher_id)
                {
                    List<Peer_assessment> peer_Assessments = PeerAssessmentDao.getPeerAssessmentByExpId(expid);
                    
                    var props = peer_Assessments.First().GetType().GetProperties();
                    int cnt = 0;
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
                        User student = UserDao.GetUserById(pr.student_id);
                        User assessor = UserDao.GetUserById(pr.assessor_id);
                        temp.Add("student_name", student.name.ToString());
                        temp.Add("assessor_name", assessor.name.ToString());
                      
                        retData.Add(temp);
                    }
                    return new Response(1001, "Success", retData).Convert();            
                }
                else
                {
                    return new Response(2002, "无权限访问该实验相关信息").Convert();
                }

            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }


        /// <summary>
        /// 获取未完成实验API by jyf
        /// 根据：stuid&termid
        /// 2019.7.22
        /// </summary>
        /// <param name="account">termId</param>
        /// <returns>
        /// {
        ///     "code":,
        ///     "msg":""
        ///     "data":{
        ///         'id': '',
        ///         'name': '',
        ///         'course': '',
        ///         'teacher': '',
        ///         'term': '',
        ///         'startTime': '',
        ///         'endTime': '',
        ///         'deadline': '',
        ///         'status': ''
        ///     }
        /// }
        /// </returns>
        /// 
        [Route("student/getUnfinishedExp"), HttpGet]
        public HttpResponseMessage GetUnfinishedExperiment([FromUri]JObject conditions)//fromUri 没用
        {
            //Dictionary<string, string> retData = new Dictionary<string, string>();
            List<Dictionary<string, string>> retData = new List<Dictionary<string, string>>();
            Dictionary<string, string> expInfo;
            try
            {
                var jsonParams = Request.GetQueryNameValuePairs().ToDictionary(k => k.Key, v => v.Value);
                var currentTime = DateTime.Now;
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                //var jsonParams = HttpUtil.Deserialize(conditions);
                int termId = Convert.ToInt32(jsonParams["termId"]);
                
                bool isLogin = redis.IsSet(signature);
                if (!isLogin)
                {
                    return new Response(2001, "未登录账户").Convert();
                }

                string id = redis.Get<string>(signature);
                
                //string id = "16211094";
                //List<Course_student_mapping> courseList = CourseDao.GetMapByStudentId(id);
                List<Course> courseList = CourseDao.GetCoursesByStuIdAndTermId(id, termId);
                List<Experiment> allExperiment = new List<Experiment>();
                foreach (var map in courseList)
                {
                    List<Experiment> exp = ExperimentDao.GetExperimentByCourseId(map.id);
                    allExperiment = allExperiment.Union(exp).ToList();
                }
                allExperiment = allExperiment.OrderBy(i => i.id).ToList();
                List<Assignment> allAssignments = AssignmentDao.GetAssignmentsByStuId(id).OrderBy(i => i.experiment_id).ToList();
                List<Peer_assessment> peerAssessments = PeerAssessmentDao.getPeerAssessmentByStuId(id);
                List<Experiment> assignmentFinishedExp = allExperiment.Where(e => allAssignments.Exists(a => a.experiment_id == e.id)).ToList();
                allExperiment = allExperiment.Except(assignmentFinishedExp).ToList();//没完成作业的

                List<Experiment> peerExp = assignmentFinishedExp.Where(e => e.is_peer_assessment == true).ToList();//作业完成但要互评
                foreach(Experiment e in peerExp)
                {
                    if (PeerAssessmentDao.getPeerAssessmentByAssessorId(id, e.id).Where(p => p.appeal_status == 1).Count() > 0)
                    {
                        allExperiment.Add(e);
                    }
                }//得到没完成作业的&没互评的

                allExperiment = allExperiment.Where(e => (e.is_peer_assessment == false && DateTime.Compare(Convert.ToDateTime(e.deadline), currentTime) > 0 
                                                        || e.is_peer_assessment==true && DateTime.Compare(Convert.ToDateTime(e.peer_assessment_deadline),currentTime)>0)).ToList();
                //得到ddl前的
                return new Response(1001, "获取成功", ExperimentDao.GetExpRet(allExperiment)).Convert();
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }

        /// <summary>
        /// 返回学生所有实验 api
        /// created by jyf 
        /// 2019.8.19
        /// </summary>
        /// <returns>json</returns>
        /// TODO:具体是啥日后再说
        [Route("student/getAllExperiments")]
        public HttpResponseMessage GetAllExperiments()
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
                var retData = ExperimentDao.GetExperimentByStuId(id);
                return new Response(1001, "获取成功", ExperimentDao.GetExpRet(retData)).Convert();
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }

        /// <summary>
        /// 学生按课程查找实验 api
        /// created by jyf 
        /// 2019.8.19
        /// </summary>
        /// <param name="">courseId</param>
        /// <returns>json</returns>
        /// TODO:具体是啥日后再说
        [Route("student/getExperimentByCourseId"),HttpGet]
        public HttpResponseMessage GetExperimentByCourseId()
        {
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
                string id = redis.Get<string>(signature);

                var retData = ExperimentDao.GetExperimentByCourseId(courseId);
                return new Response(1001, "获取成功", ExperimentDao.GetExpRet(retData)).Convert();
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }

        /// <summary>
        /// 学生按学期查找实验 api
        /// created by jyf 
        /// 2019.8.19
        /// </summary>
        /// <param name="">termId</param>
        /// <returns>json</returns>
        /// TODO:具体是啥日后再说
        [Route("student/getExperimentByTermId"),HttpGet]
        public HttpResponseMessage GetExperimentByTermId()
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                var jsonParams = Request.GetQueryNameValuePairs().ToDictionary(k => k.Key, v => v.Value);
                int termId = Convert.ToInt32(jsonParams["termId"]);


                bool isLogin = redis.IsSet(signature);
                if (!isLogin)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string id = redis.Get<string>(signature);

                var retData = ExperimentDao.GetExperimentByTermIdAndStuId(id, termId);
                return new Response(1001, "获取成功", ExperimentDao.GetExpRet(retData)).Convert();
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }

        /// <summary>
        /// 实验详细信息返回类
        /// created by jyf
        /// 2019.7.24
        /// </summary>
        protected class tmpRetExpDetail
        {
            public string name { get; set; }
            //满分数值
            public int score { get; set; }
            //参考分数
            public int score2 { get; set; }
            //参考分数说明
            public string reason { get; set; }
            public bool type { get; set; }
            public string course { get; set; }
            public int course_id { get; set; }
            public string teacher { get; set; }
            public string teacher_id { get; set; }
            public string startTime { get; set; }
            public string endTime { get; set; }
            public string deadline { get; set; }
            public string peerDeadline { get; set; }
            public string details { get; set; }
            //public List<Dictionary<string, string>> sourcelist { get; set; }
            public string resource { get; set; }
            public string status { get; set; }
            public string appealDeadline { get; set; }
            public string peerAssessmentRule { get; set; }
            public bool peerStarted { get; set; }
        }

        /// <summary>
        /// 获取实验详细信息
        /// created by jyf
        /// 2019.7.23
        /// </summary>
        /// <param name="account"></param>
        /// <returns>
        /// {
        ///     "code":"",
        ///     "msg":"",
        ///     "data":{
        ///         'name': '@cword(3,5)实验',
        ///         'course': '@cword(3,7)',
        ///         'teacher': '@cname',
        ///         'deadline': '@datetime',
        ///         'peerDeadline': '@datetime',
        ///         'details': '@csentence(50,800)',
        ///         'resource':'fefsg'
        ///          }],
        ///         'status|1': ['已提交', '未提交'],
        ///         'appealDeadline':'',
        ///         'peerAssessmentDeadline',''
        ///     }
        /// }
        /// </returns>
        [Route("getExperimentDetail"), HttpGet]
        public HttpResponseMessage GetExperimentDetail([FromBody]JObject account)
        {
            try
            {
                var jsonParams = Request.GetQueryNameValuePairs().ToDictionary(k => k.Key, v => v.Value);
                int id = Convert.ToInt32(jsonParams["id"]);    //experimentid
                
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                //var jsonParams = HttpUtil.Deserialize(account);
                

                bool isLogin = redis.IsSet(signature);
                if (!isLogin)
                {
                    return new Response(2001, "未登录账户").Convert();
                }

                string userId = redis.Get<string>(signature);
                
                //string userId = "16211084";
                User user = UserDao.GetUserById(userId);
                Experiment experiment = ExperimentDao.GetExperimentById(id);
                Course course = CourseDao.GetCourseInfoById(experiment.id);
                List<Assignment> assignment = AssignmentDao.GetAssignmentsByStuId(userId);
                bool flag = false;
                foreach (var ass in assignment)
                {
                    if (ass.experiment_id == id)
                    {
                        flag = true;
                        break;
                    }
                }

                tmpRetExpDetail ret = new tmpRetExpDetail();
                ret.name = experiment.name;
                ret.type = experiment.type.GetValueOrDefault();
                ret.score = Convert.ToInt32(experiment.vm_status);
                ret.score2 = Convert.ToInt32(experiment.vm_apply_id);
                ret.reason = experiment.vm_name;
                if(experiment.course_id != null)
                {
                    
                    //找出其他任课老师
                    List<Assistant> AssistantTeas = CourseDao.GetAssistantsByCourseId((int)experiment.course_id).ToList();
                    List<string> teachers = UserDao.GetUserByRole(2).Where(a => AssistantTeas.Exists(t => t.student_id == a.id)).Select(a => a.name).ToList();

                    ret.course = CourseDao.GetCourseInfoById((int)experiment.course_id).name;
                    ret.course_id = (int)experiment.course_id;
                    ret.teacher =  UserDao.GetUserById(CourseDao.GetCourseInfoById((int)experiment.course_id).teacher_id).name;
                    ret.teacher_id = CourseDao.GetCourseInfoById((int)experiment.course_id).teacher_id;
                    if(teachers.Count != 0)
                    {
                        foreach(string t in teachers)
                        {
                            ret.teacher = ret.teacher + "," + t;
                        }
                    }
                }
                else
                {
                    ret.course = "";
                    ret.teacher = "";
                }
                ret.deadline = experiment.deadline;
                ret.peerDeadline = experiment.peer_assessment_deadline;
                ret.details = experiment.detail;
                /**
                ret.sourcelist = new List<Dictionary<string, string>>();
                ret.sourcelist.Add(new Dictionary<string, string> {
                    { "source", experiment.resource },
                    { "name","" }//TODO:这啥属性？
                });
                **/
                ret.resource = experiment.resource;
                ret.appealDeadline = experiment.appeal_deadline;
                ret.peerAssessmentRule = experiment.peer_assessment_rules;
                ret.peerStarted = experiment.peer_assessment_start == true ? true:false;
                ret.startTime = experiment.start_time;
                ret.endTime = experiment.end_time;
                if (user.role == 1)
                {
                    ret.status = flag ? "1" : "0";
                }
                else
                {
                    ret.status = AssignmentDao.GetAssignmentsByExpId(experiment.id).Count().ToString() + "/" + CourseDao.GetMapByCourseId((int)experiment.course_id).Count().ToString();
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
        /// 取某实验中没有虚拟机的学生  
        /// created by zzw
        /// 2019.8.28
        /// </summary>
        /// <param name="expInfo">expid</param>
        /// <returns>
        ///  return 
        ///  {
        ///     "code":,
        ///     "msg":""
        ///     "data": {
        ///         'id' : '',
        ///     }
        ///  }
        /// </returns>
        ///
        [Route("getStusWithoutVMByExpId"), HttpGet]
        public HttpResponseMessage getStusWithoutVMByExpId([FromBody]JObject expInfo)
        {
            try
            {
                List<Dictionary<string, string>> retData = new List<Dictionary<string, string>>();
                Dictionary<string, string> stuidInfo;
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                var jsonParams = Request.GetQueryNameValuePairs().ToDictionary(k => k.Key, v => v.Value);
                bool isLogin = redis.IsSet(signature);
                if (!isLogin)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string userid = redis.Get<string>(signature);
                int expid = Convert.ToInt32(jsonParams["expid"]);
                Experiment exp = ExperimentDao.GetExperimentById(expid);
                if (exp.course_id == null)
                {
                    return new Response(1001, "该实验没有所属课程").Convert();
                }
                int courseId = (int)ExperimentDao.GetExperimentById(expid).course_id;
                User user = UserDao.GetUserById(userid);
                if ((user.role == 2 && CourseDao.GetCourseInfoById(courseId).teacher_id == user.id) || (user.role == 1 && CourseDao.GetAssistantsByCourseId(courseId).Where(a => a.student_id == user.id).Count() != 0))
                {
                    List<User> stulist = CourseDao.GetStudentsById(courseId);
                    List<VMConfig> vmlist = VMDao.GetVMsByVmName(exp.vm_name);
                    foreach (User stu in stulist)
                    {
                        if (vmlist.Find(vm => vm.student_id.Equals(stu.id)) == null)
                        {
                            stuidInfo = new Dictionary<string, string>
                            {
                                { "id", stu.id},
                            };
                            retData.Add(stuidInfo);
                        }
                    }
                    return new Response(1001, "Success", retData).Convert();
                }
                else
                {
                    return new Response(2001, "没有权限获取信息").Convert();
                }
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }

        /// <summary>
        /// 管理员获取实验
        /// </summary>
        /// <returns>termId，courseId（都可以不选）</returns>
        [Route("admin/getExperiments"),HttpGet]
        public HttpResponseMessage AdminGetAllExperiments()
        {
            try
            {
                string signature = HttpUtil.GetAuthorization(Request);
                if (signature == null || !redis.IsSet(signature))
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                var jsonParams = Request.GetQueryNameValuePairs().ToDictionary(k => k.Key, v => v.Value);
                int termId = 0;
                int courseId = 0;
                if (jsonParams.ContainsKey("termId"))
                {
                    termId = Convert.ToInt32(jsonParams["termId"]);
                }
                if (jsonParams.ContainsKey("courseId"))
                {
                    courseId = Convert.ToInt32(jsonParams["courseId"]);
                }

                bool isLogin = redis.IsSet(signature);
                if (!isLogin)
                {
                    return new Response(2001, "未登录账户").Convert();
                }
                string id = redis.Get<string>(signature);

                //string id = "admin";
                User user = UserDao.GetUserById(id);

                if (user.role < 3)
                {
                    return new Response(2002, "权限不足").Convert();
                }

                List<Experiment> experiments = ExperimentDao.GetExperimentByCourseIdAndTermId(courseId,termId);

                if (user.role == 3)
                {
                    List<Course> courses = CourseDao.GetCourseByDepId(user.department_id);
                    experiments = experiments.Where(e => courses.Exists(c => c.id == e.course_id)).ToList();
                }
                return new Response(1001, "获取成功", ExperimentDao.GetExpRet(experiments)).Convert();
            }
            catch (Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, Request);
                return Response.Error();
            }
        }
    }
}