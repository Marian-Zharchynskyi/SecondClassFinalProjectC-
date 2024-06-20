using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Storage.Core.Entities
{
    public class Order
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public DateTime OrderDate { get; set; }
        public virtual Status Status { get; set; }
        public virtual ICollection<OrderDetails> OrderDetails { get; set; }
    }
}
