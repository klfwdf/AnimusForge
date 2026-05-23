using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json
{
	/// <summary>
	/// Represents a reader that provides fast, non-cached, forward-only access to serialized JSON data.
	/// </summary>
	// Token: 0x02000027 RID: 39
	[NullableContext(2)]
	[Nullable(0)]
	public abstract class JsonReader : IDisposable
	{
		/// <summary>
		/// Asynchronously reads the next JSON token from the source.
		/// </summary>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task`1" /> that represents the asynchronous read. The <see cref="P:System.Threading.Tasks.Task`1.Result" />
		/// property returns <c>true</c> if the next token was read successfully; <c>false</c> if there are no more tokens to read.</returns>
		/// <remarks>The default behaviour is to execute synchronously, returning an already-completed task. Derived
		/// classes can override this behaviour for true asynchronicity.</remarks>
		// Token: 0x060000D5 RID: 213 RVA: 0x000034FF File Offset: 0x000016FF
		[NullableContext(1)]
		public virtual Task<bool> ReadAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			return cancellationToken.CancelIfRequestedAsync<bool>() ?? this.Read().ToAsync();
		}

		/// <summary>
		/// Asynchronously skips the children of the current token.
		/// </summary>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
		/// <remarks>The default behaviour is to execute synchronously, returning an already-completed task. Derived
		/// classes can override this behaviour for true asynchronicity.</remarks>
		// Token: 0x060000D6 RID: 214 RVA: 0x00003518 File Offset: 0x00001718
		[NullableContext(1)]
		public async Task SkipAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			if (this.TokenType == JsonToken.PropertyName)
			{
				await this.ReadAsync(cancellationToken).ConfigureAwait(false);
			}
			if (JsonTokenUtils.IsStartToken(this.TokenType))
			{
				int depth = this.Depth;
				ConfiguredTaskAwaitable<bool>.ConfiguredTaskAwaiter configuredTaskAwaiter;
				do
				{
					configuredTaskAwaiter = this.ReadAsync(cancellationToken).ConfigureAwait(false).GetAwaiter();
					if (!configuredTaskAwaiter.IsCompleted)
					{
						await configuredTaskAwaiter;
						ConfiguredTaskAwaitable<bool>.ConfiguredTaskAwaiter configuredTaskAwaiter2;
						configuredTaskAwaiter = configuredTaskAwaiter2;
						configuredTaskAwaiter2 = default(ConfiguredTaskAwaitable<bool>.ConfiguredTaskAwaiter);
					}
				}
				while (configuredTaskAwaiter.GetResult() && depth < this.Depth);
			}
		}

		// Token: 0x060000D7 RID: 215 RVA: 0x00003564 File Offset: 0x00001764
		[NullableContext(1)]
		internal async Task ReaderReadAndAssertAsync(CancellationToken cancellationToken)
		{
			ConfiguredTaskAwaitable<bool>.ConfiguredTaskAwaiter configuredTaskAwaiter = this.ReadAsync(cancellationToken).ConfigureAwait(false).GetAwaiter();
			if (!configuredTaskAwaiter.IsCompleted)
			{
				await configuredTaskAwaiter;
				ConfiguredTaskAwaitable<bool>.ConfiguredTaskAwaiter configuredTaskAwaiter2;
				configuredTaskAwaiter = configuredTaskAwaiter2;
				configuredTaskAwaiter2 = default(ConfiguredTaskAwaitable<bool>.ConfiguredTaskAwaiter);
			}
			if (!configuredTaskAwaiter.GetResult())
			{
				throw this.CreateUnexpectedEndException();
			}
		}

		/// <summary>
		/// Asynchronously reads the next JSON token from the source as a <see cref="T:System.Nullable`1" /> of <see cref="T:System.Boolean" />.
		/// </summary>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task`1" /> that represents the asynchronous read. The <see cref="P:System.Threading.Tasks.Task`1.Result" />
		/// property returns the <see cref="T:System.Nullable`1" /> of <see cref="T:System.Boolean" />. This result will be <c>null</c> at the end of an array.</returns>
		/// <remarks>The default behaviour is to execute synchronously, returning an already-completed task. Derived
		/// classes can override this behaviour for true asynchronicity.</remarks>
		// Token: 0x060000D8 RID: 216 RVA: 0x000035AF File Offset: 0x000017AF
		[NullableContext(1)]
		public virtual Task<bool?> ReadAsBooleanAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			return cancellationToken.CancelIfRequestedAsync<bool?>() ?? Task.FromResult<bool?>(this.ReadAsBoolean());
		}

		/// <summary>
		/// Asynchronously reads the next JSON token from the source as a <see cref="T:System.Byte" />[].
		/// </summary>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task`1" /> that represents the asynchronous read. The <see cref="P:System.Threading.Tasks.Task`1.Result" />
		/// property returns the <see cref="T:System.Byte" />[]. This result will be <c>null</c> at the end of an array.</returns>
		/// <remarks>The default behaviour is to execute synchronously, returning an already-completed task. Derived
		/// classes can override this behaviour for true asynchronicity.</remarks>
		// Token: 0x060000D9 RID: 217 RVA: 0x000035C6 File Offset: 0x000017C6
		[return: Nullable(new byte[] { 1, 2 })]
		public virtual Task<byte[]> ReadAsBytesAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			return cancellationToken.CancelIfRequestedAsync<byte[]>() ?? Task.FromResult<byte[]>(this.ReadAsBytes());
		}

		// Token: 0x060000DA RID: 218 RVA: 0x000035E0 File Offset: 0x000017E0
		[return: Nullable(new byte[] { 1, 2 })]
		internal Task<byte[]> ReadArrayIntoByteArrayAsync(CancellationToken cancellationToken)
		{
			JsonReader.<ReadArrayIntoByteArrayAsync>d__5 <ReadArrayIntoByteArrayAsync>d__;
			<ReadArrayIntoByteArrayAsync>d__.<>t__builder = AsyncTaskMethodBuilder<byte[]>.Create();
			<ReadArrayIntoByteArrayAsync>d__.<>4__this = this;
			<ReadArrayIntoByteArrayAsync>d__.cancellationToken = cancellationToken;
			<ReadArrayIntoByteArrayAsync>d__.<>1__state = -1;
			<ReadArrayIntoByteArrayAsync>d__.<>t__builder.Start<JsonReader.<ReadArrayIntoByteArrayAsync>d__5>(ref <ReadArrayIntoByteArrayAsync>d__);
			return <ReadArrayIntoByteArrayAsync>d__.<>t__builder.Task;
		}

		/// <summary>
		/// Asynchronously reads the next JSON token from the source as a <see cref="T:System.Nullable`1" /> of <see cref="T:System.DateTime" />.
		/// </summary>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task`1" /> that represents the asynchronous read. The <see cref="P:System.Threading.Tasks.Task`1.Result" />
		/// property returns the <see cref="T:System.Nullable`1" /> of <see cref="T:System.DateTime" />. This result will be <c>null</c> at the end of an array.</returns>
		/// <remarks>The default behaviour is to execute synchronously, returning an already-completed task. Derived
		/// classes can override this behaviour for true asynchronicity.</remarks>
		// Token: 0x060000DB RID: 219 RVA: 0x0000362B File Offset: 0x0000182B
		[NullableContext(1)]
		public virtual Task<DateTime?> ReadAsDateTimeAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			return cancellationToken.CancelIfRequestedAsync<DateTime?>() ?? Task.FromResult<DateTime?>(this.ReadAsDateTime());
		}

		/// <summary>
		/// Asynchronously reads the next JSON token from the source as a <see cref="T:System.Nullable`1" /> of <see cref="T:System.DateTimeOffset" />.
		/// </summary>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task`1" /> that represents the asynchronous read. The <see cref="P:System.Threading.Tasks.Task`1.Result" />
		/// property returns the <see cref="T:System.Nullable`1" /> of <see cref="T:System.DateTimeOffset" />. This result will be <c>null</c> at the end of an array.</returns>
		/// <remarks>The default behaviour is to execute synchronously, returning an already-completed task. Derived
		/// classes can override this behaviour for true asynchronicity.</remarks>
		// Token: 0x060000DC RID: 220 RVA: 0x00003642 File Offset: 0x00001842
		[NullableContext(1)]
		public virtual Task<DateTimeOffset?> ReadAsDateTimeOffsetAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			return cancellationToken.CancelIfRequestedAsync<DateTimeOffset?>() ?? Task.FromResult<DateTimeOffset?>(this.ReadAsDateTimeOffset());
		}

		/// <summary>
		/// Asynchronously reads the next JSON token from the source as a <see cref="T:System.Nullable`1" /> of <see cref="T:System.Decimal" />.
		/// </summary>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task`1" /> that represents the asynchronous read. The <see cref="P:System.Threading.Tasks.Task`1.Result" />
		/// property returns the <see cref="T:System.Nullable`1" /> of <see cref="T:System.Decimal" />. This result will be <c>null</c> at the end of an array.</returns>
		/// <remarks>The default behaviour is to execute synchronously, returning an already-completed task. Derived
		/// classes can override this behaviour for true asynchronicity.</remarks>
		// Token: 0x060000DD RID: 221 RVA: 0x00003659 File Offset: 0x00001859
		[NullableContext(1)]
		public virtual Task<decimal?> ReadAsDecimalAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			return cancellationToken.CancelIfRequestedAsync<decimal?>() ?? Task.FromResult<decimal?>(this.ReadAsDecimal());
		}

		/// <summary>
		/// Asynchronously reads the next JSON token from the source as a <see cref="T:System.Nullable`1" /> of <see cref="T:System.Double" />.
		/// </summary>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task`1" /> that represents the asynchronous read. The <see cref="P:System.Threading.Tasks.Task`1.Result" />
		/// property returns the <see cref="T:System.Nullable`1" /> of <see cref="T:System.Double" />. This result will be <c>null</c> at the end of an array.</returns>
		/// <remarks>The default behaviour is to execute synchronously, returning an already-completed task. Derived
		/// classes can override this behaviour for true asynchronicity.</remarks>
		// Token: 0x060000DE RID: 222 RVA: 0x00003670 File Offset: 0x00001870
		[NullableContext(1)]
		public virtual Task<double?> ReadAsDoubleAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			return Task.FromResult<double?>(this.ReadAsDouble());
		}

		/// <summary>
		/// Asynchronously reads the next JSON token from the source as a <see cref="T:System.Nullable`1" /> of <see cref="T:System.Int32" />.
		/// </summary>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task`1" /> that represents the asynchronous read. The <see cref="P:System.Threading.Tasks.Task`1.Result" />
		/// property returns the <see cref="T:System.Nullable`1" /> of <see cref="T:System.Int32" />. This result will be <c>null</c> at the end of an array.</returns>
		/// <remarks>The default behaviour is to execute synchronously, returning an already-completed task. Derived
		/// classes can override this behaviour for true asynchronicity.</remarks>
		// Token: 0x060000DF RID: 223 RVA: 0x0000367D File Offset: 0x0000187D
		[NullableContext(1)]
		public virtual Task<int?> ReadAsInt32Async(CancellationToken cancellationToken = default(CancellationToken))
		{
			return cancellationToken.CancelIfRequestedAsync<int?>() ?? Task.FromResult<int?>(this.ReadAsInt32());
		}

		/// <summary>
		/// Asynchronously reads the next JSON token from the source as a <see cref="T:System.String" />.
		/// </summary>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
		/// <returns>A <see cref="T:System.Threading.Tasks.Task`1" /> that represents the asynchronous read. The <see cref="P:System.Threading.Tasks.Task`1.Result" />
		/// property returns the <see cref="T:System.String" />. This result will be <c>null</c> at the end of an array.</returns>
		/// <remarks>The default behaviour is to execute synchronously, returning an already-completed task. Derived
		/// classes can override this behaviour for true asynchronicity.</remarks>
		// Token: 0x060000E0 RID: 224 RVA: 0x00003694 File Offset: 0x00001894
		[return: Nullable(new byte[] { 1, 2 })]
		public virtual Task<string> ReadAsStringAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			return cancellationToken.CancelIfRequestedAsync<string>() ?? Task.FromResult<string>(this.ReadAsString());
		}

		// Token: 0x060000E1 RID: 225 RVA: 0x000036AC File Offset: 0x000018AC
		[NullableContext(1)]
		internal async Task<bool> ReadAndMoveToContentAsync(CancellationToken cancellationToken)
		{
			bool flag = await this.ReadAsync(cancellationToken).ConfigureAwait(false);
			if (flag)
			{
				flag = await this.MoveToContentAsync(cancellationToken).ConfigureAwait(false);
			}
			return flag;
		}

		// Token: 0x060000E2 RID: 226 RVA: 0x000036F8 File Offset: 0x000018F8
		[NullableContext(1)]
		internal Task<bool> MoveToContentAsync(CancellationToken cancellationToken)
		{
			JsonToken tokenType = this.TokenType;
			if (tokenType == JsonToken.None || tokenType == JsonToken.Comment)
			{
				return this.MoveToContentFromNonContentAsync(cancellationToken);
			}
			return AsyncUtils.True;
		}

		// Token: 0x060000E3 RID: 227 RVA: 0x00003720 File Offset: 0x00001920
		[NullableContext(1)]
		private async Task<bool> MoveToContentFromNonContentAsync(CancellationToken cancellationToken)
		{
			for (;;)
			{
				ConfiguredTaskAwaitable<bool>.ConfiguredTaskAwaiter configuredTaskAwaiter = this.ReadAsync(cancellationToken).ConfigureAwait(false).GetAwaiter();
				if (!configuredTaskAwaiter.IsCompleted)
				{
					await configuredTaskAwaiter;
					ConfiguredTaskAwaitable<bool>.ConfiguredTaskAwaiter configuredTaskAwaiter2;
					configuredTaskAwaiter = configuredTaskAwaiter2;
					configuredTaskAwaiter2 = default(ConfiguredTaskAwaitable<bool>.ConfiguredTaskAwaiter);
				}
				if (!configuredTaskAwaiter.GetResult())
				{
					break;
				}
				JsonToken tokenType = this.TokenType;
				if (tokenType != JsonToken.None && tokenType != JsonToken.Comment)
				{
					goto Block_3;
				}
			}
			return false;
			Block_3:
			return true;
		}

		/// <summary>
		/// Gets the current reader state.
		/// </summary>
		/// <value>The current reader state.</value>
		// Token: 0x1700002D RID: 45
		// (get) Token: 0x060000E4 RID: 228 RVA: 0x0000376B File Offset: 0x0000196B
		protected JsonReader.State CurrentState
		{
			get
			{
				return this._currentState;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether the source should be closed when this reader is closed.
		/// </summary>
		/// <value>
		/// <c>true</c> to close the source when this reader is closed; otherwise <c>false</c>. The default is <c>true</c>.
		/// </value>
		// Token: 0x1700002E RID: 46
		// (get) Token: 0x060000E5 RID: 229 RVA: 0x00003773 File Offset: 0x00001973
		// (set) Token: 0x060000E6 RID: 230 RVA: 0x0000377B File Offset: 0x0000197B
		public bool CloseInput { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether multiple pieces of JSON content can
		/// be read from a continuous stream without erroring.
		/// </summary>
		/// <value>
		/// <c>true</c> to support reading multiple pieces of JSON content; otherwise <c>false</c>.
		/// The default is <c>false</c>.
		/// </value>
		// Token: 0x1700002F RID: 47
		// (get) Token: 0x060000E7 RID: 231 RVA: 0x00003784 File Offset: 0x00001984
		// (set) Token: 0x060000E8 RID: 232 RVA: 0x0000378C File Offset: 0x0000198C
		public bool SupportMultipleContent { get; set; }

		/// <summary>
		/// Gets the quotation mark character used to enclose the value of a string.
		/// </summary>
		// Token: 0x17000030 RID: 48
		// (get) Token: 0x060000E9 RID: 233 RVA: 0x00003795 File Offset: 0x00001995
		// (set) Token: 0x060000EA RID: 234 RVA: 0x0000379D File Offset: 0x0000199D
		public virtual char QuoteChar
		{
			get
			{
				return this._quoteChar;
			}
			protected internal set
			{
				this._quoteChar = value;
			}
		}

		/// <summary>
		/// Gets or sets how <see cref="T:System.DateTime" /> time zones are handled when reading JSON.
		/// </summary>
		// Token: 0x17000031 RID: 49
		// (get) Token: 0x060000EB RID: 235 RVA: 0x000037A6 File Offset: 0x000019A6
		// (set) Token: 0x060000EC RID: 236 RVA: 0x000037AE File Offset: 0x000019AE
		public DateTimeZoneHandling DateTimeZoneHandling
		{
			get
			{
				return this._dateTimeZoneHandling;
			}
			set
			{
				if (value < DateTimeZoneHandling.Local || value > DateTimeZoneHandling.RoundtripKind)
				{
					throw new ArgumentOutOfRangeException("value");
				}
				this._dateTimeZoneHandling = value;
			}
		}

		/// <summary>
		/// Gets or sets how date formatted strings, e.g. "\/Date(1198908717056)\/" and "2012-03-21T05:40Z", are parsed when reading JSON.
		/// </summary>
		// Token: 0x17000032 RID: 50
		// (get) Token: 0x060000ED RID: 237 RVA: 0x000037CA File Offset: 0x000019CA
		// (set) Token: 0x060000EE RID: 238 RVA: 0x000037D2 File Offset: 0x000019D2
		public DateParseHandling DateParseHandling
		{
			get
			{
				return this._dateParseHandling;
			}
			set
			{
				if (value < DateParseHandling.None || value > DateParseHandling.DateTimeOffset)
				{
					throw new ArgumentOutOfRangeException("value");
				}
				this._dateParseHandling = value;
			}
		}

		/// <summary>
		/// Gets or sets how floating point numbers, e.g. 1.0 and 9.9, are parsed when reading JSON text.
		/// </summary>
		// Token: 0x17000033 RID: 51
		// (get) Token: 0x060000EF RID: 239 RVA: 0x000037EE File Offset: 0x000019EE
		// (set) Token: 0x060000F0 RID: 240 RVA: 0x000037F6 File Offset: 0x000019F6
		public FloatParseHandling FloatParseHandling
		{
			get
			{
				return this._floatParseHandling;
			}
			set
			{
				if (value < FloatParseHandling.Double || value > FloatParseHandling.Decimal)
				{
					throw new ArgumentOutOfRangeException("value");
				}
				this._floatParseHandling = value;
			}
		}

		/// <summary>
		/// Gets or sets how custom date formatted strings are parsed when reading JSON.
		/// </summary>
		// Token: 0x17000034 RID: 52
		// (get) Token: 0x060000F1 RID: 241 RVA: 0x00003812 File Offset: 0x00001A12
		// (set) Token: 0x060000F2 RID: 242 RVA: 0x0000381A File Offset: 0x00001A1A
		public string DateFormatString
		{
			get
			{
				return this._dateFormatString;
			}
			set
			{
				this._dateFormatString = value;
			}
		}

		/// <summary>
		/// Gets or sets the maximum depth allowed when reading JSON. Reading past this depth will throw a <see cref="T:Newtonsoft.Json.JsonReaderException" />.
		/// A null value means there is no maximum. 
		/// The default value is <c>64</c>.
		/// </summary>
		// Token: 0x17000035 RID: 53
		// (get) Token: 0x060000F3 RID: 243 RVA: 0x00003823 File Offset: 0x00001A23
		// (set) Token: 0x060000F4 RID: 244 RVA: 0x0000382C File Offset: 0x00001A2C
		public int? MaxDepth
		{
			get
			{
				return this._maxDepth;
			}
			set
			{
				int? num = value;
				int num2 = 0;
				if ((num.GetValueOrDefault() <= num2) & (num != null))
				{
					throw new ArgumentException("Value must be positive.", "value");
				}
				this._maxDepth = value;
			}
		}

		/// <summary>
		/// Gets the type of the current JSON token. 
		/// </summary>
		// Token: 0x17000036 RID: 54
		// (get) Token: 0x060000F5 RID: 245 RVA: 0x0000386B File Offset: 0x00001A6B
		public virtual JsonToken TokenType
		{
			get
			{
				return this._tokenType;
			}
		}

		/// <summary>
		/// Gets the text value of the current JSON token.
		/// </summary>
		// Token: 0x17000037 RID: 55
		// (get) Token: 0x060000F6 RID: 246 RVA: 0x00003873 File Offset: 0x00001A73
		public virtual object Value
		{
			get
			{
				return this._value;
			}
		}

		/// <summary>
		/// Gets the .NET type for the current JSON token.
		/// </summary>
		// Token: 0x17000038 RID: 56
		// (get) Token: 0x060000F7 RID: 247 RVA: 0x0000387B File Offset: 0x00001A7B
		public virtual Type ValueType
		{
			get
			{
				object value = this._value;
				if (value == null)
				{
					return null;
				}
				return value.GetType();
			}
		}

		/// <summary>
		/// Gets the depth of the current token in the JSON document.
		/// </summary>
		/// <value>The depth of the current token in the JSON document.</value>
		// Token: 0x17000039 RID: 57
		// (get) Token: 0x060000F8 RID: 248 RVA: 0x00003890 File Offset: 0x00001A90
		public virtual int Depth
		{
			get
			{
				List<JsonPosition> stack = this._stack;
				int num = ((stack != null) ? stack.Count : 0);
				if (JsonTokenUtils.IsStartToken(this.TokenType) || this._currentPosition.Type == JsonContainerType.None)
				{
					return num;
				}
				return num + 1;
			}
		}

		/// <summary>
		/// Gets the path of the current JSON token. 
		/// </summary>
		// Token: 0x1700003A RID: 58
		// (get) Token: 0x060000F9 RID: 249 RVA: 0x000038D0 File Offset: 0x00001AD0
		[Nullable(1)]
		public virtual string Path
		{
			[NullableContext(1)]
			get
			{
				if (this._currentPosition.Type == JsonContainerType.None)
				{
					return string.Empty;
				}
				JsonPosition? currentPosition = ((this._currentState != JsonReader.State.ArrayStart && this._currentState != JsonReader.State.ConstructorStart && this._currentState != JsonReader.State.ObjectStart) ? new JsonPosition?(this._currentPosition) : null);
				return JsonPosition.BuildPath(this._stack, currentPosition);
			}
		}

		/// <summary>
		/// Gets or sets the culture used when reading JSON. Defaults to <see cref="P:System.Globalization.CultureInfo.InvariantCulture" />.
		/// </summary>
		// Token: 0x1700003B RID: 59
		// (get) Token: 0x060000FA RID: 250 RVA: 0x00003937 File Offset: 0x00001B37
		// (set) Token: 0x060000FB RID: 251 RVA: 0x00003948 File Offset: 0x00001B48
		[Nullable(1)]
		public CultureInfo Culture
		{
			[NullableContext(1)]
			get
			{
				return this._culture ?? CultureInfo.InvariantCulture;
			}
			[NullableContext(1)]
			set
			{
				this._culture = value;
			}
		}

		// Token: 0x060000FC RID: 252 RVA: 0x00003951 File Offset: 0x00001B51
		internal JsonPosition GetPosition(int depth)
		{
			if (this._stack != null && depth < this._stack.Count)
			{
				return this._stack[depth];
			}
			return this._currentPosition;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.JsonReader" /> class.
		/// </summary>
		// Token: 0x060000FD RID: 253 RVA: 0x0000397C File Offset: 0x00001B7C
		protected JsonReader()
		{
			this._currentState = JsonReader.State.Start;
			this._dateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind;
			this._dateParseHandling = DateParseHandling.DateTime;
			this._floatParseHandling = FloatParseHandling.Double;
			this._maxDepth = new int?(64);
			this.CloseInput = true;
		}

		// Token: 0x060000FE RID: 254 RVA: 0x000039B4 File Offset: 0x00001BB4
		private void Push(JsonContainerType value)
		{
			this.UpdateScopeWithFinishedValue();
			if (this._currentPosition.Type == JsonContainerType.None)
			{
				this._currentPosition = new JsonPosition(value);
				return;
			}
			if (this._stack == null)
			{
				this._stack = new List<JsonPosition>();
			}
			this._stack.Add(this._currentPosition);
			this._currentPosition = new JsonPosition(value);
			if (this._maxDepth != null)
			{
				int num = this.Depth + 1;
				int? maxDepth = this._maxDepth;
				if (((num > maxDepth.GetValueOrDefault()) & (maxDepth != null)) && !this._hasExceededMaxDepth)
				{
					this._hasExceededMaxDepth = true;
					throw JsonReaderException.Create(this, "The reader's MaxDepth of {0} has been exceeded.".FormatWith(CultureInfo.InvariantCulture, this._maxDepth));
				}
			}
		}

		// Token: 0x060000FF RID: 255 RVA: 0x00003A70 File Offset: 0x00001C70
		private JsonContainerType Pop()
		{
			JsonPosition currentPosition;
			if (this._stack != null && this._stack.Count > 0)
			{
				currentPosition = this._currentPosition;
				this._currentPosition = this._stack[this._stack.Count - 1];
				this._stack.RemoveAt(this._stack.Count - 1);
			}
			else
			{
				currentPosition = this._currentPosition;
				this._currentPosition = default(JsonPosition);
			}
			if (this._maxDepth != null)
			{
				int depth = this.Depth;
				int? maxDepth = this._maxDepth;
				if ((depth <= maxDepth.GetValueOrDefault()) & (maxDepth != null))
				{
					this._hasExceededMaxDepth = false;
				}
			}
			return currentPosition.Type;
		}

		// Token: 0x06000100 RID: 256 RVA: 0x00003B22 File Offset: 0x00001D22
		private JsonContainerType Peek()
		{
			return this._currentPosition.Type;
		}

		/// <summary>
		/// Reads the next JSON token from the source.
		/// </summary>
		/// <returns><c>true</c> if the next token was read successfully; <c>false</c> if there are no more tokens to read.</returns>
		// Token: 0x06000101 RID: 257
		public abstract bool Read();

		/// <summary>
		/// Reads the next JSON token from the source as a <see cref="T:System.Nullable`1" /> of <see cref="T:System.Int32" />.
		/// </summary>
		/// <returns>A <see cref="T:System.Nullable`1" /> of <see cref="T:System.Int32" />. This method will return <c>null</c> at the end of an array.</returns>
		// Token: 0x06000102 RID: 258 RVA: 0x00003B30 File Offset: 0x00001D30
		public virtual int? ReadAsInt32()
		{
			JsonToken contentToken = this.GetContentToken();
			if (contentToken != JsonToken.None)
			{
				switch (contentToken)
				{
				case JsonToken.Integer:
				case JsonToken.Float:
				{
					object value = this.Value;
					int num;
					if (value is int)
					{
						num = (int)value;
						return new int?(num);
					}
					if (value is BigInteger)
					{
						BigInteger value2 = (BigInteger)value;
						num = (int)value2;
					}
					else
					{
						try
						{
							num = Convert.ToInt32(value, CultureInfo.InvariantCulture);
						}
						catch (Exception ex)
						{
							throw JsonReaderException.Create(this, "Could not convert to integer: {0}.".FormatWith(CultureInfo.InvariantCulture, value), ex);
						}
					}
					this.SetToken(JsonToken.Integer, num, false);
					return new int?(num);
				}
				case JsonToken.String:
				{
					string s = (string)this.Value;
					return this.ReadInt32String(s);
				}
				case JsonToken.Null:
				case JsonToken.EndArray:
					goto IL_37;
				}
				throw JsonReaderException.Create(this, "Error reading integer. Unexpected token: {0}.".FormatWith(CultureInfo.InvariantCulture, contentToken));
			}
			IL_37:
			return null;
		}

		// Token: 0x06000103 RID: 259 RVA: 0x00003C34 File Offset: 0x00001E34
		internal int? ReadInt32String(string s)
		{
			if (StringUtils.IsNullOrEmpty(s))
			{
				this.SetToken(JsonToken.Null, null, false);
				return null;
			}
			int num;
			if (int.TryParse(s, NumberStyles.Integer, this.Culture, out num))
			{
				this.SetToken(JsonToken.Integer, num, false);
				return new int?(num);
			}
			this.SetToken(JsonToken.String, s, false);
			throw JsonReaderException.Create(this, "Could not convert string to integer: {0}.".FormatWith(CultureInfo.InvariantCulture, s));
		}

		/// <summary>
		/// Reads the next JSON token from the source as a <see cref="T:System.String" />.
		/// </summary>
		/// <returns>A <see cref="T:System.String" />. This method will return <c>null</c> at the end of an array.</returns>
		// Token: 0x06000104 RID: 260 RVA: 0x00003CA4 File Offset: 0x00001EA4
		public virtual string ReadAsString()
		{
			JsonToken contentToken = this.GetContentToken();
			if (contentToken <= JsonToken.String)
			{
				if (contentToken != JsonToken.None)
				{
					if (contentToken != JsonToken.String)
					{
						goto IL_2E;
					}
					return (string)this.Value;
				}
			}
			else if (contentToken != JsonToken.Null && contentToken != JsonToken.EndArray)
			{
				goto IL_2E;
			}
			return null;
			IL_2E:
			if (JsonTokenUtils.IsPrimitiveToken(contentToken))
			{
				object value = this.Value;
				if (value != null)
				{
					IFormattable formattable = value as IFormattable;
					string text;
					if (formattable != null)
					{
						text = formattable.ToString(null, this.Culture);
					}
					else
					{
						Uri uri = value as Uri;
						text = ((uri != null) ? uri.OriginalString : value.ToString());
					}
					this.SetToken(JsonToken.String, text, false);
					return text;
				}
			}
			throw JsonReaderException.Create(this, "Error reading string. Unexpected token: {0}.".FormatWith(CultureInfo.InvariantCulture, contentToken));
		}

		/// <summary>
		/// Reads the next JSON token from the source as a <see cref="T:System.Byte" />[].
		/// </summary>
		/// <returns>A <see cref="T:System.Byte" />[] or <c>null</c> if the next JSON token is null. This method will return <c>null</c> at the end of an array.</returns>
		// Token: 0x06000105 RID: 261 RVA: 0x00003D50 File Offset: 0x00001F50
		public virtual byte[] ReadAsBytes()
		{
			JsonToken contentToken = this.GetContentToken();
			if (contentToken <= JsonToken.String)
			{
				switch (contentToken)
				{
				case JsonToken.None:
					break;
				case JsonToken.StartObject:
				{
					this.ReadIntoWrappedTypeObject();
					byte[] array = this.ReadAsBytes();
					this.ReaderReadAndAssert();
					if (this.TokenType != JsonToken.EndObject)
					{
						throw JsonReaderException.Create(this, "Error reading bytes. Unexpected token: {0}.".FormatWith(CultureInfo.InvariantCulture, this.TokenType));
					}
					this.SetToken(JsonToken.Bytes, array, false);
					return array;
				}
				case JsonToken.StartArray:
					return this.ReadArrayIntoByteArray();
				default:
				{
					if (contentToken != JsonToken.String)
					{
						goto IL_11C;
					}
					string text = (string)this.Value;
					byte[] array2;
					Guid guid;
					if (text.Length == 0)
					{
						array2 = CollectionUtils.ArrayEmpty<byte>();
					}
					else if (ConvertUtils.TryConvertGuid(text, out guid))
					{
						array2 = guid.ToByteArray();
					}
					else
					{
						array2 = Convert.FromBase64String(text);
					}
					this.SetToken(JsonToken.Bytes, array2, false);
					return array2;
				}
				}
			}
			else if (contentToken != JsonToken.Null && contentToken != JsonToken.EndArray)
			{
				if (contentToken != JsonToken.Bytes)
				{
					goto IL_11C;
				}
				object value = this.Value;
				if (value is Guid)
				{
					byte[] array3 = ((Guid)value).ToByteArray();
					this.SetToken(JsonToken.Bytes, array3, false);
					return array3;
				}
				return (byte[])this.Value;
			}
			return null;
			IL_11C:
			throw JsonReaderException.Create(this, "Error reading bytes. Unexpected token: {0}.".FormatWith(CultureInfo.InvariantCulture, contentToken));
		}

		// Token: 0x06000106 RID: 262 RVA: 0x00003E94 File Offset: 0x00002094
		[NullableContext(1)]
		internal byte[] ReadArrayIntoByteArray()
		{
			List<byte> list = new List<byte>();
			do
			{
				if (!this.Read())
				{
					this.SetToken(JsonToken.None);
				}
			}
			while (!this.ReadArrayElementIntoByteArrayReportDone(list));
			byte[] array = list.ToArray();
			this.SetToken(JsonToken.Bytes, array, false);
			return array;
		}

		// Token: 0x06000107 RID: 263 RVA: 0x00003ED4 File Offset: 0x000020D4
		[NullableContext(1)]
		private bool ReadArrayElementIntoByteArrayReportDone(List<byte> buffer)
		{
			JsonToken tokenType = this.TokenType;
			if (tokenType <= JsonToken.Comment)
			{
				if (tokenType == JsonToken.None)
				{
					throw JsonReaderException.Create(this, "Unexpected end when reading bytes.");
				}
				if (tokenType == JsonToken.Comment)
				{
					return false;
				}
			}
			else
			{
				if (tokenType == JsonToken.Integer)
				{
					buffer.Add(Convert.ToByte(this.Value, CultureInfo.InvariantCulture));
					return false;
				}
				if (tokenType == JsonToken.EndArray)
				{
					return true;
				}
			}
			throw JsonReaderException.Create(this, "Unexpected token when reading bytes: {0}.".FormatWith(CultureInfo.InvariantCulture, this.TokenType));
		}

		/// <summary>
		/// Reads the next JSON token from the source as a <see cref="T:System.Nullable`1" /> of <see cref="T:System.Double" />.
		/// </summary>
		/// <returns>A <see cref="T:System.Nullable`1" /> of <see cref="T:System.Double" />. This method will return <c>null</c> at the end of an array.</returns>
		// Token: 0x06000108 RID: 264 RVA: 0x00003F48 File Offset: 0x00002148
		public virtual double? ReadAsDouble()
		{
			JsonToken contentToken = this.GetContentToken();
			if (contentToken != JsonToken.None)
			{
				switch (contentToken)
				{
				case JsonToken.Integer:
				case JsonToken.Float:
				{
					object value = this.Value;
					double num;
					if (value is double)
					{
						num = (double)value;
						return new double?(num);
					}
					if (value is BigInteger)
					{
						BigInteger value2 = (BigInteger)value;
						num = (double)value2;
					}
					else
					{
						num = Convert.ToDouble(value, CultureInfo.InvariantCulture);
					}
					this.SetToken(JsonToken.Float, num, false);
					return new double?(num);
				}
				case JsonToken.String:
					return this.ReadDoubleString((string)this.Value);
				case JsonToken.Null:
				case JsonToken.EndArray:
					goto IL_34;
				}
				throw JsonReaderException.Create(this, "Error reading double. Unexpected token: {0}.".FormatWith(CultureInfo.InvariantCulture, contentToken));
			}
			IL_34:
			return null;
		}

		// Token: 0x06000109 RID: 265 RVA: 0x0000401C File Offset: 0x0000221C
		internal double? ReadDoubleString(string s)
		{
			if (StringUtils.IsNullOrEmpty(s))
			{
				this.SetToken(JsonToken.Null, null, false);
				return null;
			}
			double num;
			if (double.TryParse(s, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands | NumberStyles.AllowExponent, this.Culture, out num))
			{
				this.SetToken(JsonToken.Float, num, false);
				return new double?(num);
			}
			this.SetToken(JsonToken.String, s, false);
			throw JsonReaderException.Create(this, "Could not convert string to double: {0}.".FormatWith(CultureInfo.InvariantCulture, s));
		}

		/// <summary>
		/// Reads the next JSON token from the source as a <see cref="T:System.Nullable`1" /> of <see cref="T:System.Boolean" />.
		/// </summary>
		/// <returns>A <see cref="T:System.Nullable`1" /> of <see cref="T:System.Boolean" />. This method will return <c>null</c> at the end of an array.</returns>
		// Token: 0x0600010A RID: 266 RVA: 0x00004090 File Offset: 0x00002290
		public virtual bool? ReadAsBoolean()
		{
			JsonToken contentToken = this.GetContentToken();
			if (contentToken != JsonToken.None)
			{
				switch (contentToken)
				{
				case JsonToken.Integer:
				case JsonToken.Float:
				{
					object value = this.Value;
					bool flag;
					if (value is BigInteger)
					{
						BigInteger left = (BigInteger)value;
						flag = left != 0L;
					}
					else
					{
						flag = Convert.ToBoolean(this.Value, CultureInfo.InvariantCulture);
					}
					this.SetToken(JsonToken.Boolean, flag, false);
					return new bool?(flag);
				}
				case JsonToken.String:
					return this.ReadBooleanString((string)this.Value);
				case JsonToken.Boolean:
					return new bool?((bool)this.Value);
				case JsonToken.Null:
				case JsonToken.EndArray:
					goto IL_34;
				}
				throw JsonReaderException.Create(this, "Error reading boolean. Unexpected token: {0}.".FormatWith(CultureInfo.InvariantCulture, contentToken));
			}
			IL_34:
			return null;
		}

		// Token: 0x0600010B RID: 267 RVA: 0x00004164 File Offset: 0x00002364
		internal bool? ReadBooleanString(string s)
		{
			if (StringUtils.IsNullOrEmpty(s))
			{
				this.SetToken(JsonToken.Null, null, false);
				return null;
			}
			bool flag;
			if (bool.TryParse(s, out flag))
			{
				this.SetToken(JsonToken.Boolean, flag, false);
				return new bool?(flag);
			}
			this.SetToken(JsonToken.String, s, false);
			throw JsonReaderException.Create(this, "Could not convert string to boolean: {0}.".FormatWith(CultureInfo.InvariantCulture, s));
		}

		/// <summary>
		/// Reads the next JSON token from the source as a <see cref="T:System.Nullable`1" /> of <see cref="T:System.Decimal" />.
		/// </summary>
		/// <returns>A <see cref="T:System.Nullable`1" /> of <see cref="T:System.Decimal" />. This method will return <c>null</c> at the end of an array.</returns>
		// Token: 0x0600010C RID: 268 RVA: 0x000041D0 File Offset: 0x000023D0
		public virtual decimal? ReadAsDecimal()
		{
			JsonToken contentToken = this.GetContentToken();
			if (contentToken != JsonToken.None)
			{
				switch (contentToken)
				{
				case JsonToken.Integer:
				case JsonToken.Float:
				{
					object value = this.Value;
					decimal num;
					if (value is decimal)
					{
						num = (decimal)value;
						return new decimal?(num);
					}
					if (value is BigInteger)
					{
						BigInteger value2 = (BigInteger)value;
						num = (decimal)value2;
					}
					else
					{
						try
						{
							num = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
						}
						catch (Exception ex)
						{
							throw JsonReaderException.Create(this, "Could not convert to decimal: {0}.".FormatWith(CultureInfo.InvariantCulture, value), ex);
						}
					}
					this.SetToken(JsonToken.Float, num, false);
					return new decimal?(num);
				}
				case JsonToken.String:
					return this.ReadDecimalString((string)this.Value);
				case JsonToken.Null:
				case JsonToken.EndArray:
					goto IL_37;
				}
				throw JsonReaderException.Create(this, "Error reading decimal. Unexpected token: {0}.".FormatWith(CultureInfo.InvariantCulture, contentToken));
			}
			IL_37:
			return null;
		}

		// Token: 0x0600010D RID: 269 RVA: 0x000042D0 File Offset: 0x000024D0
		internal decimal? ReadDecimalString(string s)
		{
			if (StringUtils.IsNullOrEmpty(s))
			{
				this.SetToken(JsonToken.Null, null, false);
				return null;
			}
			decimal num;
			if (decimal.TryParse(s, NumberStyles.Number, this.Culture, out num))
			{
				this.SetToken(JsonToken.Float, num, false);
				return new decimal?(num);
			}
			if (ConvertUtils.DecimalTryParse(s.ToCharArray(), 0, s.Length, out num) == ParseResult.Success)
			{
				this.SetToken(JsonToken.Float, num, false);
				return new decimal?(num);
			}
			this.SetToken(JsonToken.String, s, false);
			throw JsonReaderException.Create(this, "Could not convert string to decimal: {0}.".FormatWith(CultureInfo.InvariantCulture, s));
		}

		/// <summary>
		/// Reads the next JSON token from the source as a <see cref="T:System.Nullable`1" /> of <see cref="T:System.DateTime" />.
		/// </summary>
		/// <returns>A <see cref="T:System.Nullable`1" /> of <see cref="T:System.DateTime" />. This method will return <c>null</c> at the end of an array.</returns>
		// Token: 0x0600010E RID: 270 RVA: 0x0000436C File Offset: 0x0000256C
		public virtual DateTime? ReadAsDateTime()
		{
			JsonToken contentToken = this.GetContentToken();
			if (contentToken <= JsonToken.String)
			{
				if (contentToken != JsonToken.None)
				{
					if (contentToken != JsonToken.String)
					{
						goto IL_7F;
					}
					return this.ReadDateTimeString((string)this.Value);
				}
			}
			else if (contentToken != JsonToken.Null && contentToken != JsonToken.EndArray)
			{
				if (contentToken != JsonToken.Date)
				{
					goto IL_7F;
				}
				object value = this.Value;
				if (value is DateTimeOffset)
				{
					this.SetToken(JsonToken.Date, ((DateTimeOffset)value).DateTime, false);
				}
				return new DateTime?((DateTime)this.Value);
			}
			return null;
			IL_7F:
			throw JsonReaderException.Create(this, "Error reading date. Unexpected token: {0}.".FormatWith(CultureInfo.InvariantCulture, this.TokenType));
		}

		// Token: 0x0600010F RID: 271 RVA: 0x00004418 File Offset: 0x00002618
		internal DateTime? ReadDateTimeString(string s)
		{
			if (StringUtils.IsNullOrEmpty(s))
			{
				this.SetToken(JsonToken.Null, null, false);
				return null;
			}
			DateTime dateTime;
			if (DateTimeUtils.TryParseDateTime(s, this.DateTimeZoneHandling, this._dateFormatString, this.Culture, out dateTime))
			{
				dateTime = DateTimeUtils.EnsureDateTime(dateTime, this.DateTimeZoneHandling);
				this.SetToken(JsonToken.Date, dateTime, false);
				return new DateTime?(dateTime);
			}
			if (DateTime.TryParse(s, this.Culture, DateTimeStyles.RoundtripKind, out dateTime))
			{
				dateTime = DateTimeUtils.EnsureDateTime(dateTime, this.DateTimeZoneHandling);
				this.SetToken(JsonToken.Date, dateTime, false);
				return new DateTime?(dateTime);
			}
			throw JsonReaderException.Create(this, "Could not convert string to DateTime: {0}.".FormatWith(CultureInfo.InvariantCulture, s));
		}

		/// <summary>
		/// Reads the next JSON token from the source as a <see cref="T:System.Nullable`1" /> of <see cref="T:System.DateTimeOffset" />.
		/// </summary>
		/// <returns>A <see cref="T:System.Nullable`1" /> of <see cref="T:System.DateTimeOffset" />. This method will return <c>null</c> at the end of an array.</returns>
		// Token: 0x06000110 RID: 272 RVA: 0x000044D0 File Offset: 0x000026D0
		public virtual DateTimeOffset? ReadAsDateTimeOffset()
		{
			JsonToken contentToken = this.GetContentToken();
			if (contentToken <= JsonToken.String)
			{
				if (contentToken != JsonToken.None)
				{
					if (contentToken != JsonToken.String)
					{
						goto IL_83;
					}
					string s = (string)this.Value;
					return this.ReadDateTimeOffsetString(s);
				}
			}
			else if (contentToken != JsonToken.Null && contentToken != JsonToken.EndArray)
			{
				if (contentToken != JsonToken.Date)
				{
					goto IL_83;
				}
				object value = this.Value;
				if (value is DateTime)
				{
					DateTime dateTime = (DateTime)value;
					this.SetToken(JsonToken.Date, new DateTimeOffset(dateTime), false);
				}
				return new DateTimeOffset?((DateTimeOffset)this.Value);
			}
			return null;
			IL_83:
			throw JsonReaderException.Create(this, "Error reading date. Unexpected token: {0}.".FormatWith(CultureInfo.InvariantCulture, contentToken));
		}

		// Token: 0x06000111 RID: 273 RVA: 0x0000457C File Offset: 0x0000277C
		internal DateTimeOffset? ReadDateTimeOffsetString(string s)
		{
			if (StringUtils.IsNullOrEmpty(s))
			{
				this.SetToken(JsonToken.Null, null, false);
				return null;
			}
			DateTimeOffset dateTimeOffset;
			if (DateTimeUtils.TryParseDateTimeOffset(s, this._dateFormatString, this.Culture, out dateTimeOffset))
			{
				this.SetToken(JsonToken.Date, dateTimeOffset, false);
				return new DateTimeOffset?(dateTimeOffset);
			}
			if (DateTimeOffset.TryParse(s, this.Culture, DateTimeStyles.RoundtripKind, out dateTimeOffset))
			{
				this.SetToken(JsonToken.Date, dateTimeOffset, false);
				return new DateTimeOffset?(dateTimeOffset);
			}
			this.SetToken(JsonToken.String, s, false);
			throw JsonReaderException.Create(this, "Could not convert string to DateTimeOffset: {0}.".FormatWith(CultureInfo.InvariantCulture, s));
		}

		// Token: 0x06000112 RID: 274 RVA: 0x0000461C File Offset: 0x0000281C
		internal void ReaderReadAndAssert()
		{
			if (!this.Read())
			{
				throw this.CreateUnexpectedEndException();
			}
		}

		// Token: 0x06000113 RID: 275 RVA: 0x0000462D File Offset: 0x0000282D
		[NullableContext(1)]
		internal JsonReaderException CreateUnexpectedEndException()
		{
			return JsonReaderException.Create(this, "Unexpected end when reading JSON.");
		}

		// Token: 0x06000114 RID: 276 RVA: 0x0000463C File Offset: 0x0000283C
		internal void ReadIntoWrappedTypeObject()
		{
			this.ReaderReadAndAssert();
			if (this.Value != null && this.Value.ToString() == "$type")
			{
				this.ReaderReadAndAssert();
				if (this.Value != null && this.Value.ToString().StartsWith("System.Byte[]", StringComparison.Ordinal))
				{
					this.ReaderReadAndAssert();
					if (this.Value.ToString() == "$value")
					{
						return;
					}
				}
			}
			throw JsonReaderException.Create(this, "Error reading bytes. Unexpected token: {0}.".FormatWith(CultureInfo.InvariantCulture, JsonToken.StartObject));
		}

		/// <summary>
		/// Skips the children of the current token.
		/// </summary>
		// Token: 0x06000115 RID: 277 RVA: 0x000046D0 File Offset: 0x000028D0
		public void Skip()
		{
			if (this.TokenType == JsonToken.PropertyName)
			{
				this.Read();
			}
			if (JsonTokenUtils.IsStartToken(this.TokenType))
			{
				int depth = this.Depth;
				while (this.Read() && depth < this.Depth)
				{
				}
			}
		}

		/// <summary>
		/// Sets the current token.
		/// </summary>
		/// <param name="newToken">The new token.</param>
		// Token: 0x06000116 RID: 278 RVA: 0x00004712 File Offset: 0x00002912
		protected void SetToken(JsonToken newToken)
		{
			this.SetToken(newToken, null, true);
		}

		/// <summary>
		/// Sets the current token and value.
		/// </summary>
		/// <param name="newToken">The new token.</param>
		/// <param name="value">The value.</param>
		// Token: 0x06000117 RID: 279 RVA: 0x0000471D File Offset: 0x0000291D
		protected void SetToken(JsonToken newToken, object value)
		{
			this.SetToken(newToken, value, true);
		}

		/// <summary>
		/// Sets the current token and value.
		/// </summary>
		/// <param name="newToken">The new token.</param>
		/// <param name="value">The value.</param>
		/// <param name="updateIndex">A flag indicating whether the position index inside an array should be updated.</param>
		// Token: 0x06000118 RID: 280 RVA: 0x00004728 File Offset: 0x00002928
		protected void SetToken(JsonToken newToken, object value, bool updateIndex)
		{
			this._tokenType = newToken;
			this._value = value;
			switch (newToken)
			{
			case JsonToken.StartObject:
				this._currentState = JsonReader.State.ObjectStart;
				this.Push(JsonContainerType.Object);
				return;
			case JsonToken.StartArray:
				this._currentState = JsonReader.State.ArrayStart;
				this.Push(JsonContainerType.Array);
				return;
			case JsonToken.StartConstructor:
				this._currentState = JsonReader.State.ConstructorStart;
				this.Push(JsonContainerType.Constructor);
				return;
			case JsonToken.PropertyName:
				this._currentState = JsonReader.State.Property;
				this._currentPosition.PropertyName = (string)value;
				return;
			case JsonToken.Comment:
				break;
			case JsonToken.Raw:
			case JsonToken.Integer:
			case JsonToken.Float:
			case JsonToken.String:
			case JsonToken.Boolean:
			case JsonToken.Null:
			case JsonToken.Undefined:
			case JsonToken.Date:
			case JsonToken.Bytes:
				this.SetPostValueState(updateIndex);
				break;
			case JsonToken.EndObject:
				this.ValidateEnd(JsonToken.EndObject);
				return;
			case JsonToken.EndArray:
				this.ValidateEnd(JsonToken.EndArray);
				return;
			case JsonToken.EndConstructor:
				this.ValidateEnd(JsonToken.EndConstructor);
				return;
			default:
				return;
			}
		}

		// Token: 0x06000119 RID: 281 RVA: 0x000047F9 File Offset: 0x000029F9
		internal void SetPostValueState(bool updateIndex)
		{
			if (this.Peek() != JsonContainerType.None || this.SupportMultipleContent)
			{
				this._currentState = JsonReader.State.PostValue;
			}
			else
			{
				this.SetFinished();
			}
			if (updateIndex)
			{
				this.UpdateScopeWithFinishedValue();
			}
		}

		// Token: 0x0600011A RID: 282 RVA: 0x00004823 File Offset: 0x00002A23
		private void UpdateScopeWithFinishedValue()
		{
			if (this._currentPosition.HasIndex)
			{
				this._currentPosition.Position = this._currentPosition.Position + 1;
			}
		}

		// Token: 0x0600011B RID: 283 RVA: 0x00004844 File Offset: 0x00002A44
		private void ValidateEnd(JsonToken endToken)
		{
			JsonContainerType jsonContainerType = this.Pop();
			if (this.GetTypeForCloseToken(endToken) != jsonContainerType)
			{
				throw JsonReaderException.Create(this, "JsonToken {0} is not valid for closing JsonType {1}.".FormatWith(CultureInfo.InvariantCulture, endToken, jsonContainerType));
			}
			if (this.Peek() != JsonContainerType.None || this.SupportMultipleContent)
			{
				this._currentState = JsonReader.State.PostValue;
				return;
			}
			this.SetFinished();
		}

		/// <summary>
		/// Sets the state based on current token type.
		/// </summary>
		// Token: 0x0600011C RID: 284 RVA: 0x000048A4 File Offset: 0x00002AA4
		protected void SetStateBasedOnCurrent()
		{
			JsonContainerType jsonContainerType = this.Peek();
			switch (jsonContainerType)
			{
			case JsonContainerType.None:
				this.SetFinished();
				return;
			case JsonContainerType.Object:
				this._currentState = JsonReader.State.Object;
				return;
			case JsonContainerType.Array:
				this._currentState = JsonReader.State.Array;
				return;
			case JsonContainerType.Constructor:
				this._currentState = JsonReader.State.Constructor;
				return;
			default:
				throw JsonReaderException.Create(this, "While setting the reader state back to current object an unexpected JsonType was encountered: {0}".FormatWith(CultureInfo.InvariantCulture, jsonContainerType));
			}
		}

		// Token: 0x0600011D RID: 285 RVA: 0x0000490B File Offset: 0x00002B0B
		private void SetFinished()
		{
			this._currentState = (this.SupportMultipleContent ? JsonReader.State.Start : JsonReader.State.Finished);
		}

		// Token: 0x0600011E RID: 286 RVA: 0x00004920 File Offset: 0x00002B20
		private JsonContainerType GetTypeForCloseToken(JsonToken token)
		{
			switch (token)
			{
			case JsonToken.EndObject:
				return JsonContainerType.Object;
			case JsonToken.EndArray:
				return JsonContainerType.Array;
			case JsonToken.EndConstructor:
				return JsonContainerType.Constructor;
			default:
				throw JsonReaderException.Create(this, "Not a valid close JsonToken: {0}".FormatWith(CultureInfo.InvariantCulture, token));
			}
		}

		// Token: 0x0600011F RID: 287 RVA: 0x0000495A File Offset: 0x00002B5A
		void IDisposable.Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Releases unmanaged and - optionally - managed resources.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
		// Token: 0x06000120 RID: 288 RVA: 0x00004969 File Offset: 0x00002B69
		protected virtual void Dispose(bool disposing)
		{
			if (this._currentState != JsonReader.State.Closed && disposing)
			{
				this.Close();
			}
		}

		/// <summary>
		/// Changes the reader's state to <see cref="F:Newtonsoft.Json.JsonReader.State.Closed" />.
		/// If <see cref="P:Newtonsoft.Json.JsonReader.CloseInput" /> is set to <c>true</c>, the source is also closed.
		/// </summary>
		// Token: 0x06000121 RID: 289 RVA: 0x00004981 File Offset: 0x00002B81
		public virtual void Close()
		{
			this._currentState = JsonReader.State.Closed;
			this._tokenType = JsonToken.None;
			this._value = null;
		}

		// Token: 0x06000122 RID: 290 RVA: 0x00004998 File Offset: 0x00002B98
		internal void ReadAndAssert()
		{
			if (!this.Read())
			{
				throw JsonSerializationException.Create(this, "Unexpected end when reading JSON.");
			}
		}

		// Token: 0x06000123 RID: 291 RVA: 0x000049AE File Offset: 0x00002BAE
		internal void ReadForTypeAndAssert(JsonContract contract, bool hasConverter)
		{
			if (!this.ReadForType(contract, hasConverter))
			{
				throw JsonSerializationException.Create(this, "Unexpected end when reading JSON.");
			}
		}

		// Token: 0x06000124 RID: 292 RVA: 0x000049C8 File Offset: 0x00002BC8
		internal bool ReadForType(JsonContract contract, bool hasConverter)
		{
			if (hasConverter)
			{
				return this.Read();
			}
			switch ((contract != null) ? contract.InternalReadType : ReadType.Read)
			{
			case ReadType.Read:
				return this.ReadAndMoveToContent();
			case ReadType.ReadAsInt32:
				this.ReadAsInt32();
				break;
			case ReadType.ReadAsInt64:
			{
				bool result = this.ReadAndMoveToContent();
				if (this.TokenType == JsonToken.Undefined)
				{
					throw JsonReaderException.Create(this, "An undefined token is not a valid {0}.".FormatWith(CultureInfo.InvariantCulture, ((contract != null) ? contract.UnderlyingType : null) ?? typeof(long)));
				}
				return result;
			}
			case ReadType.ReadAsBytes:
				this.ReadAsBytes();
				break;
			case ReadType.ReadAsString:
				this.ReadAsString();
				break;
			case ReadType.ReadAsDecimal:
				this.ReadAsDecimal();
				break;
			case ReadType.ReadAsDateTime:
				this.ReadAsDateTime();
				break;
			case ReadType.ReadAsDateTimeOffset:
				this.ReadAsDateTimeOffset();
				break;
			case ReadType.ReadAsDouble:
				this.ReadAsDouble();
				break;
			case ReadType.ReadAsBoolean:
				this.ReadAsBoolean();
				break;
			default:
				throw new ArgumentOutOfRangeException();
			}
			return this.TokenType > JsonToken.None;
		}

		// Token: 0x06000125 RID: 293 RVA: 0x00004AC1 File Offset: 0x00002CC1
		internal bool ReadAndMoveToContent()
		{
			return this.Read() && this.MoveToContent();
		}

		// Token: 0x06000126 RID: 294 RVA: 0x00004AD4 File Offset: 0x00002CD4
		internal bool MoveToContent()
		{
			JsonToken tokenType = this.TokenType;
			while (tokenType == JsonToken.None || tokenType == JsonToken.Comment)
			{
				if (!this.Read())
				{
					return false;
				}
				tokenType = this.TokenType;
			}
			return true;
		}

		// Token: 0x06000127 RID: 295 RVA: 0x00004B04 File Offset: 0x00002D04
		private JsonToken GetContentToken()
		{
			while (this.Read())
			{
				JsonToken tokenType = this.TokenType;
				if (tokenType != JsonToken.Comment)
				{
					return tokenType;
				}
			}
			this.SetToken(JsonToken.None);
			return JsonToken.None;
		}

		// Token: 0x0400005E RID: 94
		private JsonToken _tokenType;

		// Token: 0x0400005F RID: 95
		private object _value;

		// Token: 0x04000060 RID: 96
		internal char _quoteChar;

		// Token: 0x04000061 RID: 97
		internal JsonReader.State _currentState;

		// Token: 0x04000062 RID: 98
		private JsonPosition _currentPosition;

		// Token: 0x04000063 RID: 99
		private CultureInfo _culture;

		// Token: 0x04000064 RID: 100
		private DateTimeZoneHandling _dateTimeZoneHandling;

		// Token: 0x04000065 RID: 101
		private int? _maxDepth;

		// Token: 0x04000066 RID: 102
		private bool _hasExceededMaxDepth;

		// Token: 0x04000067 RID: 103
		internal DateParseHandling _dateParseHandling;

		// Token: 0x04000068 RID: 104
		internal FloatParseHandling _floatParseHandling;

		// Token: 0x04000069 RID: 105
		private string _dateFormatString;

		// Token: 0x0400006A RID: 106
		private List<JsonPosition> _stack;

		/// <summary>
		/// Specifies the state of the reader.
		/// </summary>
		// Token: 0x02000117 RID: 279
		[NullableContext(0)]
		protected internal enum State
		{
			/// <summary>
			/// A <see cref="T:Newtonsoft.Json.JsonReader" /> read method has not been called.
			/// </summary>
			// Token: 0x0400047F RID: 1151
			Start,
			/// <summary>
			/// The end of the file has been reached successfully.
			/// </summary>
			// Token: 0x04000480 RID: 1152
			Complete,
			/// <summary>
			/// Reader is at a property.
			/// </summary>
			// Token: 0x04000481 RID: 1153
			Property,
			/// <summary>
			/// Reader is at the start of an object.
			/// </summary>
			// Token: 0x04000482 RID: 1154
			ObjectStart,
			/// <summary>
			/// Reader is in an object.
			/// </summary>
			// Token: 0x04000483 RID: 1155
			Object,
			/// <summary>
			/// Reader is at the start of an array.
			/// </summary>
			// Token: 0x04000484 RID: 1156
			ArrayStart,
			/// <summary>
			/// Reader is in an array.
			/// </summary>
			// Token: 0x04000485 RID: 1157
			Array,
			/// <summary>
			/// The <see cref="M:Newtonsoft.Json.JsonReader.Close" /> method has been called.
			/// </summary>
			// Token: 0x04000486 RID: 1158
			Closed,
			/// <summary>
			/// Reader has just read a value.
			/// </summary>
			// Token: 0x04000487 RID: 1159
			PostValue,
			/// <summary>
			/// Reader is at the start of a constructor.
			/// </summary>
			// Token: 0x04000488 RID: 1160
			ConstructorStart,
			/// <summary>
			/// Reader is in a constructor.
			/// </summary>
			// Token: 0x04000489 RID: 1161
			Constructor,
			/// <summary>
			/// An error occurred that prevents the read operation from continuing.
			/// </summary>
			// Token: 0x0400048A RID: 1162
			Error,
			/// <summary>
			/// The end of the file has been reached successfully.
			/// </summary>
			// Token: 0x0400048B RID: 1163
			Finished
		}
	}
}
