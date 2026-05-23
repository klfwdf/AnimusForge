using System;
using System.Collections.Generic;
using TaleWorlds.Localization;

namespace TaleWorlds.Core
{
	// Token: 0x02000076 RID: 118
	public class GameText
	{
		// Token: 0x170002CB RID: 715
		// (get) Token: 0x06000827 RID: 2087 RVA: 0x0001AF44 File Offset: 0x00019144
		// (set) Token: 0x06000828 RID: 2088 RVA: 0x0001AF4C File Offset: 0x0001914C
		public string Id { get; private set; }

		// Token: 0x170002CC RID: 716
		// (get) Token: 0x06000829 RID: 2089 RVA: 0x0001AF55 File Offset: 0x00019155
		public IEnumerable<GameText.GameTextVariation> Variations
		{
			get
			{
				return this._variationList;
			}
		}

		// Token: 0x170002CD RID: 717
		// (get) Token: 0x0600082A RID: 2090 RVA: 0x0001AF5D File Offset: 0x0001915D
		public TextObject DefaultText
		{
			get
			{
				if (this._variationList != null && this._variationList.Count > 0)
				{
					return this._variationList[0].Text;
				}
				return null;
			}
		}

		// Token: 0x0600082B RID: 2091 RVA: 0x0001AF88 File Offset: 0x00019188
		internal GameText()
		{
			this._variationList = new List<GameText.GameTextVariation>();
		}

		// Token: 0x0600082C RID: 2092 RVA: 0x0001AF9B File Offset: 0x0001919B
		internal GameText(string id)
		{
			this.Id = id;
			this._variationList = new List<GameText.GameTextVariation>();
		}

		// Token: 0x0600082D RID: 2093 RVA: 0x0001AFB8 File Offset: 0x000191B8
		internal TextObject GetVariation(string variationId)
		{
			foreach (GameText.GameTextVariation gameTextVariation in this._variationList)
			{
				if (gameTextVariation.Id.Equals(variationId))
				{
					return gameTextVariation.Text;
				}
			}
			return null;
		}

		// Token: 0x0600082E RID: 2094 RVA: 0x0001B020 File Offset: 0x00019220
		public void AddVariationWithId(string variationId, TextObject text, List<GameTextManager.ChoiceTag> choiceTags)
		{
			foreach (GameText.GameTextVariation gameTextVariation in this._variationList)
			{
				if (gameTextVariation.Id.Equals(variationId) && gameTextVariation.Text.ToString().Equals(text.ToString()))
				{
					return;
				}
			}
			this._variationList.Add(new GameText.GameTextVariation(variationId, text, choiceTags));
		}

		// Token: 0x0600082F RID: 2095 RVA: 0x0001B0A8 File Offset: 0x000192A8
		public void SetVariationWithId(string variationId, TextObject text, List<GameTextManager.ChoiceTag> choiceTags)
		{
			for (int i = 0; i < this._variationList.Count; i++)
			{
				if (this._variationList[i].Id.Equals(variationId))
				{
					this._variationList[i] = new GameText.GameTextVariation(variationId, text, choiceTags);
					return;
				}
			}
			this._variationList.Add(new GameText.GameTextVariation(variationId, text, choiceTags));
		}

		// Token: 0x06000830 RID: 2096 RVA: 0x0001B10C File Offset: 0x0001930C
		public void AddVariation(string text, params object[] propertiesAndWeights)
		{
			List<GameTextManager.ChoiceTag> list = new List<GameTextManager.ChoiceTag>();
			for (int i = 0; i < propertiesAndWeights.Length; i += 2)
			{
				string tagName = (string)propertiesAndWeights[i];
				int weight = Convert.ToInt32(propertiesAndWeights[i + 1]);
				list.Add(new GameTextManager.ChoiceTag(tagName, weight));
			}
			this.AddVariationWithId("", new TextObject(text, null), list);
		}

		// Token: 0x0400041B RID: 1051
		private readonly List<GameText.GameTextVariation> _variationList;

		// Token: 0x02000119 RID: 281
		public struct GameTextVariation
		{
			// Token: 0x06000BEC RID: 3052 RVA: 0x0002602B File Offset: 0x0002422B
			internal GameTextVariation(string id, TextObject text, List<GameTextManager.ChoiceTag> choiceTags)
			{
				this.Id = id;
				this.Text = text;
				this.Tags = choiceTags.ToArray();
			}

			// Token: 0x040007A5 RID: 1957
			public readonly string Id;

			// Token: 0x040007A6 RID: 1958
			public readonly TextObject Text;

			// Token: 0x040007A7 RID: 1959
			public readonly GameTextManager.ChoiceTag[] Tags;
		}
	}
}
