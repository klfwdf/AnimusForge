using System;
using TaleWorlds.Core.ImageIdentifiers;

namespace TaleWorlds.Core.ViewModelCollection.ImageIdentifiers
{
	// Token: 0x0200001E RID: 30
	public class CharacterImageIdentifierVM : ImageIdentifierVM
	{
		// Token: 0x0600019A RID: 410 RVA: 0x000059BE File Offset: 0x00003BBE
		public CharacterImageIdentifierVM(CharacterCode characterCode)
		{
			base.ImageIdentifier = new CharacterImageIdentifier(characterCode);
		}
	}
}
