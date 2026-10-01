using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RichExecutions.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace RichExecutions.UI;

internal sealed class ExecutionJudgementVM : ViewModel
{
    private readonly Settlement _venue;
    private readonly IReadOnlyList<ExecutionChargeDefinition> _chargeDefinitions;
    private readonly Func<Hero, ExecutionChargeDefinition, ExecutionEvidencePresentation> _evidenceProvider;
    private readonly Func<Hero, IReadOnlyList<ExecutionCrimeIncidentPresentation>> _crimeHistoryProvider;
    private readonly Action<ExecutionJudgementSelection> _onExecute;
    private readonly Action _onClose;
    private readonly int _influenceCost;

    private MBBindingList<ExecutionPrisonerItemVM> _prisoners = new();
    private MBBindingList<ExecutionChargeItemVM> _charges = new();
    private MBBindingList<ExecutionCrimeIncidentItemVM> _crimeHistory = new();
    private MBBindingList<ExecutionMethodItemVM> _methods = new();
    private MBBindingList<ExecutionToneItemVM> _tones = new();
    private ExecutionPrisonerItemVM? _selectedPrisoner;
    private ExecutionChargeItemVM? _selectedCharge;
    private ExecutionMethodItemVM? _selectedMethod;
    private ExecutionToneItemVM? _selectedTone;
    private string _titleText = string.Empty;
    private string _subtitleText = string.Empty;
    private string _prisonersHeaderText = string.Empty;
    private string _crimesHeaderText = string.Empty;
    private string _methodsHeaderText = string.Empty;
    private string _toneHeaderText = string.Empty;
    private string _crimeHistoryHeaderText = string.Empty;
    private string _crimeHistoryEmptyText = string.Empty;
    private string _selectedCrimeText = string.Empty;
    private string _selectedCrimeDetailText = string.Empty;
    private string _selectedMethodDescriptionText = string.Empty;
    private string _legitimacyText = string.Empty;
    private string _consequencesText = string.Empty;
    private string _authorityCostText = string.Empty;
    private string _executeText = string.Empty;
    private string _closeHintText = string.Empty;
    private bool _canExecute;
    private bool _hasCrimeHistory;
    private bool _isCrimeHistoryEmpty = true;

    public ExecutionJudgementVM(
        Settlement venue,
        IReadOnlyList<ExecutionPrisonerOption> prisonerOptions,
        IReadOnlyList<ExecutionMethodDefinition> methodDefinitions,
        IReadOnlyList<ExecutionChargeDefinition> chargeDefinitions,
        Func<Hero, ExecutionChargeDefinition, ExecutionEvidencePresentation> evidenceProvider,
        Func<Hero, IReadOnlyList<ExecutionCrimeIncidentPresentation>> crimeHistoryProvider,
        int influenceCost,
        Action<ExecutionJudgementSelection> onExecute,
        Action onClose)
    {
        _venue = venue;
        _chargeDefinitions = chargeDefinitions;
        _evidenceProvider = evidenceProvider;
        _crimeHistoryProvider = crimeHistoryProvider;
        _influenceCost = influenceCost;
        _onExecute = onExecute;
        _onClose = onClose;

        TitleText = Localize("{=REX_UI_Title}Public trial and execution");
        var subtitle = new TextObject("{=REX_UI_Subtitle}Court of {TOWN} · choose the condemned, the charge, and the sentence");
        subtitle.SetTextVariable("TOWN", venue.Name);
        SubtitleText = subtitle.ToString();
        PrisonersHeaderText = Localize("{=REX_UI_Prisoners_Header}Condemned lords");
        CrimesHeaderText = Localize("{=REX_UI_Crimes_Header}Recorded crimes and charge");
        MethodsHeaderText = Localize("{=REX_UI_Methods_Header}Method of execution");
        ToneHeaderText = Localize("{=REX_UI_Tone_Header}Ceremonial tone");
        CrimeHistoryHeaderText = Localize("{=REX_UI_Crime_History_Header}Recorded incidents");
        CrimeHistoryEmptyText = Localize("{=REX_UI_Crime_History_Empty}No specific incident is recorded");
        ExecuteText = Localize("{=REX_UI_Execute}Hold the execution");
        CloseHintText = Localize("{=REX_UI_Close_Hint}Esc closes this court without cost or death");

        Prisoners = new MBBindingList<ExecutionPrisonerItemVM>();
        foreach (var option in prisonerOptions)
        {
            Prisoners.Add(new ExecutionPrisonerItemVM(
                option,
                _chargeDefinitions,
                _evidenceProvider,
                SelectPrisoner));
        }

        Methods = new MBBindingList<ExecutionMethodItemVM>();
        foreach (var method in methodDefinitions)
        {
            if (!ExecutionMethodRules.IsVisibleInSelection(method.StringId)) continue;
            Methods.Add(new ExecutionMethodItemVM(method, SelectMethod));
        }

        Tones = new MBBindingList<ExecutionToneItemVM>
        {
            new(ExecutionTone.Judicial, SelectTone),
            new(ExecutionTone.Spectacle, SelectTone),
            new(ExecutionTone.Terror, SelectTone)
        };

        if (Prisoners.Count > 0)
        {
            SelectPrisoner(Prisoners[0]);
        }

        var beheading = Methods.FirstOrDefault(item =>
            string.Equals(item.Method.StringId, ExecutionMethodRules.Beheading, StringComparison.OrdinalIgnoreCase));
        if (beheading is not null)
        {
            SelectMethod(beheading);
        }
        else if (Methods.Count > 0)
        {
            SelectMethod(Methods[0]);
        }

        SelectTone(Tones[0]);
        RefreshSummary();
    }

    [DataSourceProperty]
    public MBBindingList<ExecutionPrisonerItemVM> Prisoners
    {
        get => _prisoners;
        set
        {
            if (!ReferenceEquals(_prisoners, value))
            {
                _prisoners = value;
                OnPropertyChangedWithValue(value, nameof(Prisoners));
            }
        }
    }

    [DataSourceProperty]
    public MBBindingList<ExecutionChargeItemVM> Charges
    {
        get => _charges;
        set
        {
            if (!ReferenceEquals(_charges, value))
            {
                _charges = value;
                OnPropertyChangedWithValue(value, nameof(Charges));
            }
        }
    }

    [DataSourceProperty]
    public MBBindingList<ExecutionCrimeIncidentItemVM> CrimeHistory
    {
        get => _crimeHistory;
        set
        {
            if (!ReferenceEquals(_crimeHistory, value))
            {
                _crimeHistory = value;
                OnPropertyChangedWithValue(value, nameof(CrimeHistory));
            }
        }
    }

    [DataSourceProperty]
    public MBBindingList<ExecutionMethodItemVM> Methods
    {
        get => _methods;
        set
        {
            if (!ReferenceEquals(_methods, value))
            {
                _methods = value;
                OnPropertyChangedWithValue(value, nameof(Methods));
            }
        }
    }

    [DataSourceProperty]
    public MBBindingList<ExecutionToneItemVM> Tones
    {
        get => _tones;
        set
        {
            if (!ReferenceEquals(_tones, value))
            {
                _tones = value;
                OnPropertyChangedWithValue(value, nameof(Tones));
            }
        }
    }

    [DataSourceProperty]
    public string TitleText
    {
        get => _titleText;
        set => SetField(ref _titleText, value, nameof(TitleText));
    }

    [DataSourceProperty]
    public string SubtitleText
    {
        get => _subtitleText;
        set => SetField(ref _subtitleText, value, nameof(SubtitleText));
    }

    [DataSourceProperty]
    public string PrisonersHeaderText
    {
        get => _prisonersHeaderText;
        set => SetField(ref _prisonersHeaderText, value, nameof(PrisonersHeaderText));
    }

    [DataSourceProperty]
    public string CrimesHeaderText
    {
        get => _crimesHeaderText;
        set => SetField(ref _crimesHeaderText, value, nameof(CrimesHeaderText));
    }

    [DataSourceProperty]
    public string MethodsHeaderText
    {
        get => _methodsHeaderText;
        set => SetField(ref _methodsHeaderText, value, nameof(MethodsHeaderText));
    }

    [DataSourceProperty]
    public string ToneHeaderText
    {
        get => _toneHeaderText;
        set => SetField(ref _toneHeaderText, value, nameof(ToneHeaderText));
    }

    [DataSourceProperty]
    public string CrimeHistoryHeaderText
    {
        get => _crimeHistoryHeaderText;
        set => SetField(ref _crimeHistoryHeaderText, value, nameof(CrimeHistoryHeaderText));
    }

    [DataSourceProperty]
    public string CrimeHistoryEmptyText
    {
        get => _crimeHistoryEmptyText;
        set => SetField(ref _crimeHistoryEmptyText, value, nameof(CrimeHistoryEmptyText));
    }

    [DataSourceProperty]
    public string SelectedCrimeText
    {
        get => _selectedCrimeText;
        set => SetField(ref _selectedCrimeText, value, nameof(SelectedCrimeText));
    }

    [DataSourceProperty]
    public string SelectedCrimeDetailText
    {
        get => _selectedCrimeDetailText;
        set => SetField(ref _selectedCrimeDetailText, value, nameof(SelectedCrimeDetailText));
    }

    [DataSourceProperty]
    public string SelectedMethodDescriptionText
    {
        get => _selectedMethodDescriptionText;
        set => SetField(ref _selectedMethodDescriptionText, value, nameof(SelectedMethodDescriptionText));
    }

    [DataSourceProperty]
    public string LegitimacyText
    {
        get => _legitimacyText;
        set => SetField(ref _legitimacyText, value, nameof(LegitimacyText));
    }

    [DataSourceProperty]
    public string ConsequencesText
    {
        get => _consequencesText;
        set => SetField(ref _consequencesText, value, nameof(ConsequencesText));
    }

    [DataSourceProperty]
    public string AuthorityCostText
    {
        get => _authorityCostText;
        set => SetField(ref _authorityCostText, value, nameof(AuthorityCostText));
    }

    [DataSourceProperty]
    public string ExecuteText
    {
        get => _executeText;
        set => SetField(ref _executeText, value, nameof(ExecuteText));
    }

    [DataSourceProperty]
    public string CloseHintText
    {
        get => _closeHintText;
        set => SetField(ref _closeHintText, value, nameof(CloseHintText));
    }

    [DataSourceProperty]
    public bool CanExecute
    {
        get => _canExecute;
        set => SetField(ref _canExecute, value, nameof(CanExecute));
    }

    [DataSourceProperty]
    public bool HasCrimeHistory
    {
        get => _hasCrimeHistory;
        set => SetField(ref _hasCrimeHistory, value, nameof(HasCrimeHistory));
    }

    [DataSourceProperty]
    public bool IsCrimeHistoryEmpty
    {
        get => _isCrimeHistoryEmpty;
        set => SetField(ref _isCrimeHistoryEmpty, value, nameof(IsCrimeHistoryEmpty));
    }

    public void ExecuteClose() => _onClose();

    public void ExecuteHoldNow()
    {
        if (!CanExecute || _selectedPrisoner is null || _selectedCharge is null ||
            _selectedMethod is null || _selectedTone is null)
        {
            return;
        }

        _onExecute(new ExecutionJudgementSelection(
            _selectedPrisoner.Option.Hero,
            _selectedPrisoner.Option.Source,
            _selectedMethod.Method,
            _selectedCharge.Charge,
            _selectedTone.Tone));
    }

    public override void OnFinalize()
    {
        foreach (var prisoner in Prisoners)
        {
            prisoner.OnFinalize();
        }

        foreach (var charge in Charges)
        {
            charge.OnFinalize();
        }

        foreach (var incident in CrimeHistory)
        {
            incident.OnFinalize();
        }

        foreach (var method in Methods)
        {
            method.OnFinalize();
        }

        foreach (var tone in Tones)
        {
            tone.OnFinalize();
        }

        base.OnFinalize();
    }

    private void SelectPrisoner(ExecutionPrisonerItemVM item)
    {
        _selectedPrisoner = item;
        foreach (var prisoner in Prisoners)
        {
            prisoner.IsSelected = ReferenceEquals(prisoner, item);
        }

        RebuildCharges(item.Option.Hero);
        RebuildCrimeHistory(item.Option.Hero);
        RefreshSummary();
    }

    private void RebuildCharges(Hero victim)
    {
        foreach (var existing in Charges)
        {
            existing.OnFinalize();
        }

        var rebuilt = new MBBindingList<ExecutionChargeItemVM>();
        foreach (var charge in _chargeDefinitions)
        {
            ExecutionEvidencePresentation presentation;
            try
            {
                presentation = _evidenceProvider(victim, charge);
            }
            catch
            {
                presentation = new ExecutionEvidencePresentation(
                    EvidenceStrength.None,
                    Localize("{=REX_UI_Crime_No_Record}No verified incident is recorded"),
                    Localize("{=REX_Evidence_None}no evidence"));
            }

            rebuilt.Add(new ExecutionChargeItemVM(charge, presentation, SelectCharge));
        }

        Charges = rebuilt;
        var preferred = Charges
            .OrderByDescending(item => (int)item.Presentation.Strength)
            .FirstOrDefault();
        if (preferred is not null)
        {
            SelectCharge(preferred);
        }
        else
        {
            _selectedCharge = null;
            SelectedCrimeText = string.Empty;
            SelectedCrimeDetailText = string.Empty;
        }
    }

    private void RebuildCrimeHistory(Hero victim)
    {
        foreach (var existing in CrimeHistory)
        {
            existing.OnFinalize();
        }

        IReadOnlyList<ExecutionCrimeIncidentPresentation> incidents;
        try
        {
            incidents = _crimeHistoryProvider(victim) ?? Array.Empty<ExecutionCrimeIncidentPresentation>();
        }
        catch
        {
            incidents = Array.Empty<ExecutionCrimeIncidentPresentation>();
        }

        var rebuilt = new MBBindingList<ExecutionCrimeIncidentItemVM>();
        foreach (var incident in incidents
                     .OrderByDescending(item => item.CampaignDay)
                     .ThenByDescending(item => (int)item.Strength))
        {
            rebuilt.Add(new ExecutionCrimeIncidentItemVM(incident));
        }

        CrimeHistory = rebuilt;
        HasCrimeHistory = CrimeHistory.Count > 0;
        IsCrimeHistoryEmpty = !HasCrimeHistory;
    }

    private void SelectCharge(ExecutionChargeItemVM item)
    {
        _selectedCharge = item;
        foreach (var charge in Charges)
        {
            charge.IsSelected = ReferenceEquals(charge, item);
        }

        SelectedCrimeText = item.CrimeText;
        SelectedCrimeDetailText = item.DetailText;
        RefreshSummary();
    }

    private void SelectMethod(ExecutionMethodItemVM item)
    {
        _selectedMethod = item;
        foreach (var method in Methods)
        {
            method.IsSelected = ReferenceEquals(method, item);
        }

        SelectedMethodDescriptionText = item.Method.GetDescription().ToString();
        RefreshSummary();
    }

    private void SelectTone(ExecutionToneItemVM item)
    {
        _selectedTone = item;
        foreach (var tone in Tones)
        {
            tone.IsSelected = ReferenceEquals(tone, item);
        }

        RefreshSummary();
    }

    private void RefreshSummary()
    {
        CanExecute = _selectedPrisoner is not null && _selectedCharge is not null &&
                     _selectedMethod is not null && _selectedTone is not null;
        if (!CanExecute)
        {
            LegitimacyText = Localize("{=REX_UI_Select_All}Complete all three columns to review the sentence");
            ConsequencesText = string.Empty;
            AuthorityCostText = string.Empty;
            return;
        }

        var victim = _selectedPrisoner!.Option.Hero;
        var method = _selectedMethod!.Method;
        var charge = _selectedCharge!;
        var tone = _selectedTone!.Tone;
        var input = new LegitimacyInput(
            ExecutionService.GetVenueAuthority(_venue),
            ExecutionService.GetVictimPoliticalStatus(victim),
            charge.Presentation.Strength,
            tone,
            method.StringId,
            victim.IsLord);
        var score = ExecutionRuleMath.CalculateLegitimacyScore(input);
        var tier = ExecutionRuleMath.GetLegitimacyTier(score);
        var deltas = ExecutionRuleMath.CalculateConsequences(tier, tone, method.StringId, influenceCost: 0);

        var legitimacy = new TextObject("{=REX_UI_Legitimacy}Legitimacy: {TIER} · Evidence: {EVIDENCE}");
        legitimacy.SetTextVariable("TIER", GetTierName(tier));
        legitimacy.SetTextVariable("EVIDENCE", GetEvidenceName(charge.Presentation.Strength));
        LegitimacyText = legitimacy.ToString();

        var firstConsequencesLine = new TextObject(
            "{=REX_UI_Consequences_A}Security {SECURITY} · Loyalty {LOYALTY} · Influence {INFLUENCE}");
        firstConsequencesLine.SetTextVariable("SECURITY", FormatRange(deltas.Security));
        firstConsequencesLine.SetTextVariable("LOYALTY", FormatRange(deltas.Loyalty));
        firstConsequencesLine.SetTextVariable("INFLUENCE", FormatRange(deltas.Influence));

        var secondConsequencesLine = new TextObject(
            "{=REX_UI_Consequences_B}Honor {HONOR} · Mercy {MERCY} · Local relations {RELATION}");
        secondConsequencesLine.SetTextVariable("HONOR", FormatRange(deltas.HonorXp));
        secondConsequencesLine.SetTextVariable("MERCY", FormatRange(deltas.MercyXp));
        secondConsequencesLine.SetTextVariable("RELATION", FormatRange(deltas.LocalRelation));
        ConsequencesText = firstConsequencesLine.ToString() +
                           System.Environment.NewLine +
                           secondConsequencesLine.ToString();

        var cost = new TextObject("{=REX_UI_Authority_Cost}Authority cost: {COST}");
        cost.SetTextVariable(
            "COST",
            _influenceCost == 0
                ? new TextObject("{=REX_Cost_Free}Free in your own town")
                : new TextObject("{=!}" + _influenceCost.ToString(CultureInfo.InvariantCulture)));
        AuthorityCostText = cost.ToString();
    }

    private static string Localize(string text) => new TextObject(text).ToString();

    private static TextObject GetToneName(ExecutionTone tone) => tone switch
    {
        ExecutionTone.Judicial => new TextObject("{=REX_Tone_Judicial}Judicial sentence"),
        ExecutionTone.Spectacle => new TextObject("{=REX_Tone_Spectacle}Grand spectacle"),
        ExecutionTone.Terror => new TextObject("{=REX_Tone_Terror}Terror and intimidation"),
        _ => new TextObject("{=REX_Unknown}Unknown")
    };

    private static TextObject GetEvidenceName(EvidenceStrength evidence) => evidence switch
    {
        EvidenceStrength.Strong => new TextObject("{=REX_Evidence_Strong}strong evidence"),
        EvidenceStrength.Circumstantial => new TextObject("{=REX_Evidence_Circumstantial}circumstantial evidence"),
        _ => new TextObject("{=REX_Evidence_None}no evidence")
    };

    private static TextObject GetTierName(LegitimacyTier tier) => tier switch
    {
        LegitimacyTier.Legal => new TextObject("{=REX_Tier_Legal}legal"),
        LegitimacyTier.Disputed => new TextObject("{=REX_Tier_Disputed}disputed"),
        _ => new TextObject("{=REX_Tier_Illegal}illegal")
    };

    private static string FormatRange(float value)
    {
        if (Math.Abs(value) < 0.001f)
        {
            return "0";
        }

        var absolute = Math.Abs(value);
        var low = Math.Max(1, (int)Math.Floor(absolute * 0.8f));
        var high = Math.Max(low, (int)Math.Ceiling(absolute * 1.2f));
        return value > 0f ? $"+{low}–+{high}" : $"-{high}–-{low}";
    }

    internal sealed class ExecutionPrisonerItemVM : ViewModel
    {
        private readonly Action<ExecutionPrisonerItemVM> _onSelect;
        private bool _isSelected;

        public ExecutionPrisonerItemVM(
            ExecutionPrisonerOption option,
            IReadOnlyList<ExecutionChargeDefinition> chargeDefinitions,
            Func<Hero, ExecutionChargeDefinition, ExecutionEvidencePresentation> evidenceProvider,
            Action<ExecutionPrisonerItemVM> onSelect)
        {
            Option = option;
            _onSelect = onSelect;
            Portrait = new CharacterImageIdentifierVM(CharacterCode.CreateFrom(option.Hero.CharacterObject));
            NameText = option.Hero.Name.ToString();
            ClanText = option.Hero.Clan?.Name.ToString() ?? Localize("{=REX_No_Clan}No clan");
            CustodyText = option.Source == PrisonerSource.PlayerParty
                ? Localize("{=REX_Source_Party}Player party")
                : Localize("{=REX_Source_Dungeon}Town dungeon");
            var relation = new TextObject("{=REX_UI_Relation}Relation: {VALUE}");
            relation.SetTextVariable("VALUE", Hero.MainHero.GetRelation(option.Hero));
            RelationText = relation.ToString();

            var supportedCrimes = new List<ExecutionEvidencePresentation>();
            foreach (var charge in chargeDefinitions)
            {
                try
                {
                    var presentation = evidenceProvider(option.Hero, charge);
                    if (presentation.Strength != EvidenceStrength.None &&
                        !string.IsNullOrWhiteSpace(presentation.RecordText))
                    {
                        supportedCrimes.Add(presentation);
                    }
                }
                catch
                {
                    // One third-party or stale evidence record must not prevent
                    // the remaining prisoners from being shown in the court.
                }
            }

            var orderedCrimes = supportedCrimes
                .OrderByDescending(presentation => (int)presentation.Strength)
                .ThenBy(presentation => presentation.RecordText, StringComparer.CurrentCulture)
                .GroupBy(presentation => presentation.RecordText, StringComparer.CurrentCulture)
                .Select(group => group.First())
                .ToList();
            HasCrimeEvidence = orderedCrimes.Count > 0;
            if (!HasCrimeEvidence)
            {
                CrimeEvidenceText = GetEvidenceName(EvidenceStrength.None).ToString();
                CrimePrimaryText = Localize("{=REX_UI_Crime_No_Record}No verified incident is recorded");
                CrimeSecondaryText = string.Empty;
            }
            else
            {
                CrimeEvidenceText = orderedCrimes[0].DetailText;
                CrimePrimaryText = string.Join(" · ", orderedCrimes.Take(2).Select(item => item.RecordText));
                CrimeSecondaryText = string.Join(" · ", orderedCrimes.Skip(2).Select(item => item.RecordText));
            }
        }

        public ExecutionPrisonerOption Option { get; }

        [DataSourceProperty]
        public CharacterImageIdentifierVM Portrait { get; }

        [DataSourceProperty]
        public string NameText { get; }

        [DataSourceProperty]
        public string ClanText { get; }

        [DataSourceProperty]
        public string CustodyText { get; }

        [DataSourceProperty]
        public string RelationText { get; }

        [DataSourceProperty]
        public string CrimeEvidenceText { get; }

        [DataSourceProperty]
        public string CrimePrimaryText { get; }

        [DataSourceProperty]
        public string CrimeSecondaryText { get; }

        [DataSourceProperty]
        public bool HasCrimeEvidence { get; }

        [DataSourceProperty]
        public bool IsSelected
        {
            get => _isSelected;
            set => SetField(ref _isSelected, value, nameof(IsSelected));
        }

        public void ExecuteSelect() => _onSelect(this);

        public override void OnFinalize()
        {
            Portrait.OnFinalize();
            base.OnFinalize();
        }
    }

    internal sealed class ExecutionCrimeIncidentItemVM : ViewModel
    {
        public ExecutionCrimeIncidentItemVM(ExecutionCrimeIncidentPresentation incident)
        {
            ChargeText = incident.ChargeText;
            RecordText = incident.RecordText;
            DetailText = incident.DetailText;
            HasStrongEvidence = incident.Strength == EvidenceStrength.Strong;
        }

        [DataSourceProperty]
        public string ChargeText { get; }

        [DataSourceProperty]
        public string RecordText { get; }

        [DataSourceProperty]
        public string DetailText { get; }

        [DataSourceProperty]
        public bool HasStrongEvidence { get; }
    }

    internal sealed class ExecutionChargeItemVM : ViewModel
    {
        private readonly Action<ExecutionChargeItemVM> _onSelect;
        private bool _isSelected;

        public ExecutionChargeItemVM(
            ExecutionChargeDefinition charge,
            ExecutionEvidencePresentation presentation,
            Action<ExecutionChargeItemVM> onSelect)
        {
            Charge = charge;
            Presentation = presentation;
            _onSelect = onSelect;
            NameText = charge.GetName().ToString();
            CrimeText = presentation.RecordText;
            DetailText = presentation.DetailText;
            EvidenceText = GetEvidenceName(presentation.Strength).ToString();
            HasEvidence = presentation.Strength != EvidenceStrength.None;
        }

        public ExecutionChargeDefinition Charge { get; }
        public ExecutionEvidencePresentation Presentation { get; }

        [DataSourceProperty]
        public string NameText { get; }

        [DataSourceProperty]
        public string CrimeText { get; }

        [DataSourceProperty]
        public string DetailText { get; }

        [DataSourceProperty]
        public string EvidenceText { get; }

        [DataSourceProperty]
        public bool HasEvidence { get; }

        [DataSourceProperty]
        public bool IsSelected
        {
            get => _isSelected;
            set => SetField(ref _isSelected, value, nameof(IsSelected));
        }

        public void ExecuteSelect() => _onSelect(this);
    }

    internal sealed class ExecutionMethodItemVM : ViewModel
    {
        private readonly Action<ExecutionMethodItemVM> _onSelect;
        private bool _isSelected;

        public ExecutionMethodItemVM(ExecutionMethodDefinition method, Action<ExecutionMethodItemVM> onSelect)
        {
            Method = method;
            _onSelect = onSelect;
            NameText = method.GetName().ToString();
        }

        public ExecutionMethodDefinition Method { get; }

        [DataSourceProperty]
        public string NameText { get; }

        [DataSourceProperty]
        public bool IsSelected
        {
            get => _isSelected;
            set => SetField(ref _isSelected, value, nameof(IsSelected));
        }

        public void ExecuteSelect() => _onSelect(this);
    }

    internal sealed class ExecutionToneItemVM : ViewModel
    {
        private readonly Action<ExecutionToneItemVM> _onSelect;
        private bool _isSelected;

        public ExecutionToneItemVM(ExecutionTone tone, Action<ExecutionToneItemVM> onSelect)
        {
            Tone = tone;
            _onSelect = onSelect;
            NameText = GetToneName(tone).ToString();
        }

        public ExecutionTone Tone { get; }

        [DataSourceProperty]
        public string NameText { get; }

        [DataSourceProperty]
        public bool IsSelected
        {
            get => _isSelected;
            set => SetField(ref _isSelected, value, nameof(IsSelected));
        }

        public void ExecuteSelect() => _onSelect(this);
    }
}
