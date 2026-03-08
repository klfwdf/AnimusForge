using System;

namespace Newtonsoft.Json
{
	/// <summary>
	/// Specifies the state of the <see cref="T:Newtonsoft.Json.JsonWriter" />.
	/// </summary>
	// Token: 0x0200003F RID: 63
	public enum WriteState
	{
		/// <summary>
		/// An exception has been thrown, which has left the <see cref="T:Newtonsoft.Json.JsonWriter" /> in an invalid state.
		/// You may call the <see cref="M:Newtonsoft.Json.JsonWriter.Close" /> method to put the <see cref="T:Newtonsoft.Json.JsonWriter" /> in the <c>Closed</c> state.
		/// Any other <see cref="T:Newtonsoft.Json.JsonWriter" /> method calls result in an <see cref="T:System.InvalidOperationException" /> being thrown.
		/// </summary>
		// Token: 0x04000144 RID: 324
		Error,
		/// <summary>
		/// The <see cref="M:Newtonsoft.Json.JsonWriter.Close" /> method has been called.
		/// </summary>
		// Token: 0x04000145 RID: 325
		Closed,
		/// <summary>
		/// An object is being written. 
		/// </summary>
		// Token: 0x04000146 RID: 326
		Object,
		/// <summary>
		/// An array is being written.
		/// </summary>
		// Token: 0x04000147 RID: 327
		Array,
		/// <summary>
		/// A constructor is being written.
		/// </summary>
		// Token: 0x04000148 RID: 328
		Constructor,
		/// <summary>
		/// A property is being written.
		/// </summary>
		// Token: 0x04000149 RID: 329
		Property,
		/// <summary>
		/// A <see cref="T:Newtonsoft.Json.JsonWriter" /> write method has not been called.
		/// </summary>
		// Token: 0x0400014A RID: 330
		Start
	}
}
