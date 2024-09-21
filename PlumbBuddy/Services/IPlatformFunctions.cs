namespace PlumbBuddy.Services;

public interface IPlatformFunctions
{
    StringComparison FileSystemStringComparison { get; }

    Task<Process?> GetGameProcessAsync(DirectoryInfo installationDirectory);
    Task SendLocalNotificationAsync(string caption, string text);
    Task SetBadgeNumberAsync(int number);
    void ViewDirectory(DirectoryInfo directoryInfo);
    void ViewFile(FileInfo fileInfo);
}
