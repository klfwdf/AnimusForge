using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Newtonsoft.Json
{
	/// <summary>
	/// The exception thrown when an error occurs during JSON serialization or deserialization.
	/// </summary>
	// Token: 0x0200002A RID: 42
	[NullableContext(1)]
	[Nullable(0)]
	[Serializable]
	public class JsonSerializationException : JsonException
	{
		/// <summary>
		/// Gets the line number indicating where the error occurred.
		/// </summary>
		/// <value>The line number indicating where the error occurred.</value>
		// Token: 0x1700003F RID: 63
		// (get) Token: 0x06000134 RID: 308 RVA: 0x00004BF4 File Offset: 0x00002DF4
		public int LineNumber { get; }

		/// <summary>
		/// Gets the line position indicating where the error occurred.
		/// </summary>
		/// <value>The line position indicating where the error occurred.</value>
		// Token: 0x17000040 RID: 64
		// (get) Token: 0x06000135 RID: 309 RVA: 0x00004BFC File Offset: 0x00002DFC
		public int LinePosition { get; }

		/// <summary>
		/// Gets the path to the JSON where the error occurred.
		/// </summary>
		/// <value>The path to the JSON where the error occurred.</value>
		// Token: 0x17000041 RID: 65
		// (get) Token: 0x06000136 RID: 310 RVA: 0x00004C04 File Offset: 0x00002E04
		[Nullable(2)]
		public string Path
		{
			[NullableContext(2)]
			get;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.JsonSerializationException" /> class.
		/// </summary>
		// Token: 0x06000137 RID: 311 RVA: 0x00004C0C File Offset: 0x00002E0C
		public JsonSerializationException()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.JsonSerializationException" /> class
		/// with a specified error message.
		/// </summary>
		/// <param name="message">The error message that explains the reason for the exception.</param>
		// Token: 0x06000138 RID: 312 RVA: 0x00004C14 File Offset: 0x00002E14
		public JsonSerializationException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.JsonSerializationException" /> class
		/// with a specified error message and a reference to the inner exception that is the cause of this exception.
		/// </summary>
		/// <param name="message">The error message that explains the reason for the exception.</param>
		/// <param name="innerException">The exception that is the cause of the current exception, or <c>null</c> if no inner exception is specified.</param>
		// Token: 0x06000139 RID: 313 RVA: 0x00004C1D File Offset: 0x00002E1D
		public JsonSerializationException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.JsonSerializationException" /> class.
		/// </summary>
		/// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
		/// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext" /> that contains contextual information about the source or destination.</param>
		/// <exception cref="T:System.ArgumentNullException">The <paramref name="info" /> parameter is <c>null</c>.</exception>
		/// <exception cref="T:System.Runtime.Serialization.SerializationException">The class name is <c>null</c> or <see cref="P:System.Exception.HResult" /> is zero (0).</exception>
		// Token: 0x0600013A RID: 314 RVA: 0x00004C27 File Offset: 0x00002E27
		public JsonSerializationException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.JsonSerializationException" /> class
		/// with a specified error message, JSON path, line number, line position, and a reference to the inner exception that is the cause of this exception.
		/// </summary>
		/// <param name="message">The error message that explains the reason for the exception.</param>
		/// <param name="path">The path to the JSON where the error occurred.</param>
		/// <param name="lineNumber">The line number indicating where the error occurred.</param>
		/// <param name="linePosition">The line position indicating where the error occurred.</param>
		/// <param name="innerException">The exception that is the cause of the current exception, or <c>null</c> if no inner exception is specified.</param>
		// Token: 0x0600013B RID: 315 RVA: 0x00004C31 File Offset: 0x00002E31
		public JsonSerializationException(string message, string path, int lineNumber, int linePosition, [Nullable(2)] Exception innerException)
			: base(message, innerException)
		{
			this.Path = path;
			this.LineNumber = lineNumber;
			this.LinePosition = linePosition;
		}

		// Token: 0x0600013C RID: 316 RVA: 0x00004C52 File Offset: 0x00002E52
		internal static JsonSerializationException Create(JsonReader reader, string message)
		{
			return JsonSerializationException.Create(reader, message, null);
		}

		// Token: 0x0600013D RID: 317 RVA: 0x00004C5C File Offset: 0x00002E5C
		internal static JsonSerializationException Create(JsonReader reader, string message, [Nullable(2)] Exception ex)
		{
			return JsonSerializationException.Create(reader as IJsonLineInfo, reader.Path, message, ex);
		}

		// Token: 0x0600013E RID: 318 RVA: 0x00004C74 File Offset: 0x00002E74
		internal static JsonSerializationException Create([Nullable(2)] IJsonLineInfo lineInfo, string path, string message, [Nullable(2)] Exception ex)
		{
			message = JsonPosition.FormatMessage(lineInfo, path, message);
			int lineNumber;
			int linePosition;
			if (lineInfo != null && lineInfo.HasLineInfo())
			{
				lineNumber = lineInfo.LineNumber;
				linePosition = lineInfo.LinePosition;
			}
			else
			{
				lineNumber = 0;
				linePosition = 0;
			}
			return new JsonSerializationException(message, path, lineNumber, linePosition, ex);
		}
	}
}
