using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WebApi.Data;
using WebApi.Models;
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
