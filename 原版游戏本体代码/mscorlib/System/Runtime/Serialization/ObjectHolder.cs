using System;
using System.Reflection;
using System.Security;

namespace System.Runtime.Serialization
{
	// Token: 0x0200074C RID: 1868
	internal sealed class ObjectHolder
	{
		// Token: 0x06005289 RID: 21129 RVA: 0x00122676 File Offset: 0x00120876
		internal ObjectHolder(long objID)
			: this(null, objID, null, null, 0L, null, null)
		{
		}

		// Token: 0x0600528A RID: 21130 RVA: 0x00122688 File Offset: 0x00120888
		internal ObjectHolder(object obj, long objID, SerializationInfo info, ISerializationSurrogate surrogate, long idOfContainingObj, FieldInfo field, int[] arrayIndex)
		{
			this.m_object = obj;
			this.m_id = objID;
			this.m_flags = 0;
			this.m_missingElementsRemaining = 0;
			this.m_missingDecendents = 0;
			this.m_dependentObjects = null;
			this.m_next = null;
			this.m_serInfo = info;
			this.m_surrogate = surrogate;
			this.m_markForFixupWhenAvailable = false;
			if (obj is TypeLoadExceptionHolder)
			{
				this.m_typeLoad = (TypeLoadExceptionHolder)obj;
			}
			if (idOfContainingObj != 0L && ((field != null && field.FieldType.IsValueType) || arrayIndex != null))
			{
				if (idOfContainingObj == objID)
				{
					throw new SerializationException(Environment.GetResourceString("Serialization_ParentChildIdentical"));
				}
				this.m_valueFixup = new ValueTypeFixupInfo(idOfContainingObj, field, arrayIndex);
			}
			this.SetFlags();
		}

		// Token: 0x0600528B RID: 21131 RVA: 0x00122744 File Offset: 0x00120944
		internal ObjectHolder(string obj, long objID, SerializationInfo info, ISerializationSurrogate surrogate, long idOfContainingObj, FieldInfo field, int[] arrayIndex)
		{
			this.m_object = obj;
			this.m_id = objID;
			this.m_flags = 0;
			this.m_missingElementsRemaining = 0;
			this.m_missingDecendents = 0;
			this.m_dependentObjects = null;
			this.m_next = null;
			this.m_serInfo = info;
			this.m_surrogate = surrogate;
			this.m_markForFixupWhenAvailable = false;
			if (idOfContainingObj != 0L && arrayIndex != null)
			{
				this.m_valueFixup = new ValueTypeFixupInfo(idOfContainingObj, field, arrayIndex);
			}
			if (this.m_valueFixup != null)
			{
				this.m_flags |= 8;
			}
		}

		// Token: 0x0600528C RID: 21132 RVA: 0x001227CD File Offset: 0x001209CD
		private void IncrementDescendentFixups(int amount)
		{
			this.m_missingDecendents += amount;
		}

		// Token: 0x0600528D RID: 21133 RVA: 0x001227DD File Offset: 0x001209DD
		internal void DecrementFixupsRemaining(ObjectManager manager)
		{
			this.m_missingElementsRemaining--;
			if (this.RequiresValueTypeFixup)
			{
				this.UpdateDescendentDependencyChain(-1, manager);
			}
		}

		// Token: 0x0600528E RID: 21134 RVA: 0x001227FD File Offset: 0x001209FD
		internal void RemoveDependency(long id)
		{
			this.m_dependentObjects.RemoveElement(id);
		}

		// Token: 0x0600528F RID: 21135 RVA: 0x0012280C File Offset: 0x00120A0C
		internal void AddFixup(FixupHolder fixup, ObjectManager manager)
		{
			if (this.m_missingElements == null)
			{
				this.m_missingElements = new FixupHolderList();
			}
			this.m_missingElements.Add(fixup);
			this.m_missingElementsRemaining++;
			if (this.RequiresValueTypeFixup)
			{
				this.UpdateDescendentDependencyChain(1, manager);
			}
		}

		// Token: 0x06005290 RID: 21136 RVA: 0x0012284C File Offset: 0x00120A4C
		private void UpdateDescendentDependencyChain(int amount, ObjectManager manager)
		{
			ObjectHolder objectHolder = this;
			do
			{
				objectHolder = manager.FindOrCreateObjectHolder(objectHolder.ContainerID);
				objectHolder.IncrementDescendentFixups(amount);
			}
			while (objectHolder.RequiresValueTypeFixup);
		}

		// Token: 0x06005291 RID: 21137 RVA: 0x00122877 File Offset: 0x00120A77
		internal void AddDependency(long dependentObject)
		{
			if (this.m_dependentObjects == null)
			{
				this.m_dependentObjects = new LongList();
			}
			this.m_dependentObjects.Add(dependentObject);
		}

		// Token: 0x06005292 RID: 21138 RVA: 0x00122898 File Offset: 0x00120A98
		[SecurityCritical]
		internal void UpdateData(object obj, SerializationInfo info, ISerializationSurrogate surrogate, long idOfContainer, FieldInfo field, int[] arrayIndex, ObjectManager manager)
		{
			this.SetObjectValue(obj, manager);
			this.m_serInfo = info;
			this.m_surrogate = surrogate;
			if (idOfContainer != 0L && ((field != null && field.FieldType.IsValueType) || arrayIndex != null))
			{
				if (idOfContainer == this.m_id)
				{
					throw new SerializationException(Environment.GetResourceString("Serialization_ParentChildIdentical"));
				}
				this.m_valueFixup = new ValueTypeFixupInfo(idOfContainer, field, arrayIndex);
			}
			this.SetFlags();
			if (this.RequiresValueTypeFixup)
			{
				this.UpdateDescendentDependencyChain(this.m_missingElementsRemaining, manager);
			}
		}

		// Token: 0x06005293 RID: 21139 RVA: 0x00122923 File Offset: 0x00120B23
		internal void MarkForCompletionWhenAvailable()
		{
			this.m_markForFixupWhenAvailable = true;
		}

		// Token: 0x06005294 RID: 21140 RVA: 0x0012292C File Offset: 0x00120B2C
		internal void SetFlags()
		{
			if (this.m_object is IObjectReference)
			{
				this.m_flags |= 1;
			}
			this.m_flags &= -7;
			if (this.m_surrogate != null)
			{
				this.m_flags |= 4;
			}
			else if (this.m_object is ISerializable)
			{
				this.m_flags |= 2;
			}
			if (this.m_valueFixup != null)
			{
				this.m_flags |= 8;
			}
		}

		// Token: 0x17000D96 RID: 3478
		// (get) Token: 0x06005295 RID: 21141 RVA: 0x001229AC File Offset: 0x00120BAC
		// (set) Token: 0x06005296 RID: 21142 RVA: 0x001229B9 File Offset: 0x00120BB9
		internal bool IsIncompleteObjectReference
		{
			get
			{
				return (this.m_flags & 1) != 0;
			}
			set
			{
				if (value)
				{
					this.m_flags |= 1;
					return;
				}
				this.m_flags &= -2;
			}
		}

		// Token: 0x17000D97 RID: 3479
		// (get) Token: 0x06005297 RID: 21143 RVA: 0x001229DC File Offset: 0x00120BDC
		internal bool RequiresDelayedFixup
		{
			get
			{
				return (this.m_flags & 7) != 0;
			}
		}

		// Token: 0x17000D98 RID: 3480
		// (get) Token: 0x06005298 RID: 21144 RVA: 0x001229E9 File Offset: 0x00120BE9
		internal bool RequiresValueTypeFixup
		{
			get
			{
				return (this.m_flags & 8) != 0;
			}
		}

		// Token: 0x17000D99 RID: 3481
		// (get) Token: 0x06005299 RID: 21145 RVA: 0x001229F6 File Offset: 0x00120BF6
		// (set) Token: 0x0600529A RID: 21146 RVA: 0x00122A2A File Offset: 0x00120C2A
		internal bool ValueTypeFixupPerformed
		{
			get
			{
				return (this.m_flags & 32768) != 0 || (this.m_object != null && (this.m_dependentObjects == null || this.m_dependentObjects.Count == 0));
			}
			set
			{
				if (value)
				{
					this.m_flags |= 32768;
				}
			}
		}

		// Token: 0x17000D9A RID: 3482
		// (get) Token: 0x0600529B RID: 21147 RVA: 0x00122A41 File Offset: 0x00120C41
		internal bool HasISerializable
		{
			get
			{
				return (this.m_flags & 2) != 0;
			}
		}

		// Token: 0x17000D9B RID: 3483
		// (get) Token: 0x0600529C RID: 21148 RVA: 0x00122A4E File Offset: 0x00120C4E
		internal bool HasSurrogate
		{
			get
			{
				return (this.m_flags & 4) != 0;
			}
		}

		// Token: 0x17000D9C RID: 3484
		// (get) Token: 0x0600529D RID: 21149 RVA: 0x00122A5B File Offset: 0x00120C5B
		internal bool CanSurrogatedObjectValueChange
		{
			get
			{
				return this.m_surrogate == null || this.m_surrogate.GetType() != typeof(SurrogateForCyclicalReference);
			}
		}

		// Token: 0x17000D9D RID: 3485
		// (get) Token: 0x0600529E RID: 21150 RVA: 0x00122A81 File Offset: 0x00120C81
		internal bool CanObjectValueChange
		{
			get
			{
				return this.IsIncompleteObjectReference || (this.HasSurrogate && this.CanSurrogatedObjectValueChange);
			}
		}

		// Token: 0x17000D9E RID: 3486
		// (get) Token: 0x0600529F RID: 21151 RVA: 0x00122A9D File Offset: 0x00120C9D
		internal int DirectlyDependentObjects
		{
			get
			{
				return this.m_missingElementsRemaining;
			}
		}

		// Token: 0x17000D9F RID: 3487
		// (get) Token: 0x060052A0 RID: 21152 RVA: 0x00122AA5 File Offset: 0x00120CA5
		internal int TotalDependentObjects
		{
			get
			{
				return this.m_missingElementsRemaining + this.m_missingDecendents;
			}
		}

		// Token: 0x17000DA0 RID: 3488
		// (get) Token: 0x060052A1 RID: 21153 RVA: 0x00122AB4 File Offset: 0x00120CB4
		// (set) Token: 0x060052A2 RID: 21154 RVA: 0x00122ABC File Offset: 0x00120CBC
		internal bool Reachable
		{
			get
			{
				return this.m_reachable;
			}
			set
			{
				this.m_reachable = value;
			}
		}

		// Token: 0x17000DA1 RID: 3489
		// (get) Token: 0x060052A3 RID: 21155 RVA: 0x00122AC5 File Offset: 0x00120CC5
		internal bool TypeLoadExceptionReachable
		{
			get
			{
				return this.m_typeLoad != null;
			}
		}

		// Token: 0x17000DA2 RID: 3490
		// (get) Token: 0x060052A4 RID: 21156 RVA: 0x00122AD0 File Offset: 0x00120CD0
		// (set) Token: 0x060052A5 RID: 21157 RVA: 0x00122AD8 File Offset: 0x00120CD8
		internal TypeLoadExceptionHolder TypeLoadException
		{
			get
			{
				return this.m_typeLoad;
			}
			set
			{
				this.m_typeLoad = value;
			}
		}

		// Token: 0x17000DA3 RID: 3491
		// (get) Token: 0x060052A6 RID: 21158 RVA: 0x00122AE1 File Offset: 0x00120CE1
		internal object ObjectValue
		{
			get
			{
				return this.m_object;
			}
		}

		// Token: 0x060052A7 RID: 21159 RVA: 0x00122AE9 File Offset: 0x00120CE9
		[SecurityCritical]
		internal void SetObjectValue(object obj, ObjectManager manager)
		{
			this.m_object = obj;
			if (obj == manager.TopObject)
			{
				this.m_reachable = true;
			}
			if (obj is TypeLoadExceptionHolder)
			{
				this.m_typeLoad = (TypeLoadExceptionHolder)obj;
			}
			if (this.m_markForFixupWhenAvailable)
			{
				manager.CompleteObject(this, true);
			}
		}

		// Token: 0x17000DA4 RID: 3492
		// (get) Token: 0x060052A8 RID: 21160 RVA: 0x00122B26 File Offset: 0x00120D26
		// (set) Token: 0x060052A9 RID: 21161 RVA: 0x00122B2E File Offset: 0x00120D2E
		internal SerializationInfo SerializationInfo
		{
			get
			{
				return this.m_serInfo;
			}
			set
			{
				this.m_serInfo = value;
			}
		}

		// Token: 0x17000DA5 RID: 3493
		// (get) Token: 0x060052AA RID: 21162 RVA: 0x00122B37 File Offset: 0x00120D37
		internal ISerializationSurrogate Surrogate
		{
			get
			{
				return this.m_surrogate;
			}
		}

		// Token: 0x17000DA6 RID: 3494
		// (get) Token: 0x060052AB RID: 21163 RVA: 0x00122B3F File Offset: 0x00120D3F
		// (set) Token: 0x060052AC RID: 21164 RVA: 0x00122B47 File Offset: 0x00120D47
		internal LongList DependentObjects
		{
			get
			{
				return this.m_dependentObjects;
			}
			set
			{
				this.m_dependentObjects = value;
			}
		}

		// Token: 0x17000DA7 RID: 3495
		// (get) Token: 0x060052AD RID: 21165 RVA: 0x00122B50 File Offset: 0x00120D50
		// (set) Token: 0x060052AE RID: 21166 RVA: 0x00122B77 File Offset: 0x00120D77
		internal bool RequiresSerInfoFixup
		{
			get
			{
				return ((this.m_flags & 4) != 0 || (this.m_flags & 2) != 0) && (this.m_flags & 16384) == 0;
			}
			set
			{
				if (!value)
				{
					this.m_flags |= 16384;
					return;
				}
				this.m_flags &= -16385;
			}
		}

		// Token: 0x17000DA8 RID: 3496
		// (get) Token: 0x060052AF RID: 21167 RVA: 0x00122BA1 File Offset: 0x00120DA1
		internal ValueTypeFixupInfo ValueFixup
		{
			get
			{
				return this.m_valueFixup;
			}
		}

		// Token: 0x17000DA9 RID: 3497
		// (get) Token: 0x060052B0 RID: 21168 RVA: 0x00122BA9 File Offset: 0x00120DA9
		internal bool CompletelyFixed
		{
			get
			{
				return !this.RequiresSerInfoFixup && !this.IsIncompleteObjectReference;
			}
		}

		// Token: 0x17000DAA RID: 3498
		// (get) Token: 0x060052B1 RID: 21169 RVA: 0x00122BBE File Offset: 0x00120DBE
		internal long ContainerID
		{
			get
			{
				if (this.m_valueFixup != null)
				{
					return this.m_valueFixup.ContainerID;
				}
				return 0L;
			}
		}

		// Token: 0x0400248B RID: 9355
		internal const int INCOMPLETE_OBJECT_REFERENCE = 1;

		// Token: 0x0400248C RID: 9356
		internal const int HAS_ISERIALIZABLE = 2;

		// Token: 0x0400248D RID: 9357
		internal const int HAS_SURROGATE = 4;

		// Token: 0x0400248E RID: 9358
		internal const int REQUIRES_VALUETYPE_FIXUP = 8;

		// Token: 0x0400248F RID: 9359
		internal const int REQUIRES_DELAYED_FIXUP = 7;

		// Token: 0x04002490 RID: 9360
		internal const int SER_INFO_FIXED = 16384;

		// Token: 0x04002491 RID: 9361
		internal const int VALUETYPE_FIXUP_PERFORMED = 32768;

		// Token: 0x04002492 RID: 9362
		private object m_object;

		// Token: 0x04002493 RID: 9363
		internal long m_id;

		// Token: 0x04002494 RID: 9364
		private int m_missingElementsRemaining;

		// Token: 0x04002495 RID: 9365
		private int m_missingDecendents;

		// Token: 0x04002496 RID: 9366
		internal SerializationInfo m_serInfo;

		// Token: 0x04002497 RID: 9367
		internal ISerializationSurrogate m_surrogate;

		// Token: 0x04002498 RID: 9368
		internal FixupHolderList m_missingElements;

		// Token: 0x04002499 RID: 9369
		internal LongList m_dependentObjects;

		// Token: 0x0400249A RID: 9370
		internal ObjectHolder m_next;

		// Token: 0x0400249B RID: 9371
		internal int m_flags;

		// Token: 0x0400249C RID: 9372
		private bool m_markForFixupWhenAvailable;

		// Token: 0x0400249D RID: 9373
		private ValueTypeFixupInfo m_valueFixup;

		// Token: 0x0400249E RID: 9374
		private TypeLoadExceptionHolder m_typeLoad;

		// Token: 0x0400249F RID: 9375
		private bool m_reachable;
	}
}
