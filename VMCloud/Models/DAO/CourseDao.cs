/*
*	Author:       Xing Zhihuan
*   Email:        1@roycent.cn	
*	Created:      2019/7/16 13:42:34
*   Description:  数据库访问类 课程相关
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using VMCloud.Utils;

namespace VMCloud.Models.DAO
{
    public class CourseDao
    {
        /*
        * Create By zzw
        * 根据课程id，查看课程信息
        * 
        */
        public static Course GetCourseInfoById(int courseId)
        {
            using (var dbContext = new DataModels())
            {
                Course course = dbContext.Courses.Find(courseId);
                if (course != null)
                {
                    return course;
                }
            }
            return null;
        }
        /*
       * Create By zzw
       * 修改课程信息
       * 
       */
        public static void ChangeCourseInfo(Course courseNew)
        {
            using (var dbContext = new DataModels())
            {
                Course course = dbContext.Courses.Find(courseNew.id);
                QuickCopy.Copy<Course>(courseNew, ref course);
                dbContext.SaveChanges();
            }
        }
        /*
      * Create By zzw
      * 根据课程id，删除课程
      * 
      */
        public static void DeleteCourse(int courseId)
        {
            using (var dbContext = new DataModels())
            {
                Course course = dbContext.Courses.Find(courseId);
                if (course != null)
                {
                    dbContext.Courses.Remove(course);
                    dbContext.SaveChanges();
                }
            }
        }
        /*
     * Create By zzw
     * 增加课程
     * 
     */
        public static void AddCourse(Course newCourse)
        {
            using (var dbContext = new DataModels())
            {
                dbContext.Courses.Add(newCourse);
                dbContext.SaveChanges();
            }
        }
        /*
        * Create By zzw
        * 根据课程id返回学生列表
        */
        public static List<User> GetStudentsById(int courseId)
        {
            using (var dbContext = new DataModels())
            {
                List<Course_student_mapping> mapList = dbContext.course_student_mapping.Where(map => map.course_id == courseId).ToList();
                List<User> stuList = new List<User>();
                for (int i = 0; i < mapList.Count; i++)
                {
                    stuList.Add(dbContext.Users.Find(mapList[i].student_id));
                }
                return stuList;
            }
        }
        /*
        * Create By zzw
        * 根据课程id返回学生个数
        */
        public static int GetStudentsNumByCourseId(int courseId)
        {
            using (var dbContext = new DataModels())
            {
                int listNum = dbContext.course_student_mapping.Where(map => map.course_id == courseId).Count();
                return listNum;
            }
        }
        /*
       * Create By zzw
       * 根据课程id返回教师
       */
        public static User GetProfessorById(int courseId)
        {
            using (var dbContext = new DataModels())
            {
                string professorId = dbContext.Courses.Find(courseId).teacher_id;
                User professor = dbContext.Users.Find(professorId);
                return professor;
            }
        }
        /*
      * Create By zzw
      * 根据学生id和学期id，返回课程列表
      */
        public static List<Course> GetCoursesByStuIdAndTermId(string stuId,int termId)
        {
            using (var dbContext = new DataModels())
            {
                List<Course_student_mapping> mapList = dbContext.course_student_mapping.Where(map => map.student_id == stuId).ToList();
                List<Course> courseList = new List<Course>();
                for (int i = 0; i < mapList.Count; i++)
                {
                    int tmp = mapList[i].course_id;
                    Course course = dbContext.Courses.Find(tmp);
                    if(course != null)
                    {
                        if(course.term_id == termId)
                        {
                            courseList.Add(course);
                        }
                    }
                    
                }
                return courseList;
            }
        }
        /*
        * Create By zzw
        * 根据教师id，返回课程列表
        */
        public static List<Course> GetCoursesByProfessorId(string professorId)
        {
            using (var dbContext = new DataModels())
            {
                List<Course> mapList = dbContext.Courses.Where(map => map.teacher_id == professorId).ToList();
                return mapList;
            }
        }

        /// <summary>
        /// created by jyf
        /// 2019.7.22
        /// 增加一个学生-课程
        /// </summary>
        /// <param></param>
        /// <returns></returns>
        public static int AddMap(Course_student_mapping csm)
        {
            using (var dbContext = new DataModels())
            {
                List<Course_student_mapping> map = dbContext.course_student_mapping.Where(t => t.course_id == csm.course_id).Where(t => t.student_id == csm.student_id).ToList();
                if (map.Count == 0)
                {
                    dbContext.course_student_mapping.Add(csm);
                    return dbContext.SaveChanges();
                }
                else return 0;
            }
        }

        /// <summary>
        /// 按课程id删除课程-学生 
        /// created by jyf
        /// 2019.7.22
        /// </summary>
        /// <param name="courseId"></param>
        /// <returns>n</returns>
        public static int DeleteMapByCourseId(int courseId)
        {
            using (var dbContext = new DataModels())
            {
                var vm = dbContext.course_student_mapping.Where(r => r.course_id == courseId);
                vm.ToList().ForEach(t => dbContext.Entry(t).State = System.Data.Entity.EntityState.Deleted);
                dbContext.course_student_mapping.RemoveRange(vm);
                return dbContext.SaveChanges();
            }
        }

        /// <summary>
        /// 按学生id删除课程-学生 
        /// created by jyf
        /// 2019.7.22
        /// </summary>
        /// <param name="courseId"></param>
        /// <returns>n</returns>
        public static int DeleteMapByStudentId(string studentId)
        {
            using (var dbContext = new DataModels())
            {
                var vm = dbContext.course_student_mapping.Where(r => r.student_id == studentId);
                vm.ToList().ForEach(t => dbContext.Entry(t).State = System.Data.Entity.EntityState.Deleted);
                dbContext.course_student_mapping.RemoveRange(vm);
                return dbContext.SaveChanges();
            }
        }

        /// <summary>
        /// 按学生id及课程id删除课程-学生 
        /// created by jyf
        /// 2019.7.22
        /// </summary>
        /// <param name="courseId"></param>
        /// <returns>n</returns>
        public static int DeleteMapByStudentIdAndCourseId(string studentId, int courseId)
        {
            using (var dbContext = new DataModels())
            {
                var vm = dbContext.course_student_mapping.Where(r => r.student_id == studentId).Where(r => r.course_id == courseId);
                vm.ToList().ForEach(t => dbContext.Entry(t).State = System.Data.Entity.EntityState.Deleted);
                dbContext.course_student_mapping.RemoveRange(vm);
                return dbContext.SaveChanges();
            }
        }

        /// <summary>
        /// 按课程id查看关联
        /// created by jyf
        /// 2019.7.22
        /// 
        /// </summary>
        /// <param name="courseId"></param>
        /// <returns>List<Course_student_mapping></returns>
        public static List<Course_student_mapping> GetMapByCourseId(int courseId)
        {
            using (var dbContext = new DataModels())
            {
                var vm = dbContext.course_student_mapping.Where(r => r.course_id == courseId);
                return vm.ToList();
            }
        }

        /// <summary>
        /// 按学生id查看关联
        /// created by jyf
        /// 2019.7.22
        /// 
        /// </summary>
        /// <param name="courseId"></param>
        /// <returns>List<Course_student_mapping></returns>
        public static List<Course_student_mapping> GetMapByStudentId(string studentId)
        {
            using (var dbContext = new DataModels())
            {
                var vm = dbContext.course_student_mapping.Where(r => r.student_id == studentId);
                return vm.ToList();
            }
        }

        /// <summary>
        /// 根据id查看学期名称
        /// created by jyf
        /// 2019.7.23
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Term</returns>
        public static Term GetTermById(int id)
        {
            using (var dbContext = new DataModels())
            {
                return dbContext.Terms.Find(id);
            }
        }

        public static List<Term> GetTerms()
        {
            using (var dbContext = new DataModels())
            {
                return dbContext.Terms.ToList();
            }
        }

        /// <summary>
        /// 根据id查看学院
        /// created by zzw
        /// 2019.7.23
        /// </summary>
        /// <param name="departmentId"></param>
        /// <returns>Department</returns>
        public static Department GetDepartmentById(string departmentId)
        {
            if (departmentId == null)
                return null;
            using (var dbContext = new DataModels())
            {
                return dbContext.Departments.Find(departmentId);
            }
        }

        /// <summary>
        /// 获取学院全部
        /// created by jyf
        /// 2019.7.29
        /// </summary>
        /// <returns>List<Department></returns>
        public static List<Department> GetDepartments()
        {
            using (var dbContext = new DataModels())
            {
                return dbContext.Departments.ToList();
            }
        }

        /// <summary>
        /// 根据学期名称查看学期
        /// created by jyf
        /// 2019.7.23
        /// </summary>
        /// <param name="name"></param>
        /// <returns>Term</returns>
        public static Term GetTermByName(string name)
        {
            using (var dbContext = new DataModels())
            {
                List<Term> terms = dbContext.Terms.Where(i => i.name == name).ToList();
                if (terms.Count == 0) return null;
                return terms.Single();
            }
        }

        public static List<Assistant> GetAssistantsByCourseId(int courseId)
        {
            using (var context = new DataModels())
            {
                return context.Assistants.Where(a => a.course_id == courseId).ToList();
            }
        }

        public static List<Assistant> GetAssistantsByStuId(string stuId)
        {
            using (var context = new DataModels())
            {
                return context.Assistants.Where(a => a.student_id == stuId).ToList();
            }
        }

        public static List<Course> GetCourseByDepId(string depId)
        {
            using (var context = new DataModels())
            {
                return context.Courses.Where(c => c.department_id == depId).ToList();
            }
        }
        public static List<Course> GetAllCourse()
        {
            using (var context = new DataModels())
            {
                return context.Courses.ToList();
            }
        }


    }
}