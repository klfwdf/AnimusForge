using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public interface IMoveableSiegeWeapon
{
	SiegeWeaponMovementComponent MovementComponent { get; }

	void HighlightPath();

	void SwitchGhostEntityMovementMode(bool isGhostEnabled);

	MatrixFrame GetInitialFrame();
}
