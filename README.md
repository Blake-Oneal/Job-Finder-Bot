# Job Finder Bot

An automated Discord bot that aggregates software engineering job
postings, filters and scores them based on customizable criteria, stores
results in a local database, and delivers the highest-ranked
opportunities directly to a Discord server.

This project was built to automate my own software engineering job
search by reducing time spent manually browsing job boards while
surfacing the positions that best match my experience.

------------------------------------------------------------------------

## Features

-   Searches for jobs using the Adzuna Job Search API
-   Supports both remote software engineering positions and local jobs
    within a configurable radius
-   Aggregates results across multiple search queries
-   Removes duplicate job postings
-   Scores each job using a configurable weighted scoring algorithm
-   Filters out security clearance positions, irrelevant job titles, and
    low-scoring opportunities
-   Stores job history using Entity Framework Core with SQLite
-   Prevents duplicate notifications
-   Sends the highest-ranked jobs directly to a Discord channel
-   Runs continuously on a configurable polling interval

------------------------------------------------------------------------

## Tech Stack

### Languages

-   C#
-   SQL

### Frameworks & Libraries

-   .NET
-   Entity Framework Core
-   Discord.Net

### Database

-   SQLite

### APIs

-   Adzuna Job Search API
-   Discord API

------------------------------------------------------------------------

## How It Works

``` text
Search Queries
      │
      ▼
Retrieve Remote & Local Jobs
      │
      ▼
Remove Duplicates
      │
      ▼
Filter Irrelevant Jobs
      │
      ▼
Score Jobs
      │
      ▼
Persist to Database
      │
      ▼
Select Top N Results
      │
      ▼
Send Discord Notification
```

Each discovered job receives a configurable score based on factors such
as programming languages, frameworks, cloud technologies, seniority, job
title, required experience, and remote availability.

Jobs that do not meet the minimum score threshold are stored but
excluded from future notifications.

------------------------------------------------------------------------

## Configuration

The application uses Windows User Environment Variables to securely
store API credentials.

Configure the following variables before running the application:

  Variable         Description
  ---------------- ----------------------------
  DISCORD_TOKEN    Discord Bot Token
  ADZUNA_APP_ID    Adzuna API Application ID
  ADZUNA_APP_KEY   Adzuna API Application Key

Example (PowerShell):

``` powershell
[System.Environment]::SetEnvironmentVariable(
    "DISCORD_TOKEN",
    "your-discord-token",
    "User"
)
```

Restart Visual Studio or your terminal after updating environment
variables.

------------------------------------------------------------------------

## Running the Project

``` bash
git clone https://github.com/<your-username>/Job-Finder-Bot.git
cd Job-Finder-Bot
dotnet restore
dotnet run
```

------------------------------------------------------------------------

## Current Scoring Strategy

The scoring algorithm rewards technologies and attributes that match my
preferred software engineering roles.

**Positive weighting** - C# - .NET - ASP.NET - Angular - TypeScript -
SQL - Azure - Kubernetes - REST APIs - Entity Framework

**Negative weighting** - Senior leadership positions - Security
clearance requirements - Management roles - Non-software positions

------------------------------------------------------------------------

## Future Improvements

-   Support Greenhouse, Lever, and Ashby job boards
-   Configurable scoring via JSON/YAML
-   Web dashboard
-   Email notifications
-   Resume keyword matching
-   Docker deployment
-   Unit and integration tests
-   AI-assisted job relevance scoring

------------------------------------------------------------------------

## Why I Built This

After being impacted by a company-wide restructuring, I wanted a way
to automate the repetitive parts of searching for software engineering
positions while still maintaining control over what constituted a good
opportunity.

Rather than manually reviewing hundreds of postings every day, this
application continuously searches, filters, ranks, and notifies me of
the jobs most relevant to my background.

This project also provided an opportunity to continue sharpening my
.NET, Entity Framework, REST API, and software architecture skills while
solving a real-world problem.

------------------------------------------------------------------------

## License

This project is intended for educational and portfolio purposes.
