using System;

namespace SandBox.View.CharacterCreation
{
	// Token: 0x0200007C RID: 124
	public sealed class CharacterCreationStageViewAttribute : Attribute
	{
		// Token: 0x06000557 RID: 1367 RVA: 0x00028668 File Offset: 0x00026868
		public CharacterCreationStageViewAttribute(Type stageType)
		{
			this.StageType = stageType;
		}

		// Token: 0x04000282 RID: 642
		public readonly Type StageType;
	}
}
