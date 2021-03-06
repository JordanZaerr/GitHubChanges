using System;
using System.IO;
using System.Text.Json;

namespace GitHubChanges.Settings;

public class SettingsManager<T> where T : class
{
    private readonly string _filePath;

    public SettingsManager(string fileName)
    {
        _filePath = GetLocalFilePath(fileName);
    }

    private string GetLocalFilePath(string fileName)
    {
        string appData = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(appData, fileName);
    }

    public T LoadSettings() =>
        File.Exists(_filePath) 
            ? JsonSerializer.Deserialize<T>(File.ReadAllText(_filePath)) 
            : null;

    public void SaveSettings(T settings)
    {
        string json = JsonSerializer.Serialize(settings);
        File.WriteAllText(_filePath, json);
    }
}
