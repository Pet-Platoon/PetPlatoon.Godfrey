using System.Collections.Generic;
using Godfrey.Models.Common;
using Godfrey.Models.Quotes;
using Godfrey.Models.Servers;

namespace Godfrey.Models.Users
{
    public class User : BaseKeyEntity<ulong>
    {
        public string Name { get; set; }
        public ulong Coins { get; set; }
        public virtual ICollection<Quote> AuthoredQuotes { get; set; }
        public virtual ICollection<Quote> QuotedMessages { get; set; }
        public virtual ICollection<Server> OwnedServers { get; set; }
        public virtual ICollection<ServerMember> Servers { get; set; }
    }
}
