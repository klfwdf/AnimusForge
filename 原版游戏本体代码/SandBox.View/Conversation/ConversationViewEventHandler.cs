using System;

namespace SandBox.View.Conversation
{
	// Token: 0x02000078 RID: 120
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
	public class ConversationViewEventHandler : Attribute
	{
		// Token: 0x170000B0 RID: 176
		// (get) Token: 0x0600053D RID: 1341 RVA: 0x00027FA5 File Offset: 0x000261A5
		public string Id { get; }

		// Token: 0x170000B1 RID: 177
		// (get) Token: 0x0600053E RID: 1342 RVA: 0x00027FAD File Offset: 0x000261AD
		public ConversationViewEventHandler.EventType Type { get; }

		// Token: 0x0600053F RID: 1343 RVA: 0x00027FB5 File Offset: 0x000261B5
		public ConversationViewEventHandler(string id, ConversationViewEventHandler.EventType type)
		{
			this.Id = id;
			this.Type = type;
		}

		// Token: 0x020000CC RID: 204
		public enum EventType
		{
			// Token: 0x040003E0 RID: 992
			OnCondition,
			// Token: 0x040003E1 RID: 993
			OnConsequence
		}
	}
}
