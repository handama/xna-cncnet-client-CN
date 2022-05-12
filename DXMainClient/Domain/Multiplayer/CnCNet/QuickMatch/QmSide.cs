using Newtonsoft.Json;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch
{
    public class QmSide
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("ladder_id")]
        public int LadderId { get; set; }

        [JsonProperty("local_id")]
        public int LocalId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
