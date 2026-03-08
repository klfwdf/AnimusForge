using System;
using System.Runtime.CompilerServices;
using System.Text;
using MCM.Abstractions;
using TaleWorlds.Localization;

namespace MCM.UI
{
	// Token: 0x02000011 RID: 17
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class PresetKey : IEquatable<PresetKey>
	{
		// Token: 0x1700000D RID: 13
		// (get) Token: 0x06000048 RID: 72 RVA: 0x00002AFC File Offset: 0x00000CFC
		[CompilerGenerated]
		private Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(PresetKey);
			}
		}

		// Token: 0x1700000E RID: 14
		// (get) Token: 0x06000049 RID: 73 RVA: 0x00002B08 File Offset: 0x00000D08
		// (set) Token: 0x0600004A RID: 74 RVA: 0x00002B10 File Offset: 0x00000D10
		public string Id { get; set; }

		// Token: 0x1700000F RID: 15
		// (get) Token: 0x0600004B RID: 75 RVA: 0x00002B19 File Offset: 0x00000D19
		public string Name
		{
			get
			{
				return new TextObject(this._name, null).ToString();
			}
		}

		// Token: 0x0600004C RID: 76 RVA: 0x00002B2C File Offset: 0x00000D2C
		public PresetKey(ISettingsPreset preset)
		{
			this.Id = preset.Id;
			this._name = preset.Name;
		}

		// Token: 0x0600004D RID: 77 RVA: 0x00002B4C File Offset: 0x00000D4C
		public PresetKey(string id, string name)
		{
			this.Id = id;
			this._name = name;
		}

		// Token: 0x0600004E RID: 78 RVA: 0x00002B62 File Offset: 0x00000D62
		public override string ToString()
		{
			return this.Name;
		}

		// Token: 0x0600004F RID: 79 RVA: 0x00002B6A File Offset: 0x00000D6A
		public override int GetHashCode()
		{
			return this.Id.GetHashCode();
		}

		// Token: 0x06000050 RID: 80 RVA: 0x00002B77 File Offset: 0x00000D77
		[NullableContext(2)]
		public bool Equals(PresetKey other)
		{
			return this.Id.Equals((other != null) ? other.Id : null);
		}

		// Token: 0x06000051 RID: 81 RVA: 0x00002B90 File Offset: 0x00000D90
		[CompilerGenerated]
		private bool PrintMembers(StringBuilder builder)
		{
			RuntimeHelpers.EnsureSufficientExecutionStack();
			builder.Append("Id = ");
			builder.Append(this.Id);
			builder.Append(", Name = ");
			builder.Append(this.Name);
			return true;
		}

		// Token: 0x06000052 RID: 82 RVA: 0x00002BCA File Offset: 0x00000DCA
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator !=(PresetKey left, PresetKey right)
		{
			return !(left == right);
		}

		// Token: 0x06000053 RID: 83 RVA: 0x00002BD6 File Offset: 0x00000DD6
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator ==(PresetKey left, PresetKey right)
		{
			return left == right || (left != null && left.Equals(right));
		}

		// Token: 0x06000054 RID: 84 RVA: 0x00002BEA File Offset: 0x00000DEA
		[NullableContext(2)]
		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			return this.Equals(obj as PresetKey);
		}

		// Token: 0x06000056 RID: 86 RVA: 0x00002C00 File Offset: 0x00000E00
		[CompilerGenerated]
		private PresetKey(PresetKey original)
		{
			this.Id = original.<Id>k__BackingField;
			this._name = original._name;
		}

		// Token: 0x04000016 RID: 22
		private readonly string _name;
	}
}
