namespace PlumbBuddy.Services;

public interface IPlatformFunctions :
    INotifyPropertyChanged
{
    IReadOnlyList<Regex> DiscardableDirectoryNamePatterns { get; }
    IReadOnlyList<Regex> DiscardableFileNamePatterns { get; }
    StringComparison FileSystemStringComparison { get; }
    IReadOnlyList<Regex> ForeignDirectoryNamePatterns { get; }
    IReadOnlyList<Regex> ForeignFileNamePatterns { get; }

    int ProgressMaximum { get; set; }
    AppProgressState ProgressState { get; set; }
    int ProgressValue { get; set; }

    Task<Process?> GetGameProcessAsync(DirectoryInfo installationDirectory);
    Task<bool> SendLocalNotificationAsync(string caption, string text);
    Task<bool> SetBadgeNumberAsync(int number);
    void ViewDirectory(DirectoryInfo directoryInfo);
    void ViewFile(FileInfo fileInfo);
}
