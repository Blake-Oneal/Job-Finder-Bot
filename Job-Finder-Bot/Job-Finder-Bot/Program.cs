using Discord;
using Discord.WebSocket;
using Job_Finder_Bot.Configuration;
using Job_Finder_Bot.Data;
using Job_Finder_Bot.Models;
using Job_Finder_Bot.Services;
using Microsoft.EntityFrameworkCore;
using Job_Finder_Bot.Utilities;

// Read the bot token from environment variable
var token = Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN");

if (string.IsNullOrEmpty(token))
{
    Console.WriteLine("Error: DISCORD_BOT_TOKEN environment variable is not set!");
    Console.WriteLine("Please set it using: $env:DISCORD_BOT_TOKEN=\"your_bot_token_here\"");
    return;
}

// Configure the Discord client
var config = new DiscordSocketConfig
{
    GatewayIntents = GatewayIntents.Guilds | 
                     GatewayIntents.GuildMessages
};

var client = new DiscordSocketClient(config);

// Initialize the job monitoring service
var jobMonitor = new JobMonitoringService(minimumScoreThreshold: JobSearchConstants.MinimumScoreThreshold);
var adzunaService = new AdzunaService();

// Initialize database
await InitializeDatabaseAsync();

// Log when the bot is ready
client.Log += Log;
client.Ready += Ready;

// Log in and start the bot
await client.LoginAsync(TokenType.Bot, token);
await client.StartAsync();

// Block the program until it is closed
await Task.Delay(Timeout.Infinite);

Task Log(LogMessage msg)
{
    Console.WriteLine(msg.ToString());
    return Task.CompletedTask;
}

async Task Ready()
{
    Console.WriteLine($"{client.CurrentUser} is connected and ready!");

    // Find the channel named "job-finder-bot"
    var channel = client.Guilds
        .SelectMany(g => g.TextChannels)
        .FirstOrDefault(c => c.Name == "job-finder-bot");

    if (channel != null)
    {
        Console.WriteLine($"Connected to #{channel.Name}");

        // Start the periodic job checking on a background task (don't block Ready)
        _ = Task.Run(async () =>
        {
            try
            {
                await StartJobCheckingLoop(channel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in background job loop: {ex.Message}");
            }
        });
    }
    else
    {
        Console.WriteLine("Warning: Could not find channel 'job-finder-bot'");
    }
}

async Task StartJobCheckingLoop(IMessageChannel channel)
{
    var checkInterval = TimeSpan.FromMinutes(JobSearchConstants.JobCheckIntervalMinutes);

    while (true)
    {
        try
        {
            Console.WriteLine($"[{DateTime.Now}] Checking for new jobs...");

            var newJobs = await FetchJobsFromSource();

            if (newJobs.Any())
            {
                Console.WriteLine($"Processing {newJobs.Count} jobs from API...");
                var jobsToNotify = await jobMonitor.ProcessNewJobsAsync(newJobs);

                if (jobsToNotify.Any())
                {
                    Console.WriteLine($"Sending {jobsToNotify.Count} job notifications to Discord...");
                    foreach (var job in jobsToNotify)
                    {
                        var embed = new EmbedBuilder()
                            .WithTitle($"New Job Match! (Score: {job.Score})")
                            .WithDescription(job.JobTitle)
                            .AddField("Company", job.Company, inline: true)
                            .AddField("Location", job.Location ?? "Not specified", inline: true)
                            .AddField("Posted", job.PostedDate.ToString("MMM dd, yyyy"), inline: true)
                            .WithUrl(job.SourceUrl)
                            .WithColor(Color.Green)
                            .WithCurrentTimestamp()
                            .Build();

                        await channel.SendMessageAsync(embed: embed);
                        Console.WriteLine($"Notified: {job.JobTitle} at {job.Company}");
                    }
                }
                else
                {
                    Console.WriteLine("No jobs met the notification criteria");
                }

                Console.WriteLine($"Summary: {newJobs.Count} fetched, {jobsToNotify.Count} notified");
            }
            else
            {
                Console.WriteLine("No new jobs found");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in job checking loop: {ex.Message}");
        }
        finally
        {
            // Always wait before next check, even if an error occurred
            Console.WriteLine($"Waiting {checkInterval.TotalMinutes} minutes before next check...");
            await Task.Delay(checkInterval);
        }
    }
}

async Task<List<JobPosting>> FetchJobsFromSource()
{
    Console.WriteLine("[Job Fetcher] Starting job search...");
    var allJobs = new List<JobPosting>();
    var uniqueUrls = new HashSet<string>();

    try
    {
        using var db = new JobFinderDbContext();
        var existingJobs = await db.JobPostings.ToDictionaryAsync(j => j.UniqueJobId);

        foreach (var query in JobSearchConstants.SearchQueries)
        {
            int jobsBeforeQuery = allJobs.Count;

            // Search 1: Remote jobs (nationwide) - always get up to target count
            Console.WriteLine($"[Job Fetcher] Searching remote jobs for '{query}'...");
            var remoteResults = await FetchJobsWithPagination(
                query,
                location: null,
                remoteOnly: true,
                allJobs,
                uniqueUrls,
                db,
                JobSearchConstants.TargetJobCount, // Get up to target count remote jobs
                JobSearchConstants.MaxPages,
                existingJobs
            );
            allJobs = remoteResults.AllJobs;
            uniqueUrls = remoteResults.UniqueUrls;

            // Search 2: Local jobs near target location - always get up to target count
            Console.WriteLine($"[Job Fetcher] Searching local jobs near {JobSearchConstants.LocalZipCode} for '{query}'...");
            var localResults = await FetchJobsWithPagination(
                query,
                location: JobSearchConstants.LocalZipCode,
                remoteOnly: false,
                allJobs,
                uniqueUrls,
                db,
                JobSearchConstants.TargetJobCount, // Get up to target count local jobs
                JobSearchConstants.MaxPages,
                existingJobs
            );
            allJobs = localResults.AllJobs;
            uniqueUrls = localResults.UniqueUrls;

            int jobsFromThisQuery = allJobs.Count - jobsBeforeQuery;
            Console.WriteLine($"[Job Fetcher] Finished '{query}': {jobsFromThisQuery} new recent jobs collected");
        }

        Console.WriteLine($"[Job Fetcher] Total jobs fetched: {allJobs.Count}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Job Fetcher] Error: {ex.Message}");
        Console.WriteLine($"[Job Fetcher] Stack trace: {ex.StackTrace}");
    }

    return allJobs;
}

async Task<(List<JobPosting> AllJobs, HashSet<string> UniqueUrls)> FetchJobsWithPagination(
    string query,
    string? location,
    bool remoteOnly,
    List<JobPosting> allJobs,
    HashSet<string> uniqueUrls,
    JobFinderDbContext db,
    int targetJobCount,
    int maxPages,
    Dictionary<string, JobPosting> existingJobs)
{
    int page = 1;
    int jobsFromThisQuery = 0; // Track only non-filtered jobs from this query

    while (jobsFromThisQuery < targetJobCount && page <= maxPages)
    {
        Console.WriteLine($"[Job Fetcher] Fetching page {page} for '{query}' {(remoteOnly ? "(remote)" : $"(near {location})")}");

        var pageJobs = await adzunaService.GetJobPostingsAsyncBySearch(
            country: JobSearchConstants.Country,
            searchTerm: query,
            existingJobs: existingJobs,
            location: location,
            remoteOnly: remoteOnly,
            resultsPerPage: JobSearchConstants.ResultsPerPage,
            page: page
        );

        // If the API returns a "No results found" job, we can stop pagination early
        if (pageJobs.Any() && pageJobs[0].JobTitle == "No results found")
        {
            Console.WriteLine($"[Job Fetcher] No more results, stopping pagination");
            break;
        }

        // Filter for unique, new, and recent jobs
        int addedFromPage = 0;
        foreach (var job in pageJobs)
        {
            // Normalize the job URL to get unique identifier
            var uniqueJobId = JobUrlHelper.NormalizeJobUrl(job.SourceUrl);

            // Skip duplicates within this batch (using normalized ID)
            if (uniqueUrls.Contains(uniqueJobId))
            {
                continue;
            }

            // Check if job is already in database (using normalized ID)
            if (existingJobs.ContainsKey(uniqueJobId))
            {
                continue;
            }

            // Check if job was posted recently
            var daysSincePosted = (DateTime.UtcNow - job.PostedDate).TotalDays;
            if (daysSincePosted > JobSearchConstants.MaxJobAgeDays)
            {
                continue;
            }

            // This is a new, recent job!
            job.UniqueJobId = uniqueJobId; // Set the normalized ID
            allJobs.Add(job);
            uniqueUrls.Add(uniqueJobId); // Track by normalized ID
            addedFromPage++;
            jobsFromThisQuery++;

            // Check if we've reached the target for this query
            if (jobsFromThisQuery >= targetJobCount)
            {
                break;
            }
        }

        Console.WriteLine($"[Job Fetcher] Page {page}: Found {addedFromPage} new recent jobs (query total: {jobsFromThisQuery}/{targetJobCount})");

        page++;
    }

    return (allJobs, uniqueUrls);
}

async Task InitializeDatabaseAsync()
{
    using var db = new JobFinderDbContext();
    await db.Database.EnsureCreatedAsync();
    Console.WriteLine("Database initialized successfully");
}