using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using WebApi.Data;
using WebApi.Models;
using WebApi.Services;

namespace Tools
{
   
    public class Queue : BackgroundService
    {

        private readonly JBService _jbservice;
        private readonly Redis _redis;
        private readonly IConfiguration _Configuration;

        public Queue(Redis redis, IConfiguration Configuration)
        {
            _redis = redis;
            _Configuration = Configuration;
            var optionsBuilder = new DbContextOptionsBuilder<JBContext>();
            optionsBuilder.UseSqlServer(_Configuration.GetConnectionString("DefaultConnection"));
            _jbservice = new JBService(new JBContext(optionsBuilder.Options));
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {

            return Task.Run(async () =>
            {
                //List<string> orderlist = new List<string>();
                //Stopwatch stopwatch = new Stopwatch();
                //stopwatch.Start();
                while (true)
                {
                    var data = _redis.redisDb.ListRightPopAsync("PeopleQueue").Result;
                    if (data.HasValue)
                    {
                        var param = data.ToString().Split('-');
                        Console.WriteLine(param);
                        JB jB = await _jbservice.ReduceStockAsync(int.Parse(param[0]), int.Parse(param[1]));
                        if (jB != null)
                        {
                            Console.WriteLine($"库存减少成功:" + jB.Num);
                        }
                        else
                        {
                            Console.WriteLine($"库存减少失败");
                        }
                    }
                    else
                    {
                        Console.WriteLine("队列为空，休眠一秒");
                        Thread.Sleep(1000);
                    }

                    //try
                    //{
                    //    var data =  _redis.redisDb.ListRightPopAsync("PeopleQueue").Result;

                    //    if (data.HasValue)
                    //    {
                    //        orderlist.Add(data);
                    //    }
                    //    else
                    //    {
                    //        Console.WriteLine("队列为空，休眠一秒");
                    //        Thread.Sleep(2000);
                    //    }
                    //    if (orderlist.Count >= 5 || stopwatch.ElapsedMilliseconds > 1000)
                    //    {
                    //        if (orderlist.Count > 0)
                    //        {
                    //            try
                    //            {
                    //                //foreach (var item in orderlist)
                    //                //{
                    //                //    var param = item.Split('-');
                    //                //    JB jB = await _jbservice.ReduceStockAsync(int.Parse(param[0]),int.Parse(param[1]));
                    //                //    if (jB != null)
                    //                //    {
                    //                //        Console.WriteLine($"库存减少成功:" + jB);
                    //                //        orderlist.Clear();
                    //                //    }
                    //                //    else
                    //                //    {
                    //                //        Console.WriteLine($"库存减少失败");
                    //                //    }
                    //                //}
                    //            }
                    //            catch (Exception ex)
                    //            {
                    //                Console.WriteLine($"出列失败:{ex.Message}");
                    //            }
                    //        }
                    //        stopwatch.Restart();
                    //    }

                    //}
                    //catch (Exception ex)
                    //{
                    //    Console.WriteLine($"执行失败：{ex.Message}");
                    //}
                }
            }, stoppingToken);
        }
    }
}
