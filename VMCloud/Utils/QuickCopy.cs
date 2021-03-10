/*
*	Author:       Xing Zhihuan
*   Email:        1@roycent.cn	
*	Created:      2019/7/15 22:58:07
*   Description:  对象复制类
 */
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace VMCloud.Utils
{
    public class QuickCopy
    {
        /// <summary>
        /// 将JObject对象中的数据复制到相应对象中，JObject中没有的字段数据在相应对象中置为null
        /// </summary>
        /// <typeparam name="T">需要转换的类型</typeparam>
        /// <param name="src">源</param>
        /// <param name="dst">目的类</param>
        public static void Copy<T>(JObject src, ref T dst)
        {
            var properties = dst.GetType().GetProperties();
            foreach(var pi in properties)
            {
                var data = src.Property(pi.Name);
                if(data!=null)
                {
                    pi.SetValue(dst,Convert.ChangeType(src.Property(pi.Name).Value, pi.PropertyType), null);
                }
            }
        }

        /// <summary>
        /// 将一个类中的数据复制到另一个类中。
        /// 跳过源类中为null的字段，两个类都有的字段，源类中的数据会覆盖目的类
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="src">源</param>
        /// <param name="dst">目的</param>
        public static void Copy<T>(T src, ref T dst)
        {
            var properties = dst.GetType().GetProperties();
            var type = src.GetType();
            foreach(var pi in properties)
            {
                var value = type.GetProperty(pi.Name).GetValue(src, null);
                if (value!=null)
                {
                    pi.SetValue(dst, value);
                }
            }
        }
    }
}