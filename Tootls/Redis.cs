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

        public Redis()
        {
            redis = ConnectionMultiplexer.Connect("localhost:6379");
            redisDb = redis.GetDatabase(0);
            sub = redis.GetSubscriber();
        }


        public bool StringSet(RedisKey key, RedisValue value, TimeSpan? expiry = null)
        {
            return  redisDb.StringSet(key,value,expiry);
        }

        public async Task<bool> StringSetAsync(RedisKey key, RedisValue value, TimeSpan? expiry = null)
        {
            return await redisDb.StringSetAsync(key, value, expiry).ConfigureAwait(false);
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
