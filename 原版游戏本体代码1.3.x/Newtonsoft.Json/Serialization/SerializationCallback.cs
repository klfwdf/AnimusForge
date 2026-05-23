using System;
using System.Runtime.Serialization;

namespace Newtonsoft.Json.Serialization
{
	/// <summary>
	/// Handles <see cref="T:Newtonsoft.Json.JsonSerializer" /> serialization callback events.
	/// </summary>
	/// <param name="o">The object that raised the callback event.</param>
	/// <param name="context">The streaming context.</param>
	// Token: 0x02000087 RID: 135
	// (Invoke) Token: 0x060006AC RID: 1708
	public delegate void SerializationCallback(object o, StreamingContext context);
}
