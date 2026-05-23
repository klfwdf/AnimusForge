using System;

namespace TaleWorlds.Core.ImageIdentifiers
{
	// Token: 0x020000E4 RID: 228
	public class CharacterImageIdentifier : ImageIdentifier
	{
		// Token: 0x06000B7E RID: 2942 RVA: 0x00024FDC File Offset: 0x000231DC
		public CharacterImageIdentifier(CharacterCode characterCode)
		{
			base.Id = ((characterCode != null) ? characterCode.Code : null) ?? "";
			base.AdditionalArgs = "";
			base.TextureProviderName = "CharacterImageTextureProvider";
		}
	}
}
