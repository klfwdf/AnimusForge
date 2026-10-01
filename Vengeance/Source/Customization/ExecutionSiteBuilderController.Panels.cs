using System;
using System.Collections.Generic;
using System.Linq;
using RichExecutions.Campaign;
using RichExecutions.Core;
using RichExecutions.Diagnostics;
using RichExecutions.Scene;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using BannerlordCampaign = TaleWorlds.CampaignSystem.Campaign;

namespace RichExecutions.Customization;

internal sealed partial class ExecutionSiteBuilderController
{
    private void ShowPrisonerPanel()
    {
        if (_prisonerOptions.Count == 0)
        {
            Status("{=REX_Builder_No_Prisoners}No eligible prisoner is available for this site.");
            return;
        }

        var current = CurrentVictim;
        var items = _prisonerOptions
            .Select(option => new ExecutionSiteBuilderPanelDefinition(
                option.Hero.StringId,
                option.Hero.Name?.ToString() ?? string.Empty,
                FormatPrisonerEntry(option),
                option.Source == PrisonerSource.PlayerParty
                    ? T("{=REX_Source_Party}Player party")
                    : T("{=REX_Source_Dungeon}Town dungeon"),
                string.Empty,
                ExecutionSiteResourceType.Prefab,
                ReferenceEquals(option.Hero, current)))
            .ToList();
        ShowPanel(PanelMode.Prisoner, T("{=REX_Builder_Prisoner_Title}Condemned prisoner"), items);
    }

    private static string FormatPrisonerEntry(ExecutionStudioPrisonerOption option)
    {
        var clan = option.Hero.Clan?.Name?.ToString();
        var text = new TextObject("{=REX_Builder_Prisoner_Entry}{CLAN} · relation {RELATION}");
        text.SetTextVariable("CLAN", string.IsNullOrWhiteSpace(clan)
            ? T("{=REX_Builder_Prisoner_No_Clan}No clan")
            : clan);
        text.SetTextVariable("RELATION", Hero.MainHero.GetRelation(option.Hero));
        return text.ToString();
    }

    // Switching the prisoner refreshes the charge evidence, the 3D model, the
    // recorded-crime list and the title block. Nothing is committed here: the
    // request is only rebuilt when the deployment is finished.
    private void SelectPrisoner(string heroId)
    {
        var option = _prisonerOptions.FirstOrDefault(candidate =>
            string.Equals(candidate.Hero.StringId, heroId, StringComparison.OrdinalIgnoreCase));
        if (option is null)
        {
            Status("{=REX_Builder_Prisoner_Invalid}That prisoner is no longer available.");
            return;
        }

        _selectedPrisoner = option;
        var text = new TextObject("{=REX_Builder_Prisoner_Selected}The site will condemn {NAME}.");
        text.SetTextVariable("NAME", option.Hero.Name);
        ShowMainPanel();
        Status(text);
    }

    private void ExecutePrisonerPanelOption(string optionId) => SelectPrisoner(optionId);

    private void ShowChargePanel()
    {
        var victim = CurrentVictim;
        var items = RichExecutionApi.Charges.All
            .Select(charge => new ExecutionSiteBuilderPanelDefinition(
                charge.StringId,
                charge.GetName().ToString(),
                charge.GetDescription().ToString(),
                FormatEvidenceLabel(victim, charge),
                string.Empty,
                ExecutionSiteResourceType.Prefab,
                string.Equals(charge.StringId, _displayCharge.StringId, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        ShowPanel(PanelMode.Charges, T("{=REX_Builder_Charges_Title}Announced charge"), items);
    }

    // Evidence is per prisoner, so the label follows whichever prisoner the
    // studio currently has selected.
    private static string FormatEvidenceLabel(Hero? victim, ExecutionChargeDefinition charge)
    {
        if (victim is null)
        {
            return string.Empty;
        }

        try
        {
            var behavior = BannerlordCampaign.Current?
                .GetCampaignBehavior<RichExecutionCampaignBehavior>();
            if (behavior is null)
            {
                return string.Empty;
            }

            return behavior.GetDeploymentEvidenceStrength(victim, charge) switch
            {
                EvidenceStrength.Strong => T("{=REX_Evidence_Strong}Conclusive evidence"),
                EvidenceStrength.Circumstantial => T("{=REX_Evidence_Circumstantial}Circumstantial evidence"),
                _ => T("{=REX_Evidence_None}No evidence")
            };
        }
        catch (Exception exception)
        {
            RexLog.Warning($"Could not read charge evidence: {exception.Message}");
            return string.Empty;
        }
    }

    // Presentation only: the campaign request keeps its own read-only charge, so
    // the influence cost, consequences and saved history stay unchanged.
    private void SelectDisplayCharge(string chargeId)
    {
        var charge = RichExecutionApi.Charges.All.FirstOrDefault(candidate =>
            string.Equals(candidate.StringId, chargeId, StringComparison.OrdinalIgnoreCase));
        if (charge is null)
        {
            Status("{=REX_Builder_Charge_Invalid}That charge is no longer available.");
            return;
        }

        _displayCharge = charge;
        var text = new TextObject("{=REX_Builder_Charge_Selected}The site will announce: {CHARGE}");
        text.SetTextVariable("CHARGE", charge.GetName());
        ShowMainPanel();
        Status(text);
    }

    private void ShowMethodPanel()
    {
        var items = RichExecutionApi.Methods.All
            .Where(method => ExecutionMethodRules.IsVisibleInSelection(method.StringId))
            .Select(method => new ExecutionSiteBuilderPanelDefinition(
                method.StringId,
                method.GetName().ToString(),
                method.GetDescription().ToString(),
                string.Empty,
                string.Empty,
                ExecutionSiteResourceType.Prefab,
                string.Equals(method.StringId, _selectedMethod.StringId, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        ShowPanel(PanelMode.Methods, T("{=REX_Builder_Methods}Execution methods"), items);
    }

    private void SelectMethod(string methodId)
    {
        var method = RichExecutionApi.Methods.All.FirstOrDefault(candidate =>
            string.Equals(candidate.StringId, methodId, StringComparison.OrdinalIgnoreCase));
        if (method is null || !ExecutionMethodRules.IsVisibleInSelection(method.StringId))
        {
            Status("{=REX_Builder_Method_Invalid}The selected execution method is no longer available.");
            return;
        }

        if (string.Equals(_selectedMethod.StringId, method.StringId, StringComparison.OrdinalIgnoreCase))
        {
            ShowMainPanel();
            Status("{=REX_Builder_Method_Unchanged}The current execution method remains selected.");
            return;
        }

        _selectedMethod = method;
        _preset = null;
        _rootPlaced = false;
        _rootPosition = Vec3.Invalid;
        _rootForward = Vec2.Forward;
        _pendingPiece = null;
        _pendingMarker = null;
        _pendingPreviewEntity = null;
        _selectedPieceId = null;
        _selectedMarkerId = null;
        _selectedTroopSlot = null;
        _undo.Clear();
        _redo.Clear();
        RemoveAllPreviews();
        _state = ExecutionSiteBuilderState.WaitingForBuilder;
        ShowMainPanel();
        Status("{=REX_Builder_Method_Changed}Punishment changed. Choose a matching preset; the previous layout was reset.");
    }

    private void ShowPresetPanel()
    {
        var methodId = _selectedMethod.StringId;
        var items = ExecutionSitePresetStore.LoadAll(methodId)
            .Where(result => result.IsValid && result.Preset is not null)
            .OrderBy(result => ExecutionSitePresetFactory.GetBuiltInPresetRank(methodId, result.Preset!.Id))
            .ThenBy(result => result.Preset!.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(result => new ExecutionSiteBuilderPanelDefinition(
                result.Preset!.Id.ToString("D"), result.Preset.Name,
                FormatPresetCounts(result.Preset.Pieces.Count, result.Preset.Markers.Count),
                _selectedMethod.GetName().ToString()))
            .ToList();
        if (items.Count == 0)
        {
            Status("{=REX_Builder_No_Presets}No saved preset exists for this execution method. Create or clone one first.");
            return;
        }
        ShowPanel(PanelMode.Presets, T("{=REX_Builder_Presets}Saved presets"), items);
    }

    private void ShowResourcePanel()
    {
        ShowPanel(PanelMode.Resources, T("{=REX_Builder_Resources}Mesh / Prefab catalog"),
            _resources.Select(resource => new ExecutionSiteBuilderPanelDefinition(
                resource.Id,
                T(resource.Name),
                T("{=REX_Builder_Resource_Entry}Place this catalog resource in the execution site."),
                LocalizedCategory(resource.Category),
                resource.ResourceKey,
                resource.ResourceType)).ToList());
    }





    private void ShowTroopSlotPanel()
    {
        ShowPanel(PanelMode.TroopSlots, T("{=REX_Builder_Troop_Slots}Personnel role"),
            new List<ExecutionSiteBuilderPanelDefinition>
            {
                new("executioner", T("{=REX_Builder_Executioner}Executioner"), CurrentTroop(_preset!.Troops.ExecutionerCharacterId)),
                new("melee", T("{=REX_Builder_Melee_Guard}Melee guard"), CurrentTroop(_preset!.Troops.MeleeGuardCharacterId)),
                new("ranged", T("{=REX_Builder_Ranged_Guard}Ranged guard"), CurrentTroop(_preset!.Troops.RangedGuardCharacterId))
            });
    }

    private void ShowTroopPanel(string slot)
    {
        _selectedTroopSlot = slot;
        var items = new List<ExecutionSiteBuilderPanelDefinition>
        {
            new("local", T("{=REX_Builder_Local_Culture}Local culture fallback"),
                T("{=REX_Builder_Local_Culture_Desc}Use a suitable troop from the settlement culture if the saved template is unavailable."), "fallback")
        };
        foreach (var character in CultureTroopSelector.GetPlayerPartyRegularSoldierTemplates())
        {
            var count = MobileParty.MainParty?.MemberRoster?.GetTroopCount(character) ?? 0;
            items.Add(new ExecutionSiteBuilderPanelDefinition(
                character.StringId,
                character.Name.ToString(),
                FormatTroopDetails(character.Tier, character.Culture?.Name.ToString(), count),
                character.Culture?.Name.ToString() ?? string.Empty));
        }
        ShowPanel(PanelMode.Troops, T("{=REX_Builder_Troops}Player-party troop templates"), items);
    }

    private void ShowPanel(PanelMode mode, string title, IReadOnlyList<ExecutionSiteBuilderPanelDefinition> items)
    {
        if (_view?.ShowPanel(title, items) != true)
        {
            Status("{=REX_Builder_Panel_Failed}The selection panel could not open.");
            return;
        }
        _panelMode = mode;
        _panelOpen = true;
        _state = ExecutionSiteBuilderState.PresetSelecting;
    }

    private void ClosePanel()
    {
        _view?.ClosePanel();
        _panelOpen = false;
        _panelMode = PanelMode.None;
        if (_state == ExecutionSiteBuilderState.PresetSelecting)
            _state = _preset is null ? ExecutionSiteBuilderState.WaitingForBuilder : ExecutionSiteBuilderState.Editing;
    }

    private void SelectPreset(string id)
    {
        if (!Guid.TryParse(id, out var presetId)) return;
        var result = ExecutionSitePresetStore.LoadAll(_selectedMethod.StringId)
            .FirstOrDefault(candidate => candidate.IsValid && candidate.Preset?.Id == presetId);
        if (result?.Preset is null)
        {
            Status("{=REX_Builder_Preset_Missing}That preset is no longer available.");
            return;
        }
        ClosePanel();
        BeginRootPlacement(ClonePreset(result.Preset));
    }

    private void SelectResource(string id)
    {
        var resource = _resources.FirstOrDefault(candidate => string.Equals(candidate.Id, id, StringComparison.OrdinalIgnoreCase));
        if (resource is null || _preset is null) return;
        ClosePanel();
        PushUndo();
        _pendingPiece = new ExecutionSitePiece
        {
            Id = Guid.NewGuid(), ResourceKey = resource.ResourceKey,
            ResourceType = resource.ResourceType, RoleId = resource.RoleId, Collision = resource.Collision,
            Transform = new ExecutionPresetTransform { Scale = 1f }
        };
        _selectedPieceId = _pendingPiece.Id;
        _selectedMarkerId = null;
        _state = ExecutionSiteBuilderState.Placing;
        Status("{=REX_Builder_Place_Piece}Move the mouse to place. Wheel: yaw; Shift+wheel: height; Ctrl+wheel: scale; Q/E: pitch; Z/C: roll; left click confirms.");
    }

    private void SelectFunctionalPiece(string roleId)
    {
        if (_preset is null) return;
        var definition = ExecutionSiteFunctionalCatalog.Get(_selectedMethod.StringId)
            .FirstOrDefault(candidate => string.Equals(candidate.RoleId, roleId, StringComparison.OrdinalIgnoreCase));
        if (definition is null) return;
        ClosePanel();
        PushUndo();
        var duplicate = _preset.Pieces.FirstOrDefault(piece =>
            string.Equals(piece.RoleId, definition.RoleId, StringComparison.OrdinalIgnoreCase));
        if (duplicate is not null)
        {
            RemovePreview(_piecePreviewEntities, duplicate.Id);
            _preset.Pieces.Remove(duplicate);
        }
        _pendingPiece = new ExecutionSitePiece
        {
            Id = Guid.NewGuid(),
            ResourceKey = definition.ResourceKey,
            ResourceType = definition.ResourceType,
            RoleId = definition.RoleId,
            Collision = definition.Collision,
            Transform = definition.Transform.Clone()
        };
        _selectedPieceId = _pendingPiece.Id;
        _selectedMarkerId = null;
        _state = ExecutionSiteBuilderState.Placing;
        Status("{=REX_Builder_Place_Functional}Place the functional apparatus. Its stable role will be preserved for the execution state machine.");
    }

    private void SelectMarkerRole(string roleId)
    {
        if (_preset is null) return;
        ClosePanel();
        PushUndo();
        _pendingMarker = new ExecutionSiteMarker
        {
            Id = Guid.NewGuid(), RoleId = roleId, Kind = GetMarkerKind(roleId),
            Transform = new ExecutionPresetTransform { Scale = 1f }
        };
        _selectedMarkerId = _pendingMarker.Id;
        _selectedPieceId = null;
        _state = ExecutionSiteBuilderState.Placing;
        Status("{=REX_Builder_Place_Marker}Move the marker with the mouse; use the same rotation and height controls and left click to confirm.");
    }

    private void SelectTroopSlot(string slot) => ShowTroopPanel(slot);

    private void SelectTroop(string characterId)
    {
        if (_preset is null || string.IsNullOrWhiteSpace(_selectedTroopSlot)) return;
        PushUndo();
        var selected = string.Equals(characterId, "local", StringComparison.OrdinalIgnoreCase) ? null : characterId;
        switch (_selectedTroopSlot)
        {
            case "executioner": _preset.Troops.ExecutionerCharacterId = selected; break;
            case "melee": _preset.Troops.MeleeGuardCharacterId = selected; break;
            case "ranged": _preset.Troops.RangedGuardCharacterId = selected; break;
        }
        _selectedTroopSlot = null;
        ShowMainPanel();
        Status("{=REX_Builder_Troop_Saved}Personnel template updated. No party troops were consumed.");
    }

    private void FinishBuilding()
    {
        if (!TryValidateCurrentDraft(out var validationError) || _preset is null)
        {
            Status(validationError ?? new TextObject(
                "{=REX_Builder_Confirm_Failed}The layout could not be initialized; the builder remains open."));
            return;
        }

        // Validate and install the layout while the request is still provisional.
        // No campaign session has been started yet, so a layout failure is cheap
        // to undo and cannot leave an active execution behind.
        if (!_host.TryAcceptCustomSiteLayout(_selectedMethod, ClonePreset(_preset), _rootPosition, _rootForward, out var error))
        {
            _state = ExecutionSiteBuilderState.Editing;
            RebuildPreview();
            Status(error ?? new TextObject("{=REX_Builder_Confirm_Failed}The layout could not be initialized; the builder remains open."));
            return;
        }

        // Start and commit the final campaign request only after the layout is
        // accepted. Any failure here rolls the host back to the provisional
        // request and leaves the builder usable for another attempt.
        if (!TryCommitDeploymentSelection(out var commitError))
        {
            _host.RollbackCustomSiteLayout();
            _state = ExecutionSiteBuilderState.Editing;
            RebuildPreview();
            Status(commitError ?? new TextObject(
                "{=REX_Builder_Commit_Failed}The deployment could not be committed; nothing was spent and the prisoner lives."));
            return;
        }

        // Custom layouts are session-only. Keep the adjusted clone in memory for
        // this ceremony; the preset store remains read-only during gameplay.
        _state = ExecutionSiteBuilderState.LayoutConfirmed;
        RemoveAllPreviews();

        _view?.ClosePanel();
        _view?.SetStatus(string.Empty);
        _state = ExecutionSiteBuilderState.CeremonyInitializing;
        _disposed = true;
    }

    // Rebuilds the campaign request from the final studio selection and starts
    // the session. The provisional request that opened the scene was never begun,
    // so a failure here still spends nothing and kills nobody.
    private bool TryCommitDeploymentSelection(out TextObject? error)
    {
        error = null;
        var prisoner = _selectedPrisoner;
        if (prisoner is null)
        {
            error = new TextObject(
                "{=REX_Builder_No_Prisoners}No eligible prisoner is available for this site.");
            return false;
        }

        ExecutionRequest? begunRequest = null;
        try
        {
            var behavior = BannerlordCampaign.Current?
                .GetCampaignBehavior<RichExecutionCampaignBehavior>();
            if (behavior is null)
            {
                error = new TextObject(
                    "{=REX_Builder_Commit_Failed}The deployment could not be committed; nothing was spent and the prisoner lives.");
                return false;
            }

            var request = behavior.BuildDeploymentRequest(
                prisoner.Hero,
                prisoner.Source,
                _selectedMethod,
                _displayCharge,
                _host.Request.Tone,
                _host.Request.Venue);
            if (request is null)
            {
                error = new TextObject(
                    "{=REX_Builder_Commit_Failed}The deployment could not be committed; nothing was spent and the prisoner lives.");
                return false;
            }

            var service = _host.DeploymentService;
            var validation = service.Validate(request, ExecutionValidationStage.Confirmation);
            if (!validation.IsValid)
            {
                error = validation.Message;
                return false;
            }

            // Begin the final request before replacing the provisional request.
            // If the host swap fails, the finally block cancels this new entry.
            var begin = service.Begin(request);
            if (!begin.IsValid)
            {
                error = begin.Message;
                return false;
            }
            begunRequest = request;

            if (!_host.TryCommitDeploymentRequest(request, out error))
            {
                return false;
            }

            begunRequest = null;
            return true;
        }
        catch (Exception exception)
        {
            RexLog.Error("Could not commit the custom deployment request.", exception);
            error = new TextObject(
                "{=REX_Builder_Commit_Failed}The deployment could not be committed; nothing was spent and the prisoner lives.");
            return false;
        }
        finally
        {
            if (begunRequest is not null)
            {
                _host.DeploymentService.Cancel(
                    begunRequest,
                    ExecutionFailureReason.UnexpectedError,
                    error ?? new TextObject(
                        "{=REX_Builder_Commit_Failed}The deployment could not be committed; nothing was spent and the prisoner lives."));
            }
        }
    }

    private static ExecutionSiteMarkerKind GetMarkerKind(string roleId)
    {
        if (roleId.StartsWith("crowd_", StringComparison.OrdinalIgnoreCase)) return ExecutionSiteMarkerKind.Crowd;
        if (roleId.StartsWith("guard_", StringComparison.OrdinalIgnoreCase) || roleId is "victim" or "executioner") return ExecutionSiteMarkerKind.Actor;
        if (roleId.Contains("interaction") || roleId.Contains("release") || roleId.Contains("control") || roleId.Contains("ignition") || roleId.Contains("throw")) return ExecutionSiteMarkerKind.Interaction;
        return ExecutionSiteMarkerKind.Functional;
    }


    private static string FormatPresetCounts(int pieceCount, int markerCount)
    {
        var text = new TextObject("{=REX_Builder_Preset_Counts}{PIECES} pieces · {MARKERS} markers");
        text.SetTextVariable("PIECES", pieceCount);
        text.SetTextVariable("MARKERS", markerCount);
        return text.ToString();
    }

    private static string LocalizedCategory(string category) => category switch
    {
        "platform" => T("{=REX_Builder_Category_Platform}platform"),
        "planks" => T("{=REX_Builder_Category_Planks}planks"),
        "beams" => T("{=REX_Builder_Category_Beams}beams"),
        "rope_chain" => T("{=REX_Builder_Category_Rope_Chain}rope and chain"),
        "wood" => T("{=REX_Builder_Category_Wood}wood"),
        "supplies" => T("{=REX_Builder_Category_Supplies}supplies"),
        "weapons" => T("{=REX_Builder_Category_Weapons}weapons"),
        "apparatus" => T("{=REX_Builder_Category_Apparatus}apparatus"),
        _ => T("{=REX_Builder_Category_Decoration}decoration")
    };
    private static string CurrentTroop(string? id) => string.IsNullOrWhiteSpace(id) ? T("{=REX_Builder_Local_Culture}Local culture fallback") : id!;
}
