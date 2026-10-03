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
	[DataSourceProperty] public string TitleText => "繁荣／户数／民兵上限及粮仓容量";
	[DataSourceProperty] public string DescriptionText => "每日按原版＋政策的净变化结算：正数最多加到总量上限；负数照常扣除，超额旧存量不削减。";
	[DataSourceProperty] public string FoodRuleText => "玩家和 NPC 全部适用。默认仅开启城市／城堡粮仓容量1000，其余六项默认关闭；粮仓不叠加城堡、建筑加成。\n繁荣度耗粮始终取消，不受这些开关影响；驻军耗粮、围城、供粮和政策粮食变化保留。";
	[DataSourceProperty] public string RestoreDefaultsText => "恢复默认";
	[DataSourceProperty] public string CancelText => "取消";
	[DataSourceProperty] public string SaveText => "保存并关闭";
	[DataSourceProperty] public MBBindingList<SettlementBalanceRowVM> Rows { get; }
	[DataSourceProperty] public string StatusText
	{
		get => _statusText;
		private set { if (_statusText == value) return; _statusText = value; OnPropertyChangedWithValue(value, nameof(StatusText)); }
	}
	internal void SetSaveFailure(string error) { StatusText = "保存失败：" + error; }
	private void RefreshStatus()
	{
		foreach (var row in Rows)
			if (!row.IsValid) { StatusText = row.NameText + "：" + row.ValidationError; return; }
		StatusText = "未开启的预填数值不生效。保存后应用；取消或 Esc 丢弃编辑；恢复默认后需保存。";
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
	[DataSourceProperty] public string StatusText => Enabled ? "已开启" : "关闭";
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
				ValidationError = "请输入 " + MinValue + "–" + MaxValue + " 之间的整数。";
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
