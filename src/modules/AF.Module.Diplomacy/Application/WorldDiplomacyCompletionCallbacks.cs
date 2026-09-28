using System;

namespace AnimusForge;

// Adapter for the retained public callback signature; production uses the typed port.
internal readonly struct WorldDiplomacyCompletionCallbacks : IWorldDiplomacyCompletionEffects
{
    private readonly Func<WorldDiplomacyJob, bool> _hasStaleThreatPresentation;
    private readonly Func<WorldDiplomacyJob, bool> _refreshThreatPresentation;
    private readonly Func<WorldDiplomacyJob, bool> _hasStaleActionPresentation;
    private readonly Func<WorldDiplomacyJob, bool> _refreshActionPresentation;
    private readonly Action<WorldDiplomacyJob, string> _handleTruncatedDraft;
    private readonly Action<WorldDiplomacyJob, string> _commitGeneratedDocument;
    private readonly Action<WorldDiplomacyJob, string> _commitAnalysis;
    private readonly Action<WorldDiplomacyJob, string> _commitCompression;
    private readonly Action<WorldDiplomacyJob, string> _commitRoundPlan;
    private readonly Action<WorldDiplomacyJob, string> _commitRoundCompression;
    private readonly Action<WorldDiplomacyJob, string> _commitFailedJob;
    private readonly Action<string> _removeJob;
    private readonly Action<string> _log;
    internal WorldDiplomacyCompletionCallbacks(
        Func<WorldDiplomacyJob, bool> hasStaleThreatPresentation,
        Func<WorldDiplomacyJob, bool> refreshThreatPresentation,
        Func<WorldDiplomacyJob, bool> hasStaleActionPresentation,
        Func<WorldDiplomacyJob, bool> refreshActionPresentation,
        Action<WorldDiplomacyJob, string> handleTruncatedDraft,
        Action<WorldDiplomacyJob, string> commitGeneratedDocument,
        Action<WorldDiplomacyJob, string> commitAnalysis,
        Action<WorldDiplomacyJob, string> commitCompression,
        Action<WorldDiplomacyJob, string> commitRoundPlan,
        Action<WorldDiplomacyJob, string> commitRoundCompression,
        Action<WorldDiplomacyJob, string> commitFailedJob,
        Action<string> removeJob,
        Action<string> log)
    {
        _hasStaleThreatPresentation = hasStaleThreatPresentation;
        _refreshThreatPresentation = refreshThreatPresentation;
        _hasStaleActionPresentation = hasStaleActionPresentation;
        _refreshActionPresentation = refreshActionPresentation;
        _handleTruncatedDraft = handleTruncatedDraft;
        _commitGeneratedDocument = commitGeneratedDocument;
        _commitAnalysis = commitAnalysis;
        _commitCompression = commitCompression;
        _commitRoundPlan = commitRoundPlan;
        _commitRoundCompression = commitRoundCompression;
        _commitFailedJob = commitFailedJob;
        _removeJob = removeJob;
        _log = log;
    }
    public bool HasStaleThreatPresentation(WorldDiplomacyJob job) => _hasStaleThreatPresentation?.Invoke(job) == true;
    public bool RefreshThreatPresentation(WorldDiplomacyJob job) => _refreshThreatPresentation?.Invoke(job) == true;
    public bool HasStaleActionPresentation(WorldDiplomacyJob job) => _hasStaleActionPresentation?.Invoke(job) == true;
    public bool RefreshActionPresentation(WorldDiplomacyJob job) => _refreshActionPresentation?.Invoke(job) == true;
    public void HandleTruncatedDraft(WorldDiplomacyJob job, string content) => _handleTruncatedDraft?.Invoke(job, content);
    public void CommitGeneratedDocument(WorldDiplomacyJob job, string content) => _commitGeneratedDocument?.Invoke(job, content);
    public void CommitAnalysis(WorldDiplomacyJob job, string content) => _commitAnalysis?.Invoke(job, content);
    public void CommitCompression(WorldDiplomacyJob job, string content) => _commitCompression?.Invoke(job, content);
    public void CommitRoundPlan(WorldDiplomacyJob job, string content) => _commitRoundPlan?.Invoke(job, content);
    public void CommitRoundCompression(WorldDiplomacyJob job, string content) => _commitRoundCompression?.Invoke(job, content);
    public void CommitFailedJob(WorldDiplomacyJob job, string content) => _commitFailedJob?.Invoke(job, content);
    public void RemoveJob(string jobId) => _removeJob?.Invoke(jobId);
    public void Log(string message) => _log?.Invoke(message);
}
