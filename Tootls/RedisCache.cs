using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tools
{

    /// <summary>
    /// Redis缓存的封装类
    /// </summary>
    public class RedisCache
    {
        /*
            Redis ： 是一种高速数据缓存，支持分布式缓存
                     使用键值对存储

            支持的存储类型：
                    string类型：字符串
                    List类型：  列表
                    Set类型 :   无序集合
                    SortedSet类型： 有序集合
                    Hash类型 ：哈希串

             连接串： IP:端口，如本地默认：127.0.0.1:6379
         */

        int DatabaseIndex { get; }
        ConnectionMultiplexer RedisConnection { get; }
        IDatabase Db => RedisConnection.GetDatabase(DatabaseIndex);

        /// <summary>
        /// 默认
        /// </summary>
        public RedisCache()
        {
            DatabaseIndex = 0;
            RedisConnection = ConnectionMultiplexer.Connect("localhost:6379");
            // ConnectionMultiplexer是StackExchange.Redis包的核心对象，在一个应用程序中只有一个该类实例
        }
        /// <summary>
        /// 配置化
        /// </summary>
        /// <param name="config"></param>
        /// <param name="databaseIndex"></param>
        public RedisCache(string config, int databaseIndex = 0)
        {
            this.DatabaseIndex = databaseIndex;
            RedisConnection = ConnectionMultiplexer.Connect(config);
        }
        /// <summary>
        /// 检查Redis缓存键是否存在
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainKey(string key)
        {
            return Db.KeyExists(key);
        }
        /// <summary>
        /// 设置缓存键，带有过期时间
        /// </summary>
        /// <param name="key"></param>
        /// <param name="expire"></param>
        public void SetKeyExpire(string key, TimeSpan expire)
        {
            Db.KeyExpire(key, expire);
        }

        #region string类型
        /*以字符串的格式缓存数据，用途最广*/
        /// <summary>
        /// 通过键取缓存值
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object GetCache(string key)
        {
            // 通过key以字符串格式取值
            var redisValue = Db.StringGet(key);
            if (!redisValue.HasValue)
                return null;
            // 执行JSON序列化
            ValueInfoEntity valueInfo = JsonConvert.DeserializeObject<ValueInfoEntity>(redisValue.ToString());
            object value;
            // 判断值的类型，如果是字符串类型，直接调用value得到结果， 如果不是，接着使用它本身的类型序列化JSON
            if (valueInfo.TypeName == typeof(string).AssemblyQualifiedName)
                value = valueInfo.Value;
            else
                value = JsonConvert.DeserializeObject(valueInfo.Value, Type.GetType(valueInfo.TypeName));

            // 过期时间的判断，如果是相对过期时间，需要再次保存到Redis，就可以延续缓存数据的过期时间
            if (valueInfo.ExpireTime != null && valueInfo.ExpireType == ExpireType.Relative)
                SetKeyExpire(key, valueInfo.ExpireTime.Value);

            return value;
        }
        /// <summary>
        /// 获取缓存值（泛型版）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public T GetCache<T>(string key) where T : class
        {
            return (T)GetCache(key);
        }
        /// <summary>
        /// 删除缓存项
        /// </summary>
        /// <param name="key"></param>
        public void RemoveCache(string key)
        {
            Db.KeyDelete(key);
        }
        /// <summary>
        /// 写入缓存
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="tiemout">过期时间</param>
        /// <param name="expireType">过期时间类型</param>
        public void SetCache(string key, object value, TimeSpan? timeout, ExpireType? expireType)
        {
            // 由于此处使用的是字符串格式缓存，为了能构支持对象缓存，使用JSON序列化存储
            string jsonStr;
            // 如果要缓存的值是string就直接缓存，如果不是则序列化JSON
            if (value is string)
                jsonStr = value as string;
            else
                jsonStr = JsonConvert.SerializeObject(value);
            // 初始化下面的对象，保存缓存值的状态
            ValueInfoEntity valueInfo = new()
            {
                Value = jsonStr,
                TypeName = value.GetType().AssemblyQualifiedName,
                ExpireTime = timeout,
                ExpireType = expireType
            };
            // 将上面的对象序列化，缓存到Redis
            string theValue = JsonConvert.SerializeObject(valueInfo);
            // 如果没有过期时间，直接缓存
            if (timeout == null)
                Db.StringSet(key, theValue);
            else
                Db.StringSet(key, theValue, timeout);
        }

        #endregion

        #region List类型
        /*
         * 有序列表，值可以重复，通过pop、push从头和尾删除或添加数据
           适合制作队列数据结构
         */
         /// <summary>
         /// 获取最后的10条评论
         /// </summary>
        public void LatesTop10()
        {
            for (int i = 1; i <= 100; i++)
            {
                // 入队列100个评论
                Db.ListLeftPush("comment", $"评论：{i}");
            }
            // 取队列的最后10个数据
            Db.ListTrim("comment", 0, 9);

            // 获取评论的队列数据
            RedisValue[] values = Db.ListRange("comment");
            foreach (var item in values)
            {
                Console.Write($"{item} ");
            }
            Db.KeyDelete("comment");
        }
        #endregion

        #region Set类型
        /*
            无序集合，值不能重复
            可以执行交集和并集查询
         */
         /// <summary>
         /// 获取交集、并集查询结果
         /// </summary>
        public void RedisSetTest()
        {
            for (int i = 1; i <= 10; i++)
            {
                Db.SetAdd("文章1", $"用户{i}");
            }

            for (int i = 5; i <= 20; i++)
            {
                Db.SetAdd("文章2", $"用户{i}");
            }
            // 交集
            RedisValue[] inter = Db.SetCombine(SetOperation.Intersect, "文章1", "文章2");
            Console.WriteLine("两篇文章都发表过评论的：");
            foreach (var item in inter)
            {
                Console.Write($"{item} ");
            }

            // 并集
            RedisValue[] union = Db.SetCombine(SetOperation.Union, "文章1", "文章2");
            Console.WriteLine("\n两篇文章评论过之一的：");
            foreach (var item in union)
            {
                Console.Write($"{item} ");
            }

            RedisValue[] dif1 = Db.SetCombine(SetOperation.Difference, "文章1", "文章2");
            Console.WriteLine("\n只评论过文章1的：");
            foreach (var item in dif1)
            {
                Console.Write($"{item} ");
            }

            RedisValue[] dif2 = Db.SetCombine(SetOperation.Difference, "文章2", "文章1");
            Console.WriteLine("\n只评论过文章2的：");
            foreach (var item in dif2)
            {
                Console.Write($"{item} ");
            }

            Db.KeyDelete("文章1");
            Db.KeyDelete("文章2");

        }
        #endregion

        #region SortedSet类型
        /*有序集合，值不可重复*/
        /// <summary>
        /// 查询出文章被点赞最多的10条评论
        /// </summary>
        public void HotestUserTop10()
        {
            // 100个用户评论，每个用户点赞1次
            //List<SortedSetEntry> entries = new();
            for (int i = 1; i <=100 ; i++)
            {
                Db.SortedSetAdd("文章1", $"用户{i}", 1);
            }
            // 用户2点赞2次
            Db.SortedSetIncrement("文章1", "用户2", 2);
            // 用户100点赞4次
            Db.SortedSetIncrement("文章1", "用户100", 4);
            // 获取SortedSet的数据 倒序10个
            RedisValue[] values = Db.SortedSetRangeByRank("文章1", 0, 10, Order.Descending);
            for (int i = 0; i < values.Length; i++)
            {
                Console.WriteLine($"{values[i]}:点赞{Db.SortedSetScore("文章1", values[i])}次");
            }

            Db.KeyDelete("文章1");
        }
        #endregion

        #region Hash
        /*
         * 占用内存最小，速度不太快的类型
           适合以属性存储对象
         */
        /// <summary>
        /// 使用Hash保存对象
        /// </summary>
        public void RedisHashTest()
        {
            Db.HashSet("stu1", "sid", "STOO1");
            Db.HashSet("stu1", "sname", "张三");
            Db.HashSet("stu1", "sage", "12");
            Console.WriteLine($"hash数据：姓名：{Db.HashGet("stu1","sname")}");
            Console.WriteLine("完整的学生信息：");
            RedisValue[] values = Db.HashGet("stu1", new RedisValue[] { "sid", "sname", "sage" });
            Console.WriteLine(string.Join(",",values));

            Db.KeyDelete("stu1");
        }
        #endregion

        #region 队列
        /*
            Reids支持消息队列：
                一种发布和订阅的数据模式： 一个程序发放数据，一个程序接收和处理数据
                将大数据量的请求存放到队列中，逐个处理。为服务器缓解并发请求压力
         */
         // 取Redis频道队列中的值
        public void Subscribe(string subChannel, Action<RedisChannel, RedisValue> handler = null)
        {
            ISubscriber sub = RedisConnection.GetSubscriber();
            sub.Subscribe(subChannel, (x, y) => { handler(x, y); });
        }

        // 写入Redis频道队列的值
        public long Publish<T>(string channel, T msg)
        {
            ISubscriber sub = RedisConnection.GetSubscriber();
            string result = msg is string ? msg.ToString() : JsonConvert.SerializeObject(msg);
            return sub.Publish(channel, result);

        }
        // 删除指定的频道队列
        public void Unsubscribe(string channel)
        {
            ISubscriber sub = RedisConnection.GetSubscriber();
            sub.Unsubscribe(channel);
        }
        // 删除所有队列
        public void UnsubscribeAll()
        {
            ISubscriber sub = RedisConnection.GetSubscriber();
            sub.UnsubscribeAll();
        }
        #endregion
    }

    /// <summary>
    /// 结构体（值类型）为Reids封装类提供可以序列化的数据结构
    /// </summary>
    public struct ValueInfoEntity
    {
        // 保存到Redis的值
        public string Value { get; set; }
        // 保存的类型
        public string TypeName { get; set; }
        // 缓存的过期时间
        public TimeSpan? ExpireTime { get; set; }
        // 过期时间类型
        public ExpireType? ExpireType { get; set; }
    }
    /// <summary>
    ///  过期时间类型
    /// </summary>
    public enum ExpireType
    {
        // 绝对过期时间
        Absolute,
        // 相对过期时间
        Relative
    }

}
