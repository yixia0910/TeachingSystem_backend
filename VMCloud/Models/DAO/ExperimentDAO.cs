/*
*	Author:       jyf
*	Created:      2019/7/17 
*   Description:  数据库访问类 实验相关
 */
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Web;
using VMCloud.Utils;

namespace VMCloud.Models.DAO
{
    public class ExperimentDao
    {

        public class expReturn
        {
            public int id { get; set; }

            public int? course_id { get; set; }

            public string name { get; set; }

            public bool? type { get; set; }

            public string detail { get; set; }

            public string resource { get; set; }

            public string create_time { get; set; }

            public string start_time { get; set; }

            public string end_time { get; set; }

            public string deadline { get; set; }

            public int? vm_status { get; set; }

            public string vm_name { get; set; }

            public int? vm_apply_id { get; set; }

            public string vm_passwd { get; set; }

            public bool? is_peer_assessment { get; set; }

            public string peer_assessment_deadline { get; set; }

            public string appeal_deadline { get; set; }

            public string peer_assessment_rules { get; set; }

            public bool? peer_assessment_start { get; set; }
            public string teacher_name { get; set; }
            public string course_name { get; set; }
            public int term_id { get; set; }
            public string term_name { get; set; }
        }

        /// <summary>
        /// 创造带课程和教师名的exp返回
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static expReturn GetExpRet(Experiment e)
        {
            expReturn exp = new expReturn();
            var properties = exp.GetType().GetProperties();
            var type = e.GetType();
            foreach (var pi in properties)
            {
                if(pi.Name== "teacher_name"||pi.Name== "course_name"||pi.Name=="term_name"||pi.Name=="term_id")
                {
                    continue;
                }
                var value = type.GetProperty(pi.Name).GetValue(e, null);
                if (value != null)
                {
                    pi.SetValue(exp, value);
                }
            }
            Course course = CourseDao.GetCourseInfoById((int)e.course_id);
            exp.course_name = course.name;
            exp.teacher_name = UserDao.GetUserById(course.teacher_id).name;
            exp.term_id = Convert.ToInt32(course.term_id);
            exp.term_name = CourseDao.GetTermById(exp.term_id).name;
            return exp;
        }

        /// <summary>
        /// 创造带课程和教师名的exp返回
        /// </summary>
        /// <param name="es"></param>
        /// <returns></returns>
        public static List<expReturn> GetExpRet(List<Experiment> es)
        {
            List<expReturn> exps = new List<expReturn>();
            foreach (Experiment e in es)
            {
                exps.Add(GetExpRet(e));
            }
            return exps;
        }


        //增添：

        /// <summary>
        /// 增加新的experiment
        /// </summary>
        /// <param name="experiment"></param>
        /// <returns>0|1</returns>
        /// 思路：传入experiment拥有courseid，name，type，detail，resource
        /// createtime，starttime，endtime，deadline.......
        /// 缺：好像啥都不缺...
        public static int AddExperiment(Experiment experiment)
        {
            using (var dbContext = new DataModels())
            {
                dbContext.Experiments.Add(experiment);
                dbContext.SaveChanges();
                dbContext.Entry(experiment);
                return experiment.id;
            }
        }

        //删除：

        /// <summary>
        /// 按experimentId删除单个experiment
        /// </summary>
        /// <param name="experimentId">experimentId</param>
        /// <returns>0|1</returns>
        public static int DeleteExperimentById(int experimentId)
        {
            using (var dbContext = new DataModels())
            {
                Experiment ex = dbContext.Experiments.Find(experimentId);
                dbContext.Experiments.Remove(ex);
                return dbContext.SaveChanges();
            }
        }

        /// <summary>
        /// 按experimentId删除一组experiment
        /// </summary>
        /// <param name="experimentIds">experimentIds</param>
        /// <returns>0~数组数量</returns>
        /// 返回不为数组数量说明部分错误
        public static int DeleteExperimentByIds(int[] experimentIds)
        {
            using (var dbContext = new DataModels())
            {
                Experiment ex;
                foreach (int experimentId in experimentIds)
                {
                    ex = dbContext.Experiments.Find(experimentId);
                    dbContext.Experiments.Remove(ex);
                }
                return dbContext.SaveChanges();
            }
        }

        //修改：
        //
        //使用quickcopy

        public static int ChangeExperimentInfo(Experiment inExperiment)
        {
            using (var dbContext = new DataModels())
            {
                Experiment experiment = dbContext.Experiments.Find(inExperiment.id);
                QuickCopy.Copy(inExperiment, ref experiment);
                return dbContext.SaveChanges();
            }
        }

        //查找：

        /// <summary>
        /// 返回学生所有实验
        /// </summary>
        /// <param name="stuId">学生id</param>
        /// <returns>experiment对象|null</returns>
        public static List<Experiment> GetExperimentByStuId(string stuId)
        {
            using (DataModels dbContext = new DataModels())
            {
                List<Experiment> allExps = new List<Experiment>();
                List<Course_student_mapping> map = new List<Course_student_mapping>();
                List<Course> course = new List<Course>();
                List<int> list;

                map = dbContext.course_student_mapping.Where(m => m.student_id == stuId).ToList();
                course = dbContext.Courses.ToList().Where(c => map.Exists(m => m.course_id == c.id)).ToList();
                /**
                list = new List<int>();
                foreach(Course_student_mapping cs in map)
                {
                    list.Add(cs.course_id);
                }**/
                //course = dbContext.Courses.Where(c => list.Contains(c.id)).ToList();
                allExps = dbContext.Experiments.ToList();
                allExps = allExps. Where(e => course.Exists(c => c.id == (int)e.course_id)).ToList();
                return allExps;
            }
        }

        /// <summary>
        /// 学生按学期查找实验
        /// </summary>
        /// <param name="stuId">学生id</param>
        /// <param name="termId">学期id</param>
        /// <returns>experiment对象|null</returns>
        public static List<Experiment> GetExperimentByTermIdAndStuId(string stuId, int termId)
        {
            using (DataModels dbContext = new DataModels())
            {
                List<Experiment> allExps = new List<Experiment>();
                List<Course_student_mapping> map = new List<Course_student_mapping>();
                List<Course> course = new List<Course>();

                map = dbContext.course_student_mapping.Where(m => m.student_id == stuId).ToList();
                course = dbContext.Courses.ToList().Where(c => map.Exists(m => m.course_id == c.id)).Where(c => c.term_id == termId).ToList();
                allExps = dbContext.Experiments.ToList().Where(e => course.Exists(c => c.id == e.course_id)).ToList();
                return allExps;
            }
        }

        /// <summary>
        /// 学生按课程id查找实验
        /// </summary>
        /// <param name="stuId">学生id</param>
        /// <param name="courseId">课程id</param>
        /// <returns>experiment对象|null</returns>
        /// **弃用，与单纯依照courseId查找实验功能基本相同
        public static List<Experiment> FindExperimentByCourseIdAndStuId(string stuId, int courseId)
        {
            using (DataModels dbContext = new DataModels())
            {
                List<Experiment> allExps = new List<Experiment>();
                List<Course_student_mapping> map = new List<Course_student_mapping>();
                List<Course> course = new List<Course>();

                map = dbContext.course_student_mapping.Where(m => m.student_id == stuId).ToList();
                course = dbContext.Courses.Where(c => c.id == courseId).ToList();
                allExps = dbContext.Experiments.ToList().Where(e => course.Exists(c => c.id == e.course_id)).ToList();
                return allExps;
            }
        }

        /// <summary>
        /// 按Id查找实验
        /// </summary>
        /// <param name="experimentId">实验id</param>
        /// <returns>experiment对象|null</returns>
        public static Experiment GetExperimentById(int experimentId)
        {
            using (var dbContext = new DataModels())
            {
                Experiment experiment = dbContext.Experiments.Find(experimentId);
                return experiment;
            }
        }

        /// <summary>
        /// 按courseId查找实验
        /// </summary>
        /// <param name="experimentId">实验id</param>
        /// <returns>experiment对象|null</returns>
        public static List<Experiment> GetExperimentByCourseId(int courseId)
        {
            using (var dbContext = new DataModels())
            {
                //int id = Convert.ToInt32(courseId);
                var query = dbContext.Experiments.Where(e => e.course_id == courseId);
                List<Experiment> experiment = query.ToList();
                return experiment;
            }
        }

        /// <summary>
        /// 按courseId查找实验
        /// </summary>
        /// <param name="experimentId">实验id</param>
        /// <returns>experiment对象|null</returns>
        public static List<Experiment> GetExperimentByCourseIdAndTermId(int courseId, int termId)
        {
            using (var dbContext = new DataModels())
            {
                var query = dbContext.Experiments.ToList();
                if (termId != 0)
                {
                    var courses = dbContext.Courses.Where(c => c.term_id == termId).ToList();
                    query = query.Where(e => courses.Exists(c => c.id == e.course_id)).ToList();
                    if (courseId != 0)
                    {
                        query = query.Where(e => e.course_id == courseId).ToList();
                    }
                }
                else if(courseId != 0)
                {
                    query = query.Where(e => e.course_id == courseId).ToList();
                }
                
                
                return query;
            }
        }

        /// <summary>
        /// 按courseName查找实验
        /// </summary>
        /// <param name="experimentId">实验name</param>
        /// <returns>experiment对象|null</returns>
        public static List<Experiment> GetExperimentByCourseName(string courseName)
        {
            using (var dbContext = new DataModels())
            {
                var course = dbContext.Courses.Where(c => c.name == courseName).Single();
                int courseId = course.id;
                var query = dbContext.Experiments.Where(e => e.course_id == courseId);
                List<Experiment> experiment = query.ToList();
                return experiment;
            }
        }

        /// <summary>
        /// 按experimentName查找实验
        /// </summary>
        /// <param name="experimentId">实验name</param>
        /// <returns>experiment对象|null</returns>
        public static Experiment GetExperimentByExperimentName(string experimentName)
        {
            using (var dbContext = new DataModels())
            {
                var query = dbContext.Experiments.Where(e => e.name == experimentName);
                Experiment experiment = query.Single();
                return experiment;
            }
        }

        /// <summary>
        /// 依据老师id获取实验
        /// created by jyf
        /// 2019.7.24
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static List<Experiment> GetExperimentByTeacherId(string id)
        {
            using (var dbContext = new DataModels())
            {
                var query = dbContext.Courses.Where(c => c.teacher_id == id).ToList();
                List<Experiment> allExp = new List<Experiment>();
                allExp = dbContext.Experiments.ToList().Where(e => query.Exists(q => q.id == e.course_id)).ToList();
                return allExp;
            }
        }

        /// <summary>
        /// 依据老师id及课程id获取实验
        /// created by jyf
        /// 2019.8.20
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static List<Experiment> GetExperimentByTeacherId(string id, int courseId)
        {
            using (var dbContext = new DataModels())
            {
                var query = dbContext.Courses.Where(c => c.teacher_id == id).ToList();
                List<Experiment> allExp = new List<Experiment>();
                allExp = dbContext.Experiments.ToList().Where(e => query.Exists(q => q.id == e.course_id)).ToList();
                allExp = allExp.Where(e => e.course_id == courseId).ToList();
                return allExp;
            }
        }

        /// <summary>
        /// 按照applyId获取实验
        /// create by xzh
        /// 2019.8.9
        /// </summary>
        /// <param name="applyId"></param>
        /// <returns></returns>
        public static Experiment GetExperimentByApplyId(int applyId)
        {
            using(var dbContext = new DataModels())
            {
                return dbContext.Experiments.Where(e => e.vm_apply_id == applyId).First();
            }
        }

        /// <summary>
        /// 按照学期获取实验
        /// </summary>
        /// <param name="termId"></param>
        /// <returns></returns>
        public static List<Experiment> GetExperimentByTermId(int termId)
        {
            using (var dbContext = new DataModels())
            {
                List<int>courseIds = dbContext.Courses
                    .Where(c => c.term_id == termId)
                    .Select(c => c.id)
                    .ToList();
                List<Experiment> allExp = dbContext.Experiments
                    .Where(e => courseIds.Contains((int)e.course_id)).ToList();
                return allExp;
            }
        }
    }
}