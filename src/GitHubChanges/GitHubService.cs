using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Octokit;
using Semver;

namespace GitHubChanges;

public interface IGitHubService
{
    Task<List<Organization>> GetOrganizationsForUser();
    Task<List<Repository>> GetRepositories(string orgName);
    Task<List<Tag>> GetTags(long repositoryId);
    Task<List<Commit>> GetCommits(long repositoryId, string originalTag, string newTag);
}

public class GitHubService : IGitHubService
{
    private readonly string[] _tagPrefixes;

    private readonly GitHubClient _github;

    private List<Organization> _usersOrganizations;

    private readonly Dictionary<string, List<Repository>> _repositoriesByOrg = new();

    private readonly Dictionary<long, List<Tag>> _tagsByRepo = new();

    public GitHubService(string pat, string[] tagPrefixes)
    {
        _github = new GitHubClient(new ProductHeaderValue("GitHubChanges"));
        _github.Credentials = new Credentials(pat);
        _tagPrefixes = tagPrefixes;
    }

    public async Task<List<Organization>> GetOrganizationsForUser()
    {
        return _usersOrganizations ??= (await _github.Organization.GetAllForCurrent()).OrderBy(x => x.Login).ToList();
    }

    public async Task<List<Repository>> GetRepositories(string orgName)
    {
        if (_repositoriesByOrg.TryGetValue(orgName, out var repositories)) return repositories;

        repositories = (await _github.Repository.GetAllForOrg(orgName)).OrderBy(x => x.Name).ToList();
        _repositoriesByOrg.Add(orgName, repositories);
        return repositories;
    }

    public async Task<List<Tag>> GetTags(long repositoryId)
    {
        if (_tagsByRepo.TryGetValue(repositoryId, out var tags)) return tags;

        tags = (await _github.Repository.GetAllTags(repositoryId))
            .Select(x => new Tag
            {
                SemVersion = GetSemVer(x.Name),
                RepoTag = x
            })
            .OrderByDescending(x => x.SemVersion)
            .ToList();

        _tagsByRepo.Add(repositoryId, tags);

        return tags;
    }

    public async Task<List<Commit>> GetCommits(long repositoryId, string originalTag, string newTag)
    {
        var githubCommits = await _github.Repository.Commit.Compare(repositoryId, originalTag, newTag);

        return githubCommits.Commits.Select(x => new Commit(x)).ToList();
    }

    private SemVersion GetSemVer(string version)
    {
        try
        {
            return SemVersion.Parse(_tagPrefixes.Aggregate(version, (x, prefix) => x.TrimStart(prefix.ToCharArray())));
        }
        catch (Exception)
        {
            return null;
        }
    }
}

public class Tag
{
    public SemVersion SemVersion { get; set; }
    public string DisplayName => RepoTag.Name;
    public RepositoryTag RepoTag { get; set; }
}

public class Commit
{
    public Commit(GitHubCommit commit)
    {
        var innerCommit = commit?.Commit;
        Date = innerCommit?.Committer.Date.ToLocalTime();
        Message = innerCommit?.Message;

        Author = innerCommit?.Committer?.Name == innerCommit?.Author?.Name 
            ? innerCommit?.Committer?.Name 
            : $"{innerCommit.Committer.Name} committed on behalf of {innerCommit.Author?.Name}";
    }

    public DateTimeOffset? Date { get; set; }
    public string Author { get; set; }
    public string Message { get; set; }
}