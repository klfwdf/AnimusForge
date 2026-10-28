using System.Collections;

namespace TaleWorlds.Library;

public interface IMBBindingList : IList, ICollection, IEnumerable
{
	event ListChangedEventHandler ListChanged;
}
