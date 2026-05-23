using System;

namespace TaleWorlds.Core
{
	// Token: 0x02000085 RID: 133
	public interface IFaceGeneratorCustomFilter
	{
		// Token: 0x06000892 RID: 2194
		int[] GetHaircutIndices(BasicCharacterObject character);

		// Token: 0x06000893 RID: 2195
		int[] GetFacialHairIndices(BasicCharacterObject character);

		// Token: 0x06000894 RID: 2196
		FaceGeneratorStage[] GetAvailableStages();
	}
}
