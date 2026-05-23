using System;
using System.Collections.Generic;

namespace TaleWorlds.Core
{
	// Token: 0x0200005C RID: 92
	public class FaceGenHistory
	{
		// Token: 0x0600072C RID: 1836 RVA: 0x00018D24 File Offset: 0x00016F24
		public FaceGenHistory(List<UndoRedoKey> undoCommands, List<UndoRedoKey> redoCommands, Dictionary<string, float> initialValues)
		{
			this.UndoCommands = undoCommands;
			this.RedoCommands = redoCommands;
			this.InitialValues = initialValues;
		}

		// Token: 0x0600072D RID: 1837 RVA: 0x00018D41 File Offset: 0x00016F41
		public void ClearHistory()
		{
			this.UndoCommands.Clear();
			this.RedoCommands.Clear();
			this.InitialValues.Clear();
		}

		// Token: 0x04000396 RID: 918
		public readonly List<UndoRedoKey> UndoCommands;

		// Token: 0x04000397 RID: 919
		public readonly List<UndoRedoKey> RedoCommands;

		// Token: 0x04000398 RID: 920
		public readonly Dictionary<string, float> InitialValues;
	}
}
