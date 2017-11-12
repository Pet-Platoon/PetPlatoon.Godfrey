using Godfrey.Models.Common;
using Godfrey.Models.Servers;

namespace Godfrey.Models.Configs
{
    public class Config : BaseKeyEntity<int>
    {
        public ulong ServerId { get; set; }
        public virtual Server Server { get; set; }

        public string Key { get; set; }
        public string Value { get; set; }
    }
}
