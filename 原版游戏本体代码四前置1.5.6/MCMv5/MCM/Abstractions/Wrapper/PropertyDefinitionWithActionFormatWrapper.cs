using System;
using HarmonyLib.BUTR.Extensions;

namespace MCM.Abstractions.Wrapper;

public sealed class PropertyDefinitionWithActionFormatWrapper : BasePropertyDefinitionWrapper, IPropertyDefinitionWithActionFormat
{
	public Func<object, string> ValueFormatFunc { get; }

	public PropertyDefinitionWithActionFormatWrapper(object @object)
		: base(@object)
	{
		Type type = @object.GetType();
		ValueFormatFunc = (AccessTools2.Property(type, "ValueFormatFunc")?.GetValue(@object) as Func<object, string>) ?? ((Func<object, string>)((object obj) => obj.ToString()));
	}
}
