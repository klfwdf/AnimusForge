using System;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CharacterCreationContent
{
	// Token: 0x0200020E RID: 526
	public sealed class NarrativeMenu
	{
		// Token: 0x170007D9 RID: 2009
		// (get) Token: 0x06001FC4 RID: 8132 RVA: 0x0008E60A File Offset: 0x0008C80A
		public List<NarrativeMenuCharacter> Characters
		{
			get
			{
				return this._characters;
			}
		}

		// Token: 0x170007DA RID: 2010
		// (get) Token: 0x06001FC5 RID: 8133 RVA: 0x0008E612 File Offset: 0x0008C812
		public MBReadOnlyList<NarrativeMenuOption> CharacterCreationMenuOptions
		{
			get
			{
				return this._characterCreationMenuOptions;
			}
		}

		// Token: 0x06001FC6 RID: 8134 RVA: 0x0008E61C File Offset: 0x0008C81C
		public NarrativeMenu(string stringId, string inputMenuId, string outputMenuId, TextObject title, TextObject description, List<NarrativeMenuCharacter> characters, NarrativeMenu.GetNarrativeMenuCharacterArgsDelegate getNarrativeMenuCharacterArgs)
		{
			this.StringId = stringId;
			this.InputMenuId = inputMenuId;
			this.OutputMenuId = outputMenuId;
			this.Title = title;
			this.Description = description;
			this._characters = characters;
			this.GetNarrativeMenuCharacterArgs = getNarrativeMenuCharacterArgs;
			this._characterCreationMenuOptions = new MBList<NarrativeMenuOption>();
		}

		// Token: 0x06001FC7 RID: 8135 RVA: 0x0008E66F File Offset: 0x0008C86F
		public void AddNarrativeMenuOption(NarrativeMenuOption narrativeMenuOption)
		{
			this._characterCreationMenuOptions.Add(narrativeMenuOption);
		}

		// Token: 0x06001FC8 RID: 8136 RVA: 0x0008E67D File Offset: 0x0008C87D
		public void RemoveNarrativeMenuOption(NarrativeMenuOption narrativeMenuOption)
		{
			this._characterCreationMenuOptions.Remove(narrativeMenuOption);
		}

		// Token: 0x0400093F RID: 2367
		public readonly string StringId;

		// Token: 0x04000940 RID: 2368
		public readonly string InputMenuId;

		// Token: 0x04000941 RID: 2369
		public readonly string OutputMenuId;

		// Token: 0x04000942 RID: 2370
		public readonly TextObject Title;

		// Token: 0x04000943 RID: 2371
		public readonly TextObject Description;

		// Token: 0x04000944 RID: 2372
		private readonly List<NarrativeMenuCharacter> _characters;

		// Token: 0x04000945 RID: 2373
		private readonly MBList<NarrativeMenuOption> _characterCreationMenuOptions;

		// Token: 0x04000946 RID: 2374
		public readonly NarrativeMenu.GetNarrativeMenuCharacterArgsDelegate GetNarrativeMenuCharacterArgs;

		// Token: 0x020005FD RID: 1533
		// (Invoke) Token: 0x06004F2A RID: 20266
		public delegate List<NarrativeMenuCharacterArgs> GetNarrativeMenuCharacterArgsDelegate(CultureObject culture, string occupationType, CharacterCreationManager characterCreationManager);
	}
}
