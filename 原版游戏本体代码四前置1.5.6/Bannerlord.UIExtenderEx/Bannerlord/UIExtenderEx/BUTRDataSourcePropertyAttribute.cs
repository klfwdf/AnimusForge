using System;

namespace Bannerlord.UIExtenderEx;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
internal class BUTRDataSourcePropertyAttribute : Attribute
{
	public string? OverrideName { get; set; }
}
