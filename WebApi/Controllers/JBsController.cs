using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using StackExchange.Redis;
using Tools;
using WebApi.Data;
using WebApi.Models;
using WebApi.Services;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JBsController : ControllerBase
    {
        private readonly JBContext _context;
        private readonly Redis _redis;
        private readonly IJBService _jbservice;

        private static readonly object obj = new object();

        public JBsController(JBContext context,Redis redis, IJBService jbService)
        {
            _context = context;
            _redis = redis;
            _jbservice = jbService;
        }

        /// <summary>
        /// 秒杀接口（Redis）
        /// </summary>
        /// <param name="id"></param>
        /// <param name="number"></param>
        /// <returns></returns>
        [Route(nameof(RedisTest))]
        [HttpPost]
        public async Task<IActionResult> RedisTest(string id, int number)
        {
            int redisStock = (int)await _redis.redisDb.StringGetAsync(id);
            if (redisStock == 0)
            {
                return NotFound(new { message = "秒杀结束！" });
            }
            long stock = await _redis.redisDb.StringDecrementAsync(id, number);
            if (stock < 0)
            {
                redisStock = (int)await _redis.redisDb.StringGetAsync(id);
                if (redisStock != 0 && redisStock < number)
                {
                    await _redis.redisDb.StringIncrementAsync(id, number);
                }
                else if (redisStock == 0)                       
                {
                    await _redis.redisDb.StringSetAsync(id, 0);
                }
                return NotFound(new { message = "库存不足,秒杀结束！" });
            }
            //加入队列
            await _redis.redisDb.ListLeftPushAsync("PeopleQueue", $"{id}-{number}");
            return Ok(new { message = $"秒杀成功{number}个商品！" });
        }

        /// <summary>
        /// 秒杀接口（数据库有锁）
        /// </summary>
        /// <param name="id"></param>
        /// <param name="number"></param>
        /// <returns></returns>
        [Route(nameof(LockDBTest))]
        [HttpPost]
        public  IActionResult LockDBTest(int id, int number)
        {

            lock (obj)
            {
                JB jB = _jbservice.ReduceStock(id, number);
                if (jB!=null)
                {
                    return Ok(jB);
                }
                else
                {
                    return NotFound();
                }
            }
        }

        /// <summary>
        /// 秒杀接口（数据库无锁）
        /// </summary>
        /// <param name="id"></param>
        /// <param name="number"></param>
        /// <returns></returns>
        [Route(nameof(DBTest))]
        [HttpPost]
        public IActionResult DBTest(int id, int number)
        {
            JB jB = _jbservice.ReduceStock(id, number);
            if (jB != null)
            {
                return Ok(jB);
            }
            else
            {
                return NotFound();
            }
        }

        /// <summary>
        /// 添加100万数据到数据库
        /// </summary>
        /// <returns></returns>
        [Route(nameof(SetData))]
        [HttpGet]
        public IActionResult SetData()
        {
            if (!_context.JBs.Any())
            {
                List<JB> list = new List<JB>();
                for (int i = 1; i <= 1000000; i++)
                {
                    list.Add(new JB { Name = i.ToString(), Num = 100000 });
                }
                _context.JBs.AddRange(list);
                int count = _context.SaveChanges();
                if (count == 1000000)
                {
                    return Ok();
                }
                else
                {
                    return NoContent();
                }
            }   
            return Ok();

        }

        /// <summary>
        /// 设置缓存数据
        /// </summary>
        /// <returns></returns>
        [Route(nameof(SetRedisCache))]
        [HttpGet]
        public IActionResult SetRedisCache()
        {
            IEnumerable<JB> list = _context.JBs.ToList();
            List<RedisValue> listRedisValue = new List<RedisValue>();
            foreach (var item in list)
            {
                string json = JsonConvert.SerializeObject(item);
                listRedisValue.Add(json);
            }
            _redis.redisDb.SetAddAsync("JbSet", listRedisValue.ToArray());
            return Ok();
        }

        /// <summary>
        /// 获取JB列表（缓存）
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [Route(nameof(GetJBsRedisAsync))]
        [HttpGet]
        public IEnumerable<JB> GetJBsRedisAsync(int pageIndex,int pageSize)
        {
            List<JB> result = new List<JB>();
            if (_redis.redisDb.SetLength("JbSet")>0)
            {
                var arr = _redis.redisDb.SetMembers("JbSet");
                foreach (var item in arr)
                {
                    if (!item.IsNullOrEmpty)
                    {
                        var t = JsonConvert.DeserializeObject<JB>(item);
                        if (t != null)
                        {
                            result.Add(t);
                        }
                    }
                }
            }
            return result.OrderBy(x=>x.Id).Skip(pageSize * (pageIndex - 1)).Take(pageSize);
        }

        /// <summary>
        /// 获取JB列表（数据库）
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [Route(nameof(GetJBs))]
        [HttpGet]
        public IEnumerable<JB> GetJBs(int pageIndex, int pageSize)
        {
            IEnumerable<JB> list = _context.JBs.OrderBy(x=>x.Id).Skip(pageSize * (pageIndex - 1)).Take(pageSize).ToList();
            return list;
        }

        /// <summary>
        /// 获取单个JB对象
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<JB>> GetJB(int id)
        {
            var jB = await _context.JBs.FindAsync(id);

            if (jB == null)
            {
                return NotFound();
            }

            return jB;
        }

        /// <summary>
        /// 修改JB对象
        /// </summary>
        /// <param name="id"></param>
        /// <param name="jB"></param>
        /// <returns></returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> PutJB(int id, JB jB)
        {
            if (id != jB.Id)
            {
                return BadRequest();
            }

            _context.Entry(jB).State = EntityState.Modified;

            try
            {
                
               
                if (await _context.SaveChangesAsync() == 1)
                {
                    ////更新redis缓存
                    _redis.redisDb.StringSet(jB.Id.ToString(), jB.Num);
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!JBExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        /// <summary>
        /// 添加JB对象
        /// </summary>
        /// <param name="jB"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult<JB>> PostJB(JB jB)
        {
            _context.JBs.Add(jB);
           
            if (await _context.SaveChangesAsync() == 1)
            {
                ////更新redis缓存
                _redis.redisDb.StringSet(jB.Id.ToString(), jB.Num);
            }

            return CreatedAtAction("GetJB", new { id = jB.Id }, jB);
        }

        /// <summary>
        /// 删除JB对象
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteJB(int id)
        {
            var jB = await _context.JBs.FindAsync(id);
            if (jB == null)
            {
                return NotFound();
            }

            _context.JBs.Remove(jB);
           
            if (await _context.SaveChangesAsync() == 1)
            {
                 //删除redis缓存
                _redis.redisDb.KeyDelete(jB.Id.ToString());
            }

            return NoContent();
        }

        /// <summary>
        /// 判断是否存在JB对象
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private bool JBExists(int id)
        {
            return _context.JBs.Any(e => e.Id == id);
        }
    }
}
