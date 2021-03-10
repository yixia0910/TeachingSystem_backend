/*
*	Author:       Xing Zhihuan
*   Email:        1@roycent.cn	
*	Created:      2019/7/17 20:18:08
*   Description:  Dao相关辅助类
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;

namespace VMCloud.Utils
{
	public static class DaoUtil
    {    /// <summary>
         /// 分页
         /// </summary>
         /// <param name="list"> 数据源 </param>
         /// <param name="order"> 排序表达式 </param>
         /// <param name="page"> 第几页 </param>
         /// <param name="size"> 每页记录数 </param>
         /// <param name="count"> 记录总数 </param>
         /// <returns></returns>
        public static IQueryable<T> Pagination<T, TKey>(this IQueryable<T> list, Expression<Func<T, TKey>> order, int page, int size, out int count, Expression<Func<T, TKey>> order2 = null)
        {
            count = list.Count();
            if (order2 == null)
                return list.Distinct().OrderBy(order).Skip((page - 1) * size).Take(size);
            else
                return list.Distinct().OrderBy(order).ThenBy(order2).Skip((page - 1) * size).Take(size);
        }
    }
}