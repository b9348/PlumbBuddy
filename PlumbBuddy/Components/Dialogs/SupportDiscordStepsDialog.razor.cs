namespace PlumbBuddy.Components.Dialogs;

partial class SupportDiscordStepsDialog
{
    [Parameter]
    public string? CreatorName { get; set; }

    [Parameter]
    public FileInfo? ErrorFile { get; set; }

    [Parameter]
    public bool IsPatchDay { get; set; }

    [CascadingParameter]
    MudDialogInstance? MudDialog { get; set; }

    Dictionary<string, Collection<SupportDiscordStep>> Steps =>
        CreatorName is { } creatorName
        && SupportDiscord!.SpecificCreators.TryGetValue(creatorName, out var specificCreator)
        && specificCreator.AskForHelpSteps.Count is > 0
        ? specificCreator.AskForHelpSteps
        : IsPatchDay
        && SupportDiscord!.PatchDayHelpSteps.Count is > 0
        ? SupportDiscord!.PatchDayHelpSteps
        : ErrorFile is not null
        && SupportDiscord!.TextFileSubmissionSteps.Count is > 0
        ? SupportDiscord!.TextFileSubmissionSteps
        : SupportDiscord!.AskForHelpSteps;

    [Parameter]
    public SupportDiscord? SupportDiscord { get; set; }

    [Parameter]
    public string? SupportDiscordName { get; set; }

    void CloseOnClickHandler() =>
        MudDialog?.Close();

    Task<bool> HandlePreventStepChangeAsync(StepChangeDirection direction, int targetIndex)
    {
        if (targetIndex == Steps.GetLanguageOptimalValue(() => new()).Count)
            MudDialog?.Close();
        return Task.FromResult(false);
    }

    async Task HandleShowGameVersionFileOnClickAsync()
    {
        var gameVersionFile = new FileInfo(Path.Combine(Settings.UserDataFolderPath, "GameVersion.txt"));
        if (!gameVersionFile.Exists)
        {
            await DialogService.ShowErrorDialogAsync("I Couldn't Find Your Game Version File", "It looks like you need to launch The Sims 4 so that it will write that file. Once you've done that, come back here and click this button again.");
            return;
        }
        PlatformFunctions.ViewFile(gameVersionFile);
    }

    async Task HandleShowErrorFileOnClickAsync()
    {
        if (ErrorFile is null)
            return;
        if (!ErrorFile.Exists)
        {
            await DialogService.ShowErrorDialogAsync("I Couldn't Find Your Error File", "Something must have happened to it, I'm so sorry.");
            return;
        }
        PlatformFunctions.ViewFile(ErrorFile);
    }
}
