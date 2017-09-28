using Newtonsoft.Json;

namespace Godfrey
{
    public class ButlerConfig
    {
        [JsonProperty("token")]
        public string Token { get; private set; } = string.Empty;

        [JsonProperty("command_prefix")]
        public string CommandPrefix { get; private set; } = "?";

        [JsonProperty("shards")]
        public int ShardCount { get; private set; } = 1;

        [JsonProperty("user")]
        public bool UseUserToken { get; private set; }

        [JsonProperty("connectionstring")]
        public string ConnectionString { get; private set; }
    }
}
