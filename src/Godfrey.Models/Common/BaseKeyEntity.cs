using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Godfrey.Models.Common
{
    public class BaseKeyEntity<T> : BaseEntity
    {
        [Key]
        [Column(Order = 0)]
        public T Id { get; set; }
    }
}
