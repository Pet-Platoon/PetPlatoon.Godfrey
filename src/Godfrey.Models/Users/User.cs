using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Godfrey.Models.Common;
using Godfrey.Models.Quotes;
using Godfrey.Models.Servers;

namespace Godfrey.Models.Users
{
    public class User : BaseKeyEntity<ulong>, IVersionedEntity
    {
        public string Name { get; set; }
        [ConcurrencyCheck]
        public long Coins { get; set; }
        public DateTime LastCasinoCommandIssued { get; set; }
        public Guid Version { get; set; }
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
    }
}
