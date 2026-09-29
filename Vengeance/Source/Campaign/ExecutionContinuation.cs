using System;
using System.Collections.Generic;
using System.Linq;
using RichExecutions.Core;
using RichExecutions.Diagnostics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace RichExecutions.Campaign;

/// <summary>
/// After one ceremony settles, offers another escorted prisoner in a new
/// town-center mission. The finished ceremony is never reused.
/// </summary>
internal static class ExecutionContinuation
{
    private static ExecutionRequest _template;
    private static bool _choiceOpen;
    private static bool _waitingForMissionExit;
    private static string[] _candidateIds = Array.Empty<string>();
    private static TaleWorlds.CampaignSystem.Campaign _campaign;
    internal static Func<IEnumerable<Hero>> EscortedHeroesProvider { get; set; }

    internal static void Queue(ExecutionRequest request)
    {
        var campaign = TaleWorlds.CampaignSystem.Campaign.Current;
        if (!ReferenceEquals(_campaign, campaign)) _choiceOpen = false;
        _template = request != null &&
                    request.SceneMode == ExecutionSceneMode.Automatic &&
                    Mission.Current != null
            ? request
            : null;
        _waitingForMissionExit = _template != null;
        _campaign = _waitingForMissionExit ? campaign : null;
        _candidateIds = Array.Empty<string>();
        if (!_waitingForMissionExit || EscortedHeroesProvider == null) return;
        try
        {
            _candidateIds = EscortedHeroesProvider()
                .Where(hero => hero != null && hero != request.Victim && !string.IsNullOrWhiteSpace(hero.StringId))
                .Select(hero => hero.StringId)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }
        catch (Exception exception) { RexLog.Warning("Could not capture escorted prisoners for continuation: " + exception.Message); }
    }

    internal static void Tick()
    {
        if (_template is null || _choiceOpen || !_waitingForMissionExit || Mission.Current != null) return;
        if (!ReferenceEquals(_campaign, TaleWorlds.CampaignSystem.Campaign.Current))
        {
            _template = null;
            _candidateIds = Array.Empty<string>();
            _waitingForMissionExit = false;
            return;
        }
        _waitingForMissionExit = false;
        var template = _template;
        _template = null;
        var next = ListCandidates(template);
        _candidateIds = Array.Empty<string>();
        if (next.Count == 0) return;
        ShowChoice(template, next);
    }

    private static List<Hero> ListCandidates(ExecutionRequest template)
    {
        var roster = MobileParty.MainParty?.PrisonRoster;
        if (roster is null) return new List<Hero>();
        var result = new List<Hero>();
        foreach (string id in _candidateIds)
        {
            var hero = MBObjectManager.Instance?.GetObject<Hero>(id);
            if (hero is null || hero == template.Victim || hero.IsDead) continue;
            if (roster.FindIndexOfTroop(hero.CharacterObject) < 0) continue;
            result.Add(hero);
        }

        return result;
    }

    private static void ShowChoice(ExecutionRequest template, List<Hero> candidates)
    {
        var elements = candidates.Select(hero => new InquiryElement(
            hero,
            hero.Name?.ToString() ?? hero.StringId,
            null)).ToList();
        elements.Add(new InquiryElement(null, new TextObject("{=REX_Continue_Leave}Leave the execution site.").ToString(), null));
        _choiceOpen = true;
        MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
            new TextObject("{=REX_Continue_Title}Choose the next prisoner").ToString(),
            new TextObject("{=REX_Continue_Description}The sentence is settled. Choose another escorted prisoner, or leave.").ToString(),
            elements,
            true,
            1,
            1,
            new TextObject("{=REX_Continue_Confirm}Continue").ToString(),
            new TextObject("{=REX_Continue_Cancel}Leave").ToString(),
            selected =>
            {
                _choiceOpen = false;
                var hero = selected?.FirstOrDefault()?.Identifier as Hero;
                if (hero != null) OpenNext(template, hero);
            },
            _ => _choiceOpen = false), pauseGameActiveState: true);
    }

    private static void OpenNext(ExecutionRequest template, Hero hero)
    {
        var behavior = TaleWorlds.CampaignSystem.Campaign.Current?.GetCampaignBehavior<RichExecutionCampaignBehavior>();
        if (behavior is null)
        {
            RexLog.Warning("The next execution could not be opened because the campaign behavior is gone.");
            return;
        }

        behavior.OpenContinuationRequest(template, hero);
    }
}
