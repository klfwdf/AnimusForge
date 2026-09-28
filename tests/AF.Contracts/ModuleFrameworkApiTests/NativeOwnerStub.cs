// Directory-only suite cannot run a game owner; NativeModuleSubmissionTests covers the real entry.
namespace AnimusForge;
internal static class ShoutBehavior
{
    internal static string IssueModuleSceneTicket(string clientId) => null;
    internal static void RevokeModuleSceneTickets(string clientId) { }
    internal static void SubmitModuleNativeDialogue(Refactor.Modules.CoreDialogueOperation operation)
        => operation.Finish("native.owner_unavailable");
    internal static void SubmitModuleSceneDialogue(Refactor.Modules.CoreDialogueOperation operation)
        => operation.Finish("scene.owner_unavailable");
}
