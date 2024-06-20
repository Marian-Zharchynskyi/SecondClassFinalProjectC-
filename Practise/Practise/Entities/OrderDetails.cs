using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Storage.Core.Entities
{
    public class OrderDetails
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int OrderProductQuantity { get; set; }
        public float Price { get; set; }
        public virtual Product Product { get; set; }
        public virtual Order Order { get; set; }

    }
}
