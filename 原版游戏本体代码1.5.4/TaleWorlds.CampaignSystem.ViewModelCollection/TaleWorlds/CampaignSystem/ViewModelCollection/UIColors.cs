using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection;

public static class UIColors
{
	private static Color _positiveIndicator = Color.FromUint(4285250886u);

	private static Color _negativeIndicator = Color.FromUint(4290070086u);

	private static Color _gold = Color.FromUint(4294957447u);

	public static Color PositiveIndicator => _positiveIndicator;

	public static Color NegativeIndicator => _negativeIndicator;

	public static Color Gold => _gold;
}
