using System;
using System.IO;
using System.Text;
using Mono.Cecil.Cil;
using Mono.Cecil.PE;
using Mono.Collections.Generic;

namespace Mono.Cecil.Pdb
{
	// Token: 0x02000360 RID: 864
	internal class CustomMetadataWriter : IDisposable
	{
		// Token: 0x06001712 RID: 5906 RVA: 0x00047398 File Offset: 0x00045598
		public CustomMetadataWriter(SymWriter sym_writer)
		{
			this.sym_writer = sym_writer;
			this.stream = new MemoryStream();
			this.writer = new BinaryStreamWriter(this.stream);
			this.writer.WriteByte(4);
			this.writer.WriteByte(0);
			this.writer.Align(4);
		}

		// Token: 0x06001713 RID: 5907 RVA: 0x000473F4 File Offset: 0x000455F4
		public void WriteUsingInfo(ImportDebugInformation import_info)
		{
			this.Write(CustomMetadataType.UsingInfo, delegate
			{
				this.writer.WriteUInt16(1);
				this.writer.WriteUInt16((ushort)import_info.Targets.Count);
			});
		}

		// Token: 0x06001714 RID: 5908 RVA: 0x00047428 File Offset: 0x00045628
		public void WriteForwardInfo(MetadataToken import_parent)
		{
			this.Write(CustomMetadataType.ForwardInfo, delegate
			{
				this.writer.WriteUInt32(import_parent.ToUInt32());
			});
		}

		// Token: 0x06001715 RID: 5909 RVA: 0x0004745C File Offset: 0x0004565C
		public void WriteIteratorScopes(StateMachineScopeDebugInformation state_machine, MethodDebugInformation debug_info)
		{
			this.Write(CustomMetadataType.IteratorScopes, delegate
			{
				Collection<StateMachineScope> scopes = state_machine.Scopes;
				this.writer.WriteInt32(scopes.Count);
				foreach (StateMachineScope scope in scopes)
				{
					int start = scope.Start.Offset;
					int end = (scope.End.IsEndOfMethod ? debug_info.code_size : scope.End.Offset);
					this.writer.WriteInt32(start);
					this.writer.WriteInt32(end - 1);
				}
			});
		}

		// Token: 0x06001716 RID: 5910 RVA: 0x00047498 File Offset: 0x00045698
		public void WriteForwardIterator(TypeReference type)
		{
			this.Write(CustomMetadataType.ForwardIterator, delegate
			{
				this.writer.WriteBytes(Encoding.Unicode.GetBytes(type.Name));
			});
		}

		// Token: 0x06001717 RID: 5911 RVA: 0x000474CC File Offset: 0x000456CC
		private void Write(CustomMetadataType type, Action write)
		{
			this.count++;
			this.writer.WriteByte(4);
			this.writer.WriteByte((byte)type);
			this.writer.Align(4);
			int length_position = this.writer.Position;
			this.writer.WriteUInt32(0U);
			write();
			this.writer.Align(4);
			int end = this.writer.Position;
			int length = end - length_position + 4;
			this.writer.Position = length_position;
			this.writer.WriteInt32(length);
			this.writer.Position = end;
		}

		// Token: 0x06001718 RID: 5912 RVA: 0x0004756C File Offset: 0x0004576C
		public void WriteCustomMetadata()
		{
			if (this.count == 0)
			{
				return;
			}
			this.writer.BaseStream.Position = 1L;
			this.writer.WriteByte((byte)this.count);
			this.writer.Flush();
			this.sym_writer.DefineCustomMetadata("MD2", this.stream.ToArray());
		}

		// Token: 0x06001719 RID: 5913 RVA: 0x000475CC File Offset: 0x000457CC
		public void Dispose()
		{
			this.stream.Dispose();
		}

		// Token: 0x04000B47 RID: 2887
		private readonly SymWriter sym_writer;

		// Token: 0x04000B48 RID: 2888
		private readonly MemoryStream stream;

		// Token: 0x04000B49 RID: 2889
		private readonly BinaryStreamWriter writer;

		// Token: 0x04000B4A RID: 2890
		private int count;

		// Token: 0x04000B4B RID: 2891
		private const byte version = 4;
	}
}
