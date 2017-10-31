using System.Collections.Generic;
using Godfrey.Models.Common;
using Godfrey.Models.Context;
using Godfrey.Models.Quotes;
using Godfrey.Models.Servers;

namespace Godfrey.Models.Users
{
    public class User : BaseKeyEntity<ulong>
    {
        public string Name { get; set; }

        public ulong Coins { get; set; }

        public IEnumerable<Quote> AuthoredQuotes { get; set; }

        public IEnumerable<Quote> QuotedMessages { get; set; }

        public IEnumerable<Server> Servers { get; set; }

        public static User CreateUser(ulong userId, string name, DatabaseContext ctx, params Server[] servers)
        {
            var user = new User
            {
                Id = userId,
                Name = name,
                Coins = 100,
                Servers = servers
            };
            ctx.Users.Add(user);
            return user;
        }
    }
}
