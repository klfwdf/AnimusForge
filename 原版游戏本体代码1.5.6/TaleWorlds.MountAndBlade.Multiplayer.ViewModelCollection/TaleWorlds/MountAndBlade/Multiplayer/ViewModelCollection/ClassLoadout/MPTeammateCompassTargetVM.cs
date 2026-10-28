using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.MountAndBlade.ViewModelCollection.HUD.Compass;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.ClassLoadout;

public class MPTeammateCompassTargetVM : CompassTargetVM
{
	public MPTeammateCompassTargetVM(TargetIconType iconType, uint color, uint color2, Banner banner, bool isAlly)
		: base(iconType, color, color2, banner, isAttacker: false, isAlly)
	{
		base.IconType = iconType.ToString();
		base.IsFlag = false;
		base.Banner = ((banner != null) ? new BannerImageIdentifierVM(banner) : new BannerImageIdentifierVM(null));
	}

	public void RefreshTargetIconType(TargetIconType targetIconType)
	{
		base.IconType = targetIconType.ToString();
	}

	public void RefreshTeam(Banner banner, bool isAlly)
	{
		base.Banner = ((banner != null) ? new BannerImageIdentifierVM(banner) : new BannerImageIdentifierVM(null));
		base.IsEnemy = !isAlly;
	}
}
