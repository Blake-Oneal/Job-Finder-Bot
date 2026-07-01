using Job_Finder_Bot.Models;

namespace Job_Finder_Bot.Services;

public class JobScoringService
{
    private readonly Dictionary<string, int> _keywordScores = new()
    {
        // --- Core backend / full stack tech (high value) ---
        { "c#", 25 },
        { "csharp", 25 },
        { ".net", 25 },
        { ".net core", 28 },
        { "asp.net", 22 },
        { "asp.net core", 25 },
        { "entity framework", 18 },
        { "ef core", 18 },
        { "rest api", 18 },
        { "rest apis", 18 },
        { "web api", 16 },
        { "api development", 15 },
        { "backend", 18 },
        { "back-end", 18 },
        { "full stack", 18 },
        { "full-stack", 18 },

        // --- Frontend ---
        { "angular", 18 },
        { "typescript", 15 },
        { "javascript", 10 },
        { "html", 5 },
        { "css", 5 },

        // --- Data / infra ---
        { "sql", 15 },
        { "sql server", 18 },
        { "relational database", 10 },
        { "database", 8 },
        { "azure", 16 },
        { "azure devops", 18 },
        { "aws", 10 },
        { "kubernetes", 16 },
        { "terraform", 14 },
        { "openshift", 16 },
        { "docker", 10 },
        { "ci/cd", 12 },
        { "jenkins", 10 },

        // --- Enterprise / architecture signals ---
        { "microservices", 12 },
        { "distributed systems", 12 },
        { "enterprise", 8 },
        { "agile", 6 },
        { "scrum", 5 },
        { "devops", 8 },
        { "devsecops", 10 },
        { "security", 6 },
        { "cloud", 8 },

        // --- Work arrangement ---
        { "remote", 25 },
        { "hybrid", 12 },
        { "on-site", -12 },
        { "on site", -12 },
        { "onsite", -12 },
        { "relocation", -15 },

        // --- Mobile roles - not my current target ---
        { "ios", -35 },
        { "android", -35 },
        { "swift", -25 },
        { "kotlin", -25 },
        { "react native", -20 },
        { "mobile developer", -35 },
        { "mobile engineer", -35 },

        // --- ML / data science roles - not my current target right now ---
        { "machine learning", -40 },
        { "ml engineer", -45 },
        { "deep learning", -35 },
        { "llm", -35 },
        { "large language model", -35 },
        { "transformer", -30 },
        { "transformers", -30 },
        { "rag", -25 },
        { "nlp", -30 },
        { "natural language processing", -30 },
        { "computer vision", -30 },
        { "pytorch", -25 },
        { "tensorflow", -25 },
        { "data scientist", -45 },
        { "data science", -35 },
        { "data analyst", -35 },

        // --- QA / support / non-dev roles ---
        { "qa engineer", -35 },
        { "quality assurance", -30 },
        { "test engineer", -25 },
        { "manual testing", -30 },
        { "support engineer", -25 },
        { "help desk", -50 },
        { "desktop support", -50 },
        { "business analyst", -35 },
        { "project manager", -60 },
        { "product manager", -60 },
        { "scrum master", -50 },

        // --- Strong negative filters ---
        { "must reside", -30 },
        { "must be located", -25 },
        { "local candidates only", -30 },
        { "in office", -15 },
        { "5 days onsite", -35 },
        { "10+ years", -50 },
        { "12+ years", -60 },
        { "15+ years", -75 },
        { "active security clearance", -40 },
        { "top secret", -50 },
        { "secret clearance", -40 }
    };

    private static readonly Dictionary<string, int> _titleScores = new()
    {
        // --- Ideal titles ---
        { "software engineer", 35 },
        { "software developer", 35 },
        { "application developer", 30 },
        { "applications developer", 30 },
        { "backend engineer", 35 },
        { "back-end engineer", 35 },
        { "backend developer", 35 },
        { "back-end developer", 35 },
        { "full stack developer", 32 },
        { "full-stack developer", 32 },
        { "full stack engineer", 32 },
        { "full-stack engineer", 32 },
        { "systems programmer", 20 },
        { "systems developer", 20 },

        // --- Mid-level preference ---
        { "engineer ii", 30 },
        { "developer ii", 30 },
        { "software engineer ii", 35 },
        { "software developer ii", 35 },
        { "mid-level", 25 },
        { "mid level", 25 },
        { "level 2", 22 },

        // --- Seniority filters ---
        { "vp", -120 },
        { "vice president", -120 },
        { "director", -100 },
        { "dir,", -100 },
        { "manager", -85 },
        { "head of", -85 },

        // --- Architect / leadership ---
        { "principal", -55 },
        { "staff", -55 },
        { "architect", -45 },
        { "technical lead", -40 },
        { "tech lead", -40 },
        { "lead engineer", -40 },
        { "lead developer", -35 },
        { "team lead", -35 },

        // --- Senior: not impossible, but not preferred ---
        { "senior", -20 },
        { "sr.", -20 },
        { "sr ", -20 },

        // --- Entry / early career ---
        { "new grad", -15 },
        { "entry level", -10 },
        { "junior", 5 },
        { "associate software engineer", 15 },
        { "associate developer", 15 },
        { "intern", -100 },
        { "internship", -100 },

        // --- Bad title matches ---
        { "machine learning", -70 },
        { "data scientist", -80 },
        { "data engineer", -35 },
        { "devops engineer", -10 },
        { "site reliability", -25 },
        { "sre", -25 },
        { "mobile", -50 },
        { "ios", -60 },
        { "android", -60 },
        { "qa", -45 },
        { "test engineer", -40 },
        { "business analyst", -50 },
        { "project manager", -80 },
        { "product manager", -80 },

        // --- Neutral-positive filler terms ---
        { "experienced", 8 }
    };

    public int CalculateScore(JobPosting job)
    {
        int score = 0;

        var title = (job.JobTitle ?? "").ToLowerInvariant();
        var description = (job.Description ?? "").ToLowerInvariant();
        var combinedText = $"{title} {description}";

        foreach (var keyword in _titleScores)
        {
            if (title.Contains(keyword.Key))
            {
                score += keyword.Value;
            }
        }

        foreach (var keyword in _keywordScores)
        {
            if (title.Contains(keyword.Key))
            {
                score += keyword.Value * 2;
                continue;
            }

            if (description.Contains(keyword.Key))
            {
                score += keyword.Value;
            }
        }

        // Extra guardrail: if the title itself is clearly outside the target,
        // make sure it cannot sneak through just because the description mentions APIs/cloud/etc.
        if (title.Contains("machine learning") || title.Contains("data scientist") || title.Contains("mobile") || title.Contains("ios") || title.Contains("android") || title.Contains("project manager") || title.Contains("product manager"))
        {
            score -= 50;
        }

        return score;
    }
}