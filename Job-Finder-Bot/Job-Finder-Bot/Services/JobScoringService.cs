using Job_Finder_Bot.Models;

namespace Job_Finder_Bot.Services;

public class JobScoringService
{
    private readonly Dictionary<string, int> _keywordScores = new()
    {
        // --- Core backend / full stack tech (high value) ---
        { "c#", 20 },
        { ".net", 20 },
        { "asp.net", 18 },
        { ".net core", 22 },
        { "entity framework", 15 },

        // --- Frontend ---
        { "angular", 18 },
        { "typescript", 15 },
        { "javascript", 12 },

        // --- Data / infra ---
        { "sql", 12 },
        { "azure", 14 },
        { "aws", 14 },
        { "kubernetes", 16 },
        { "terraform", 16 },
        { "openshift", 14 },

        // --- DevOps / tooling ---
        { "microservices", 10 },

        // --- Work arrangement ---
        { "remote", 25 },
        { "hybrid", 12 },
        { "on-site", -12 },
        { "on site", -12 },

        // --- Strong negative filters ---
        { "must reside", -25 },
        { "must be located", -20 },
        { "in office", -10 }
    };

    private static readonly Dictionary<string, int> _titleScores = new()
    {
        // --- Strong seniority filters ---
        { "vp", -120 },
        { "vice president", -120 },
        { "director", -100 },
        { "manager", -80 },
        { "head of", -80 },

        // --- Architect / leadership ---
        { "principal", -45 },
        { "staff", -45 },
        { "architect", -40 },
        { "technical lead", -35 },
        { "tech lead", -35 },
        { "lead engineer", -35 },
        { "lead developer", -30 },
        { "team lead", -30 },

        // --- Senior ---
        { "senior", -20 },
        { "sr.", -20 },

        // --- Mid-level preference ---
        { "engineer ii", 25 },
        { "developer ii", 25 },
        { "mid-level", 20 },
        { "mid level", 20 },
        { "level 2", 22 },

        // --- Entry level signals ---
        { "entry level", -10 },   // slightly negative (optional filter)
        { "junior", 10 },
        {"intern", -100 },

        // --- Neutral-positive filler terms ---
        { "experienced", 8 }
    };

    public int CalculateScore(JobPosting job)
    {
        int score = 0;

        var title = (job.JobTitle ?? "").ToLowerInvariant();
        var description = (job.Description ?? "").ToLowerInvariant();

        foreach (var keyword in _titleScores)
        {
            if (title.Contains(keyword.Key))
            {
                score += keyword.Value;
            }
        }

        foreach (var keyword in _keywordScores)
        {
            // Title matches are more important
            if (title.Contains(keyword.Key))
            {
                score += keyword.Value * 2;
                continue; // Skip description check if title matches
            }

            // Description matches are still valuable
            if (description.Contains(keyword.Key))
            {
                score += keyword.Value;
            }
        }

        return score;
    }
}
