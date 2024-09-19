namespace PlumbBuddy.Data;

[Index(nameof(Sha256), IsUnique = true)]
public class ModFileHash
{
    [Key]
    public long Id { get; set; }

    [Required]
    [Length(32, 32)]
    [SuppressMessage("Performance", "CA1819: Properties should not return arrays")]
    public required byte[] Sha256 { get; set; }

    [Required]
    public bool ResourcesAndManifestCataloged { get; set; }

    [SuppressMessage("Usage", "CA2227: Collection properties should be read only")]
    public ICollection<ModFileResource>? Resources { get; set; }

    public long? ModManifestId { get; set; }

    [ForeignKey(nameof(ModManifestId))]
    public ModManifest? ModManifest { get; set; }

    [SuppressMessage("Usage", "CA2227: Collection properties should be read only")]
    public ICollection<ModFile>? ModFiles { get; set; }
}