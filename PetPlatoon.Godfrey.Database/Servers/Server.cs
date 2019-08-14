using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using PetPlatoon.Godfrey.Database.Common;
using PetPlatoon.Godfrey.Database.Configs;
using PetPlatoon.Godfrey.Database.Quotes;
using PetPlatoon.Godfrey.Database.Users;

namespace PetPlatoon.Godfrey.Database.Servers
{
    public class Server : BaseKeyEntity<ulong>
    {
        public string Name { get; set; }
        public ulong OwnerId { get; set; }
        public virtual User Owner { get; set; }
        
        public virtual ICollection<Config> Configs { get; set; }
        public virtual ICollection<Quote> Quotes { get; set; }
        public virtual ICollection<ServerMember> Members { get; set; }

        [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
        public Server()
        {
            Configs = new HashSet<Config>();
            Quotes = new HashSet<Quote>();
            Members = new HashSet<ServerMember>();
        }
    }
}
