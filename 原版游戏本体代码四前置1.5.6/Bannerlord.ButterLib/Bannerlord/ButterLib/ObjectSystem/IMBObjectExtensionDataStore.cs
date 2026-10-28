using TaleWorlds.ObjectSystem;

namespace Bannerlord.ButterLib.ObjectSystem;

internal interface IMBObjectExtensionDataStore
{
	bool HasVariable(MBObjectBase @object, string name);

	bool RemoveVariable(MBObjectBase @object, string name);

	void SetVariable(MBObjectBase @object, string name, object? data);

	bool TryGetVariable<T>(MBObjectBase @object, string name, out T? value);

	bool HasFlag(MBObjectBase @object, string name);

	bool RemoveFlag(MBObjectBase @object, string name);

	void SetFlag(MBObjectBase @object, string name);
}
