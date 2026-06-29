using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Job_Finder_Bot.Models
{
    public class AdzunaCompany
    {
        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; }
    }
}
