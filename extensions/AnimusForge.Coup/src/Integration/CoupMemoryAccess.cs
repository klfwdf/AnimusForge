using System;
using AnimusForge.Refactor.Contracts;
#if ANIMUSFORGE_COUP_STANDALONE
using System.Reflection;
#endif

namespace AnimusForge.CoupSystem;

// Cached once when AfAccess initializes. No reflective constructor, object[]
// invocation or member lookup in the outcome/retry path of the shipped same-DLL mod.
internal sealed class CoupMemoryAccess
{
    internal delegate bool PrepareIdentity(InteractionMemoryCommit commit, string npcName,
        out string recoveryId, out string payloadHash, out string errorCode);

    internal readonly Func<string, string, string, int, string, InteractionMemoryCommit> CreateCommit;
    internal readonly PrepareIdentity Prepare;
    internal readonly Func<InteractionMemoryCommit, string, MemoryCommitResult> Commit;
    internal readonly Func<string, string, string, string> GetStatus;

    internal CoupMemoryAccess()
    {
#if ANIMUSFORGE_COUP_STANDALONE
        // Legacy standalone source builds retain a narrow host bridge. Only the stable
        // internal port is bound; it is intentionally not a new public/versioned API.
        Type port = typeof(MyBehavior).Assembly.GetType("AnimusForge.CoupOutcomeMemoryPort", true);
        CreateCommit = Bind<Func<string, string, string, int, string, InteractionMemoryCommit>>(port, "CreateCommit");
        Prepare = Bind<PrepareIdentity>(port, "Prepare");
        Commit = Bind<Func<InteractionMemoryCommit, string, MemoryCommitResult>>(port, "Commit");
        GetStatus = Bind<Func<string, string, string, string>>(port, "GetStatus");
#else
        CreateCommit = CoupOutcomeMemoryPort.CreateCommit;
        Prepare = CoupOutcomeMemoryPort.Prepare;
        Commit = CoupOutcomeMemoryPort.Commit;
        GetStatus = CoupOutcomeMemoryPort.GetStatus;
#endif
    }

#if ANIMUSFORGE_COUP_STANDALONE
    private static T Bind<T>(Type owner, string name) where T : Delegate
        => (T)Delegate.CreateDelegate(typeof(T), owner.GetMethod(name,
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(owner.FullName, name));
#endif
}
