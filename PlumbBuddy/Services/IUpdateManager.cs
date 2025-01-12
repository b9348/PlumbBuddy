namespace PlumbBuddy.Services;

public interface IUpdateManager
{
    Version CurrentVersion { get; }

    Task<(Version? version, string? releaseNotes, Uri? downloadUrl)> CheckForUpdateAsync();

    Task PresentUpdateAsync(Version version, string? releaseNotes, Uri? downloadUrl);
}
