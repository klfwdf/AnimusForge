namespace TaleWorlds.MountAndBlade;

public interface IScreenFadeHandler
{
	void BeginFadeOutAndIn(float fadeOutDuration = 0.5f, float blackOutDuration = 0.5f, float fadeInDuration = 0.5f);

	void BeginFadeOut(float fadeOutDuration = 0.5f);

	void BeginFadeIn(float fadeInDuration = 0.5f);

	ScreenFadeController.ScreenFadeState GetScreenFadeState();
}
