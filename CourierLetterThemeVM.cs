using TaleWorlds.Library;

namespace AnimusForge;

// Read-only stationery layers bound by both courier letter prefabs via DataSource="{Theme}".
public sealed class CourierLetterThemeVM : ViewModel
{
	private readonly CourierLetterTheme _theme;

	public CourierLetterThemeVM(CourierLetterTheme theme)
	{
		_theme = theme ?? CourierLetterThemes.Resolve(null);
	}

	internal CourierLetterTheme Source => _theme;

	[DataSourceProperty]
	public string ScrollSprite => AnimusForgeCourierUiSprites.ScrollBaseSpriteName;

	[DataSourceProperty]
	public string LeftPatternSprite => _theme.LeftSpriteName;

	[DataSourceProperty]
	public string RightPatternSprite => _theme.RightSpriteName;

	[DataSourceProperty]
	public string SealSprite => _theme.SealSpriteName;

	[DataSourceProperty]
	public float LeftPatternWidth => _theme.LeftWidth;

	[DataSourceProperty]
	public float RightPatternWidth => _theme.RightWidth;
}
