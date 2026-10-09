using System;
using System.Globalization;
using TaleWorlds.Library;

namespace AnimusForge;

internal sealed class SettlementBalanceEditorVM : ViewModel
{
	private readonly Action _save;
	private readonly Action _cancel;
	private string _statusText;
	internal SettlementBalanceEditorVM(SettlementBalanceSnapshot snapshot, Action save, Action cancel)
	{
		_save = save;
		_cancel = cancel;
		Rows = new MBBindingList<SettlementBalanceRowVM>();
		foreach (var definition in SettlementBalanceRules.Definitions)
			Rows.Add(new SettlementBalanceRowVM(definition, snapshot.Get(definition.Metric), RefreshStatus));
		RefreshStatus();
	}
	[DataSourceProperty] public string TitleText => PolicyUiTexts.Get("AFPOL_UI_BalanceTitle", "Settlement Balance Limits");
	[DataSourceProperty] public string DescriptionText => PolicyUiTexts.Get("AFPOL_UI_BalanceDescription", "Each day, native and policy net changes are applied: positive changes are capped at the total limit; negative changes still apply, and existing stock above a new cap is not reduced.");
	[DataSourceProperty] public string FoodRuleText => PolicyUiTexts.Get("AFPOL_UI_BalanceFoodRule", "Applies to players and NPCs. By default only city/castle food capacity 1000 is enabled; the other six limits are disabled. Prosperity food consumption remains controlled by its separate MCM switch. Garrison food, sieges, supply, and policy food changes are preserved.");
	[DataSourceProperty] public string RestoreDefaultsText => PolicyUiTexts.Get("AFPOL_UI_RestoreDefaults", "Restore Defaults");
	[DataSourceProperty] public string CancelText => PolicyUiTexts.Get("AFPOL_UI_Cancel", "Cancel");
	[DataSourceProperty] public string SaveText => PolicyUiTexts.Get("AFPOL_UI_SaveAndClose", "Save and Close");
	[DataSourceProperty] public MBBindingList<SettlementBalanceRowVM> Rows { get; }
	[DataSourceProperty] public string StatusText
	{
		get => _statusText;
		private set { if (_statusText == value) return; _statusText = value; OnPropertyChangedWithValue(value, nameof(StatusText)); }
	}
	internal void SetSaveFailure(string error) { StatusText = PolicyUiTexts.Format("AFPOL_UI_SaveFailed", "Save failed: {ERROR}", "ERROR", error); }
	private void RefreshStatus()
	{
		foreach (var row in Rows)
			if (!row.IsValid) { StatusText = row.NameText + "：" + row.ValidationError; return; }
		StatusText = PolicyUiTexts.Get("AFPOL_UI_BalanceInitialStatus", "Disabled prefilled values have no effect. Changes apply after saving; Cancel or Esc discards edits, and restored defaults must be saved.");
	}
	internal bool TryCreateSnapshot(out SettlementBalanceSnapshot snapshot, out string error)
	{
		snapshot = SettlementBalanceSnapshot.Default;
		foreach (var row in Rows)
		{
			if (!row.IsValid) { error = row.NameText + "：" + row.ValidationError; return false; }
			snapshot = snapshot.With(row.Metric, row.Enabled, row.ValueInt);
		}
		return snapshot.TryValidate(out error);
	}
	public void ExecuteSave() { _save?.Invoke(); }
	public void ExecuteCancel() { _cancel?.Invoke(); }
	public void ExecuteRestoreDefaults()
	{
		foreach (var row in Rows) row.RestoreDefault();
		RefreshStatus();
	}
}

internal sealed class SettlementBalanceRowVM : ViewModel
{
	private readonly SettlementBalanceDefinition _definition;
	private readonly Action _changed;
	private bool _enabled;
	private int _value;
	private string _text;
	internal SettlementBalanceRowVM(SettlementBalanceDefinition definition, SettlementBalanceLimit limit, Action changed)
	{
		_definition = definition;
		_changed = changed;
		_enabled = limit.Enabled;
		_value = limit.Value;
		_text = limit.Value.ToString(CultureInfo.InvariantCulture);
	}
	internal SettlementBalanceMetric Metric => _definition.Metric;
	internal bool IsValid => string.IsNullOrEmpty(ValidationError);
	internal string ValidationError { get; private set; }
	[DataSourceProperty] public string NameText => _definition.Name;
	[DataSourceProperty] public string UnitText => _definition.Unit + "；范围 " + MinValue + "–" + MaxValue;
	[DataSourceProperty] public int MinValue => _definition.Minimum;
	[DataSourceProperty] public int MaxValue => _definition.Maximum;
	[DataSourceProperty] public bool Enabled
	{
		get => _enabled;
		set
		{
			if (_enabled == value) return;
			_enabled = value;
			OnPropertyChangedWithValue(value, nameof(Enabled));
			OnPropertyChangedWithValue(StatusText, nameof(StatusText));
			if (!value) SetValue(_value); // Discard incomplete text when disabling the row.
			_changed?.Invoke();
		}
	}
	[DataSourceProperty] public string StatusText => Enabled ? PolicyUiTexts.Get("AFPOL_UI_Enabled", "Enabled") : PolicyUiTexts.Get("AFPOL_UI_Disabled", "Disabled");
	[DataSourceProperty] public int ValueInt
	{
		get => _value;
		set
		{
			int normalized = Math.Max(MinValue, Math.Min(MaxValue, value));
			if (normalized != _value) { SetValue(normalized); _changed?.Invoke(); }
		}
	}
	[DataSourceProperty] public string ValueText
	{
		get => _text;
		set
		{
			if (_text == value) return;
			_text = value ?? string.Empty;
			OnPropertyChangedWithValue(_text, nameof(ValueText));
			if (!int.TryParse(_text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number) || number < MinValue || number > MaxValue)
				ValidationError = PolicyUiTexts.Format("AFPOL_UI_BalanceIntegerRange", "Enter an integer between {MIN} and {MAX}.", "MIN", MinValue.ToString(CultureInfo.InvariantCulture), "MAX", MaxValue.ToString(CultureInfo.InvariantCulture));
			else
			{
				ValidationError = string.Empty;
				_value = number;
				OnPropertyChangedWithValue(number, nameof(ValueInt));
			}
			_changed?.Invoke();
		}
	}
	private void SetValue(int value)
	{
		_value = value;
		_text = value.ToString(CultureInfo.InvariantCulture);
		ValidationError = string.Empty;
		OnPropertyChangedWithValue(_value, nameof(ValueInt));
		OnPropertyChangedWithValue(_text, nameof(ValueText));
	}
	internal void RestoreDefault() { Enabled = _definition.DefaultEnabled; SetValue(_definition.DefaultValue); }
	public void ExecuteToggle() { Enabled = !Enabled; }
}
