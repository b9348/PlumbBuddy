namespace PlumbBuddy.Services;

public partial class SmartSimObserver :
    ISmartSimObserver
{
    [GeneratedRegex(@"^Mods[\\/].+$")]
    private static partial Regex GetModsDirectoryRelativePathPattern();

    [GeneratedRegex(@"^(?<path>.*?)[\\/]?$")]
    private static partial Regex GetTrimmedLocalPathSegmentsPattern();

    [GeneratedRegex(@"^\wp\d{2,}$", RegexOptions.IgnoreCase)]
    private static partial Regex GetTs4PackCodePattern();

    static readonly Regex modsDirectoryRelativePathPattern = GetModsDirectoryRelativePathPattern();
    static readonly Regex trimmedLocalPathSegmentsPattern = GetTrimmedLocalPathSegmentsPattern();

    public const string GlobalModsManifestPackageName = "PlumbBuddy_GlobalModsManifest.package";

    public SmartSimObserver(ILifetimeScope lifetimeScope, ILogger<ISmartSimObserver> logger, IPlatformFunctions platformFunctions, IPlayer player, IModsDirectoryCataloger modsDirectoryCataloger, ISuperSnacks superSnacks, PbDbContext pbDbContext)
    {
        ArgumentNullException.ThrowIfNull(lifetimeScope);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(platformFunctions);
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(modsDirectoryCataloger);
        ArgumentNullException.ThrowIfNull(superSnacks);
        ArgumentNullException.ThrowIfNull(pbDbContext);
        this.lifetimeScope = lifetimeScope.BeginLifetimeScope(ConfigureLifetimeScope);
        this.logger = logger;
        this.platformFunctions = platformFunctions;
        this.player = player;
        this.modsDirectoryCataloger = modsDirectoryCataloger;
        this.superSnacks = superSnacks;
        this.pbDbContext = pbDbContext;
        enqueuedScanningTaskLock = new();
        enqueuedResamplingPacksTaskLock = new();
        enqueuedFresheningTaskLock = new();
        fresheningTaskLock = new();
        resamplingPacksTaskLock = new();
        scanInstances = [];
        scanInstancesLock = new();
        scanIssues = [];
        scanningTaskLock = new();
        fileSystemStringComparison = platformFunctions.FileSystemStringComparison;
        this.modsDirectoryCataloger.PropertyChanged += HandleModsDirectoryCatalogerPropertyChanged;
        this.player.PropertyChanged += HandlePlayerPropertyChanged;
        ConnectToInstallationDirectory();
        ConnectToUserDataDirectory();
    }

    ~SmartSimObserver() =>
        Dispose(false);

    ImmutableArray<FileSystemInfo> cacheComponents;
    readonly AsyncLock enqueuedFresheningTaskLock;
    readonly AsyncLock enqueuedResamplingPacksTaskLock;
    readonly AsyncLock enqueuedScanningTaskLock;
    readonly StringComparison fileSystemStringComparison;
    readonly AsyncLock fresheningTaskLock;
    ImmutableArray<byte> globalModsManifestLastSha256 = ImmutableArray<byte>.Empty;
    FileSystemWatcher? installationDirectoryWatcher;
    bool isCurrentlyScanning;
    bool isModsDisabledGameSettingOn;
    bool isScriptModsEnabledGameSettingOn;
    bool isShowModListStartupGameSettingOn;
    IReadOnlyList<string> installedPackCodes = [];
    readonly ILifetimeScope lifetimeScope;
    readonly ILogger<ISmartSimObserver> logger;
    readonly IModsDirectoryCataloger modsDirectoryCataloger;
    FileSystemWatcher? packsDirectoryWatcher;
    readonly PbDbContext pbDbContext;
    readonly IPlatformFunctions platformFunctions;
    readonly IPlayer player;
    readonly AsyncLock resamplingPacksTaskLock;
    IReadOnlyList<ScanIssue> scanIssues;
    readonly Dictionary<Type, IScan> scanInstances;
    readonly AsyncLock scanInstancesLock;
    readonly AsyncLock scanningTaskLock;
    readonly ISuperSnacks superSnacks;
    FileSystemWatcher? userDataDirectoryWatcher;

    public bool IsCurrentlyScanning
    {
        get => isCurrentlyScanning;
        private set
        {
            if (isCurrentlyScanning == value)
                return;
            isCurrentlyScanning = value;
            OnPropertyChanged();
        }
    }

    public bool IsModsDisabledGameSettingOn
    {
        get => isModsDisabledGameSettingOn;
        private set
        {
            if (isModsDisabledGameSettingOn == value)
                return;
            isModsDisabledGameSettingOn = value;
            OnPropertyChanged();
        }
    }

    public bool IsScriptModsEnabledGameSettingOn
    {
        get => isScriptModsEnabledGameSettingOn;
        private set
        {
            if (isScriptModsEnabledGameSettingOn == value)
                return;
            isScriptModsEnabledGameSettingOn = value;
            OnPropertyChanged();
        }
    }

    public bool IsShowModListStartupGameSettingOn
    {
        get => isShowModListStartupGameSettingOn;
        private set
        {
            if (isShowModListStartupGameSettingOn == value)
                return;
            isShowModListStartupGameSettingOn = value;
            OnPropertyChanged();
        }
    }

    public IReadOnlyList<string> InstalledPackCodes
    {
        get => installedPackCodes;
        private set
        {
            var cleaned = value
                .Where(code => GetTs4PackCodePattern().IsMatch(code))
                .Select(code => code.Trim().ToUpperInvariant())
                .Distinct()
                .Order();
            if (installedPackCodes.SequenceEqual(cleaned))
                return;
            installedPackCodes = [..cleaned];
            FreshenGlobalManifest(force: true);
            OnPropertyChanged();
        }
    }

    string PacksDirectoryPath =>
#if MACCATALYST
        Path.Combine(new DirectoryInfo(player.InstallationFolderPath).Parent!.FullName, "The Sims 4 Packs");
#else
        player.InstallationFolderPath;
#endif

    public IReadOnlyList<ScanIssue> ScanIssues
    {
        get => scanIssues;
        private set
        {
            scanIssues = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    bool CatalogIfInModsDirectory(string userDataDirectoryRelativePath, out bool wasGlobalManifestChange)
    {
        if (modsDirectoryRelativePathPattern.IsMatch(userDataDirectoryRelativePath))
        {
            PutCatalogerToBedIfGameIsRunning();
            var modsDirectoryRelativePath = userDataDirectoryRelativePath[5..];
            wasGlobalManifestChange = modsDirectoryRelativePath is GlobalModsManifestPackageName;
            if (!wasGlobalManifestChange)
                modsDirectoryCataloger.Catalog(modsDirectoryRelativePath);
            return !wasGlobalManifestChange;
        }
        wasGlobalManifestChange = false;
        return false;
    }

    bool CatalogIfModsDirectory(string userDataDirectoryRelativePath)
    {
        if (userDataDirectoryRelativePath == "Mods")
        {
            PutCatalogerToBedIfGameIsRunning();
            modsDirectoryCataloger.Catalog(string.Empty);
            return true;
        }
        return false;
    }

    public void ClearCache()
    {
        try
        {
            foreach (var cacheComponent in cacheComponents)
            {
                cacheComponent.Refresh();
                if (cacheComponent.Exists)
                {
                    if (cacheComponent is DirectoryInfo directoryCacheComponent)
                        directoryCacheComponent.Delete(true);
                    else
                        cacheComponent.Delete();
                }
            }
        }
        catch (Exception ex)
        {
            superSnacks.OfferRefreshments(new MarkupString(
                $"""
                <h3>Whoops!</h3>
                I ran into a problem trying to clear your cache for you.<br />
                <br />
                Brief technical details:<br />
                <span style="font-family: monospace;">{ex.GetType().Name}: {ex.Message}</span><br />
                <br />
                There is more detailed technical information available in the log I write to the PlumbBuddy folder in your Documents.
                """), Severity.Warning, options =>
                {
                    options.RequireInteraction = true;
                    options.Icon = MaterialDesignIcons.Normal.EraserVariant;
                });
        }
    }

    void ConfigureLifetimeScope(ContainerBuilder containerBuilder)
    {
        containerBuilder.RegisterType<ModSettingScan>().As<IModSettingScan>();
        containerBuilder.RegisterType<ScriptModSettingScan>().As<IScriptModSettingScan>();
        containerBuilder.RegisterType<ShowModListStartupSettingScan>().As<IShowModListStartupSettingScan>();
        containerBuilder.RegisterType<PackageDepthScan>().As<IPackageDepthScan>();
        containerBuilder.RegisterType<Ts4ScriptDepthScan>().As<ITs4ScriptDepthScan>();
        containerBuilder.RegisterType<LooseZipArchiveScan>().As<ILooseZipArchiveScan>();
        containerBuilder.RegisterType<LooseRarArchiveScan>().As<ILooseRarArchiveScan>();
        containerBuilder.RegisterType<Loose7ZipArchiveScan>().As<ILoose7ZipArchiveScan>();
        containerBuilder.RegisterType<ErrorLogScan>().As<IErrorLogScan>();
        containerBuilder.RegisterType<McccMissingScan>().As<IMcccMissingScan>();
        containerBuilder.RegisterType<BeMissingScan>().As<IBeMissingScan>();
        containerBuilder.RegisterType<ModGuardMissingScan>().As<IModGuardMissingScan>();
        containerBuilder.RegisterType<DependencyScan>().As<IDependencyScan>();
        containerBuilder.RegisterType<CacheStalenessScan>().As<ICacheStalenessScan>();
        containerBuilder.RegisterType<MultipleModVersionsScan>().As<IMultipleModVersionsScan>();
    }

    void ConnectToInstallationDirectory()
    {
        if (player.Onboarded && Directory.Exists(player.InstallationFolderPath))
        {
            installationDirectoryWatcher = new FileSystemWatcher(player.InstallationFolderPath)
            {
                IncludeSubdirectories = true,
                NotifyFilter =
                      NotifyFilters.CreationTime
                    | NotifyFilters.DirectoryName
                    | NotifyFilters.FileName
                    | NotifyFilters.LastWrite
                    | NotifyFilters.Size
            };
            installationDirectoryWatcher.Changed += InstallationDirectoryFileSystemEntryChangedHandler;
            installationDirectoryWatcher.Created += InstallationDirectoryFileSystemEntryCreatedHandler;
            installationDirectoryWatcher.Deleted += InstallationDirectoryFileSystemEntryDeletedHandler;
            installationDirectoryWatcher.Error += InstallationDirectoryWatcherErrorHandler;
            installationDirectoryWatcher.Renamed += InstallationDirectoryFileSystemEntryRenamedHandler;
            installationDirectoryWatcher.EnableRaisingEvents = true;
            packsDirectoryWatcher = new FileSystemWatcher(PacksDirectoryPath)
            {
                IncludeSubdirectories = true,
                NotifyFilter =
                      NotifyFilters.CreationTime
                    | NotifyFilters.DirectoryName
                    | NotifyFilters.FileName
                    | NotifyFilters.LastWrite
                    | NotifyFilters.Size
            };
            packsDirectoryWatcher.Changed += PacksDirectoryFileSystemEntryChangedHandler;
            packsDirectoryWatcher.Created += PacksDirectoryFileSystemEntryCreatedHandler;
            packsDirectoryWatcher.Deleted += PacksDirectoryFileSystemEntryDeletedHandler;
            packsDirectoryWatcher.Error += PacksDirectoryWatcherErrorHandler;
            packsDirectoryWatcher.Renamed += PacksDirectoryFileSystemEntryRenamedHandler;
            packsDirectoryWatcher.EnableRaisingEvents = true;
            ResampleInstalledPackCodes();
            UpdateScanInitializationStatus();
        }
    }

    void ConnectToUserDataDirectory()
    {
        if (player.Onboarded && Directory.Exists(player.UserDataFolderPath))
        {
            Task.Run(ResampleGameOptionsAsync);
            cacheComponents =
            [
                new FileInfo(Path.Combine(player.UserDataFolderPath, "avatarcache.package")),
                new FileInfo(Path.Combine(player.UserDataFolderPath, "clientDB.package")),
                new FileInfo(Path.Combine(player.UserDataFolderPath, "houseDescription-client.package")),
                new FileInfo(Path.Combine(player.UserDataFolderPath, "localthumbcache.package")),
                new DirectoryInfo(Path.Combine(player.UserDataFolderPath, "cachestr")),
                new DirectoryInfo(Path.Combine(player.UserDataFolderPath, "onlinethumbnailcache"))
            ];
            ResampleCacheClarity();
            userDataDirectoryWatcher = new FileSystemWatcher(player.UserDataFolderPath)
            {
                IncludeSubdirectories = true,
                NotifyFilter =
                      NotifyFilters.CreationTime
                    | NotifyFilters.DirectoryName
                    | NotifyFilters.FileName
                    | NotifyFilters.LastWrite
                    | NotifyFilters.Size
            };
            userDataDirectoryWatcher.Changed += UserDataDirectoryFileSystemEntryChangedHandler;
            userDataDirectoryWatcher.Created += UserDataDirectoryFileSystemEntryCreatedHandler;
            userDataDirectoryWatcher.Deleted += UserDataDirectoryFileSystemEntryDeletedHandler;
            userDataDirectoryWatcher.Error += UserDataDirectoryWatcherErrorHandler;
            userDataDirectoryWatcher.Renamed += UserDataDirectoryFileSystemEntryRenamedHandler;
            userDataDirectoryWatcher.EnableRaisingEvents = true;
            UpdateScanInitializationStatus();
            PutCatalogerToBedIfGameIsRunning();
            modsDirectoryCataloger.Catalog(string.Empty);
            FreshenGlobalManifest(force: true);
        }
    }

    void DisconnectFromInstallationDirectoryWatcher()
    {
        if (installationDirectoryWatcher is not null)
        {
            installationDirectoryWatcher.Changed -= InstallationDirectoryFileSystemEntryChangedHandler;
            installationDirectoryWatcher.Created -= InstallationDirectoryFileSystemEntryCreatedHandler;
            installationDirectoryWatcher.Deleted -= InstallationDirectoryFileSystemEntryDeletedHandler;
            installationDirectoryWatcher.Error -= InstallationDirectoryWatcherErrorHandler;
            installationDirectoryWatcher.Renamed -= InstallationDirectoryFileSystemEntryRenamedHandler;
            installationDirectoryWatcher.Dispose();
            installationDirectoryWatcher = null;
        }
        if (packsDirectoryWatcher is not null)
        {
            packsDirectoryWatcher.Changed -= PacksDirectoryFileSystemEntryChangedHandler;
            packsDirectoryWatcher.Created -= PacksDirectoryFileSystemEntryCreatedHandler;
            packsDirectoryWatcher.Deleted -= PacksDirectoryFileSystemEntryDeletedHandler;
            packsDirectoryWatcher.Error -= PacksDirectoryWatcherErrorHandler;
            packsDirectoryWatcher.Renamed -= PacksDirectoryFileSystemEntryRenamedHandler;
            packsDirectoryWatcher.Dispose();
            packsDirectoryWatcher = null;
        }
    }

    void DisconnectFromUserDataDirectoryWatcher()
    {
        if (userDataDirectoryWatcher is not null)
        {
            userDataDirectoryWatcher.Changed -= UserDataDirectoryFileSystemEntryChangedHandler;
            userDataDirectoryWatcher.Created -= UserDataDirectoryFileSystemEntryCreatedHandler;
            userDataDirectoryWatcher.Deleted -= UserDataDirectoryFileSystemEntryDeletedHandler;
            userDataDirectoryWatcher.Error -= UserDataDirectoryWatcherErrorHandler;
            userDataDirectoryWatcher.Renamed -= UserDataDirectoryFileSystemEntryRenamedHandler;
            userDataDirectoryWatcher.Dispose();
            userDataDirectoryWatcher = null;
            cacheComponents = [];
            IsModsDisabledGameSettingOn = true;
            IsScriptModsEnabledGameSettingOn = false;
            IsShowModListStartupGameSettingOn = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            DisconnectFromInstallationDirectoryWatcher();
            DisconnectFromUserDataDirectoryWatcher();
            modsDirectoryCataloger.PropertyChanged -= HandleModsDirectoryCatalogerPropertyChanged;
            player.PropertyChanged -= HandlePlayerPropertyChanged;
            lifetimeScope.Dispose();
        }
    }

    void FreshenGlobalManifest(bool force = false) =>
        _ = Task.Run(() => FreshenGlobalManifestAsync(force));

    async Task FreshenGlobalManifestAsync(bool force = false)
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        cts.Cancel();
        var enqueuedFresheningTaskLockPotentiallyHeld = await enqueuedFresheningTaskLock.LockAsync(token).ConfigureAwait(false);
        if (enqueuedFresheningTaskLockPotentiallyHeld is null)
            return;
        using var fresheningTaskLockHeld = await fresheningTaskLock.LockAsync().ConfigureAwait(false);
        enqueuedFresheningTaskLockPotentiallyHeld.Dispose();
        var modsDirectory = new DirectoryInfo(Path.Combine(player.UserDataFolderPath, "Mods"));
        if (!modsDirectory.Exists)
            return;
        var globalModsManifestPackageFileInfo = new FileInfo(Path.Combine(modsDirectory.FullName, GlobalModsManifestPackageName));
        if (!force
            && globalModsManifestPackageFileInfo.Exists
            && globalModsManifestLastSha256.SequenceEqual(await ModFileManifestModel.GetFileSha256HashAsync(globalModsManifestPackageFileInfo.FullName).ConfigureAwait(false)))
            return;
        var manifestedModFiles = new List<GlobalModsManifestModelManifestedModFile>();
        foreach (var modFileHashElements in await pbDbContext.ModFileHashes
            .AsSplitQuery()
            .Where(mfh => mfh.ModFiles!.Any(mf => mf.Path != null && mf.AbsenceNoticed == null) && mfh.ModFileManifests!.Any())
            .Select(mfh => new
            {
                Paths = mfh.ModFiles!.Where(mf => mf.Path != null && mf.AbsenceNoticed == null).Select(mf => mf.Path!).ToList(),
                Manifests = mfh.ModFileManifests!.Select(mfm => new
                {
                    mfm.Key,
                    mfm.TuningName,
                    CalculatedSha256 = mfm.CalculatedModFileManifestHash!.Sha256,
                    SubsumedSha256 = mfm.SubsumedHashes!.Select(mfmh => mfmh.Sha256).ToList()
                }).ToList()
            })
            .ToListAsync()
            .ConfigureAwait(false))
            foreach (var manifest in modFileHashElements.Manifests)
            {
                var hashes = manifest.SubsumedSha256
                    .Append(manifest.CalculatedSha256)
                    .Select(byteArray => byteArray.ToImmutableArray())
                    .Select(ia => ia.ToHexString())
                    .Distinct()
                    .Select(hex => hex.ToByteSequence().ToImmutableArray())
                    .ToImmutableArray();
                manifestedModFiles.AddRange(modFileHashElements.Paths.Select(path =>
                {
                    var manifestedModFile = new GlobalModsManifestModelManifestedModFile
                    {
                        ModsFolderPath = path,
                        ManifestKey = manifest.Key,
                        ManifestTuningName = manifest.TuningName
                    };
                    manifestedModFile.Hashes.UnionWith(hashes);
                    return manifestedModFile;
                }));
            }
        var model = new GlobalModsManifestModel();
        foreach (var packCode in InstalledPackCodes)
            model.InstalledPacks.Add(packCode);
        foreach (var manifestedModFile in manifestedModFiles.OrderBy(mfm => mfm.ModsFolderPath))
            model.ManifestedModFiles.Add(manifestedModFile);
        using var globalManifestPackage = new DataBasePackedFile();
        await globalManifestPackage.SetAsync(GlobalModsManifestModel.ResourceKey, model).ConfigureAwait(false);
        await globalManifestPackage.SaveAsAsync(globalModsManifestPackageFileInfo.FullName).ConfigureAwait(false);
        globalModsManifestLastSha256 = await ModFileManifestModel.GetFileSha256HashAsync(globalModsManifestPackageFileInfo.FullName).ConfigureAwait(false);
    }

    string GetRelativePathInUserDataFolder(string fullPath)
    {
        var trimmedLocalPathSegmentsMatch = trimmedLocalPathSegmentsPattern.Match(player.UserDataFolderPath);
        if (!trimmedLocalPathSegmentsMatch.Success)
            throw new InvalidOperationException("User data folder path is not a valid path");
        try
        {
            return fullPath[(trimmedLocalPathSegmentsMatch.Groups["path"].Value.Length + 1)..];
        }
        catch (Exception ex)
        {
            throw new ArgumentException("Path is not valid or not within the user data folder", nameof(fullPath), ex);
        }
    }

    void HandleModsDirectoryCatalogerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IModsDirectoryCataloger.State)
            && modsDirectoryCataloger.State is ModsDirectoryCatalogerState.Idle)
        {
            FreshenGlobalManifest(force: true);
            Scan();
        }
    }

    void HandlePlayerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IPlayer.CacheStatus) or nameof(IPlayer.Type))
            Scan();
        else if (e.PropertyName == nameof(IPlayer.InstallationFolderPath))
        {
            DisconnectFromInstallationDirectoryWatcher();
            ConnectToInstallationDirectory();
        }
        else if (e.PropertyName == nameof(IPlayer.Onboarded))
        {
            DisconnectFromInstallationDirectoryWatcher();
            DisconnectFromUserDataDirectoryWatcher();
            ConnectToInstallationDirectory();
            ConnectToUserDataDirectory();
        }
        else if (e.PropertyName == nameof(IPlayer.UserDataFolderPath))
        {
            DisconnectFromUserDataDirectoryWatcher();
            ConnectToUserDataDirectory();
        }
        else if (e.PropertyName?.StartsWith("Scan", StringComparison.OrdinalIgnoreCase) ?? false)
            UpdateScanInitializationStatus();
    }

    void InstallationDirectoryFileSystemEntryChangedHandler(object sender, FileSystemEventArgs e)
    {
        // TODO: Phase 2 PreJector
    }

    void InstallationDirectoryFileSystemEntryCreatedHandler(object sender, FileSystemEventArgs e)
    {
        // TODO: Phase 2 PreJector
    }

    void InstallationDirectoryFileSystemEntryDeletedHandler(object sender, FileSystemEventArgs e)
    {
        // TODO: Phase 2 PreJector
    }

    void InstallationDirectoryFileSystemEntryRenamedHandler(object sender, RenamedEventArgs e)
    {
        // TODO: Phase 2 PreJector
    }

    void InstallationDirectoryWatcherErrorHandler(object sender, ErrorEventArgs e)
    {
        DisconnectFromInstallationDirectoryWatcher();
        ConnectToInstallationDirectory();
    }

    bool IsCacheLocked()
    {
        try
        {
            foreach (var fileInfo in cacheComponents.OfType<FileInfo>())
            {
                fileInfo.Refresh();
                if (fileInfo.Exists)
                {
                    using var fs = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                }
            }
            return false;
        }
        catch (IOException)
        {
            return true;
        }
    }

    void OnPropertyChanged(PropertyChangedEventArgs e) =>
        PropertyChanged?.Invoke(this, e);

    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        OnPropertyChanged(new PropertyChangedEventArgs(propertyName));

    public void OpenDownloadsFolder() =>
        platformFunctions.ViewDirectory(new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")));

    public void OpenModsFolder()
    {
        if (player.Onboarded)
        {
            var modsDirectory = new DirectoryInfo(Path.Combine(player.UserDataFolderPath, "Mods"));
            if (modsDirectory.Exists)
                platformFunctions.ViewDirectory(modsDirectory);
        }
    }

    void PacksDirectoryFileSystemEntryChangedHandler(object sender, FileSystemEventArgs e) =>
        ResampleInstalledPackCodes();

    void PacksDirectoryFileSystemEntryCreatedHandler(object sender, FileSystemEventArgs e) =>
        ResampleInstalledPackCodes();

    void PacksDirectoryFileSystemEntryDeletedHandler(object sender, FileSystemEventArgs e) =>
        ResampleInstalledPackCodes();

    void PacksDirectoryFileSystemEntryRenamedHandler(object sender, RenamedEventArgs e) =>
        ResampleInstalledPackCodes();

    void PacksDirectoryWatcherErrorHandler(object sender, ErrorEventArgs e)
    {
        DisconnectFromInstallationDirectoryWatcher();
        ConnectToInstallationDirectory();
    }

    void PutCatalogerToBedIfGameIsRunning()
    {
        if (modsDirectoryCataloger.State is not ModsDirectoryCatalogerState.Sleeping
            && (DeviceInfo.Platform == DevicePlatform.macOS || DeviceInfo.Platform == DevicePlatform.MacCatalyst)
            || IsCacheLocked())
            Task.Run(PutCatalogerToBedWhileGameIsRunningAsync);
    }

    async Task PutCatalogerToBedWhileGameIsRunningAsync()
    {
        if (await platformFunctions.GetGameProcessAsync(new DirectoryInfo(player.InstallationFolderPath)).ConfigureAwait(false) is { } ts4Process)
        {
            modsDirectoryCataloger.GoToSleep();
            await ts4Process.WaitForExitAsync().ConfigureAwait(false);
            ts4Process.Dispose();
            modsDirectoryCataloger.WakeUp();
        }
    }

    void ResampleCacheClarity()
    {
        foreach (var cacheComponent in cacheComponents)
            cacheComponent.Refresh();
        var anyCacheComponentsExistOnDisk = cacheComponents.Any(ce => ce.Exists);
        if (player.CacheStatus is SmartSimCacheStatus.Clear && anyCacheComponentsExistOnDisk)
        {
            player.CacheStatus = SmartSimCacheStatus.Normal;
            PutCatalogerToBedIfGameIsRunning();
        }
        else if (player.CacheStatus is not SmartSimCacheStatus.Clear && !anyCacheComponentsExistOnDisk)
            player.CacheStatus = SmartSimCacheStatus.Clear;
    }

    async Task ResampleGameOptionsAsync()
    {
        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
        var previousIsModsDisabledGameSettingOn = IsModsDisabledGameSettingOn;
        var previousIsScriptModsEnabledGameSettingOn = IsScriptModsEnabledGameSettingOn;
        var previousIsShowModListStartupGameSettingOn = IsShowModListStartupGameSettingOn;
        var parsedSuccessfully = false;
        var optionsIniFile = new FileInfo(Path.Combine(player.UserDataFolderPath, "Options.ini"));
        if (optionsIniFile.Exists)
        {
            try
            {
                var parser = new IniDataParser();
                var data = parser.Parse(await File.ReadAllTextAsync(optionsIniFile.FullName).ConfigureAwait(false));
                var optionsData = data["options"];
                IsModsDisabledGameSettingOn = optionsData["modsdisabled"] == "1";
                IsScriptModsEnabledGameSettingOn = optionsData["scriptmodsenabled"] == "1";
                IsShowModListStartupGameSettingOn = optionsData["showmodliststartup"] == "1";
                parsedSuccessfully = true;
            }
            catch (ParsingException ex)
            {
                // eww, a bad INI file?
                logger.LogWarning(ex, "attempting to parse the game options INI file at {path} failed", optionsIniFile.FullName);
                superSnacks.OfferRefreshments(new MarkupString(
                    $"""
                    <h3>Whoops!</h3>
                    I ran into a problem trying to read the file which contains your game options for The Sims 4:<br />
                    <strong>{optionsIniFile.FullName}</strong><br />
                    <br />
                    Brief technical details:<br />
                    <span style="font-family: monospace;">{ex.GetType().Name}: {ex.Message}</span><br />
                    <br />
                    There is more detailed technical information available in the log I write to the PlumbBuddy folder in your Documents.
                    """), Severity.Warning, options =>
                    {
                        options.RequireInteraction = true;
                        options.Icon = MaterialDesignIcons.Normal.CogOff;
                    });
            }
        }
        if (!parsedSuccessfully)
        {
            IsModsDisabledGameSettingOn = true;
            IsScriptModsEnabledGameSettingOn = false;
            IsShowModListStartupGameSettingOn = true;
        }
        if (IsModsDisabledGameSettingOn != previousIsModsDisabledGameSettingOn
            || IsScriptModsEnabledGameSettingOn != previousIsScriptModsEnabledGameSettingOn
            || IsShowModListStartupGameSettingOn != previousIsShowModListStartupGameSettingOn)
            Scan();
    }

    bool ResampleGameOptionsIfTheyChanged(string relativePath)
    {
        if (relativePath.Equals("Options.ini", fileSystemStringComparison))
        {
            Task.Run(ResampleGameOptionsAsync);
            return true;
        }
        return false;
    }

    void ResampleInstalledPackCodes() =>
        _ = Task.Run(ResampleInstalledPackCodesAsync);

    async Task ResampleInstalledPackCodesAsync()
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        cts.Cancel();
        var enqueuedResamplingPacksTaskLockPotentiallyHeld = await enqueuedResamplingPacksTaskLock.LockAsync(token).ConfigureAwait(false);
        if (enqueuedResamplingPacksTaskLockPotentiallyHeld is null)
            return;
        using var resamplingPacksTaskLockHeld = await resamplingPacksTaskLock.LockAsync().ConfigureAwait(false);
        enqueuedResamplingPacksTaskLockPotentiallyHeld.Dispose();
        if (installedPackCodes.Count is > 0)
            await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        var packsDirectory = new DirectoryInfo(PacksDirectoryPath);
        if (!packsDirectory.Exists)
            return;
        InstalledPackCodes = [.. packsDirectory.GetDirectories().Select(directoryInfo => directoryInfo.Name)];
    }

    void Scan() =>
        Task.Run(ScanAsync);

    async Task ScanAsync()
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        cts.Cancel();
        var enqueuedScanningTaskLockPotentiallyHeld = await enqueuedScanningTaskLock.LockAsync(token).ConfigureAwait(false);
        if (enqueuedScanningTaskLockPotentiallyHeld is null)
            return;
        using var scanningTaskLockHeld = await scanningTaskLock.LockAsync().ConfigureAwait(false);
        enqueuedScanningTaskLockPotentiallyHeld.Dispose();
        using var scanInstancesLockHeld = await scanInstancesLock.LockAsync();
        IsCurrentlyScanning = true;
        var scanIssues = new ConcurrentBag<ScanIssue>();
        using (var semaphore = new SemaphoreSlim(Math.Max(1, Environment.ProcessorCount / 4)))
        {
            await Task.WhenAll(scanInstances.Values.Select(async scanInstance =>
            {
                await semaphore.WaitAsync().ConfigureAwait(false);
                try
                {
                    await foreach (var scanIssue in scanInstance.ScanAsync())
                        scanIssues.Add(scanIssue);
                }
                finally
                {
                    semaphore.Release();
                }
            })).ConfigureAwait(false);
        }
        ScanIssues = [..scanIssues.OrderByDescending(scanIssue => scanIssue.Type).ThenBy(scanIssue => scanIssue.Caption)];
        await platformFunctions.SetBadgeNumberAsync(scanIssues.Count(si => si.Type is not ScanIssueType.Healthy)).ConfigureAwait(false);
        IsCurrentlyScanning = false;
    }

    void UpdateScanInitializationStatus() =>
        Task.Run(UpdateScanInitializationStatusAsync);

    async Task UpdateScanInitializationStatusAsync()
    {
        using var scanInstancesLockHeld = await scanInstancesLock.LockAsync();
        var fullyConnectedToGame = installationDirectoryWatcher is not null && userDataDirectoryWatcher is not null;
        var scansInitialized = scanInstances.Count > 0;
        if (!fullyConnectedToGame && scansInitialized)
        {
            foreach (var scan in scanInstances.Values)
                scan.Dispose();
            scanInstances.Clear();
            scanInstances.TrimExcess();
            ScanIssues = [];
            return;
        }
        if (fullyConnectedToGame)
        {
            var initializationChange = false;
            bool checkScanInitialization(bool playerHasScanEnabled, Type scanInterface)
            {
                if (playerHasScanEnabled && !scanInstances.ContainsKey(scanInterface))
                {
                    scanInstances.Add(scanInterface, (IScan)lifetimeScope.Resolve(scanInterface));
                    return true;
                }
                if (!playerHasScanEnabled && scanInstances.Remove(scanInterface, out var scanInstance))
                {
                    scanInstance.Dispose();
                    return true;
                }
                return false;
            }
            initializationChange |= checkScanInitialization(player.ScanForModsDisabled, typeof(IModSettingScan));
            initializationChange |= checkScanInitialization(player.ScanForScriptModsDisabled, typeof(IScriptModSettingScan));
            initializationChange |= checkScanInitialization(player.ScanForShowModsListAtStartupEnabled, typeof(IShowModListStartupSettingScan));
            initializationChange |= checkScanInitialization(player.ScanForInvalidModSubdirectoryDepth, typeof(IPackageDepthScan));
            initializationChange |= checkScanInitialization(player.ScanForInvalidScriptModSubdirectoryDepth, typeof(ITs4ScriptDepthScan));
            initializationChange |= checkScanInitialization(player.ScanForLooseZipArchives, typeof(ILooseZipArchiveScan));
            initializationChange |= checkScanInitialization(player.ScanForLooseRarArchives, typeof(ILooseRarArchiveScan));
            initializationChange |= checkScanInitialization(player.ScanForLoose7ZipArchives, typeof(ILoose7ZipArchiveScan));
            initializationChange |= checkScanInitialization(player.ScanForErrorLogs, typeof(IErrorLogScan));
            initializationChange |= checkScanInitialization(player.ScanForMissingMccc, typeof(IMcccMissingScan));
            initializationChange |= checkScanInitialization(player.ScanForMissingBe, typeof(IBeMissingScan));
            initializationChange |= checkScanInitialization(player.ScanForMissingModGuard, typeof(IModGuardMissingScan));
            initializationChange |= checkScanInitialization(player.ScanForMissingDependency, typeof(IDependencyScan));
            initializationChange |= checkScanInitialization(player.ScanForCacheStaleness, typeof(ICacheStalenessScan));
            initializationChange |= checkScanInitialization(player.ScanForMultipleModVersions, typeof(IMultipleModVersionsScan));
            if (initializationChange)
                Scan();
        }
    }

    void UserDataDirectoryFileSystemEntryChangedHandler(object sender, FileSystemEventArgs e)
    {
        var relativePath = GetRelativePathInUserDataFolder(e.FullPath);
        if (ResampleGameOptionsIfTheyChanged(relativePath))
            return;
        if (CatalogIfInModsDirectory(relativePath, out var globalManifestWasOverwritten))
            return;
        if (globalManifestWasOverwritten)
            FreshenGlobalManifest();
    }

    void UserDataDirectoryFileSystemEntryCreatedHandler(object sender, FileSystemEventArgs e)
    {
        if (player.CacheStatus is SmartSimCacheStatus.Clear && cacheComponents.Any(cc => e.FullPath.StartsWith(cc.FullName, fileSystemStringComparison)))
        {
            player.CacheStatus = SmartSimCacheStatus.Normal;
            return;
        }
        var relativePath = GetRelativePathInUserDataFolder(e.FullPath);
        if (ResampleGameOptionsIfTheyChanged(relativePath))
            return;
        if (CatalogIfModsDirectory(relativePath))
        {
            FreshenGlobalManifest();
            return;
        }
        if (CatalogIfInModsDirectory(relativePath, out var globalManifestWasOverwritten))
            return;
        if (globalManifestWasOverwritten)
            FreshenGlobalManifest();
    }

    void UserDataDirectoryFileSystemEntryDeletedHandler(object sender, FileSystemEventArgs e)
    {
        var fullPath = Path.GetFullPath(e.FullPath);
        if (player.CacheStatus is not SmartSimCacheStatus.Clear && cacheComponents.Any(cc => Path.GetFullPath(cc.FullName).Equals(fullPath, fileSystemStringComparison)))
        {
            ResampleCacheClarity();
            return;
        }
        var relativePath = GetRelativePathInUserDataFolder(e.FullPath);
        if (ResampleGameOptionsIfTheyChanged(relativePath))
            return;
        if (CatalogIfModsDirectory(relativePath))
        {
            FreshenGlobalManifest();
            return;
        }
        if (CatalogIfInModsDirectory(relativePath, out var globalManifestWasDeleted))
            return;
        if (globalManifestWasDeleted)
            FreshenGlobalManifest();
    }

    void UserDataDirectoryFileSystemEntryRenamedHandler(object sender, RenamedEventArgs e)
    {
        var oldRelativePath = GetRelativePathInUserDataFolder(e.OldFullPath);
        var relativePath = GetRelativePathInUserDataFolder(e.FullPath);
        if (ResampleGameOptionsIfTheyChanged(oldRelativePath) | ResampleGameOptionsIfTheyChanged(relativePath))
            return;
        if (CatalogIfModsDirectory(oldRelativePath) | CatalogIfModsDirectory(relativePath))
        {
            FreshenGlobalManifest();
            return;
        }
        if (CatalogIfInModsDirectory(oldRelativePath, out var globalManifestWasRenamed) | CatalogIfInModsDirectory(relativePath, out var globalManifestWasOverwritten))
            return;
        if (globalManifestWasRenamed || globalManifestWasOverwritten)
            FreshenGlobalManifest();
    }

    void UserDataDirectoryWatcherErrorHandler(object sender, ErrorEventArgs e)
    {
        DisconnectFromUserDataDirectoryWatcher();
        ConnectToUserDataDirectory();
    }
}
