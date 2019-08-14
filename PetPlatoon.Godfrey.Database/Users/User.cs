using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using PetPlatoon.Godfrey.Database.Common;
using PetPlatoon.Godfrey.Database.Quotes;
using PetPlatoon.Godfrey.Database.Servers;

namespace PetPlatoon.Godfrey.Database.Users
{
    public class User : BaseKeyEntity<ulong>, IVersionedEntity
    {
        public string Name { get; set; }

        [ConcurrencyCheck]
        public long Coins { get; set; }

        public DateTime LastCasinoCommandIssued { get; set; }
        public virtual ICollection<Quote> AuthoredQuotes { get; set; }
        public virtual ICollection<Quote> QuotedMessages { get; set; }
        public virtual ICollection<Server> OwnedServers { get; set; }
        public virtual ICollection<ServerMember> Servers { get; set; }

        [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
        public User()
        {
            Version = Guid.NewGuid();
            AuthoredQuotes = new HashSet<Quote>();
            QuotedMessages = new HashSet<Quote>();
            OwnedServers = new HashSet<Server>();
            Servers = new HashSet<ServerMember>();
        }

        public Guid Version { get; set; }
    }
}
