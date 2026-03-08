using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Iced.Intel.BlockEncoderInternal
{
	// Token: 0x020007DC RID: 2012
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class Block
	{
		// Token: 0x170007FF RID: 2047
		// (get) Token: 0x060026BB RID: 9915 RVA: 0x00085307 File Offset: 0x00083507
		public Instr[] Instructions
		{
			get
			{
				return this.instructions;
			}
		}

		// Token: 0x060026BC RID: 9916 RVA: 0x00085310 File Offset: 0x00083510
		public Block(BlockEncoder blockEncoder, CodeWriter codeWriter, ulong rip, [Nullable(2)] List<RelocInfo> relocInfos)
		{
			this.CodeWriter = new CodeWriterImpl(codeWriter);
			this.RIP = rip;
			this.relocInfos = relocInfos;
			this.instructions = Array2.Empty<Instr>();
			this.dataList = new List<BlockData>();
			this.alignment = (ulong)(blockEncoder.Bitness / 8);
			this.validData = new List<BlockData>();
		}

		// Token: 0x060026BD RID: 9917 RVA: 0x0008536E File Offset: 0x0008356E
		internal void SetInstructions(Instr[] instructions)
		{
			this.instructions = instructions;
		}

		// Token: 0x060026BE RID: 9918 RVA: 0x00085378 File Offset: 0x00083578
		public BlockData AllocPointerLocation()
		{
			BlockData data = new BlockData
			{
				IsValid = true
			};
			this.dataList.Add(data);
			return data;
		}

		// Token: 0x060026BF RID: 9919 RVA: 0x000853A0 File Offset: 0x000835A0
		public void InitializeData()
		{
			ulong baseAddr;
			if (this.Instructions.Length != 0)
			{
				Instr instr = this.Instructions[this.Instructions.Length - 1];
				baseAddr = instr.IP + (ulong)instr.Size;
			}
			else
			{
				baseAddr = this.RIP;
			}
			this.validDataAddress = baseAddr;
			ulong addr = (baseAddr + this.alignment - 1UL) & ~(this.alignment - 1UL);
			this.validDataAddressAligned = addr;
			foreach (BlockData data in this.dataList)
			{
				if (data.IsValid)
				{
					data.__dont_use_address = addr;
					data.__dont_use_address_initd = true;
					this.validData.Add(data);
					addr += this.alignment;
				}
			}
		}

		// Token: 0x060026C0 RID: 9920 RVA: 0x00085474 File Offset: 0x00083674
		public void WriteData()
		{
			if (this.validData.Count == 0)
			{
				return;
			}
			CodeWriterImpl codeWriter = this.CodeWriter;
			int alignment = (int)(this.validDataAddressAligned - this.validDataAddress);
			for (int i = 0; i < alignment; i++)
			{
				codeWriter.WriteByte(204);
			}
			List<RelocInfo> relocInfos = this.relocInfos;
			if ((int)this.alignment == 8)
			{
				using (List<BlockData>.Enumerator enumerator = this.validData.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						BlockData data = enumerator.Current;
						if (relocInfos != null)
						{
							relocInfos.Add(new RelocInfo(RelocKind.Offset64, data.Address));
						}
						uint d = (uint)data.Data;
						codeWriter.WriteByte((byte)d);
						codeWriter.WriteByte((byte)(d >> 8));
						codeWriter.WriteByte((byte)(d >> 16));
						codeWriter.WriteByte((byte)(d >> 24));
						d = (uint)(data.Data >> 32);
						codeWriter.WriteByte((byte)d);
						codeWriter.WriteByte((byte)(d >> 8));
						codeWriter.WriteByte((byte)(d >> 16));
						codeWriter.WriteByte((byte)(d >> 24));
					}
					return;
				}
			}
			throw new InvalidOperationException();
		}

		// Token: 0x17000800 RID: 2048
		// (get) Token: 0x060026C1 RID: 9921 RVA: 0x0008559C File Offset: 0x0008379C
		public bool CanAddRelocInfos
		{
			get
			{
				return this.relocInfos != null;
			}
		}

		// Token: 0x060026C2 RID: 9922 RVA: 0x000855AA File Offset: 0x000837AA
		public void AddRelocInfo(RelocInfo relocInfo)
		{
			List<RelocInfo> list = this.relocInfos;
			if (list == null)
			{
				return;
			}
			list.Add(relocInfo);
		}

		// Token: 0x04003954 RID: 14676
		public readonly CodeWriterImpl CodeWriter;

		// Token: 0x04003955 RID: 14677
		public readonly ulong RIP;

		// Token: 0x04003956 RID: 14678
		[Nullable(2)]
		public readonly List<RelocInfo> relocInfos;

		// Token: 0x04003957 RID: 14679
		private Instr[] instructions;

		// Token: 0x04003958 RID: 14680
		private readonly List<BlockData> dataList;

		// Token: 0x04003959 RID: 14681
		private readonly ulong alignment;

		// Token: 0x0400395A RID: 14682
		private readonly List<BlockData> validData;

		// Token: 0x0400395B RID: 14683
		private ulong validDataAddress;

		// Token: 0x0400395C RID: 14684
		private ulong validDataAddressAligned;
	}
}
