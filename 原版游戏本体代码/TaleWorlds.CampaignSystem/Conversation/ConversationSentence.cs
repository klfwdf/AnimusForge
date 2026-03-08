using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using TaleWorlds.CampaignSystem.Conversation.Persuasion;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Conversation
{
	// Token: 0x02000237 RID: 567
	public class ConversationSentence
	{
		// Token: 0x17000874 RID: 2164
		// (get) Token: 0x06002266 RID: 8806 RVA: 0x00097B44 File Offset: 0x00095D44
		// (set) Token: 0x06002267 RID: 8807 RVA: 0x00097B4C File Offset: 0x00095D4C
		public TextObject Text { get; private set; }

		// Token: 0x17000875 RID: 2165
		// (get) Token: 0x06002268 RID: 8808 RVA: 0x00097B55 File Offset: 0x00095D55
		// (set) Token: 0x06002269 RID: 8809 RVA: 0x00097B5D File Offset: 0x00095D5D
		public int Index { get; internal set; }

		// Token: 0x17000876 RID: 2166
		// (get) Token: 0x0600226A RID: 8810 RVA: 0x00097B66 File Offset: 0x00095D66
		// (set) Token: 0x0600226B RID: 8811 RVA: 0x00097B6E File Offset: 0x00095D6E
		public string Id { get; private set; }

		// Token: 0x17000877 RID: 2167
		// (get) Token: 0x0600226C RID: 8812 RVA: 0x00097B77 File Offset: 0x00095D77
		// (set) Token: 0x0600226D RID: 8813 RVA: 0x00097B80 File Offset: 0x00095D80
		public bool IsPlayer
		{
			get
			{
				return this.GetFlags(ConversationSentence.DialogLineFlags.PlayerLine);
			}
			internal set
			{
				this.set_flags(value, ConversationSentence.DialogLineFlags.PlayerLine);
			}
		}

		// Token: 0x17000878 RID: 2168
		// (get) Token: 0x0600226E RID: 8814 RVA: 0x00097B8A File Offset: 0x00095D8A
		// (set) Token: 0x0600226F RID: 8815 RVA: 0x00097B93 File Offset: 0x00095D93
		public bool IsRepeatable
		{
			get
			{
				return this.GetFlags(ConversationSentence.DialogLineFlags.RepeatForObjects);
			}
			internal set
			{
				this.set_flags(value, ConversationSentence.DialogLineFlags.RepeatForObjects);
			}
		}

		// Token: 0x17000879 RID: 2169
		// (get) Token: 0x06002270 RID: 8816 RVA: 0x00097B9D File Offset: 0x00095D9D
		// (set) Token: 0x06002271 RID: 8817 RVA: 0x00097BA6 File Offset: 0x00095DA6
		public bool IsSpecial
		{
			get
			{
				return this.GetFlags(ConversationSentence.DialogLineFlags.SpecialLine);
			}
			internal set
			{
				this.set_flags(value, ConversationSentence.DialogLineFlags.SpecialLine);
			}
		}

		// Token: 0x1700087A RID: 2170
		// (get) Token: 0x06002272 RID: 8818 RVA: 0x00097BB0 File Offset: 0x00095DB0
		// (set) Token: 0x06002273 RID: 8819 RVA: 0x00097BB9 File Offset: 0x00095DB9
		public bool IsUsedOnce
		{
			get
			{
				return this.GetFlags(ConversationSentence.DialogLineFlags.UsedOnce);
			}
			internal set
			{
				this.set_flags(value, ConversationSentence.DialogLineFlags.UsedOnce);
			}
		}

		// Token: 0x06002274 RID: 8820 RVA: 0x00097BC3 File Offset: 0x00095DC3
		private bool GetFlags(ConversationSentence.DialogLineFlags flag)
		{
			return (this._flags & (uint)flag) > 0U;
		}

		// Token: 0x06002275 RID: 8821 RVA: 0x00097BD0 File Offset: 0x00095DD0
		private void set_flags(bool val, ConversationSentence.DialogLineFlags newFlag)
		{
			if (val)
			{
				this._flags |= (uint)newFlag;
				return;
			}
			this._flags &= (uint)(~(uint)newFlag);
		}

		// Token: 0x1700087B RID: 2171
		// (get) Token: 0x06002276 RID: 8822 RVA: 0x00097BF3 File Offset: 0x00095DF3
		// (set) Token: 0x06002277 RID: 8823 RVA: 0x00097BFB File Offset: 0x00095DFB
		public int Priority { get; private set; }

		// Token: 0x1700087C RID: 2172
		// (get) Token: 0x06002278 RID: 8824 RVA: 0x00097C04 File Offset: 0x00095E04
		// (set) Token: 0x06002279 RID: 8825 RVA: 0x00097C0C File Offset: 0x00095E0C
		public int InputToken { get; private set; }

		// Token: 0x1700087D RID: 2173
		// (get) Token: 0x0600227A RID: 8826 RVA: 0x00097C15 File Offset: 0x00095E15
		// (set) Token: 0x0600227B RID: 8827 RVA: 0x00097C1D File Offset: 0x00095E1D
		public int OutputToken { get; private set; }

		// Token: 0x1700087E RID: 2174
		// (get) Token: 0x0600227C RID: 8828 RVA: 0x00097C26 File Offset: 0x00095E26
		// (set) Token: 0x0600227D RID: 8829 RVA: 0x00097C2E File Offset: 0x00095E2E
		public object RelatedObject { get; private set; }

		// Token: 0x1700087F RID: 2175
		// (get) Token: 0x0600227F RID: 8831 RVA: 0x00097C40 File Offset: 0x00095E40
		// (set) Token: 0x0600227E RID: 8830 RVA: 0x00097C37 File Offset: 0x00095E37
		public bool IsWithVariation { get; private set; }

		// Token: 0x17000880 RID: 2176
		// (get) Token: 0x06002280 RID: 8832 RVA: 0x00097C48 File Offset: 0x00095E48
		// (set) Token: 0x06002281 RID: 8833 RVA: 0x00097C50 File Offset: 0x00095E50
		public PersuasionOptionArgs PersuationOptionArgs { get; private set; }

		// Token: 0x17000881 RID: 2177
		// (get) Token: 0x06002282 RID: 8834 RVA: 0x00097C59 File Offset: 0x00095E59
		public bool HasPersuasion
		{
			get
			{
				return this._onPersuasionOption != null;
			}
		}

		// Token: 0x17000882 RID: 2178
		// (get) Token: 0x06002283 RID: 8835 RVA: 0x00097C64 File Offset: 0x00095E64
		public string SkillName
		{
			get
			{
				if (!this.HasPersuasion)
				{
					return "";
				}
				return this.PersuationOptionArgs.SkillUsed.ToString();
			}
		}

		// Token: 0x17000883 RID: 2179
		// (get) Token: 0x06002284 RID: 8836 RVA: 0x00097C84 File Offset: 0x00095E84
		public string TraitName
		{
			get
			{
				if (!this.HasPersuasion)
				{
					return "";
				}
				if (this.PersuationOptionArgs.TraitUsed == null)
				{
					return "";
				}
				return this.PersuationOptionArgs.TraitUsed.ToString();
			}
		}

		// Token: 0x06002285 RID: 8837 RVA: 0x00097CB8 File Offset: 0x00095EB8
		internal ConversationSentence(string idString, TextObject text, string inputToken, string outputToken, ConversationSentence.OnConditionDelegate conditionDelegate, ConversationSentence.OnClickableConditionDelegate clickableConditionDelegate, ConversationSentence.OnConsequenceDelegate consequenceDelegate, uint flags = 0U, int priority = 100, int agentIndex = 0, int nextAgentIndex = 0, object relatedObject = null, bool withVariation = false, ConversationSentence.OnMultipleConversationConsequenceDelegate speakerDelegate = null, ConversationSentence.OnMultipleConversationConsequenceDelegate listenerDelegate = null, ConversationSentence.OnPersuasionOptionDelegate persuasionOptionDelegate = null)
		{
			this.Index = Campaign.Current.ConversationManager.CreateConversationSentenceIndex();
			this.Id = idString;
			this.Text = text;
			this.InputToken = Campaign.Current.ConversationManager.GetStateIndex(inputToken);
			this.OutputToken = Campaign.Current.ConversationManager.GetStateIndex(outputToken);
			this.OnCondition = conditionDelegate;
			this.OnClickableCondition = clickableConditionDelegate;
			this.OnConsequence = consequenceDelegate;
			this._flags = flags;
			this.Priority = priority;
			this.AgentIndex = agentIndex;
			this.NextAgentIndex = nextAgentIndex;
			this.RelatedObject = relatedObject;
			this.IsWithVariation = withVariation;
			this.IsSpeaker = speakerDelegate;
			this.IsListener = listenerDelegate;
			this._onPersuasionOption = persuasionOptionDelegate;
		}

		// Token: 0x06002286 RID: 8838 RVA: 0x00097D82 File Offset: 0x00095F82
		internal ConversationSentence(int index)
		{
			this.Index = index;
		}

		// Token: 0x06002287 RID: 8839 RVA: 0x00097D98 File Offset: 0x00095F98
		public ConversationSentence Variation(params object[] list)
		{
			Game.Current.GameTextManager.AddGameText(this.Id).AddVariation((string)list[0], list.Skip(1).ToArray<object>());
			return this;
		}

		// Token: 0x06002288 RID: 8840 RVA: 0x00097DC9 File Offset: 0x00095FC9
		internal void RunConsequence(Game game)
		{
			if (this.OnConsequence != null)
			{
				this.OnConsequence();
			}
			Campaign.Current.ConversationManager.OnConsequence(this);
			if (this.HasPersuasion)
			{
				ConversationManager.PersuasionCommitProgress(this.PersuationOptionArgs);
			}
		}

		// Token: 0x06002289 RID: 8841 RVA: 0x00097E04 File Offset: 0x00096004
		internal bool RunCondition()
		{
			bool flag = true;
			if (this.OnCondition != null)
			{
				flag = this.OnCondition();
			}
			if (flag && this.HasPersuasion)
			{
				this.PersuationOptionArgs = this._onPersuasionOption();
			}
			Campaign.Current.ConversationManager.OnCondition(this);
			return flag;
		}

		// Token: 0x0600228A RID: 8842 RVA: 0x00097E54 File Offset: 0x00096054
		internal bool RunClickableCondition()
		{
			bool result = true;
			if (this.OnClickableCondition != null)
			{
				result = this.OnClickableCondition(out this.HintText);
			}
			Campaign.Current.ConversationManager.OnClickableCondition(this);
			return result;
		}

		// Token: 0x0600228B RID: 8843 RVA: 0x00097E90 File Offset: 0x00096090
		public void Deserialize(XmlNode node, Type typeOfConversationCallbacks, ConversationManager conversationManager, int defaultPriority)
		{
			if (node.Attributes == null)
			{
				throw new TWXmlLoadException("node.Attributes != null");
			}
			this.Id = node.Attributes["id"].Value;
			XmlNode xmlNode = node.Attributes["on_condition"];
			if (xmlNode != null)
			{
				string innerText = xmlNode.InnerText;
				this._methodOnCondition = typeOfConversationCallbacks.GetMethod(innerText);
				if (this._methodOnCondition == null)
				{
					throw new MBMethodNameNotFoundException(innerText);
				}
				this.OnCondition = Delegate.CreateDelegate(typeof(ConversationSentence.OnConditionDelegate), null, this._methodOnCondition) as ConversationSentence.OnConditionDelegate;
			}
			XmlNode xmlNode2 = node.Attributes["on_clickable_condition"];
			if (xmlNode2 != null)
			{
				string innerText2 = xmlNode2.InnerText;
				this._methodOnClickableCondition = typeOfConversationCallbacks.GetMethod(innerText2);
				if (this._methodOnClickableCondition == null)
				{
					throw new MBMethodNameNotFoundException(innerText2);
				}
				this.OnClickableCondition = Delegate.CreateDelegate(typeof(ConversationSentence.OnClickableConditionDelegate), null, this._methodOnClickableCondition) as ConversationSentence.OnClickableConditionDelegate;
			}
			XmlNode xmlNode3 = node.Attributes["on_consequence"];
			if (xmlNode3 != null)
			{
				string innerText3 = xmlNode3.InnerText;
				this._methodOnConsequence = typeOfConversationCallbacks.GetMethod(innerText3);
				if (this._methodOnConsequence == null)
				{
					throw new MBMethodNameNotFoundException(innerText3);
				}
				this.OnConsequence = Delegate.CreateDelegate(typeof(ConversationSentence.OnConsequenceDelegate), null, this._methodOnConsequence) as ConversationSentence.OnConsequenceDelegate;
			}
			XmlNode xmlNode4 = node.Attributes["is_player"];
			if (xmlNode4 != null)
			{
				string innerText4 = xmlNode4.InnerText;
				this.IsPlayer = Convert.ToBoolean(innerText4);
			}
			XmlNode xmlNode5 = node.Attributes["is_repeatable"];
			if (xmlNode5 != null)
			{
				string innerText5 = xmlNode5.InnerText;
				this.IsRepeatable = Convert.ToBoolean(innerText5);
			}
			XmlNode xmlNode6 = node.Attributes["is_speacial_option"];
			if (xmlNode6 != null)
			{
				string innerText6 = xmlNode6.InnerText;
				this.IsSpecial = Convert.ToBoolean(innerText6);
			}
			XmlNode xmlNode7 = node.Attributes["is_used_once"];
			if (xmlNode7 != null)
			{
				string innerText7 = xmlNode7.InnerText;
				this.IsUsedOnce = Convert.ToBoolean(innerText7);
			}
			XmlNode xmlNode8 = node.Attributes["text"];
			if (xmlNode8 != null)
			{
				this.Text = new TextObject(xmlNode8.InnerText, null);
			}
			XmlNode xmlNode9 = node.Attributes["istate"];
			if (xmlNode9 != null)
			{
				this.InputToken = conversationManager.GetStateIndex(xmlNode9.InnerText);
			}
			XmlNode xmlNode10 = node.Attributes["ostate"];
			if (xmlNode10 != null)
			{
				this.OutputToken = conversationManager.GetStateIndex(xmlNode10.InnerText);
			}
			XmlNode xmlNode11 = node.Attributes["priority"];
			this.Priority = ((xmlNode11 != null) ? int.Parse(xmlNode11.InnerText) : defaultPriority);
		}

		// Token: 0x17000884 RID: 2180
		// (get) Token: 0x0600228C RID: 8844 RVA: 0x00098142 File Offset: 0x00096342
		public static object CurrentProcessedRepeatObject
		{
			get
			{
				return Campaign.Current.ConversationManager.GetCurrentProcessedRepeatObject();
			}
		}

		// Token: 0x17000885 RID: 2181
		// (get) Token: 0x0600228D RID: 8845 RVA: 0x00098153 File Offset: 0x00096353
		public static object SelectedRepeatObject
		{
			get
			{
				return Campaign.Current.ConversationManager.GetSelectedRepeatObject();
			}
		}

		// Token: 0x17000886 RID: 2182
		// (get) Token: 0x0600228E RID: 8846 RVA: 0x00098164 File Offset: 0x00096364
		public static TextObject SelectedRepeatLine
		{
			get
			{
				return Campaign.Current.ConversationManager.GetCurrentDialogLine();
			}
		}

		// Token: 0x0600228F RID: 8847 RVA: 0x00098175 File Offset: 0x00096375
		public static void SetObjectsToRepeatOver(IReadOnlyList<object> objectsToRepeatOver, int maxRepeatedDialogsInConversation = 5)
		{
			Campaign.Current.ConversationManager.SetDialogRepeatCount(objectsToRepeatOver, maxRepeatedDialogsInConversation);
		}

		// Token: 0x04000A2C RID: 2604
		public const int DefaultPriority = 100;

		// Token: 0x04000A30 RID: 2608
		public int AgentIndex;

		// Token: 0x04000A31 RID: 2609
		public int NextAgentIndex;

		// Token: 0x04000A32 RID: 2610
		public bool IsClickable = true;

		// Token: 0x04000A33 RID: 2611
		public TextObject HintText;

		// Token: 0x04000A38 RID: 2616
		private MethodInfo _methodOnCondition;

		// Token: 0x04000A39 RID: 2617
		public ConversationSentence.OnConditionDelegate OnCondition;

		// Token: 0x04000A3A RID: 2618
		private MethodInfo _methodOnClickableCondition;

		// Token: 0x04000A3B RID: 2619
		public ConversationSentence.OnClickableConditionDelegate OnClickableCondition;

		// Token: 0x04000A3C RID: 2620
		private MethodInfo _methodOnConsequence;

		// Token: 0x04000A3D RID: 2621
		public ConversationSentence.OnConsequenceDelegate OnConsequence;

		// Token: 0x04000A3E RID: 2622
		public ConversationSentence.OnMultipleConversationConsequenceDelegate IsSpeaker;

		// Token: 0x04000A3F RID: 2623
		public ConversationSentence.OnMultipleConversationConsequenceDelegate IsListener;

		// Token: 0x04000A40 RID: 2624
		private uint _flags;

		// Token: 0x04000A42 RID: 2626
		private ConversationSentence.OnPersuasionOptionDelegate _onPersuasionOption;

		// Token: 0x0200063D RID: 1597
		public enum DialogLineFlags
		{
			// Token: 0x0400195F RID: 6495
			PlayerLine = 1,
			// Token: 0x04001960 RID: 6496
			RepeatForObjects,
			// Token: 0x04001961 RID: 6497
			SpecialLine = 4,
			// Token: 0x04001962 RID: 6498
			UsedOnce = 8
		}

		// Token: 0x0200063E RID: 1598
		// (Invoke) Token: 0x0600503C RID: 20540
		public delegate bool OnConditionDelegate();

		// Token: 0x0200063F RID: 1599
		// (Invoke) Token: 0x06005040 RID: 20544
		public delegate bool OnClickableConditionDelegate(out TextObject explanation);

		// Token: 0x02000640 RID: 1600
		// (Invoke) Token: 0x06005044 RID: 20548
		public delegate PersuasionOptionArgs OnPersuasionOptionDelegate();

		// Token: 0x02000641 RID: 1601
		// (Invoke) Token: 0x06005048 RID: 20552
		public delegate void OnConsequenceDelegate();

		// Token: 0x02000642 RID: 1602
		// (Invoke) Token: 0x0600504C RID: 20556
		public delegate bool OnMultipleConversationConsequenceDelegate(IAgent agent);
	}
}
