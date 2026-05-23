using System;

namespace Newtonsoft.Json.Serialization
{
	/// <summary>
	/// When applied to a method, specifies that the method is called when an error occurs serializing an object.
	/// </summary>
	// Token: 0x0200009F RID: 159
	[AttributeUsage(AttributeTargets.Method, Inherited = false)]
	public sealed class OnErrorAttribute : Attribute
	{
	}
}
