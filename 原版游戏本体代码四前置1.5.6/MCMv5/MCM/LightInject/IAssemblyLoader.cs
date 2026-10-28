using System.Collections.Generic;
using System.Reflection;

namespace MCM.LightInject;

internal interface IAssemblyLoader
{
	IEnumerable<Assembly> Load(string searchPattern);
}
