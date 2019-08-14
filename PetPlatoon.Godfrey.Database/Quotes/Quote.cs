using System;
using PetPlatoon.Godfrey.Database.Common;
using PetPlatoon.Godfrey.Database.Servers;
using PetPlatoon.Godfrey.Database.Users;

namespace PetPlatoon.Godfrey.Database.Quotes
{
    public class Quote : BaseKeyEntity<ulong>
    {
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsNotSafeForWork { get; set; }
        public bool IsDeleted { get; set; }
        public ulong AuthorId { get; set; }
        public ulong QuoterId { get; set; }
        public ulong ServerId { get; set; }
        public ulong ChannelId { get; set; }
        public virtual User Author { get; set; }
        public virtual User Quoter { get; set; }
        public virtual Server Server { get; set; }
    }
}
