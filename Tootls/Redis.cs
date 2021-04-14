using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Tools
{
    public class Redis
    {

        public readonly ISubscriber subscriber;
        public readonly IDatabase redisDb;
        public IConnectionMultiplexer Multiplexer => redisDb.Multiplexer;

        public Redis(ConfigurationOptions config)
        {
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(config);
            redisDb = redis.GetDatabase();
            subscriber = redis.GetSubscriber();
        }

       
    }
}
