/*
*	Author:       Xing Zhihuan
*   Email:        1@roycent.cn	
*	Created:      2019/7/11 19:49:53
*   Description:  系统普通日志记录类
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using VMCloud.Models;

namespace VMCloud.Utils
{
    public class LogUtil
    {
        /// <summary>
        /// 记录一个日志
        /// </summary>
        /// <param name="request"></param>
        /// <param name="target_type"></param>
        /// <param name="target_id"></param>
        /// <param name="operator_id"></param>
        /// <param name="_operator_role"></param>
        /// <param name="_state"></param>
        /// <param name="_content"></param>
        /// <param name="_complete_time"></param>
        /// <returns></returns>
        public static bool Log(HttpRequestMessage request, string target_type, string target_id, string operator_id, int? _operator_role, string _state = "", string _content = "", string _complete_time = "")
        {
            try
            {
                using (var logger = new Logger())
                {
                    System_log log = new System_log
                    {
                        operate_target_type = target_type,
                        operate_target_id = target_id,
                        operator_id = operator_id,
                        operator_role = _operator_role,
                        state = _state,
                        content = _content,
                        complete_time = _complete_time,
                        time = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")
                    };
                    string user_ip = HttpUtil.GetHeader(request, "X-Forwarded-For").First().ToString();
                    log.user_ip = user_ip;
                    logger.Logs.Add(log);
                    logger.SaveChanges();
                    return true;
                }
            }catch(Exception e)
            {
                ErrorLogUtil.WriteLogToFile(e, request);
                return false;
            }

        }
    }
}