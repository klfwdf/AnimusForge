using System;
using System.Globalization;
using System.IO;
using System.Security;
using System.Text;

namespace System.Runtime.Serialization.Formatters.Binary
{
	// Token: 0x0200077A RID: 1914
	internal sealed class __BinaryParser
	{
		// Token: 0x06005352 RID: 21330 RVA: 0x0012433C File Offset: 0x0012253C
		internal __BinaryParser(Stream stream, ObjectReader objectReader)
		{
			this.input = stream;
			this.objectReader = objectReader;
			this.dataReader = new BinaryReader(this.input, __BinaryParser.encoding);
		}

		// Token: 0x17000DD4 RID: 3540
		// (get) Token: 0x06005353 RID: 21331 RVA: 0x0012438A File Offset: 0x0012258A
		internal BinaryAssemblyInfo SystemAssemblyInfo
		{
			get
			{
				if (this.systemAssemblyInfo == null)
				{
					this.systemAssemblyInfo = new BinaryAssemblyInfo(Converter.urtAssemblyString, Converter.urtAssembly);
				}
				return this.systemAssemblyInfo;
			}
		}

		// Token: 0x17000DD5 RID: 3541
		// (get) Token: 0x06005354 RID: 21332 RVA: 0x001243AF File Offset: 0x001225AF
		internal SizedArray ObjectMapIdTable
		{
			get
			{
				if (this.objectMapIdTable == null)
				{
					this.objectMapIdTable = new SizedArray();
				}
				return this.objectMapIdTable;
			}
		}

		// Token: 0x17000DD6 RID: 3542
		// (get) Token: 0x06005355 RID: 21333 RVA: 0x001243CA File Offset: 0x001225CA
		internal SizedArray AssemIdToAssemblyTable
		{
			get
			{
				if (this.assemIdToAssemblyTable == null)
				{
					this.assemIdToAssemblyTable = new SizedArray(2);
				}
				return this.assemIdToAssemblyTable;
			}
		}

		// Token: 0x17000DD7 RID: 3543
		// (get) Token: 0x06005356 RID: 21334 RVA: 0x001243E6 File Offset: 0x001225E6
		internal ParseRecord prs
		{
			get
			{
				if (this.PRS == null)
				{
					this.PRS = new ParseRecord();
				}
				return this.PRS;
			}
		}

		// Token: 0x06005357 RID: 21335 RVA: 0x00124404 File Offset: 0x00122604
		[SecurityCritical]
		internal void Run()
		{
			try
			{
				bool flag = true;
				this.ReadBegin();
				this.ReadSerializationHeaderRecord();
				while (flag)
				{
					BinaryHeaderEnum binaryHeaderEnum = BinaryHeaderEnum.Object;
					BinaryTypeEnum binaryTypeEnum = this.expectedType;
					if (binaryTypeEnum != BinaryTypeEnum.Primitive)
					{
						if (binaryTypeEnum - BinaryTypeEnum.String > 6)
						{
							throw new SerializationException(Environment.GetResourceString("Serialization_TypeExpected"));
						}
						byte b = this.dataReader.ReadByte();
						binaryHeaderEnum = (BinaryHeaderEnum)b;
						switch (binaryHeaderEnum)
						{
						case BinaryHeaderEnum.Object:
							this.ReadObject();
							break;
						case BinaryHeaderEnum.ObjectWithMap:
						case BinaryHeaderEnum.ObjectWithMapAssemId:
							this.ReadObjectWithMap(binaryHeaderEnum);
							break;
						case BinaryHeaderEnum.ObjectWithMapTyped:
						case BinaryHeaderEnum.ObjectWithMapTypedAssemId:
							this.ReadObjectWithMapTyped(binaryHeaderEnum);
							break;
						case BinaryHeaderEnum.ObjectString:
						case BinaryHeaderEnum.CrossAppDomainString:
							this.ReadObjectString(binaryHeaderEnum);
							break;
						case BinaryHeaderEnum.Array:
						case BinaryHeaderEnum.ArraySinglePrimitive:
						case BinaryHeaderEnum.ArraySingleObject:
						case BinaryHeaderEnum.ArraySingleString:
							this.ReadArray(binaryHeaderEnum);
							break;
						case BinaryHeaderEnum.MemberPrimitiveTyped:
							this.ReadMemberPrimitiveTyped();
							break;
						case BinaryHeaderEnum.MemberReference:
							this.ReadMemberReference();
							break;
						case BinaryHeaderEnum.ObjectNull:
						case BinaryHeaderEnum.ObjectNullMultiple256:
						case BinaryHeaderEnum.ObjectNullMultiple:
							this.ReadObjectNull(binaryHeaderEnum);
							break;
						case BinaryHeaderEnum.MessageEnd:
							flag = false;
							this.ReadMessageEnd();
							this.ReadEnd();
							break;
						case BinaryHeaderEnum.Assembly:
						case BinaryHeaderEnum.CrossAppDomainAssembly:
							this.ReadAssembly(binaryHeaderEnum);
							break;
						case BinaryHeaderEnum.CrossAppDomainMap:
							this.ReadCrossAppDomainMap();
							break;
						case BinaryHeaderEnum.MethodCall:
						case BinaryHeaderEnum.MethodReturn:
							this.ReadMethodObject(binaryHeaderEnum);
							break;
						default:
							throw new SerializationException(Environment.GetResourceString("Serialization_BinaryHeader", new object[] { b }));
						}
					}
					else
					{
						this.ReadMemberPrimitiveUnTyped();
					}
					if (binaryHeaderEnum != BinaryHeaderEnum.Assembly)
					{
						bool flag2 = false;
						while (!flag2)
						{
							ObjectProgress objectProgress = (ObjectProgress)this.stack.Peek();
							if (objectProgress == null)
							{
								this.expectedType = BinaryTypeEnum.ObjectUrt;
								this.expectedTypeInformation = null;
								flag2 = true;
							}
							else
							{
								flag2 = objectProgress.GetNext(out objectProgress.expectedType, out objectProgress.expectedTypeInformation);
								this.expectedType = objectProgress.expectedType;
								this.expectedTypeInformation = objectProgress.expectedTypeInformation;
								if (!flag2)
								{
									this.prs.Init();
									if (objectProgress.memberValueEnum == InternalMemberValueE.Nested)
									{
										this.prs.PRparseTypeEnum = InternalParseTypeE.MemberEnd;
										this.prs.PRmemberTypeEnum = objectProgress.memberTypeEnum;
										this.prs.PRmemberValueEnum = objectProgress.memberValueEnum;
										this.objectReader.Parse(this.prs);
									}
									else
									{
										this.prs.PRparseTypeEnum = InternalParseTypeE.ObjectEnd;
										this.prs.PRmemberTypeEnum = objectProgress.memberTypeEnum;
										this.prs.PRmemberValueEnum = objectProgress.memberValueEnum;
										this.objectReader.Parse(this.prs);
									}
									this.stack.Pop();
									this.PutOp(objectProgress);
								}
							}
						}
					}
				}
			}
			catch (EndOfStreamException)
			{
				throw new SerializationException(Environment.GetResourceString("Serialization_StreamEnd"));
			}
		}

		// Token: 0x06005358 RID: 21336 RVA: 0x001246BC File Offset: 0x001228BC
		internal void ReadBegin()
		{
		}

		// Token: 0x06005359 RID: 21337 RVA: 0x001246BE File Offset: 0x001228BE
		internal void ReadEnd()
		{
		}

		// Token: 0x0600535A RID: 21338 RVA: 0x001246C0 File Offset: 0x001228C0
		internal bool ReadBoolean()
		{
			return this.dataReader.ReadBoolean();
		}

		// Token: 0x0600535B RID: 21339 RVA: 0x001246CD File Offset: 0x001228CD
		internal byte ReadByte()
		{
			return this.dataReader.ReadByte();
		}

		// Token: 0x0600535C RID: 21340 RVA: 0x001246DA File Offset: 0x001228DA
		internal byte[] ReadBytes(int length)
		{
			return this.dataReader.ReadBytes(length);
		}

		// Token: 0x0600535D RID: 21341 RVA: 0x001246E8 File Offset: 0x001228E8
		internal void ReadBytes(byte[] byteA, int offset, int size)
		{
			while (size > 0)
			{
				int num = this.dataReader.Read(byteA, offset, size);
				if (num == 0)
				{
					__Error.EndOfFile();
				}
				offset += num;
				size -= num;
			}
		}

		// Token: 0x0600535E RID: 21342 RVA: 0x0012471C File Offset: 0x0012291C
		internal char ReadChar()
		{
			return this.dataReader.ReadChar();
		}

		// Token: 0x0600535F RID: 21343 RVA: 0x00124729 File Offset: 0x00122929
		internal char[] ReadChars(int length)
		{
			return this.dataReader.ReadChars(length);
		}

		// Token: 0x06005360 RID: 21344 RVA: 0x00124737 File Offset: 0x00122937
		internal decimal ReadDecimal()
		{
			return decimal.Parse(this.dataReader.ReadString(), CultureInfo.InvariantCulture);
		}

		// Token: 0x06005361 RID: 21345 RVA: 0x0012474E File Offset: 0x0012294E
		internal float ReadSingle()
		{
			return this.dataReader.ReadSingle();
		}

		// Token: 0x06005362 RID: 21346 RVA: 0x0012475B File Offset: 0x0012295B
		internal double ReadDouble()
		{
			return this.dataReader.ReadDouble();
		}

		// Token: 0x06005363 RID: 21347 RVA: 0x00124768 File Offset: 0x00122968
		internal short ReadInt16()
		{
			return this.dataReader.ReadInt16();
		}

		// Token: 0x06005364 RID: 21348 RVA: 0x00124775 File Offset: 0x00122975
		internal int ReadInt32()
		{
			return this.dataReader.ReadInt32();
		}

		// Token: 0x06005365 RID: 21349 RVA: 0x00124782 File Offset: 0x00122982
		internal long ReadInt64()
		{
			return this.dataReader.ReadInt64();
		}

		// Token: 0x06005366 RID: 21350 RVA: 0x0012478F File Offset: 0x0012298F
		internal sbyte ReadSByte()
		{
			return (sbyte)this.ReadByte();
		}

		// Token: 0x06005367 RID: 21351 RVA: 0x00124798 File Offset: 0x00122998
		internal string ReadString()
		{
			return this.dataReader.ReadString();
		}

		// Token: 0x06005368 RID: 21352 RVA: 0x001247A5 File Offset: 0x001229A5
		internal TimeSpan ReadTimeSpan()
		{
			return new TimeSpan(this.ReadInt64());
		}

		// Token: 0x06005369 RID: 21353 RVA: 0x001247B2 File Offset: 0x001229B2
		internal DateTime ReadDateTime()
		{
			return DateTime.FromBinaryRaw(this.ReadInt64());
		}

		// Token: 0x0600536A RID: 21354 RVA: 0x001247BF File Offset: 0x001229BF
		internal ushort ReadUInt16()
		{
			return this.dataReader.ReadUInt16();
		}

		// Token: 0x0600536B RID: 21355 RVA: 0x001247CC File Offset: 0x001229CC
		internal uint ReadUInt32()
		{
			return this.dataReader.ReadUInt32();
		}

		// Token: 0x0600536C RID: 21356 RVA: 0x001247D9 File Offset: 0x001229D9
		internal ulong ReadUInt64()
		{
			return this.dataReader.ReadUInt64();
		}

		// Token: 0x0600536D RID: 21357 RVA: 0x001247E8 File Offset: 0x001229E8
		[SecurityCritical]
		internal void ReadSerializationHeaderRecord()
		{
			SerializationHeaderRecord serializationHeaderRecord = new SerializationHeaderRecord();
			serializationHeaderRecord.Read(this);
			serializationHeaderRecord.Dump();
			this.topId = ((serializationHeaderRecord.topId > 0) ? this.objectReader.GetId((long)serializationHeaderRecord.topId) : ((long)serializationHeaderRecord.topId));
			this.headerId = ((serializationHeaderRecord.headerId > 0) ? this.objectReader.GetId((long)serializationHeaderRecord.headerId) : ((long)serializationHeaderRecord.headerId));
		}

		// Token: 0x0600536E RID: 21358 RVA: 0x0012485C File Offset: 0x00122A5C
		[SecurityCritical]
		internal void ReadAssembly(BinaryHeaderEnum binaryHeaderEnum)
		{
			BinaryAssembly binaryAssembly = new BinaryAssembly();
			if (binaryHeaderEnum == BinaryHeaderEnum.CrossAppDomainAssembly)
			{
				BinaryCrossAppDomainAssembly binaryCrossAppDomainAssembly = new BinaryCrossAppDomainAssembly();
				binaryCrossAppDomainAssembly.Read(this);
				binaryCrossAppDomainAssembly.Dump();
				binaryAssembly.assemId = binaryCrossAppDomainAssembly.assemId;
				binaryAssembly.assemblyString = this.objectReader.CrossAppDomainArray(binaryCrossAppDomainAssembly.assemblyIndex) as string;
				if (binaryAssembly.assemblyString == null)
				{
					throw new SerializationException(Environment.GetResourceString("Serialization_CrossAppDomainError", new object[] { "String", binaryCrossAppDomainAssembly.assemblyIndex }));
				}
			}
			else
			{
				binaryAssembly.Read(this);
				binaryAssembly.Dump();
			}
			this.AssemIdToAssemblyTable[binaryAssembly.assemId] = new BinaryAssemblyInfo(binaryAssembly.assemblyString);
		}

		// Token: 0x0600536F RID: 21359 RVA: 0x0012490C File Offset: 0x00122B0C
		[SecurityCritical]
		internal void ReadMethodObject(BinaryHeaderEnum binaryHeaderEnum)
		{
			if (binaryHeaderEnum == BinaryHeaderEnum.MethodCall)
			{
				BinaryMethodCall binaryMethodCall = new BinaryMethodCall();
				binaryMethodCall.Read(this);
				binaryMethodCall.Dump();
				this.objectReader.SetMethodCall(binaryMethodCall);
				return;
			}
			BinaryMethodReturn binaryMethodReturn = new BinaryMethodReturn();
			binaryMethodReturn.Read(this);
			binaryMethodReturn.Dump();
			this.objectReader.SetMethodReturn(binaryMethodReturn);
		}

		// Token: 0x06005370 RID: 21360 RVA: 0x00124960 File Offset: 0x00122B60
		[SecurityCritical]
		private void ReadObject()
		{
			if (this.binaryObject == null)
			{
				this.binaryObject = new BinaryObject();
			}
			this.binaryObject.Read(this);
			this.binaryObject.Dump();
			ObjectMap objectMap = (ObjectMap)this.ObjectMapIdTable[this.binaryObject.mapId];
			if (objectMap == null)
			{
				throw new SerializationException(Environment.GetResourceString("Serialization_Map", new object[] { this.binaryObject.mapId }));
			}
			ObjectProgress op = this.GetOp();
			ParseRecord pr = op.pr;
			this.stack.Push(op);
			op.objectTypeEnum = InternalObjectTypeE.Object;
			op.binaryTypeEnumA = objectMap.binaryTypeEnumA;
			op.memberNames = objectMap.memberNames;
			op.memberTypes = objectMap.memberTypes;
			op.typeInformationA = objectMap.typeInformationA;
			op.memberLength = op.binaryTypeEnumA.Length;
			ObjectProgress objectProgress = (ObjectProgress)this.stack.PeekPeek();
			if (objectProgress == null || objectProgress.isInitial)
			{
				op.name = objectMap.objectName;
				pr.PRparseTypeEnum = InternalParseTypeE.Object;
				op.memberValueEnum = InternalMemberValueE.Empty;
			}
			else
			{
				pr.PRparseTypeEnum = InternalParseTypeE.Member;
				pr.PRmemberValueEnum = InternalMemberValueE.Nested;
				op.memberValueEnum = InternalMemberValueE.Nested;
				InternalObjectTypeE objectTypeEnum = objectProgress.objectTypeEnum;
				if (objectTypeEnum != InternalObjectTypeE.Object)
				{
					if (objectTypeEnum != InternalObjectTypeE.Array)
					{
						throw new SerializationException(Environment.GetResourceString("Serialization_Map", new object[] { objectProgress.objectTypeEnum.ToString() }));
					}
					pr.PRmemberTypeEnum = InternalMemberTypeE.Item;
					op.memberTypeEnum = InternalMemberTypeE.Item;
				}
				else
				{
					pr.PRname = objectProgress.name;
					pr.PRmemberTypeEnum = InternalMemberTypeE.Field;
					op.memberTypeEnum = InternalMemberTypeE.Field;
				}
			}
			pr.PRobjectId = this.objectReader.GetId((long)this.binaryObject.objectId);
			pr.PRobjectInfo = objectMap.CreateObjectInfo(ref pr.PRsi, ref pr.PRmemberData);
			if (pr.PRobjectId == this.topId)
			{
				pr.PRobjectPositionEnum = InternalObjectPositionE.Top;
			}
			pr.PRobjectTypeEnum = InternalObjectTypeE.Object;
			pr.PRkeyDt = objectMap.objectName;
			pr.PRdtType = objectMap.objectType;
			pr.PRdtTypeCode = InternalPrimitiveTypeE.Invalid;
			this.objectReader.Parse(pr);
		}

		// Token: 0x06005371 RID: 21361 RVA: 0x00124B70 File Offset: 0x00122D70
		[SecurityCritical]
		internal void ReadCrossAppDomainMap()
		{
			BinaryCrossAppDomainMap binaryCrossAppDomainMap = new BinaryCrossAppDomainMap();
			binaryCrossAppDomainMap.Read(this);
			binaryCrossAppDomainMap.Dump();
			object obj = this.objectReader.CrossAppDomainArray(binaryCrossAppDomainMap.crossAppDomainArrayIndex);
			BinaryObjectWithMap binaryObjectWithMap = obj as BinaryObjectWithMap;
			if (binaryObjectWithMap != null)
			{
				binaryObjectWithMap.Dump();
				this.ReadObjectWithMap(binaryObjectWithMap);
				return;
			}
			BinaryObjectWithMapTyped binaryObjectWithMapTyped = obj as BinaryObjectWithMapTyped;
			if (binaryObjectWithMapTyped != null)
			{
				this.ReadObjectWithMapTyped(binaryObjectWithMapTyped);
				return;
			}
			throw new SerializationException(Environment.GetResourceString("Serialization_CrossAppDomainError", new object[] { "BinaryObjectMap", obj }));
		}

		// Token: 0x06005372 RID: 21362 RVA: 0x00124BF0 File Offset: 0x00122DF0
		[SecurityCritical]
		internal void ReadObjectWithMap(BinaryHeaderEnum binaryHeaderEnum)
		{
			if (this.bowm == null)
			{
				this.bowm = new BinaryObjectWithMap(binaryHeaderEnum);
			}
			else
			{
				this.bowm.binaryHeaderEnum = binaryHeaderEnum;
			}
			this.bowm.Read(this);
			this.bowm.Dump();
			this.ReadObjectWithMap(this.bowm);
		}

		// Token: 0x06005373 RID: 21363 RVA: 0x00124C44 File Offset: 0x00122E44
		[SecurityCritical]
		private void ReadObjectWithMap(BinaryObjectWithMap record)
		{
			BinaryAssemblyInfo binaryAssemblyInfo = null;
			ObjectProgress op = this.GetOp();
			ParseRecord pr = op.pr;
			this.stack.Push(op);
			if (record.binaryHeaderEnum == BinaryHeaderEnum.ObjectWithMapAssemId)
			{
				if (record.assemId < 1)
				{
					throw new SerializationException(Environment.GetResourceString("Serialization_Assembly", new object[] { record.name }));
				}
				binaryAssemblyInfo = (BinaryAssemblyInfo)this.AssemIdToAssemblyTable[record.assemId];
				if (binaryAssemblyInfo == null)
				{
					throw new SerializationException(Environment.GetResourceString("Serialization_Assembly", new object[] { record.assemId.ToString() + " " + record.name }));
				}
			}
			else if (record.binaryHeaderEnum == BinaryHeaderEnum.ObjectWithMap)
			{
				binaryAssemblyInfo = this.SystemAssemblyInfo;
			}
			Type type = this.objectReader.GetType(binaryAssemblyInfo, record.name);
			ObjectMap objectMap = ObjectMap.Create(record.name, type, record.memberNames, this.objectReader, record.objectId, binaryAssemblyInfo);
			this.ObjectMapIdTable[record.objectId] = objectMap;
			op.objectTypeEnum = InternalObjectTypeE.Object;
			op.binaryTypeEnumA = objectMap.binaryTypeEnumA;
			op.typeInformationA = objectMap.typeInformationA;
			op.memberLength = op.binaryTypeEnumA.Length;
			op.memberNames = objectMap.memberNames;
			op.memberTypes = objectMap.memberTypes;
			ObjectProgress objectProgress = (ObjectProgress)this.stack.PeekPeek();
			if (objectProgress == null || objectProgress.isInitial)
			{
				op.name = record.name;
				pr.PRparseTypeEnum = InternalParseTypeE.Object;
				op.memberValueEnum = InternalMemberValueE.Empty;
			}
			else
			{
				pr.PRparseTypeEnum = InternalParseTypeE.Member;
				pr.PRmemberValueEnum = InternalMemberValueE.Nested;
				op.memberValueEnum = InternalMemberValueE.Nested;
				InternalObjectTypeE objectTypeEnum = objectProgress.objectTypeEnum;
				if (objectTypeEnum != InternalObjectTypeE.Object)
				{
					if (objectTypeEnum != InternalObjectTypeE.Array)
					{
						throw new SerializationException(Environment.GetResourceString("Serialization_ObjectTypeEnum", new object[] { objectProgress.objectTypeEnum.ToString() }));
					}
					pr.PRmemberTypeEnum = InternalMemberTypeE.Item;
					op.memberTypeEnum = InternalMemberTypeE.Field;
				}
				else
				{
					pr.PRname = objectProgress.name;
					pr.PRmemberTypeEnum = InternalMemberTypeE.Field;
					op.memberTypeEnum = InternalMemberTypeE.Field;
				}
			}
			pr.PRobjectTypeEnum = InternalObjectTypeE.Object;
			pr.PRobjectId = this.objectReader.GetId((long)record.objectId);
			pr.PRobjectInfo = objectMap.CreateObjectInfo(ref pr.PRsi, ref pr.PRmemberData);
			if (pr.PRobjectId == this.topId)
			{
				pr.PRobjectPositionEnum = InternalObjectPositionE.Top;
			}
			pr.PRkeyDt = record.name;
			pr.PRdtType = objectMap.objectType;
			pr.PRdtTypeCode = InternalPrimitiveTypeE.Invalid;
			this.objectReader.Parse(pr);
		}

		// Token: 0x06005374 RID: 21364 RVA: 0x00124EC4 File Offset: 0x001230C4
		[SecurityCritical]
		internal void ReadObjectWithMapTyped(BinaryHeaderEnum binaryHeaderEnum)
		{
			if (this.bowmt == null)
			{
				this.bowmt = new BinaryObjectWithMapTyped(binaryHeaderEnum);
			}
			else
			{
				this.bowmt.binaryHeaderEnum = binaryHeaderEnum;
			}
			this.bowmt.Read(this);
			this.ReadObjectWithMapTyped(this.bowmt);
		}

		// Token: 0x06005375 RID: 21365 RVA: 0x00124F00 File Offset: 0x00123100
		[SecurityCritical]
		private void ReadObjectWithMapTyped(BinaryObjectWithMapTyped record)
		{
			BinaryAssemblyInfo binaryAssemblyInfo = null;
			ObjectProgress op = this.GetOp();
			ParseRecord pr = op.pr;
			this.stack.Push(op);
			if (record.binaryHeaderEnum == BinaryHeaderEnum.ObjectWithMapTypedAssemId)
			{
				if (record.assemId < 1)
				{
					throw new SerializationException(Environment.GetResourceString("Serialization_AssemblyId", new object[] { record.name }));
				}
				binaryAssemblyInfo = (BinaryAssemblyInfo)this.AssemIdToAssemblyTable[record.assemId];
				if (binaryAssemblyInfo == null)
				{
					throw new SerializationException(Environment.GetResourceString("Serialization_AssemblyId", new object[] { record.assemId.ToString() + " " + record.name }));
				}
			}
			else if (record.binaryHeaderEnum == BinaryHeaderEnum.ObjectWithMapTyped)
			{
				binaryAssemblyInfo = this.SystemAssemblyInfo;
			}
			ObjectMap objectMap = ObjectMap.Create(record.name, record.memberNames, record.binaryTypeEnumA, record.typeInformationA, record.memberAssemIds, this.objectReader, record.objectId, binaryAssemblyInfo, this.AssemIdToAssemblyTable);
			this.ObjectMapIdTable[record.objectId] = objectMap;
			op.objectTypeEnum = InternalObjectTypeE.Object;
			op.binaryTypeEnumA = objectMap.binaryTypeEnumA;
			op.typeInformationA = objectMap.typeInformationA;
			op.memberLength = op.binaryTypeEnumA.Length;
			op.memberNames = objectMap.memberNames;
			op.memberTypes = objectMap.memberTypes;
			ObjectProgress objectProgress = (ObjectProgress)this.stack.PeekPeek();
			if (objectProgress == null || objectProgress.isInitial)
			{
				op.name = record.name;
				pr.PRparseTypeEnum = InternalParseTypeE.Object;
				op.memberValueEnum = InternalMemberValueE.Empty;
			}
			else
			{
				pr.PRparseTypeEnum = InternalParseTypeE.Member;
				pr.PRmemberValueEnum = InternalMemberValueE.Nested;
				op.memberValueEnum = InternalMemberValueE.Nested;
				InternalObjectTypeE objectTypeEnum = objectProgress.objectTypeEnum;
				if (objectTypeEnum != InternalObjectTypeE.Object)
				{
					if (objectTypeEnum != InternalObjectTypeE.Array)
					{
						throw new SerializationException(Environment.GetResourceString("Serialization_ObjectTypeEnum", new object[] { objectProgress.objectTypeEnum.ToString() }));
					}
					pr.PRmemberTypeEnum = InternalMemberTypeE.Item;
					op.memberTypeEnum = InternalMemberTypeE.Item;
				}
				else
				{
					pr.PRname = objectProgress.name;
					pr.PRmemberTypeEnum = InternalMemberTypeE.Field;
					op.memberTypeEnum = InternalMemberTypeE.Field;
				}
			}
			pr.PRobjectTypeEnum = InternalObjectTypeE.Object;
			pr.PRobjectInfo = objectMap.CreateObjectInfo(ref pr.PRsi, ref pr.PRmemberData);
			pr.PRobjectId = this.objectReader.GetId((long)record.objectId);
			if (pr.PRobjectId == this.topId)
			{
				pr.PRobjectPositionEnum = InternalObjectPositionE.Top;
			}
			pr.PRkeyDt = record.name;
			pr.PRdtType = objectMap.objectType;
			pr.PRdtTypeCode = InternalPrimitiveTypeE.Invalid;
			this.objectReader.Parse(pr);
		}

		// Token: 0x06005376 RID: 21366 RVA: 0x0012517C File Offset: 0x0012337C
		[SecurityCritical]
		private void ReadObjectString(BinaryHeaderEnum binaryHeaderEnum)
		{
			if (this.objectString == null)
			{
				this.objectString = new BinaryObjectString();
			}
			if (binaryHeaderEnum == BinaryHeaderEnum.ObjectString)
			{
				this.objectString.Read(this);
				this.objectString.Dump();
			}
			else
			{
				if (this.crossAppDomainString == null)
				{
					this.crossAppDomainString = new BinaryCrossAppDomainString();
				}
				this.crossAppDomainString.Read(this);
				this.crossAppDomainString.Dump();
				this.objectString.value = this.objectReader.CrossAppDomainArray(this.crossAppDomainString.value) as string;
				if (this.objectString.value == null)
				{
					throw new SerializationException(Environment.GetResourceString("Serialization_CrossAppDomainError", new object[]
					{
						"String",
						this.crossAppDomainString.value
					}));
				}
				this.objectString.objectId = this.crossAppDomainString.objectId;
			}
			this.prs.Init();
			this.prs.PRparseTypeEnum = InternalParseTypeE.Object;
			this.prs.PRobjectId = this.objectReader.GetId((long)this.objectString.objectId);
			if (this.prs.PRobjectId == this.topId)
			{
				this.prs.PRobjectPositionEnum = InternalObjectPositionE.Top;
			}
			this.prs.PRobjectTypeEnum = InternalObjectTypeE.Object;
			ObjectProgress objectProgress = (ObjectProgress)this.stack.Peek();
			this.prs.PRvalue = this.objectString.value;
			this.prs.PRkeyDt = "System.String";
			this.prs.PRdtType = Converter.typeofString;
			this.prs.PRdtTypeCode = InternalPrimitiveTypeE.Invalid;
			this.prs.PRvarValue = this.objectString.value;
			if (objectProgress == null)
			{
				this.prs.PRparseTypeEnum = InternalParseTypeE.Object;
				this.prs.PRname = "System.String";
			}
			else
			{
				this.prs.PRparseTypeEnum = InternalParseTypeE.Member;
				this.prs.PRmemberValueEnum = InternalMemberValueE.InlineValue;
				InternalObjectTypeE objectTypeEnum = objectProgress.objectTypeEnum;
				if (objectTypeEnum != InternalObjectTypeE.Object)
				{
					if (objectTypeEnum != InternalObjectTypeE.Array)
					{
						throw new SerializationException(Environment.GetResourceString("Serialization_ObjectTypeEnum", new object[] { objectProgress.objectTypeEnum.ToString() }));
					}
					this.prs.PRmemberTypeEnum = InternalMemberTypeE.Item;
				}
				else
				{
					this.prs.PRname = objectProgress.name;
					this.prs.PRmemberTypeEnum = InternalMemberTypeE.Field;
				}
			}
			this.objectReader.Parse(this.prs);
		}

		// Token: 0x06005377 RID: 21367 RVA: 0x001253E0 File Offset: 0x001235E0
		[SecurityCritical]
		private void ReadMemberPrimitiveTyped()
		{
			if (this.memberPrimitiveTyped == null)
			{
				this.memberPrimitiveTyped = new MemberPrimitiveTyped();
			}
			this.memberPrimitiveTyped.Read(this);
			this.memberPrimitiveTyped.Dump();
			this.prs.PRobjectTypeEnum = InternalObjectTypeE.Object;
			ObjectProgress objectProgress = (ObjectProgress)this.stack.Peek();
			this.prs.Init();
			this.prs.PRvarValue = this.memberPrimitiveTyped.value;
			this.prs.PRkeyDt = Converter.ToComType(this.memberPrimitiveTyped.primitiveTypeEnum);
			this.prs.PRdtType = Converter.ToType(this.memberPrimitiveTyped.primitiveTypeEnum);
			this.prs.PRdtTypeCode = this.memberPrimitiveTyped.primitiveTypeEnum;
			if (objectProgress == null)
			{
				this.prs.PRparseTypeEnum = InternalParseTypeE.Object;
				this.prs.PRname = "System.Variant";
			}
			else
			{
				this.prs.PRparseTypeEnum = InternalParseTypeE.Member;
				this.prs.PRmemberValueEnum = InternalMemberValueE.InlineValue;
				InternalObjectTypeE objectTypeEnum = objectProgress.objectTypeEnum;
				if (objectTypeEnum != InternalObjectTypeE.Object)
				{
					if (objectTypeEnum != InternalObjectTypeE.Array)
					{
						throw new SerializationException(Environment.GetResourceString("Serialization_ObjectTypeEnum", new object[] { objectProgress.objectTypeEnum.ToString() }));
					}
					this.prs.PRmemberTypeEnum = InternalMemberTypeE.Item;
				}
				else
				{
					this.prs.PRname = objectProgress.name;
					this.prs.PRmemberTypeEnum = InternalMemberTypeE.Field;
				}
			}
			this.objectReader.Parse(this.prs);
		}

		// Token: 0x06005378 RID: 21368 RVA: 0x00125554 File Offset: 0x00123754
		[SecurityCritical]
		private void ReadArray(BinaryHeaderEnum binaryHeaderEnum)
		{
			BinaryArray binaryArray = new BinaryArray(binaryHeaderEnum);
			binaryArray.Read(this);
			BinaryAssemblyInfo assemblyInfo;
			if (binaryArray.binaryTypeEnum == BinaryTypeEnum.ObjectUser)
			{
				if (binaryArray.assemId < 1)
				{
					throw new SerializationException(Environment.GetResourceString("Serialization_AssemblyId", new object[] { binaryArray.typeInformation }));
				}
				assemblyInfo = (BinaryAssemblyInfo)this.AssemIdToAssemblyTable[binaryArray.assemId];
			}
			else
			{
				assemblyInfo = this.SystemAssemblyInfo;
			}
			ObjectProgress op = this.GetOp();
			ParseRecord pr = op.pr;
			op.objectTypeEnum = InternalObjectTypeE.Array;
			op.binaryTypeEnum = binaryArray.binaryTypeEnum;
			op.typeInformation = binaryArray.typeInformation;
			ObjectProgress objectProgress = (ObjectProgress)this.stack.PeekPeek();
			if (objectProgress == null || binaryArray.objectId > 0)
			{
				op.name = "System.Array";
				pr.PRparseTypeEnum = InternalParseTypeE.Object;
				op.memberValueEnum = InternalMemberValueE.Empty;
			}
			else
			{
				pr.PRparseTypeEnum = InternalParseTypeE.Member;
				pr.PRmemberValueEnum = InternalMemberValueE.Nested;
				op.memberValueEnum = InternalMemberValueE.Nested;
				InternalObjectTypeE objectTypeEnum = objectProgress.objectTypeEnum;
				if (objectTypeEnum != InternalObjectTypeE.Object)
				{
					if (objectTypeEnum != InternalObjectTypeE.Array)
					{
						throw new SerializationException(Environment.GetResourceString("Serialization_ObjectTypeEnum", new object[] { objectProgress.objectTypeEnum.ToString() }));
					}
					pr.PRmemberTypeEnum = InternalMemberTypeE.Item;
					op.memberTypeEnum = InternalMemberTypeE.Item;
				}
				else
				{
					pr.PRname = objectProgress.name;
					pr.PRmemberTypeEnum = InternalMemberTypeE.Field;
					op.memberTypeEnum = InternalMemberTypeE.Field;
					pr.PRkeyDt = objectProgress.name;
					pr.PRdtType = objectProgress.dtType;
				}
			}
			pr.PRobjectId = this.objectReader.GetId((long)binaryArray.objectId);
			if (pr.PRobjectId == this.topId)
			{
				pr.PRobjectPositionEnum = InternalObjectPositionE.Top;
			}
			else if (this.headerId > 0L && pr.PRobjectId == this.headerId)
			{
				pr.PRobjectPositionEnum = InternalObjectPositionE.Headers;
			}
			else
			{
				pr.PRobjectPositionEnum = InternalObjectPositionE.Child;
			}
			pr.PRobjectTypeEnum = InternalObjectTypeE.Array;
			BinaryConverter.TypeFromInfo(binaryArray.binaryTypeEnum, binaryArray.typeInformation, this.objectReader, assemblyInfo, out pr.PRarrayElementTypeCode, out pr.PRarrayElementTypeString, out pr.PRarrayElementType, out pr.PRisArrayVariant);
			pr.PRdtTypeCode = InternalPrimitiveTypeE.Invalid;
			pr.PRrank = binaryArray.rank;
			pr.PRlengthA = binaryArray.lengthA;
			pr.PRlowerBoundA = binaryArray.lowerBoundA;
			bool flag = false;
			switch (binaryArray.binaryArrayTypeEnum)
			{
			case BinaryArrayTypeEnum.Single:
			case BinaryArrayTypeEnum.SingleOffset:
				op.numItems = binaryArray.lengthA[0];
				pr.PRarrayTypeEnum = InternalArrayTypeE.Single;
				if (Converter.IsWriteAsByteArray(pr.PRarrayElementTypeCode) && binaryArray.lowerBoundA[0] == 0)
				{
					flag = true;
					this.ReadArrayAsBytes(pr);
				}
				break;
			case BinaryArrayTypeEnum.Jagged:
			case BinaryArrayTypeEnum.JaggedOffset:
				op.numItems = binaryArray.lengthA[0];
				pr.PRarrayTypeEnum = InternalArrayTypeE.Jagged;
				break;
			case BinaryArrayTypeEnum.Rectangular:
			case BinaryArrayTypeEnum.RectangularOffset:
			{
				int num = 1;
				for (int i = 0; i < binaryArray.rank; i++)
				{
					num *= binaryArray.lengthA[i];
				}
				op.numItems = num;
				pr.PRarrayTypeEnum = InternalArrayTypeE.Rectangular;
				break;
			}
			default:
				throw new SerializationException(Environment.GetResourceString("Serialization_ArrayType", new object[] { binaryArray.binaryArrayTypeEnum.ToString() }));
			}
			if (!flag)
			{
				this.stack.Push(op);
			}
			else
			{
				this.PutOp(op);
			}
			this.objectReader.Parse(pr);
			if (flag)
			{
				pr.PRparseTypeEnum = InternalParseTypeE.ObjectEnd;
				this.objectReader.Parse(pr);
			}
		}

		// Token: 0x06005379 RID: 21369 RVA: 0x001258A0 File Offset: 0x00123AA0
		[SecurityCritical]
		private void ReadArrayAsBytes(ParseRecord pr)
		{
			if (pr.PRarrayElementTypeCode == InternalPrimitiveTypeE.Byte)
			{
				pr.PRnewObj = this.ReadBytes(pr.PRlengthA[0]);
				return;
			}
			if (pr.PRarrayElementTypeCode == InternalPrimitiveTypeE.Char)
			{
				pr.PRnewObj = this.ReadChars(pr.PRlengthA[0]);
				return;
			}
			int num = Converter.TypeLength(pr.PRarrayElementTypeCode);
			pr.PRnewObj = Converter.CreatePrimitiveArray(pr.PRarrayElementTypeCode, pr.PRlengthA[0]);
			Array array = (Array)pr.PRnewObj;
			int i = 0;
			if (this.byteBuffer == null)
			{
				this.byteBuffer = new byte[4096];
			}
			while (i < array.Length)
			{
				int num2 = Math.Min(4096 / num, array.Length - i);
				int num3 = num2 * num;
				this.ReadBytes(this.byteBuffer, 0, num3);
				Buffer.InternalBlockCopy(this.byteBuffer, 0, array, i * num, num3);
				i += num2;
			}
		}

		// Token: 0x0600537A RID: 21370 RVA: 0x00125980 File Offset: 0x00123B80
		[SecurityCritical]
		private void ReadMemberPrimitiveUnTyped()
		{
			ObjectProgress objectProgress = (ObjectProgress)this.stack.Peek();
			if (this.memberPrimitiveUnTyped == null)
			{
				this.memberPrimitiveUnTyped = new MemberPrimitiveUnTyped();
			}
			this.memberPrimitiveUnTyped.Set((InternalPrimitiveTypeE)this.expectedTypeInformation);
			this.memberPrimitiveUnTyped.Read(this);
			this.memberPrimitiveUnTyped.Dump();
			this.prs.Init();
			this.prs.PRvarValue = this.memberPrimitiveUnTyped.value;
			this.prs.PRdtTypeCode = (InternalPrimitiveTypeE)this.expectedTypeInformation;
			this.prs.PRdtType = Converter.ToType(this.prs.PRdtTypeCode);
			this.prs.PRparseTypeEnum = InternalParseTypeE.Member;
			this.prs.PRmemberValueEnum = InternalMemberValueE.InlineValue;
			if (objectProgress.objectTypeEnum == InternalObjectTypeE.Object)
			{
				this.prs.PRmemberTypeEnum = InternalMemberTypeE.Field;
				this.prs.PRname = objectProgress.name;
			}
			else
			{
				this.prs.PRmemberTypeEnum = InternalMemberTypeE.Item;
			}
			this.objectReader.Parse(this.prs);
		}

		// Token: 0x0600537B RID: 21371 RVA: 0x00125A90 File Offset: 0x00123C90
		[SecurityCritical]
		private void ReadMemberReference()
		{
			if (this.memberReference == null)
			{
				this.memberReference = new MemberReference();
			}
			this.memberReference.Read(this);
			this.memberReference.Dump();
			ObjectProgress objectProgress = (ObjectProgress)this.stack.Peek();
			this.prs.Init();
			this.prs.PRidRef = this.objectReader.GetId((long)this.memberReference.idRef);
			this.prs.PRparseTypeEnum = InternalParseTypeE.Member;
			this.prs.PRmemberValueEnum = InternalMemberValueE.Reference;
			if (objectProgress.objectTypeEnum == InternalObjectTypeE.Object)
			{
				this.prs.PRmemberTypeEnum = InternalMemberTypeE.Field;
				this.prs.PRname = objectProgress.name;
				this.prs.PRdtType = objectProgress.dtType;
			}
			else
			{
				this.prs.PRmemberTypeEnum = InternalMemberTypeE.Item;
			}
			this.objectReader.Parse(this.prs);
		}

		// Token: 0x0600537C RID: 21372 RVA: 0x00125B74 File Offset: 0x00123D74
		[SecurityCritical]
		private void ReadObjectNull(BinaryHeaderEnum binaryHeaderEnum)
		{
			if (this.objectNull == null)
			{
				this.objectNull = new ObjectNull();
			}
			this.objectNull.Read(this, binaryHeaderEnum);
			this.objectNull.Dump();
			ObjectProgress objectProgress = (ObjectProgress)this.stack.Peek();
			this.prs.Init();
			this.prs.PRparseTypeEnum = InternalParseTypeE.Member;
			this.prs.PRmemberValueEnum = InternalMemberValueE.Null;
			if (objectProgress.objectTypeEnum == InternalObjectTypeE.Object)
			{
				this.prs.PRmemberTypeEnum = InternalMemberTypeE.Field;
				this.prs.PRname = objectProgress.name;
				this.prs.PRdtType = objectProgress.dtType;
			}
			else
			{
				this.prs.PRmemberTypeEnum = InternalMemberTypeE.Item;
				this.prs.PRnullCount = this.objectNull.nullCount;
				objectProgress.ArrayCountIncrement(this.objectNull.nullCount - 1);
			}
			this.objectReader.Parse(this.prs);
		}

		// Token: 0x0600537D RID: 21373 RVA: 0x00125C60 File Offset: 0x00123E60
		[SecurityCritical]
		private void ReadMessageEnd()
		{
			if (__BinaryParser.messageEnd == null)
			{
				__BinaryParser.messageEnd = new MessageEnd();
			}
			__BinaryParser.messageEnd.Read(this);
			__BinaryParser.messageEnd.Dump();
			if (!this.stack.IsEmpty())
			{
				throw new SerializationException(Environment.GetResourceString("Serialization_StreamEnd"));
			}
		}

		// Token: 0x0600537E RID: 21374 RVA: 0x00125CB8 File Offset: 0x00123EB8
		internal object ReadValue(InternalPrimitiveTypeE code)
		{
			switch (code)
			{
			case InternalPrimitiveTypeE.Boolean:
				return this.ReadBoolean();
			case InternalPrimitiveTypeE.Byte:
				return this.ReadByte();
			case InternalPrimitiveTypeE.Char:
				return this.ReadChar();
			case InternalPrimitiveTypeE.Decimal:
				return this.ReadDecimal();
			case InternalPrimitiveTypeE.Double:
				return this.ReadDouble();
			case InternalPrimitiveTypeE.Int16:
				return this.ReadInt16();
			case InternalPrimitiveTypeE.Int32:
				return this.ReadInt32();
			case InternalPrimitiveTypeE.Int64:
				return this.ReadInt64();
			case InternalPrimitiveTypeE.SByte:
				return this.ReadSByte();
			case InternalPrimitiveTypeE.Single:
				return this.ReadSingle();
			case InternalPrimitiveTypeE.TimeSpan:
				return this.ReadTimeSpan();
			case InternalPrimitiveTypeE.DateTime:
				return this.ReadDateTime();
			case InternalPrimitiveTypeE.UInt16:
				return this.ReadUInt16();
			case InternalPrimitiveTypeE.UInt32:
				return this.ReadUInt32();
			case InternalPrimitiveTypeE.UInt64:
				return this.ReadUInt64();
			}
			throw new SerializationException(Environment.GetResourceString("Serialization_TypeCode", new object[] { code.ToString() }));
		}

		// Token: 0x0600537F RID: 21375 RVA: 0x00125E24 File Offset: 0x00124024
		private ObjectProgress GetOp()
		{
			ObjectProgress objectProgress;
			if (this.opPool != null && !this.opPool.IsEmpty())
			{
				objectProgress = (ObjectProgress)this.opPool.Pop();
				objectProgress.Init();
			}
			else
			{
				objectProgress = new ObjectProgress();
			}
			return objectProgress;
		}

		// Token: 0x06005380 RID: 21376 RVA: 0x00125E68 File Offset: 0x00124068
		private void PutOp(ObjectProgress op)
		{
			if (this.opPool == null)
			{
				this.opPool = new SerStack("opPool");
			}
			this.opPool.Push(op);
		}

		// Token: 0x04002584 RID: 9604
		internal ObjectReader objectReader;

		// Token: 0x04002585 RID: 9605
		internal Stream input;

		// Token: 0x04002586 RID: 9606
		internal long topId;

		// Token: 0x04002587 RID: 9607
		internal long headerId;

		// Token: 0x04002588 RID: 9608
		internal SizedArray objectMapIdTable;

		// Token: 0x04002589 RID: 9609
		internal SizedArray assemIdToAssemblyTable;

		// Token: 0x0400258A RID: 9610
		internal SerStack stack = new SerStack("ObjectProgressStack");

		// Token: 0x0400258B RID: 9611
		internal BinaryTypeEnum expectedType = BinaryTypeEnum.ObjectUrt;

		// Token: 0x0400258C RID: 9612
		internal object expectedTypeInformation;

		// Token: 0x0400258D RID: 9613
		internal ParseRecord PRS;

		// Token: 0x0400258E RID: 9614
		private BinaryAssemblyInfo systemAssemblyInfo;

		// Token: 0x0400258F RID: 9615
		private BinaryReader dataReader;

		// Token: 0x04002590 RID: 9616
		private static Encoding encoding = new UTF8Encoding(false, true);

		// Token: 0x04002591 RID: 9617
		private SerStack opPool;

		// Token: 0x04002592 RID: 9618
		private BinaryObject binaryObject;

		// Token: 0x04002593 RID: 9619
		private BinaryObjectWithMap bowm;

		// Token: 0x04002594 RID: 9620
		private BinaryObjectWithMapTyped bowmt;

		// Token: 0x04002595 RID: 9621
		internal BinaryObjectString objectString;

		// Token: 0x04002596 RID: 9622
		internal BinaryCrossAppDomainString crossAppDomainString;

		// Token: 0x04002597 RID: 9623
		internal MemberPrimitiveTyped memberPrimitiveTyped;

		// Token: 0x04002598 RID: 9624
		private byte[] byteBuffer;

		// Token: 0x04002599 RID: 9625
		private const int chunkSize = 4096;

		// Token: 0x0400259A RID: 9626
		internal MemberPrimitiveUnTyped memberPrimitiveUnTyped;

		// Token: 0x0400259B RID: 9627
		internal MemberReference memberReference;

		// Token: 0x0400259C RID: 9628
		internal ObjectNull objectNull;

		// Token: 0x0400259D RID: 9629
		internal static volatile MessageEnd messageEnd;
	}
}
