/*
*	Author:       Xing Zhihuan
*   Email:        1@roycent.cn	
*	Created:      2019/7/10 16:28:04
*   Description:  Redis操作类
 */
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Web;

namespace VMCloud.Models.DAO
{
    public class RedisHelper
    {
        /// <summary>
        /// 连接字符串
        /// </summary>
        private static readonly string ConnectionString = ConfigurationManager.AppSettings["RedisConnectionString"];
        /// <summary>
        /// 锁
        /// </summary>
        private static readonly object _lock = new object();
        /// <summary>
        /// 连接对象
        /// </summary>
        private volatile static IConnectionMultiplexer _connection = null;
        /// <summary>
        /// 数据库
        /// </summary>
        private static RedisHelper myRedisHelper = null;
        private static IDatabase _db = null;
        public static RedisHelper GetRedisHelper()
        {
            if(_db==null || _connection == null || !_connection.IsConnected)
            {
                if(myRedisHelper == null)
                {
                    myRedisHelper = new RedisHelper();
                }
                _connection = ConnectionMultiplexer.Connect(ConnectionString);
                _db = GetDatabase();
            }
            return myRedisHelper;
        }
        /// <summary>
        /// 获取连接
        /// </summary>
        /// <returns></returns>
        protected static IConnectionMultiplexer GetConnection()
        {
            if (_connection != null && _connection.IsConnected)
            {
                return _connection;
            }
            lock (_lock)
            {
                if (_connection != null && _connection.IsConnected)
                {
                    return _connection;
                }

                if (_connection != null)
                {
                    _connection.Dispose();
                }
                _connection = ConnectionMultiplexer.Connect(ConnectionString);
            }

            return _connection;
        }
        /// <summary>
        /// 获取数据库
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        public static  IDatabase GetDatabase(int? db = null)
        {
            return GetConnection().GetDatabase(db ?? -1);
        }
        /// <summary>
        /// 设置
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="data">值</param>
        /// <param name="cacheTime">时间</param>
        public virtual void Set(string key, object data, int cacheTime)
        {
            if (data == null)
            {
                return;
            }
            var entryBytes = Serialize(data);
            var expiresIn = TimeSpan.FromMinutes(cacheTime);

            _db.StringSet(key, entryBytes, expiresIn);
        }
        /// <summary>
        /// 根据键获取值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public virtual T Get<T>(string key)
        {

            var rValue = _db.StringGet(key);
            if (!rValue.HasValue)
            {
                return default(T);
            }

            var result = Deserialize<T>(rValue);

            return result;
        }
        /// <summary>
        /// 反序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serializedObject"></param>
        /// <returns></returns>
        protected virtual T Deserialize<T>(byte[] serializedObject)
        {
            if (serializedObject == null)
            {
                return default(T);
            }
            var json = Encoding.UTF8.GetString(serializedObject);
            return JsonConvert.DeserializeObject<T>(json);
        }
        /// <summary>
        /// 判断是否已经设置
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public virtual bool IsSet(string key)
        {
            return _db.KeyExists(key);
        }
        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="data"></param>
        /// <returns>byte[]</returns>
        private byte[] Serialize(object data)
        {
            var json = JsonConvert.SerializeObject(data);
            return Encoding.UTF8.GetBytes(json);
        }
        /// <summary>
        /// 删除键
        /// </summary>
        /// <param name="key"></param>
        public void Delete(string key)
        {
            _db.KeyDelete(key);
        }
    }
}