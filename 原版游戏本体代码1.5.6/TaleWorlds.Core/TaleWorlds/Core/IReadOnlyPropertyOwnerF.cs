using TaleWorlds.ObjectSystem;

namespace TaleWorlds.Core;

public interface IReadOnlyPropertyOwnerF<T> where T : MBObjectBase
{
	float GetPropertyValue(T attribute);

	bool HasProperty(T attribute);
}
