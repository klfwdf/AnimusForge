using System;

namespace Bannerlord.UIExtenderEx;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
internal class BUTRDataSourceMethodAttribute : Attribute
{
	public string? OverrideName { get; set; }
}
