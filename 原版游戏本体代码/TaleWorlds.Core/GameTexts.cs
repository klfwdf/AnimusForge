using System;
using System.Collections.Generic;
using TaleWorlds.Localization;

namespace TaleWorlds.Core
{
	// Token: 0x02000078 RID: 120
	public static class GameTexts
	{
		// Token: 0x0600083A RID: 2106 RVA: 0x0001B670 File Offset: 0x00019870
		public static void Initialize(GameTextManager gameTextManager)
		{
			GameTexts._gameTextManager = gameTextManager;
			GameTexts.InitializeGlobalTags();
		}

		// Token: 0x0600083B RID: 2107 RVA: 0x0001B67D File Offset: 0x0001987D
		public static TextObject FindText(string id, string variation = null)
		{
			return GameTexts._gameTextManager.FindText(id, variation);
		}

		// Token: 0x0600083C RID: 2108 RVA: 0x0001B68B File Offset: 0x0001988B
		public static bool TryGetText(string id, out TextObject textObject, string variation = null)
		{
			return GameTexts._gameTextManager.TryGetText(id, variation, out textObject);
		}

		// Token: 0x0600083D RID: 2109 RVA: 0x0001B69A File Offset: 0x0001989A
		public static IEnumerable<TextObject> FindAllTextVariations(string id)
		{
			return GameTexts._gameTextManager.FindAllTextVariations(id);
		}

		// Token: 0x0600083E RID: 2110 RVA: 0x0001B6A7 File Offset: 0x000198A7
		public static void SetVariable(string variableName, string content)
		{
			MBTextManager.SetTextVariable(variableName, content, false);
		}

		// Token: 0x0600083F RID: 2111 RVA: 0x0001B6B1 File Offset: 0x000198B1
		public static void SetVariable(string variableName, float content)
		{
			MBTextManager.SetTextVariable(variableName, content, 2);
		}

		// Token: 0x06000840 RID: 2112 RVA: 0x0001B6BB File Offset: 0x000198BB
		public static void SetVariable(string variableName, int content)
		{
			MBTextManager.SetTextVariable(variableName, content);
		}

		// Token: 0x06000841 RID: 2113 RVA: 0x0001B6C4 File Offset: 0x000198C4
		public static void SetVariable(string variableName, TextObject content)
		{
			MBTextManager.SetTextVariable(variableName, content, false);
		}

		// Token: 0x06000842 RID: 2114 RVA: 0x0001B6CE File Offset: 0x000198CE
		public static void ClearInstance()
		{
			GameTexts._gameTextManager = null;
		}

		// Token: 0x06000843 RID: 2115 RVA: 0x0001B6D6 File Offset: 0x000198D6
		public static GameTexts.GameTextHelper AddGameTextWithVariation(string id)
		{
			return new GameTexts.GameTextHelper(id);
		}

		// Token: 0x06000844 RID: 2116 RVA: 0x0001B6DE File Offset: 0x000198DE
		private static void InitializeGlobalTags()
		{
			GameTexts.SetVariable("newline", "\n");
		}

		// Token: 0x0400041D RID: 1053
		private static GameTextManager _gameTextManager;

		// Token: 0x0200011C RID: 284
		public class GameTextHelper
		{
			// Token: 0x06000BFD RID: 3069 RVA: 0x00026247 File Offset: 0x00024447
			public GameTextHelper(string id)
			{
				this._id = id;
			}

			// Token: 0x06000BFE RID: 3070 RVA: 0x00026256 File Offset: 0x00024456
			public GameTexts.GameTextHelper Variation(string text, params object[] propertiesAndWeights)
			{
				GameTexts._gameTextManager.AddGameText(this._id).AddVariation(text, propertiesAndWeights);
				return this;
			}

			// Token: 0x06000BFF RID: 3071 RVA: 0x00026270 File Offset: 0x00024470
			public static TextObject MergeTextObjectsWithComma(List<TextObject> textObjects, bool includeAnd)
			{
				return GameTexts.GameTextHelper.MergeTextObjectsWithSymbol(textObjects, new TextObject("{=kfdxjIad}, ", null), includeAnd ? new TextObject("{=eob9goyW} and ", null) : null);
			}

			// Token: 0x06000C00 RID: 3072 RVA: 0x00026294 File Offset: 0x00024494
			public static TextObject MergeTextObjectsWithSymbol(List<TextObject> textObjects, TextObject symbol, TextObject lastSymbol = null)
			{
				int count = textObjects.Count;
				TextObject textObject;
				if (count == 0)
				{
					textObject = TextObject.GetEmpty();
				}
				else if (count == 1)
				{
					textObject = textObjects[0];
				}
				else
				{
					string text = "{=!}";
					for (int i = 0; i < textObjects.Count - 2; i++)
					{
						text = string.Concat(new object[] { text, "{VAR_", i, "}{SYMBOL}" });
					}
					text = string.Concat(new object[]
					{
						text,
						"{VAR_",
						textObjects.Count - 2,
						"}{LAST_SYMBOL}{VAR_",
						textObjects.Count - 1,
						"}"
					});
					textObject = new TextObject(text, null);
					for (int j = 0; j < textObjects.Count; j++)
					{
						textObject.SetTextVariable("VAR_" + j, textObjects[j]);
					}
					textObject.SetTextVariable("SYMBOL", symbol);
					textObject.SetTextVariable("LAST_SYMBOL", lastSymbol ?? symbol);
				}
				return textObject;
			}

			// Token: 0x040007B2 RID: 1970
			private string _id;
		}
	}
}
