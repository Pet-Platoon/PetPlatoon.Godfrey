using System.Collections.Generic;
using Godfrey.Models.Common;
using Godfrey.Models.Quotes;
using Godfrey.Models.Users;

namespace Godfrey.Models.Servers
{
    public class Server : BaseKeyEntity<ulong>
    {
        public string Name { get; set; }
        public ulong OwnerId { get; set; }
        public virtual User Owner { get; set; }
        public virtual ICollection<Quote> Quotes { get; set; }
        public virtual ICollection<ServerMember> Members { get; set; }
    }
}
