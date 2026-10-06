using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond.Cosmetics;
using TaleWorlds.MountAndBlade.Diamond.Cosmetics.CosmeticTypes;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Armory.CosmeticItem;

public class MPArmoryCosmeticTauntItemVM : MPArmoryCosmeticItemBaseVM
{
	private bool _isSelected;

	private bool _requiresOnFoot;

	private float _previewAnimationRatio;

	private string _selectSlotText;

	private string _cancelEquipText;

	private string _tauntId;

	private HintViewModel _blocksMovementOnUsageHint;

	private MBBindingList<StringItemWithEnabledAndHintVM> _tauntUsages;

	public MPArmoryCosmeticsVM.TauntCategoryFlag TauntCategory { get; }

	public TauntCosmeticElement TauntCosmeticElement { get; }

	[DataSourceProperty]
	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			if (value != _isSelected)
			{
				_isSelected = value;
				OnPropertyChangedWithValue(value, "IsSelected");
				UpdatePreviewAndActionTexts();
			}
		}
	}

	[DataSourceProperty]
	public bool RequiresOnFoot
	{
		get
		{
			return _requiresOnFoot;
		}
		set
		{
			if (value != _requiresOnFoot)
			{
				_requiresOnFoot = value;
				OnPropertyChangedWithValue(value, "RequiresOnFoot");
			}
		}
	}

	[DataSourceProperty]
	public float PreviewAnimationRatio
	{
		get
		{
			return _previewAnimationRatio;
		}
		set
		{
			if (value != _previewAnimationRatio)
			{
				_previewAnimationRatio = value;
				OnPropertyChangedWithValue(value, "PreviewAnimationRatio");
			}
		}
	}

	[DataSourceProperty]
	public string SelectSlotText
	{
		get
		{
			return _selectSlotText;
		}
		set
		{
			if (value != _selectSlotText)
			{
				_selectSlotText = value;
				OnPropertyChangedWithValue(value, "SelectSlotText");
				UpdatePreviewAndActionTexts();
			}
		}
	}

	[DataSourceProperty]
	public string CancelEquipText
	{
		get
		{
			return _cancelEquipText;
		}
		set
		{
			if (value != _cancelEquipText)
			{
				_cancelEquipText = value;
				OnPropertyChangedWithValue(value, "CancelEquipText");
				UpdatePreviewAndActionTexts();
			}
		}
	}

	[DataSourceProperty]
	public string TauntID
	{
		get
		{
			return _tauntId;
		}
		set
		{
			if (value != _tauntId)
			{
				_tauntId = value;
				OnPropertyChangedWithValue(value, "TauntID");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<StringItemWithEnabledAndHintVM> TauntUsages
	{
		get
		{
			return _tauntUsages;
		}
		set
		{
			if (value != _tauntUsages)
			{
				_tauntUsages = value;
				OnPropertyChangedWithValue(value, "TauntUsages");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel BlocksMovementOnUsageHint
	{
		get
		{
			return _blocksMovementOnUsageHint;
		}
		set
		{
			if (value != _blocksMovementOnUsageHint)
			{
				_blocksMovementOnUsageHint = value;
				OnPropertyChangedWithValue(value, "BlocksMovementOnUsageHint");
			}
		}
	}

	public MPArmoryCosmeticTauntItemVM(string tauntId, CosmeticElement cosmetic, string cosmeticID)
		: base(cosmetic, cosmeticID, CosmeticsManager.CosmeticType.Taunt)
	{
		TauntID = tauntId;
		TauntCosmeticElement = cosmetic as TauntCosmeticElement;
		TauntCategory = GetCategoryOfTaunt();
		TauntUsages = new MBBindingList<StringItemWithEnabledAndHintVM>();
		RefreshTauntUsages();
		BlocksMovementOnUsageHint = new HintViewModel();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		if (BlocksMovementOnUsageHint != null)
		{
			BlocksMovementOnUsageHint.HintText = new TextObject("{=BUQsaZMg}Blocks Movement on Usage");
		}
		SelectSlotText = new TextObject("{=4gfAb1ar}Select a Slot").ToString();
		CancelEquipText = new TextObject("{=avYRbfHA}Cancel Equip").ToString();
		base.Name = TauntCosmeticElement?.Name?.ToString();
	}

	private void RefreshTauntUsages()
	{
		TauntUsages.Clear();
		TauntUsageManager.TauntUsage.TauntUsageFlag tauntUsageFlag = TauntUsageManager.Instance.GetUsageSet(TauntCosmeticElement.Id)?.GetUsages()?.FirstOrDefault().UsageFlag ?? TauntUsageManager.TauntUsage.TauntUsageFlag.None;
		TextObject textObject = new TextObject("{=aeDp7IEK}Usable with {USAGE}");
		if ((tauntUsageFlag & TauntUsageManager.TauntUsage.TauntUsageFlag.UnsuitableForOneHanded) == 0)
		{
			TextObject variable = new TextObject("{=PiHpR4QL}One Handed");
			TextObject hintText = textObject.CopyTextObject().SetTextVariable("USAGE", variable);
			TauntUsages.Add(new StringItemWithEnabledAndHintVM(null, "UsableWithOneHanded", enabled: true, null, hintText));
		}
		if ((tauntUsageFlag & TauntUsageManager.TauntUsage.TauntUsageFlag.UnsuitableForTwoHanded) == 0)
		{
			TextObject variable2 = new TextObject("{=t78atYqH}Two Handed");
			TextObject hintText2 = textObject.CopyTextObject().SetTextVariable("USAGE", variable2);
			TauntUsages.Add(new StringItemWithEnabledAndHintVM(null, "UsableWithTwoHanded", enabled: true, null, hintText2));
		}
		if ((tauntUsageFlag & TauntUsageManager.TauntUsage.TauntUsageFlag.UnsuitableForBow) == 0)
		{
			TextObject variable3 = new TextObject("{=5rj7xQE4}Bow");
			TextObject hintText3 = textObject.CopyTextObject().SetTextVariable("USAGE", variable3);
			TauntUsages.Add(new StringItemWithEnabledAndHintVM(null, "UsableWithBow", enabled: true, null, hintText3));
		}
		if ((tauntUsageFlag & TauntUsageManager.TauntUsage.TauntUsageFlag.UnsuitableForCrossbow) == 0)
		{
			TextObject variable4 = new TextObject("{=TTWL7RLe}Crossbow");
			TextObject hintText4 = textObject.CopyTextObject().SetTextVariable("USAGE", variable4);
			TauntUsages.Add(new StringItemWithEnabledAndHintVM(null, "UsableWithCrossbow", enabled: true, null, hintText4));
		}
		if ((tauntUsageFlag & TauntUsageManager.TauntUsage.TauntUsageFlag.UnsuitableForShield) == 0)
		{
			TextObject variable5 = new TextObject("{=Jd0Kq9lD}Shield");
			TextObject hintText5 = textObject.CopyTextObject().SetTextVariable("USAGE", variable5);
			TauntUsages.Add(new StringItemWithEnabledAndHintVM(null, "UsableWithShield", enabled: true, null, hintText5));
		}
		if ((tauntUsageFlag & TauntUsageManager.TauntUsage.TauntUsageFlag.RequiresOnFoot) == 0)
		{
			TextObject variable6 = new TextObject("{=uGM8DWrm}Mount");
			TextObject hintText6 = textObject.CopyTextObject().SetTextVariable("USAGE", variable6);
			TauntUsages.Add(new StringItemWithEnabledAndHintVM(null, "UsableWithMount", enabled: true, null, hintText6));
		}
		RequiresOnFoot = (tauntUsageFlag & TauntUsageManager.TauntUsage.TauntUsageFlag.RequiresOnFoot) != 0;
	}

	private MPArmoryCosmeticsVM.TauntCategoryFlag GetCategoryOfTaunt()
	{
		MBReadOnlyList<TauntUsageManager.TauntUsage> mBReadOnlyList = TauntUsageManager.Instance.GetUsageSet(TauntCosmeticElement.Id)?.GetUsages();
		MPArmoryCosmeticsVM.TauntCategoryFlag tauntCategoryFlag = MPArmoryCosmeticsVM.TauntCategoryFlag.None;
		if (mBReadOnlyList == null || mBReadOnlyList.Count <= 0)
		{
			return tauntCategoryFlag;
		}
		if (AnyUsageNotHaveFlag(mBReadOnlyList, TauntUsageManager.TauntUsage.TauntUsageFlag.RequiresOnFoot))
		{
			tauntCategoryFlag |= MPArmoryCosmeticsVM.TauntCategoryFlag.UsableWithMount;
		}
		if (AnyUsageNotHaveFlag(mBReadOnlyList, TauntUsageManager.TauntUsage.TauntUsageFlag.UnsuitableForShield))
		{
			tauntCategoryFlag |= MPArmoryCosmeticsVM.TauntCategoryFlag.UsableWithShield;
		}
		if (AnyUsageNotHaveFlag(mBReadOnlyList, TauntUsageManager.TauntUsage.TauntUsageFlag.UnsuitableForOneHanded))
		{
			tauntCategoryFlag |= MPArmoryCosmeticsVM.TauntCategoryFlag.UsableWithOneHanded;
		}
		if (AnyUsageNotHaveFlag(mBReadOnlyList, TauntUsageManager.TauntUsage.TauntUsageFlag.UnsuitableForTwoHanded))
		{
			tauntCategoryFlag |= MPArmoryCosmeticsVM.TauntCategoryFlag.UsableWithTwoHanded;
		}
		if (AnyUsageNotHaveFlag(mBReadOnlyList, TauntUsageManager.TauntUsage.TauntUsageFlag.UnsuitableForBow))
		{
			tauntCategoryFlag |= MPArmoryCosmeticsVM.TauntCategoryFlag.UsableWithBow;
		}
		if (AnyUsageNotHaveFlag(mBReadOnlyList, TauntUsageManager.TauntUsage.TauntUsageFlag.UnsuitableForCrossbow))
		{
			tauntCategoryFlag |= MPArmoryCosmeticsVM.TauntCategoryFlag.UsableWithCrossbow;
		}
		return tauntCategoryFlag;
	}

	private bool AllUsagesHaveFlag(MBReadOnlyList<TauntUsageManager.TauntUsage> list, TauntUsageManager.TauntUsage.TauntUsageFlag flag)
	{
		return list.All((TauntUsageManager.TauntUsage u) => (u.UsageFlag & flag) != 0);
	}

	private bool AnyUsageHaveFlag(MBReadOnlyList<TauntUsageManager.TauntUsage> list, TauntUsageManager.TauntUsage.TauntUsageFlag flag)
	{
		return list.Any((TauntUsageManager.TauntUsage u) => (u.UsageFlag & flag) != 0);
	}

	private bool AnyUsageNotHaveFlag(MBReadOnlyList<TauntUsageManager.TauntUsage> list, TauntUsageManager.TauntUsage.TauntUsageFlag flag)
	{
		return list.Any((TauntUsageManager.TauntUsage u) => (u.UsageFlag & flag) == 0);
	}
}
