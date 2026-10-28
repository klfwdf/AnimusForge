using System;
using System.Reflection;

namespace MCM.LightInject;

internal interface IConstructorSelector
{
	ConstructorInfo Execute(Type implementingType);
}
