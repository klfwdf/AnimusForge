using System;
using System.Collections.Generic;
using System.Linq;
using RichExecutions.Diagnostics;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace RichExecutions.Customization;

internal sealed class ExecutionSiteBuilderWheelDefinition
{
    public ExecutionSiteBuilderWheelDefinition(string id, string name, string description, string statusText = "")
    {
        Id = id;
        Name = name;
        Description = description;
        StatusText = statusText;
    }

    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public string StatusText { get; }
}

internal sealed class ExecutionSiteBuilderPanelDefinition
{
    public ExecutionSiteBuilderPanelDefinition(
        string id,
        string name,
        string description,
        string category = "",
        string resourceKey = "",
        ExecutionSiteResourceType resourceType = ExecutionSiteResourceType.Prefab,
        bool isSelected = false)
    {
        Id = id;
        Name = name;
        Description = description;
        Category = category;
        ResourceKey = resourceKey;
        ResourceType = resourceType;
        IsSelected = isSelected;
    }

    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public string Category { get; }
    public string ResourceKey { get; }
    public ExecutionSiteResourceType ResourceType { get; }
    public bool IsSelected { get; }
}

// One recorded incident from the campaign evidence ledger. The studio only
// displays these; the ledger itself stays owned by the campaign behavior.
internal sealed class ExecutionSiteBuilderIncidentDefinition
{
    public ExecutionSiteBuilderIncidentDefinition(
        string chargeText,
        string recordText,
        string detailText,
        int evidenceRank)
    {
        ChargeText = chargeText;
        RecordText = recordText;
        DetailText = detailText;
        EvidenceRank = evidenceRank;
    }

    public string ChargeText { get; }
    public string RecordText { get; }
    public string DetailText { get; }

    // 0 = rumour only, 1 = circumstantial, 2 = conclusive.
    public int EvidenceRank { get; }
}

internal sealed class ExecutionSiteBuilderVM : ViewModel
{
    private readonly Action<string> _onWheelHighlighted;
    private readonly Action<string> _onWheelClicked;
    private readonly Action<string> _onPanelSelected;
    private readonly Action _onFinishDeployment;
    private readonly Action _onBack;
    private readonly List<ExecutionSiteBuilderPanelDefinition> _allPanelItems = new();
    private bool _isWheelVisible;
    private bool _isPanelVisible;
    private string _selectedWheelName = string.Empty;
    private string _selectedWheelDescription = string.Empty;
    private string _footerText = string.Empty;
    private string _panelTitle = string.Empty;
    private string _searchText = string.Empty;
    private string _statusText = string.Empty;
    private string _currentMethodText = string.Empty;
    private string _currentPresetText = string.Empty;
    private string _personnelText = string.Empty;
    private string _deploymentStateText = string.Empty;
    private string _prisonerNameText = string.Empty;
    private string _prisonerClanText = string.Empty;
    private string _prisonerChargeText = string.Empty;
    private string _prisonerTitleText = string.Empty;
    private string _prisonerOriginText = string.Empty;
    private bool _isMainPanel;
    private bool _canFinishDeployment;
    private bool _hasPrisonerModel;
    private bool _hasIncidents;
    private BannerImageIdentifierVM? _clanBanner_9;
    private bool _hasClanBanner;

    internal ExecutionSiteBuilderVM(
        Action<string> onWheelHighlighted,
        Action<string> onWheelClicked,
        Action<string> onPanelSelected,
        Action onFinishDeployment,
        Action onBack)
    {
        _onWheelHighlighted = onWheelHighlighted;
        _onWheelClicked = onWheelClicked;
        _onPanelSelected = onPanelSelected;
        _onFinishDeployment = onFinishDeployment;
        _onBack = onBack;
        WheelItems = new MBBindingList<ExecutionSiteBuilderWheelItemVM>();
        PanelItems = new MBBindingList<ExecutionSiteBuilderPanelItemVM>();
        Incidents = new MBBindingList<ExecutionSiteBuilderIncidentVM>();
        PrisonerModel = new CharacterViewModel(CharacterViewModel.StanceTypes.None);
    }

    [DataSourceProperty]
    public bool IsWheelVisible
    {
        get => _isWheelVisible;
        set
        {
            if (_isWheelVisible == value) return;
            _isWheelVisible = value;
            OnPropertyChangedWithValue(value, nameof(IsWheelVisible));
        }
    }

    [DataSourceProperty]
    public bool IsPanelVisible
    {
        get => _isPanelVisible;
        set
        {
            if (_isPanelVisible == value) return;
            _isPanelVisible = value;
            OnPropertyChangedWithValue(value, nameof(IsPanelVisible));
            OnPropertyChanged(nameof(IsMainPanelVisible));
            OnPropertyChanged(nameof(IsSubPanelVisible));
            OnPropertyChanged(nameof(IsStatusBannerVisible));
        }
    }

    // The studio already shows the status inside its own panel; the world-space
    // banner is only for placement mode, when no panel is open.
    [DataSourceProperty]
    public bool IsStatusBannerVisible => !IsPanelVisible && !string.IsNullOrEmpty(StatusText);

    [DataSourceProperty]
    public string SelectedWheelName
    {
        get => _selectedWheelName;
        set
        {
            if (_selectedWheelName == value) return;
            _selectedWheelName = value;
            OnPropertyChangedWithValue(value, nameof(SelectedWheelName));
        }
    }

    [DataSourceProperty]
    public string SelectedWheelDescription
    {
        get => _selectedWheelDescription;
        set
        {
            if (_selectedWheelDescription == value) return;
            _selectedWheelDescription = value;
            OnPropertyChangedWithValue(value, nameof(SelectedWheelDescription));
        }
    }

    [DataSourceProperty]
    public string FooterText
    {
        get => _footerText;
        set
        {
            if (_footerText == value) return;
            _footerText = value;
            OnPropertyChangedWithValue(value, nameof(FooterText));
        }
    }

    [DataSourceProperty]
    public string PanelTitle
    {
        get => _panelTitle;
        set
        {
            if (_panelTitle == value) return;
            _panelTitle = value;
            OnPropertyChangedWithValue(value, nameof(PanelTitle));
        }
    }

    [DataSourceProperty]
    public string SearchText
    {
        get => _searchText;
        set
        {
            value ??= string.Empty;
            if (_searchText == value) return;
            _searchText = value;
            OnPropertyChangedWithValue(value, nameof(SearchText));
            RebuildPanelItems();
        }
    }

    [DataSourceProperty]
    public string StatusText
    {
        get => _statusText;
        set
        {
            value ??= string.Empty;
            if (_statusText == value) return;
            _statusText = value;
            OnPropertyChangedWithValue(value, nameof(StatusText));
            OnPropertyChanged(nameof(IsStatusBannerVisible));
        }
    }

    [DataSourceProperty]
    public string DeploymentStateText
    {
        get => _deploymentStateText;
        set
        {
            value ??= string.Empty;
            if (_deploymentStateText == value) return;
            _deploymentStateText = value;
            OnPropertyChangedWithValue(value, nameof(DeploymentStateText));
        }
    }

    [DataSourceProperty]
    public bool IsMainPanel
    {
        get => _isMainPanel;
        private set
        {
            if (_isMainPanel == value) return;
            _isMainPanel = value;
            OnPropertyChangedWithValue(value, nameof(IsMainPanel));
            OnPropertyChanged(nameof(IsMainPanelVisible));
            OnPropertyChanged(nameof(IsSubPanelVisible));
        }
    }

    [DataSourceProperty]
    public string CurrentMethodText
    {
        get => _currentMethodText;
        set
        {
            value ??= string.Empty;
            if (_currentMethodText == value) return;
            _currentMethodText = value;
            OnPropertyChangedWithValue(value, nameof(CurrentMethodText));
        }
    }

    [DataSourceProperty]
    public string CurrentPresetText
    {
        get => _currentPresetText;
        set
        {
            value ??= string.Empty;
            if (_currentPresetText == value) return;
            _currentPresetText = value;
            OnPropertyChangedWithValue(value, nameof(CurrentPresetText));
        }
    }

    [DataSourceProperty]
    public string PersonnelText
    {
        get => _personnelText;
        set
        {
            value ??= string.Empty;
            if (_personnelText == value) return;
            _personnelText = value;
            OnPropertyChangedWithValue(value, nameof(PersonnelText));
        }
    }

    [DataSourceProperty]
    public CharacterViewModel PrisonerModel { get; }

    [DataSourceProperty]
    public string PrisonerNameText
    {
        get => _prisonerNameText;
        set
        {
            value ??= string.Empty;
            if (_prisonerNameText == value) return;
            _prisonerNameText = value;
            OnPropertyChangedWithValue(value, nameof(PrisonerNameText));
        }
    }

    [DataSourceProperty]
    public string PrisonerClanText
    {
        get => _prisonerClanText;
        set
        {
            value ??= string.Empty;
            if (_prisonerClanText == value) return;
            _prisonerClanText = value;
            OnPropertyChangedWithValue(value, nameof(PrisonerClanText));
        }
    }

    [DataSourceProperty]
    public string PrisonerChargeText
    {
        get => _prisonerChargeText;
        set
        {
            value ??= string.Empty;
            if (_prisonerChargeText == value) return;
            _prisonerChargeText = value;
            OnPropertyChangedWithValue(value, nameof(PrisonerChargeText));
        }
    }

    [DataSourceProperty]
    public bool HasPrisonerModel
    {
        get => _hasPrisonerModel;
        private set
        {
            if (_hasPrisonerModel == value) return;
            _hasPrisonerModel = value;
            OnPropertyChangedWithValue(value, nameof(HasPrisonerModel));
        }
    }

    [DataSourceProperty]
    public BannerImageIdentifierVM? ClanBanner_9
    {
        get => _clanBanner_9;
        private set
        {
            if (_clanBanner_9 == value) return;
            _clanBanner_9 = value;
            OnPropertyChangedWithValue(value, nameof(ClanBanner_9));
        }
    }

    [DataSourceProperty]
    public bool HasClanBanner
    {
        get => _hasClanBanner;
        private set
        {
            if (_hasClanBanner == value) return;
            _hasClanBanner = value;
            OnPropertyChangedWithValue(value, nameof(HasClanBanner));
        }
    }

    [DataSourceProperty]
    public string PrisonerTitleText
    {
        get => _prisonerTitleText;
        set
        {
            value ??= string.Empty;
            if (_prisonerTitleText == value) return;
            _prisonerTitleText = value;
            OnPropertyChangedWithValue(value, nameof(PrisonerTitleText));
        }
    }

    [DataSourceProperty]
    public string PrisonerOriginText
    {
        get => _prisonerOriginText;
        set
        {
            value ??= string.Empty;
            if (_prisonerOriginText == value) return;
            _prisonerOriginText = value;
            OnPropertyChangedWithValue(value, nameof(PrisonerOriginText));
        }
    }

    [DataSourceProperty] public MBBindingList<ExecutionSiteBuilderIncidentVM> Incidents { get; }

    [DataSourceProperty]
    public bool HasIncidents
    {
        get => _hasIncidents;
        private set
        {
            if (_hasIncidents == value) return;
            _hasIncidents = value;
            OnPropertyChangedWithValue(value, nameof(HasIncidents));
            OnPropertyChanged(nameof(IsIncidentListEmpty));
        }
    }

    [DataSourceProperty]
    public bool IsIncidentListEmpty => !HasIncidents;

    internal void SetPrisonerModel(BasicCharacterObject? character, string? bannerCode)
    {
        try
        {
            PrisonerModel.ExecuteStopCustomAnimation();
        }
        catch (Exception exception)
        {
            RexLog.Warning($"Could not stop the previous prisoner tableau animation: {exception.Message}");
        }

        // CharacterTableauWidget can retain the previous render target when
        // only a nested DataSource changes. Toggle the model off, refill the
        // same VM, then re-enable it so a second prisoner gets a fresh tableau.
        PrisonerModel.IsTableauEnabled = false;
        HasPrisonerModel = false;
        ClanBanner_9 = null;
        HasClanBanner = false;
        if (character is null)
        {
            return;
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(bannerCode))
            {
                try
                {
                    var banner = new Banner(bannerCode);
                    ClanBanner_9 = new BannerImageIdentifierVM(banner, nineGrid: false);
                    HasClanBanner = true;
                    RexLog.Info($"Builder prisoner clan banner set. bannerCode='{bannerCode}'");
                }
                catch (Exception bannerEx)
                {
                    RexLog.Warning($"Could not create the clan banner image identifier: {bannerEx.Message}");
                    ClanBanner_9 = null;
                    HasClanBanner = false;
                }
            }
            else
            {
                RexLog.Info("Builder prisoner has no bannerCode; HasClanBanner=false.");
                ClanBanner_9 = null;
                HasClanBanner = false;
            }

            PrisonerModel.FillFrom(character, -1, bannerCode);

            // Condemned prisoners wear civilian attire rather than battle armor
            var civilianEquipment = character.FirstCivilianEquipment;
            Equipment presentationEquipment;
            if (civilianEquipment is not null && !civilianEquipment.IsEmpty())
            {
                presentationEquipment = civilianEquipment.Clone(cloneWithoutWeapons: true);
            }
            else
            {
                presentationEquipment = character.Equipment?.Clone(cloneWithoutWeapons: true) ?? new Equipment();
                presentationEquipment[EquipmentIndex.Cape] = default;
            }

            // Ensure all weapon slots are stripped so no weapons or shields clip with bound pose
            for (int i = 0; i <= (int)EquipmentIndex.ExtraWeaponSlot; i++)
            {
                presentationEquipment[(EquipmentIndex)i] = default;
            }

            // Strip helmets / headwear so prisoner's facial expression, hair and neck remain clearly visible
            presentationEquipment[EquipmentIndex.Head] = default;

            // Strip mount and harness so no horse is spawned
            presentationEquipment[EquipmentIndex.Horse] = default;
            presentationEquipment[EquipmentIndex.HorseHarness] = default;

            PrisonerModel.SetEquipment(presentationEquipment);
            PrisonerModel.BodyProperties = character.GetBodyProperties(presentationEquipment, -1).ToString();
            PrisonerModel.MountCreationKey = string.Empty;

            PrisonerModel.IsTableauEnabled = true;
            HasPrisonerModel = !string.IsNullOrWhiteSpace(PrisonerModel.CharStringId) &&
                               !string.IsNullOrWhiteSpace(PrisonerModel.BodyProperties) &&
                               !string.IsNullOrWhiteSpace(PrisonerModel.EquipmentCode);
            RexLog.Info(
                $"Builder prisoner tableau refreshed for '{PrisonerModel.CharStringId}': " +
                $"body={(string.IsNullOrWhiteSpace(PrisonerModel.BodyProperties) ? "empty" : "ready")}, " +
                $"equipment={(string.IsNullOrWhiteSpace(PrisonerModel.EquipmentCode) ? "empty" : "ready")}.");
        }
        catch (Exception exception)
        {
            PrisonerModel.IsTableauEnabled = false;
            HasPrisonerModel = false;
            ClanBanner_9 = null;
            HasClanBanner = false;
            RexLog.Warning($"Could not fill the prisoner tableau: {exception.Message}");
            return;
        }

        StartPrisonerIdleAnimation();
    }

    // Keep this on the native idle path. Custom animation startup can run before
    // the tableau provider has a render target and leave the texture empty.
    private void StartPrisonerIdleAnimation()
    {
        try
        {
            PrisonerModel.ExecuteStopCustomAnimation();
            PrisonerModel.IdleAction = "act_prisoner_conversation_idle_1";
        }
        catch (Exception exception)
        {
            RexLog.Warning($"Could not start the prisoner idle animation: {exception.Message}");
        }
    }

    internal void SetPrisonerSummary(string nameText, string clanText, string chargeText)
    {
        PrisonerNameText = nameText;
        PrisonerClanText = clanText;
        PrisonerChargeText = chargeText;
    }

    internal void SetPrisonerDetails(string titleText, string originText)
    {
        PrisonerTitleText = titleText;
        PrisonerOriginText = originText;
    }

    internal void SetIncidents(IEnumerable<ExecutionSiteBuilderIncidentDefinition> incidents)
    {
        foreach (var existing in Incidents)
        {
            existing.OnFinalize();
        }

        Incidents.Clear();
        foreach (var incident in incidents)
        {
            Incidents.Add(new ExecutionSiteBuilderIncidentVM(incident));
        }

        HasIncidents = Incidents.Count > 0;
    }

    [DataSourceProperty]
    public string MainPanelTitle => new TextObject("{=REX_Builder_Main_Title}Custom execution-site deployment").ToString();
    [DataSourceProperty] public string MainPanelHint => new TextObject("{=REX_Builder_Main_Hint}Choose the punishment, layout and personnel before finishing deployment.").ToString();
    [DataSourceProperty] public string MainPanelCloseText => new TextObject("{=REX_Builder_Main_Close}Esc: close without deploying").ToString();
    [DataSourceProperty] public string NavigationHeaderText => new TextObject("{=REX_Builder_Main_Navigation}STUDIO SECTIONS").ToString();
    [DataSourceProperty] public string SummaryHeaderText => new TextObject("{=REX_Builder_Main_Summary}CURRENT DEPLOYMENT").ToString();
    [DataSourceProperty] public string PersonnelHeaderText => new TextObject("{=REX_Builder_Main_Personnel}CEREMONY PERSONNEL").ToString();
    [DataSourceProperty] public string JudgementWarrantSuffixText => new TextObject("{=REX_Builder_Judgement_Warrant_Suffix}'s public judgement warrant").ToString();
    [DataSourceProperty] public string LayoutAnchorsHeaderText => new TextObject("{=REX_Builder_Layout_Anchors_Header}Execution layout and anchors").ToString();
    [DataSourceProperty] public string StudioHintText => new TextObject("{=REX_Builder_Main_Status}Deployment status").ToString();
    [DataSourceProperty] public string FinishButtonText => new TextObject("{=REX_Builder_Main_Finish_Button}Finish deployment").ToString();
    [DataSourceProperty] public string BackButtonText => new TextObject("{=REX_Builder_Back_Button}Back").ToString();
    [DataSourceProperty] public string PrisonerHeaderText => new TextObject("{=REX_Builder_Prisoner_Header}CONDEMNED").ToString();
    [DataSourceProperty] public string PrisonerChargeLabelText => new TextObject("{=REX_Builder_Prisoner_Charge_Label}Charge").ToString();
    [DataSourceProperty] public string IncidentsHeaderText => new TextObject("{=REX_Builder_Incidents_Header}RECORDED CRIMES").ToString();
    [DataSourceProperty] public string IncidentsEmptyText => new TextObject("{=REX_Builder_Incidents_Empty}No specific incident is recorded against this prisoner.").ToString();
    [DataSourceProperty] public string EmptyIncidentsTitle => new TextObject("{=REX_Builder_NoIncident_Title}Wartime Law & Final Verdict").ToString();
    [DataSourceProperty] public string EmptyIncidentsDescription => new TextObject("{=REX_Builder_NoIncident_Desc}The prisoner has no recorded blood feuds or rebellion pacts. Under imperial statutes, the execution proceeds upon the publicly announced indictment by the presiding officer.").ToString();
    [DataSourceProperty] public string FinishHintText => CanFinishDeployment
        ? new TextObject("{=REX_Builder_Main_Finish_Ready}The draft passed all checks.").ToString()
        : new TextObject("{=REX_Builder_Main_Finish_Hint}Choose a valid layout before finishing deployment.").ToString();
    [DataSourceProperty] public string PanelSubtitleText => new TextObject("{=REX_Builder_Panel_Hint}Select an option to continue configuring this deployment.").ToString();

    [DataSourceProperty]
    public bool IsMainPanelVisible => IsPanelVisible && IsMainPanel;

    [DataSourceProperty]
    public bool IsSubPanelVisible => IsPanelVisible && !IsMainPanel;

    public void ExecuteFinishDeployment() => _onFinishDeployment();

    public void ExecuteBack() => _onBack();

    [DataSourceProperty]
    public bool CanFinishDeployment
    {
        get => _canFinishDeployment;
        private set
        {
            if (_canFinishDeployment == value) return;
            _canFinishDeployment = value;
            OnPropertyChangedWithValue(value, nameof(CanFinishDeployment));
            OnPropertyChanged(nameof(FinishHintText));
        }
    }

    [DataSourceProperty] public string PanelCloseText => new TextObject("{=REX_Builder_Panel_Close}Esc: close").ToString();
    [DataSourceProperty] public MBBindingList<ExecutionSiteBuilderWheelItemVM> WheelItems { get; }
    [DataSourceProperty] public MBBindingList<ExecutionSiteBuilderPanelItemVM> PanelItems { get; }

    internal void ConfigureWheel(
        IEnumerable<ExecutionSiteBuilderWheelDefinition> definitions,
        string defaultName,
        string defaultDescription,
        string footerText)
    {
        WheelItems.Clear();
        foreach (var definition in definitions)
        {
            WheelItems.Add(new ExecutionSiteBuilderWheelItemVM(definition, HighlightWheel, ClickWheel));
        }
        if (WheelItems.Count > 0)
        {
            WheelItems[0].IsSelected = true;
        }

        SelectedWheelName = defaultName;
        SelectedWheelDescription = defaultDescription;
        FooterText = footerText;
    }

    internal void SelectWheelIndex(int index)
    {
        if (index < 0 || index >= WheelItems.Count || WheelItems[index].IsSelected)
        {
            return;
        }

        WheelItems[index].ExecuteHighlight();
    }

    internal void ConfigurePanel(
        string title,
        IEnumerable<ExecutionSiteBuilderPanelDefinition> definitions,
        bool isMainPanel = false)
    {
        PanelTitle = title;
        IsMainPanel = isMainPanel;
        SearchText = string.Empty;
        _allPanelItems.Clear();
        _allPanelItems.AddRange(definitions);
        RebuildPanelItems();
        IsPanelVisible = true;
    }

    internal void ConfigurePanelSummary(
        string methodText,
        string presetText,
        string personnelText,
        string deploymentStateText,
        bool canFinish)
    {
        CurrentMethodText = methodText;
        CurrentPresetText = presetText;
        PersonnelText = personnelText;
        DeploymentStateText = deploymentStateText;
        CanFinishDeployment = canFinish;
    }

    internal void ClosePanel()
    {
        IsPanelVisible = false;
        IsMainPanel = false;
        _allPanelItems.Clear();
        PanelItems.Clear();
        SearchText = string.Empty;
    }

    private void HighlightWheel(ExecutionSiteBuilderWheelDefinition definition)
    {
        foreach (var item in WheelItems)
        {
            item.IsSelected = string.Equals(item.Id, definition.Id, StringComparison.Ordinal);
        }
        SelectedWheelName = definition.Name;
        SelectedWheelDescription = definition.Description;
        _onWheelHighlighted(definition.Id);
    }

    private void ClickWheel(ExecutionSiteBuilderWheelDefinition definition) =>
        _onWheelClicked(definition.Id);

    private void RebuildPanelItems()
    {
        PanelItems.Clear();
        var filter = SearchText.Trim();
        foreach (var item in _allPanelItems.Where(item =>
                     filter.Length == 0 ||
                     item.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                     item.Description.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                     item.Category.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0))
        {
            PanelItems.Add(new ExecutionSiteBuilderPanelItemVM(item, _onPanelSelected));
        }
    }

    public override void OnFinalize()
    {
        WheelItems.Clear();
        PanelItems.Clear();
        foreach (var incident in Incidents)
        {
            incident.OnFinalize();
        }
        Incidents.Clear();
        _allPanelItems.Clear();
        _clanBanner_9 = null;
        _hasClanBanner = false;
        try { PrisonerModel.ExecuteStopCustomAnimation(); } catch { }
        base.OnFinalize();
    }
}

internal sealed class ExecutionSiteBuilderWheelItemVM : ViewModel
{
    private readonly ExecutionSiteBuilderWheelDefinition _definition;
    private readonly Action<ExecutionSiteBuilderWheelDefinition> _onHighlighted;
    private readonly Action<ExecutionSiteBuilderWheelDefinition> _onClicked;
    private bool _isSelected;

    internal ExecutionSiteBuilderWheelItemVM(
        ExecutionSiteBuilderWheelDefinition definition,
        Action<ExecutionSiteBuilderWheelDefinition> onHighlighted,
        Action<ExecutionSiteBuilderWheelDefinition> onClicked)
    {
        _definition = definition;
        _onHighlighted = onHighlighted;
        _onClicked = onClicked;
    }

    internal string Id => _definition.Id;
    [DataSourceProperty] public string Name => _definition.Name;
    [DataSourceProperty] public string StatusText => _definition.StatusText;
    [DataSourceProperty]
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            OnPropertyChangedWithValue(value, nameof(IsSelected));
        }
    }
    public void ExecuteHighlight() => _onHighlighted(_definition);
    public void ExecuteClick() => _onClicked(_definition);
}

internal sealed class ExecutionSiteBuilderPanelItemVM : ViewModel
{
    private readonly ExecutionSiteBuilderPanelDefinition _definition;
    private readonly Action<string> _onSelected;
    private bool _isSelected;

    internal ExecutionSiteBuilderPanelItemVM(
        ExecutionSiteBuilderPanelDefinition definition,
        Action<string> onSelected)
    {
        _definition = definition;
        _onSelected = onSelected;
        _isSelected = definition.IsSelected;
    }

    [DataSourceProperty] public string Name => _definition.Name;
    [DataSourceProperty] public string Description => _definition.Description;
    [DataSourceProperty] public string Category => _definition.Category;
    [DataSourceProperty] public string ResourceKey => _definition.ResourceKey;
    [DataSourceProperty] public int ResourceType => (int)_definition.ResourceType;
    [DataSourceProperty] public bool HasThumbnail => !string.IsNullOrWhiteSpace(_definition.ResourceKey);
    [DataSourceProperty] public bool HasCategory => !string.IsNullOrWhiteSpace(_definition.Category);

    [DataSourceProperty]
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            OnPropertyChangedWithValue(value, nameof(IsSelected));
        }
    }

    public void ExecuteSelect() => _onSelected(_definition.Id);
}

internal sealed class ExecutionSiteBuilderIncidentVM : ViewModel
{
    private readonly ExecutionSiteBuilderIncidentDefinition _definition;

    internal ExecutionSiteBuilderIncidentVM(ExecutionSiteBuilderIncidentDefinition definition)
    {
        _definition = definition;
    }

    [DataSourceProperty] public string ChargeText => _definition.ChargeText;
    [DataSourceProperty] public string RecordText => _definition.RecordText;
    [DataSourceProperty] public string DetailText => _definition.DetailText;
    [DataSourceProperty] public bool IsConclusive => _definition.EvidenceRank >= 2;
    [DataSourceProperty] public bool IsCircumstantial => _definition.EvidenceRank == 1;
    [DataSourceProperty] public bool IsRumourOnly => _definition.EvidenceRank <= 0;
}

public sealed class ExecutionSiteBuilderWheelItemWidget : ButtonWidget
{
    public ExecutionSiteBuilderWheelItemWidget(UIContext context) : base(context)
    {
        if (!ContainsState("Selected")) AddState("Selected");
        if (!ContainsState("Default")) AddState("Default");
    }

    protected override void OnConnectedToRoot()
    {
        base.OnConnectedToRoot();
        boolPropertyChanged += HandleBoolPropertyChanged;
    }

    protected override void OnDisconnectedFromRoot()
    {
        boolPropertyChanged -= HandleBoolPropertyChanged;
        base.OnDisconnectedFromRoot();
    }

    private void HandleBoolPropertyChanged(PropertyOwnerObject widget, string propertyName, bool value)
    {
        if (propertyName != "IsSelected") return;
        SetState(value ? "Selected" : "Default");
        if (value) EventFired("OnHighlighted", Array.Empty<object>());
    }
}
