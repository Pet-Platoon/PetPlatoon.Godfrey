using Godfrey.Models.Common;

namespace Godfrey.Models.Configs
{
    public class Config : BaseKeyEntity<int>
    {
        public ulong GuildId { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
