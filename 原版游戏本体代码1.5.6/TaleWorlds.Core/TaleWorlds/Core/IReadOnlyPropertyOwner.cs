using TaleWorlds.ObjectSystem;

namespace TaleWorlds.Core;

public interface IReadOnlyPropertyOwner<T> where T : MBObjectBase
{
	int GetPropertyValue(T attribute);

	bool HasProperty(T attribute);
}
