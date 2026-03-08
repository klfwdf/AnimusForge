using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Newtonsoft.Json
{
	/// <summary>
	/// The exception thrown when an error occurs while reading JSON text.
	/// </summary>
	// Token: 0x02000028 RID: 40
	[NullableContext(1)]
	[Nullable(0)]
	[Serializable]
	public class JsonReaderException : JsonException
	{
		/// <summary>
		/// Gets the line number indicating where the error occurred.
		/// </summary>
		/// <value>The line number indicating where the error occurred.</value>
		// Token: 0x1700003C RID: 60
		// (get) Token: 0x06000128 RID: 296 RVA: 0x00004B2E File Offset: 0x00002D2E
		public int LineNumber { get; }

		/// <summary>
		/// Gets the line position indicating where the error occurred.
		/// </summary>
		/// <value>The line position indicating where the error occurred.</value>
		// Token: 0x1700003D RID: 61
		// (get) Token: 0x06000129 RID: 297 RVA: 0x00004B36 File Offset: 0x00002D36
		public int LinePosition { get; }

		/// <summary>
		/// Gets the path to the JSON where the error occurred.
		/// </summary>
		/// <value>The path to the JSON where the error occurred.</value>
		// Token: 0x1700003E RID: 62
		// (get) Token: 0x0600012A RID: 298 RVA: 0x00004B3E File Offset: 0x00002D3E
		[Nullable(2)]
		public string Path
		{
			[NullableContext(2)]
			get;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.JsonReaderException" /> class.
		/// </summary>
		// Token: 0x0600012B RID: 299 RVA: 0x00004B46 File Offset: 0x00002D46
		public JsonReaderException()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.JsonReaderException" /> class
		/// with a specified error message.
		/// </summary>
		/// <param name="message">The error message that explains the reason for the exception.</param>
		// Token: 0x0600012C RID: 300 RVA: 0x00004B4E File Offset: 0x00002D4E
		public JsonReaderException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.JsonReaderException" /> class
		/// with a specified error message and a reference to the inner exception that is the cause of this exception.
		/// </summary>
		/// <param name="message">The error message that explains the reason for the exception.</param>
		/// <param name="innerException">The exception that is the cause of the current exception, or <c>null</c> if no inner exception is specified.</param>
		// Token: 0x0600012D RID: 301 RVA: 0x00004B57 File Offset: 0x00002D57
		public JsonReaderException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.JsonReaderException" /> class.
		/// </summary>
		/// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
		/// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext" /> that contains contextual information about the source or destination.</param>
		/// <exception cref="T:System.ArgumentNullException">The <paramref name="info" /> parameter is <c>null</c>.</exception>
		/// <exception cref="T:System.Runtime.Serialization.SerializationException">The class name is <c>null</c> or <see cref="P:System.Exception.HResult" /> is zero (0).</exception>
		// Token: 0x0600012E RID: 302 RVA: 0x00004B61 File Offset: 0x00002D61
		public JsonReaderException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.JsonReaderException" /> class
		/// with a specified error message, JSON path, line number, line position, and a reference to the inner exception that is the cause of this exception.
		/// </summary>
		/// <param name="message">The error message that explains the reason for the exception.</param>
		/// <param name="path">The path to the JSON where the error occurred.</param>
		/// <param name="lineNumber">The line number indicating where the error occurred.</param>
		/// <param name="linePosition">The line position indicating where the error occurred.</param>
		/// <param name="innerException">The exception that is the cause of the current exception, or <c>null</c> if no inner exception is specified.</param>
		// Token: 0x0600012F RID: 303 RVA: 0x00004B6B File Offset: 0x00002D6B
		public JsonReaderException(string message, string path, int lineNumber, int linePosition, [Nullable(2)] Exception innerException)
			: base(message, innerException)
		{
			this.Path = path;
			this.LineNumber = lineNumber;
			this.LinePosition = linePosition;
		}

		// Token: 0x06000130 RID: 304 RVA: 0x00004B8C File Offset: 0x00002D8C
		internal static JsonReaderException Create(JsonReader reader, string message)
		{
			return JsonReaderException.Create(reader, message, null);
		}

		// Token: 0x06000131 RID: 305 RVA: 0x00004B96 File Offset: 0x00002D96
		internal static JsonReaderException Create(JsonReader reader, string message, [Nullable(2)] Exception ex)
		{
			return JsonReaderException.Create(reader as IJsonLineInfo, reader.Path, message, ex);
		}

		// Token: 0x06000132 RID: 306 RVA: 0x00004BAC File Offset: 0x00002DAC
		internal static JsonReaderException Create([Nullable(2)] IJsonLineInfo lineInfo, string path, string message, [Nullable(2)] Exception ex)
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
			return new JsonReaderException(message, path, lineNumber, linePosition, ex);
		}
	}
}
