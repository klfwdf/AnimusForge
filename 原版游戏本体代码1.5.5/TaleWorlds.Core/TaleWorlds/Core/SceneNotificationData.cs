using System;
using System.Collections.Generic;
using TaleWorlds.Localization;

namespace TaleWorlds.Core;

public class SceneNotificationData
{
	public readonly struct SceneNotificationCharacter(BasicCharacterObject character, Equipment overriddenEquipment = null, BodyProperties overriddenBodyProperties = default(BodyProperties), bool useCivilianEquipment = false, uint customColor1 = uint.MaxValue, uint customColor2 = uint.MaxValue, bool useHorse = false)
	{
		public readonly BasicCharacterObject Character = character;

		public readonly Equipment OverriddenEquipment = overriddenEquipment;

		public readonly BodyProperties OverriddenBodyProperties = overriddenBodyProperties;

		public readonly bool UseCivilianEquipment = useCivilianEquipment;

		public readonly bool UseHorse = useHorse;

		public readonly uint CustomColor1 = customColor1;

		public readonly uint CustomColor2 = customColor2;
	}

	public readonly struct SceneNotificationShip(string shipPrefabId, List<ShipVisualSlotInfo> shipUpgrades, float shipHitPointRatio, uint sailColor1, uint sailColor2, int shipSeed)
	{
		public readonly string ShipPrefabId = shipPrefabId;

		public readonly List<ShipVisualSlotInfo> ShipUpgrades = shipUpgrades;

		public readonly float ShipHitPointRatio = shipHitPointRatio;

		public readonly uint SailColor1 = sailColor1;

		public readonly uint SailColor2 = sailColor2;

		public readonly int ShipSeed = shipSeed;
	}

	public struct NotificationSceneProperties
	{
		public bool InitializePhysics;

		public bool DisableStaticShadows;

		public float? OverriddenWaterStrength;
	}

	public enum RelevantContextType
	{
		Any,
		MPLobby,
		CustomBattle,
		Mission,
		Map
	}

	public virtual string SceneID { get; }

	public virtual string SoundEventPath { get; }

	public virtual TextObject TitleText { get; }

	public virtual TextObject DescriptionText { get; }

	public virtual TextObject AffirmativeDescriptionText { get; }

	public virtual TextObject NegativeDescriptionText { get; }

	public virtual TextObject AffirmativeHintText { get; }

	public virtual TextObject AffirmativeHintTextExtended { get; }

	public virtual TextObject AffirmativeTitleText { get; }

	public virtual TextObject NegativeTitleText { get; }

	public virtual TextObject AffirmativeText { get; }

	public virtual TextObject NegativeText { get; }

	public virtual bool IsAffirmativeOptionShown { get; }

	public virtual bool IsNegativeOptionShown { get; }

	public virtual bool ShouldAutoConfirm { get; }

	public virtual bool PauseActiveState { get; } = true;

	public virtual RelevantContextType RelevantContext { get; }

	public virtual NotificationSceneProperties SceneProperties { get; } = new NotificationSceneProperties
	{
		InitializePhysics = false,
		DisableStaticShadows = false,
		OverriddenWaterStrength = null
	};

	public virtual void OnAffirmativeAction()
	{
	}

	public virtual void OnNegativeAction()
	{
	}

	public virtual void OnCloseAction()
	{
	}

	public virtual Banner[] GetBanners()
	{
		return Array.Empty<Banner>();
	}

	public virtual SceneNotificationCharacter[] GetSceneNotificationCharacters()
	{
		return Array.Empty<SceneNotificationCharacter>();
	}

	public virtual SceneNotificationShip[] GetShips()
	{
		return Array.Empty<SceneNotificationShip>();
	}
}
