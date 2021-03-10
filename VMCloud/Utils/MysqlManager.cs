using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MySql.Data.MySqlClient;

namespace VMCloud.Utils
{
    public class MysqlManager
    {

        static MySqlConnection conn=null;
        /// <summary>
        /// 连接数据库
        /// </summary>
        /// <returns>success|fail</returns>
        public static String Connect()
        {
            String connetStr = "server=10.251.254.72;port=3306;user=softcloud;password=@buaa21; database=softcloud;";
            
            conn = new MySqlConnection(connetStr);
            try
            {
                conn.Open();//打开通道，建立连接，可能出现异常,使用try catch语句
               // Console.WriteLine("已经建立连接");
                return "success";
                //在这里使用代码对数据库进行增删查改
            }
            catch (MySqlException ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                conn.Close();
            }
            return "fail";

        }
        /// <summary>
        /// 向acloud_server中增加一条记录
        /// </summary>
        /// <param name="name">虚拟机名字</param>
        /// <param name="id">虚拟机id</param>
        /// <returns>success</returns>
        public static String AddServer(String id,String name)
        {
            Connect();
            conn.Open();
            string sql = "insert into acloud_server(id,name,is_image) values('"+id+"','"+name+"',0)";
            MySqlCommand cmd = new MySqlCommand(sql, conn);
            int result = cmd.ExecuteNonQuery();
            return "success";
        }
        /// <summary>
        /// 通过虚拟机名字查找id
        /// </summary>
        /// <param name="name">虚拟机名字</param>
        /// <returns>虚拟机id|Not Found</returns>
        public static String FindServer(String name)
        {

            Connect();
            conn.Open();
            string sql = "select * from acloud_server where name='"+name+"'";
            MySqlCommand cmd = new MySqlCommand(sql, conn);
            MySqlDataReader reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return reader.GetString("id");
            }
            return "Not Found";

        }

        /// <summary>
        /// 通过镜像名字判定是镜像还是模板卷
        /// </summary>
        /// <param name="name">镜像名字</param>
        /// <returns>true|false</returns>
        public static Boolean IsImage(String name)
        {
            Connect();
            conn.Open();
            string sql = "select * from acloud_server where name='" + name + "'";
            MySqlCommand cmd = new MySqlCommand(sql, conn);
            MySqlDataReader reader = cmd.ExecuteReader();
            reader.Read();
            if (reader.GetInt16("is_image") == 1)
                 return true;
            else return false;
        }


        /// <summary>
        /// 根据虚拟机id删除虚拟机
        /// </summary>
        /// <param name="id">虚拟机id</param>
        /// <returns>success</returns>
        public static String DeleteServer(String id)
        {
            Connect();
            conn.Open();
            string sql = "delete from acloud_server where id='" + id+"'";
            MySqlCommand cmd = new MySqlCommand(sql, conn);
            int result = cmd.ExecuteNonQuery();
            return "delete success";
        }

        /// <summary>
        /// 找到网络id
        /// </summary>
        /// <returns>网络id|Not Found</returns>
        public static String FindNetworks()
        {
            Connect();
            conn.Open();
            string sql = "select * from acloud_setting";
            MySqlCommand cmd = new MySqlCommand(sql, conn);
            MySqlDataReader reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                if (reader.GetString("name").Equals("networks"))
                    return reader.GetString("content");
            }
            return "Not Found";
        }

        /// <summary>
        /// 找到可用区名称
        /// </summary>
        /// <returns>可用区名称|Not Found</returns>
        public static String FindZone()
        {
            Connect();
            conn.Open();
            string sql = "select * from acloud_setting";
            MySqlCommand cmd = new MySqlCommand(sql, conn);
            MySqlDataReader reader = cmd.ExecuteReader();
            while(reader.Read())
            {
                if (reader.GetString("name").Equals("availability_zone"))
                    return reader.GetString("content");
            }
            return "Not Found";
        }






    }
}