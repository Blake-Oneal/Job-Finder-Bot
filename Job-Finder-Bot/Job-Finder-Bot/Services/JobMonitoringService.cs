using Job_Finder_Bot.Configuration;
using Job_Finder_Bot.Data;
using Job_Finder_Bot.Models;
using Microsoft.EntityFrameworkCore;

namespace Job_Finder_Bot.Services;

public class JobMonitoringService
{
    private readonly JobScoringService _scoringService;
    private readonly int _minimumScoreThreshold;

    public JobMonitoringService(int minimumScoreThreshold = 20)
    {
        _scoringService = new JobScoringService();
        _minimumScoreThreshold = minimumScoreThreshold;
    }

    public async Task<List<JobPosting>> ProcessNewJobsAsync(List<JobPosting> newJobs)
    {
        using var db = new JobFinderDbContext();

        var processedIds = new HashSet<string>();
        var jobsToInsert = new List<JobPosting>();

        var existingRelevantUnnotified = await db.JobPostings
            .Where(j => j.IsRelevant && !j.HasNotified)
            .ToListAsync();

        var existingJobIds = await db.JobPostings
            .Select(j => j.UniqueJobId)
            .ToHashSetAsync();

        var jobsForNotificationPool = new List<JobPosting>();

        // Existing jobs: already in DB, do not AddRange them
        jobsForNotificationPool.AddRange(existingRelevantUnnotified);

        foreach (var job in newJobs)
        {
            if (string.IsNullOrWhiteSpace(job.UniqueJobId))
                continue;

            if (!processedIds.Add(job.UniqueJobId))
                continue;

            // If already in DB, skip inserting it
            if (existingJobIds.Contains(job.UniqueJobId))
                continue;

            if (string.IsNullOrWhiteSpace(job.JobTitle) ||
                string.IsNullOrWhiteSpace(job.Company) ||
                string.IsNullOrWhiteSpace(job.SourceUrl))
                continue;

            if (job.PostedDate == default)
                job.PostedDate = DateTime.UtcNow;

            if (job.DiscoveredDate == default)
                job.DiscoveredDate = DateTime.UtcNow;

            job.Score = _scoringService.CalculateScore(job);
            job.IsRelevant = job.Score >= _minimumScoreThreshold;
            job.HasNotified = false;

            jobsToInsert.Add(job);

            if (job.IsRelevant)
                jobsForNotificationPool.Add(job);
        }

        var topJobs = jobsForNotificationPool
            .Where(j => j.IsRelevant && !j.HasNotified)
            .OrderByDescending(j => j.Score)
            .Take(JobSearchConstants.MaxJobsToNotify)
            .ToList();

        foreach (var job in topJobs)
        {
            job.HasNotified = true;
        }

        if (jobsToInsert.Count > 0)
        {
            db.JobPostings.AddRange(jobsToInsert);
        }

        await db.SaveChangesAsync();

        return topJobs;
    }

    public async Task<int> GetTotalJobsSeenAsync()
    {
        using var db = new JobFinderDbContext();
        return await db.JobPostings.CountAsync();
    }
}
