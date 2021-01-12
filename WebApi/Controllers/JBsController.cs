using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Data;
using WebApi.Models;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JBsController : ControllerBase
    {
        private readonly JBContext _context;

        public JBsController(JBContext context)
        {
            _context = context;
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
