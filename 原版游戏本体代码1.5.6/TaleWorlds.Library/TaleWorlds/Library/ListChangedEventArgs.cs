using System;

namespace TaleWorlds.Library;

public class ListChangedEventArgs : EventArgs
{
	public ListChangedType ListChangedType { get; private set; }

	public int NewIndex { get; private set; }

	public int OldIndex { get; private set; }

	public ListChangedEventArgs(ListChangedType listChangedType, int newIndex)
	{
		ListChangedType = listChangedType;
		NewIndex = newIndex;
		OldIndex = -1;
	}

	public ListChangedEventArgs(ListChangedType listChangedType, int newIndex, int oldIndex)
	{
		ListChangedType = listChangedType;
		NewIndex = newIndex;
		OldIndex = oldIndex;
	}
}
