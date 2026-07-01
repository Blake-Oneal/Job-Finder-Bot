using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Job_Finder_Bot.Models;

namespace Job_Finder_Bot.Services
{
    public class DescriptionEnrichmentService
    {
        private readonly HttpClient _httpClient;

        public DescriptionEnrichmentService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string?> TryEnrichDescriptionAsync(JobPosting job)
        {
            var url = NormalizeAdzunaUrlForScraping(job.SourceUrl);
            {
                if (string.IsNullOrWhiteSpace(url))
                {
                    return null;
                }

                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, url);

                    request.Headers.UserAgent.ParseAdd(
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
                        "(KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36"
                    );

                    request.Headers.Accept.ParseAdd(
                    "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8"
                    );

                    using var response = await _httpClient.SendAsync(request);

                    Console.WriteLine($"[Enrichment] URL: {url}");
                    Console.WriteLine($"[Enrichment] Status: {(int)response.StatusCode} {response.ReasonPhrase}");
                    Console.WriteLine($"[Enrichment] Final URL: {response.RequestMessage?.RequestUri}");


                    if (!response.IsSuccessStatusCode)
                    {
                        return null;
                    }

                    job.JobUrl = response.RequestMessage!.RequestUri!.ToString();

                    var html = await response.Content.ReadAsStringAsync();

                    var extracted = ExtractLikelyJobDescription(html);

                    if (string.IsNullOrWhiteSpace(extracted))
                    {
                        return null;
                    }

                    // Only use enriched text if it is actually better than Adzuna's teaser.
                    if (extracted.Length <= job.Description!.Length + 250)
                    {
                        return null;
                    }

                    return extracted;
                }
                catch
                {
                    return null;
                }
            }
        }

        private static string ExtractLikelyJobDescription(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var nodesToRemove = doc.DocumentNode.SelectNodes(
                "//script|//style|//nav|//footer|//header|//noscript|//svg"
            );

            if (nodesToRemove != null)
            {
                foreach (var node in nodesToRemove)
                    node.Remove();
            }

            // Try common job-description containers first.
            var candidates = doc.DocumentNode.SelectNodes(
                "//*[contains(translate(@class, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'job-description') " +
                "or contains(translate(@class, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'description') " +
                "or contains(translate(@id, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'job-description') " +
                "or contains(translate(@id, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'description') " +
                "or contains(translate(@class, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'posting') " +
                "or contains(translate(@class, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'content')]"
            );

            var bestText = "";

            if (candidates != null)
            {
                foreach (var candidate in candidates)
                {
                    var text = CleanText(candidate.InnerText);

                    if (LooksLikeJobDescription(text) && text.Length > bestText.Length)
                        bestText = text;
                }
            }

            // Fallback: use full body text if no obvious container was found.
            if (string.IsNullOrWhiteSpace(bestText))
            {
                var body = doc.DocumentNode.SelectSingleNode("//body");
                if (body != null)
                {
                    var text = CleanText(body.InnerText);

                    if (LooksLikeJobDescription(text))
                    {
                        bestText = text;
                    }
                }
            }

            return bestText;
        }

        private static bool LooksLikeJobDescription(string text)
        {
            if (text.Length < 800)
                return false;

            var lower = text.ToLowerInvariant();

            var signals = new[]
            {
            "responsibilities",
            "requirements",
            "qualifications",
            "experience",
            "about the role",
            "what you'll do",
            "what you will do",
            "benefits",
            "salary",
            "apply"
        };

            return signals.Count(signal => lower.Contains(signal)) >= 2;
        }

        private static string CleanText(string text)
        {
            text = HtmlEntity.DeEntitize(text);
            text = Regex.Replace(text, @"\s+", " ");
            return text.Trim();
        }

        private static string NormalizeAdzunaUrlForScraping(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return url;

            return url.Replace("/land/ad/", "/details/");
        }


    }
}
