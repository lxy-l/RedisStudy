using System.Threading.Tasks;
using WebApi.Data;
using WebApi.Models;

namespace WebApi.Services
{
    public class JBService
    {
        private readonly JBContext _context;

        public JBService(JBContext context)
        {
            _context = context;
        }

        public async Task<JB>  ReduceStockAsync(int id,int number)
        {
            JB jB = await _context.JBs.FindAsync(id);
            if (jB.Num >= number)
            {
                jB.Num -= number;
                _context.JBs.Update(jB);
                await _context.SaveChangesAsync().ConfigureAwait(false);
                return jB;
            }
            return null;
        }

    }
}
