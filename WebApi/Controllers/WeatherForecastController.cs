using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Tools;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly Redis _redis;

        public WeatherForecastController(ILogger<WeatherForecastController> logger,Redis redis)
        {
            _logger = logger;
            _redis = redis;
        }



        [HttpPost]
        public IActionResult Post()
        {
            var rng = new Random();
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            for (int i = 0; i < 100000; i++)
            {
                //_redis.redisDb.StringSetAsync(i.ToString(), rng.ToString()).ConfigureAwait(false);
                _redis.redisDb.PublishAsync("Messages",i.ToString()).ConfigureAwait(false);
            }
            stopwatch.Stop();
            //var b= _redis.StringSet(num.ToString(), rng.ToString());
            //if (b)
            //{
            //    return Ok();
            //}
            //return NotFound();
            return Ok(new { Time=stopwatch.ElapsedMilliseconds});
        }



        [HttpGet]
        public IActionResult Get()
        {
            _redis.subscriber.SubscribeAsync("Messages", (channel, message) =>
            {
                Console.WriteLine(channel + ":" + message);
            }).ConfigureAwait(false);

            return Ok();
        }   
    }
}
