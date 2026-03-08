using System;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Newtonsoft.Json.Linq
{
	/// <summary>
	/// Represents a raw JSON string.
	/// </summary>
	// Token: 0x020000C2 RID: 194
	[NullableContext(1)]
	[Nullable(0)]
	public class JRaw : JValue
	{
		/// <summary>
		/// Asynchronously creates an instance of <see cref="T:Newtonsoft.Json.Linq.JRaw" /> with the content of the reader's current token.
		/// </summary>
		/// <param name="reader">The reader.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task`1" /> representing the asynchronous creation. The <see cref="P:System.Threading.Tasks.Task`1.Result" />
		/// property returns an instance of <see cref="T:Newtonsoft.Json.Linq.JRaw" /> with the content of the reader's current token.</returns>
		// Token: 0x06000AB9 RID: 2745 RVA: 0x0002B1AC File Offset: 0x000293AC
		public static async Task<JRaw> CreateAsync(JsonReader reader, CancellationToken cancellationToken = default(CancellationToken))
		{
			JRaw result;
			using (StringWriter sw = new StringWriter(CultureInfo.InvariantCulture))
			{
				using (JsonTextWriter jsonWriter = new JsonTextWriter(sw))
				{
					await jsonWriter.WriteTokenSyncReadingAsync(reader, cancellationToken).ConfigureAwait(false);
					result = new JRaw(sw.ToString());
				}
			}
			return result;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Linq.JRaw" /> class from another <see cref="T:Newtonsoft.Json.Linq.JRaw" /> object.
		/// </summary>
		/// <param name="other">A <see cref="T:Newtonsoft.Json.Linq.JRaw" /> object to copy from.</param>
		// Token: 0x06000ABA RID: 2746 RVA: 0x0002B1F7 File Offset: 0x000293F7
		public JRaw(JRaw other)
			: base(other, null)
		{
		}

		// Token: 0x06000ABB RID: 2747 RVA: 0x0002B201 File Offset: 0x00029401
		internal JRaw(JRaw other, [Nullable(2)] JsonCloneSettings settings)
			: base(other, settings)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Linq.JRaw" /> class.
		/// </summary>
		/// <param name="rawJson">The raw json.</param>
		// Token: 0x06000ABC RID: 2748 RVA: 0x0002B20B File Offset: 0x0002940B
		[NullableContext(2)]
		public JRaw(object rawJson)
			: base(rawJson, JTokenType.Raw)
		{
		}

		/// <summary>
		/// Creates an instance of <see cref="T:Newtonsoft.Json.Linq.JRaw" /> with the content of the reader's current token.
		/// </summary>
		/// <param name="reader">The reader.</param>
		/// <returns>An instance of <see cref="T:Newtonsoft.Json.Linq.JRaw" /> with the content of the reader's current token.</returns>
		// Token: 0x06000ABD RID: 2749 RVA: 0x0002B218 File Offset: 0x00029418
		public static JRaw Create(JsonReader reader)
		{
			JRaw result;
			using (StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture))
			{
				using (JsonTextWriter jsonTextWriter = new JsonTextWriter(stringWriter))
				{
					jsonTextWriter.WriteToken(reader);
					result = new JRaw(stringWriter.ToString());
				}
			}
			return result;
		}

		// Token: 0x06000ABE RID: 2750 RVA: 0x0002B280 File Offset: 0x00029480
		internal override JToken CloneToken([Nullable(2)] JsonCloneSettings settings)
		{
			return new JRaw(this, settings);
		}
	}
}
