using System.Threading;

namespace AnimusForge.Refactor.Modules;

internal sealed class OnboardingOperationVersionOwner
{
    internal enum Kind { ApiValidation, BaseUrlValidation, ModelFetch }

    private int _apiValidation;
    private int _baseUrlValidation;
    private int _modelFetch;

    internal int Begin(Kind kind) => Increment(kind);
    internal void Cancel(Kind kind) => Increment(kind);

    internal bool IsCurrent(Kind kind, int version)
    {
        switch (kind)
        {
            case Kind.ApiValidation: return Volatile.Read(ref _apiValidation) == version;
            case Kind.BaseUrlValidation: return Volatile.Read(ref _baseUrlValidation) == version;
            default: return Volatile.Read(ref _modelFetch) == version;
        }
    }

    private int Increment(Kind kind)
    {
        switch (kind)
        {
            case Kind.ApiValidation: return Interlocked.Increment(ref _apiValidation);
            case Kind.BaseUrlValidation: return Interlocked.Increment(ref _baseUrlValidation);
            default: return Interlocked.Increment(ref _modelFetch);
        }
    }
}
