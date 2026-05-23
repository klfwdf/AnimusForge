using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Iced.Intel
{
	// Token: 0x02000653 RID: 1619
	[NullableContext(1)]
	[Nullable(0)]
	[DebuggerDisplay("Count = {Count}")]
	[DebuggerTypeProxy(typeof(InstructionListDebugView))]
	[EditorBrowsable(EditorBrowsableState.Never)]
	internal sealed class InstructionList : IList<Instruction>, ICollection<Instruction>, IEnumerable<Instruction>, IEnumerable, IReadOnlyList<Instruction>, IReadOnlyCollection<Instruction>, IList, ICollection
	{
		// Token: 0x170007DF RID: 2015
		// (get) Token: 0x06002310 RID: 8976 RVA: 0x00071B87 File Offset: 0x0006FD87
		public int Count
		{
			get
			{
				return this.count;
			}
		}

		// Token: 0x170007E0 RID: 2016
		// (get) Token: 0x06002311 RID: 8977 RVA: 0x00071B87 File Offset: 0x0006FD87
		int ICollection<Instruction>.Count
		{
			get
			{
				return this.count;
			}
		}

		// Token: 0x170007E1 RID: 2017
		// (get) Token: 0x06002312 RID: 8978 RVA: 0x00071B87 File Offset: 0x0006FD87
		int ICollection.Count
		{
			get
			{
				return this.count;
			}
		}

		// Token: 0x170007E2 RID: 2018
		// (get) Token: 0x06002313 RID: 8979 RVA: 0x00071B87 File Offset: 0x0006FD87
		int IReadOnlyCollection<Instruction>.Count
		{
			get
			{
				return this.count;
			}
		}

		// Token: 0x170007E3 RID: 2019
		// (get) Token: 0x06002314 RID: 8980 RVA: 0x00071B8F File Offset: 0x0006FD8F
		public int Capacity
		{
			get
			{
				return this.elements.Length;
			}
		}

		// Token: 0x170007E4 RID: 2020
		// (get) Token: 0x06002315 RID: 8981 RVA: 0x0001B69F File Offset: 0x0001989F
		bool ICollection<Instruction>.IsReadOnly
		{
			get
			{
				return false;
			}
		}

		// Token: 0x170007E5 RID: 2021
		// (get) Token: 0x06002316 RID: 8982 RVA: 0x0001B69F File Offset: 0x0001989F
		bool IList.IsReadOnly
		{
			get
			{
				return false;
			}
		}

		// Token: 0x170007E6 RID: 2022
		// (get) Token: 0x06002317 RID: 8983 RVA: 0x0001B69F File Offset: 0x0001989F
		bool IList.IsFixedSize
		{
			get
			{
				return false;
			}
		}

		// Token: 0x170007E7 RID: 2023
		// (get) Token: 0x06002318 RID: 8984 RVA: 0x0001B69F File Offset: 0x0001989F
		bool ICollection.IsSynchronized
		{
			get
			{
				return false;
			}
		}

		// Token: 0x170007E8 RID: 2024
		// (get) Token: 0x06002319 RID: 8985 RVA: 0x0001B6A2 File Offset: 0x000198A2
		object ICollection.SyncRoot
		{
			get
			{
				return this;
			}
		}

		// Token: 0x170007E9 RID: 2025
		public Instruction this[int index]
		{
			get
			{
				return ref this.elements[index];
			}
		}

		// Token: 0x170007EA RID: 2026
		Instruction IList<Instruction>.this[int index]
		{
			get
			{
				return this.elements[index];
			}
			set
			{
				this.elements[index] = value;
			}
		}

		// Token: 0x170007EB RID: 2027
		Instruction IReadOnlyList<Instruction>.this[int index]
		{
			get
			{
				return this.elements[index];
			}
		}

		// Token: 0x170007EC RID: 2028
		[Nullable(2)]
		object IList.this[int index]
		{
			get
			{
				return this.elements[index];
			}
			set
			{
				if (value == null)
				{
					ThrowHelper.ThrowArgumentNullException_value();
				}
				if (!(value is Instruction))
				{
					ThrowHelper.ThrowArgumentException();
				}
				this.elements[index] = (Instruction)value;
			}
		}

		// Token: 0x06002320 RID: 8992 RVA: 0x00071C00 File Offset: 0x0006FE00
		public InstructionList()
		{
			this.elements = Array2.Empty<Instruction>();
		}

		// Token: 0x06002321 RID: 8993 RVA: 0x00071C13 File Offset: 0x0006FE13
		public InstructionList(int capacity)
		{
			if (capacity < 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_capacity();
			}
			this.elements = ((capacity == 0) ? Array2.Empty<Instruction>() : new Instruction[capacity]);
		}

		// Token: 0x06002322 RID: 8994 RVA: 0x00071C3C File Offset: 0x0006FE3C
		public InstructionList(InstructionList list)
		{
			if (list == null)
			{
				ThrowHelper.ThrowArgumentNullException_list();
			}
			int length = list.count;
			if (length == 0)
			{
				this.elements = Array2.Empty<Instruction>();
				return;
			}
			Instruction[] elements = new Instruction[length];
			this.elements = elements;
			this.count = length;
			Array.Copy(list.elements, 0, elements, 0, length);
		}

		// Token: 0x06002323 RID: 8995 RVA: 0x00071C94 File Offset: 0x0006FE94
		public InstructionList(IEnumerable<Instruction> collection)
		{
			if (collection == null)
			{
				ThrowHelper.ThrowArgumentNullException_collection();
			}
			ICollection<Instruction> coll = collection as ICollection<Instruction>;
			if (coll == null)
			{
				this.elements = Array2.Empty<Instruction>();
				foreach (Instruction elem in collection)
				{
					this.Add(elem);
				}
				return;
			}
			int count = coll.Count;
			if (count == 0)
			{
				this.elements = Array2.Empty<Instruction>();
				return;
			}
			Instruction[] elements = new Instruction[count];
			this.elements = elements;
			coll.CopyTo(elements, 0);
			this.count = count;
		}

		// Token: 0x06002324 RID: 8996 RVA: 0x00071D38 File Offset: 0x0006FF38
		private void SetMinCapacity(int minCapacity)
		{
			Instruction[] elements = this.elements;
			uint capacity = (uint)elements.Length;
			if (minCapacity <= (int)capacity)
			{
				return;
			}
			uint newCapacity = capacity * 2U;
			if (newCapacity < 4U)
			{
				newCapacity = 4U;
			}
			if (newCapacity < (uint)minCapacity)
			{
				newCapacity = (uint)minCapacity;
			}
			if (newCapacity > 2146435071U)
			{
				newCapacity = 2146435071U;
			}
			Instruction[] newElements = new Instruction[newCapacity];
			Array.Copy(elements, 0, newElements, 0, this.count);
			this.elements = newElements;
		}

		// Token: 0x06002325 RID: 8997 RVA: 0x00071D90 File Offset: 0x0006FF90
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ref Instruction AllocUninitializedElement()
		{
			int count = this.count;
			Instruction[] elements = this.elements;
			if (count == elements.Length)
			{
				this.SetMinCapacity(count + 1);
				elements = this.elements;
			}
			this.count = count + 1;
			return ref elements[count];
		}

		// Token: 0x06002326 RID: 8998 RVA: 0x00071DD4 File Offset: 0x0006FFD4
		private void MakeRoom(int index, int extraLength)
		{
			this.SetMinCapacity(this.count + extraLength);
			int copyCount = this.count - index;
			if (copyCount != 0)
			{
				Instruction[] elements = this.elements;
				Array.Copy(elements, index, elements, index + extraLength, copyCount);
			}
		}

		// Token: 0x06002327 RID: 8999 RVA: 0x00071E10 File Offset: 0x00070010
		public void Insert(int index, in Instruction instruction)
		{
			int count = this.count;
			if (index > count)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_index();
			}
			this.MakeRoom(index, 1);
			this.elements[index] = instruction;
			this.count = count + 1;
		}

		// Token: 0x06002328 RID: 9000 RVA: 0x00071E50 File Offset: 0x00070050
		void IList<Instruction>.Insert(int index, Instruction instruction)
		{
			this.Insert(index, instruction);
		}

		// Token: 0x06002329 RID: 9001 RVA: 0x00071E5C File Offset: 0x0007005C
		void IList.Insert(int index, object value)
		{
			if (value == null)
			{
				ThrowHelper.ThrowArgumentNullException_value();
			}
			if (!(value is Instruction))
			{
				ThrowHelper.ThrowArgumentException();
			}
			Instruction instruction = (Instruction)value;
			this.Insert(index, instruction);
		}

		// Token: 0x0600232A RID: 9002 RVA: 0x00071E90 File Offset: 0x00070090
		public void RemoveAt(int index)
		{
			int newCount = this.count;
			if (index >= newCount)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_index();
			}
			newCount--;
			this.count = newCount;
			int copyCount = newCount - index;
			if (copyCount != 0)
			{
				Instruction[] elements = this.elements;
				Array.Copy(elements, index + 1, elements, index, copyCount);
			}
		}

		// Token: 0x0600232B RID: 9003 RVA: 0x00071ED2 File Offset: 0x000700D2
		void IList<Instruction>.RemoveAt(int index)
		{
			this.RemoveAt(index);
		}

		// Token: 0x0600232C RID: 9004 RVA: 0x00071ED2 File Offset: 0x000700D2
		void IList.RemoveAt(int index)
		{
			this.RemoveAt(index);
		}

		// Token: 0x0600232D RID: 9005 RVA: 0x00071EDB File Offset: 0x000700DB
		public void AddRange(IEnumerable<Instruction> collection)
		{
			this.InsertRange(this.count, collection);
		}

		// Token: 0x0600232E RID: 9006 RVA: 0x00071EEC File Offset: 0x000700EC
		public void InsertRange(int index, IEnumerable<Instruction> collection)
		{
			if (index > this.count)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_index();
			}
			if (collection == null)
			{
				ThrowHelper.ThrowArgumentNullException_collection();
			}
			InstructionList list = collection as InstructionList;
			if (list != null)
			{
				int list_count = list.count;
				if (list_count != 0)
				{
					this.MakeRoom(index, list_count);
					this.count += list_count;
					Array.Copy(list.elements, 0, this.elements, index, list_count);
					return;
				}
			}
			else
			{
				IList<Instruction> ilist = collection as IList<Instruction>;
				if (ilist != null)
				{
					int ilist_Count = ilist.Count;
					if (ilist_Count != 0)
					{
						this.MakeRoom(index, ilist_Count);
						this.count += ilist_Count;
						Instruction[] elements = this.elements;
						for (int i = 0; i < ilist_Count; i++)
						{
							elements[index + i] = ilist[i];
						}
						return;
					}
				}
				else
				{
					IReadOnlyList<Instruction> roList = collection as IReadOnlyList<Instruction>;
					if (roList != null)
					{
						int roList_Count = roList.Count;
						if (roList_Count != 0)
						{
							this.MakeRoom(index, roList_Count);
							this.count += roList_Count;
							Instruction[] elements2 = this.elements;
							for (int j = 0; j < roList_Count; j++)
							{
								elements2[index + j] = roList[j];
							}
							return;
						}
					}
					else
					{
						foreach (Instruction instruction in collection)
						{
							this.Insert(index++, instruction);
						}
					}
				}
			}
		}

		// Token: 0x0600232F RID: 9007 RVA: 0x00072054 File Offset: 0x00070254
		public void RemoveRange(int index, int count)
		{
			if (index < 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_index();
			}
			if (count < 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_count();
			}
			if (index + count > this.count)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_count();
			}
			int newCount = this.count;
			newCount -= count;
			this.count = newCount;
			int copyCount = newCount - index;
			if (copyCount != 0)
			{
				Instruction[] elements = this.elements;
				Array.Copy(elements, index + count, elements, index, copyCount);
			}
		}

		// Token: 0x06002330 RID: 9008 RVA: 0x000720B0 File Offset: 0x000702B0
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Add(in Instruction instruction)
		{
			int count = this.count;
			Instruction[] elements = this.elements;
			if (count == elements.Length)
			{
				this.SetMinCapacity(count + 1);
				elements = this.elements;
			}
			elements[count] = instruction;
			this.count = count + 1;
		}

		// Token: 0x06002331 RID: 9009 RVA: 0x000720F7 File Offset: 0x000702F7
		void ICollection<Instruction>.Add(Instruction instruction)
		{
			this.Add(instruction);
		}

		// Token: 0x06002332 RID: 9010 RVA: 0x00072104 File Offset: 0x00070304
		int IList.Add(object value)
		{
			if (value == null)
			{
				ThrowHelper.ThrowArgumentNullException_value();
			}
			if (!(value is Instruction))
			{
				ThrowHelper.ThrowArgumentException();
			}
			Instruction instruction = (Instruction)value;
			this.Add(instruction);
			return this.count - 1;
		}

		// Token: 0x06002333 RID: 9011 RVA: 0x0007213D File Offset: 0x0007033D
		public void Clear()
		{
			this.count = 0;
		}

		// Token: 0x06002334 RID: 9012 RVA: 0x00072146 File Offset: 0x00070346
		void ICollection<Instruction>.Clear()
		{
			this.Clear();
		}

		// Token: 0x06002335 RID: 9013 RVA: 0x00072146 File Offset: 0x00070346
		void IList.Clear()
		{
			this.Clear();
		}

		// Token: 0x06002336 RID: 9014 RVA: 0x0007214E File Offset: 0x0007034E
		public bool Contains(in Instruction instruction)
		{
			return this.IndexOf(instruction) >= 0;
		}

		// Token: 0x06002337 RID: 9015 RVA: 0x0007215D File Offset: 0x0007035D
		bool ICollection<Instruction>.Contains(Instruction instruction)
		{
			return this.Contains(instruction);
		}

		// Token: 0x06002338 RID: 9016 RVA: 0x00072168 File Offset: 0x00070368
		bool IList.Contains(object value)
		{
			if (value is Instruction)
			{
				Instruction instruction = (Instruction)value;
				return this.Contains(instruction);
			}
			return false;
		}

		// Token: 0x06002339 RID: 9017 RVA: 0x00072190 File Offset: 0x00070390
		public int IndexOf(in Instruction instruction)
		{
			Instruction[] elements = this.elements;
			int count = this.count;
			for (int i = 0; i < count; i++)
			{
				if ((elements[i]) == (instruction))
				{
					return i;
				}
			}
			return -1;
		}

		// Token: 0x0600233A RID: 9018 RVA: 0x000721C9 File Offset: 0x000703C9
		int IList<Instruction>.IndexOf(Instruction instruction)
		{
			return this.IndexOf(instruction);
		}

		// Token: 0x0600233B RID: 9019 RVA: 0x000721D4 File Offset: 0x000703D4
		int IList.IndexOf(object value)
		{
			if (value is Instruction)
			{
				Instruction instruction = (Instruction)value;
				return this.IndexOf(instruction);
			}
			return -1;
		}

		// Token: 0x0600233C RID: 9020 RVA: 0x000721FC File Offset: 0x000703FC
		public int IndexOf(in Instruction instruction, int index)
		{
			int count = this.count;
			if (index > count)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_index();
			}
			Instruction[] elements = this.elements;
			for (int i = index; i < count; i++)
			{
				if ((elements[i]) == (instruction))
				{
					return i;
				}
			}
			return -1;
		}

		// Token: 0x0600233D RID: 9021 RVA: 0x00072240 File Offset: 0x00070440
		public int IndexOf(in Instruction instruction, int index, int count)
		{
			if (index < 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_index();
			}
			if (count < 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_count();
			}
			int end = index + count;
			if (end > this.count)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_count();
			}
			Instruction[] elements = this.elements;
			for (int i = index; i < end; i++)
			{
				if ((elements[i]) == (instruction))
				{
					return i;
				}
			}
			return -1;
		}

		// Token: 0x0600233E RID: 9022 RVA: 0x00072298 File Offset: 0x00070498
		public int LastIndexOf(in Instruction instruction)
		{
			for (int i = this.count - 1; i >= 0; i--)
			{
				if ((this.elements[i]) == (instruction))
				{
					return i;
				}
			}
			return -1;
		}

		// Token: 0x0600233F RID: 9023 RVA: 0x000722D0 File Offset: 0x000704D0
		public int LastIndexOf(in Instruction instruction, int index)
		{
			int count = this.count;
			if (index > count)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_index();
			}
			Instruction[] elements = this.elements;
			for (int i = count - 1; i >= index; i--)
			{
				if ((elements[i]) == (instruction))
				{
					return i;
				}
			}
			return -1;
		}

		// Token: 0x06002340 RID: 9024 RVA: 0x00072314 File Offset: 0x00070514
		public int LastIndexOf(in Instruction instruction, int index, int count)
		{
			if (index < 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_index();
			}
			if (count < 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_count();
			}
			int num = index + count;
			if (num > this.count)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_count();
			}
			Instruction[] elements = this.elements;
			for (int i = num - 1; i >= index; i--)
			{
				if ((elements[i]) == (instruction))
				{
					return i;
				}
			}
			return -1;
		}

		// Token: 0x06002341 RID: 9025 RVA: 0x0007236C File Offset: 0x0007056C
		public bool Remove(in Instruction instruction)
		{
			int index = this.IndexOf(instruction);
			if (index >= 0)
			{
				this.RemoveAt(index);
			}
			return index >= 0;
		}

		// Token: 0x06002342 RID: 9026 RVA: 0x00072393 File Offset: 0x00070593
		bool ICollection<Instruction>.Remove(Instruction instruction)
		{
			return this.Remove(instruction);
		}

		// Token: 0x06002343 RID: 9027 RVA: 0x000723A0 File Offset: 0x000705A0
		void IList.Remove(object value)
		{
			if (value is Instruction)
			{
				Instruction instruction = (Instruction)value;
				this.Remove(instruction);
			}
		}

		// Token: 0x06002344 RID: 9028 RVA: 0x000723C5 File Offset: 0x000705C5
		public void CopyTo(Instruction[] array)
		{
			this.CopyTo(array, 0);
		}

		// Token: 0x06002345 RID: 9029 RVA: 0x000723CF File Offset: 0x000705CF
		public void CopyTo(Instruction[] array, int arrayIndex)
		{
			Array.Copy(this.elements, 0, array, arrayIndex, this.count);
		}

		// Token: 0x06002346 RID: 9030 RVA: 0x000723E5 File Offset: 0x000705E5
		void ICollection<Instruction>.CopyTo(Instruction[] array, int arrayIndex)
		{
			this.CopyTo(array, arrayIndex);
		}

		// Token: 0x06002347 RID: 9031 RVA: 0x000723F0 File Offset: 0x000705F0
		void ICollection.CopyTo(Array array, int index)
		{
			if (array == null)
			{
				ThrowHelper.ThrowArgumentNullException_array();
				return;
			}
			Instruction[] elemArray = array as Instruction[];
			if (elemArray != null)
			{
				this.CopyTo(elemArray, index);
				return;
			}
			ThrowHelper.ThrowArgumentException();
		}

		// Token: 0x06002348 RID: 9032 RVA: 0x0007241E File Offset: 0x0007061E
		public void CopyTo(int index, Instruction[] array, int arrayIndex, int count)
		{
			Array.Copy(this.elements, index, array, arrayIndex, count);
		}

		// Token: 0x06002349 RID: 9033 RVA: 0x00072430 File Offset: 0x00070630
		public InstructionList GetRange(int index, int count)
		{
			if (index < 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_index();
			}
			if (count < 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_count();
			}
			if (index + count > this.count)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_count();
			}
			InstructionList list = new InstructionList(count);
			Array.Copy(this.elements, index, list.elements, 0, count);
			list.count = count;
			return list;
		}

		// Token: 0x0600234A RID: 9034 RVA: 0x00072482 File Offset: 0x00070682
		public InstructionList.Enumerator GetEnumerator()
		{
			return new InstructionList.Enumerator(this);
		}

		// Token: 0x0600234B RID: 9035 RVA: 0x0007248A File Offset: 0x0007068A
		IEnumerator<Instruction> IEnumerable<Instruction>.GetEnumerator()
		{
			return new InstructionList.Enumerator(this);
		}

		// Token: 0x0600234C RID: 9036 RVA: 0x0007248A File Offset: 0x0007068A
		IEnumerator IEnumerable.GetEnumerator()
		{
			return new InstructionList.Enumerator(this);
		}

		// Token: 0x0600234D RID: 9037 RVA: 0x00072497 File Offset: 0x00070697
		public ReadOnlyCollection<Instruction> AsReadOnly()
		{
			return new ReadOnlyCollection<Instruction>(this);
		}

		// Token: 0x0600234E RID: 9038 RVA: 0x000724A0 File Offset: 0x000706A0
		public Instruction[] ToArray()
		{
			int count = this.count;
			if (count == 0)
			{
				return Array2.Empty<Instruction>();
			}
			Instruction[] res = new Instruction[count];
			Array.Copy(this.elements, 0, res, 0, res.Length);
			return res;
		}

		// Token: 0x04002AF0 RID: 10992
		private Instruction[] elements;

		// Token: 0x04002AF1 RID: 10993
		private int count;

		// Token: 0x02000654 RID: 1620
		[NullableContext(0)]
		public struct Enumerator : IEnumerator<Instruction>, IDisposable, IEnumerator
		{
			// Token: 0x170007ED RID: 2029
			// (get) Token: 0x0600234F RID: 9039 RVA: 0x000724D6 File Offset: 0x000706D6
			public ref Instruction Current
			{
				get
				{
					return ref this.list.elements[this.index];
				}
			}

			// Token: 0x170007EE RID: 2030
			// (get) Token: 0x06002350 RID: 9040 RVA: 0x000724EE File Offset: 0x000706EE
			Instruction IEnumerator<Instruction>.Current
			{
				get
				{
					return this.list.elements[this.index];
				}
			}

			// Token: 0x170007EF RID: 2031
			// (get) Token: 0x06002351 RID: 9041 RVA: 0x00072506 File Offset: 0x00070706
			[Nullable(1)]
			object IEnumerator.Current
			{
				get
				{
					return this.list.elements[this.index];
				}
			}

			// Token: 0x06002352 RID: 9042 RVA: 0x00072523 File Offset: 0x00070723
			[NullableContext(1)]
			internal Enumerator(InstructionList list)
			{
				this.list = list;
				this.index = -1;
			}

			// Token: 0x06002353 RID: 9043 RVA: 0x00072533 File Offset: 0x00070733
			public bool MoveNext()
			{
				this.index++;
				return this.index < this.list.count;
			}

			// Token: 0x06002354 RID: 9044 RVA: 0x00003BBE File Offset: 0x00001DBE
			void IEnumerator.Reset()
			{
				throw new NotSupportedException();
			}

			// Token: 0x06002355 RID: 9045 RVA: 0x0001B842 File Offset: 0x00019A42
			public void Dispose()
			{
			}

			// Token: 0x04002AF2 RID: 10994
			private readonly InstructionList list;

			// Token: 0x04002AF3 RID: 10995
			private int index;
		}
	}
}
