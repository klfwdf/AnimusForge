using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Iced.Intel.Internal;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007D9 RID: 2009
	[NullableContext(1)]
	[Nullable(0)]
	internal ref struct TableDeserializer
	{
		// Token: 0x060026A6 RID: 9894 RVA: 0x0008496F File Offset: 0x00082B6F
		[NullableContext(0)]
		public TableDeserializer([Nullable(1)] OpCodeHandlerReader handlerReader, int maxIds, ReadOnlySpan<byte> data)
		{
			this.handlerReader = handlerReader;
			this.reader = new DataReader(data);
			this.idToHandler = new List<HandlerInfo>(maxIds);
			this.handlerArray = new OpCodeHandler[1];
		}

		// Token: 0x060026A7 RID: 9895 RVA: 0x0008499C File Offset: 0x00082B9C
		public void Deserialize()
		{
			while (this.reader.CanRead)
			{
				SerializedDataKind serializedDataKind = (SerializedDataKind)this.reader.ReadByte();
				if (serializedDataKind != SerializedDataKind.HandlerReference)
				{
					if (serializedDataKind != SerializedDataKind.ArrayReference)
					{
						throw new InvalidOperationException();
					}
					this.idToHandler.Add(new HandlerInfo(this.ReadHandlers((int)this.reader.ReadCompressedUInt32())));
				}
				else
				{
					this.idToHandler.Add(new HandlerInfo(this.ReadHandler()));
				}
			}
			if (this.reader.CanRead)
			{
				throw new InvalidOperationException();
			}
		}

		// Token: 0x060026A8 RID: 9896 RVA: 0x00084A21 File Offset: 0x00082C21
		public LegacyOpCodeHandlerKind ReadLegacyOpCodeHandlerKind()
		{
			return (LegacyOpCodeHandlerKind)this.reader.ReadByte();
		}

		// Token: 0x060026A9 RID: 9897 RVA: 0x00084A21 File Offset: 0x00082C21
		public VexOpCodeHandlerKind ReadVexOpCodeHandlerKind()
		{
			return (VexOpCodeHandlerKind)this.reader.ReadByte();
		}

		// Token: 0x060026AA RID: 9898 RVA: 0x00084A21 File Offset: 0x00082C21
		public EvexOpCodeHandlerKind ReadEvexOpCodeHandlerKind()
		{
			return (EvexOpCodeHandlerKind)this.reader.ReadByte();
		}

		// Token: 0x060026AB RID: 9899 RVA: 0x00084A2E File Offset: 0x00082C2E
		public Code ReadCode()
		{
			return (Code)this.reader.ReadCompressedUInt32();
		}

		// Token: 0x060026AC RID: 9900 RVA: 0x00084A21 File Offset: 0x00082C21
		public Register ReadRegister()
		{
			return (Register)this.reader.ReadByte();
		}

		// Token: 0x060026AD RID: 9901 RVA: 0x00084A2E File Offset: 0x00082C2E
		public DecoderOptions ReadDecoderOptions()
		{
			return (DecoderOptions)this.reader.ReadCompressedUInt32();
		}

		// Token: 0x060026AE RID: 9902 RVA: 0x00084A2E File Offset: 0x00082C2E
		public HandlerFlags ReadHandlerFlags()
		{
			return (HandlerFlags)this.reader.ReadCompressedUInt32();
		}

		// Token: 0x060026AF RID: 9903 RVA: 0x00084A2E File Offset: 0x00082C2E
		public LegacyHandlerFlags ReadLegacyHandlerFlags()
		{
			return (LegacyHandlerFlags)this.reader.ReadCompressedUInt32();
		}

		// Token: 0x060026B0 RID: 9904 RVA: 0x00084A21 File Offset: 0x00082C21
		public TupleType ReadTupleType()
		{
			return (TupleType)this.reader.ReadByte();
		}

		// Token: 0x060026B1 RID: 9905 RVA: 0x00084A3B File Offset: 0x00082C3B
		public bool ReadBoolean()
		{
			return this.reader.ReadByte() > 0;
		}

		// Token: 0x060026B2 RID: 9906 RVA: 0x00084A2E File Offset: 0x00082C2E
		public int ReadInt32()
		{
			return (int)this.reader.ReadCompressedUInt32();
		}

		// Token: 0x060026B3 RID: 9907 RVA: 0x00084A4B File Offset: 0x00082C4B
		public OpCodeHandler ReadHandler()
		{
			OpCodeHandler opCodeHandler = this.ReadHandlerOrNull();
			if (opCodeHandler == null)
			{
				throw new InvalidOperationException();
			}
			return opCodeHandler;
		}

		// Token: 0x060026B4 RID: 9908 RVA: 0x00084A5D File Offset: 0x00082C5D
		[NullableContext(2)]
		public OpCodeHandler ReadHandlerOrNull()
		{
			if (this.handlerReader.ReadHandlers(ref this, this.handlerArray, 0) != 1)
			{
				throw new InvalidOperationException();
			}
			return this.handlerArray[0];
		}

		// Token: 0x060026B5 RID: 9909 RVA: 0x00084A84 File Offset: 0x00082C84
		[return: Nullable(new byte[] { 1, 2 })]
		public OpCodeHandler[] ReadHandlers(int count)
		{
			OpCodeHandler[] handlers = new OpCodeHandler[count];
			int num;
			for (int i = 0; i < handlers.Length; i += num)
			{
				num = this.handlerReader.ReadHandlers(ref this, handlers, i);
				if (num <= 0 || i + num > handlers.Length)
				{
					throw new InvalidOperationException();
				}
			}
			return handlers;
		}

		// Token: 0x060026B6 RID: 9910 RVA: 0x00084AC8 File Offset: 0x00082CC8
		public OpCodeHandler ReadHandlerReference()
		{
			uint index = (uint)this.reader.ReadByte();
			OpCodeHandler handler = this.idToHandler[(int)index].handler;
			if (handler == null)
			{
				throw new InvalidOperationException();
			}
			return handler;
		}

		// Token: 0x060026B7 RID: 9911 RVA: 0x00084AFC File Offset: 0x00082CFC
		public OpCodeHandler[] ReadArrayReference(uint kind)
		{
			if ((uint)this.reader.ReadByte() != kind)
			{
				throw new InvalidOperationException();
			}
			return this.GetTable((uint)this.reader.ReadByte());
		}

		// Token: 0x060026B8 RID: 9912 RVA: 0x00084B23 File Offset: 0x00082D23
		public OpCodeHandler[] GetTable(uint index)
		{
			OpCodeHandler[] handlers = this.idToHandler[(int)index].handlers;
			if (handlers == null)
			{
				throw new InvalidOperationException();
			}
			return handlers;
		}

		// Token: 0x04003900 RID: 14592
		private DataReader reader;

		// Token: 0x04003901 RID: 14593
		private readonly OpCodeHandlerReader handlerReader;

		// Token: 0x04003902 RID: 14594
		private readonly List<HandlerInfo> idToHandler;

		// Token: 0x04003903 RID: 14595
		private readonly OpCodeHandler[] handlerArray;
	}
}
