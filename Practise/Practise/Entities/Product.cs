using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace Storage.Core.Entities
{
    public class Product
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Name {  get; set; }
        public float Price {  get; set; }
        public string Image {  get; set; }
        public int ProductQuantity {  get; set; }
        [JsonIgnore]
        public virtual Category Category { get; set; }
        public virtual ICollection<StorageTransaction> StorageTransactions { get; set; }
        public virtual ICollection<OrderDetails> OrderDetails { get; set; }
    }
}
