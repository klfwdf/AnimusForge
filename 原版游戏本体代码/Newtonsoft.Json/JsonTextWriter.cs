using System;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json
{
	/// <summary>
	/// Represents a writer that provides a fast, non-cached, forward-only way of generating JSON data.
	/// </summary>
	// Token: 0x0200002F RID: 47
	[NullableContext(1)]
	[Nullable(0)]
	public class JsonTextWriter : JsonWriter
	{
		/// <summary>
		/// Asynchronously flushes whatever is in the buffer to the destination and also flushes the destination.
		/// </summary>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x06000260 RID: 608 RVA: 0x0000A1D6 File Offset: 0x000083D6
		public override Task FlushAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.FlushAsync(cancellationToken);
			}
			return this.DoFlushAsync(cancellationToken);
		}

		// Token: 0x06000261 RID: 609 RVA: 0x0000A1EF File Offset: 0x000083EF
		internal Task DoFlushAsync(CancellationToken cancellationToken)
		{
			return cancellationToken.CancelIfRequestedAsync() ?? this._writer.FlushAsync();
		}

		/// <summary>
		/// Asynchronously writes the JSON value delimiter.
		/// </summary>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x06000262 RID: 610 RVA: 0x0000A206 File Offset: 0x00008406
		protected override Task WriteValueDelimiterAsync(CancellationToken cancellationToken)
		{
			if (!this._safeAsync)
			{
				return base.WriteValueDelimiterAsync(cancellationToken);
			}
			return this.DoWriteValueDelimiterAsync(cancellationToken);
		}

		// Token: 0x06000263 RID: 611 RVA: 0x0000A21F File Offset: 0x0000841F
		internal Task DoWriteValueDelimiterAsync(CancellationToken cancellationToken)
		{
			return this._writer.WriteAsync(',', cancellationToken);
		}

		/// <summary>
		/// Asynchronously writes the specified end token.
		/// </summary>
		/// <param name="token">The end token to write.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x06000264 RID: 612 RVA: 0x0000A22F File Offset: 0x0000842F
		protected override Task WriteEndAsync(JsonToken token, CancellationToken cancellationToken)
		{
			if (!this._safeAsync)
			{
				return base.WriteEndAsync(token, cancellationToken);
			}
			return this.DoWriteEndAsync(token, cancellationToken);
		}

		// Token: 0x06000265 RID: 613 RVA: 0x0000A24C File Offset: 0x0000844C
		internal Task DoWriteEndAsync(JsonToken token, CancellationToken cancellationToken)
		{
			switch (token)
			{
			case JsonToken.EndObject:
				return this._writer.WriteAsync('}', cancellationToken);
			case JsonToken.EndArray:
				return this._writer.WriteAsync(']', cancellationToken);
			case JsonToken.EndConstructor:
				return this._writer.WriteAsync(')', cancellationToken);
			default:
				throw JsonWriterException.Create(this, "Invalid JsonToken: " + token.ToString(), null);
			}
		}

		/// <summary>
		/// Asynchronously closes this writer.
		/// If <see cref="P:Newtonsoft.Json.JsonWriter.CloseOutput" /> is set to <c>true</c>, the destination is also closed.
		/// </summary>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x06000266 RID: 614 RVA: 0x0000A2BB File Offset: 0x000084BB
		public override Task CloseAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.CloseAsync(cancellationToken);
			}
			return this.DoCloseAsync(cancellationToken);
		}

		// Token: 0x06000267 RID: 615 RVA: 0x0000A2D4 File Offset: 0x000084D4
		internal async Task DoCloseAsync(CancellationToken cancellationToken)
		{
			if (base.Top == 0)
			{
				cancellationToken.ThrowIfCancellationRequested();
			}
			while (base.Top > 0)
			{
				await this.WriteEndAsync(cancellationToken).ConfigureAwait(false);
			}
			await this.CloseBufferAndWriterAsync().ConfigureAwait(false);
		}

		// Token: 0x06000268 RID: 616 RVA: 0x0000A320 File Offset: 0x00008520
		private async Task CloseBufferAndWriterAsync()
		{
			if (this._writeBuffer != null)
			{
				BufferUtils.ReturnBuffer(this._arrayPool, this._writeBuffer);
				this._writeBuffer = null;
			}
			if (base.CloseOutput && this._writer != null)
			{
				await this._writer.FlushAsync().ConfigureAwait(false);
				this._writer.Close();
			}
		}

		/// <summary>
		/// Asynchronously writes the end of the current JSON object or array.
		/// </summary>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x06000269 RID: 617 RVA: 0x0000A363 File Offset: 0x00008563
		public override Task WriteEndAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteEndAsync(cancellationToken);
			}
			return base.WriteEndInternalAsync(cancellationToken);
		}

		/// <summary>
		/// Asynchronously writes indent characters.
		/// </summary>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x0600026A RID: 618 RVA: 0x0000A37C File Offset: 0x0000857C
		protected override Task WriteIndentAsync(CancellationToken cancellationToken)
		{
			if (!this._safeAsync)
			{
				return base.WriteIndentAsync(cancellationToken);
			}
			return this.DoWriteIndentAsync(cancellationToken);
		}

		// Token: 0x0600026B RID: 619 RVA: 0x0000A398 File Offset: 0x00008598
		internal Task DoWriteIndentAsync(CancellationToken cancellationToken)
		{
			int num = base.Top * this._indentation;
			int num2 = this.SetIndentChars();
			if (num <= 12)
			{
				return this._writer.WriteAsync(this._indentChars, 0, num2 + num, cancellationToken);
			}
			return this.WriteIndentAsync(num, num2, cancellationToken);
		}

		// Token: 0x0600026C RID: 620 RVA: 0x0000A3E0 File Offset: 0x000085E0
		private async Task WriteIndentAsync(int currentIndentCount, int newLineLen, CancellationToken cancellationToken)
		{
			await this._writer.WriteAsync(this._indentChars, 0, newLineLen + Math.Min(currentIndentCount, 12), cancellationToken).ConfigureAwait(false);
			while ((currentIndentCount -= 12) > 0)
			{
				await this._writer.WriteAsync(this._indentChars, newLineLen, Math.Min(currentIndentCount, 12), cancellationToken).ConfigureAwait(false);
			}
		}

		// Token: 0x0600026D RID: 621 RVA: 0x0000A43C File Offset: 0x0000863C
		private Task WriteValueInternalAsync(JsonToken token, string value, CancellationToken cancellationToken)
		{
			Task task = base.InternalWriteValueAsync(token, cancellationToken);
			if (task.IsCompletedSuccessfully())
			{
				return this._writer.WriteAsync(value, cancellationToken);
			}
			return this.WriteValueInternalAsync(task, value, cancellationToken);
		}

		// Token: 0x0600026E RID: 622 RVA: 0x0000A474 File Offset: 0x00008674
		private async Task WriteValueInternalAsync(Task task, string value, CancellationToken cancellationToken)
		{
			await task.ConfigureAwait(false);
			await this._writer.WriteAsync(value, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Asynchronously writes an indent space.
		/// </summary>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x0600026F RID: 623 RVA: 0x0000A4CF File Offset: 0x000086CF
		protected override Task WriteIndentSpaceAsync(CancellationToken cancellationToken)
		{
			if (!this._safeAsync)
			{
				return base.WriteIndentSpaceAsync(cancellationToken);
			}
			return this.DoWriteIndentSpaceAsync(cancellationToken);
		}

		// Token: 0x06000270 RID: 624 RVA: 0x0000A4E8 File Offset: 0x000086E8
		internal Task DoWriteIndentSpaceAsync(CancellationToken cancellationToken)
		{
			return this._writer.WriteAsync(' ', cancellationToken);
		}

		/// <summary>
		/// Asynchronously writes raw JSON without changing the writer's state.
		/// </summary>
		/// <param name="json">The raw JSON to write.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x06000271 RID: 625 RVA: 0x0000A4F8 File Offset: 0x000086F8
		public override Task WriteRawAsync([Nullable(2)] string json, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteRawAsync(json, cancellationToken);
			}
			return this.DoWriteRawAsync(json, cancellationToken);
		}

		// Token: 0x06000272 RID: 626 RVA: 0x0000A513 File Offset: 0x00008713
		internal Task DoWriteRawAsync([Nullable(2)] string json, CancellationToken cancellationToken)
		{
			return this._writer.WriteAsync(json, cancellationToken);
		}

		/// <summary>
		/// Asynchronously writes a null value.
		/// </summary>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x06000273 RID: 627 RVA: 0x0000A522 File Offset: 0x00008722
		public override Task WriteNullAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteNullAsync(cancellationToken);
			}
			return this.DoWriteNullAsync(cancellationToken);
		}

		// Token: 0x06000274 RID: 628 RVA: 0x0000A53B File Offset: 0x0000873B
		internal Task DoWriteNullAsync(CancellationToken cancellationToken)
		{
			return this.WriteValueInternalAsync(JsonToken.Null, JsonConvert.Null, cancellationToken);
		}

		// Token: 0x06000275 RID: 629 RVA: 0x0000A54C File Offset: 0x0000874C
		private Task WriteDigitsAsync(ulong uvalue, bool negative, CancellationToken cancellationToken)
		{
			if ((uvalue <= 9UL) & !negative)
			{
				return this._writer.WriteAsync((char)(48UL + uvalue), cancellationToken);
			}
			int count = this.WriteNumberToBuffer(uvalue, negative);
			return this._writer.WriteAsync(this._writeBuffer, 0, count, cancellationToken);
		}

		// Token: 0x06000276 RID: 630 RVA: 0x0000A59C File Offset: 0x0000879C
		private Task WriteIntegerValueAsync(ulong uvalue, bool negative, CancellationToken cancellationToken)
		{
			Task task = base.InternalWriteValueAsync(JsonToken.Integer, cancellationToken);
			if (task.IsCompletedSuccessfully())
			{
				return this.WriteDigitsAsync(uvalue, negative, cancellationToken);
			}
			return this.WriteIntegerValueAsync(task, uvalue, negative, cancellationToken);
		}

		// Token: 0x06000277 RID: 631 RVA: 0x0000A5D0 File Offset: 0x000087D0
		private async Task WriteIntegerValueAsync(Task task, ulong uvalue, bool negative, CancellationToken cancellationToken)
		{
			await task.ConfigureAwait(false);
			await this.WriteDigitsAsync(uvalue, negative, cancellationToken).ConfigureAwait(false);
		}

		// Token: 0x06000278 RID: 632 RVA: 0x0000A634 File Offset: 0x00008834
		internal Task WriteIntegerValueAsync(long value, CancellationToken cancellationToken)
		{
			bool flag = value < 0L;
			if (flag)
			{
				value = -value;
			}
			return this.WriteIntegerValueAsync((ulong)value, flag, cancellationToken);
		}

		// Token: 0x06000279 RID: 633 RVA: 0x0000A657 File Offset: 0x00008857
		internal Task WriteIntegerValueAsync(ulong uvalue, CancellationToken cancellationToken)
		{
			return this.WriteIntegerValueAsync(uvalue, false, cancellationToken);
		}

		// Token: 0x0600027A RID: 634 RVA: 0x0000A664 File Offset: 0x00008864
		private Task WriteEscapedStringAsync(string value, bool quote, CancellationToken cancellationToken)
		{
			return JavaScriptUtils.WriteEscapedJavaScriptStringAsync(this._writer, value, this._quoteChar, quote, this._charEscapeFlags, base.StringEscapeHandling, this, this._writeBuffer, cancellationToken);
		}

		/// <summary>
		/// Asynchronously writes the property name of a name/value pair of a JSON object.
		/// </summary>
		/// <param name="name">The name of the property.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x0600027B RID: 635 RVA: 0x0000A698 File Offset: 0x00008898
		public override Task WritePropertyNameAsync(string name, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WritePropertyNameAsync(name, cancellationToken);
			}
			return this.DoWritePropertyNameAsync(name, cancellationToken);
		}

		// Token: 0x0600027C RID: 636 RVA: 0x0000A6B4 File Offset: 0x000088B4
		internal Task DoWritePropertyNameAsync(string name, CancellationToken cancellationToken)
		{
			Task task = base.InternalWritePropertyNameAsync(name, cancellationToken);
			if (!task.IsCompletedSuccessfully())
			{
				return this.DoWritePropertyNameAsync(task, name, cancellationToken);
			}
			task = this.WriteEscapedStringAsync(name, this._quoteName, cancellationToken);
			if (task.IsCompletedSuccessfully())
			{
				return this._writer.WriteAsync(':', cancellationToken);
			}
			return JavaScriptUtils.WriteCharAsync(task, this._writer, ':', cancellationToken);
		}

		// Token: 0x0600027D RID: 637 RVA: 0x0000A714 File Offset: 0x00008914
		private async Task DoWritePropertyNameAsync(Task task, string name, CancellationToken cancellationToken)
		{
			await task.ConfigureAwait(false);
			await this.WriteEscapedStringAsync(name, this._quoteName, cancellationToken).ConfigureAwait(false);
			await this._writer.WriteAsync(':').ConfigureAwait(false);
		}

		/// <summary>
		/// Asynchronously writes the property name of a name/value pair of a JSON object.
		/// </summary>
		/// <param name="name">The name of the property.</param>
		/// <param name="escape">A flag to indicate whether the text should be escaped when it is written as a JSON property name.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x0600027E RID: 638 RVA: 0x0000A76F File Offset: 0x0000896F
		public override Task WritePropertyNameAsync(string name, bool escape, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WritePropertyNameAsync(name, escape, cancellationToken);
			}
			return this.DoWritePropertyNameAsync(name, escape, cancellationToken);
		}

		// Token: 0x0600027F RID: 639 RVA: 0x0000A78C File Offset: 0x0000898C
		internal async Task DoWritePropertyNameAsync(string name, bool escape, CancellationToken cancellationToken)
		{
			await base.InternalWritePropertyNameAsync(name, cancellationToken).ConfigureAwait(false);
			if (escape)
			{
				await this.WriteEscapedStringAsync(name, this._quoteName, cancellationToken).ConfigureAwait(false);
			}
			else
			{
				if (this._quoteName)
				{
					await this._writer.WriteAsync(this._quoteChar).ConfigureAwait(false);
				}
				await this._writer.WriteAsync(name, cancellationToken).ConfigureAwait(false);
				if (this._quoteName)
				{
					await this._writer.WriteAsync(this._quoteChar).ConfigureAwait(false);
				}
			}
			await this._writer.WriteAsync(':').ConfigureAwait(false);
		}

		/// <summary>
		/// Asynchronously writes the beginning of a JSON array.
		/// </summary>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x06000280 RID: 640 RVA: 0x0000A7E7 File Offset: 0x000089E7
		public override Task WriteStartArrayAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteStartArrayAsync(cancellationToken);
			}
			return this.DoWriteStartArrayAsync(cancellationToken);
		}

		// Token: 0x06000281 RID: 641 RVA: 0x0000A800 File Offset: 0x00008A00
		internal Task DoWriteStartArrayAsync(CancellationToken cancellationToken)
		{
			Task task = base.InternalWriteStartAsync(JsonToken.StartArray, JsonContainerType.Array, cancellationToken);
			if (task.IsCompletedSuccessfully())
			{
				return this._writer.WriteAsync('[', cancellationToken);
			}
			return this.DoWriteStartArrayAsync(task, cancellationToken);
		}

		// Token: 0x06000282 RID: 642 RVA: 0x0000A838 File Offset: 0x00008A38
		internal async Task DoWriteStartArrayAsync(Task task, CancellationToken cancellationToken)
		{
			await task.ConfigureAwait(false);
			await this._writer.WriteAsync('[', cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Asynchronously writes the beginning of a JSON object.
		/// </summary>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x06000283 RID: 643 RVA: 0x0000A88B File Offset: 0x00008A8B
		public override Task WriteStartObjectAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteStartObjectAsync(cancellationToken);
			}
			return this.DoWriteStartObjectAsync(cancellationToken);
		}

		// Token: 0x06000284 RID: 644 RVA: 0x0000A8A4 File Offset: 0x00008AA4
		internal Task DoWriteStartObjectAsync(CancellationToken cancellationToken)
		{
			Task task = base.InternalWriteStartAsync(JsonToken.StartObject, JsonContainerType.Object, cancellationToken);
			if (task.IsCompletedSuccessfully())
			{
				return this._writer.WriteAsync('{', cancellationToken);
			}
			return this.DoWriteStartObjectAsync(task, cancellationToken);
		}

		// Token: 0x06000285 RID: 645 RVA: 0x0000A8DC File Offset: 0x00008ADC
		internal async Task DoWriteStartObjectAsync(Task task, CancellationToken cancellationToken)
		{
			await task.ConfigureAwait(false);
			await this._writer.WriteAsync('{', cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Asynchronously writes the start of a constructor with the given name.
		/// </summary>
		/// <param name="name">The name of the constructor.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x06000286 RID: 646 RVA: 0x0000A92F File Offset: 0x00008B2F
		public override Task WriteStartConstructorAsync(string name, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteStartConstructorAsync(name, cancellationToken);
			}
			return this.DoWriteStartConstructorAsync(name, cancellationToken);
		}

		// Token: 0x06000287 RID: 647 RVA: 0x0000A94C File Offset: 0x00008B4C
		internal async Task DoWriteStartConstructorAsync(string name, CancellationToken cancellationToken)
		{
			await base.InternalWriteStartAsync(JsonToken.StartConstructor, JsonContainerType.Constructor, cancellationToken).ConfigureAwait(false);
			await this._writer.WriteAsync("new ", cancellationToken).ConfigureAwait(false);
			await this._writer.WriteAsync(name, cancellationToken).ConfigureAwait(false);
			await this._writer.WriteAsync('(').ConfigureAwait(false);
		}

		/// <summary>
		/// Asynchronously writes an undefined value.
		/// </summary>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x06000288 RID: 648 RVA: 0x0000A99F File Offset: 0x00008B9F
		public override Task WriteUndefinedAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteUndefinedAsync(cancellationToken);
			}
			return this.DoWriteUndefinedAsync(cancellationToken);
		}

		// Token: 0x06000289 RID: 649 RVA: 0x0000A9B8 File Offset: 0x00008BB8
		internal Task DoWriteUndefinedAsync(CancellationToken cancellationToken)
		{
			Task task = base.InternalWriteValueAsync(JsonToken.Undefined, cancellationToken);
			if (task.IsCompletedSuccessfully())
			{
				return this._writer.WriteAsync(JsonConvert.Undefined, cancellationToken);
			}
			return this.DoWriteUndefinedAsync(task, cancellationToken);
		}

		// Token: 0x0600028A RID: 650 RVA: 0x0000A9F4 File Offset: 0x00008BF4
		private async Task DoWriteUndefinedAsync(Task task, CancellationToken cancellationToken)
		{
			await task.ConfigureAwait(false);
			await this._writer.WriteAsync(JsonConvert.Undefined, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Asynchronously writes the given white space.
		/// </summary>
		/// <param name="ws">The string of white space characters.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x0600028B RID: 651 RVA: 0x0000AA47 File Offset: 0x00008C47
		public override Task WriteWhitespaceAsync(string ws, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteWhitespaceAsync(ws, cancellationToken);
			}
			return this.DoWriteWhitespaceAsync(ws, cancellationToken);
		}

		// Token: 0x0600028C RID: 652 RVA: 0x0000AA62 File Offset: 0x00008C62
		internal Task DoWriteWhitespaceAsync(string ws, CancellationToken cancellationToken)
		{
			base.InternalWriteWhitespace(ws);
			return this._writer.WriteAsync(ws, cancellationToken);
		}

		/// <summary>
		/// Asynchronously writes a <see cref="T:System.Nullable`1" /> of <see cref="T:System.Boolean" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Nullable`1" /> of <see cref="T:System.Boolean" /> value to write.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x0600028D RID: 653 RVA: 0x0000AA78 File Offset: 0x00008C78
		public override Task WriteValueAsync(bool value, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteValueAsync(value, cancellationToken);
			}
			return this.DoWriteValueAsync(value, cancellationToken);
		}

		// Token: 0x0600028E RID: 654 RVA: 0x0000AA93 File Offset: 0x00008C93
		internal Task DoWriteValueAsync(bool value, CancellationToken cancellationToken)
		{
			return this.WriteValueInternalAsync(JsonToken.Boolean, JsonConvert.ToString(value), cancellationToken);
		}

		/// <summary>
		/// Asynchronously writes a <see cref="T:System.Boolean" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Boolean" /> value to write.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x0600028F RID: 655 RVA: 0x0000AAA4 File Offset: 0x00008CA4
		public override Task WriteValueAsync(bool? value, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteValueAsync(value, cancellationToken);
			}
			return this.DoWriteValueAsync(value, cancellationToken);
		}

		// Token: 0x06000290 RID: 656 RVA: 0x0000AABF File Offset: 0x00008CBF
		internal Task DoWriteValueAsync(bool? value, CancellationToken cancellationToken)
		{
			if (value != null)
			{
				return this.DoWriteValueAsync(value.GetValueOrDefault(), cancellationToken);
			}
			return this.DoWriteNullAsync(cancellationToken);
		}

		/// <summary>
		/// Asynchronously writes a <see cref="T:System.Byte" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Byte" /> value to write.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x06000291 RID: 657 RVA: 0x0000AAE0 File Offset: 0x00008CE0
		public override Task WriteValueAsync(byte value, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteValueAsync(value, cancellationToken);
			}
			return this.WriteIntegerValueAsync((long)((ulong)value), cancellationToken);
		}

		/// <summary>
		/// Asynchronously writes a <see cref="T:System.Nullable`1" /> of <see cref="T:System.Byte" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Nullable`1" /> of <see cref="T:System.Byte" /> value to write.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x06000292 RID: 658 RVA: 0x0000AAFC File Offset: 0x00008CFC
		public override Task WriteValueAsync(byte? value, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteValueAsync(value, cancellationToken);
			}
			return this.DoWriteValueAsync(value, cancellationToken);
		}

		// Token: 0x06000293 RID: 659 RVA: 0x0000AB17 File Offset: 0x00008D17
		internal Task DoWriteValueAsync(byte? value, CancellationToken cancellationToken)
		{
			if (value != null)
			{
				return this.WriteIntegerValueAsync((long)((ulong)value.GetValueOrDefault()), cancellationToken);
			}
			return this.DoWriteNullAsync(cancellationToken);
		}

		/// <summary>
		/// Asynchronously writes a <see cref="T:System.Byte" />[] value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Byte" />[] value to write.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x06000294 RID: 660 RVA: 0x0000AB39 File Offset: 0x00008D39
		public override Task WriteValueAsync([Nullable(2)] byte[] value, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteValueAsync(value, cancellationToken);
			}
			if (value != null)
			{
				return this.WriteValueNonNullAsync(value, cancellationToken);
			}
			return this.WriteNullAsync(cancellationToken);
		}

		// Token: 0x06000295 RID: 661 RVA: 0x0000AB60 File Offset: 0x00008D60
		internal async Task WriteValueNonNullAsync(byte[] value, CancellationToken cancellationToken)
		{
			await base.InternalWriteValueAsync(JsonToken.Bytes, cancellationToken).ConfigureAwait(false);
			await this._writer.WriteAsync(this._quoteChar).ConfigureAwait(false);
			await this.Base64Encoder.EncodeAsync(value, 0, value.Length, cancellationToken).ConfigureAwait(false);
			await this.Base64Encoder.FlushAsync(cancellationToken).ConfigureAwait(false);
			await this._writer.WriteAsync(this._quoteChar).ConfigureAwait(false);
		}

		/// <summary>
		/// Asynchronously writes a <see cref="T:System.Char" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Char" /> value to write.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x06000296 RID: 662 RVA: 0x0000ABB3 File Offset: 0x00008DB3
		public override Task WriteValueAsync(char value, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteValueAsync(value, cancellationToken);
			}
			return this.DoWriteValueAsync(value, cancellationToken);
		}

		// Token: 0x06000297 RID: 663 RVA: 0x0000ABCE File Offset: 0x00008DCE
		internal Task DoWriteValueAsync(char value, CancellationToken cancellationToken)
		{
			return this.WriteValueInternalAsync(JsonToken.String, JsonConvert.ToString(value), cancellationToken);
		}

		/// <summary>
		/// Asynchronously writes a <see cref="T:System.Nullable`1" /> of <see cref="T:System.Char" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Nullable`1" /> of <see cref="T:System.Char" /> value to write.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x06000298 RID: 664 RVA: 0x0000ABDF File Offset: 0x00008DDF
		public override Task WriteValueAsync(char? value, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteValueAsync(value, cancellationToken);
			}
			return this.DoWriteValueAsync(value, cancellationToken);
		}

		// Token: 0x06000299 RID: 665 RVA: 0x0000ABFA File Offset: 0x00008DFA
		internal Task DoWriteValueAsync(char? value, CancellationToken cancellationToken)
		{
			if (value != null)
			{
				return this.DoWriteValueAsync(value.GetValueOrDefault(), cancellationToken);
			}
			return this.DoWriteNullAsync(cancellationToken);
		}

		/// <summary>
		/// Asynchronously writes a <see cref="T:System.DateTime" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.DateTime" /> value to write.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x0600029A RID: 666 RVA: 0x0000AC1B File Offset: 0x00008E1B
		public override Task WriteValueAsync(DateTime value, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteValueAsync(value, cancellationToken);
			}
			return this.DoWriteValueAsync(value, cancellationToken);
		}

		// Token: 0x0600029B RID: 667 RVA: 0x0000AC38 File Offset: 0x00008E38
		internal async Task DoWriteValueAsync(DateTime value, CancellationToken cancellationToken)
		{
			await base.InternalWriteValueAsync(JsonToken.Date, cancellationToken).ConfigureAwait(false);
			value = DateTimeUtils.EnsureDateTime(value, base.DateTimeZoneHandling);
			if (StringUtils.IsNullOrEmpty(base.DateFormatString))
			{
				int count = this.WriteValueToBuffer(value);
				await this._writer.WriteAsync(this._writeBuffer, 0, count, cancellationToken).ConfigureAwait(false);
			}
			else
			{
				await this._writer.WriteAsync(this._quoteChar).ConfigureAwait(false);
				await this._writer.WriteAsync(value.ToString(base.DateFormatString, base.Culture), cancellationToken).ConfigureAwait(false);
				await this._writer.WriteAsync(this._quoteChar).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Asynchronously writes a <see cref="T:System.Nullable`1" /> of <see cref="T:System.DateTime" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Nullable`1" /> of <see cref="T:System.DateTime" /> value to write.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x0600029C RID: 668 RVA: 0x0000AC8B File Offset: 0x00008E8B
		public override Task WriteValueAsync(DateTime? value, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteValueAsync(value, cancellationToken);
			}
			return this.DoWriteValueAsync(value, cancellationToken);
		}

		// Token: 0x0600029D RID: 669 RVA: 0x0000ACA6 File Offset: 0x00008EA6
		internal Task DoWriteValueAsync(DateTime? value, CancellationToken cancellationToken)
		{
			if (value != null)
			{
				return this.DoWriteValueAsync(value.GetValueOrDefault(), cancellationToken);
			}
			return this.DoWriteNullAsync(cancellationToken);
		}

		/// <summary>
		/// Asynchronously writes a <see cref="T:System.DateTimeOffset" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.DateTimeOffset" /> value to write.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x0600029E RID: 670 RVA: 0x0000ACC7 File Offset: 0x00008EC7
		public override Task WriteValueAsync(DateTimeOffset value, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteValueAsync(value, cancellationToken);
			}
			return this.DoWriteValueAsync(value, cancellationToken);
		}

		// Token: 0x0600029F RID: 671 RVA: 0x0000ACE4 File Offset: 0x00008EE4
		internal async Task DoWriteValueAsync(DateTimeOffset value, CancellationToken cancellationToken)
		{
			await base.InternalWriteValueAsync(JsonToken.Date, cancellationToken).ConfigureAwait(false);
			if (StringUtils.IsNullOrEmpty(base.DateFormatString))
			{
				int count = this.WriteValueToBuffer(value);
				await this._writer.WriteAsync(this._writeBuffer, 0, count, cancellationToken).ConfigureAwait(false);
			}
			else
			{
				await this._writer.WriteAsync(this._quoteChar).ConfigureAwait(false);
				await this._writer.WriteAsync(value.ToString(base.DateFormatString, base.Culture), cancellationToken).ConfigureAwait(false);
				await this._writer.WriteAsync(this._quoteChar).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Asynchronously writes a <see cref="T:System.Nullable`1" /> of <see cref="T:System.DateTimeOffset" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Nullable`1" /> of <see cref="T:System.DateTimeOffset" /> value to write.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x060002A0 RID: 672 RVA: 0x0000AD37 File Offset: 0x00008F37
		public override Task WriteValueAsync(DateTimeOffset? value, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteValueAsync(value, cancellationToken);
			}
			return this.DoWriteValueAsync(value, cancellationToken);
		}

		// Token: 0x060002A1 RID: 673 RVA: 0x0000AD52 File Offset: 0x00008F52
		internal Task DoWriteValueAsync(DateTimeOffset? value, CancellationToken cancellationToken)
		{
			if (value != null)
			{
				return this.DoWriteValueAsync(value.GetValueOrDefault(), cancellationToken);
			}
			return this.DoWriteNullAsync(cancellationToken);
		}

		/// <summary>
		/// Asynchronously writes a <see cref="T:System.Decimal" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Decimal" /> value to write.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x060002A2 RID: 674 RVA: 0x0000AD73 File Offset: 0x00008F73
		public override Task WriteValueAsync(decimal value, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteValueAsync(value, cancellationToken);
			}
			return this.DoWriteValueAsync(value, cancellationToken);
		}

		// Token: 0x060002A3 RID: 675 RVA: 0x0000AD8E File Offset: 0x00008F8E
		internal Task DoWriteValueAsync(decimal value, CancellationToken cancellationToken)
		{
			return this.WriteValueInternalAsync(JsonToken.Float, JsonConvert.ToString(value), cancellationToken);
		}

		/// <summary>
		/// Asynchronously writes a <see cref="T:System.Nullable`1" /> of <see cref="T:System.Decimal" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Nullable`1" /> of <see cref="T:System.Decimal" /> value to write.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x060002A4 RID: 676 RVA: 0x0000AD9E File Offset: 0x00008F9E
		public override Task WriteValueAsync(decimal? value, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteValueAsync(value, cancellationToken);
			}
			return this.DoWriteValueAsync(value, cancellationToken);
		}

		// Token: 0x060002A5 RID: 677 RVA: 0x0000ADB9 File Offset: 0x00008FB9
		internal Task DoWriteValueAsync(decimal? value, CancellationToken cancellationToken)
		{
			if (value != null)
			{
				return this.DoWriteValueAsync(value.GetValueOrDefault(), cancellationToken);
			}
			return this.DoWriteNullAsync(cancellationToken);
		}

		/// <summary>
		/// Asynchronously writes a <see cref="T:System.Double" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Double" /> value to write.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x060002A6 RID: 678 RVA: 0x0000ADDA File Offset: 0x00008FDA
		public override Task WriteValueAsync(double value, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteValueAsync(value, cancellationToken);
			}
			return this.WriteValueAsync(value, false, cancellationToken);
		}

		// Token: 0x060002A7 RID: 679 RVA: 0x0000ADF6 File Offset: 0x00008FF6
		internal Task WriteValueAsync(double value, bool nullable, CancellationToken cancellationToken)
		{
			return this.WriteValueInternalAsync(JsonToken.Float, JsonConvert.ToString(value, base.FloatFormatHandling, this.QuoteChar, nullable), cancellationToken);
		}

		/// <summary>
		/// Asynchronously writes a <see cref="T:System.Nullable`1" /> of <see cref="T:System.Double" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Nullable`1" /> of <see cref="T:System.Double" /> value to write.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x060002A8 RID: 680 RVA: 0x0000AE13 File Offset: 0x00009013
		public override Task WriteValueAsync(double? value, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteValueAsync(value, cancellationToken);
			}
			if (value == null)
			{
				return this.WriteNullAsync(cancellationToken);
			}
			return this.WriteValueAsync(value.GetValueOrDefault(), true, cancellationToken);
		}

		/// <summary>
		/// Asynchronously writes a <see cref="T:System.Single" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Single" /> value to write.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x060002A9 RID: 681 RVA: 0x0000AE46 File Offset: 0x00009046
		public override Task WriteValueAsync(float value, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteValueAsync(value, cancellationToken);
			}
			return this.WriteValueAsync(value, false, cancellationToken);
		}

		// Token: 0x060002AA RID: 682 RVA: 0x0000AE62 File Offset: 0x00009062
		internal Task WriteValueAsync(float value, bool nullable, CancellationToken cancellationToken)
		{
			return this.WriteValueInternalAsync(JsonToken.Float, JsonConvert.ToString(value, base.FloatFormatHandling, this.QuoteChar, nullable), cancellationToken);
		}

		/// <summary>
		/// Asynchronously writes a <see cref="T:System.Nullable`1" /> of <see cref="T:System.Single" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Nullable`1" /> of <see cref="T:System.Single" /> value to write.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x060002AB RID: 683 RVA: 0x0000AE7F File Offset: 0x0000907F
		public override Task WriteValueAsync(float? value, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteValueAsync(value, cancellationToken);
			}
			if (value == null)
			{
				return this.WriteNullAsync(cancellationToken);
			}
			return this.WriteValueAsync(value.GetValueOrDefault(), true, cancellationToken);
		}

		/// <summary>
		/// Asynchronously writes a <see cref="T:System.Guid" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Guid" /> value to write.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x060002AC RID: 684 RVA: 0x0000AEB2 File Offset: 0x000090B2
		public override Task WriteValueAsync(Guid value, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteValueAsync(value, cancellationToken);
			}
			return this.DoWriteValueAsync(value, cancellationToken);
		}

		// Token: 0x060002AD RID: 685 RVA: 0x0000AED0 File Offset: 0x000090D0
		internal async Task DoWriteValueAsync(Guid value, CancellationToken cancellationToken)
		{
			await base.InternalWriteValueAsync(JsonToken.String, cancellationToken).ConfigureAwait(false);
			await this._writer.WriteAsync(this._quoteChar).ConfigureAwait(false);
			await this._writer.WriteAsync(value.ToString("D", CultureInfo.InvariantCulture), cancellationToken).ConfigureAwait(false);
			await this._writer.WriteAsync(this._quoteChar).ConfigureAwait(false);
		}

		/// <summary>
		/// Asynchronously writes a <see cref="T:System.Nullable`1" /> of <see cref="T:System.Guid" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Nullable`1" /> of <see cref="T:System.Guid" /> value to write.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x060002AE RID: 686 RVA: 0x0000AF23 File Offset: 0x00009123
		public override Task WriteValueAsync(Guid? value, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteValueAsync(value, cancellationToken);
			}
			return this.DoWriteValueAsync(value, cancellationToken);
		}

		// Token: 0x060002AF RID: 687 RVA: 0x0000AF3E File Offset: 0x0000913E
		internal Task DoWriteValueAsync(Guid? value, CancellationToken cancellationToken)
		{
			if (value != null)
			{
				return this.DoWriteValueAsync(value.GetValueOrDefault(), cancellationToken);
			}
			return this.DoWriteNullAsync(cancellationToken);
		}

		/// <summary>
		/// Asynchronously writes a <see cref="T:System.Int32" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Int32" /> value to write.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x060002B0 RID: 688 RVA: 0x0000AF5F File Offset: 0x0000915F
		public override Task WriteValueAsync(int value, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteValueAsync(value, cancellationToken);
			}
			return this.WriteIntegerValueAsync((long)value, cancellationToken);
		}

		/// <summary>
		/// Asynchronously writes a <see cref="T:System.Nullable`1" /> of <see cref="T:System.Int32" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Nullable`1" /> of <see cref="T:System.Int32" /> value to write.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x060002B1 RID: 689 RVA: 0x0000AF7B File Offset: 0x0000917B
		public override Task WriteValueAsync(int? value, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteValueAsync(value, cancellationToken);
			}
			return this.DoWriteValueAsync(value, cancellationToken);
		}

		// Token: 0x060002B2 RID: 690 RVA: 0x0000AF96 File Offset: 0x00009196
		internal Task DoWriteValueAsync(int? value, CancellationToken cancellationToken)
		{
			if (value != null)
			{
				return this.WriteIntegerValueAsync((long)value.GetValueOrDefault(), cancellationToken);
			}
			return this.DoWriteNullAsync(cancellationToken);
		}

		/// <summary>
		/// Asynchronously writes a <see cref="T:System.Int64" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Int64" /> value to write.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x060002B3 RID: 691 RVA: 0x0000AFB8 File Offset: 0x000091B8
		public override Task WriteValueAsync(long value, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteValueAsync(value, cancellationToken);
			}
			return this.WriteIntegerValueAsync(value, cancellationToken);
		}

		/// <summary>
		/// Asynchronously writes a <see cref="T:System.Nullable`1" /> of <see cref="T:System.Int64" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Nullable`1" /> of <see cref="T:System.Int64" /> value to write.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x060002B4 RID: 692 RVA: 0x0000AFD3 File Offset: 0x000091D3
		public override Task WriteValueAsync(long? value, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteValueAsync(value, cancellationToken);
			}
			return this.DoWriteValueAsync(value, cancellationToken);
		}

		// Token: 0x060002B5 RID: 693 RVA: 0x0000AFEE File Offset: 0x000091EE
		internal Task DoWriteValueAsync(long? value, CancellationToken cancellationToken)
		{
			if (value != null)
			{
				return this.WriteIntegerValueAsync(value.GetValueOrDefault(), cancellationToken);
			}
			return this.DoWriteNullAsync(cancellationToken);
		}

		// Token: 0x060002B6 RID: 694 RVA: 0x0000B00F File Offset: 0x0000920F
		internal Task WriteValueAsync(BigInteger value, CancellationToken cancellationToken)
		{
			return this.WriteValueInternalAsync(JsonToken.Integer, value.ToString(CultureInfo.InvariantCulture), cancellationToken);
		}

		/// <summary>
		/// Asynchronously writes a <see cref="T:System.Object" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Object" /> value to write.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x060002B7 RID: 695 RVA: 0x0000B028 File Offset: 0x00009228
		public override Task WriteValueAsync([Nullable(2)] object value, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteValueAsync(value, cancellationToken);
			}
			if (value == null)
			{
				return this.WriteNullAsync(cancellationToken);
			}
			if (value is BigInteger)
			{
				BigInteger value2 = (BigInteger)value;
				return this.WriteValueAsync(value2, cancellationToken);
			}
			return JsonWriter.WriteValueAsync(this, ConvertUtils.GetTypeCode(value.GetType()), value, cancellationToken);
		}

		/// <summary>
		/// Asynchronously writes a <see cref="T:System.SByte" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.SByte" /> value to write.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x060002B8 RID: 696 RVA: 0x0000B07C File Offset: 0x0000927C
		[CLSCompliant(false)]
		public override Task WriteValueAsync(sbyte value, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteValueAsync(value, cancellationToken);
			}
			return this.WriteIntegerValueAsync((long)value, cancellationToken);
		}

		/// <summary>
		/// Asynchronously writes a <see cref="T:System.Nullable`1" /> of <see cref="T:System.SByte" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Nullable`1" /> of <see cref="T:System.SByte" /> value to write.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x060002B9 RID: 697 RVA: 0x0000B098 File Offset: 0x00009298
		[CLSCompliant(false)]
		public override Task WriteValueAsync(sbyte? value, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteValueAsync(value, cancellationToken);
			}
			return this.DoWriteValueAsync(value, cancellationToken);
		}

		// Token: 0x060002BA RID: 698 RVA: 0x0000B0B3 File Offset: 0x000092B3
		internal Task DoWriteValueAsync(sbyte? value, CancellationToken cancellationToken)
		{
			if (value != null)
			{
				return this.WriteIntegerValueAsync((long)value.GetValueOrDefault(), cancellationToken);
			}
			return this.DoWriteNullAsync(cancellationToken);
		}

		/// <summary>
		/// Asynchronously writes a <see cref="T:System.Int16" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Int16" /> value to write.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x060002BB RID: 699 RVA: 0x0000B0D5 File Offset: 0x000092D5
		public override Task WriteValueAsync(short value, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteValueAsync(value, cancellationToken);
			}
			return this.WriteIntegerValueAsync((long)value, cancellationToken);
		}

		/// <summary>
		/// Asynchronously writes a <see cref="T:System.Nullable`1" /> of <see cref="T:System.Int16" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Nullable`1" /> of <see cref="T:System.Int16" /> value to write.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x060002BC RID: 700 RVA: 0x0000B0F1 File Offset: 0x000092F1
		public override Task WriteValueAsync(short? value, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteValueAsync(value, cancellationToken);
			}
			return this.DoWriteValueAsync(value, cancellationToken);
		}

		// Token: 0x060002BD RID: 701 RVA: 0x0000B10C File Offset: 0x0000930C
		internal Task DoWriteValueAsync(short? value, CancellationToken cancellationToken)
		{
			if (value != null)
			{
				return this.WriteIntegerValueAsync((long)value.GetValueOrDefault(), cancellationToken);
			}
			return this.DoWriteNullAsync(cancellationToken);
		}

		/// <summary>
		/// Asynchronously writes a <see cref="T:System.String" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.String" /> value to write.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x060002BE RID: 702 RVA: 0x0000B12E File Offset: 0x0000932E
		public override Task WriteValueAsync([Nullable(2)] string value, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteValueAsync(value, cancellationToken);
			}
			return this.DoWriteValueAsync(value, cancellationToken);
		}

		// Token: 0x060002BF RID: 703 RVA: 0x0000B14C File Offset: 0x0000934C
		internal Task DoWriteValueAsync([Nullable(2)] string value, CancellationToken cancellationToken)
		{
			Task task = base.InternalWriteValueAsync(JsonToken.String, cancellationToken);
			if (!task.IsCompletedSuccessfully())
			{
				return this.DoWriteValueAsync(task, value, cancellationToken);
			}
			if (value != null)
			{
				return this.WriteEscapedStringAsync(value, true, cancellationToken);
			}
			return this._writer.WriteAsync(JsonConvert.Null, cancellationToken);
		}

		// Token: 0x060002C0 RID: 704 RVA: 0x0000B194 File Offset: 0x00009394
		private async Task DoWriteValueAsync(Task task, [Nullable(2)] string value, CancellationToken cancellationToken)
		{
			await task.ConfigureAwait(false);
			await((value == null) ? this._writer.WriteAsync(JsonConvert.Null, cancellationToken) : this.WriteEscapedStringAsync(value, true, cancellationToken)).ConfigureAwait(false);
		}

		/// <summary>
		/// Asynchronously writes a <see cref="T:System.TimeSpan" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.TimeSpan" /> value to write.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x060002C1 RID: 705 RVA: 0x0000B1EF File Offset: 0x000093EF
		public override Task WriteValueAsync(TimeSpan value, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteValueAsync(value, cancellationToken);
			}
			return this.DoWriteValueAsync(value, cancellationToken);
		}

		// Token: 0x060002C2 RID: 706 RVA: 0x0000B20C File Offset: 0x0000940C
		internal async Task DoWriteValueAsync(TimeSpan value, CancellationToken cancellationToken)
		{
			await base.InternalWriteValueAsync(JsonToken.String, cancellationToken).ConfigureAwait(false);
			await this._writer.WriteAsync(this._quoteChar, cancellationToken).ConfigureAwait(false);
			await this._writer.WriteAsync(value.ToString(null, CultureInfo.InvariantCulture), cancellationToken).ConfigureAwait(false);
			await this._writer.WriteAsync(this._quoteChar, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Asynchronously writes a <see cref="T:System.Nullable`1" /> of <see cref="T:System.TimeSpan" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Nullable`1" /> of <see cref="T:System.TimeSpan" /> value to write.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x060002C3 RID: 707 RVA: 0x0000B25F File Offset: 0x0000945F
		public override Task WriteValueAsync(TimeSpan? value, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteValueAsync(value, cancellationToken);
			}
			return this.DoWriteValueAsync(value, cancellationToken);
		}

		// Token: 0x060002C4 RID: 708 RVA: 0x0000B27A File Offset: 0x0000947A
		internal Task DoWriteValueAsync(TimeSpan? value, CancellationToken cancellationToken)
		{
			if (value != null)
			{
				return this.DoWriteValueAsync(value.GetValueOrDefault(), cancellationToken);
			}
			return this.DoWriteNullAsync(cancellationToken);
		}

		/// <summary>
		/// Asynchronously writes a <see cref="T:System.UInt32" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.UInt32" /> value to write.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x060002C5 RID: 709 RVA: 0x0000B29B File Offset: 0x0000949B
		[CLSCompliant(false)]
		public override Task WriteValueAsync(uint value, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteValueAsync(value, cancellationToken);
			}
			return this.WriteIntegerValueAsync((long)((ulong)value), cancellationToken);
		}

		/// <summary>
		/// Asynchronously writes a <see cref="T:System.Nullable`1" /> of <see cref="T:System.UInt32" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Nullable`1" /> of <see cref="T:System.UInt32" /> value to write.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x060002C6 RID: 710 RVA: 0x0000B2B7 File Offset: 0x000094B7
		[CLSCompliant(false)]
		public override Task WriteValueAsync(uint? value, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteValueAsync(value, cancellationToken);
			}
			return this.DoWriteValueAsync(value, cancellationToken);
		}

		// Token: 0x060002C7 RID: 711 RVA: 0x0000B2D2 File Offset: 0x000094D2
		internal Task DoWriteValueAsync(uint? value, CancellationToken cancellationToken)
		{
			if (value != null)
			{
				return this.WriteIntegerValueAsync((long)((ulong)value.GetValueOrDefault()), cancellationToken);
			}
			return this.DoWriteNullAsync(cancellationToken);
		}

		/// <summary>
		/// Asynchronously writes a <see cref="T:System.UInt64" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.UInt64" /> value to write.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x060002C8 RID: 712 RVA: 0x0000B2F4 File Offset: 0x000094F4
		[CLSCompliant(false)]
		public override Task WriteValueAsync(ulong value, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteValueAsync(value, cancellationToken);
			}
			return this.WriteIntegerValueAsync(value, cancellationToken);
		}

		/// <summary>
		/// Asynchronously writes a <see cref="T:System.Nullable`1" /> of <see cref="T:System.UInt64" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Nullable`1" /> of <see cref="T:System.UInt64" /> value to write.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x060002C9 RID: 713 RVA: 0x0000B30F File Offset: 0x0000950F
		[CLSCompliant(false)]
		public override Task WriteValueAsync(ulong? value, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteValueAsync(value, cancellationToken);
			}
			return this.DoWriteValueAsync(value, cancellationToken);
		}

		// Token: 0x060002CA RID: 714 RVA: 0x0000B32A File Offset: 0x0000952A
		internal Task DoWriteValueAsync(ulong? value, CancellationToken cancellationToken)
		{
			if (value != null)
			{
				return this.WriteIntegerValueAsync(value.GetValueOrDefault(), cancellationToken);
			}
			return this.DoWriteNullAsync(cancellationToken);
		}

		/// <summary>
		/// Asynchronously writes a <see cref="T:System.Uri" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Uri" /> value to write.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x060002CB RID: 715 RVA: 0x0000B34B File Offset: 0x0000954B
		public override Task WriteValueAsync([Nullable(2)] Uri value, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteValueAsync(value, cancellationToken);
			}
			if (!(value == null))
			{
				return this.WriteValueNotNullAsync(value, cancellationToken);
			}
			return this.WriteNullAsync(cancellationToken);
		}

		// Token: 0x060002CC RID: 716 RVA: 0x0000B378 File Offset: 0x00009578
		internal Task WriteValueNotNullAsync(Uri value, CancellationToken cancellationToken)
		{
			Task task = base.InternalWriteValueAsync(JsonToken.String, cancellationToken);
			if (task.IsCompletedSuccessfully())
			{
				return this.WriteEscapedStringAsync(value.OriginalString, true, cancellationToken);
			}
			return this.WriteValueNotNullAsync(task, value, cancellationToken);
		}

		// Token: 0x060002CD RID: 717 RVA: 0x0000B3B0 File Offset: 0x000095B0
		internal async Task WriteValueNotNullAsync(Task task, Uri value, CancellationToken cancellationToken)
		{
			await task.ConfigureAwait(false);
			await this.WriteEscapedStringAsync(value.OriginalString, true, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Asynchronously writes a <see cref="T:System.UInt16" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.UInt16" /> value to write.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x060002CE RID: 718 RVA: 0x0000B40B File Offset: 0x0000960B
		[CLSCompliant(false)]
		public override Task WriteValueAsync(ushort value, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteValueAsync(value, cancellationToken);
			}
			return this.WriteIntegerValueAsync((long)((ulong)value), cancellationToken);
		}

		/// <summary>
		/// Asynchronously writes a <see cref="T:System.Nullable`1" /> of <see cref="T:System.UInt16" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Nullable`1" /> of <see cref="T:System.UInt16" /> value to write.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x060002CF RID: 719 RVA: 0x0000B427 File Offset: 0x00009627
		[CLSCompliant(false)]
		public override Task WriteValueAsync(ushort? value, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteValueAsync(value, cancellationToken);
			}
			return this.DoWriteValueAsync(value, cancellationToken);
		}

		// Token: 0x060002D0 RID: 720 RVA: 0x0000B442 File Offset: 0x00009642
		internal Task DoWriteValueAsync(ushort? value, CancellationToken cancellationToken)
		{
			if (value != null)
			{
				return this.WriteIntegerValueAsync((long)((ulong)value.GetValueOrDefault()), cancellationToken);
			}
			return this.DoWriteNullAsync(cancellationToken);
		}

		/// <summary>
		/// Asynchronously writes a comment <c>/*...*/</c> containing the specified text.
		/// </summary>
		/// <param name="text">Text to place inside the comment.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x060002D1 RID: 721 RVA: 0x0000B464 File Offset: 0x00009664
		public override Task WriteCommentAsync([Nullable(2)] string text, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteCommentAsync(text, cancellationToken);
			}
			return this.DoWriteCommentAsync(text, cancellationToken);
		}

		// Token: 0x060002D2 RID: 722 RVA: 0x0000B480 File Offset: 0x00009680
		internal async Task DoWriteCommentAsync([Nullable(2)] string text, CancellationToken cancellationToken)
		{
			await base.InternalWriteCommentAsync(cancellationToken).ConfigureAwait(false);
			await this._writer.WriteAsync("/*", cancellationToken).ConfigureAwait(false);
			await this._writer.WriteAsync(text ?? string.Empty, cancellationToken).ConfigureAwait(false);
			await this._writer.WriteAsync("*/", cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Asynchronously writes the end of an array.
		/// </summary>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x060002D3 RID: 723 RVA: 0x0000B4D3 File Offset: 0x000096D3
		public override Task WriteEndArrayAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteEndArrayAsync(cancellationToken);
			}
			return base.InternalWriteEndAsync(JsonContainerType.Array, cancellationToken);
		}

		/// <summary>
		/// Asynchronously writes the end of a constructor.
		/// </summary>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x060002D4 RID: 724 RVA: 0x0000B4ED File Offset: 0x000096ED
		public override Task WriteEndConstructorAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteEndConstructorAsync(cancellationToken);
			}
			return base.InternalWriteEndAsync(JsonContainerType.Constructor, cancellationToken);
		}

		/// <summary>
		/// Asynchronously writes the end of a JSON object.
		/// </summary>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x060002D5 RID: 725 RVA: 0x0000B507 File Offset: 0x00009707
		public override Task WriteEndObjectAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteEndObjectAsync(cancellationToken);
			}
			return base.InternalWriteEndAsync(JsonContainerType.Object, cancellationToken);
		}

		/// <summary>
		/// Asynchronously writes raw JSON where a value is expected and updates the writer's state.
		/// </summary>
		/// <param name="json">The raw JSON to write.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>Derived classes must override this method to get asynchronous behaviour. Otherwise it will
		/// execute synchronously, returning an already-completed task.</remarks>
		// Token: 0x060002D6 RID: 726 RVA: 0x0000B521 File Offset: 0x00009721
		public override Task WriteRawValueAsync([Nullable(2)] string json, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!this._safeAsync)
			{
				return base.WriteRawValueAsync(json, cancellationToken);
			}
			return this.DoWriteRawValueAsync(json, cancellationToken);
		}

		// Token: 0x060002D7 RID: 727 RVA: 0x0000B53C File Offset: 0x0000973C
		internal Task DoWriteRawValueAsync([Nullable(2)] string json, CancellationToken cancellationToken)
		{
			base.UpdateScopeWithFinishedValue();
			Task task = base.AutoCompleteAsync(JsonToken.Undefined, cancellationToken);
			if (task.IsCompletedSuccessfully())
			{
				return this.WriteRawAsync(json, cancellationToken);
			}
			return this.DoWriteRawValueAsync(task, json, cancellationToken);
		}

		// Token: 0x060002D8 RID: 728 RVA: 0x0000B574 File Offset: 0x00009774
		private async Task DoWriteRawValueAsync(Task task, [Nullable(2)] string json, CancellationToken cancellationToken)
		{
			await task.ConfigureAwait(false);
			await this.WriteRawAsync(json, cancellationToken).ConfigureAwait(false);
		}

		// Token: 0x060002D9 RID: 729 RVA: 0x0000B5D0 File Offset: 0x000097D0
		internal char[] EnsureWriteBuffer(int length, int copyTo)
		{
			if (length < 35)
			{
				length = 35;
			}
			char[] writeBuffer = this._writeBuffer;
			if (writeBuffer == null)
			{
				return this._writeBuffer = BufferUtils.RentBuffer(this._arrayPool, length);
			}
			if (writeBuffer.Length >= length)
			{
				return writeBuffer;
			}
			char[] array = BufferUtils.RentBuffer(this._arrayPool, length);
			if (copyTo != 0)
			{
				Array.Copy(writeBuffer, array, copyTo);
			}
			BufferUtils.ReturnBuffer(this._arrayPool, writeBuffer);
			this._writeBuffer = array;
			return array;
		}

		// Token: 0x17000084 RID: 132
		// (get) Token: 0x060002DA RID: 730 RVA: 0x0000B63A File Offset: 0x0000983A
		private Base64Encoder Base64Encoder
		{
			get
			{
				if (this._base64Encoder == null)
				{
					this._base64Encoder = new Base64Encoder(this._writer);
				}
				return this._base64Encoder;
			}
		}

		/// <summary>
		/// Gets or sets the writer's character array pool.
		/// </summary>
		// Token: 0x17000085 RID: 133
		// (get) Token: 0x060002DB RID: 731 RVA: 0x0000B65B File Offset: 0x0000985B
		// (set) Token: 0x060002DC RID: 732 RVA: 0x0000B663 File Offset: 0x00009863
		[Nullable(2)]
		public IArrayPool<char> ArrayPool
		{
			[NullableContext(2)]
			get
			{
				return this._arrayPool;
			}
			[NullableContext(2)]
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value");
				}
				this._arrayPool = value;
			}
		}

		/// <summary>
		/// Gets or sets how many <see cref="P:Newtonsoft.Json.JsonTextWriter.IndentChar" />s to write for each level in the hierarchy when <see cref="P:Newtonsoft.Json.JsonWriter.Formatting" /> is set to <see cref="F:Newtonsoft.Json.Formatting.Indented" />.
		/// </summary>
		// Token: 0x17000086 RID: 134
		// (get) Token: 0x060002DD RID: 733 RVA: 0x0000B67A File Offset: 0x0000987A
		// (set) Token: 0x060002DE RID: 734 RVA: 0x0000B682 File Offset: 0x00009882
		public int Indentation
		{
			get
			{
				return this._indentation;
			}
			set
			{
				if (value < 0)
				{
					throw new ArgumentException("Indentation value must be greater than 0.");
				}
				this._indentation = value;
			}
		}

		/// <summary>
		/// Gets or sets which character to use to quote attribute values.
		/// </summary>
		// Token: 0x17000087 RID: 135
		// (get) Token: 0x060002DF RID: 735 RVA: 0x0000B69A File Offset: 0x0000989A
		// (set) Token: 0x060002E0 RID: 736 RVA: 0x0000B6A2 File Offset: 0x000098A2
		public char QuoteChar
		{
			get
			{
				return this._quoteChar;
			}
			set
			{
				if (value != '"' && value != '\'')
				{
					throw new ArgumentException("Invalid JavaScript string quote character. Valid quote characters are ' and \".");
				}
				this._quoteChar = value;
				this.UpdateCharEscapeFlags();
			}
		}

		/// <summary>
		/// Gets or sets which character to use for indenting when <see cref="P:Newtonsoft.Json.JsonWriter.Formatting" /> is set to <see cref="F:Newtonsoft.Json.Formatting.Indented" />.
		/// </summary>
		// Token: 0x17000088 RID: 136
		// (get) Token: 0x060002E1 RID: 737 RVA: 0x0000B6C6 File Offset: 0x000098C6
		// (set) Token: 0x060002E2 RID: 738 RVA: 0x0000B6CE File Offset: 0x000098CE
		public char IndentChar
		{
			get
			{
				return this._indentChar;
			}
			set
			{
				if (value != this._indentChar)
				{
					this._indentChar = value;
					this._indentChars = null;
				}
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether object names will be surrounded with quotes.
		/// </summary>
		// Token: 0x17000089 RID: 137
		// (get) Token: 0x060002E3 RID: 739 RVA: 0x0000B6E7 File Offset: 0x000098E7
		// (set) Token: 0x060002E4 RID: 740 RVA: 0x0000B6EF File Offset: 0x000098EF
		public bool QuoteName
		{
			get
			{
				return this._quoteName;
			}
			set
			{
				this._quoteName = value;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.JsonTextWriter" /> class using the specified <see cref="T:System.IO.TextWriter" />.
		/// </summary>
		/// <param name="textWriter">The <see cref="T:System.IO.TextWriter" /> to write to.</param>
		// Token: 0x060002E5 RID: 741 RVA: 0x0000B6F8 File Offset: 0x000098F8
		public JsonTextWriter(TextWriter textWriter)
		{
			if (textWriter == null)
			{
				throw new ArgumentNullException("textWriter");
			}
			this._writer = textWriter;
			this._quoteChar = '"';
			this._quoteName = true;
			this._indentChar = ' ';
			this._indentation = 2;
			this.UpdateCharEscapeFlags();
			this._safeAsync = base.GetType() == typeof(JsonTextWriter);
		}

		/// <summary>
		/// Flushes whatever is in the buffer to the underlying <see cref="T:System.IO.TextWriter" /> and also flushes the underlying <see cref="T:System.IO.TextWriter" />.
		/// </summary>
		// Token: 0x060002E6 RID: 742 RVA: 0x0000B75F File Offset: 0x0000995F
		public override void Flush()
		{
			this._writer.Flush();
		}

		/// <summary>
		/// Closes this writer.
		/// If <see cref="P:Newtonsoft.Json.JsonWriter.CloseOutput" /> is set to <c>true</c>, the underlying <see cref="T:System.IO.TextWriter" /> is also closed.
		/// If <see cref="P:Newtonsoft.Json.JsonWriter.AutoCompleteOnClose" /> is set to <c>true</c>, the JSON is auto-completed.
		/// </summary>
		// Token: 0x060002E7 RID: 743 RVA: 0x0000B76C File Offset: 0x0000996C
		public override void Close()
		{
			base.Close();
			this.CloseBufferAndWriter();
		}

		// Token: 0x060002E8 RID: 744 RVA: 0x0000B77A File Offset: 0x0000997A
		private void CloseBufferAndWriter()
		{
			if (this._writeBuffer != null)
			{
				BufferUtils.ReturnBuffer(this._arrayPool, this._writeBuffer);
				this._writeBuffer = null;
			}
			if (base.CloseOutput)
			{
				TextWriter writer = this._writer;
				if (writer == null)
				{
					return;
				}
				writer.Close();
			}
		}

		/// <summary>
		/// Writes the beginning of a JSON object.
		/// </summary>
		// Token: 0x060002E9 RID: 745 RVA: 0x0000B7B4 File Offset: 0x000099B4
		public override void WriteStartObject()
		{
			base.InternalWriteStart(JsonToken.StartObject, JsonContainerType.Object);
			this._writer.Write('{');
		}

		/// <summary>
		/// Writes the beginning of a JSON array.
		/// </summary>
		// Token: 0x060002EA RID: 746 RVA: 0x0000B7CB File Offset: 0x000099CB
		public override void WriteStartArray()
		{
			base.InternalWriteStart(JsonToken.StartArray, JsonContainerType.Array);
			this._writer.Write('[');
		}

		/// <summary>
		/// Writes the start of a constructor with the given name.
		/// </summary>
		/// <param name="name">The name of the constructor.</param>
		// Token: 0x060002EB RID: 747 RVA: 0x0000B7E2 File Offset: 0x000099E2
		public override void WriteStartConstructor(string name)
		{
			base.InternalWriteStart(JsonToken.StartConstructor, JsonContainerType.Constructor);
			this._writer.Write("new ");
			this._writer.Write(name);
			this._writer.Write('(');
		}

		/// <summary>
		/// Writes the specified end token.
		/// </summary>
		/// <param name="token">The end token to write.</param>
		// Token: 0x060002EC RID: 748 RVA: 0x0000B818 File Offset: 0x00009A18
		protected override void WriteEnd(JsonToken token)
		{
			switch (token)
			{
			case JsonToken.EndObject:
				this._writer.Write('}');
				return;
			case JsonToken.EndArray:
				this._writer.Write(']');
				return;
			case JsonToken.EndConstructor:
				this._writer.Write(')');
				return;
			default:
				throw JsonWriterException.Create(this, "Invalid JsonToken: " + token.ToString(), null);
			}
		}

		/// <summary>
		/// Writes the property name of a name/value pair on a JSON object.
		/// </summary>
		/// <param name="name">The name of the property.</param>
		// Token: 0x060002ED RID: 749 RVA: 0x0000B884 File Offset: 0x00009A84
		public override void WritePropertyName(string name)
		{
			base.InternalWritePropertyName(name);
			this.WriteEscapedString(name, this._quoteName);
			this._writer.Write(':');
		}

		/// <summary>
		/// Writes the property name of a name/value pair on a JSON object.
		/// </summary>
		/// <param name="name">The name of the property.</param>
		/// <param name="escape">A flag to indicate whether the text should be escaped when it is written as a JSON property name.</param>
		// Token: 0x060002EE RID: 750 RVA: 0x0000B8A8 File Offset: 0x00009AA8
		public override void WritePropertyName(string name, bool escape)
		{
			base.InternalWritePropertyName(name);
			if (escape)
			{
				this.WriteEscapedString(name, this._quoteName);
			}
			else
			{
				if (this._quoteName)
				{
					this._writer.Write(this._quoteChar);
				}
				this._writer.Write(name);
				if (this._quoteName)
				{
					this._writer.Write(this._quoteChar);
				}
			}
			this._writer.Write(':');
		}

		// Token: 0x060002EF RID: 751 RVA: 0x0000B919 File Offset: 0x00009B19
		internal override void OnStringEscapeHandlingChanged()
		{
			this.UpdateCharEscapeFlags();
		}

		// Token: 0x060002F0 RID: 752 RVA: 0x0000B921 File Offset: 0x00009B21
		private void UpdateCharEscapeFlags()
		{
			this._charEscapeFlags = JavaScriptUtils.GetCharEscapeFlags(base.StringEscapeHandling, this._quoteChar);
		}

		/// <summary>
		/// Writes indent characters.
		/// </summary>
		// Token: 0x060002F1 RID: 753 RVA: 0x0000B93C File Offset: 0x00009B3C
		protected override void WriteIndent()
		{
			int num = base.Top * this._indentation;
			int num2 = this.SetIndentChars();
			this._writer.Write(this._indentChars, 0, num2 + Math.Min(num, 12));
			while ((num -= 12) > 0)
			{
				this._writer.Write(this._indentChars, num2, Math.Min(num, 12));
			}
		}

		// Token: 0x060002F2 RID: 754 RVA: 0x0000B9A0 File Offset: 0x00009BA0
		private int SetIndentChars()
		{
			string newLine = this._writer.NewLine;
			int length = newLine.Length;
			bool flag = this._indentChars != null && this._indentChars.Length == 12 + length;
			if (flag)
			{
				for (int num = 0; num != length; num++)
				{
					if (newLine[num] != this._indentChars[num])
					{
						flag = false;
						break;
					}
				}
			}
			if (!flag)
			{
				this._indentChars = (newLine + new string(this._indentChar, 12)).ToCharArray();
			}
			return length;
		}

		/// <summary>
		/// Writes the JSON value delimiter.
		/// </summary>
		// Token: 0x060002F3 RID: 755 RVA: 0x0000BA20 File Offset: 0x00009C20
		protected override void WriteValueDelimiter()
		{
			this._writer.Write(',');
		}

		/// <summary>
		/// Writes an indent space.
		/// </summary>
		// Token: 0x060002F4 RID: 756 RVA: 0x0000BA2F File Offset: 0x00009C2F
		protected override void WriteIndentSpace()
		{
			this._writer.Write(' ');
		}

		// Token: 0x060002F5 RID: 757 RVA: 0x0000BA3E File Offset: 0x00009C3E
		private void WriteValueInternal(string value, JsonToken token)
		{
			this._writer.Write(value);
		}

		/// <summary>
		/// Writes a <see cref="T:System.Object" /> value.
		/// An error will raised if the value cannot be written as a single JSON token.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Object" /> value to write.</param>
		// Token: 0x060002F6 RID: 758 RVA: 0x0000BA4C File Offset: 0x00009C4C
		[NullableContext(2)]
		public override void WriteValue(object value)
		{
			if (value is BigInteger)
			{
				BigInteger bigInteger = (BigInteger)value;
				base.InternalWriteValue(JsonToken.Integer);
				this.WriteValueInternal(bigInteger.ToString(CultureInfo.InvariantCulture), JsonToken.String);
				return;
			}
			base.WriteValue(value);
		}

		/// <summary>
		/// Writes a null value.
		/// </summary>
		// Token: 0x060002F7 RID: 759 RVA: 0x0000BA8B File Offset: 0x00009C8B
		public override void WriteNull()
		{
			base.InternalWriteValue(JsonToken.Null);
			this.WriteValueInternal(JsonConvert.Null, JsonToken.Null);
		}

		/// <summary>
		/// Writes an undefined value.
		/// </summary>
		// Token: 0x060002F8 RID: 760 RVA: 0x0000BAA2 File Offset: 0x00009CA2
		public override void WriteUndefined()
		{
			base.InternalWriteValue(JsonToken.Undefined);
			this.WriteValueInternal(JsonConvert.Undefined, JsonToken.Undefined);
		}

		/// <summary>
		/// Writes raw JSON.
		/// </summary>
		/// <param name="json">The raw JSON to write.</param>
		// Token: 0x060002F9 RID: 761 RVA: 0x0000BAB9 File Offset: 0x00009CB9
		[NullableContext(2)]
		public override void WriteRaw(string json)
		{
			base.InternalWriteRaw();
			this._writer.Write(json);
		}

		/// <summary>
		/// Writes a <see cref="T:System.String" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.String" /> value to write.</param>
		// Token: 0x060002FA RID: 762 RVA: 0x0000BACD File Offset: 0x00009CCD
		[NullableContext(2)]
		public override void WriteValue(string value)
		{
			base.InternalWriteValue(JsonToken.String);
			if (value == null)
			{
				this.WriteValueInternal(JsonConvert.Null, JsonToken.Null);
				return;
			}
			this.WriteEscapedString(value, true);
		}

		// Token: 0x060002FB RID: 763 RVA: 0x0000BAF0 File Offset: 0x00009CF0
		private void WriteEscapedString(string value, bool quote)
		{
			this.EnsureWriteBuffer();
			JavaScriptUtils.WriteEscapedJavaScriptString(this._writer, value, this._quoteChar, quote, this._charEscapeFlags, base.StringEscapeHandling, this._arrayPool, ref this._writeBuffer);
		}

		/// <summary>
		/// Writes a <see cref="T:System.Int32" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Int32" /> value to write.</param>
		// Token: 0x060002FC RID: 764 RVA: 0x0000BB23 File Offset: 0x00009D23
		public override void WriteValue(int value)
		{
			base.InternalWriteValue(JsonToken.Integer);
			this.WriteIntegerValue(value);
		}

		/// <summary>
		/// Writes a <see cref="T:System.UInt32" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.UInt32" /> value to write.</param>
		// Token: 0x060002FD RID: 765 RVA: 0x0000BB33 File Offset: 0x00009D33
		[CLSCompliant(false)]
		public override void WriteValue(uint value)
		{
			base.InternalWriteValue(JsonToken.Integer);
			this.WriteIntegerValue((long)((ulong)value));
		}

		/// <summary>
		/// Writes a <see cref="T:System.Int64" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Int64" /> value to write.</param>
		// Token: 0x060002FE RID: 766 RVA: 0x0000BB44 File Offset: 0x00009D44
		public override void WriteValue(long value)
		{
			base.InternalWriteValue(JsonToken.Integer);
			this.WriteIntegerValue(value);
		}

		/// <summary>
		/// Writes a <see cref="T:System.UInt64" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.UInt64" /> value to write.</param>
		// Token: 0x060002FF RID: 767 RVA: 0x0000BB54 File Offset: 0x00009D54
		[CLSCompliant(false)]
		public override void WriteValue(ulong value)
		{
			base.InternalWriteValue(JsonToken.Integer);
			this.WriteIntegerValue(value, false);
		}

		/// <summary>
		/// Writes a <see cref="T:System.Single" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Single" /> value to write.</param>
		// Token: 0x06000300 RID: 768 RVA: 0x0000BB65 File Offset: 0x00009D65
		public override void WriteValue(float value)
		{
			base.InternalWriteValue(JsonToken.Float);
			this.WriteValueInternal(JsonConvert.ToString(value, base.FloatFormatHandling, this.QuoteChar, false), JsonToken.Float);
		}

		/// <summary>
		/// Writes a <see cref="T:System.Nullable`1" /> of <see cref="T:System.Single" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Nullable`1" /> of <see cref="T:System.Single" /> value to write.</param>
		// Token: 0x06000301 RID: 769 RVA: 0x0000BB88 File Offset: 0x00009D88
		public override void WriteValue(float? value)
		{
			if (value == null)
			{
				this.WriteNull();
				return;
			}
			base.InternalWriteValue(JsonToken.Float);
			this.WriteValueInternal(JsonConvert.ToString(value.GetValueOrDefault(), base.FloatFormatHandling, this.QuoteChar, true), JsonToken.Float);
		}

		/// <summary>
		/// Writes a <see cref="T:System.Double" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Double" /> value to write.</param>
		// Token: 0x06000302 RID: 770 RVA: 0x0000BBC1 File Offset: 0x00009DC1
		public override void WriteValue(double value)
		{
			base.InternalWriteValue(JsonToken.Float);
			this.WriteValueInternal(JsonConvert.ToString(value, base.FloatFormatHandling, this.QuoteChar, false), JsonToken.Float);
		}

		/// <summary>
		/// Writes a <see cref="T:System.Nullable`1" /> of <see cref="T:System.Double" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Nullable`1" /> of <see cref="T:System.Double" /> value to write.</param>
		// Token: 0x06000303 RID: 771 RVA: 0x0000BBE4 File Offset: 0x00009DE4
		public override void WriteValue(double? value)
		{
			if (value == null)
			{
				this.WriteNull();
				return;
			}
			base.InternalWriteValue(JsonToken.Float);
			this.WriteValueInternal(JsonConvert.ToString(value.GetValueOrDefault(), base.FloatFormatHandling, this.QuoteChar, true), JsonToken.Float);
		}

		/// <summary>
		/// Writes a <see cref="T:System.Boolean" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Boolean" /> value to write.</param>
		// Token: 0x06000304 RID: 772 RVA: 0x0000BC1D File Offset: 0x00009E1D
		public override void WriteValue(bool value)
		{
			base.InternalWriteValue(JsonToken.Boolean);
			this.WriteValueInternal(JsonConvert.ToString(value), JsonToken.Boolean);
		}

		/// <summary>
		/// Writes a <see cref="T:System.Int16" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Int16" /> value to write.</param>
		// Token: 0x06000305 RID: 773 RVA: 0x0000BC35 File Offset: 0x00009E35
		public override void WriteValue(short value)
		{
			base.InternalWriteValue(JsonToken.Integer);
			this.WriteIntegerValue((int)value);
		}

		/// <summary>
		/// Writes a <see cref="T:System.UInt16" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.UInt16" /> value to write.</param>
		// Token: 0x06000306 RID: 774 RVA: 0x0000BC45 File Offset: 0x00009E45
		[CLSCompliant(false)]
		public override void WriteValue(ushort value)
		{
			base.InternalWriteValue(JsonToken.Integer);
			this.WriteIntegerValue((int)value);
		}

		/// <summary>
		/// Writes a <see cref="T:System.Char" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Char" /> value to write.</param>
		// Token: 0x06000307 RID: 775 RVA: 0x0000BC55 File Offset: 0x00009E55
		public override void WriteValue(char value)
		{
			base.InternalWriteValue(JsonToken.String);
			this.WriteValueInternal(JsonConvert.ToString(value), JsonToken.String);
		}

		/// <summary>
		/// Writes a <see cref="T:System.Byte" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Byte" /> value to write.</param>
		// Token: 0x06000308 RID: 776 RVA: 0x0000BC6D File Offset: 0x00009E6D
		public override void WriteValue(byte value)
		{
			base.InternalWriteValue(JsonToken.Integer);
			this.WriteIntegerValue((int)value);
		}

		/// <summary>
		/// Writes a <see cref="T:System.SByte" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.SByte" /> value to write.</param>
		// Token: 0x06000309 RID: 777 RVA: 0x0000BC7D File Offset: 0x00009E7D
		[CLSCompliant(false)]
		public override void WriteValue(sbyte value)
		{
			base.InternalWriteValue(JsonToken.Integer);
			this.WriteIntegerValue((int)value);
		}

		/// <summary>
		/// Writes a <see cref="T:System.Decimal" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Decimal" /> value to write.</param>
		// Token: 0x0600030A RID: 778 RVA: 0x0000BC8D File Offset: 0x00009E8D
		public override void WriteValue(decimal value)
		{
			base.InternalWriteValue(JsonToken.Float);
			this.WriteValueInternal(JsonConvert.ToString(value), JsonToken.Float);
		}

		/// <summary>
		/// Writes a <see cref="T:System.DateTime" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.DateTime" /> value to write.</param>
		// Token: 0x0600030B RID: 779 RVA: 0x0000BCA4 File Offset: 0x00009EA4
		public override void WriteValue(DateTime value)
		{
			base.InternalWriteValue(JsonToken.Date);
			value = DateTimeUtils.EnsureDateTime(value, base.DateTimeZoneHandling);
			if (StringUtils.IsNullOrEmpty(base.DateFormatString))
			{
				int count = this.WriteValueToBuffer(value);
				this._writer.Write(this._writeBuffer, 0, count);
				return;
			}
			this._writer.Write(this._quoteChar);
			this._writer.Write(value.ToString(base.DateFormatString, base.Culture));
			this._writer.Write(this._quoteChar);
		}

		// Token: 0x0600030C RID: 780 RVA: 0x0000BD30 File Offset: 0x00009F30
		private int WriteValueToBuffer(DateTime value)
		{
			this.EnsureWriteBuffer();
			int num = 0;
			this._writeBuffer[num++] = this._quoteChar;
			num = DateTimeUtils.WriteDateTimeString(this._writeBuffer, num, value, null, value.Kind, base.DateFormatHandling);
			this._writeBuffer[num++] = this._quoteChar;
			return num;
		}

		/// <summary>
		/// Writes a <see cref="T:System.Byte" />[] value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Byte" />[] value to write.</param>
		// Token: 0x0600030D RID: 781 RVA: 0x0000BD90 File Offset: 0x00009F90
		[NullableContext(2)]
		public override void WriteValue(byte[] value)
		{
			if (value == null)
			{
				this.WriteNull();
				return;
			}
			base.InternalWriteValue(JsonToken.Bytes);
			this._writer.Write(this._quoteChar);
			this.Base64Encoder.Encode(value, 0, value.Length);
			this.Base64Encoder.Flush();
			this._writer.Write(this._quoteChar);
		}

		/// <summary>
		/// Writes a <see cref="T:System.DateTimeOffset" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.DateTimeOffset" /> value to write.</param>
		// Token: 0x0600030E RID: 782 RVA: 0x0000BDEC File Offset: 0x00009FEC
		public override void WriteValue(DateTimeOffset value)
		{
			base.InternalWriteValue(JsonToken.Date);
			if (StringUtils.IsNullOrEmpty(base.DateFormatString))
			{
				int count = this.WriteValueToBuffer(value);
				this._writer.Write(this._writeBuffer, 0, count);
				return;
			}
			this._writer.Write(this._quoteChar);
			this._writer.Write(value.ToString(base.DateFormatString, base.Culture));
			this._writer.Write(this._quoteChar);
		}

		// Token: 0x0600030F RID: 783 RVA: 0x0000BE6C File Offset: 0x0000A06C
		private int WriteValueToBuffer(DateTimeOffset value)
		{
			this.EnsureWriteBuffer();
			int num = 0;
			this._writeBuffer[num++] = this._quoteChar;
			num = DateTimeUtils.WriteDateTimeString(this._writeBuffer, num, (base.DateFormatHandling == DateFormatHandling.IsoDateFormat) ? value.DateTime : value.UtcDateTime, new TimeSpan?(value.Offset), DateTimeKind.Local, base.DateFormatHandling);
			this._writeBuffer[num++] = this._quoteChar;
			return num;
		}

		/// <summary>
		/// Writes a <see cref="T:System.Guid" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Guid" /> value to write.</param>
		// Token: 0x06000310 RID: 784 RVA: 0x0000BEE0 File Offset: 0x0000A0E0
		public override void WriteValue(Guid value)
		{
			base.InternalWriteValue(JsonToken.String);
			string value2 = value.ToString("D", CultureInfo.InvariantCulture);
			this._writer.Write(this._quoteChar);
			this._writer.Write(value2);
			this._writer.Write(this._quoteChar);
		}

		/// <summary>
		/// Writes a <see cref="T:System.TimeSpan" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.TimeSpan" /> value to write.</param>
		// Token: 0x06000311 RID: 785 RVA: 0x0000BF38 File Offset: 0x0000A138
		public override void WriteValue(TimeSpan value)
		{
			base.InternalWriteValue(JsonToken.String);
			string value2 = value.ToString(null, CultureInfo.InvariantCulture);
			this._writer.Write(this._quoteChar);
			this._writer.Write(value2);
			this._writer.Write(this._quoteChar);
		}

		/// <summary>
		/// Writes a <see cref="T:System.Uri" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Uri" /> value to write.</param>
		// Token: 0x06000312 RID: 786 RVA: 0x0000BF89 File Offset: 0x0000A189
		[NullableContext(2)]
		public override void WriteValue(Uri value)
		{
			if (value == null)
			{
				this.WriteNull();
				return;
			}
			base.InternalWriteValue(JsonToken.String);
			this.WriteEscapedString(value.OriginalString, true);
		}

		/// <summary>
		/// Writes a comment <c>/*...*/</c> containing the specified text. 
		/// </summary>
		/// <param name="text">Text to place inside the comment.</param>
		// Token: 0x06000313 RID: 787 RVA: 0x0000BFB0 File Offset: 0x0000A1B0
		[NullableContext(2)]
		public override void WriteComment(string text)
		{
			base.InternalWriteComment();
			this._writer.Write("/*");
			this._writer.Write(text);
			this._writer.Write("*/");
		}

		/// <summary>
		/// Writes the given white space.
		/// </summary>
		/// <param name="ws">The string of white space characters.</param>
		// Token: 0x06000314 RID: 788 RVA: 0x0000BFE4 File Offset: 0x0000A1E4
		public override void WriteWhitespace(string ws)
		{
			base.InternalWriteWhitespace(ws);
			this._writer.Write(ws);
		}

		// Token: 0x06000315 RID: 789 RVA: 0x0000BFF9 File Offset: 0x0000A1F9
		private void EnsureWriteBuffer()
		{
			if (this._writeBuffer == null)
			{
				this._writeBuffer = BufferUtils.RentBuffer(this._arrayPool, 35);
			}
		}

		// Token: 0x06000316 RID: 790 RVA: 0x0000C018 File Offset: 0x0000A218
		private void WriteIntegerValue(long value)
		{
			if (value >= 0L && value <= 9L)
			{
				this._writer.Write((char)(48L + value));
				return;
			}
			bool flag = value < 0L;
			this.WriteIntegerValue((ulong)(flag ? (-(ulong)value) : value), flag);
		}

		// Token: 0x06000317 RID: 791 RVA: 0x0000C058 File Offset: 0x0000A258
		private void WriteIntegerValue(ulong value, bool negative)
		{
			if (!negative & (value <= 9UL))
			{
				this._writer.Write((char)(48UL + value));
				return;
			}
			int count = this.WriteNumberToBuffer(value, negative);
			this._writer.Write(this._writeBuffer, 0, count);
		}

		// Token: 0x06000318 RID: 792 RVA: 0x0000C0A4 File Offset: 0x0000A2A4
		private int WriteNumberToBuffer(ulong value, bool negative)
		{
			if (value <= (ulong)(-1))
			{
				return this.WriteNumberToBuffer((uint)value, negative);
			}
			this.EnsureWriteBuffer();
			int num = MathUtils.IntLength(value);
			if (negative)
			{
				num++;
				this._writeBuffer[0] = '-';
			}
			int num2 = num;
			do
			{
				ulong num3 = value / 10UL;
				ulong num4 = value - num3 * 10UL;
				this._writeBuffer[--num2] = (char)(48UL + num4);
				value = num3;
			}
			while (value != 0UL);
			return num;
		}

		// Token: 0x06000319 RID: 793 RVA: 0x0000C108 File Offset: 0x0000A308
		private void WriteIntegerValue(int value)
		{
			if (value >= 0 && value <= 9)
			{
				this._writer.Write((char)(48 + value));
				return;
			}
			bool flag = value < 0;
			this.WriteIntegerValue((uint)(flag ? (-(uint)value) : value), flag);
		}

		// Token: 0x0600031A RID: 794 RVA: 0x0000C144 File Offset: 0x0000A344
		private void WriteIntegerValue(uint value, bool negative)
		{
			if (!negative & (value <= 9U))
			{
				this._writer.Write((char)(48U + value));
				return;
			}
			int count = this.WriteNumberToBuffer(value, negative);
			this._writer.Write(this._writeBuffer, 0, count);
		}

		// Token: 0x0600031B RID: 795 RVA: 0x0000C190 File Offset: 0x0000A390
		private int WriteNumberToBuffer(uint value, bool negative)
		{
			this.EnsureWriteBuffer();
			int num = MathUtils.IntLength((ulong)value);
			if (negative)
			{
				num++;
				this._writeBuffer[0] = '-';
			}
			int num2 = num;
			do
			{
				uint num3 = value / 10U;
				uint num4 = value - num3 * 10U;
				this._writeBuffer[--num2] = (char)(48U + num4);
				value = num3;
			}
			while (value != 0U);
			return num;
		}

		// Token: 0x040000E1 RID: 225
		private readonly bool _safeAsync;

		// Token: 0x040000E2 RID: 226
		private const int IndentCharBufferSize = 12;

		// Token: 0x040000E3 RID: 227
		private readonly TextWriter _writer;

		// Token: 0x040000E4 RID: 228
		[Nullable(2)]
		private Base64Encoder _base64Encoder;

		// Token: 0x040000E5 RID: 229
		private char _indentChar;

		// Token: 0x040000E6 RID: 230
		private int _indentation;

		// Token: 0x040000E7 RID: 231
		private char _quoteChar;

		// Token: 0x040000E8 RID: 232
		private bool _quoteName;

		// Token: 0x040000E9 RID: 233
		[Nullable(2)]
		private bool[] _charEscapeFlags;

		// Token: 0x040000EA RID: 234
		[Nullable(2)]
		private char[] _writeBuffer;

		// Token: 0x040000EB RID: 235
		[Nullable(2)]
		private IArrayPool<char> _arrayPool;

		// Token: 0x040000EC RID: 236
		[Nullable(2)]
		private char[] _indentChars;
	}
}
