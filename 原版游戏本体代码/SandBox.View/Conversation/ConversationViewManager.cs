using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.Library;

namespace SandBox.View.Conversation
{
	// Token: 0x0200007A RID: 122
	public class ConversationViewManager
	{
		// Token: 0x170000B2 RID: 178
		// (get) Token: 0x06000544 RID: 1348 RVA: 0x00027FCB File Offset: 0x000261CB
		public static ConversationViewManager Instance
		{
			get
			{
				return SandBoxViewSubModule.ConversationViewManager;
			}
		}

		// Token: 0x06000545 RID: 1349 RVA: 0x00027FD4 File Offset: 0x000261D4
		public ConversationViewManager()
		{
			this.FillEventHandlers();
			Campaign.Current.ConversationManager.ConditionRunned += this.OnCondition;
			Campaign.Current.ConversationManager.ConsequenceRunned += this.OnConsequence;
		}

		// Token: 0x06000546 RID: 1350 RVA: 0x00028024 File Offset: 0x00026224
		private void FillEventHandlers()
		{
			this._conditionEventHandlers = new Dictionary<string, ConversationViewEventHandlerDelegate>();
			this._consequenceEventHandlers = new Dictionary<string, ConversationViewEventHandlerDelegate>();
			Assembly assembly = typeof(ConversationViewEventHandlerDelegate).Assembly;
			this.FillEventHandlersWith(assembly);
			foreach (Assembly assembly2 in assembly.GetReferencingAssembliesSafe(null))
			{
				this.FillEventHandlersWith(assembly2);
			}
		}

		// Token: 0x06000547 RID: 1351 RVA: 0x00028080 File Offset: 0x00026280
		private void FillEventHandlersWith(Assembly assembly)
		{
			foreach (Type type in assembly.GetTypesSafe(null))
			{
				foreach (MethodInfo method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
				{
					object[] customAttributesSafe = method.GetCustomAttributesSafe(typeof(ConversationViewEventHandler), false);
					if (customAttributesSafe != null && customAttributesSafe.Length != 0)
					{
						foreach (ConversationViewEventHandler conversationViewEventHandler in customAttributesSafe)
						{
							ConversationViewEventHandlerDelegate value = Delegate.CreateDelegate(typeof(ConversationViewEventHandlerDelegate), method) as ConversationViewEventHandlerDelegate;
							if (conversationViewEventHandler.Type == ConversationViewEventHandler.EventType.OnCondition)
							{
								if (!this._conditionEventHandlers.ContainsKey(conversationViewEventHandler.Id))
								{
									this._conditionEventHandlers.Add(conversationViewEventHandler.Id, value);
								}
								else
								{
									this._conditionEventHandlers[conversationViewEventHandler.Id] = value;
								}
							}
							else if (conversationViewEventHandler.Type == ConversationViewEventHandler.EventType.OnConsequence)
							{
								if (!this._consequenceEventHandlers.ContainsKey(conversationViewEventHandler.Id))
								{
									this._consequenceEventHandlers.Add(conversationViewEventHandler.Id, value);
								}
								else
								{
									this._consequenceEventHandlers[conversationViewEventHandler.Id] = value;
								}
							}
						}
					}
				}
			}
		}

		// Token: 0x06000548 RID: 1352 RVA: 0x000281F4 File Offset: 0x000263F4
		private void OnConsequence(ConversationSentence sentence)
		{
			ConversationViewEventHandlerDelegate conversationViewEventHandlerDelegate;
			if (this._consequenceEventHandlers.TryGetValue(sentence.Id, out conversationViewEventHandlerDelegate))
			{
				conversationViewEventHandlerDelegate();
			}
		}

		// Token: 0x06000549 RID: 1353 RVA: 0x0002821C File Offset: 0x0002641C
		private void OnCondition(ConversationSentence sentence)
		{
			ConversationViewEventHandlerDelegate conversationViewEventHandlerDelegate;
			if (this._conditionEventHandlers.TryGetValue(sentence.Id, out conversationViewEventHandlerDelegate))
			{
				conversationViewEventHandlerDelegate();
			}
		}

		// Token: 0x04000278 RID: 632
		private Dictionary<string, ConversationViewEventHandlerDelegate> _conditionEventHandlers;

		// Token: 0x04000279 RID: 633
		private Dictionary<string, ConversationViewEventHandlerDelegate> _consequenceEventHandlers;
	}
}
