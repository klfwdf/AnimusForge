using TaleWorlds.Core;

namespace StoryMode.StoryModeObjects;

public class StoryModeBannerEffects
{
	private const string NotImplementedText = "{=!}Not Implemented.";

	private BannerEffect _dragonBannerEffect;

	public static BannerEffect DragonBannerEffect => StoryModeManager.Current.StoryModeBannerEffects._dragonBannerEffect;

	public StoryModeBannerEffects()
	{
		RegisterAll();
	}

	private void RegisterAll()
	{
		_dragonBannerEffect = Create("dragon_banner_effect");
		InitializeAll();
	}

	private BannerEffect Create(string stringId)
	{
		return Game.Current.ObjectManager.RegisterPresumedObject(new BannerEffect(stringId));
	}

	private void InitializeAll()
	{
		_dragonBannerEffect.Initialize("{=!}Not Implemented.", "{=!}Not Implemented.", 0f, 0f, 0f, EffectIncrementType.Invalid);
	}
}
