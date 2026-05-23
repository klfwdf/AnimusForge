using System;
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json.Bson
{
	/// <summary>
	/// Represents a BSON Oid (object id).
	/// </summary>
	// Token: 0x02000107 RID: 263
	[Obsolete("BSON reading and writing has been moved to its own package. See https://www.nuget.org/packages/Newtonsoft.Json.Bson for more details.")]
	public class BsonObjectId
	{
		/// <summary>
		/// Gets or sets the value of the Oid.
		/// </summary>
		/// <value>The value of the Oid.</value>
		// Token: 0x17000273 RID: 627
		// (get) Token: 0x06000D79 RID: 3449 RVA: 0x00036389 File Offset: 0x00034589
		public byte[] Value { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Bson.BsonObjectId" /> class.
		/// </summary>
		/// <param name="value">The Oid value.</param>
		// Token: 0x06000D7A RID: 3450 RVA: 0x00036391 File Offset: 0x00034591
		public BsonObjectId(byte[] value)
		{
			ValidationUtils.ArgumentNotNull(value, "value");
			if (value.Length != 12)
			{
				throw new ArgumentException("An ObjectId must be 12 bytes", "value");
			}
			this.Value = value;
		}
	}
}
