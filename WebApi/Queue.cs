using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using WebApi.Services;

namespace Tools
{

    public class Queue : BackgroundService
    {

        private readonly Redis _redis;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public Queue(Redis redis,IServiceScopeFactory serviceScopeFactory)
        {
            _redis = redis;
            _serviceScopeFactory = serviceScopeFactory;
        }


        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Run(async () =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    await _redis.redisDb.ListRightPopAsync("PeopleQueue").ContinueWith(task =>
                    {
                        if (task.Result.HasValue)
                        {
                            int[] param = Array.ConvertAll(task.Result.ToString().Split('-'), s => int.Parse(s));
                            if (HandleData(param))
                            {
                                Console.WriteLine("成功出队：" + task.Result);
                            }
                            else
                            {
                                _redis.redisDb.ListLeftPushAsync("PeopleQueue", task.Result);
                                Console.WriteLine("出列失败，返回队列中");
                            }
                           
                        }
                        else
                        {
                            Console.WriteLine("休眠2秒");
                            Thread.Sleep(2000);
                        }

                    }, stoppingToken);
                }
            });
        }


        /**
         * 由_serviceScopeFactory.CreateScope()创建的外部scope在每个using语句之后被释放，
         * 而每个消息仍然试图依赖现在已释放的作用域和附加的上下文来处理该消息。
         * 
         * 如果直接释放会出现下列问题：
         * 1.System.ObjectDisposedException:'无法访问已释放的上下文实例。导致此错误的一个常见原因是释放从依赖注入解析的上下文实例，然后在应用程   序的其他地方尝试使用相同的上下文实例。如果对上下文实例调用“Dispose”，或将其包装在using语句中，则可能会发生这种情况。如果使用       依赖注    入，则应该让依赖注入容器负责处理上下文实例。对象名：'IntegrationDbContext'
         * **/
        private bool HandleData(int[] param)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                JBService _jbservice = scope.ServiceProvider.GetRequiredService<JBService>();
                return _jbservice.ReduceStock(param[0], param[1])!=null;
            }
        }
    }
}
