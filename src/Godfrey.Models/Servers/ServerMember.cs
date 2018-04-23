using Godfrey.Models.Common;
using Godfrey.Models.Users;

namespace Godfrey.Models.Servers
{
    public class ServerMember : BaseKeyEntity<int>
    {
        public ulong UserId { get; set; }
        public virtual User User { get; set; }

        public ulong ServerId { get; set; }
        public virtual Server Server { get; set; }
    }
}
