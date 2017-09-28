using System;
using Godfrey.Models.Common;

namespace Godfrey.Models.Quotes
{
    public class Quote : BaseKeyEntity<int>
    {
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ulong AuthorId { get; set; }
        public ulong QuoterId { get; set; }
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong MessageId { get; set; }

        public string AuthorName { get; set; }
        public string QuoterName { get; set; }
        public string Message { get; set; }
    }
}
