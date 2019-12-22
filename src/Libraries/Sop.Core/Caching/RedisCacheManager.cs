﻿using System;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Sop.Data.Caching;
using StackExchange.Redis;

namespace Sop.Core.Caching
{
    /// <summary>
    ///     使用Redis的缓存服务实现
    /// </summary>
    public sealed class RedisCacheManager : ICacheManager
    {
        private static ConfigurationOptions _connection;
        private static volatile ConnectionMultiplexer _instance;
        private static readonly object Lock = new object();
        private static IDatabase _db;

        /// <summary>
        /// </summary>
        /// <exception cref="Exception"></exception>
        public static ConnectionMultiplexer Instance
        {
            get
            {
                if (_instance != null && _instance.IsConnected)
                    return _instance;
                lock (Lock)
                    try
                    {
                        if (_instance != null && _instance.IsConnected)
                            return _instance;

                        if (_connection == null)
                            throw new Exception("Redis connection string is empty");

                        _instance?.Dispose();
                        _instance = ConnectionMultiplexer.Connect(_connection);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Redis service is not started " + ex.Message);
                    }

                return _instance;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="cnnection"></param>
        public RedisCacheManager(ConfigurationOptions cnnection = null)
        {
            _connection = cnnection;

            _db = Instance.GetDatabase();
        }

        /// <summary>
        /// </summary>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        public IServer Server(EndPoint endPoint)
        {
            return Instance.GetServer(endPoint);
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public EndPoint[] GetEndpoints()
        {
            return Instance.GetEndPoints();
        }

        /// <summary>
        /// </summary>
        /// <param name="db"></param>
        public void FlushDb(int? db = null)
        {
            var endPoints = GetEndpoints();

            foreach (var endPoint in endPoints) Server(endPoint).FlushDatabase(db ?? -1);
        }

        /// <summary>
        /// </summary>
        /// <param name="key"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Get<T>(string key)
        {
            if (_db != null)
            {
                var value = _db?.StringGetAsync(key);
                var obj = Instance.Wait(value);
                if (obj.IsNull)
                    return default;
                return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(obj));
            }

            return default;
        }

        /// <summary>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="data"></param>
        /// <param name="cacheTime"></param>
        public void Set(string key, object data, int cacheTime)
        {
            if (data == null)
                return;

            var entryBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));
            var expiresIn = TimeSpan.FromMinutes(cacheTime);

            //_db.StringSet(key, entryBytes, expiresIn);
            _db.StringSetAsync(key, entryBytes, expiresIn, flags: CommandFlags.FireAndForget);
        }

        /// <summary>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="timeSpan"></param>
        public void Set(string key, object value, TimeSpan timeSpan)
        {
            if (value == null)
                return;
            var entryBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value));
            //_db.StringSet(key, entryBytes, timeSpan);
            _db.StringSetAsync(key, entryBytes, timeSpan, flags: CommandFlags.FireAndForget);
        }

        /// <summary>
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool IsSet(string key)
        {
            return _db.KeyExists(key);
        }

        /// <summary>
        /// </summary>
        /// <param name="key"></param>
        public void Remove(string key)
        {
            _db.KeyDeleteAsync(key, CommandFlags.FireAndForget);
        }

        /// <summary>
        /// </summary>
        /// <param name="pattern"></param>
        public void RemoveByPattern(string pattern)
        {
            foreach (var ep in GetEndpoints())
            {
                var server = Server(ep);
                var keys = server.Keys(pattern: "*" + pattern + "*");
                foreach (var key in keys)
                    _db.KeyDelete(key);
            }
        }

        /// <summary>
        /// </summary>
        public void Clear()
        {
            foreach (var ep in GetEndpoints())
            {
                var server = Server(ep);
                //we can use the code below (commented)
                //but it requires administration permission - ",allowAdmin=true"
                //server.FlushDatabase();

                //that's why we simply interate through all elements now
                var keys = server.Keys();
                foreach (var key in keys)
                    _db.KeyDelete(key);
            }
        }

        /// <summary>
        /// </summary>
        public void Dispose()
        {
        }
    }
}