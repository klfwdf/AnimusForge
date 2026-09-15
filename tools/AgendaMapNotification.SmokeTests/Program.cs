using System;
using System.IO;

string root = FindRepositoryRoot();
string source = File.ReadAllText(Path.Combine(root, "VoteDealBehavior.MapNotification.cs"));
string voteDealSource = File.ReadAllText(Path.Combine(root, "VoteDealBehavior.cs"));

string registration = ExtractMethod(source, "private bool TryEnsureAgendaMapNotificationRegistered()");
Assert(registration.Contains("view.RegisterMapNotificationType", StringComparison.Ordinal),
    "agenda notification type must still register on each replacement map view");
Assert(!registration.Contains("_publishedAgendaMapNotices.Clear()", StringComparison.Ordinal),
    "rebinding a replacement map view must not forget agendas already published in this runtime");

string runtimeReset = ExtractMethod(source, "private void ResetAgendaMapNotificationRuntime()");
Assert(runtimeReset.Contains("_publishedAgendaMapNotices.Clear()", StringComparison.Ordinal),
    "save-load runtime reset must still clear the publication tracker");

string publication = ExtractMethod(source, "private void TryPublishPendingAgendaMapNotifications()");
Assert(publication.Contains("_publishedAgendaMapNotices.Contains(decision)", StringComparison.Ordinal)
    && publication.Contains("_publishedAgendaMapNotices.Add(decision)", StringComparison.Ordinal),
    "agenda publication must retain its one-notice-per-decision guard");

string hourlyElection = ExtractMethod(voteDealSource, "private void StartExpiredAgendaElection(Kingdom kingdom, KingdomDecision decision)");
string patchedElection = ExtractMethod(voteDealSource, "private static bool StartDecisionElectionSafe(Kingdom kingdom, KingdomDecision decision)");
string callToWarRedirect = ExtractMethod(voteDealSource, "private static void TryConsumeRedirectedPlayerCallToWarProposal(");
Assert(hourlyElection.Contains("TryConsumeRedirectedPlayerCallToWarProposal(kingdom, decision)", StringComparison.Ordinal)
    && patchedElection.Contains("TryConsumeRedirectedPlayerCallToWarProposal(kingdom, decision)", StringComparison.Ordinal),
    "both delayed-election paths must consume redirected player call-to-war proposals");
Assert(callToWarRedirect.Contains("decision as ProposeCallToWarAgreementDecision", StringComparison.Ordinal)
    && callToWarRedirect.Contains("proposal.CalledKingdom != playerKingdom", StringComparison.Ordinal)
    && callToWarRedirect.Contains("kingdom.RemoveDecision(decision)", StringComparison.Ordinal),
    "redirect cleanup must only consume the foreign source proposal after it targets the player kingdom");

Console.WriteLine("Agenda map notification smoke tests passed: 5");

static string FindRepositoryRoot()
{
    DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory);
    while (current != null)
    {
        if (File.Exists(Path.Combine(current.FullName, "VoteDealBehavior.MapNotification.cs")))
        {
            return current.FullName;
        }
        current = current.Parent;
    }
    throw new DirectoryNotFoundException("Repository root was not found.");
}

static string ExtractMethod(string source, string signature)
{
    int start = source.IndexOf(signature, StringComparison.Ordinal);
    if (start < 0) throw new InvalidOperationException("Method not found: " + signature);
    int brace = source.IndexOf('{', start);
    if (brace < 0) throw new InvalidOperationException("Method body not found: " + signature);
    int depth = 0;
    for (int i = brace; i < source.Length; i++)
    {
        if (source[i] == '{') depth++;
        else if (source[i] == '}' && --depth == 0) return source.Substring(start, i - start + 1);
    }
    throw new InvalidOperationException("Unterminated method: " + signature);
}

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
