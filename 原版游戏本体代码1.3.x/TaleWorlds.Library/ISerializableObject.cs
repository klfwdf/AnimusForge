using System;

namespace TaleWorlds.Library
{
	// Token: 0x02000043 RID: 67
	public interface ISerializableObject
	{
		// Token: 0x0600021C RID: 540
		void DeserializeFrom(IReader reader);

		// Token: 0x0600021D RID: 541
		void SerializeTo(IWriter writer);
	}
}
