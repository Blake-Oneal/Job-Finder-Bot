using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Job_Finder_Bot.Configuration;
using Job_Finder_Bot.Models;
using System.Text.Json.Serialization;
using Job_Finder_Bot.Utilities;

namespace Job_Finder_Bot.Services
{
    public class AdzunaService
    {
        private readonly string _appId;
        private readonly string _appKey;
        public AdzunaService()
        {
            _appId = Environment.GetEnvironmentVariable("ADZUNA_API_ID") ?? throw new InvalidOperationException("ADZUNA_API_ID environment variable is not set.");
            _appKey = Environment.GetEnvironmentVariable("ADZUNA_API_KEY") ?? throw new InvalidOperationException("ADZUNA_API_KEY environment variable is not set.");
        }

        public async Task<List<JobPosting>> GetJobPostingsAsyncBySearch(string country, string searchTerm, IReadOnlyDictionary<string, JobPosting> existingJobs, string? location = null, bool remoteOnly = true, string sortBy = "date", int resultsPerPage = 100, int page = 1)
        {
            string niceToHaves = Uri.EscapeDataString(string.Join(" ", JobSearchConstants.NiceToHave));
            var encodedSearchTerm = Uri.EscapeDataString(searchTerm);
            var url = $"https://api.adzuna.com/v1/api/jobs/{country}/search/{page}?app_id={_appId}&app_key={_appKey}&results_per_page={resultsPerPage}&what={encodedSearchTerm}&what_or={niceToHaves}&what_and={encodedSearchTerm}&sort_by={sortBy}&full_time={1}&what_exclude={"military"}";

            // Add location filter if provided
            if (!string.IsNullOrWhiteSpace(location))
            {
                var encodedLocation = Uri.EscapeDataString(location);
                url += $"&where={encodedLocation}&distance={JobSearchConstants.MaxKMAway}";
            }

            Console.WriteLine($"[Adzuna API] Fetching: {searchTerm} in {location ?? "all locations"}");

            using var httpClient = new HttpClient();
            using var httpResponse = await httpClient.GetAsync(url);

            if (!httpResponse.IsSuccessStatusCode)
            {
                var errorContent = await httpResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"[Adzuna API] Error: {httpResponse.StatusCode} - {errorContent}");
                return new List<JobPosting>();
            }

            var responseBytes = await httpResponse.Content.ReadAsByteArrayAsync();
            var response = Encoding.UTF8.GetString(responseBytes);

            var adzunaResponse = JsonSerializer.Deserialize<AdzunaResponse>(response, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var jobPostings = new List<JobPosting>();
            int skippedIrrelevant = 0;
            int skippedAlreadySeen = 0;

            if (adzunaResponse?.Results.Count != 0)
            {
                Console.WriteLine($"[Adzuna API] Found {adzunaResponse.Results.Count} jobs from API");

                foreach (var result in adzunaResponse.Results)
                {
                    // Check if we've seen this job before (by UniqueJobId) in the database
                    var normalizedJobId = JobUrlHelper.NormalizeJobUrl(result.RedirectUrl);
                    if (existingJobs.TryGetValue(normalizedJobId, out var existingJob))
                    {
                        // Already notified or marked irrelevant, skip further processing
                        if (existingJob.HasNotified)
                        {
                            Console.WriteLine($"[JobMonitoring] Job already notified: {result.Title}");
                            skippedAlreadySeen++;
                            continue;
                        }

                        if (!existingJob.IsRelevant)
                        {
                            Console.WriteLine($"[JobMonitoring] Job previously marked irrelevant: {result.Title}");
                            skippedAlreadySeen++;
                            continue;
                        }
                    }
                    // Skip jobs with missing required fields
                    if (string.IsNullOrWhiteSpace(result.Title) ||
                        string.IsNullOrWhiteSpace(result.RedirectUrl))
                    {
                        Console.WriteLine($"[Adzuna API] Skipping job with missing Title or URL");
                        continue;
                    }



                    // Filter out jobs requiring security clearances
                    if (RequiresSecurityClearance(result.Title, result.Description))
                    {
                        Console.WriteLine($"[Adzuna API] Filtering out clearance job: {result.Title}");
                        skippedIrrelevant++;
                        continue;
                    }

                    // Filter out jobs with missing location
                    if (result.Location?.DisplayName == null)
                    {
                        Console.WriteLine($"[Adzuna API] Filtering out job with no location: {result.Title}");
                        skippedIrrelevant++;
                        continue;
                    }

                    // If we are looking for remote jobs only, filter out non-remote jobs
                    else if (remoteOnly && !IsJobRemote(result.Title, result.Description, result.Location?.DisplayName))
                    {
                        Console.WriteLine($"[Adzuna API] Filtering out non-remote job: {result.Title}");
                        skippedIrrelevant++;
                        continue;
                    }

                    // Otherwise if we are looking for local jobs only, filter out non-local jobs
                    else if (!remoteOnly && !IsLocalJob(result.Location?.DisplayName))
                    {
                        Console.WriteLine($"[Adzuna API] Filtering out non-local job: {result.Title}");
                        skippedIrrelevant++;
                        continue;
                    }

                    jobPostings.Add(new JobPosting
                    {
                        JobTitle = result.Title,
                        Company = result.Company?.DisplayName ?? "Unknown",
                        Location = result.Location?.DisplayName,
                        Description = result.Description,
                        SourceUrl = result.RedirectUrl,
                        Salary = result.SalaryMin.HasValue && result.SalaryMax.HasValue
                            ? $"${result.SalaryMin:N0} - ${result.SalaryMax:N0}"
                            : null,
                        PostedDate = result.Created != default ? result.Created : DateTime.UtcNow,
                        DiscoveredDate = DateTime.UtcNow
                    });
                }
            }
            // If no results were found, return a placeholder job posting
            else
            {
                Console.WriteLine($"[Adzuna API] No results in response");
                jobPostings.Add(new JobPosting
                {
                    JobTitle = "No results found",
                    SourceUrl = "https://www.adzuna.com/",
                    Company = "Adzuna API",
                });
                return jobPostings;
            }

            if (skippedIrrelevant > 0)
            {
                Console.WriteLine($"[Adzuna API] Filtered out {skippedIrrelevant} irrelevant jobs");
            }
            Console.WriteLine($"[Adzuna API] Returning {jobPostings.Count} relevant jobs");
            return jobPostings;
        }

        private bool RequiresSecurityClearance(string jobTitle, string? description)
        {
            var titleLower = jobTitle.ToLower();
            var descriptionLower = description?.ToLower() ?? "";

            // Check if any clearance keyword appears in title or description
            return JobSearchConstants.ClearanceKeywords.Any(keyword =>
                titleLower.Contains(keyword) || descriptionLower.Contains(keyword));
        }

        private bool IsJobRemote(string jobTitle, string? description, string? location)
        {

            var text = $"{jobTitle} {description ?? ""} {location ?? ""}".ToLowerInvariant();

            return JobSearchConstants.RemoteKeywords.Any(keyword => text.Contains(keyword));
        }

        private bool IsLocalJob(string? location)
        {
            if (string.IsNullOrWhiteSpace(location))
            {
                return false;
            }

            var locationLower = location.ToLower();
            return JobSearchConstants.LocalJobLocations.Any(keyword => locationLower.Contains(keyword));
        }
    }
}

