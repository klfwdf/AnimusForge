using System;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json.Serialization
{
	/// <summary>
	/// Represents a method that constructs an object.
	/// </summary>
	/// <typeparam name="T">The object type to create.</typeparam>
	// Token: 0x0200009E RID: 158
	// (Invoke) Token: 0x06000838 RID: 2104
	public delegate object ObjectConstructor<[Nullable(2)] T>([Nullable(new byte[] { 1, 2 })] params object[] args);
}
