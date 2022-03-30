using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GitHubChanges.Settings;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using Octokit;

namespace GitHubChanges;

public class MainViewModel : ObservableRecipient,
    IRecipient<PropertyChangedMessage<Organization>>,
    IRecipient<PropertyChangedMessage<Repository>>,
    IRecipient<PropertyChangedMessage<Tag>>
{
    private readonly IGitHubService _gitHubService;
    private IEnumerable<Organization> _organizations;
    private IEnumerable<Repository> _repositories;
    private IEnumerable<Tag> _tags;
    private IEnumerable<Commit> _commits;
    private IEnumerable<string> _tickets;

    private Organization _selectedOrganization;
    private Repository _selectedRepository;

    private readonly List<IRelayCommand> _commands;
    private Tag _originalTag;
    private Tag _newTag;

    private static readonly Version Version = Assembly.GetExecutingAssembly().GetName().Version;
    private readonly UserSettings _settings;
    private bool _isLoading;

    public MainViewModel(IGitHubService gitHubService, UserSettings settings)
    {
        _settings = settings;
        _gitHubService = gitHubService;

        LoadOrganizationsCommand = new AsyncRelayCommand(LoadOrganizations);
        LoadRepositoriesCommand = new AsyncRelayCommand(LoadRepositories, () => !string.IsNullOrWhiteSpace(SelectedOrganization?.Login));
        LoadTagsCommand = new AsyncRelayCommand(LoadTags, () => SelectedRepository != null && SelectedRepository.Id != 0);
        LoadCommitsCommand = new AsyncRelayCommand(LoadCommits, () => SelectedRepository != null && OriginalTag != null && NewTag != null);
        NavigateCommand = new RelayCommand<string>(NavigateToUrl);

        _commands = new List<IRelayCommand>
        {
            LoadOrganizationsCommand,
            LoadRepositoriesCommand,
            LoadTagsCommand,
            LoadCommitsCommand
        };
    }

    public string WindowTitle => $"GitHub Changes V{Version.Major}.{Version.Minor}";

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    #region Data Collections
    public IEnumerable<Organization> Organizations
    {
        get => _organizations;
        set => SetProperty(ref _organizations, value);
    }

    public IEnumerable<Repository> Repositories
    {
        get => _repositories;
        set => SetProperty(ref _repositories, value);
    }

    public IEnumerable<Tag> Tags
    {
        get => _tags;
        set => SetProperty(ref _tags, value);
    }

    public IEnumerable<Commit> Commits
    {
        get => _commits;
        set => SetProperty(ref _commits, value);
    }

    public IEnumerable<string> Tickets
    {
        get => _tickets;
        set => SetProperty(ref _tickets, value);
    }
    #endregion

    #region Selected UI Items
    public Organization SelectedOrganization
    {
        get => _selectedOrganization;
        set => SetProperty(ref _selectedOrganization, value, true);
    }

    public Repository SelectedRepository
    {
        get => _selectedRepository;
        set => SetProperty(ref _selectedRepository, value, true);
    }

    public Tag OriginalTag
    {
        get => _originalTag;
        set
        {
            SetProperty(ref _originalTag, value, true);
            RaiseCanExecute();
        }
    }

    public Tag NewTag
    {
        get => _newTag;
        set
        {
            SetProperty(ref _newTag, value, true);
            RaiseCanExecute();
        }
    } 
    #endregion

    #region Commands
    public IAsyncRelayCommand LoadOrganizationsCommand { get; }

    public IAsyncRelayCommand LoadRepositoriesCommand { get; }

    public IAsyncRelayCommand LoadTagsCommand { get; }

    public IAsyncRelayCommand LoadCommitsCommand { get; }

    public IRelayCommand<string> NavigateCommand { get; }


    private async Task LoadOrganizations()
    {
        Organizations = await GetItems(_gitHubService.GetOrganizationsForUser());
    }

    private async Task LoadRepositories()
    {
        Repositories = await GetItems(_gitHubService.GetRepositories(SelectedOrganization.Login));
    }

    private async Task LoadTags()
    {
        Tags = await GetItems(_gitHubService.GetTags(SelectedRepository.Id));
    }

    private async Task LoadCommits()
    {
        Commits = await GetItems(_gitHubService.GetCommits(SelectedRepository.Id, OriginalTag.RepoTag.Name, NewTag.RepoTag.Name));

        if (Commits != null)
        {
            var issues = new List<string>();
            foreach (var text in Commits.Select(x => x.Message))
            {
                foreach (Match match in Regex.Matches(text, @"[a-zA-Z]{2,4}-\d{1,6}"))
                {
                    match.Groups.Values.Each(x => issues.Add(x.Value));
                }
            }

            Tickets = issues.Distinct().OrderBy(x => x);
        }
    }

    private void NavigateToUrl(string jiraTicket)
    {
        Process.Start(new ProcessStartInfo(_settings.JiraRootUrl + jiraTicket)
        {
            UseShellExecute = true
        });
    }

    private void RaiseCanExecute()
    {
        foreach (var cmd in _commands)
        {
            cmd.NotifyCanExecuteChanged();
        }
    }

    private async Task<T> GetItems<T>(Task<T> loadTask)
    {
        Mouse.OverrideCursor = Cursors.Wait;
        IsLoading = true;
        try
        {
            return await loadTask;
        }
        catch (Exception e)
        {
            MessageBox.Show(e.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            Mouse.OverrideCursor = null;
            IsLoading = false;
        }

        return default;
    }

    #endregion

    #region Property Change UI Management
    public void Receive(PropertyChangedMessage<Organization> message)
    {
        ClearProperties(Level.Org);
    }

    public void Receive(PropertyChangedMessage<Repository> message)
    {
        ClearProperties(Level.Repo);
    }

    public void Receive(PropertyChangedMessage<Tag> message)
    {
        ClearProperties(Level.Tag);
    }

    private void ClearProperties(Level level)
    {
        switch (level)
        {
            case Level.Org:
                Repositories = null;
                SelectedRepository = null;
                goto case Level.Repo;
            case Level.Repo:
                Tags = null;
                OriginalTag = null;
                NewTag = null;
                goto case Level.Tag;
            case Level.Tag:
                Commits = null;
                Tickets = null;
                break;
        }
    } 
    #endregion
}