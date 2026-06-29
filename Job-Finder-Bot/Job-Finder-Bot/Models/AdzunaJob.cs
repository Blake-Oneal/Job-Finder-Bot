using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Job_Finder_Bot.Models
{
    public class AdzunaJob
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }
        [JsonPropertyName("company")]
        public AdzunaCompany Company { get; set; }
        [JsonPropertyName("location")]
        public AdzunaLocation Location { get; set; }
        [JsonPropertyName("description")]
        public string Description { get; set; }
        [JsonPropertyName("redirect_url")]
        public string RedirectUrl { get; set; }
        [JsonPropertyName("salary_min")]
        public double? SalaryMin { get; set; }
        [JsonPropertyName("salary_max")]
        public double? SalaryMax { get; set; }
        [JsonPropertyName("created")]
        public DateTime Created { get; set; }
    }
}
