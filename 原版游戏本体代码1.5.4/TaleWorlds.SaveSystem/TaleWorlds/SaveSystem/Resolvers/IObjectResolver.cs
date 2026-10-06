using TaleWorlds.SaveSystem.Load;

namespace TaleWorlds.SaveSystem.Resolvers;

public interface IObjectResolver
{
	bool CheckIfRequiresAdvancedResolving(object originalObject);

	object ResolveObject(object originalObject);

	object AdvancedResolveObject(object originalObject, MetaData metaData, ObjectLoadData objectLoadData);
}
