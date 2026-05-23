using System;

namespace TaleWorlds.Core
{
	// Token: 0x020000CD RID: 205
	public class ShipSail
	{
		// Token: 0x06000B05 RID: 2821 RVA: 0x0002393A File Offset: 0x00021B3A
		public ShipSail(MissionShipObject shipObject, int index, SailType type, float forceMultiplier, float leftRotationLimit, float rightRotationLimit, float rotationRate)
		{
			this.ShipObject = shipObject;
			this.Index = index;
			this.Type = type;
			this.ForceMultiplier = forceMultiplier;
			this.LeftRotationLimit = leftRotationLimit;
			this.RightRotationLimit = rightRotationLimit;
			this.RotationRate = rotationRate;
		}

		// Token: 0x04000620 RID: 1568
		public readonly MissionShipObject ShipObject;

		// Token: 0x04000621 RID: 1569
		public readonly int Index;

		// Token: 0x04000622 RID: 1570
		public readonly SailType Type;

		// Token: 0x04000623 RID: 1571
		public readonly float ForceMultiplier;

		// Token: 0x04000624 RID: 1572
		public readonly float LeftRotationLimit;

		// Token: 0x04000625 RID: 1573
		public readonly float RightRotationLimit;

		// Token: 0x04000626 RID: 1574
		public readonly float RotationRate;
	}
}
