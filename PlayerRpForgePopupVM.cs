using System;
using System.Globalization;
using System.Text;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AnimusForge;

public sealed class PlayerRpForgePopupVM : ViewModel
{
	private const int MaxItemNameChars = 160;

	private readonly Action<string, int, bool> _onSubmit;

	private readonly Action _onCancel;

	private string _itemName;

	private int _investmentDenars;

	private string _investmentText;

	private bool _forgeAsWeapon;

	private bool _isCrestHovered;

	private bool _isBusy;

	private string _statusText;

	private int _maxInvestmentDenars;

	public PlayerRpForgePopupVM(
		string itemName,
		int investmentDenars,
		bool forgeAsWeapon,
		int maxInvestmentDenars,
		Action<string, int, bool> onSubmit,
		Action onCancel)
	{
		_onSubmit = onSubmit;
		_onCancel = onCancel;
		MaxInvestmentDenars = Math.Max(0, maxInvestmentDenars);
		ItemName = itemName ?? "";
		InvestmentDenars = Math.Max(0, Math.Min(investmentDenars, MaxInvestmentDenars));
		ForgeAsWeapon = forgeAsWeapon;
	}

	[DataSourceProperty]
	public string ItemName
	{
		get => _itemName;
		set
		{
			string sanitized = AnimusForgeTextInputSanitizer.SanitizeSingleLine(value, MaxItemNameChars);
			if (!string.Equals(sanitized, _itemName, StringComparison.Ordinal))
			{
				_itemName = sanitized;
				OnPropertyChangedWithValue(sanitized, nameof(ItemName));
			}
		}
	}

	[DataSourceProperty]
	public int InvestmentDenars
	{
		get => _investmentDenars;
		set
		{
			int clamped = Math.Max(0, Math.Min(value, MaxInvestmentDenars));
			bool amountChanged = clamped != _investmentDenars;
			string text = clamped > 0
				? clamped.ToString(CultureInfo.InvariantCulture)
				: "";
			bool textChanged = !string.Equals(text, _investmentText, StringComparison.Ordinal);
			if (amountChanged)
			{
				_investmentDenars = clamped;
				OnPropertyChangedWithValue(clamped, nameof(InvestmentDenars));
			}
			if (textChanged)
			{
				_investmentText = text;
				OnPropertyChangedWithValue(text, nameof(InvestmentText));
			}
		}
	}

	[DataSourceProperty]
	public string InvestmentText
	{
		get => _investmentText ?? "";
		set
		{
			string incoming = value ?? "";
			StringBuilder digits = new StringBuilder(Math.Min(10, incoming.Length));
			for (int i = 0; i < incoming.Length && digits.Length < 10; i++)
			{
				char character = incoming[i];
				if (character >= '0' && character <= '9')
				{
					digits.Append(character);
				}
			}
			string normalized = digits.ToString();
			long parsed = 0L;
			if (!string.IsNullOrEmpty(normalized)
				&& !long.TryParse(
					normalized,
					NumberStyles.None,
					CultureInfo.InvariantCulture,
					out parsed))
			{
				parsed = MaxInvestmentDenars;
			}
			int amount = (int)Math.Max(0L, Math.Min(parsed, MaxInvestmentDenars));
			if (parsed > MaxInvestmentDenars)
			{
				normalized = amount > 0
					? amount.ToString(CultureInfo.InvariantCulture)
					: "";
			}
			else if (amount == 0 && MaxInvestmentDenars == 0)
			{
				normalized = "";
			}
			bool amountChanged = amount != _investmentDenars;
			bool textChanged =
				!string.Equals(normalized, _investmentText, StringComparison.Ordinal)
				|| !string.Equals(normalized, incoming, StringComparison.Ordinal);
			_investmentDenars = amount;
			_investmentText = normalized;
			if (amountChanged)
			{
				OnPropertyChangedWithValue(amount, nameof(InvestmentDenars));
			}
			if (textChanged)
			{
				OnPropertyChangedWithValue(normalized, nameof(InvestmentText));
			}
		}
	}

	[DataSourceProperty]
	public bool ForgeAsWeapon
	{
		get => _forgeAsWeapon;
		set
		{
			if (value != _forgeAsWeapon)
			{
				_forgeAsWeapon = value;
				OnPropertyChangedWithValue(value, nameof(ForgeAsWeapon));
			}
		}
	}

	[DataSourceProperty]
	public bool IsCrestHovered
	{
		get => _isCrestHovered;
		set
		{
			if (value != _isCrestHovered)
			{
				_isCrestHovered = value;
				OnPropertyChangedWithValue(value, nameof(IsCrestHovered));
			}
		}
	}

	[DataSourceProperty]
	public int MaxInvestmentDenars
	{
		get => _maxInvestmentDenars;
		private set
		{
			int normalized = Math.Max(0, value);
			if (normalized != _maxInvestmentDenars)
			{
				_maxInvestmentDenars = normalized;
				OnPropertyChangedWithValue(normalized, nameof(MaxInvestmentDenars));
			}
		}
	}

	[DataSourceProperty]
	public bool IsBusy
	{
		get => _isBusy;
		private set
		{
			if (value != _isBusy)
			{
				_isBusy = value;
				OnPropertyChangedWithValue(value, nameof(IsBusy));
				OnPropertyChangedWithValue(!value, nameof(CanInteract));
			}
		}
	}

	[DataSourceProperty]
	public bool CanInteract => !IsBusy;

	[DataSourceProperty]
	public string StatusText
	{
		get => _statusText ?? "";
		private set
		{
			string normalized = value ?? "";
			if (!string.Equals(normalized, _statusText, StringComparison.Ordinal))
			{
				_statusText = normalized;
				OnPropertyChangedWithValue(normalized, nameof(StatusText));
			}
		}
	}

	public void ExecuteToggleForgeAsWeapon()
	{
		if (IsBusy)
		{
			return;
		}
		ForgeAsWeapon = !ForgeAsWeapon;
	}

	public void ExecuteSubmit()
	{
		if (IsBusy)
		{
			return;
		}
		string itemName = (ItemName ?? "").Trim();
		if (string.IsNullOrWhiteSpace(itemName))
		{
			InformationManager.DisplayMessage(new InformationMessage("请先填写物品名称。"));
			return;
		}
		if (InvestmentDenars <= 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("制造物品至少需要投入 1 第纳尔。"));
			return;
		}
		if (InvestmentDenars > MaxInvestmentDenars)
		{
			InformationManager.DisplayMessage(new InformationMessage("投入金额不能超过玩家当前持有的第纳尔。"));
			return;
		}
		_onSubmit?.Invoke(itemName, InvestmentDenars, ForgeAsWeapon);
	}

	public void ExecuteCrestHoverBegin()
	{
		IsCrestHovered = true;
	}

	public void ExecuteCrestHoverEnd()
	{
		IsCrestHovered = false;
	}

	public void ExecuteCancel()
	{
		_onCancel?.Invoke();
	}

	internal void SetBusy(bool isBusy, string statusText)
	{
		IsBusy = isBusy;
		StatusText = isBusy ? statusText ?? "" : "";
	}

	public void StartTyping()
	{
	}

	public void StopTyping()
	{
	}
}
