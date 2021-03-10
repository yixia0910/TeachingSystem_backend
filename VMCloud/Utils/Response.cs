/*
*	Author:       Xing Zhihuan
*   Email:        1@roycent.cn	
*	Created:      2019/7/9 20:15:23
*   Description:  简化Response封装过程
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using Newtonsoft.Json;

namespace VMCloud.Utils
{
    public class Response
    {
        public int code;
        public string msg;
        public object data;

        public Response()
        {
            code = 0;
            msg = null;
            data = null;
        }
        public Response(int code, string msg = null, object data=null)
        {
            this.code = code;
            if (msg == null)
                this.msg = HttpUtil.Message[code];
            else
                this.msg = msg;
            this.data = data;
        }
        override public string ToString()
        {
            string ret = JsonConvert.SerializeObject(this);
            return ret;
        }

        public HttpResponseMessage Convert()
        {
            StringContent sc = new StringContent(JsonConvert.SerializeObject(this), System.Text.Encoding.GetEncoding("UTF-8"), "application/json");
            return new HttpResponseMessage { Content = sc };
        }

        public static HttpResponseMessage Error()
        {
            Response ret = new Response(4001);
            return ret.Convert();
        }
    }
}