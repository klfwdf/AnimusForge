using System;
using System.Collections.Generic;
using System.Linq;
using RichExecutions.Campaign;
using RichExecutions.Core;
using RichExecutions.Diagnostics;
using RichExecutions.Scene;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using BannerlordCampaign = TaleWorlds.CampaignSystem.Campaign;

namespace RichExecutions.Customization;

internal enum ExecutionSiteBuilderState
{
    WaitingForBuilder = 0,
    PresetSelecting = 1,
    Placing = 2,
    Editing = 3,
    LayoutConfirmed = 4,
    CeremonyInitializing = 5
}

internal sealed partial class ExecutionSiteBuilderController
{
    private enum PanelMode
    {
        None = 0,
        Presets = 1,
        Resources = 2,
        Functional = 3,
        Markers = 4,
        TroopSlots = 5,
        Troops = 6,
        Methods = 7,
        Main = 8,
        Prisoner = 9,
        Charges = 10
    }


    private const float DegreesToRadians = MathF.PI / 180f;
    private const int MaximumUndoSnapshots = 64;
    private readonly TownExecutionMissionBehavior _host;
    private readonly Mission _mission;
    private readonly IReadOnlyList<ExecutionSiteResourceDefinition> _resources;
    private readonly Dictionary<Guid, GameEntity> _piecePreviewEntities = new();
    private readonly Dictionary<Guid, GameEntity> _markerPreviewEntities = new();
    private readonly Stack<ExecutionSitePreset> _undo = new();
    private readonly Stack<ExecutionSitePreset> _redo = new();
    private readonly List<string> _wheelOptionIds = new();
    private ExecutionMethodDefinition _selectedMethod;
    private ExecutionChargeDefinition _displayCharge;
    private ExecutionStudioPrisonerOption? _selectedPrisoner;
    private IReadOnlyList<ExecutionStudioPrisonerOption> _prisonerOptions = Array.Empty<ExecutionStudioPrisonerOption>();
    private ExecutionSiteBuilderMissionView? _view;
    private ExecutionSitePreset? _preset;
    private ExecutionSiteBuilderState _state = ExecutionSiteBuilderState.WaitingForBuilder;
    private PanelMode _panelMode;
    private string? _selectedWheelOption;
    private string? _selectedTroopSlot;
    private Guid? _selectedPieceId;
    private Guid? _selectedMarkerId;
    private ExecutionSitePiece? _pendingPiece;
    private ExecutionSiteMarker? _pendingMarker;
    private GameEntity? _pendingPreviewEntity;
    private Vec3 _rootPosition = Vec3.Invalid;
    private Vec2 _rootForward = Vec2.Forward;
    private bool _rootPlaced;
    private float _rootHeightOffset;
    private bool _movingSelected;


    private bool _wheelOpen;
    private bool _panelOpen;
    private bool _disposed;

    internal ExecutionSiteBuilderController(TownExecutionMissionBehavior host, Mission mission)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _mission = mission ?? throw new ArgumentNullException(nameof(mission));
        _selectedMethod = _host.Request.Method;
        _displayCharge = _host.Request.Charge;
        _resources = ExecutionSiteResourceCatalog.Load()
            .Where(IsCatalogResourceAvailable)
            .ToList();
        LoadPrisonerOptions();
    }

    // The scene opened on a provisional request, so the studio owns the prisoner
    // choice. Start from whichever option matches the provisional victim.
    private void LoadPrisonerOptions()
    {
        try
        {
            var behavior = BannerlordCampaign.Current?
                .GetCampaignBehavior<RichExecutionCampaignBehavior>();
            _prisonerOptions = behavior?.GetDeploymentPrisonerOptions(_host.Request.Venue)
                ?? Array.Empty<ExecutionStudioPrisonerOption>();
        }
        catch (Exception exception)
        {
            RexLog.Warning($"Could not list deployment prisoners: {exception.Message}");
            _prisonerOptions = Array.Empty<ExecutionStudioPrisonerOption>();
        }

        _selectedPrisoner = _prisonerOptions.FirstOrDefault(option =>
            ReferenceEquals(option.Hero, _host.Request.Victim));
    }

    private Hero? CurrentVictim => _selectedPrisoner?.Hero ?? _host.Request.Victim;

    internal ExecutionSiteBuilderState State => _state;
    internal bool IsActive => !_disposed && _state < ExecutionSiteBuilderState.LayoutConfirmed;

    internal void AttachView(ExecutionSiteBuilderMissionView view)
    {
        if (_disposed) return;
        _view = view;
        Status("{=REX_Builder_Ready}Custom execution-site builder ready. Press B to open the deployment panel.");
        RexLog.Info($"Session {_host.Request.SessionId} attached the custom execution-site builder view.");
    }

    internal void DetachView(ExecutionSiteBuilderMissionView view)
    {
        if (ReferenceEquals(_view, view)) _view = null;
    }

    internal void SelectWheelOption(string optionId) =>
        _selectedWheelOption = string.IsNullOrWhiteSpace(optionId) ? null : optionId;

    internal void ConfirmWheelOption(string optionId)
    {
        if (!_wheelOpen || string.IsNullOrWhiteSpace(optionId)) return;
        CloseWheel();
        ExecuteWheelOption(optionId);
    }

    internal void SelectPanelItem(string itemId)
    {
        if (!_panelOpen || string.IsNullOrWhiteSpace(itemId)) return;
        try
        {
            switch (_panelMode)
            {
                case PanelMode.Presets: SelectPreset(itemId); break;
                case PanelMode.Resources: SelectResource(itemId); break;
                case PanelMode.Functional: SelectFunctionalPiece(itemId); break;
                case PanelMode.Markers: SelectMarkerRole(itemId); break;
                case PanelMode.TroopSlots: SelectTroopSlot(itemId); break;
                case PanelMode.Troops: SelectTroop(itemId); break;
                case PanelMode.Methods: SelectMethod(itemId); break;
                case PanelMode.Prisoner: ExecutePrisonerPanelOption(itemId); break;
                case PanelMode.Charges: SelectDisplayCharge(itemId); break;
                case PanelMode.Main: ExecuteMainPanelOption(itemId); break;
            }
        }
        catch (Exception exception)
        {
            RexLog.Error("Custom builder panel selection failed; the builder remains active.", exception);
            Status("{=REX_Builder_Selection_Failed}That selection could not be applied. The builder remains open.");
        }
    }

    internal void FinishCustomBuilderDeployment()
    {
        if (!_panelOpen || _panelMode != PanelMode.Main)
        {
            return;
        }

        if (!TryValidateCurrentDraft(out var error))
        {
            Status(error ?? new TextObject(
                "{=REX_Builder_Confirm_Failed}The layout could not be initialized; the builder remains open."));
            return;
        }

        FinishBuilding();
    }

    internal void Tick(float dt)
    {
        if (!IsActive || _view is null) return;
        if (_panelOpen)
        {
            if (_view.IsEscapeReleased || Input.IsKeyPressed(InputKey.Escape))
            {
                if (_panelMode == PanelMode.Main)
                {
                    ClosePanel();
                }
                else
                {
                    ShowMainPanel();
                }
            }
            return;
        }

        if (_wheelOpen)
        {
            if (Input.IsKeyPressed(InputKey.Escape))
            {
                CloseWheel();
                return;
            }

            var mouseOverCommand =
                _view.TryGetCommandMouseIndex(_wheelOptionIds.Count, out var mouseIndex);
            if (mouseOverCommand && mouseIndex >= 0 && mouseIndex < _wheelOptionIds.Count)
            {
                _view.SelectCommandIndex(mouseIndex);
                SelectWheelOption(_wheelOptionIds[mouseIndex]);
                if (_view.IsLeftMousePressed)
                {
                    var option = _selectedWheelOption;
                    CloseWheel();
                    ExecuteWheelOption(option);
                    return;
                }
            }

            // Keyboard fallback: press B again to confirm the highlighted command.
            if (Input.IsKeyPressed(InputKey.B))
            {
                var option = _selectedWheelOption;
                CloseWheel();
                ExecuteWheelOption(option);
            }
            return;
        }

        if (_state == ExecutionSiteBuilderState.Placing)
        {
            TickPlacementInput();
            return;
        }

        TickEditingShortcuts();
        if (Input.IsKeyPressed(InputKey.B))
        {
            OpenWheel();
        }
    }

    internal void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _view?.SetStatus(string.Empty);
        CloseWheel();
        ClosePanel();
        RemoveAllPreviews();
        _view = null;
    }

    private void OpenWheel()
    {
        ShowMainPanel();
    }

    private void ShowMainPanel()
    {
        var layoutReady = _preset is not null && _rootPlaced;
        var canFinish = TryValidateCurrentDraft(out var validationError);
        PushPrisonerToView();
        _view?.ConfigurePanelSummary(
            _selectedMethod.GetName().ToString(),
            _preset?.Name ?? T("{=REX_Builder_Summary_No_Layout}No layout selected"),
            FormatPersonnelSummary(),
            canFinish
                ? T("{=REX_Builder_Summary_Ready}Ready to finish deployment")
                : validationError?.ToString() ??
                  T("{=REX_Builder_Summary_Incomplete}Choose a layout and place its root anchor"),
            canFinish);

        var items = new List<ExecutionSiteBuilderPanelDefinition>
        {
            new("prisoner", T("{=REX_Builder_Nav_Prisoner}Condemned"),
                T("{=REX_Builder_Nav_Prisoner_Desc}Choose which prisoner is executed at this site."),
                CurrentVictim?.Name?.ToString() ?? string.Empty),
            new("charge", T("{=REX_Builder_Nav_Charge}Charge"),
                T("{=REX_Builder_Nav_Charge_Desc}Choose the charge announced before the crowd."),
                _displayCharge.GetName().ToString()),
            new("method", T("{=REX_Builder_Nav_Method}Punishment"),
                _selectedMethod.GetDescription().ToString(),
                T("{=REX_Builder_Nav_Method_Category}RITUAL")),
            new("deploy", T("{=REX_Builder_Nav_Layout}Layout"),
                T("{=REX_Builder_Nav_Layout_Desc}Select a matching preset and place the root anchor."),
                layoutReady ? T("{=REX_Builder_Main_Ready}READY") : T("{=REX_Builder_Main_Setup}SETUP")),
            new("troops", T("{=REX_Builder_Nav_Personnel}Personnel"),
                T("{=REX_Builder_Nav_Personnel_Desc}Assign executioner, melee guard and ranged guard templates."),
                layoutReady ? T("{=REX_Builder_Main_Ready}READY") : T("{=REX_Builder_Main_Locked}LOCKED")),
            new("add", T("{=REX_Builder_Nav_Details}Details"),
                T("{=REX_Builder_Nav_Details_Desc}Add decorations and edit the deployed scene pieces."),
                layoutReady ? T("{=REX_Builder_Main_Ready}READY") : T("{=REX_Builder_Main_Locked}LOCKED"))
        };

        if (_view?.ShowPanel(
                T("{=REX_Builder_Main_Title}Deployment studio"),
                items,
                isMainPanel: true) != true)
        {
            Status("{=REX_Builder_Panel_Failed}The deployment studio could not open.");
            return;
        }

        _panelMode = PanelMode.Main;
        _panelOpen = true;
        _state = ExecutionSiteBuilderState.PresetSelecting;
    }

    // The condemned hero and the campaign charge are read-only here. The charge
    // shown in this studio is presentation only; the campaign request, its cost
    // and the saved history keep the judgement-panel charge.
    private void PushPrisonerToView()
    {
        var victim = CurrentVictim;
        var bannerCode = victim?.ClanBanner?.Serialize() ?? victim?.Clan?.Banner?.Serialize();
        RexLog.Info($"PushPrisonerToView: victim='{victim?.Name}', clan='{victim?.Clan?.Name}', bannerCode='{bannerCode}'");
        _view?.ConfigurePrisoner(
            victim?.CharacterObject,
            bannerCode,
            victim?.Name?.ToString() ?? string.Empty,
            victim?.Clan?.Name?.ToString() ?? T("{=REX_Builder_Prisoner_No_Clan}No clan"),
            _displayCharge.GetName().ToString(),
            FormatPrisonerTitle(victim),
            FormatPrisonerOrigin(),
            LoadRecordedIncidents(victim));
    }

    private string FormatPrisonerTitle(Hero? victim)
    {
        if (victim is null)
        {
            return string.Empty;
        }

        var culture = victim.Culture?.Name?.ToString();
        var rank = victim.IsFactionLeader
            ? T("{=REX_Builder_Rank_Ruler}Ruler")
            : victim.Clan?.Leader == victim
                ? T("{=REX_Builder_Rank_Clan_Leader}Clan leader")
                : victim.IsLord
                    ? T("{=REX_Builder_Rank_Lord}Lord")
                    : T("{=REX_Builder_Rank_Notable}Notable");

        if (string.IsNullOrWhiteSpace(culture))
        {
            return rank;
        }

        var text = new TextObject("{=REX_Builder_Prisoner_Rank}{CULTURE} · {RANK}");
        text.SetTextVariable("CULTURE", culture);
        text.SetTextVariable("RANK", rank);
        return text.ToString();
    }

    private string FormatPrisonerOrigin()
    {
        var source = _selectedPrisoner?.Source ?? _host.Request.PrisonerSource;
        var custody = source == PrisonerSource.PlayerParty
            ? T("{=REX_Source_Party}Player party")
            : T("{=REX_Source_Dungeon}Town dungeon");
        var venue = _host.Request.Venue?.Name?.ToString();
        if (string.IsNullOrWhiteSpace(venue))
        {
            return custody;
        }

        var text = new TextObject("{=REX_Builder_Prisoner_Origin}{CUSTODY} · held at {VENUE}");
        text.SetTextVariable("CUSTODY", custody);
        text.SetTextVariable("VENUE", venue);
        return text.ToString();
    }

    // Read-only projection of the campaign evidence ledger. A missing campaign or
    // behavior simply yields an empty list; the studio stays usable.
    private static IReadOnlyList<ExecutionSiteBuilderIncidentDefinition> LoadRecordedIncidents(Hero? victim)
    {
        if (victim is null)
        {
            return Array.Empty<ExecutionSiteBuilderIncidentDefinition>();
        }

        try
        {
            var behavior = BannerlordCampaign.Current?
                .GetCampaignBehavior<RichExecutionCampaignBehavior>();
            if (behavior is null)
            {
                return Array.Empty<ExecutionSiteBuilderIncidentDefinition>();
            }

            return behavior.GetRecordedIncidents(victim)
                .Select(incident => new ExecutionSiteBuilderIncidentDefinition(
                    incident.ChargeText,
                    incident.RecordText,
                    incident.DetailText,
                    incident.Strength switch
                    {
                        EvidenceStrength.Strong => 2,
                        EvidenceStrength.Circumstantial => 1,
                        _ => 0
                    }))
                .ToList();
        }
        catch (Exception exception)
        {
            RexLog.Warning($"Could not read the recorded incident list: {exception.Message}");
            return Array.Empty<ExecutionSiteBuilderIncidentDefinition>();
        }
    }

    internal void ReturnToMainPanel()
    {
        if (!_panelOpen || _panelMode == PanelMode.Main)
        {
            return;
        }

        ShowMainPanel();
    }

    private void ExecuteMainPanelOption(string? optionId)
    {
        switch (optionId)
        {
            case "prisoner": ShowPrisonerPanel(); break;
            case "charge": ShowChargePanel(); break;
            case "method": ShowMethodPanel(); break;
            case "deploy": ShowPresetPanel(); break;
            case "add": if (EnsureReady()) ShowResourcePanel(); break;
            case "troops": if (EnsureReady()) ShowTroopSlotPanel(); break;
            case "finish": if (EnsureReady()) FinishBuilding(); break;
        }
    }

    private void CloseWheel()
    {
        _view?.CloseWheel();
        _wheelOpen = false;
        _wheelOptionIds.Clear();
        _selectedWheelOption = null;
    }

    private void ExecuteWheelOption(string? optionId)
    {
        switch (optionId)
        {
            case "method": ShowMethodPanel(); break;
            case "deploy": ShowPresetPanel(); break;
            case "add": if (EnsureReady()) ShowResourcePanel(); break;
            case "troops": if (EnsureReady()) ShowTroopSlotPanel(); break;
            case "finish": if (EnsureReady()) FinishBuilding(); break;
        }
    }

    private bool TryValidateCurrentDraft(out TextObject? error)
    {
        if (_preset is null || !_rootPlaced)
        {
            error = new TextObject(
                "{=REX_Builder_Summary_Incomplete}Choose a layout and place its root anchor");
            return false;
        }

        return _host.TryValidateCustomSiteLayout(
            _selectedMethod,
            _preset,
            _rootPosition,
            _rootForward,
            out error);
    }

    private bool EnsureReady()
    {
        if (_preset is not null && _rootPlaced) return true;
        Status("{=REX_Builder_Create_First}Deploy a built-in or saved preset before using this option.");
        return false;
    }

    private void Status(string text) => _view?.SetStatus(T(text));
    private void Status(TextObject text) => _view?.SetStatus(text.ToString());
    private string FormatPersonnelSummary()
    {
        if (_preset is null)
        {
            return T("{=REX_Builder_Summary_No_Personnel}No personnel assigned");
        }

        var executioner = CurrentTroop(_preset.Troops.ExecutionerCharacterId);
        var melee = CurrentTroop(_preset.Troops.MeleeGuardCharacterId);
        var ranged = CurrentTroop(_preset.Troops.RangedGuardCharacterId);
        var text = new TextObject("{=REX_Builder_Summary_Personnel}Executioner: {EXECUTIONER}\nMelee: {MELEE}\nRanged: {RANGED}");
        text.SetTextVariable("EXECUTIONER", executioner);
        text.SetTextVariable("MELEE", melee);
        text.SetTextVariable("RANGED", ranged);
        return text.ToString();
    }

    private static string FormatTroopDetails(int tier, string? culture, int count)
    {
        var text = new TextObject("{=REX_Builder_Troop_Details}Tier {TIER} · {CULTURE} · {COUNT} available");
        text.SetTextVariable("TIER", tier);
        text.SetTextVariable("CULTURE", culture ?? T("{=REX_Unknown}Unknown"));
        text.SetTextVariable("COUNT", count);
        return text.ToString();
    }

    private static string T(string text) =>
        new TextObject(text).ToString();

    private static float Clamp(float value, float min, float max) => value < min ? min : value > max ? max : value;
    private static bool IsCatalogResourceAvailable(ExecutionSiteResourceDefinition resource)
    {
        var available = resource.ResourceType == ExecutionSiteResourceType.Prefab
            ? GameEntity.PrefabExists(resource.ResourceKey)
            : IsMeshAvailable(resource.ResourceKey);
        if (!available)
        {
            RexLog.Warning(
                $"Custom-site catalog entry '{resource.Id}' was disabled because resource '{resource.ResourceKey}' is unavailable.");
        }
        return available;
    }

    private static ExecutionSiteBuilderWheelDefinition Wheel(string id, string name, string description, string status = "") => new(id, T(name), T(description), status);
}
