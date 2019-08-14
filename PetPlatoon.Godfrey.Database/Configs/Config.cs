using PetPlatoon.Godfrey.Database.Common;
using PetPlatoon.Godfrey.Database.Servers;

namespace PetPlatoon.Godfrey.Database.Configs
{
    public class Config : BaseKeyEntity<int>
    {
        public ulong ServerId { get; set; }
        public virtual Server Server { get; set; }

        public string Key { get; set; }
        public string Value { get; set; }
    }
}
