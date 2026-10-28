using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CharacterDevelopment;

public sealed class FeatObject : PropertyObject
{
	public enum AdditionType
	{
		Add,
		AddFactor
	}

	public static MBReadOnlyList<FeatObject> All => Campaign.Current.AllFeats;

	public float EffectBonus { get; private set; }

	public AdditionType IncrementType { get; private set; }

	public bool IsPositive { get; private set; }

	public FeatObject(string stringId)
		: base(stringId)
	{
	}

	public void Initialize(string name, string description, float effectBonus, bool isPositiveEffect, AdditionType incrementType)
	{
		Initialize(new TextObject(name), new TextObject(description));
		EffectBonus = effectBonus;
		IncrementType = incrementType;
		IsPositive = isPositiveEffect;
		AfterInitialized();
	}
}
