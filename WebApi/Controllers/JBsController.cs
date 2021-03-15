using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tools;
using WebApi.Data;
using WebApi.Models;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JBsController : ControllerBase
    {
        private readonly JBContext _context;
        private readonly Redis _redis;

        private static readonly object obj = new object();

        public JBsController(JBContext context,Redis redis)
        {
            _context = context;
            _redis = redis;
        }



        [Route("Test2")]
        [HttpPost]
        public async Task<IActionResult> Test(string id, int number)
        {
            int redisStock = (int)await _redis.redisDb.StringGetAsync(id);
            if (redisStock == 0)
            {
                return Ok(new { message = "秒杀结束！" });
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
                return Ok(new { message = "库存不足,秒杀结束！" });
            }
            //加入队列
            return Ok(new { message = $"秒杀成功{number}个商品！" });
        }

        [Route("Test")]
        [HttpPost]
        public  IActionResult Test(int id, int number)
        {

            lock (obj)
            {
                JB jB =  _context.JBs.Find(id);
                if (jB.Num >= number)
                {
                    jB.Num -= number;
                    _context.JBs.Update(jB);
                    _context.SaveChanges();
                    return Ok(jB);
                }
            }
            return NotFound();
        }



        // GET: api/JBs
        [HttpGet]
        public async Task<ActionResult<IEnumerable<JB>>> GetJBs()
        {
            return await _context.JBs.ToListAsync();
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
                await _context.SaveChangesAsync();
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
            await _context.SaveChangesAsync();

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
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool JBExists(int id)
        {
            return _context.JBs.Any(e => e.Id == id);
        }
    }
}
