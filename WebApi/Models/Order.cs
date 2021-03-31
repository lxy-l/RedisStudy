using System.ComponentModel.DataAnnotations;

namespace WebApi.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        public int ShopId { get; set; }
    
        public int Uid { get; set; }

        public int Num { get; set; }
    }
}
