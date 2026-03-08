using System;
using TaleWorlds.DotNet;

namespace TaleWorlds.Engine
{
	// Token: 0x0200007C RID: 124
	[EngineStruct("rglPhysics_contact", false, null)]
	public struct PhysicsContact
	{
		// Token: 0x17000079 RID: 121
		public PhysicsContactPair this[int index]
		{
			get
			{
				switch (index)
				{
				case 0:
					return this.ContactPair0;
				case 1:
					return this.ContactPair1;
				case 2:
					return this.ContactPair2;
				case 3:
					return this.ContactPair3;
				case 4:
					return this.ContactPair4;
				case 5:
					return this.ContactPair5;
				case 6:
					return this.ContactPair6;
				case 7:
					return this.ContactPair7;
				case 8:
					return this.ContactPair8;
				case 9:
					return this.ContactPair9;
				case 10:
					return this.ContactPair10;
				case 11:
					return this.ContactPair11;
				case 12:
					return this.ContactPair12;
				case 13:
					return this.ContactPair13;
				case 14:
					return this.ContactPair14;
				case 15:
					return this.ContactPair15;
				default:
					return default(PhysicsContactPair);
				}
			}
		}

		// Token: 0x04000180 RID: 384
		[CustomEngineStructMemberData("ignoredMember", true)]
		public readonly PhysicsContactPair ContactPair0;

		// Token: 0x04000181 RID: 385
		[CustomEngineStructMemberData("ignoredMember", true)]
		public readonly PhysicsContactPair ContactPair1;

		// Token: 0x04000182 RID: 386
		[CustomEngineStructMemberData("ignoredMember", true)]
		public readonly PhysicsContactPair ContactPair2;

		// Token: 0x04000183 RID: 387
		[CustomEngineStructMemberData("ignoredMember", true)]
		public readonly PhysicsContactPair ContactPair3;

		// Token: 0x04000184 RID: 388
		[CustomEngineStructMemberData("ignoredMember", true)]
		public readonly PhysicsContactPair ContactPair4;

		// Token: 0x04000185 RID: 389
		[CustomEngineStructMemberData("ignoredMember", true)]
		public readonly PhysicsContactPair ContactPair5;

		// Token: 0x04000186 RID: 390
		[CustomEngineStructMemberData("ignoredMember", true)]
		public readonly PhysicsContactPair ContactPair6;

		// Token: 0x04000187 RID: 391
		[CustomEngineStructMemberData("ignoredMember", true)]
		public readonly PhysicsContactPair ContactPair7;

		// Token: 0x04000188 RID: 392
		[CustomEngineStructMemberData("ignoredMember", true)]
		public readonly PhysicsContactPair ContactPair8;

		// Token: 0x04000189 RID: 393
		[CustomEngineStructMemberData("ignoredMember", true)]
		public readonly PhysicsContactPair ContactPair9;

		// Token: 0x0400018A RID: 394
		[CustomEngineStructMemberData("ignoredMember", true)]
		public readonly PhysicsContactPair ContactPair10;

		// Token: 0x0400018B RID: 395
		[CustomEngineStructMemberData("ignoredMember", true)]
		public readonly PhysicsContactPair ContactPair11;

		// Token: 0x0400018C RID: 396
		[CustomEngineStructMemberData("ignoredMember", true)]
		public readonly PhysicsContactPair ContactPair12;

		// Token: 0x0400018D RID: 397
		[CustomEngineStructMemberData("ignoredMember", true)]
		public readonly PhysicsContactPair ContactPair13;

		// Token: 0x0400018E RID: 398
		[CustomEngineStructMemberData("ignoredMember", true)]
		public readonly PhysicsContactPair ContactPair14;

		// Token: 0x0400018F RID: 399
		[CustomEngineStructMemberData("ignoredMember", true)]
		public readonly PhysicsContactPair ContactPair15;

		// Token: 0x04000190 RID: 400
		public readonly IntPtr body0;

		// Token: 0x04000191 RID: 401
		public readonly IntPtr body1;

		// Token: 0x04000192 RID: 402
		public readonly int NumberOfContactPairs;
	}
}
