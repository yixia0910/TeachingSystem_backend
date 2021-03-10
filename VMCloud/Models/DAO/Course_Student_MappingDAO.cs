/*
*	Author:       jyf
*	Created:      2019/7/16 
*   Description:  数据库访问类 学生-课程关联相关
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using VMCloud.Utils;

namespace VMCloud.Models.DAO
{
    
    public class Course_Student_MappingDao
    {   
        //增加

        /// <summary>
        /// created by jyf
        /// 2019.7.22
        /// 增加一个学生-课程
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        public static int AddMap(Course_student_mapping csm)
        {
            using (var dbContext = new DataModels())
            {
                dbContext.course_student_mapping.Add(csm);
                return dbContext.SaveChanges();
            }
        }

        //删除

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

        //修改
        //目前不需要

        //查看

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
    }
}