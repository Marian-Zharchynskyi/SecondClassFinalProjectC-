using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storage.Core.Entities
{
    public class StorageTransaction
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Name { get; set; }
        public int QuantityChanged { get; set; }
        public virtual TransactionType TransactionType { get; set; }
        public virtual Product Product { get; set; }
    }
}
