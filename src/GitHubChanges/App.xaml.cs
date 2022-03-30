using System;
using System.Windows;
using GitHubChanges.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace GitHubChanges
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            Services = ConfigureServices();
        }

        /// <summary>
        /// Gets the current <see cref="App"/> instance in use
        /// </summary>
        public new static App Current => (App)Application.Current;

        /// <summary>
        /// Gets the <see cref="IServiceProvider"/> instance to resolve application services.
        /// </summary>
        public IServiceProvider Services { get; }

        /// <summary>
        /// Configures the services for the application.
        /// </summary>
        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            //Services
            services.AddSingleton(new SettingsManager<UserSettings>("UserSettings.json"));
            services.AddSingleton(x => x.GetService<SettingsManager<UserSettings>>()?.LoadSettings());
            services.AddSingleton<IGitHubService>(x =>
            {
                var settings = x.GetService<UserSettings>();
                return new GitHubService(settings.GitHubPAT, settings.TagPrefixes);
            });

            //ViewModels
            services.AddTransient<MainViewModel>();

            return services.BuildServiceProvider();
        }
    }
}
