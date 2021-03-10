/*
*	Author:       jyf
*	Created:      2019/7/23
*   Description:  数据库访问类 学期相关
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using VMCloud.Utils;

namespace VMCloud.Models.DAO
{
    public class TermDao
    {
        //查看

        /// <summary>
        /// 根据id查看学期名称
        /// created by jyf
        /// 2019.7.23
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Term</returns>
        public static Term GetTermById(int id)
        {
            using(var dbContext = new DataModels())
            {
                return dbContext.Terms.Find(id);
            }
        }
    }
}