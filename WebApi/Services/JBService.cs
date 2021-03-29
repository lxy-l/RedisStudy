using WebApi.Data;
using WebApi.Models;

namespace WebApi.Services
{
    public class JBService:IJBService
    {
        private readonly JBContext _context;

        public JBService(JBContext context)
        {
            _context = context;
        }

        public JB ReduceStock(int id, int number)
        {
            JB jb = _context.JBs.Find(id);
            if (jb.Num >= number)
            {
                jb.Num -= number;
                _context.JBs.Update(jb);
                _context.SaveChanges();
                System.Console.WriteLine("扣除数量成功，当前库存剩余：" + jb.Num);
                return jb;
            }
            System.Console.WriteLine("库存不足！当前库存剩余：" + jb.Num);
            return null;
        }

       

    }
}
