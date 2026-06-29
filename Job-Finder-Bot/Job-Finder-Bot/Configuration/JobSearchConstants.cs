namespace Job_Finder_Bot.Configuration;

public static class JobSearchConstants
{
    // ===== SEARCH CONFIGURATION =====

    /// <summary>
    /// Job search queries to look for
    /// These will be passed into the what_and parameter of the Adzuna API
    /// </summary>
    public static readonly string[] SearchQueries = 
    {
        "software developer",
        "software engineer",
        "full stack developer",
        "full-stack developer",
    };

    /// <summary>
    /// Nice to haves for the job search
    /// These will be passed into the what_or parameter of the Adzuna API
    /// </summary>
    public static readonly string[] NiceToHave =
    {
        "c#", "typescript", ".net", "sql", "angular", "openshift", "fullstack", "full-stack", "java", "node", "dotnet", "remote"
    };

    /// <summary>
    /// Your zip code for local job searches
    /// </summary>
    public const string LocalZipCode = "07840";

    /// <summary>
    /// Max KM away from your zip code for local job searches, if applicable
    /// </summary>
    public const int MaxKMAway = 50;

    /// <summary>
    /// Country code for job searches
    /// </summary>
    public const string Country = "us";

    /// <summary>
    /// Target number of jobs to fetch per search query (after filtering)
    /// </summary>
    public const int TargetJobCount = 50;

    /// <summary>
    /// Maximum number of top-scoring jobs to notify about across all queries
    /// </summary>
    public const int MaxJobsToNotify = 25;

    /// <summary>
    /// Maximum pages to fetch per query (safety limit)
    /// </summary>
    public const int MaxPages = 20;

    /// <summary>
    /// Maximum age of jobs to consider (in days)
    /// </summary>
    public const int MaxJobAgeDays = 30;

    /// <summary>
    /// Number of results to fetch per API page
    /// </summary>
    public const int ResultsPerPage = 100;

    // ===== SECURITY CLEARANCE KEYWORDS (Jobs to exclude) =====

    /// <summary>
    /// Keywords indicating a job requires security clearance
    /// Jobs containing these will be filtered out
    /// </summary>
    public static readonly string[] ClearanceKeywords = 
    {
        "security clearance",
        "clearance required",
        "secret clearance",
        "top secret",
        "ts/sci",
        "ts sci",
        "polygraph",
        "dod clearance",
        "government clearance",
        "active clearance",
        "must have clearance",
        "clearable",
        "ability to obtain clearance",
        "eligible for clearance",
        "interim clearance",
        "public trust"
    };

    /// <summary>
    /// Keywords to filter remote job postings on
    /// </summary>
    public static readonly string[] RemoteKeywords =
    {
        "remote",
        "work from home",
        "telecommute",
        "telework",
        "distributed team"
    };


    /// <summary>
    /// Acceptable job locations for local jobs (if not remote)
    /// </summary>
    public static readonly string[] LocalJobLocations =
    {
        "nj",
        "new jersey"
    };


    // ===== SCORING CONFIGURATION =====

    /// <summary>
    /// Minimum score threshold for job notifications
    /// Jobs with scores below this won't trigger Discord notifications
    /// </summary>
    public const int MinimumScoreThreshold = 30;


    // ===== TIMING CONFIGURATION =====

    /// <summary>
    /// How often to check for new jobs (in minutes)
    /// </summary>
    public const int JobCheckIntervalMinutes = 120;
}
