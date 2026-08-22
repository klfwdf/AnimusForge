namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free current-outcome wording for the GCCZ postprocess context.
/// </summary>
public static class SiegePostprocessOutcomeTextBuilder
{
    public static string Build(SiegePostprocessOutcomeFacts facts)
    {
        return Build(facts, TownPromptTextCatalog.CreateEnglishFallback());
    }

    public static string Build(SiegePostprocessOutcomeFacts facts, TownPromptTextCatalog promptText)
    {
        TownPromptTextCatalog text = TownPromptTextCatalog.Resolve(promptText);
        if (facts == null)
        {
            return text.OutcomeNoDecision;
        }

        if (facts.MassacreStarted)
        {
            return text.OutcomeMassacreActive;
        }

        if (facts.PlunderStarted)
        {
            return text.OutcomePlunderActive;
        }

        if (facts.HasPendingAftermath)
        {
            return text.OutcomePendingTemplate.Replace(
                "{aftermath}",
                (facts.PendingAftermathName ?? string.Empty).Trim());
        }

        return text.OutcomeNoDecision;
    }
}
