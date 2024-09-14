namespace PlumbBuddy.Data;

[Index(nameof(Path))]
public class FileOfInterest
{
    [Key]
    public long Id { get; set; }

    public required string Path { get; set; }

    public ModsDirectoryFileType FileType { get; set; }
}
