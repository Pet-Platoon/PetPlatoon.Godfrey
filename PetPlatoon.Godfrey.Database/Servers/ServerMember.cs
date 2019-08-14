using PetPlatoon.Godfrey.Database.Common;
using PetPlatoon.Godfrey.Database.Users;

namespace PetPlatoon.Godfrey.Database.Servers
{
    public class ServerMember : BaseKeyEntity<int>
    {
        public ulong UserId { get; set; }
        public virtual User User { get; set; }

        public ulong ServerId { get; set; }
        public virtual Server Server { get; set; }
    }
}
