// Directory-only suite cannot run a game owner; NativeModuleSubmissionTests covers the real entry.
namespace AnimusForge;
internal static class ShoutBehavior
{
    internal static void SubmitModuleNativeDialogue(Refactor.Modules.CoreDialogueOperation operation)
        => operation.Finish("native.owner_unavailable");
}
