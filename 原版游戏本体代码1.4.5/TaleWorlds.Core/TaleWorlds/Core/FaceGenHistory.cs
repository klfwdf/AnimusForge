using System.Collections.Generic;

namespace TaleWorlds.Core;

public class FaceGenHistory
{
	public readonly List<UndoRedoKey> UndoCommands;

	public readonly List<UndoRedoKey> RedoCommands;

	public readonly Dictionary<string, float> InitialValues;

	public FaceGenHistory(List<UndoRedoKey> undoCommands, List<UndoRedoKey> redoCommands, Dictionary<string, float> initialValues)
	{
		UndoCommands = undoCommands;
		RedoCommands = redoCommands;
		InitialValues = initialValues;
	}

	public void ClearHistory()
	{
		UndoCommands.Clear();
		RedoCommands.Clear();
		InitialValues.Clear();
	}
}
