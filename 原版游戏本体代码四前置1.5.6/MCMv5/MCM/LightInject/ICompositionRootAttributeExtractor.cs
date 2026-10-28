using System.Reflection;

namespace MCM.LightInject;

internal interface ICompositionRootAttributeExtractor
{
	CompositionRootTypeAttribute[] GetAttributes(Assembly assembly);
}
