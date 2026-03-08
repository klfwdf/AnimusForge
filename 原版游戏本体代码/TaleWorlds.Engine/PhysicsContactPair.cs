using System;
using TaleWorlds.DotNet;

namespace TaleWorlds.Engine
{
	// Token: 0x0200007B RID: 123
	[EngineStruct("rglPhysics_contact_pair", false, null)]
	public struct PhysicsContactPair
	{
		// Token: 0x17000078 RID: 120
		public PhysicsContactInfo this[int index]
		{
			get
			{
				switch (index)
				{
				case 0:
					return this.Contact0;
				case 1:
					return this.Contact1;
				case 2:
					return this.Contact2;
				case 3:
					return this.Contact3;
				case 4:
					return this.Contact4;
				case 5:
					return this.Contact5;
				case 6:
					return this.Contact6;
				case 7:
					return this.Contact7;
				default:
					return default(PhysicsContactInfo);
				}
			}
		}

		// Token: 0x04000176 RID: 374
		[CustomEngineStructMemberData("ignoredMember", true)]
		public readonly PhysicsContactInfo Contact0;

		// Token: 0x04000177 RID: 375
		[CustomEngineStructMemberData("ignoredMember", true)]
		public readonly PhysicsContactInfo Contact1;

		// Token: 0x04000178 RID: 376
		[CustomEngineStructMemberData("ignoredMember", true)]
		public readonly PhysicsContactInfo Contact2;

		// Token: 0x04000179 RID: 377
		[CustomEngineStructMemberData("ignoredMember", true)]
		public readonly PhysicsContactInfo Contact3;

		// Token: 0x0400017A RID: 378
		[CustomEngineStructMemberData("ignoredMember", true)]
		public readonly PhysicsContactInfo Contact4;

		// Token: 0x0400017B RID: 379
		[CustomEngineStructMemberData("ignoredMember", true)]
		public readonly PhysicsContactInfo Contact5;

		// Token: 0x0400017C RID: 380
		[CustomEngineStructMemberData("ignoredMember", true)]
		public readonly PhysicsContactInfo Contact6;

		// Token: 0x0400017D RID: 381
		[CustomEngineStructMemberData("ignoredMember", true)]
		public readonly PhysicsContactInfo Contact7;

		// Token: 0x0400017E RID: 382
		[CustomEngineStructMemberData("type")]
		public readonly PhysicsEventType ContactEventType;

		// Token: 0x0400017F RID: 383
		public readonly int NumberOfContacts;
	}
}
