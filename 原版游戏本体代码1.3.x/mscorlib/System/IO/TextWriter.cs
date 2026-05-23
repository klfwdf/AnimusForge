using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
	// Token: 0x020001A7 RID: 423
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public abstract class TextWriter : MarshalByRefObject, IDisposable
	{
		// Token: 0x06001A3E RID: 6718 RVA: 0x00057BD9 File Offset: 0x00055DD9
		[__DynamicallyInvokable]
		protected TextWriter()
		{
			this.InternalFormatProvider = null;
		}

		// Token: 0x06001A3F RID: 6719 RVA: 0x00057BFE File Offset: 0x00055DFE
		[__DynamicallyInvokable]
		protected TextWriter(IFormatProvider formatProvider)
		{
			this.InternalFormatProvider = formatProvider;
		}

		// Token: 0x170002EC RID: 748
		// (get) Token: 0x06001A40 RID: 6720 RVA: 0x00057C23 File Offset: 0x00055E23
		[__DynamicallyInvokable]
		public virtual IFormatProvider FormatProvider
		{
			[__DynamicallyInvokable]
			get
			{
				if (this.InternalFormatProvider == null)
				{
					return Thread.CurrentThread.CurrentCulture;
				}
				return this.InternalFormatProvider;
			}
		}

		// Token: 0x06001A41 RID: 6721 RVA: 0x00057C3E File Offset: 0x00055E3E
		public virtual void Close()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		// Token: 0x06001A42 RID: 6722 RVA: 0x00057C4D File Offset: 0x00055E4D
		[__DynamicallyInvokable]
		protected virtual void Dispose(bool disposing)
		{
		}

		// Token: 0x06001A43 RID: 6723 RVA: 0x00057C4F File Offset: 0x00055E4F
		[__DynamicallyInvokable]
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		// Token: 0x06001A44 RID: 6724 RVA: 0x00057C5E File Offset: 0x00055E5E
		[__DynamicallyInvokable]
		public virtual void Flush()
		{
		}

		// Token: 0x170002ED RID: 749
		// (get) Token: 0x06001A45 RID: 6725
		[__DynamicallyInvokable]
		public abstract Encoding Encoding
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x170002EE RID: 750
		// (get) Token: 0x06001A46 RID: 6726 RVA: 0x00057C60 File Offset: 0x00055E60
		// (set) Token: 0x06001A47 RID: 6727 RVA: 0x00057C6D File Offset: 0x00055E6D
		[__DynamicallyInvokable]
		public virtual string NewLine
		{
			[__DynamicallyInvokable]
			get
			{
				return new string(this.CoreNewLine);
			}
			[__DynamicallyInvokable]
			set
			{
				if (value == null)
				{
					value = "\r\n";
				}
				this.CoreNewLine = value.ToCharArray();
			}
		}

		// Token: 0x06001A48 RID: 6728 RVA: 0x00057C85 File Offset: 0x00055E85
		[HostProtection(SecurityAction.LinkDemand, Synchronization = true)]
		public static TextWriter Synchronized(TextWriter writer)
		{
			if (writer == null)
			{
				throw new ArgumentNullException("writer");
			}
			if (writer is TextWriter.SyncTextWriter)
			{
				return writer;
			}
			return new TextWriter.SyncTextWriter(writer);
		}

		// Token: 0x06001A49 RID: 6729 RVA: 0x00057CA5 File Offset: 0x00055EA5
		[__DynamicallyInvokable]
		public virtual void Write(char value)
		{
		}

		// Token: 0x06001A4A RID: 6730 RVA: 0x00057CA7 File Offset: 0x00055EA7
		[__DynamicallyInvokable]
		public virtual void Write(char[] buffer)
		{
			if (buffer != null)
			{
				this.Write(buffer, 0, buffer.Length);
			}
		}

		// Token: 0x06001A4B RID: 6731 RVA: 0x00057CB8 File Offset: 0x00055EB8
		[__DynamicallyInvokable]
		public virtual void Write(char[] buffer, int index, int count)
		{
			if (buffer == null)
			{
				throw new ArgumentNullException("buffer", Environment.GetResourceString("ArgumentNull_Buffer"));
			}
			if (index < 0)
			{
				throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (buffer.Length - index < count)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
			}
			for (int i = 0; i < count; i++)
			{
				this.Write(buffer[index + i]);
			}
		}

		// Token: 0x06001A4C RID: 6732 RVA: 0x00057D3E File Offset: 0x00055F3E
		[__DynamicallyInvokable]
		public virtual void Write(bool value)
		{
			this.Write(value ? "True" : "False");
		}

		// Token: 0x06001A4D RID: 6733 RVA: 0x00057D55 File Offset: 0x00055F55
		[__DynamicallyInvokable]
		public virtual void Write(int value)
		{
			this.Write(value.ToString(this.FormatProvider));
		}

		// Token: 0x06001A4E RID: 6734 RVA: 0x00057D6A File Offset: 0x00055F6A
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public virtual void Write(uint value)
		{
			this.Write(value.ToString(this.FormatProvider));
		}

		// Token: 0x06001A4F RID: 6735 RVA: 0x00057D7F File Offset: 0x00055F7F
		[__DynamicallyInvokable]
		public virtual void Write(long value)
		{
			this.Write(value.ToString(this.FormatProvider));
		}

		// Token: 0x06001A50 RID: 6736 RVA: 0x00057D94 File Offset: 0x00055F94
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public virtual void Write(ulong value)
		{
			this.Write(value.ToString(this.FormatProvider));
		}

		// Token: 0x06001A51 RID: 6737 RVA: 0x00057DA9 File Offset: 0x00055FA9
		[__DynamicallyInvokable]
		public virtual void Write(float value)
		{
			this.Write(value.ToString(this.FormatProvider));
		}

		// Token: 0x06001A52 RID: 6738 RVA: 0x00057DBE File Offset: 0x00055FBE
		[__DynamicallyInvokable]
		public virtual void Write(double value)
		{
			this.Write(value.ToString(this.FormatProvider));
		}

		// Token: 0x06001A53 RID: 6739 RVA: 0x00057DD3 File Offset: 0x00055FD3
		[__DynamicallyInvokable]
		public virtual void Write(decimal value)
		{
			this.Write(value.ToString(this.FormatProvider));
		}

		// Token: 0x06001A54 RID: 6740 RVA: 0x00057DE8 File Offset: 0x00055FE8
		[__DynamicallyInvokable]
		public virtual void Write(string value)
		{
			if (value != null)
			{
				this.Write(value.ToCharArray());
			}
		}

		// Token: 0x06001A55 RID: 6741 RVA: 0x00057DFC File Offset: 0x00055FFC
		[__DynamicallyInvokable]
		public virtual void Write(object value)
		{
			if (value != null)
			{
				IFormattable formattable = value as IFormattable;
				if (formattable != null)
				{
					this.Write(formattable.ToString(null, this.FormatProvider));
					return;
				}
				this.Write(value.ToString());
			}
		}

		// Token: 0x06001A56 RID: 6742 RVA: 0x00057E36 File Offset: 0x00056036
		[__DynamicallyInvokable]
		public virtual void Write(string format, object arg0)
		{
			this.Write(string.Format(this.FormatProvider, format, arg0));
		}

		// Token: 0x06001A57 RID: 6743 RVA: 0x00057E4B File Offset: 0x0005604B
		[__DynamicallyInvokable]
		public virtual void Write(string format, object arg0, object arg1)
		{
			this.Write(string.Format(this.FormatProvider, format, arg0, arg1));
		}

		// Token: 0x06001A58 RID: 6744 RVA: 0x00057E61 File Offset: 0x00056061
		[__DynamicallyInvokable]
		public virtual void Write(string format, object arg0, object arg1, object arg2)
		{
			this.Write(string.Format(this.FormatProvider, format, arg0, arg1, arg2));
		}

		// Token: 0x06001A59 RID: 6745 RVA: 0x00057E79 File Offset: 0x00056079
		[__DynamicallyInvokable]
		public virtual void Write(string format, params object[] arg)
		{
			this.Write(string.Format(this.FormatProvider, format, arg));
		}

		// Token: 0x06001A5A RID: 6746 RVA: 0x00057E8E File Offset: 0x0005608E
		[__DynamicallyInvokable]
		public virtual void WriteLine()
		{
			this.Write(this.CoreNewLine);
		}

		// Token: 0x06001A5B RID: 6747 RVA: 0x00057E9C File Offset: 0x0005609C
		[__DynamicallyInvokable]
		public virtual void WriteLine(char value)
		{
			this.Write(value);
			this.WriteLine();
		}

		// Token: 0x06001A5C RID: 6748 RVA: 0x00057EAB File Offset: 0x000560AB
		[__DynamicallyInvokable]
		public virtual void WriteLine(char[] buffer)
		{
			this.Write(buffer);
			this.WriteLine();
		}

		// Token: 0x06001A5D RID: 6749 RVA: 0x00057EBA File Offset: 0x000560BA
		[__DynamicallyInvokable]
		public virtual void WriteLine(char[] buffer, int index, int count)
		{
			this.Write(buffer, index, count);
			this.WriteLine();
		}

		// Token: 0x06001A5E RID: 6750 RVA: 0x00057ECB File Offset: 0x000560CB
		[__DynamicallyInvokable]
		public virtual void WriteLine(bool value)
		{
			this.Write(value);
			this.WriteLine();
		}

		// Token: 0x06001A5F RID: 6751 RVA: 0x00057EDA File Offset: 0x000560DA
		[__DynamicallyInvokable]
		public virtual void WriteLine(int value)
		{
			this.Write(value);
			this.WriteLine();
		}

		// Token: 0x06001A60 RID: 6752 RVA: 0x00057EE9 File Offset: 0x000560E9
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public virtual void WriteLine(uint value)
		{
			this.Write(value);
			this.WriteLine();
		}

		// Token: 0x06001A61 RID: 6753 RVA: 0x00057EF8 File Offset: 0x000560F8
		[__DynamicallyInvokable]
		public virtual void WriteLine(long value)
		{
			this.Write(value);
			this.WriteLine();
		}

		// Token: 0x06001A62 RID: 6754 RVA: 0x00057F07 File Offset: 0x00056107
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public virtual void WriteLine(ulong value)
		{
			this.Write(value);
			this.WriteLine();
		}

		// Token: 0x06001A63 RID: 6755 RVA: 0x00057F16 File Offset: 0x00056116
		[__DynamicallyInvokable]
		public virtual void WriteLine(float value)
		{
			this.Write(value);
			this.WriteLine();
		}

		// Token: 0x06001A64 RID: 6756 RVA: 0x00057F25 File Offset: 0x00056125
		[__DynamicallyInvokable]
		public virtual void WriteLine(double value)
		{
			this.Write(value);
			this.WriteLine();
		}

		// Token: 0x06001A65 RID: 6757 RVA: 0x00057F34 File Offset: 0x00056134
		[__DynamicallyInvokable]
		public virtual void WriteLine(decimal value)
		{
			this.Write(value);
			this.WriteLine();
		}

		// Token: 0x06001A66 RID: 6758 RVA: 0x00057F44 File Offset: 0x00056144
		[__DynamicallyInvokable]
		public virtual void WriteLine(string value)
		{
			if (value == null)
			{
				this.WriteLine();
				return;
			}
			int length = value.Length;
			int num = this.CoreNewLine.Length;
			char[] array = new char[length + num];
			value.CopyTo(0, array, 0, length);
			if (num == 2)
			{
				array[length] = this.CoreNewLine[0];
				array[length + 1] = this.CoreNewLine[1];
			}
			else if (num == 1)
			{
				array[length] = this.CoreNewLine[0];
			}
			else
			{
				Buffer.InternalBlockCopy(this.CoreNewLine, 0, array, length * 2, num * 2);
			}
			this.Write(array, 0, length + num);
		}

		// Token: 0x06001A67 RID: 6759 RVA: 0x00057FCC File Offset: 0x000561CC
		[__DynamicallyInvokable]
		public virtual void WriteLine(object value)
		{
			if (value == null)
			{
				this.WriteLine();
				return;
			}
			IFormattable formattable = value as IFormattable;
			if (formattable != null)
			{
				this.WriteLine(formattable.ToString(null, this.FormatProvider));
				return;
			}
			this.WriteLine(value.ToString());
		}

		// Token: 0x06001A68 RID: 6760 RVA: 0x0005800D File Offset: 0x0005620D
		[__DynamicallyInvokable]
		public virtual void WriteLine(string format, object arg0)
		{
			this.WriteLine(string.Format(this.FormatProvider, format, arg0));
		}

		// Token: 0x06001A69 RID: 6761 RVA: 0x00058022 File Offset: 0x00056222
		[__DynamicallyInvokable]
		public virtual void WriteLine(string format, object arg0, object arg1)
		{
			this.WriteLine(string.Format(this.FormatProvider, format, arg0, arg1));
		}

		// Token: 0x06001A6A RID: 6762 RVA: 0x00058038 File Offset: 0x00056238
		[__DynamicallyInvokable]
		public virtual void WriteLine(string format, object arg0, object arg1, object arg2)
		{
			this.WriteLine(string.Format(this.FormatProvider, format, arg0, arg1, arg2));
		}

		// Token: 0x06001A6B RID: 6763 RVA: 0x00058050 File Offset: 0x00056250
		[__DynamicallyInvokable]
		public virtual void WriteLine(string format, params object[] arg)
		{
			this.WriteLine(string.Format(this.FormatProvider, format, arg));
		}

		// Token: 0x06001A6C RID: 6764 RVA: 0x00058068 File Offset: 0x00056268
		[ComVisible(false)]
		[__DynamicallyInvokable]
		[HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
		public virtual Task WriteAsync(char value)
		{
			Tuple<TextWriter, char> state = new Tuple<TextWriter, char>(this, value);
			return Task.Factory.StartNew(TextWriter._WriteCharDelegate, state, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
		}

		// Token: 0x06001A6D RID: 6765 RVA: 0x00058098 File Offset: 0x00056298
		[ComVisible(false)]
		[__DynamicallyInvokable]
		[HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
		public virtual Task WriteAsync(string value)
		{
			Tuple<TextWriter, string> state = new Tuple<TextWriter, string>(this, value);
			return Task.Factory.StartNew(TextWriter._WriteStringDelegate, state, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
		}

		// Token: 0x06001A6E RID: 6766 RVA: 0x000580C8 File Offset: 0x000562C8
		[ComVisible(false)]
		[__DynamicallyInvokable]
		[HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
		public Task WriteAsync(char[] buffer)
		{
			if (buffer == null)
			{
				return Task.CompletedTask;
			}
			return this.WriteAsync(buffer, 0, buffer.Length);
		}

		// Token: 0x06001A6F RID: 6767 RVA: 0x000580E0 File Offset: 0x000562E0
		[ComVisible(false)]
		[__DynamicallyInvokable]
		[HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
		public virtual Task WriteAsync(char[] buffer, int index, int count)
		{
			Tuple<TextWriter, char[], int, int> state = new Tuple<TextWriter, char[], int, int>(this, buffer, index, count);
			return Task.Factory.StartNew(TextWriter._WriteCharArrayRangeDelegate, state, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
		}

		// Token: 0x06001A70 RID: 6768 RVA: 0x00058114 File Offset: 0x00056314
		[ComVisible(false)]
		[__DynamicallyInvokable]
		[HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
		public virtual Task WriteLineAsync(char value)
		{
			Tuple<TextWriter, char> state = new Tuple<TextWriter, char>(this, value);
			return Task.Factory.StartNew(TextWriter._WriteLineCharDelegate, state, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
		}

		// Token: 0x06001A71 RID: 6769 RVA: 0x00058144 File Offset: 0x00056344
		[ComVisible(false)]
		[__DynamicallyInvokable]
		[HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
		public virtual Task WriteLineAsync(string value)
		{
			Tuple<TextWriter, string> state = new Tuple<TextWriter, string>(this, value);
			return Task.Factory.StartNew(TextWriter._WriteLineStringDelegate, state, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
		}

		// Token: 0x06001A72 RID: 6770 RVA: 0x00058174 File Offset: 0x00056374
		[ComVisible(false)]
		[__DynamicallyInvokable]
		[HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
		public Task WriteLineAsync(char[] buffer)
		{
			if (buffer == null)
			{
				return Task.CompletedTask;
			}
			return this.WriteLineAsync(buffer, 0, buffer.Length);
		}

		// Token: 0x06001A73 RID: 6771 RVA: 0x0005818C File Offset: 0x0005638C
		[ComVisible(false)]
		[__DynamicallyInvokable]
		[HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
		public virtual Task WriteLineAsync(char[] buffer, int index, int count)
		{
			Tuple<TextWriter, char[], int, int> state = new Tuple<TextWriter, char[], int, int>(this, buffer, index, count);
			return Task.Factory.StartNew(TextWriter._WriteLineCharArrayRangeDelegate, state, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
		}

		// Token: 0x06001A74 RID: 6772 RVA: 0x000581BE File Offset: 0x000563BE
		[ComVisible(false)]
		[__DynamicallyInvokable]
		[HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
		public virtual Task WriteLineAsync()
		{
			return this.WriteAsync(this.CoreNewLine);
		}

		// Token: 0x06001A75 RID: 6773 RVA: 0x000581CC File Offset: 0x000563CC
		[ComVisible(false)]
		[__DynamicallyInvokable]
		[HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
		public virtual Task FlushAsync()
		{
			return Task.Factory.StartNew(TextWriter._FlushDelegate, this, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
		}

		// Token: 0x04000926 RID: 2342
		[__DynamicallyInvokable]
		public static readonly TextWriter Null = new TextWriter.NullTextWriter();

		// Token: 0x04000927 RID: 2343
		[NonSerialized]
		private static Action<object> _WriteCharDelegate = delegate(object state)
		{
			Tuple<TextWriter, char> tuple = (Tuple<TextWriter, char>)state;
			tuple.Item1.Write(tuple.Item2);
		};

		// Token: 0x04000928 RID: 2344
		[NonSerialized]
		private static Action<object> _WriteStringDelegate = delegate(object state)
		{
			Tuple<TextWriter, string> tuple = (Tuple<TextWriter, string>)state;
			tuple.Item1.Write(tuple.Item2);
		};

		// Token: 0x04000929 RID: 2345
		[NonSerialized]
		private static Action<object> _WriteCharArrayRangeDelegate = delegate(object state)
		{
			Tuple<TextWriter, char[], int, int> tuple = (Tuple<TextWriter, char[], int, int>)state;
			tuple.Item1.Write(tuple.Item2, tuple.Item3, tuple.Item4);
		};

		// Token: 0x0400092A RID: 2346
		[NonSerialized]
		private static Action<object> _WriteLineCharDelegate = delegate(object state)
		{
			Tuple<TextWriter, char> tuple = (Tuple<TextWriter, char>)state;
			tuple.Item1.WriteLine(tuple.Item2);
		};

		// Token: 0x0400092B RID: 2347
		[NonSerialized]
		private static Action<object> _WriteLineStringDelegate = delegate(object state)
		{
			Tuple<TextWriter, string> tuple = (Tuple<TextWriter, string>)state;
			tuple.Item1.WriteLine(tuple.Item2);
		};

		// Token: 0x0400092C RID: 2348
		[NonSerialized]
		private static Action<object> _WriteLineCharArrayRangeDelegate = delegate(object state)
		{
			Tuple<TextWriter, char[], int, int> tuple = (Tuple<TextWriter, char[], int, int>)state;
			tuple.Item1.WriteLine(tuple.Item2, tuple.Item3, tuple.Item4);
		};

		// Token: 0x0400092D RID: 2349
		[NonSerialized]
		private static Action<object> _FlushDelegate = delegate(object state)
		{
			((TextWriter)state).Flush();
		};

		// Token: 0x0400092E RID: 2350
		private const string InitialNewLine = "\r\n";

		// Token: 0x0400092F RID: 2351
		[__DynamicallyInvokable]
		protected char[] CoreNewLine = new char[] { '\r', '\n' };

		// Token: 0x04000930 RID: 2352
		private IFormatProvider InternalFormatProvider;

		// Token: 0x02000B28 RID: 2856
		[Serializable]
		private sealed class NullTextWriter : TextWriter
		{
			// Token: 0x06006B1B RID: 27419 RVA: 0x00173395 File Offset: 0x00171595
			internal NullTextWriter()
				: base(CultureInfo.InvariantCulture)
			{
			}

			// Token: 0x1700121A RID: 4634
			// (get) Token: 0x06006B1C RID: 27420 RVA: 0x001733A2 File Offset: 0x001715A2
			public override Encoding Encoding
			{
				get
				{
					return Encoding.Default;
				}
			}

			// Token: 0x06006B1D RID: 27421 RVA: 0x001733A9 File Offset: 0x001715A9
			public override void Write(char[] buffer, int index, int count)
			{
			}

			// Token: 0x06006B1E RID: 27422 RVA: 0x001733AB File Offset: 0x001715AB
			public override void Write(string value)
			{
			}

			// Token: 0x06006B1F RID: 27423 RVA: 0x001733AD File Offset: 0x001715AD
			public override void WriteLine()
			{
			}

			// Token: 0x06006B20 RID: 27424 RVA: 0x001733AF File Offset: 0x001715AF
			public override void WriteLine(string value)
			{
			}

			// Token: 0x06006B21 RID: 27425 RVA: 0x001733B1 File Offset: 0x001715B1
			public override void WriteLine(object value)
			{
			}
		}

		// Token: 0x02000B29 RID: 2857
		[Serializable]
		internal sealed class SyncTextWriter : TextWriter, IDisposable
		{
			// Token: 0x06006B22 RID: 27426 RVA: 0x001733B3 File Offset: 0x001715B3
			internal SyncTextWriter(TextWriter t)
				: base(t.FormatProvider)
			{
				this._out = t;
			}

			// Token: 0x1700121B RID: 4635
			// (get) Token: 0x06006B23 RID: 27427 RVA: 0x001733C8 File Offset: 0x001715C8
			public override Encoding Encoding
			{
				get
				{
					return this._out.Encoding;
				}
			}

			// Token: 0x1700121C RID: 4636
			// (get) Token: 0x06006B24 RID: 27428 RVA: 0x001733D5 File Offset: 0x001715D5
			public override IFormatProvider FormatProvider
			{
				get
				{
					return this._out.FormatProvider;
				}
			}

			// Token: 0x1700121D RID: 4637
			// (get) Token: 0x06006B25 RID: 27429 RVA: 0x001733E2 File Offset: 0x001715E2
			// (set) Token: 0x06006B26 RID: 27430 RVA: 0x001733EF File Offset: 0x001715EF
			public override string NewLine
			{
				[MethodImpl(MethodImplOptions.Synchronized)]
				get
				{
					return this._out.NewLine;
				}
				[MethodImpl(MethodImplOptions.Synchronized)]
				set
				{
					this._out.NewLine = value;
				}
			}

			// Token: 0x06006B27 RID: 27431 RVA: 0x001733FD File Offset: 0x001715FD
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override void Close()
			{
				this._out.Close();
			}

			// Token: 0x06006B28 RID: 27432 RVA: 0x0017340A File Offset: 0x0017160A
			[MethodImpl(MethodImplOptions.Synchronized)]
			protected override void Dispose(bool disposing)
			{
				if (disposing)
				{
					((IDisposable)this._out).Dispose();
				}
			}

			// Token: 0x06006B29 RID: 27433 RVA: 0x0017341A File Offset: 0x0017161A
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override void Flush()
			{
				this._out.Flush();
			}

			// Token: 0x06006B2A RID: 27434 RVA: 0x00173427 File Offset: 0x00171627
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override void Write(char value)
			{
				this._out.Write(value);
			}

			// Token: 0x06006B2B RID: 27435 RVA: 0x00173435 File Offset: 0x00171635
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override void Write(char[] buffer)
			{
				this._out.Write(buffer);
			}

			// Token: 0x06006B2C RID: 27436 RVA: 0x00173443 File Offset: 0x00171643
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override void Write(char[] buffer, int index, int count)
			{
				this._out.Write(buffer, index, count);
			}

			// Token: 0x06006B2D RID: 27437 RVA: 0x00173453 File Offset: 0x00171653
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override void Write(bool value)
			{
				this._out.Write(value);
			}

			// Token: 0x06006B2E RID: 27438 RVA: 0x00173461 File Offset: 0x00171661
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override void Write(int value)
			{
				this._out.Write(value);
			}

			// Token: 0x06006B2F RID: 27439 RVA: 0x0017346F File Offset: 0x0017166F
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override void Write(uint value)
			{
				this._out.Write(value);
			}

			// Token: 0x06006B30 RID: 27440 RVA: 0x0017347D File Offset: 0x0017167D
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override void Write(long value)
			{
				this._out.Write(value);
			}

			// Token: 0x06006B31 RID: 27441 RVA: 0x0017348B File Offset: 0x0017168B
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override void Write(ulong value)
			{
				this._out.Write(value);
			}

			// Token: 0x06006B32 RID: 27442 RVA: 0x00173499 File Offset: 0x00171699
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override void Write(float value)
			{
				this._out.Write(value);
			}

			// Token: 0x06006B33 RID: 27443 RVA: 0x001734A7 File Offset: 0x001716A7
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override void Write(double value)
			{
				this._out.Write(value);
			}

			// Token: 0x06006B34 RID: 27444 RVA: 0x001734B5 File Offset: 0x001716B5
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override void Write(decimal value)
			{
				this._out.Write(value);
			}

			// Token: 0x06006B35 RID: 27445 RVA: 0x001734C3 File Offset: 0x001716C3
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override void Write(string value)
			{
				this._out.Write(value);
			}

			// Token: 0x06006B36 RID: 27446 RVA: 0x001734D1 File Offset: 0x001716D1
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override void Write(object value)
			{
				this._out.Write(value);
			}

			// Token: 0x06006B37 RID: 27447 RVA: 0x001734DF File Offset: 0x001716DF
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override void Write(string format, object arg0)
			{
				this._out.Write(format, arg0);
			}

			// Token: 0x06006B38 RID: 27448 RVA: 0x001734EE File Offset: 0x001716EE
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override void Write(string format, object arg0, object arg1)
			{
				this._out.Write(format, arg0, arg1);
			}

			// Token: 0x06006B39 RID: 27449 RVA: 0x001734FE File Offset: 0x001716FE
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override void Write(string format, object arg0, object arg1, object arg2)
			{
				this._out.Write(format, arg0, arg1, arg2);
			}

			// Token: 0x06006B3A RID: 27450 RVA: 0x00173510 File Offset: 0x00171710
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override void Write(string format, params object[] arg)
			{
				this._out.Write(format, arg);
			}

			// Token: 0x06006B3B RID: 27451 RVA: 0x0017351F File Offset: 0x0017171F
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override void WriteLine()
			{
				this._out.WriteLine();
			}

			// Token: 0x06006B3C RID: 27452 RVA: 0x0017352C File Offset: 0x0017172C
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override void WriteLine(char value)
			{
				this._out.WriteLine(value);
			}

			// Token: 0x06006B3D RID: 27453 RVA: 0x0017353A File Offset: 0x0017173A
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override void WriteLine(decimal value)
			{
				this._out.WriteLine(value);
			}

			// Token: 0x06006B3E RID: 27454 RVA: 0x00173548 File Offset: 0x00171748
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override void WriteLine(char[] buffer)
			{
				this._out.WriteLine(buffer);
			}

			// Token: 0x06006B3F RID: 27455 RVA: 0x00173556 File Offset: 0x00171756
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override void WriteLine(char[] buffer, int index, int count)
			{
				this._out.WriteLine(buffer, index, count);
			}

			// Token: 0x06006B40 RID: 27456 RVA: 0x00173566 File Offset: 0x00171766
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override void WriteLine(bool value)
			{
				this._out.WriteLine(value);
			}

			// Token: 0x06006B41 RID: 27457 RVA: 0x00173574 File Offset: 0x00171774
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override void WriteLine(int value)
			{
				this._out.WriteLine(value);
			}

			// Token: 0x06006B42 RID: 27458 RVA: 0x00173582 File Offset: 0x00171782
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override void WriteLine(uint value)
			{
				this._out.WriteLine(value);
			}

			// Token: 0x06006B43 RID: 27459 RVA: 0x00173590 File Offset: 0x00171790
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override void WriteLine(long value)
			{
				this._out.WriteLine(value);
			}

			// Token: 0x06006B44 RID: 27460 RVA: 0x0017359E File Offset: 0x0017179E
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override void WriteLine(ulong value)
			{
				this._out.WriteLine(value);
			}

			// Token: 0x06006B45 RID: 27461 RVA: 0x001735AC File Offset: 0x001717AC
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override void WriteLine(float value)
			{
				this._out.WriteLine(value);
			}

			// Token: 0x06006B46 RID: 27462 RVA: 0x001735BA File Offset: 0x001717BA
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override void WriteLine(double value)
			{
				this._out.WriteLine(value);
			}

			// Token: 0x06006B47 RID: 27463 RVA: 0x001735C8 File Offset: 0x001717C8
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override void WriteLine(string value)
			{
				this._out.WriteLine(value);
			}

			// Token: 0x06006B48 RID: 27464 RVA: 0x001735D6 File Offset: 0x001717D6
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override void WriteLine(object value)
			{
				this._out.WriteLine(value);
			}

			// Token: 0x06006B49 RID: 27465 RVA: 0x001735E4 File Offset: 0x001717E4
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override void WriteLine(string format, object arg0)
			{
				this._out.WriteLine(format, arg0);
			}

			// Token: 0x06006B4A RID: 27466 RVA: 0x001735F3 File Offset: 0x001717F3
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override void WriteLine(string format, object arg0, object arg1)
			{
				this._out.WriteLine(format, arg0, arg1);
			}

			// Token: 0x06006B4B RID: 27467 RVA: 0x00173603 File Offset: 0x00171803
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override void WriteLine(string format, object arg0, object arg1, object arg2)
			{
				this._out.WriteLine(format, arg0, arg1, arg2);
			}

			// Token: 0x06006B4C RID: 27468 RVA: 0x00173615 File Offset: 0x00171815
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override void WriteLine(string format, params object[] arg)
			{
				this._out.WriteLine(format, arg);
			}

			// Token: 0x06006B4D RID: 27469 RVA: 0x00173624 File Offset: 0x00171824
			[ComVisible(false)]
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override Task WriteAsync(char value)
			{
				this.Write(value);
				return Task.CompletedTask;
			}

			// Token: 0x06006B4E RID: 27470 RVA: 0x00173632 File Offset: 0x00171832
			[ComVisible(false)]
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override Task WriteAsync(string value)
			{
				this.Write(value);
				return Task.CompletedTask;
			}

			// Token: 0x06006B4F RID: 27471 RVA: 0x00173640 File Offset: 0x00171840
			[ComVisible(false)]
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override Task WriteAsync(char[] buffer, int index, int count)
			{
				this.Write(buffer, index, count);
				return Task.CompletedTask;
			}

			// Token: 0x06006B50 RID: 27472 RVA: 0x00173650 File Offset: 0x00171850
			[ComVisible(false)]
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override Task WriteLineAsync(char value)
			{
				this.WriteLine(value);
				return Task.CompletedTask;
			}

			// Token: 0x06006B51 RID: 27473 RVA: 0x0017365E File Offset: 0x0017185E
			[ComVisible(false)]
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override Task WriteLineAsync(string value)
			{
				this.WriteLine(value);
				return Task.CompletedTask;
			}

			// Token: 0x06006B52 RID: 27474 RVA: 0x0017366C File Offset: 0x0017186C
			[ComVisible(false)]
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override Task WriteLineAsync(char[] buffer, int index, int count)
			{
				this.WriteLine(buffer, index, count);
				return Task.CompletedTask;
			}

			// Token: 0x06006B53 RID: 27475 RVA: 0x0017367C File Offset: 0x0017187C
			[ComVisible(false)]
			[MethodImpl(MethodImplOptions.Synchronized)]
			public override Task FlushAsync()
			{
				this.Flush();
				return Task.CompletedTask;
			}

			// Token: 0x04003339 RID: 13113
			private TextWriter _out;
		}
	}
}
