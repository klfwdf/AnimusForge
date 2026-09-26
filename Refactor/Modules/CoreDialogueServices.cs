namespace AnimusForge.Refactor.Modules;

/// <summary>
/// Same-DLL team -> AF service entry. Public V1 is a separate projection of this contract.
/// It delegates to the active real Native owner; it does not create a provider/prompt/action pipeline.
/// </summary>
internal static class CoreDialogueServices
{
    internal static CoreDialogueClient CreateClient()
        => new CoreDialogueClient(ShoutBehavior.SubmitModuleNativeDialogue);
}
