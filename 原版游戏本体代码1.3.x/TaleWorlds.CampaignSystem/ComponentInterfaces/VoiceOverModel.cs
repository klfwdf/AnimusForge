using System;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001F7 RID: 503
	public abstract class VoiceOverModel : MBGameModel<VoiceOverModel>
	{
		// Token: 0x06001F24 RID: 7972
		public abstract string GetSoundPathForCharacter(CharacterObject character, VoiceObject voiceObject);

		// Token: 0x06001F25 RID: 7973
		public abstract string GetAccentClass(CultureObject culture, bool isHighClass);
	}
}
