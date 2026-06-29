using System.Text.Json.Serialization;

namespace Job_Finder_Bot.Models
{
    public class AdzunaResponse
    {
        [JsonPropertyName("results")]
        public List<AdzunaJob> Results { get; set; }
    }
}
