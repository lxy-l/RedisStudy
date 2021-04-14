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

        [Route(nameof(SetData))]
        [HttpGet]
        public IActionResult SetData()
        {
            List<JB> list = new List<JB>();
            for (int i = 1; i <= 1000000; i++)
            {
                list.Add(new JB {Name=i.ToString(),Num=100000 });
            }
            _context.JBs.AddRange(list);
            int count=_context.SaveChanges();
            if (count==1000000)
            {
                return Ok();
            }
            else
            {
                return BadRequest();
            }
        }


        [Route(nameof(GetJBsRedis))]
        [HttpGet]
        public IEnumerable<JB> GetJBsRedis(int pageIndex,int pageSize)
        {
            IEnumerable<JB> list = null;
            if (_redis.redisDb.KeyExists("WebApi:JBListString"))
            {
                string str = _redis.redisDb.StringGet("WebApi:JBListString");
                 list = JsonConvert.DeserializeObject<IEnumerable<JB>>(str);
            }
            else
            {
                 list =  _context.JBs.ToList();
                 var liststr = JsonConvert.SerializeObject(list);
                _redis.redisDb.StringSet("WebApi:JBListString", liststr);
            }
            return list.Skip(pageSize * (pageIndex - 1)).Take(pageSize);
        }

        [Route(nameof(GetJBs))]
        [HttpGet]
        public IEnumerable<JB> GetJBs(int pageIndex, int pageSize)
        {
            IEnumerable<JB> list = _context.JBs.Skip(pageSize * (pageIndex - 1)).Take(pageSize).ToList();
            return list;
        }

        // GET: api/JBs/5
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

        // PUT: api/JBs/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
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

        // POST: api/JBs
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
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

        // DELETE: api/JBs/5
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

        private bool JBExists(int id)
        {
            return _context.JBs.Any(e => e.Id == id);
        }
    }
}
