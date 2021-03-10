/*
*	Author:       Xing Zhihuan
*   Email:        1@roycent.cn	
*	Created:      2019/7/17 20:20:02
*   Description:  
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using VMCloud.Utils;

namespace VMCloud.Models.DAO
{
    public class TestDao
    {
        //仅供测试
        public static void ChangeCourseInfoTest(Course newCourse)
        {
            using (var dbContext = new DataModels())
            {
                Course course = dbContext.Courses.Find(newCourse.id);
                QuickCopy.Copy<Course>(newCourse, ref course);
                dbContext.SaveChanges();
            }
        }

        //仅供测试
        public static Course GetCourse(int courseId)
        {
            using (var dbContext = new DataModels())
            {
                return dbContext.Courses.Find(courseId);
            }
        }
        //仅供测试
        /// <summary>
        /// 分页获取课程信息
        /// </summary>
        /// <param name="page">第几页（从1开始）</param>
        /// <param name="size">每页显示多少条数据</param>
        /// <param name="hasMore">是否有更多数据</param>
        /// <returns>请求页数的数据</returns>
        public static List<Course> GetCoursesPagination(int page, int size, out bool hasMore)
        {
            using (var dbContext = new DataModels())
            {
                int count;
                List<Course> courses = dbContext.Courses.Pagination(c => c.id, page, size, out count).ToList();
                hasMore = ((courses.Count < size)) || (((page-1)*size+courses.Count) >= count) ? false : true;
                return courses;
            }
        }
    }
}