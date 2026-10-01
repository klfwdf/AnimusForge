using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RichExecutions.Core;
using RichExecutions.Customization;
using RichExecutions.Diagnostics;
using RichExecutions.Scene;
using RichExecutions.UI;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using BannerlordCampaign = TaleWorlds.CampaignSystem.Campaign;

namespace RichExecutions.Campaign;

public sealed class RichExecutionCampaignBehavior : CampaignBehaviorBase
{
    private const int MaximumHistoryEntries = 100;
    private const int MaximumEvidenceEntries = 500;
    private const int MaximumImpaledCorpseDisplays = 64;

    private sealed class PrisonerChoice
    {
        public PrisonerChoice(Hero hero, PrisonerSource source)
        {
            Hero = hero;
            Source = source;
        }

        public Hero Hero { get; }
        public PrisonerSource Source { get; }
    }

    // Presentation-only recent wording belongs to this campaign instance.
    // It is deliberately not persisted and cannot leak into another loaded campaign.
    internal SpeechRecentHistory SpeechHistory { get; } = new();

    private readonly ExecutionService _service;
    private readonly ExecutionMethodCatalog _methods;
    private readonly ExecutionChargeCatalog _charges;
    private readonly HashSet<string> _recentlyRebelliousTowns = new(StringComparer.Ordinal);

    private Hero? _selectedVictim;
    private PrisonerSource _selectedSource;
    private ExecutionMethodDefinition? _selectedMethod;
    private ExecutionChargeDefinition? _selectedCharge;
    private ExecutionTone _selectedTone;
    private ExecutionSceneMode _selectionSceneMode = ExecutionSceneMode.Automatic;

    private List<string> _historySessionIds = new();
    private List<string> _historyVictimIds = new();
    private List<string> _historyVictimNames = new();
    private List<string> _historySettlementIds = new();
    private List<string> _historyMethodIds = new();
    private List<string> _historyChargeIds = new();
    private List<int> _historyTones = new();
    private List<int> _historyLegitimacyTiers = new();
    private List<int> _historyActors = new();
    private List<float> _historyCampaignDays = new();

    private List<string> _evidenceVictimIds = new();
    private List<string> _evidenceChargeIds = new();
    private List<int> _evidenceStrengths = new();
    private List<string> _evidenceSettlementIds = new();
    private List<float> _evidenceCampaignDays = new();
    private List<string> _evidenceTargetRealmIds = new();

    private List<string> _recentlyRebelliousTownIds = new();

    private List<int> _impaledDisplaySchemaVersions = new();
    private List<string> _impaledDisplayIds = new();
    private List<string> _impaledHeroCharacterIds = new();
    private List<string> _impaledDisplayNames = new();
    private List<string> _impaledBodyProperties = new();
    private List<string> _impaledEquipmentCodes = new();
    private List<int> _impaledIsFemale = new();
    private List<int> _impaledAges = new();
    private List<int> _impaledRaces = new();
    private List<string> _impaledClothingColor1 = new();
    private List<string> _impaledClothingColor2 = new();
    private List<string> _impaledActionNames = new();
    private List<float> _impaledActionProgresses = new();
    private List<string> _impaledCapturedUtc = new();

    public RichExecutionCampaignBehavior(
        ExecutionService service,
        ExecutionMethodCatalog methods,
        ExecutionChargeCatalog charges)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _methods = methods ?? throw new ArgumentNullException(nameof(methods));
        _charges = charges ?? throw new ArgumentNullException(nameof(charges));
        _service.OutcomeCommitted += RecordOutcome;
    }

    // Read-only view of the recorded incident ledger for a hero. The custom
    // execution-site studio shows this list; it never writes to the ledger.
    internal IReadOnlyList<ExecutionCrimeIncidentPresentation> GetRecordedIncidents(Hero? victim)
    {
        if (victim is null)
        {
            return Array.Empty<ExecutionCrimeIncidentPresentation>();
        }

        try
        {
            return BuildCrimeHistory(victim);
        }
        catch (Exception exception)
        {
            RexLog.Warning($"Could not build the recorded incident list: {exception.Message}");
            return Array.Empty<ExecutionCrimeIncidentPresentation>();
        }
    }

    // Candidate prisoners for the in-scene deployment studio, in the same custody
    // order the judgement panel uses.
    internal IReadOnlyList<ExecutionStudioPrisonerOption> GetDeploymentPrisonerOptions(Settlement? settlement)
    {
        if (settlement is null)
        {
            return Array.Empty<ExecutionStudioPrisonerOption>();
        }

        try
        {
            return GetEligiblePrisoners(settlement)
                .Select(choice => new ExecutionStudioPrisonerOption(choice.Hero, choice.Source))
                .ToList();
        }
        catch (Exception exception)
        {
            RexLog.Warning($"Could not list deployment prisoners: {exception.Message}");
            return Array.Empty<ExecutionStudioPrisonerOption>();
        }
    }

    internal EvidenceStrength GetDeploymentEvidenceStrength(Hero victim, ExecutionChargeDefinition charge) =>
        GetEvidenceStrength(victim, charge);

    // Builds the committed request from the studio's final selection. The venue
    // cost and legitimacy are recomputed here, so the provisional request that
    // opened the scene never leaks into the campaign outcome.
    internal ExecutionRequest? BuildDeploymentRequest(
        Hero victim,
        PrisonerSource source,
        ExecutionMethodDefinition method,
        ExecutionChargeDefinition charge,
        ExecutionTone tone,
        Settlement? settlement)
    {
        if (victim is null || method is null || charge is null || settlement is null ||
            !GetVenueEligibility(settlement, out var cost, out _))
        {
            return null;
        }

        var authority = ExecutionService.GetVenueAuthority(settlement);
        var status = ExecutionService.GetVictimPoliticalStatus(victim);
        var evidence = GetEvidenceStrength(victim, charge);
        var isNoble = victim.IsLord;
        var score = ExecutionRuleMath.CalculateLegitimacyScore(new LegitimacyInput(
            authority,
            status,
            evidence,
            tone,
            method.StringId,
            isNoble));

        return new ExecutionRequest(
            victim,
            Hero.MainHero,
            settlement,
            method,
            charge,
            tone,
            evidence,
            ExecutionRuleMath.GetLegitimacyTier(score),
            score,
            cost,
            source,
            authority,
            status,
            isNoble,
            ExecutionSceneMode.CustomPreset);
    }

    public override void RegisterEvents()
    {
        CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        CampaignEvents.RaidCompletedEvent.AddNonSerializedListener(this, OnRaidCompleted);
        CampaignEvents.OnSiegeAftermathAppliedEvent.AddNonSerializedListener(this, OnSiegeAftermathApplied);
        CampaignEvents.OnClanDefectedEvent.AddNonSerializedListener(this, OnClanDefected);
        CampaignEvents.TownRebelliosStateChanged.AddNonSerializedListener(this, OnTownRebelliousStateChanged);
        CampaignEvents.RebellionFinished.AddNonSerializedListener(this, OnRebellionFinished);
        CampaignEvents.TickEvent.AddNonSerializedListener(this, OnCampaignTick);
    }

    public override void SyncData(IDataStore dataStore)
    {
        dataStore.SyncData("_rex_v2_history_session_ids", ref _historySessionIds);
        dataStore.SyncData("_rex_v2_history_victim_ids", ref _historyVictimIds);
        dataStore.SyncData("_rex_v2_history_victim_names", ref _historyVictimNames);
        dataStore.SyncData("_rex_v2_history_settlement_ids", ref _historySettlementIds);
        dataStore.SyncData("_rex_v2_history_method_ids", ref _historyMethodIds);
        dataStore.SyncData("_rex_v2_history_charge_ids", ref _historyChargeIds);
        dataStore.SyncData("_rex_v2_history_tones", ref _historyTones);
        dataStore.SyncData("_rex_v2_history_legitimacy_tiers", ref _historyLegitimacyTiers);
        dataStore.SyncData("_rex_v2_history_actors", ref _historyActors);
        dataStore.SyncData("_rex_v2_history_campaign_days", ref _historyCampaignDays);

        dataStore.SyncData("_rex_v2_evidence_victim_ids", ref _evidenceVictimIds);
        dataStore.SyncData("_rex_v2_evidence_charge_ids", ref _evidenceChargeIds);
        dataStore.SyncData("_rex_v2_evidence_strengths", ref _evidenceStrengths);
        dataStore.SyncData("_rex_v2_evidence_settlement_ids", ref _evidenceSettlementIds);
        dataStore.SyncData("_rex_v2_evidence_campaign_days", ref _evidenceCampaignDays);
        dataStore.SyncData("_rex_v3_evidence_target_realm_ids", ref _evidenceTargetRealmIds);

        dataStore.SyncData("_rex_v4_impaled_schema_versions", ref _impaledDisplaySchemaVersions);
        dataStore.SyncData("_rex_v4_impaled_ids", ref _impaledDisplayIds);
        dataStore.SyncData("_rex_v4_impaled_hero_character_ids", ref _impaledHeroCharacterIds);
        dataStore.SyncData("_rex_v4_impaled_display_names", ref _impaledDisplayNames);
        dataStore.SyncData("_rex_v4_impaled_body_properties", ref _impaledBodyProperties);
        dataStore.SyncData("_rex_v4_impaled_equipment_codes", ref _impaledEquipmentCodes);
        dataStore.SyncData("_rex_v4_impaled_is_female", ref _impaledIsFemale);
        dataStore.SyncData("_rex_v4_impaled_ages", ref _impaledAges);
        dataStore.SyncData("_rex_v4_impaled_races", ref _impaledRaces);
        dataStore.SyncData("_rex_v4_impaled_clothing_color_1", ref _impaledClothingColor1);
        dataStore.SyncData("_rex_v4_impaled_clothing_color_2", ref _impaledClothingColor2);
        dataStore.SyncData("_rex_v4_impaled_action_names", ref _impaledActionNames);
        dataStore.SyncData("_rex_v4_impaled_action_progresses", ref _impaledActionProgresses);
        dataStore.SyncData("_rex_v4_impaled_captured_utc", ref _impaledCapturedUtc);

        if (dataStore.IsSaving)
        {
            // Mirror the in-memory set into the serializable list so a rebellion
            // that is still open when the player saves survives the reload.
            _recentlyRebelliousTownIds = _recentlyRebelliousTowns.ToList();
        }

        // SyncData captures the list reference immediately when saving.
        dataStore.SyncData("_rex_v5_recently_rebellious_town_ids", ref _recentlyRebelliousTownIds);

        if (dataStore.IsLoading)
        {
            NormalizeHistory();
            NormalizeEvidence();
            NormalizeImpaledCorpseDisplays();
            RestoreRecentlyRebelliousTowns();
        }
    }

    private void RestoreRecentlyRebelliousTowns()
    {
        _recentlyRebelliousTownIds ??= new List<string>();
        _recentlyRebelliousTowns.Clear();
        foreach (var id in _recentlyRebelliousTownIds)
        {
            if (!string.IsNullOrWhiteSpace(id))
            {
                _recentlyRebelliousTowns.Add(id);
            }
        }
    }

    internal bool TrySaveImpaledCorpseDisplay(
        ImpaledCorpseDisplaySnapshot snapshot,
        out string saveReference)
    {
        saveReference = string.Empty;
        if (snapshot is null)
        {
            return false;
        }

        NormalizeImpaledCorpseDisplays();
        var id = snapshot.Id.ToString("D");
        for (var index = _impaledDisplayIds.Count - 1; index >= 0; index--)
        {
            if (string.Equals(_impaledDisplayIds[index], id, StringComparison.OrdinalIgnoreCase))
            {
                RemoveImpaledCorpseDisplayAt(index);
            }
        }

        while (_impaledDisplayIds.Count >= MaximumImpaledCorpseDisplays)
        {
            RemoveImpaledCorpseDisplayAt(0);
        }

        _impaledDisplaySchemaVersions.Add(snapshot.SchemaVersion);
        _impaledDisplayIds.Add(id);
        _impaledHeroCharacterIds.Add(snapshot.HeroCharacterId);
        _impaledDisplayNames.Add(snapshot.DisplayName);
        _impaledBodyProperties.Add(snapshot.BodyProperties);
        _impaledEquipmentCodes.Add(snapshot.EquipmentCode);
        _impaledIsFemale.Add(snapshot.IsFemale ? 1 : 0);
        _impaledAges.Add(snapshot.Age);
        _impaledRaces.Add(snapshot.Race);
        _impaledClothingColor1.Add(snapshot.ClothingColor1.ToString(CultureInfo.InvariantCulture));
        _impaledClothingColor2.Add(snapshot.ClothingColor2.ToString(CultureInfo.InvariantCulture));
        _impaledActionNames.Add(snapshot.ActionName);
        _impaledActionProgresses.Add(snapshot.ActionProgress);
        _impaledCapturedUtc.Add(snapshot.CapturedUtc);
        saveReference = $"campaign-save:{id}";
        return true;
    }

    internal IReadOnlyList<ImpaledCorpseDisplaySnapshot> GetImpaledCorpseDisplays()
    {
        NormalizeImpaledCorpseDisplays();
        var result = new List<ImpaledCorpseDisplaySnapshot>(_impaledDisplayIds.Count);
        for (var index = 0; index < _impaledDisplayIds.Count; index++)
        {
            if (!Guid.TryParse(_impaledDisplayIds[index], out var id))
            {
                continue;
            }

            uint.TryParse(
                _impaledClothingColor1[index],
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var clothingColor1);
            uint.TryParse(
                _impaledClothingColor2[index],
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var clothingColor2);
            result.Add(new ImpaledCorpseDisplaySnapshot
            {
                SchemaVersion = _impaledDisplaySchemaVersions[index],
                Id = id,
                HeroCharacterId = _impaledHeroCharacterIds[index],
                DisplayName = _impaledDisplayNames[index],
                BodyProperties = _impaledBodyProperties[index],
                EquipmentCode = _impaledEquipmentCodes[index],
                IsFemale = _impaledIsFemale[index] != 0,
                Age = _impaledAges[index],
                Race = _impaledRaces[index],
                ClothingColor1 = clothingColor1,
                ClothingColor2 = clothingColor2,
                ActionName = _impaledActionNames[index],
                ActionProgress = _impaledActionProgresses[index],
                CapturedUtc = _impaledCapturedUtc[index]
            });
        }

        return result;
    }

    public IReadOnlyList<ExecutionHistoryEntry> GetHistory()
    {
        NormalizeHistory();
        var result = new List<ExecutionHistoryEntry>(_historyVictimIds.Count);
        for (var index = 0; index < _historyVictimIds.Count; index++)
        {
            Guid.TryParse(_historySessionIds[index], out var sessionId);
            result.Add(new ExecutionHistoryEntry(
                sessionId,
                _historyVictimIds[index],
                _historyVictimNames[index],
                _historySettlementIds[index],
                _historyMethodIds[index],
                _historyChargeIds[index],
                (ExecutionTone)_historyTones[index],
                (LegitimacyTier)_historyLegitimacyTiers[index],
                (ExecutionActor)_historyActors[index],
                _historyCampaignDays[index]));
        }

        return result;
    }

    public IReadOnlyList<EvidenceRecord> GetEvidenceLedger()
    {
        NormalizeEvidence();
        var result = new List<EvidenceRecord>(_evidenceVictimIds.Count);
        for (var index = 0; index < _evidenceVictimIds.Count; index++)
        {
            result.Add(new EvidenceRecord(
                _evidenceVictimIds[index],
                _evidenceChargeIds[index],
                (EvidenceStrength)_evidenceStrengths[index],
                _evidenceSettlementIds[index],
                _evidenceCampaignDays[index]));
        }

        return result;
    }

    private void OnSessionLaunched(CampaignGameStarter starter)
    {
        starter.AddGameMenuOption(
            "town",
            "rex_stage_public_execution",
            "{=REX_Menu_Public_Execution}Hold a public trial and execution...",
            PublicExecutionCondition,
            PublicExecutionConsequence,
            false,
            -1,
            false);
        starter.AddGameMenuOption(
            "town",
            "rex_stage_public_execution_custom",
            "{=REX_Menu_Public_Execution_Custom}Hold a public trial and execution (custom site)...",
            PublicExecutionCondition,
            CustomPublicExecutionConsequence,
            false,
            -1,
            false);

        starter.AddDialogLine(
            "rex_executioner_intro",
            "start",
            "rex_executioner_options",
            "{=REX_Executioner_Intro}The prisoner is ready. Give the word and I will carry out the sentence.",
            ExecutionSessionCoordinator.IsConversationWithExecutioner,
            null,
            200);
        starter.AddPlayerLine(
            "rex_executioner_crossbow_personal",
            "rex_executioner_options",
            "close_window",
            "{=REX_Executioner_Crossbow_Personal}Stand aside. I will fire the execution bolt myself.",
            ExecutionSessionCoordinator.IsCrossbowConversationWithExecutioner,
            ExecutionSessionCoordinator.RequestPlayerStart,
            250);
        starter.AddPlayerLine(
            "rex_executioner_proceed",
            "rex_executioner_options",
            "close_window",
            "{=REX_Executioner_Proceed}Proceed with the execution.",
            null,
            ExecutionSessionCoordinator.RequestExecutionerStart,
            200);
        starter.AddPlayerLine(
            "rex_executioner_wait",
            "rex_executioner_options",
            "close_window",
            "{=REX_Executioner_Wait}Wait. I will give the word later.",
            null,
            null,
            100);
    }

    private static void OnCampaignTick(float deltaTime)
    {
        _ = deltaTime;
        ExecutionJudgementPanel.Tick();
        ExecutionContinuation.Tick();
    }

    private bool PublicExecutionCondition(MenuCallbackArgs args)
    {
        var settlement = Settlement.CurrentSettlement;
        if (settlement is null || !settlement.IsTown)
        {
            return false;
        }

        args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
        var eligibility = GetVenueEligibility(settlement, out _, out var reason);
        if (!eligibility)
        {
            Disable(args, reason);
        }
        else if (GetEligiblePrisoners(settlement).Count == 0)
        {
            Disable(
                args,
                new TextObject("{=REX_Tooltip_No_Prisoners}No eligible adult hero prisoner is available in the permitted custody."));
        }

        return true;
    }

    private void PublicExecutionConsequence(MenuCallbackArgs args)
    {
        BeginPublicExecutionSelection(ExecutionSceneMode.Automatic);
    }

    private void CustomPublicExecutionConsequence(MenuCallbackArgs args)
    {
        BeginCustomSiteDeployment();
    }

    // The custom site picks its prisoner, punishment and charge inside the
    // deployment studio. The scene therefore opens on a provisional request that
    // is never committed: the studio rebuilds it before the session begins, so a
    // cancelled deployment spends nothing and kills nobody.
    private void BeginCustomSiteDeployment()
    {
        ResetSelection();
        _selectionSceneMode = ExecutionSceneMode.CustomPreset;
        var settlement = Settlement.CurrentSettlement;
        if (settlement is null)
        {
            ShowMessage(
                new TextObject("{=REX_Title_Unavailable}Execution unavailable"),
                new TextObject("{=REX_Error_Venue_Unavailable}This town cannot host an execution right now."));
            return;
        }

        if (!GetVenueEligibility(settlement, out _, out var reason))
        {
            ShowMessage(new TextObject("{=REX_Title_Unavailable}Execution unavailable"), reason);
            return;
        }

        var prisoners = GetEligiblePrisoners(settlement);
        if (prisoners.Count == 0)
        {
            ShowMessage(
                new TextObject("{=REX_Title_Unavailable}Execution unavailable"),
                new TextObject("{=REX_Tooltip_No_Prisoners}No eligible adult hero prisoner is available in the permitted custody."));
            return;
        }

        var provisional = prisoners[0];
        _selectedVictim = provisional.Hero;
        _selectedSource = provisional.Source;
        _selectedMethod = _methods.All.FirstOrDefault(method => ExecutionMethodRules.IsVisibleInSelection(method.StringId));
        _selectedCharge = _charges.All.FirstOrDefault();
        _selectedTone = ExecutionTone.Judicial;

        if (_selectedMethod is null || _selectedCharge is null)
        {
            ShowMessage(
                new TextObject("{=REX_Title_Unavailable}Execution unavailable"),
                new TextObject("{=REX_Error_Invalid_Request}The execution request is incomplete."));
            ResetSelection();
            return;
        }

        var request = BuildRequest();
        if (request is null)
        {
            ShowMessage(
                new TextObject("{=REX_Title_Unavailable}Execution unavailable"),
                new TextObject("{=REX_Error_Invalid_Request}The execution request is incomplete."));
            ResetSelection();
            return;
        }

        OpenExecutionMission(request);
    }

    private void BeginPublicExecutionSelection(ExecutionSceneMode sceneMode)
    {
        ResetSelection();
        _selectionSceneMode = sceneMode;
        var settlement = Settlement.CurrentSettlement;
        if (settlement is null)
        {
            ShowMessage(
                new TextObject("{=REX_Title_Unavailable}Execution unavailable"),
                new TextObject("{=REX_Error_Venue_Unavailable}This town cannot host an execution right now."));
            return;
        }

        if (!GetVenueEligibility(settlement, out var influenceCost, out var reason))
        {
            ShowMessage(new TextObject("{=REX_Title_Unavailable}Execution unavailable"), reason);
            return;
        }

        var prisoners = GetEligiblePrisoners(settlement);
        if (prisoners.Count == 0)
        {
            ShowMessage(
                new TextObject("{=REX_Title_Unavailable}Execution unavailable"),
                new TextObject("{=REX_Tooltip_No_Prisoners}No eligible adult hero prisoner is available in the permitted custody."));
            return;
        }

        var options = prisoners
            .Select(choice => new ExecutionPrisonerOption(choice.Hero, choice.Source))
            .ToList();
        if (!ExecutionJudgementPanel.Show(
                settlement,
                options,
                _methods.All,
                _charges.All,
                BuildEvidencePresentation,
                BuildCrimeHistory,
                influenceCost,
                OnJudgementAccepted,
                ResetSelection))
        {
            ShowMessage(
                new TextObject("{=REX_Title_Unavailable}Execution unavailable"),
                new TextObject("{=REX_Error_UI_Open}The public court interface could not be opened safely."));
            ResetSelection();
        }
    }

    private void OnJudgementAccepted(ExecutionJudgementSelection selection)
    {
        _selectedVictim = selection.Victim;
        _selectedSource = selection.Source;
        _selectedMethod = selection.Method;
        _selectedCharge = selection.Charge;
        _selectedTone = selection.Tone;

        var request = BuildRequest();
        if (request is null)
        {
            ShowMessage(
                new TextObject("{=REX_Title_Unavailable}Execution unavailable"),
                new TextObject("{=REX_Error_Invalid_Request}The execution request is incomplete."));
            ResetSelection();
            return;
        }

        var validation = _service.Validate(request, ExecutionValidationStage.Confirmation);
        if (!validation.IsValid)
        {
            ShowMessage(new TextObject("{=REX_Title_Unavailable}Execution unavailable"), validation.Message);
            ResetSelection();
            return;
        }

        OpenExecutionMission(request);
    }

    private ExecutionRequest? BuildRequest()
    {
        var settlement = Settlement.CurrentSettlement;
        if (_selectedVictim is null || _selectedMethod is null || _selectedCharge is null ||
            settlement is null || !GetVenueEligibility(settlement, out var cost, out _))
        {
            return null;
        }

        var authority = ExecutionService.GetVenueAuthority(settlement);
        var status = ExecutionService.GetVictimPoliticalStatus(_selectedVictim);
        var evidence = GetEvidenceStrength(_selectedVictim, _selectedCharge);
        var isNoble = _selectedVictim.IsLord;
        var legitimacyInput = new LegitimacyInput(
            authority,
            status,
            evidence,
            _selectedTone,
            _selectedMethod.StringId,
            isNoble);
        var score = ExecutionRuleMath.CalculateLegitimacyScore(legitimacyInput);
        return new ExecutionRequest(
            _selectedVictim,
            Hero.MainHero,
            settlement,
            _selectedMethod,
            _selectedCharge,
            _selectedTone,
            evidence,
            ExecutionRuleMath.GetLegitimacyTier(score),
            score,
            cost,
            _selectedSource,
            authority,
            status,
            isNoble,
            _selectionSceneMode);
    }

    internal void OpenContinuationRequest(ExecutionRequest previous, Hero victim)
    {
        ResetSelection();
        if (previous == null || victim == null || previous.Venue != Settlement.CurrentSettlement)
        {
            ShowMessage(new TextObject("{=REX_Title_Unavailable}Execution unavailable"),
                new TextObject("{=REX_Error_Venue_Unavailable}This town cannot host an execution right now."));
            return;
        }
        _selectedVictim = victim;
        _selectedSource = PrisonerSource.PlayerParty;
        _selectedMethod = previous.Method;
        _selectedCharge = previous.Charge;
        _selectedTone = previous.Tone;
        _selectionSceneMode = ExecutionSceneMode.Automatic;
        var request = BuildRequest();
        if (request is null)
        {
            ShowMessage(new TextObject("{=REX_Title_Unavailable}Execution unavailable"),
                new TextObject("{=REX_Error_Invalid_Request}The execution request is incomplete."));
            ResetSelection();
            return;
        }
        OpenExecutionMission(request);
    }

    private void OpenExecutionMission(ExecutionRequest request)
    {
        RexLog.Info(
            $"Opening the town-center mission for session {request.SessionId} " +
            $"at venue '{request.Venue.StringId}' in {request.SceneMode} mode.");
        // CustomPreset opens a mission only as an uncommitted builder host.
        // Do not add the provisional request to ExecutionService._active:
        // the real request is created and begun only after the player finishes
        // the in-scene deployment selection.
        if (request.SceneMode != ExecutionSceneMode.CustomPreset)
        {
            var begin = _service.Begin(request);
            if (!begin.IsValid)
            {
                ShowMessage(new TextObject("{=REX_Result_Failed_Title}Execution cancelled"), begin.Message);
                ResetSelection();
                return;
            }
        }
        else
        {
            RexLog.Info(
                $"Opening provisional custom-site builder session {request.SessionId}; " +
                "campaign execution service remains inactive until deployment is confirmed.");
        }

        if (!ExecutionSessionCoordinator.Prepare(request, _service))
        {
            var message = new TextObject("{=REX_Error_Another_Session}Another public execution session is already active.");
            _service.Cancel(request, ExecutionFailureReason.SessionNotActive, message);
            ShowMessage(new TextObject("{=REX_Result_Failed_Title}Execution cancelled"), message);
            ResetSelection();
            return;
        }

        try
        {
            var location = request.Venue.LocationComplex?.GetLocationWithId("center");
            var encounter = PlayerEncounter.LocationEncounter;
            if (location is null || encounter is null)
            {
                throw new InvalidOperationException("The current town center location encounter is unavailable.");
            }

            encounter.CreateAndOpenMissionController(location, null, null, null);
            RexLog.Info($"Town-center mission open was requested for session {request.SessionId}.");
            ResetSelection();
        }
        catch (Exception exception)
        {
            RexLog.Error("Could not open the town-center execution mission.", exception);
            var message = new TextObject("{=REX_Error_Mission_Open}The town center could not be opened safely. Nothing was spent and the prisoner lives.");
            ExecutionSessionCoordinator.CancelPending(ExecutionFailureReason.ScenePlacementFailed, message);
            ShowMessage(new TextObject("{=REX_Result_Failed_Title}Execution cancelled"), message);
            ResetSelection();
        }
    }

    private bool GetVenueEligibility(Settlement settlement, out int cost, out TextObject reason)
    {
        cost = 0;
        reason = TextObject.GetEmpty();
        if (Hero.MainHero.IsPrisoner)
        {
            reason = new TextObject("{=REX_Tooltip_Player_Prisoner}You cannot organize a sentence while you are a prisoner.");
            return false;
        }

        if (BannerlordCampaign.Current.IsMainHeroDisguised)
        {
            reason = new TextObject("{=REX_Tooltip_Disguised}You cannot organize a public sentence while disguised.");
            return false;
        }

        if (settlement.IsUnderSiege)
        {
            reason = new TextObject("{=REX_Tooltip_Under_Siege}Public executions are suspended while the town is under siege.");
            return false;
        }

        var authority = ExecutionService.GetVenueAuthority(settlement);
        if (authority == VenueAuthority.None)
        {
            reason = new TextObject("{=REX_Tooltip_No_Jurisdiction}You may only hold a public execution in your own town or a town of your current kingdom.");
            return false;
        }

        var playerIsRuler = Clan.PlayerClan.Kingdom?.RulingClan == Clan.PlayerClan;
        cost = ExecutionRuleMath.GetInfluenceCost(authority, playerIsRuler);
        var owner = settlement.OwnerClan?.Leader;
        if (authority == VenueAuthority.AlliedTown && !playerIsRuler && owner is not null &&
            Hero.MainHero.GetRelation(owner) < 0)
        {
            reason = new TextObject("{=REX_Tooltip_Relation_Low}The town's lord refuses to grant you the square because your relation is below zero.");
            return false;
        }

        if (cost > 0 && Clan.PlayerClan.Influence + 0.001f < cost)
        {
            var text = new TextObject("{=REX_Tooltip_Influence_Low_Detail}This ceremony requires {COST} influence in an allied town.");
            text.SetTextVariable("COST", cost);
            reason = text;
            return false;
        }

        return true;
    }

    private static List<PrisonerChoice> GetEligiblePrisoners(Settlement settlement)
    {
        var result = new Dictionary<string, PrisonerChoice>(StringComparer.Ordinal);
        AddPrisonersFromRoster(
            result,
            MobileParty.MainParty?.PrisonRoster,
            MobileParty.MainParty?.Party,
            PrisonerSource.PlayerParty);

        if (settlement.OwnerClan == Clan.PlayerClan)
        {
            AddPrisonersFromRoster(
                result,
                settlement.Party?.PrisonRoster,
                settlement.Party,
                PrisonerSource.TownDungeon);
        }

        return result.Values
            .OrderBy(choice => choice.Hero.Name.ToString(), StringComparer.CurrentCulture)
            .ToList();
    }

    private static void AddPrisonersFromRoster(
        IDictionary<string, PrisonerChoice> result,
        TroopRoster? roster,
        PartyBase? expectedParty,
        PrisonerSource source)
    {
        if (roster is null || expectedParty is null)
        {
            return;
        }

        foreach (TroopRosterElement element in roster.GetTroopRoster())
        {
            var character = element.Character;
            if (element.Number <= 0 || character is null || !character.IsHero)
            {
                continue;
            }

            var hero = character.HeroObject;
            if (hero is null || !hero.IsAlive || hero.IsChild ||
                hero.PartyBelongedToAsPrisoner != expectedParty ||
                !hero.CanDie(KillCharacterAction.KillCharacterActionDetail.Executed))
            {
                continue;
            }

            result[hero.StringId] = new PrisonerChoice(hero, source);
        }
    }

    private EvidenceStrength GetEvidenceStrength(Hero victim, ExecutionChargeDefinition charge)
    {
        NormalizeEvidence();
        var matches = new List<EvidenceMatch>(_evidenceVictimIds.Count);
        for (var index = 0; index < _evidenceVictimIds.Count; index++)
        {
            matches.Add(new EvidenceMatch(
                _evidenceVictimIds[index],
                _evidenceChargeIds[index],
                (EvidenceStrength)_evidenceStrengths[index]));
        }

        var strength = ExecutionRuleMath.FindStrongestEvidence(
            matches,
            victim.StringId,
            charge.StringId);

        if (charge.StringId == "banditry" && victim.Clan?.IsBanditFaction == true)
        {
            strength = EvidenceStrength.Strong;
        }
        else if (charge.StringId == "treason" && victim.Clan?.IsRebelClan == true)
        {
            strength = EvidenceStrength.Strong;
        }
        else if (charge.StringId == "public_enemy" &&
                 ExecutionService.GetVictimPoliticalStatus(victim) == VictimPoliticalStatus.WarEnemy &&
                 strength < EvidenceStrength.Circumstantial)
        {
            strength = EvidenceStrength.Circumstantial;
        }

        return strength;
    }

    private ExecutionEvidencePresentation BuildEvidencePresentation(
        Hero victim,
        ExecutionChargeDefinition charge)
    {
        NormalizeEvidence();
        var strength = GetEvidenceStrength(victim, charge);
        var recordIndex = -1;
        for (var index = 0; index < _evidenceVictimIds.Count; index++)
        {
            if (_evidenceVictimIds[index] == victim.StringId &&
                string.Equals(_evidenceChargeIds[index], charge.StringId, StringComparison.OrdinalIgnoreCase))
            {
                if (recordIndex < 0 ||
                    _evidenceStrengths[index] > _evidenceStrengths[recordIndex] ||
                    (_evidenceStrengths[index] == _evidenceStrengths[recordIndex] &&
                     _evidenceCampaignDays[index] > _evidenceCampaignDays[recordIndex]))
                {
                    recordIndex = index;
                }
            }
        }

        var settlement = recordIndex >= 0 && !string.IsNullOrWhiteSpace(_evidenceSettlementIds[recordIndex])
            ? Settlement.Find(_evidenceSettlementIds[recordIndex])
            : null;
        var targetRealmId = recordIndex >= 0 ? _evidenceTargetRealmIds[recordIndex] : string.Empty;
        var concernsPlayerRealm = IsPlayerRealm(targetRealmId, settlement);
        var recordText = BuildCrimeRecordText(
            victim,
            charge,
            strength,
            settlement,
            concernsPlayerRealm,
            recordIndex >= 0);

        TextObject detail;
        if (recordIndex >= 0)
        {
            detail = BuildCrimeRecordDetail(strength, _evidenceCampaignDays[recordIndex]);
        }
        else if (strength != EvidenceStrength.None)
        {
            detail = new TextObject("{=REX_UI_Crime_Derived_Detail}{EVIDENCE} · judged from current identity and political status");
            detail.SetTextVariable("EVIDENCE", GetEvidenceName(strength));
        }
        else
        {
            detail = new TextObject("{=REX_UI_Crime_Unsupported_Detail}{EVIDENCE} · alleging this charge counts as fabrication");
            detail.SetTextVariable("EVIDENCE", GetEvidenceName(strength));
        }

        return new ExecutionEvidencePresentation(strength, recordText.ToString(), detail.ToString());
    }

    private IReadOnlyList<ExecutionCrimeIncidentPresentation> BuildCrimeHistory(Hero victim)
    {
        NormalizeEvidence();
        var result = new List<ExecutionCrimeIncidentPresentation>();
        for (var index = 0; index < _evidenceVictimIds.Count; index++)
        {
            if (_evidenceVictimIds[index] != victim.StringId ||
                !_charges.TryGet(_evidenceChargeIds[index], out var charge))
            {
                continue;
            }

            var settlement = !string.IsNullOrWhiteSpace(_evidenceSettlementIds[index])
                ? Settlement.Find(_evidenceSettlementIds[index])
                : null;
            var strength = (EvidenceStrength)_evidenceStrengths[index];
            var concernsPlayerRealm = IsPlayerRealm(_evidenceTargetRealmIds[index], settlement);
            var recordText = BuildCrimeRecordText(
                victim,
                charge,
                strength,
                settlement,
                concernsPlayerRealm,
                hasLedgerRecord: true);
            var detail = BuildCrimeRecordDetail(strength, _evidenceCampaignDays[index]);
            result.Add(new ExecutionCrimeIncidentPresentation(
                charge.StringId,
                charge.GetName().ToString(),
                recordText.ToString(),
                detail.ToString(),
                strength,
                _evidenceCampaignDays[index]));
        }

        return result
            .OrderByDescending(item => item.CampaignDay)
            .ThenByDescending(item => (int)item.Strength)
            .ToList();
    }

    private static TextObject BuildCrimeRecordDetail(EvidenceStrength strength, float campaignDay)
    {
        var detail = new TextObject("{=REX_UI_Crime_Record_Detail}{EVIDENCE} · recorded on campaign day {DAY}");
        detail.SetTextVariable("EVIDENCE", GetEvidenceName(strength));
        detail.SetTextVariable("DAY", Math.Max(0, (int)Math.Floor(campaignDay)));
        return detail;
    }

    private static TextObject BuildCrimeRecordText(
        Hero victim,
        ExecutionChargeDefinition charge,
        EvidenceStrength strength,
        Settlement? settlement,
        bool concernsPlayerRealm,
        bool hasLedgerRecord)
    {
        var place = settlement?.Name ?? new TextObject("{=REX_UI_Unknown_Place}an unknown settlement");
        TextObject text;
        switch (charge.StringId)
        {
            case "raiding_civilians" when hasLedgerRecord && concernsPlayerRealm:
                text = new TextObject("{=REX_UI_Crime_Raid_Ours}Raided our village {PLACE}");
                text.SetTextVariable("PLACE", place);
                return text;
            case "raiding_civilians" when hasLedgerRecord:
                text = new TextObject("{=REX_UI_Crime_Raid}Raided the village {PLACE}");
                text.SetTextVariable("PLACE", place);
                return text;
            case "siege_atrocity" when hasLedgerRecord && concernsPlayerRealm:
                text = new TextObject("{=REX_UI_Crime_Siege_Ours}Committed atrocities after taking our settlement {PLACE}");
                text.SetTextVariable("PLACE", place);
                return text;
            case "siege_atrocity" when hasLedgerRecord:
                text = new TextObject("{=REX_UI_Crime_Siege}Committed siege atrocities at {PLACE}");
                text.SetTextVariable("PLACE", place);
                return text;
            case "treason" when hasLedgerRecord && settlement is not null:
                text = new TextObject("{=REX_UI_Crime_Rebellion}Took part in the rebellion at {PLACE}");
                text.SetTextVariable("PLACE", place);
                return text;
            case "treason" when hasLedgerRecord:
                return new TextObject("{=REX_UI_Crime_Defection}Broke sworn allegiance and defected");
            case "treason" when victim.Clan?.IsRebelClan == true:
                return new TextObject("{=REX_UI_Crime_Rebel_Status}Leads or belongs to a rebel clan");
            case "banditry" when victim.Clan?.IsBanditFaction == true:
                return new TextObject("{=REX_UI_Crime_Bandit_Status}Lives by banditry and outlaw violence");
            case "public_enemy" when strength != EvidenceStrength.None:
                return new TextObject("{=REX_UI_Crime_Public_Enemy}Is presently at war with our realm");
            case "personal_revenge":
                return new TextObject("{=REX_UI_Crime_Personal_Revenge}Private grievance without a public criminal record");
            default:
                text = new TextObject("{=REX_UI_Crime_No_Record_For_Charge}No verified record for {CHARGE}");
                text.SetTextVariable("CHARGE", charge.GetName());
                return text;
        }
    }

    private static bool IsPlayerRealm(string targetRealmId, Settlement? settlement)
    {
        var playerRealmId = Clan.PlayerClan.Kingdom?.StringId ?? Clan.PlayerClan.StringId;
        if (!string.IsNullOrWhiteSpace(targetRealmId))
        {
            return string.Equals(targetRealmId, playerRealmId, StringComparison.Ordinal);
        }

        return settlement?.OwnerClan == Clan.PlayerClan ||
               (Clan.PlayerClan.Kingdom is not null && settlement?.MapFaction == Clan.PlayerClan.Kingdom);
    }

    private void OnRaidCompleted(BattleSideEnum winnerSide, RaidEventComponent raidEvent)
    {
        if (winnerSide != BattleSideEnum.Attacker || raidEvent is null)
        {
            return;
        }

        var settlement = raidEvent.MapEventSettlement;
        var leader = raidEvent.AttackerSide.LeaderParty?.LeaderHero;
        if (leader is not null)
        {
            AddEvidence(leader, "raiding_civilians", EvidenceStrength.Strong, settlement);
        }

        foreach (var party in raidEvent.AttackerSide.Parties)
        {
            var participant = party.Party?.LeaderHero;
            if (participant is not null && participant != leader)
            {
                AddEvidence(participant, "raiding_civilians", EvidenceStrength.Circumstantial, settlement);
            }
        }
    }

    private void OnSiegeAftermathApplied(
        MobileParty attackerParty,
        Settlement settlement,
        SiegeAftermathAction.SiegeAftermath aftermath,
        Clan previousOwner,
        Dictionary<MobileParty, float> contributions)
    {
        if (aftermath == SiegeAftermathAction.SiegeAftermath.ShowMercy)
        {
            return;
        }

        var leader = attackerParty?.LeaderHero;
        if (leader is not null)
        {
            AddEvidence(leader, "siege_atrocity", EvidenceStrength.Strong, settlement);
        }

        if (contributions is null)
        {
            return;
        }

        foreach (var contribution in contributions)
        {
            var participant = contribution.Key?.LeaderHero;
            if (participant is null || participant == leader || contribution.Value <= 0f)
            {
                continue;
            }

            AddEvidence(
                participant,
                "siege_atrocity",
                contribution.Value >= 0.25f ? EvidenceStrength.Strong : EvidenceStrength.Circumstantial,
                settlement);
        }
    }

    private void OnClanDefected(Clan clan, Kingdom oldKingdom, Kingdom newKingdom)
    {
        if (clan is null || oldKingdom is null || oldKingdom == newKingdom)
        {
            return;
        }

        if (clan.Leader is not null)
        {
            AddEvidence(clan.Leader, "treason", EvidenceStrength.Strong, null, oldKingdom.StringId);
        }

        foreach (var hero in clan.Heroes)
        {
            if (hero.IsAlive && hero.IsLord && hero != clan.Leader)
            {
                AddEvidence(hero, "treason", EvidenceStrength.Circumstantial, null, oldKingdom.StringId);
            }
        }
    }

    private void OnTownRebelliousStateChanged(Town town, bool isRebellious)
    {
        if (town?.Settlement is null)
        {
            return;
        }

        if (isRebellious)
        {
            _recentlyRebelliousTowns.Add(town.Settlement.StringId);
        }
        else
        {
            _recentlyRebelliousTowns.Remove(town.Settlement.StringId);
        }
    }

    private void OnRebellionFinished(Settlement settlement, Clan oldOwnerClan)
    {
        if (settlement is null)
        {
            return;
        }

        var rebelClan = settlement.OwnerClan;
        if (rebelClan?.IsRebelClan != true && !_recentlyRebelliousTowns.Contains(settlement.StringId))
        {
            return;
        }

        var rebelLeader = rebelClan?.Leader;
        foreach (var hero in rebelClan?.Heroes ?? Enumerable.Empty<Hero>())
        {
            if (hero.IsAlive && hero.IsLord)
            {
                AddEvidence(
                    hero,
                    "treason",
                    hero == rebelLeader ? EvidenceStrength.Strong : EvidenceStrength.Circumstantial,
                    settlement,
                    oldOwnerClan?.Kingdom?.StringId ?? oldOwnerClan?.StringId);
            }
        }

        _recentlyRebelliousTowns.Remove(settlement.StringId);
    }

    private void AddEvidence(
        Hero hero,
        string chargeId,
        EvidenceStrength strength,
        Settlement? settlement,
        string? targetRealmId = null)
    {
        NormalizeEvidence();
        var resolvedTargetRealmId = targetRealmId ??
                                    settlement?.MapFaction?.StringId ??
                                    settlement?.OwnerClan?.StringId ??
                                    string.Empty;

        if (_evidenceVictimIds.Count >= MaximumEvidenceEntries)
        {
            RemoveEvidenceAt(0);
        }

        _evidenceVictimIds.Add(hero.StringId);
        _evidenceChargeIds.Add(chargeId);
        _evidenceStrengths.Add((int)strength);
        _evidenceSettlementIds.Add(settlement?.StringId ?? string.Empty);
        _evidenceCampaignDays.Add((float)CampaignTime.Now.ToDays);
        _evidenceTargetRealmIds.Add(resolvedTargetRealmId);
    }

    private void RecordOutcome(ExecutionOutcome outcome)
    {
        if (!outcome.Success || !outcome.DeathCommitted)
        {
            return;
        }

        NormalizeHistory();
        if (_historyVictimIds.Count >= MaximumHistoryEntries)
        {
            RemoveHistoryAt(0);
        }

        var request = outcome.Request;
        _historySessionIds.Add(request.SessionId.ToString("D"));
        _historyVictimIds.Add(request.Victim.StringId);
        _historyVictimNames.Add(request.Victim.Name.ToString());
        _historySettlementIds.Add(request.Venue.StringId);
        _historyMethodIds.Add(request.Method.StringId);
        _historyChargeIds.Add(request.Charge.StringId);
        _historyTones.Add((int)request.Tone);
        _historyLegitimacyTiers.Add((int)request.LegitimacyTier);
        _historyActors.Add((int)outcome.Actor);
        _historyCampaignDays.Add((float)CampaignTime.Now.ToDays);
    }

    private void NormalizeHistory()
    {
        _historySessionIds ??= new List<string>();
        _historyVictimIds ??= new List<string>();
        _historyVictimNames ??= new List<string>();
        _historySettlementIds ??= new List<string>();
        _historyMethodIds ??= new List<string>();
        _historyChargeIds ??= new List<string>();
        _historyTones ??= new List<int>();
        _historyLegitimacyTiers ??= new List<int>();
        _historyActors ??= new List<int>();
        _historyCampaignDays ??= new List<float>();

        var count = new[]
        {
            _historySessionIds.Count,
            _historyVictimIds.Count,
            _historyVictimNames.Count,
            _historySettlementIds.Count,
            _historyMethodIds.Count,
            _historyChargeIds.Count,
            _historyTones.Count,
            _historyLegitimacyTiers.Count,
            _historyActors.Count,
            _historyCampaignDays.Count
        }.Min();

        TrimTo(_historySessionIds, count);
        TrimTo(_historyVictimIds, count);
        TrimTo(_historyVictimNames, count);
        TrimTo(_historySettlementIds, count);
        TrimTo(_historyMethodIds, count);
        TrimTo(_historyChargeIds, count);
        TrimTo(_historyTones, count);
        TrimTo(_historyLegitimacyTiers, count);
        TrimTo(_historyActors, count);
        TrimTo(_historyCampaignDays, count);
        while (_historyVictimIds.Count > MaximumHistoryEntries)
        {
            RemoveHistoryAt(0);
        }
    }

    private void NormalizeEvidence()
    {
        _evidenceVictimIds ??= new List<string>();
        _evidenceChargeIds ??= new List<string>();
        _evidenceStrengths ??= new List<int>();
        _evidenceSettlementIds ??= new List<string>();
        _evidenceCampaignDays ??= new List<float>();
        _evidenceTargetRealmIds ??= new List<string>();

        var count = new[]
        {
            _evidenceVictimIds.Count,
            _evidenceChargeIds.Count,
            _evidenceStrengths.Count,
            _evidenceSettlementIds.Count,
            _evidenceCampaignDays.Count
        }.Min();
        TrimTo(_evidenceVictimIds, count);
        TrimTo(_evidenceChargeIds, count);
        TrimTo(_evidenceStrengths, count);
        TrimTo(_evidenceSettlementIds, count);
        TrimTo(_evidenceCampaignDays, count);
        TrimTo(_evidenceTargetRealmIds, count);
        while (_evidenceTargetRealmIds.Count < count)
        {
            _evidenceTargetRealmIds.Add(string.Empty);
        }
        while (_evidenceVictimIds.Count > MaximumEvidenceEntries)
        {
            RemoveEvidenceAt(0);
        }
    }

    private void NormalizeImpaledCorpseDisplays()
    {
        _impaledDisplaySchemaVersions ??= new List<int>();
        _impaledDisplayIds ??= new List<string>();
        _impaledHeroCharacterIds ??= new List<string>();
        _impaledDisplayNames ??= new List<string>();
        _impaledBodyProperties ??= new List<string>();
        _impaledEquipmentCodes ??= new List<string>();
        _impaledIsFemale ??= new List<int>();
        _impaledAges ??= new List<int>();
        _impaledRaces ??= new List<int>();
        _impaledClothingColor1 ??= new List<string>();
        _impaledClothingColor2 ??= new List<string>();
        _impaledActionNames ??= new List<string>();
        _impaledActionProgresses ??= new List<float>();
        _impaledCapturedUtc ??= new List<string>();

        var count = new[]
        {
            _impaledDisplaySchemaVersions.Count,
            _impaledDisplayIds.Count,
            _impaledHeroCharacterIds.Count,
            _impaledDisplayNames.Count,
            _impaledBodyProperties.Count,
            _impaledEquipmentCodes.Count,
            _impaledIsFemale.Count,
            _impaledAges.Count,
            _impaledRaces.Count,
            _impaledClothingColor1.Count,
            _impaledClothingColor2.Count,
            _impaledActionNames.Count,
            _impaledActionProgresses.Count,
            _impaledCapturedUtc.Count
        }.Min();

        TrimTo(_impaledDisplaySchemaVersions, count);
        TrimTo(_impaledDisplayIds, count);
        TrimTo(_impaledHeroCharacterIds, count);
        TrimTo(_impaledDisplayNames, count);
        TrimTo(_impaledBodyProperties, count);
        TrimTo(_impaledEquipmentCodes, count);
        TrimTo(_impaledIsFemale, count);
        TrimTo(_impaledAges, count);
        TrimTo(_impaledRaces, count);
        TrimTo(_impaledClothingColor1, count);
        TrimTo(_impaledClothingColor2, count);
        TrimTo(_impaledActionNames, count);
        TrimTo(_impaledActionProgresses, count);
        TrimTo(_impaledCapturedUtc, count);
        while (_impaledDisplayIds.Count > MaximumImpaledCorpseDisplays)
        {
            RemoveImpaledCorpseDisplayAt(0);
        }
    }

    private static void TrimTo<T>(List<T> list, int count)
    {
        if (list.Count > count)
        {
            list.RemoveRange(count, list.Count - count);
        }
    }

    private void RemoveHistoryAt(int index)
    {
        _historySessionIds.RemoveAt(index);
        _historyVictimIds.RemoveAt(index);
        _historyVictimNames.RemoveAt(index);
        _historySettlementIds.RemoveAt(index);
        _historyMethodIds.RemoveAt(index);
        _historyChargeIds.RemoveAt(index);
        _historyTones.RemoveAt(index);
        _historyLegitimacyTiers.RemoveAt(index);
        _historyActors.RemoveAt(index);
        _historyCampaignDays.RemoveAt(index);
    }

    private void RemoveEvidenceAt(int index)
    {
        _evidenceVictimIds.RemoveAt(index);
        _evidenceChargeIds.RemoveAt(index);
        _evidenceStrengths.RemoveAt(index);
        _evidenceSettlementIds.RemoveAt(index);
        _evidenceCampaignDays.RemoveAt(index);
        _evidenceTargetRealmIds.RemoveAt(index);
    }

    private void RemoveImpaledCorpseDisplayAt(int index)
    {
        _impaledDisplaySchemaVersions.RemoveAt(index);
        _impaledDisplayIds.RemoveAt(index);
        _impaledHeroCharacterIds.RemoveAt(index);
        _impaledDisplayNames.RemoveAt(index);
        _impaledBodyProperties.RemoveAt(index);
        _impaledEquipmentCodes.RemoveAt(index);
        _impaledIsFemale.RemoveAt(index);
        _impaledAges.RemoveAt(index);
        _impaledRaces.RemoveAt(index);
        _impaledClothingColor1.RemoveAt(index);
        _impaledClothingColor2.RemoveAt(index);
        _impaledActionNames.RemoveAt(index);
        _impaledActionProgresses.RemoveAt(index);
        _impaledCapturedUtc.RemoveAt(index);
    }

    private static void Disable(MenuCallbackArgs args, TextObject text)
    {
        args.IsEnabled = false;
        args.Tooltip = text;
    }

    private static void ShowMessage(TextObject title, TextObject body)
    {
        InformationManager.ShowInquiry(
            new InquiryData(
                title.ToString(),
                body.ToString(),
                true,
                false,
                new TextObject("{=REX_Button_OK}OK").ToString(),
                string.Empty,
                () => { },
                null),
            pauseGameActiveState: true,
            prioritize: false);
    }

    private static TextObject GetEvidenceName(EvidenceStrength evidence) => evidence switch
    {
        EvidenceStrength.Strong => new TextObject("{=REX_Evidence_Strong}strong evidence"),
        EvidenceStrength.Circumstantial => new TextObject("{=REX_Evidence_Circumstantial}circumstantial evidence"),
        _ => new TextObject("{=REX_Evidence_None}no evidence")
    };

    private void ResetSelection()
    {
        _selectedVictim = null;
        _selectedSource = PrisonerSource.PlayerParty;
        _selectedMethod = null;
        _selectedCharge = null;
        _selectedTone = ExecutionTone.Judicial;
        _selectionSceneMode = ExecutionSceneMode.Automatic;
    }
}
