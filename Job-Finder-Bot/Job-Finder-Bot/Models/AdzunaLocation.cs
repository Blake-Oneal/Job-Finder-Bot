using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Job_Finder_Bot.Models
{
    public class AdzunaLocation
    {
        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; }
    }
}
