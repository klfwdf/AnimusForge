using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Newtonsoft.Json
{
	/// <summary>
	/// The default JSON name table implementation.
	/// </summary>
	// Token: 0x0200000F RID: 15
	[NullableContext(1)]
	[Nullable(0)]
	public class DefaultJsonNameTable : JsonNameTable
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.DefaultJsonNameTable" /> class.
		/// </summary>
		// Token: 0x0600000E RID: 14 RVA: 0x000020E8 File Offset: 0x000002E8
		public DefaultJsonNameTable()
		{
			this._entries = new DefaultJsonNameTable.Entry[this._mask + 1];
		}

		/// <summary>
		/// Gets a string containing the same characters as the specified range of characters in the given array.
		/// </summary>
		/// <param name="key">The character array containing the name to find.</param>
		/// <param name="start">The zero-based index into the array specifying the first character of the name.</param>
		/// <param name="length">The number of characters in the name.</param>
		/// <returns>A string containing the same characters as the specified range of characters in the given array.</returns>
		// Token: 0x0600000F RID: 15 RVA: 0x0000210C File Offset: 0x0000030C
		[return: Nullable(2)]
		public override string Get(char[] key, int start, int length)
		{
			if (length == 0)
			{
				return string.Empty;
			}
			int num = length + DefaultJsonNameTable.HashCodeRandomizer;
			num += (num << 7) ^ (int)key[start];
			int num2 = start + length;
			for (int i = start + 1; i < num2; i++)
			{
				num += (num << 7) ^ (int)key[i];
			}
			num -= num >> 17;
			num -= num >> 11;
			num -= num >> 5;
			int num3 = Volatile.Read(ref this._mask);
			int num4 = num & num3;
			for (DefaultJsonNameTable.Entry entry = this._entries[num4]; entry != null; entry = entry.Next)
			{
				if (entry.HashCode == num && DefaultJsonNameTable.TextEquals(entry.Value, key, start, length))
				{
					return entry.Value;
				}
			}
			return null;
		}

		/// <summary>
		/// Adds the specified string into name table.
		/// </summary>
		/// <param name="key">The string to add.</param>
		/// <remarks>This method is not thread-safe.</remarks>
		/// <returns>The resolved string.</returns>
		// Token: 0x06000010 RID: 16 RVA: 0x000021B8 File Offset: 0x000003B8
		public string Add(string key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			int length = key.Length;
			if (length == 0)
			{
				return string.Empty;
			}
			int num = length + DefaultJsonNameTable.HashCodeRandomizer;
			for (int i = 0; i < key.Length; i++)
			{
				num += (num << 7) ^ (int)key[i];
			}
			num -= num >> 17;
			num -= num >> 11;
			num -= num >> 5;
			for (DefaultJsonNameTable.Entry entry = this._entries[num & this._mask]; entry != null; entry = entry.Next)
			{
				if (entry.HashCode == num && entry.Value.Equals(key, StringComparison.Ordinal))
				{
					return entry.Value;
				}
			}
			return this.AddEntry(key, num);
		}

		// Token: 0x06000011 RID: 17 RVA: 0x00002264 File Offset: 0x00000464
		private string AddEntry(string str, int hashCode)
		{
			int num = hashCode & this._mask;
			DefaultJsonNameTable.Entry entry = new DefaultJsonNameTable.Entry(str, hashCode, this._entries[num]);
			this._entries[num] = entry;
			int count = this._count;
			this._count = count + 1;
			if (count == this._mask)
			{
				this.Grow();
			}
			return entry.Value;
		}

		// Token: 0x06000012 RID: 18 RVA: 0x000022B8 File Offset: 0x000004B8
		private void Grow()
		{
			DefaultJsonNameTable.Entry[] entries = this._entries;
			int num = this._mask * 2 + 1;
			DefaultJsonNameTable.Entry[] array = new DefaultJsonNameTable.Entry[num + 1];
			foreach (DefaultJsonNameTable.Entry entry in entries)
			{
				while (entry != null)
				{
					int num2 = entry.HashCode & num;
					DefaultJsonNameTable.Entry next = entry.Next;
					entry.Next = array[num2];
					array[num2] = entry;
					entry = next;
				}
			}
			this._entries = array;
			Volatile.Write(ref this._mask, num);
		}

		// Token: 0x06000013 RID: 19 RVA: 0x00002338 File Offset: 0x00000538
		private static bool TextEquals(string str1, char[] str2, int str2Start, int str2Length)
		{
			if (str1.Length != str2Length)
			{
				return false;
			}
			for (int i = 0; i < str1.Length; i++)
			{
				if (str1[i] != str2[str2Start + i])
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x04000014 RID: 20
		private static readonly int HashCodeRandomizer = Environment.TickCount;

		// Token: 0x04000015 RID: 21
		private int _count;

		// Token: 0x04000016 RID: 22
		private DefaultJsonNameTable.Entry[] _entries;

		// Token: 0x04000017 RID: 23
		private int _mask = 31;

		// Token: 0x02000116 RID: 278
		[Nullable(0)]
		private class Entry
		{
			// Token: 0x06000DF3 RID: 3571 RVA: 0x00037720 File Offset: 0x00035920
			internal Entry(string value, int hashCode, DefaultJsonNameTable.Entry next)
			{
				this.Value = value;
				this.HashCode = hashCode;
				this.Next = next;
			}

			// Token: 0x0400047B RID: 1147
			internal readonly string Value;

			// Token: 0x0400047C RID: 1148
			internal readonly int HashCode;

			// Token: 0x0400047D RID: 1149
			internal DefaultJsonNameTable.Entry Next;
		}
	}
}
