using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Tools
{
    public class Redis
    {

        private readonly ConnectionMultiplexer redis;
        private readonly IDatabase redisDb;
        private readonly ISubscriber sub;

        public Redis(string configstr="localhost:6379")
        {

            var config = new ConfigurationOptions
            {
                AbortOnConnectFail = false,
                AllowAdmin = true,
                ConnectTimeout = 15000,
                SyncTimeout = 5000,
                Password = "Pwd",//Redis数据库密码
                EndPoints = { configstr }// connectionString 为IP:Port 如”192.168.2.110:6379”
            };
            redis = ConnectionMultiplexer.Connect(config);
            redisDb = redis.GetDatabase(0);
            sub = redis.GetSubscriber();
        }

        public bool KeyExists(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return redisDb.KeyExists(key,flags);
        }

        public bool StringSet(RedisKey key, RedisValue value, TimeSpan? expiry = null)
        {
            return  redisDb.StringSet(key,value,expiry);
        }

        public async Task<bool> StringSetAsync(RedisKey key, RedisValue value, TimeSpan? expiry = null)
        {
            return await redisDb.StringSetAsync(key, value, expiry).ConfigureAwait(false);
        }

        public long StringDecrement(RedisKey key, long value = 1, CommandFlags flags = CommandFlags.None)
        {
            return redisDb.StringDecrement(key, value, flags);
        }

        public async Task<long> StringDecrementAsync(RedisKey key, long value = 1, CommandFlags flags = CommandFlags.None)
        {
            return await redisDb.StringIncrementAsync(key, value, flags);
        }

        public long StringIncrement(RedisKey key, long value = 1, CommandFlags flags = CommandFlags.None)
        {
            return redisDb.StringIncrement(key, value, flags);
        }
        public async Task<long> StringIncrementAsync(RedisKey key, long value = 1, CommandFlags flags = CommandFlags.None)
        {
            return await redisDb.StringIncrementAsync(key, value, flags);
        }

        public RedisValue StringGet(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return redisDb.StringGet(key, flags);
        }

        public async Task<RedisValue> StringGetAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return await redisDb.StringGetAsync(key, flags);
        }


        public void GetSubscriber()
        {
            sub.Subscribe("messages", (channel, message) =>
            {
                Console.WriteLine(channel + ":" + message);
            });
        }

        public void Publish(string i)
        {
            sub.Publish("messages", i);
        }



    }
}
