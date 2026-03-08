using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json
{
	// Token: 0x02000025 RID: 37
	[NullableContext(1)]
	[Nullable(0)]
	internal struct JsonPosition
	{
		// Token: 0x060000AC RID: 172 RVA: 0x000030B6 File Offset: 0x000012B6
		public JsonPosition(JsonContainerType type)
		{
			this.Type = type;
			this.HasIndex = JsonPosition.TypeHasIndex(type);
			this.Position = -1;
			this.PropertyName = null;
		}

		// Token: 0x060000AD RID: 173 RVA: 0x000030DC File Offset: 0x000012DC
		internal int CalculateLength()
		{
			JsonContainerType type = this.Type;
			if (type == JsonContainerType.Object)
			{
				return this.PropertyName.Length + 5;
			}
			if (type - JsonContainerType.Array > 1)
			{
				throw new ArgumentOutOfRangeException("Type");
			}
			return MathUtils.IntLength((ulong)((long)this.Position)) + 2;
		}

		// Token: 0x060000AE RID: 174 RVA: 0x00003124 File Offset: 0x00001324
		[NullableContext(2)]
		internal void WriteTo([Nullable(1)] StringBuilder sb, ref StringWriter writer, ref char[] buffer)
		{
			JsonContainerType type = this.Type;
			if (type != JsonContainerType.Object)
			{
				if (type - JsonContainerType.Array > 1)
				{
					return;
				}
				sb.Append('[');
				sb.Append(this.Position);
				sb.Append(']');
				return;
			}
			else
			{
				string propertyName = this.PropertyName;
				if (propertyName.IndexOfAny(JsonPosition.SpecialCharacters) != -1)
				{
					sb.Append("['");
					if (writer == null)
					{
						writer = new StringWriter(sb);
					}
					JavaScriptUtils.WriteEscapedJavaScriptString(writer, propertyName, '\'', false, JavaScriptUtils.SingleQuoteCharEscapeFlags, StringEscapeHandling.Default, null, ref buffer);
					sb.Append("']");
					return;
				}
				if (sb.Length > 0)
				{
					sb.Append('.');
				}
				sb.Append(propertyName);
				return;
			}
		}

		// Token: 0x060000AF RID: 175 RVA: 0x000031CA File Offset: 0x000013CA
		internal static bool TypeHasIndex(JsonContainerType type)
		{
			return type == JsonContainerType.Array || type == JsonContainerType.Constructor;
		}

		// Token: 0x060000B0 RID: 176 RVA: 0x000031D8 File Offset: 0x000013D8
		internal static string BuildPath(List<JsonPosition> positions, JsonPosition? currentPosition)
		{
			int num = 0;
			if (positions != null)
			{
				for (int i = 0; i < positions.Count; i++)
				{
					num += positions[i].CalculateLength();
				}
			}
			if (currentPosition != null)
			{
				num += currentPosition.GetValueOrDefault().CalculateLength();
			}
			StringBuilder stringBuilder = new StringBuilder(num);
			StringWriter stringWriter = null;
			char[] array = null;
			if (positions != null)
			{
				foreach (JsonPosition jsonPosition in positions)
				{
					jsonPosition.WriteTo(stringBuilder, ref stringWriter, ref array);
				}
			}
			if (currentPosition != null)
			{
				currentPosition.GetValueOrDefault().WriteTo(stringBuilder, ref stringWriter, ref array);
			}
			return stringBuilder.ToString();
		}

		// Token: 0x060000B1 RID: 177 RVA: 0x000032AC File Offset: 0x000014AC
		internal static string FormatMessage([Nullable(2)] IJsonLineInfo lineInfo, string path, string message)
		{
			if (!message.EndsWith(Environment.NewLine, StringComparison.Ordinal))
			{
				message = message.Trim();
				if (!message.EndsWith('.'))
				{
					message += ".";
				}
				message += " ";
			}
			message += "Path '{0}'".FormatWith(CultureInfo.InvariantCulture, path);
			if (lineInfo != null && lineInfo.HasLineInfo())
			{
				message += ", line {0}, position {1}".FormatWith(CultureInfo.InvariantCulture, lineInfo.LineNumber, lineInfo.LinePosition);
			}
			message += ".";
			return message;
		}

		// Token: 0x04000049 RID: 73
		private static readonly char[] SpecialCharacters = new char[]
		{
			'.', ' ', '\'', '/', '"', '[', ']', '(', ')', '\t',
			'\n', '\r', '\f', '\b', '\\', '\u0085', '\u2028', '\u2029'
		};

		// Token: 0x0400004A RID: 74
		internal JsonContainerType Type;

		// Token: 0x0400004B RID: 75
		internal int Position;

		// Token: 0x0400004C RID: 76
		[Nullable(2)]
		internal string PropertyName;

		// Token: 0x0400004D RID: 77
		internal bool HasIndex;
	}
}
