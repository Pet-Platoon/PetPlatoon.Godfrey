using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetPlatoon.Godfrey.Database.Common
{
    public class BaseKeyEntity<T> : BaseEntity
    {
        [Key]
        [Column(Order = 0)]
        public T Id { get; set; }
    }
}
