using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;

namespace TaleWorlds.CampaignSystem.ViewModelCollection
{
	// Token: 0x0200000D RID: 13
	public static class CampaignOptionsManager
	{
		// Token: 0x060000A9 RID: 169 RVA: 0x00003D8C File Offset: 0x00001F8C
		public static bool GetOptionWithIdExists(string identifier)
		{
			return !string.IsNullOrEmpty(identifier) && CampaignOptionsManager._currentOptions.Any((ICampaignOptionData x) => x.GetIdentifier() == identifier);
		}

		// Token: 0x060000AA RID: 170 RVA: 0x00003DCC File Offset: 0x00001FCC
		public static void Initialize()
		{
			foreach (Assembly assembly in ModuleHelper.GetActiveGameAssemblies())
			{
				List<Type> typesSafe = assembly.GetTypesSafe(null);
				for (int i = 0; i < typesSafe.Count; i++)
				{
					Type type = typesSafe[i];
					if (type != null && type != typeof(ICampaignOptionProvider) && typeof(ICampaignOptionProvider).IsAssignableFrom(type))
					{
						ICampaignOptionProvider item = Activator.CreateInstance(type) as ICampaignOptionProvider;
						CampaignOptionsManager._optionProviders.Add(item);
					}
				}
			}
		}

		// Token: 0x060000AB RID: 171 RVA: 0x00003E7C File Offset: 0x0000207C
		public static void ClearCachedOptions()
		{
			CampaignOptionsManager._currentOptions.Clear();
		}

		// Token: 0x060000AC RID: 172 RVA: 0x00003E88 File Offset: 0x00002088
		public static List<ICampaignOptionData> GetGameplayCampaignOptions()
		{
			CampaignOptionsManager._currentOptions.Clear();
			for (int i = CampaignOptionsManager._optionProviders.Count - 1; i >= 0; i--)
			{
				IEnumerable<ICampaignOptionData> gameplayCampaignOptions = CampaignOptionsManager._optionProviders[i].GetGameplayCampaignOptions();
				if (gameplayCampaignOptions != null)
				{
					foreach (ICampaignOptionData item in gameplayCampaignOptions)
					{
						CampaignOptionsManager._currentOptions.Add(item);
					}
				}
			}
			return CampaignOptionsManager._currentOptions;
		}

		// Token: 0x060000AD RID: 173 RVA: 0x00003F10 File Offset: 0x00002110
		public static List<ICampaignOptionData> GetCharacterCreationCampaignOptions()
		{
			CampaignOptionsManager._currentOptions.Clear();
			for (int i = CampaignOptionsManager._optionProviders.Count - 1; i >= 0; i--)
			{
				IEnumerable<ICampaignOptionData> characterCreationCampaignOptions = CampaignOptionsManager._optionProviders[i].GetCharacterCreationCampaignOptions();
				if (characterCreationCampaignOptions != null)
				{
					foreach (ICampaignOptionData item in characterCreationCampaignOptions)
					{
						CampaignOptionsManager._currentOptions.Add(item);
					}
				}
			}
			return CampaignOptionsManager._currentOptions;
		}

		// Token: 0x0400005E RID: 94
		private static readonly List<ICampaignOptionProvider> _optionProviders = new List<ICampaignOptionProvider>();

		// Token: 0x0400005F RID: 95
		private static List<ICampaignOptionData> _currentOptions = new List<ICampaignOptionData>();
	}
}
