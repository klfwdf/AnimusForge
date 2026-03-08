using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json.Linq
{
	/// <summary>
	/// Represents a token that can contain other tokens.
	/// </summary>
	// Token: 0x020000BC RID: 188
	[NullableContext(1)]
	[Nullable(0)]
	public abstract class JContainer : JToken, IList<JToken>, ICollection<JToken>, IEnumerable<JToken>, IEnumerable, ITypedList, IBindingList, IList, ICollection, INotifyCollectionChanged
	{
		// Token: 0x060009C8 RID: 2504 RVA: 0x00028B58 File Offset: 0x00026D58
		internal async Task ReadTokenFromAsync(JsonReader reader, [Nullable(2)] JsonLoadSettings options, CancellationToken cancellationToken = default(CancellationToken))
		{
			ValidationUtils.ArgumentNotNull(reader, "reader");
			int startDepth = reader.Depth;
			ConfiguredTaskAwaitable<bool>.ConfiguredTaskAwaiter configuredTaskAwaiter = reader.ReadAsync(cancellationToken).ConfigureAwait(false).GetAwaiter();
			if (!configuredTaskAwaiter.IsCompleted)
			{
				await configuredTaskAwaiter;
				ConfiguredTaskAwaitable<bool>.ConfiguredTaskAwaiter configuredTaskAwaiter2;
				configuredTaskAwaiter = configuredTaskAwaiter2;
				configuredTaskAwaiter2 = default(ConfiguredTaskAwaitable<bool>.ConfiguredTaskAwaiter);
			}
			if (!configuredTaskAwaiter.GetResult())
			{
				throw JsonReaderException.Create(reader, "Error reading {0} from JsonReader.".FormatWith(CultureInfo.InvariantCulture, base.GetType().Name));
			}
			await this.ReadContentFromAsync(reader, options, cancellationToken).ConfigureAwait(false);
			if (reader.Depth > startDepth)
			{
				throw JsonReaderException.Create(reader, "Unexpected end of content while loading {0}.".FormatWith(CultureInfo.InvariantCulture, base.GetType().Name));
			}
		}

		// Token: 0x060009C9 RID: 2505 RVA: 0x00028BB4 File Offset: 0x00026DB4
		private Task ReadContentFromAsync(JsonReader reader, [Nullable(2)] JsonLoadSettings settings, CancellationToken cancellationToken = default(CancellationToken))
		{
			JContainer.<ReadContentFromAsync>d__1 <ReadContentFromAsync>d__;
			<ReadContentFromAsync>d__.<>t__builder = AsyncTaskMethodBuilder.Create();
			<ReadContentFromAsync>d__.<>4__this = this;
			<ReadContentFromAsync>d__.reader = reader;
			<ReadContentFromAsync>d__.settings = settings;
			<ReadContentFromAsync>d__.cancellationToken = cancellationToken;
			<ReadContentFromAsync>d__.<>1__state = -1;
			<ReadContentFromAsync>d__.<>t__builder.Start<JContainer.<ReadContentFromAsync>d__1>(ref <ReadContentFromAsync>d__);
			return <ReadContentFromAsync>d__.<>t__builder.Task;
		}

		/// <summary>
		/// Occurs when the list changes or an item in the list changes.
		/// </summary>
		// Token: 0x14000004 RID: 4
		// (add) Token: 0x060009CA RID: 2506 RVA: 0x00028C0F File Offset: 0x00026E0F
		// (remove) Token: 0x060009CB RID: 2507 RVA: 0x00028C28 File Offset: 0x00026E28
		public event ListChangedEventHandler ListChanged
		{
			add
			{
				this._listChanged = (ListChangedEventHandler)Delegate.Combine(this._listChanged, value);
			}
			remove
			{
				this._listChanged = (ListChangedEventHandler)Delegate.Remove(this._listChanged, value);
			}
		}

		/// <summary>
		/// Occurs before an item is added to the collection.
		/// </summary>
		// Token: 0x14000005 RID: 5
		// (add) Token: 0x060009CC RID: 2508 RVA: 0x00028C41 File Offset: 0x00026E41
		// (remove) Token: 0x060009CD RID: 2509 RVA: 0x00028C5A File Offset: 0x00026E5A
		public event AddingNewEventHandler AddingNew
		{
			add
			{
				this._addingNew = (AddingNewEventHandler)Delegate.Combine(this._addingNew, value);
			}
			remove
			{
				this._addingNew = (AddingNewEventHandler)Delegate.Remove(this._addingNew, value);
			}
		}

		/// <summary>
		/// Occurs when the items list of the collection has changed, or the collection is reset.
		/// </summary>
		// Token: 0x14000006 RID: 6
		// (add) Token: 0x060009CE RID: 2510 RVA: 0x00028C73 File Offset: 0x00026E73
		// (remove) Token: 0x060009CF RID: 2511 RVA: 0x00028C8C File Offset: 0x00026E8C
		[Nullable(2)]
		public event NotifyCollectionChangedEventHandler CollectionChanged
		{
			[NullableContext(2)]
			add
			{
				this._collectionChanged = (NotifyCollectionChangedEventHandler)Delegate.Combine(this._collectionChanged, value);
			}
			[NullableContext(2)]
			remove
			{
				this._collectionChanged = (NotifyCollectionChangedEventHandler)Delegate.Remove(this._collectionChanged, value);
			}
		}

		/// <summary>
		/// Gets the container's children tokens.
		/// </summary>
		/// <value>The container's children tokens.</value>
		// Token: 0x170001C4 RID: 452
		// (get) Token: 0x060009D0 RID: 2512
		protected abstract IList<JToken> ChildrenTokens { get; }

		// Token: 0x060009D1 RID: 2513 RVA: 0x00028CA5 File Offset: 0x00026EA5
		internal JContainer()
		{
		}

		// Token: 0x060009D2 RID: 2514 RVA: 0x00028CB0 File Offset: 0x00026EB0
		internal JContainer(JContainer other, [Nullable(2)] JsonCloneSettings settings)
			: this()
		{
			ValidationUtils.ArgumentNotNull(other, "other");
			bool flag = settings == null || settings.CopyAnnotations;
			if (flag)
			{
				base.CopyAnnotations(this, other);
			}
			int num = 0;
			foreach (JToken content in ((IEnumerable<JToken>)other))
			{
				this.TryAddInternal(num, content, false, flag);
				num++;
			}
		}

		// Token: 0x060009D3 RID: 2515 RVA: 0x00028D2C File Offset: 0x00026F2C
		internal void CheckReentrancy()
		{
			if (this._busy)
			{
				throw new InvalidOperationException("Cannot change {0} during a collection change event.".FormatWith(CultureInfo.InvariantCulture, base.GetType()));
			}
		}

		// Token: 0x060009D4 RID: 2516 RVA: 0x00028D51 File Offset: 0x00026F51
		internal virtual IList<JToken> CreateChildrenCollection()
		{
			return new List<JToken>();
		}

		/// <summary>
		/// Raises the <see cref="E:Newtonsoft.Json.Linq.JContainer.AddingNew" /> event.
		/// </summary>
		/// <param name="e">The <see cref="T:System.ComponentModel.AddingNewEventArgs" /> instance containing the event data.</param>
		// Token: 0x060009D5 RID: 2517 RVA: 0x00028D58 File Offset: 0x00026F58
		protected virtual void OnAddingNew(AddingNewEventArgs e)
		{
			AddingNewEventHandler addingNew = this._addingNew;
			if (addingNew == null)
			{
				return;
			}
			addingNew(this, e);
		}

		/// <summary>
		/// Raises the <see cref="E:Newtonsoft.Json.Linq.JContainer.ListChanged" /> event.
		/// </summary>
		/// <param name="e">The <see cref="T:System.ComponentModel.ListChangedEventArgs" /> instance containing the event data.</param>
		// Token: 0x060009D6 RID: 2518 RVA: 0x00028D6C File Offset: 0x00026F6C
		protected virtual void OnListChanged(ListChangedEventArgs e)
		{
			ListChangedEventHandler listChanged = this._listChanged;
			if (listChanged != null)
			{
				this._busy = true;
				try
				{
					listChanged(this, e);
				}
				finally
				{
					this._busy = false;
				}
			}
		}

		/// <summary>
		/// Raises the <see cref="E:Newtonsoft.Json.Linq.JContainer.CollectionChanged" /> event.
		/// </summary>
		/// <param name="e">The <see cref="T:System.Collections.Specialized.NotifyCollectionChangedEventArgs" /> instance containing the event data.</param>
		// Token: 0x060009D7 RID: 2519 RVA: 0x00028DAC File Offset: 0x00026FAC
		protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			NotifyCollectionChangedEventHandler collectionChanged = this._collectionChanged;
			if (collectionChanged != null)
			{
				this._busy = true;
				try
				{
					collectionChanged(this, e);
				}
				finally
				{
					this._busy = false;
				}
			}
		}

		/// <summary>
		/// Gets a value indicating whether this token has child tokens.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this token has child values; otherwise, <c>false</c>.
		/// </value>
		// Token: 0x170001C5 RID: 453
		// (get) Token: 0x060009D8 RID: 2520 RVA: 0x00028DEC File Offset: 0x00026FEC
		public override bool HasValues
		{
			get
			{
				return this.ChildrenTokens.Count > 0;
			}
		}

		// Token: 0x060009D9 RID: 2521 RVA: 0x00028DFC File Offset: 0x00026FFC
		internal bool ContentsEqual(JContainer container)
		{
			if (container == this)
			{
				return true;
			}
			IList<JToken> childrenTokens = this.ChildrenTokens;
			IList<JToken> childrenTokens2 = container.ChildrenTokens;
			if (childrenTokens.Count != childrenTokens2.Count)
			{
				return false;
			}
			for (int i = 0; i < childrenTokens.Count; i++)
			{
				if (!childrenTokens[i].DeepEquals(childrenTokens2[i]))
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Get the first child token of this token.
		/// </summary>
		/// <value>
		/// A <see cref="T:Newtonsoft.Json.Linq.JToken" /> containing the first child token of the <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </value>
		// Token: 0x170001C6 RID: 454
		// (get) Token: 0x060009DA RID: 2522 RVA: 0x00028E58 File Offset: 0x00027058
		[Nullable(2)]
		public override JToken First
		{
			[NullableContext(2)]
			get
			{
				IList<JToken> childrenTokens = this.ChildrenTokens;
				if (childrenTokens.Count <= 0)
				{
					return null;
				}
				return childrenTokens[0];
			}
		}

		/// <summary>
		/// Get the last child token of this token.
		/// </summary>
		/// <value>
		/// A <see cref="T:Newtonsoft.Json.Linq.JToken" /> containing the last child token of the <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </value>
		// Token: 0x170001C7 RID: 455
		// (get) Token: 0x060009DB RID: 2523 RVA: 0x00028E80 File Offset: 0x00027080
		[Nullable(2)]
		public override JToken Last
		{
			[NullableContext(2)]
			get
			{
				IList<JToken> childrenTokens = this.ChildrenTokens;
				int count = childrenTokens.Count;
				if (count <= 0)
				{
					return null;
				}
				return childrenTokens[count - 1];
			}
		}

		/// <summary>
		/// Returns a collection of the child tokens of this token, in document order.
		/// </summary>
		/// <returns>
		/// An <see cref="T:System.Collections.Generic.IEnumerable`1" /> of <see cref="T:Newtonsoft.Json.Linq.JToken" /> containing the child tokens of this <see cref="T:Newtonsoft.Json.Linq.JToken" />, in document order.
		/// </returns>
		// Token: 0x060009DC RID: 2524 RVA: 0x00028EAA File Offset: 0x000270AA
		[return: Nullable(new byte[] { 0, 1 })]
		public override JEnumerable<JToken> Children()
		{
			return new JEnumerable<JToken>(this.ChildrenTokens);
		}

		/// <summary>
		/// Returns a collection of the child values of this token, in document order.
		/// </summary>
		/// <typeparam name="T">The type to convert the values to.</typeparam>
		/// <returns>
		/// A <see cref="T:System.Collections.Generic.IEnumerable`1" /> containing the child values of this <see cref="T:Newtonsoft.Json.Linq.JToken" />, in document order.
		/// </returns>
		// Token: 0x060009DD RID: 2525 RVA: 0x00028EB7 File Offset: 0x000270B7
		[NullableContext(2)]
		[return: Nullable(new byte[] { 1, 2 })]
		public override IEnumerable<T> Values<T>()
		{
			return this.ChildrenTokens.Convert<JToken, T>();
		}

		/// <summary>
		/// Returns a collection of the descendant tokens for this token in document order.
		/// </summary>
		/// <returns>An <see cref="T:System.Collections.Generic.IEnumerable`1" /> of <see cref="T:Newtonsoft.Json.Linq.JToken" /> containing the descendant tokens of the <see cref="T:Newtonsoft.Json.Linq.JToken" />.</returns>
		// Token: 0x060009DE RID: 2526 RVA: 0x00028EC4 File Offset: 0x000270C4
		public IEnumerable<JToken> Descendants()
		{
			return this.GetDescendants(false);
		}

		/// <summary>
		/// Returns a collection of the tokens that contain this token, and all descendant tokens of this token, in document order.
		/// </summary>
		/// <returns>An <see cref="T:System.Collections.Generic.IEnumerable`1" /> of <see cref="T:Newtonsoft.Json.Linq.JToken" /> containing this token, and all the descendant tokens of the <see cref="T:Newtonsoft.Json.Linq.JToken" />.</returns>
		// Token: 0x060009DF RID: 2527 RVA: 0x00028ECD File Offset: 0x000270CD
		public IEnumerable<JToken> DescendantsAndSelf()
		{
			return this.GetDescendants(true);
		}

		// Token: 0x060009E0 RID: 2528 RVA: 0x00028ED6 File Offset: 0x000270D6
		internal IEnumerable<JToken> GetDescendants(bool self)
		{
			if (self)
			{
				yield return this;
			}
			foreach (JToken o in this.ChildrenTokens)
			{
				yield return o;
				JContainer jcontainer = o as JContainer;
				if (jcontainer != null)
				{
					foreach (JToken jtoken in jcontainer.Descendants())
					{
						yield return jtoken;
					}
					IEnumerator<JToken> enumerator2 = null;
				}
				o = null;
			}
			IEnumerator<JToken> enumerator = null;
			yield break;
			yield break;
		}

		// Token: 0x060009E1 RID: 2529 RVA: 0x00028EED File Offset: 0x000270ED
		[NullableContext(2)]
		internal bool IsMultiContent([NotNullWhen(true)] object content)
		{
			return content is IEnumerable && !(content is string) && !(content is JToken) && !(content is byte[]);
		}

		// Token: 0x060009E2 RID: 2530 RVA: 0x00028F18 File Offset: 0x00027118
		internal JToken EnsureParentToken([Nullable(2)] JToken item, bool skipParentCheck, bool copyAnnotations)
		{
			if (item == null)
			{
				return JValue.CreateNull();
			}
			if (skipParentCheck)
			{
				return item;
			}
			if (item.Parent != null || item == this || (item.HasValues && base.Root == item))
			{
				JsonCloneSettings settings = (copyAnnotations ? null : JsonCloneSettings.SkipCopyAnnotations);
				item = item.CloneToken(settings);
			}
			return item;
		}

		// Token: 0x060009E3 RID: 2531
		[NullableContext(2)]
		internal abstract int IndexOfItem(JToken item);

		// Token: 0x060009E4 RID: 2532 RVA: 0x00028F68 File Offset: 0x00027168
		[NullableContext(2)]
		internal virtual bool InsertItem(int index, JToken item, bool skipParentCheck, bool copyAnnotations)
		{
			IList<JToken> childrenTokens = this.ChildrenTokens;
			if (index > childrenTokens.Count)
			{
				throw new ArgumentOutOfRangeException("index", "Index must be within the bounds of the List.");
			}
			this.CheckReentrancy();
			item = this.EnsureParentToken(item, skipParentCheck, copyAnnotations);
			JToken jtoken = ((index == 0) ? null : childrenTokens[index - 1]);
			JToken jtoken2 = ((index == childrenTokens.Count) ? null : childrenTokens[index]);
			this.ValidateToken(item, null);
			item.Parent = this;
			item.Previous = jtoken;
			if (jtoken != null)
			{
				jtoken.Next = item;
			}
			item.Next = jtoken2;
			if (jtoken2 != null)
			{
				jtoken2.Previous = item;
			}
			childrenTokens.Insert(index, item);
			if (this._listChanged != null)
			{
				this.OnListChanged(new ListChangedEventArgs(ListChangedType.ItemAdded, index));
			}
			if (this._collectionChanged != null)
			{
				this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
			}
			return true;
		}

		// Token: 0x060009E5 RID: 2533 RVA: 0x00029030 File Offset: 0x00027230
		internal virtual void RemoveItemAt(int index)
		{
			IList<JToken> childrenTokens = this.ChildrenTokens;
			if (index < 0)
			{
				throw new ArgumentOutOfRangeException("index", "Index is less than 0.");
			}
			if (index >= childrenTokens.Count)
			{
				throw new ArgumentOutOfRangeException("index", "Index is equal to or greater than Count.");
			}
			this.CheckReentrancy();
			JToken jtoken = childrenTokens[index];
			JToken jtoken2 = ((index == 0) ? null : childrenTokens[index - 1]);
			JToken jtoken3 = ((index == childrenTokens.Count - 1) ? null : childrenTokens[index + 1]);
			if (jtoken2 != null)
			{
				jtoken2.Next = jtoken3;
			}
			if (jtoken3 != null)
			{
				jtoken3.Previous = jtoken2;
			}
			jtoken.Parent = null;
			jtoken.Previous = null;
			jtoken.Next = null;
			childrenTokens.RemoveAt(index);
			if (this._listChanged != null)
			{
				this.OnListChanged(new ListChangedEventArgs(ListChangedType.ItemDeleted, index));
			}
			if (this._collectionChanged != null)
			{
				this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, jtoken, index));
			}
		}

		// Token: 0x060009E6 RID: 2534 RVA: 0x00029104 File Offset: 0x00027304
		[NullableContext(2)]
		internal virtual bool RemoveItem(JToken item)
		{
			if (item != null)
			{
				int num = this.IndexOfItem(item);
				if (num >= 0)
				{
					this.RemoveItemAt(num);
					return true;
				}
			}
			return false;
		}

		// Token: 0x060009E7 RID: 2535 RVA: 0x0002912A File Offset: 0x0002732A
		internal virtual JToken GetItem(int index)
		{
			return this.ChildrenTokens[index];
		}

		// Token: 0x060009E8 RID: 2536 RVA: 0x00029138 File Offset: 0x00027338
		[NullableContext(2)]
		internal virtual void SetItem(int index, JToken item)
		{
			IList<JToken> childrenTokens = this.ChildrenTokens;
			if (index < 0)
			{
				throw new ArgumentOutOfRangeException("index", "Index is less than 0.");
			}
			if (index >= childrenTokens.Count)
			{
				throw new ArgumentOutOfRangeException("index", "Index is equal to or greater than Count.");
			}
			JToken jtoken = childrenTokens[index];
			if (JContainer.IsTokenUnchanged(jtoken, item))
			{
				return;
			}
			this.CheckReentrancy();
			item = this.EnsureParentToken(item, false, true);
			this.ValidateToken(item, jtoken);
			JToken jtoken2 = ((index == 0) ? null : childrenTokens[index - 1]);
			JToken jtoken3 = ((index == childrenTokens.Count - 1) ? null : childrenTokens[index + 1]);
			item.Parent = this;
			item.Previous = jtoken2;
			if (jtoken2 != null)
			{
				jtoken2.Next = item;
			}
			item.Next = jtoken3;
			if (jtoken3 != null)
			{
				jtoken3.Previous = item;
			}
			childrenTokens[index] = item;
			jtoken.Parent = null;
			jtoken.Previous = null;
			jtoken.Next = null;
			if (this._listChanged != null)
			{
				this.OnListChanged(new ListChangedEventArgs(ListChangedType.ItemChanged, index));
			}
			if (this._collectionChanged != null)
			{
				this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, item, jtoken, index));
			}
		}

		// Token: 0x060009E9 RID: 2537 RVA: 0x00029240 File Offset: 0x00027440
		internal virtual void ClearItems()
		{
			this.CheckReentrancy();
			IList<JToken> childrenTokens = this.ChildrenTokens;
			foreach (JToken jtoken in childrenTokens)
			{
				jtoken.Parent = null;
				jtoken.Previous = null;
				jtoken.Next = null;
			}
			childrenTokens.Clear();
			if (this._listChanged != null)
			{
				this.OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
			}
			if (this._collectionChanged != null)
			{
				this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			}
		}

		// Token: 0x060009EA RID: 2538 RVA: 0x000292D0 File Offset: 0x000274D0
		internal virtual void ReplaceItem(JToken existing, JToken replacement)
		{
			if (existing == null || existing.Parent != this)
			{
				return;
			}
			int index = this.IndexOfItem(existing);
			this.SetItem(index, replacement);
		}

		// Token: 0x060009EB RID: 2539 RVA: 0x000292FA File Offset: 0x000274FA
		[NullableContext(2)]
		internal virtual bool ContainsItem(JToken item)
		{
			return this.IndexOfItem(item) != -1;
		}

		// Token: 0x060009EC RID: 2540 RVA: 0x0002930C File Offset: 0x0002750C
		internal virtual void CopyItemsTo(Array array, int arrayIndex)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array");
			}
			if (arrayIndex < 0)
			{
				throw new ArgumentOutOfRangeException("arrayIndex", "arrayIndex is less than 0.");
			}
			if (arrayIndex >= array.Length && arrayIndex != 0)
			{
				throw new ArgumentException("arrayIndex is equal to or greater than the length of array.");
			}
			if (this.Count > array.Length - arrayIndex)
			{
				throw new ArgumentException("The number of elements in the source JObject is greater than the available space from arrayIndex to the end of the destination array.");
			}
			int num = 0;
			foreach (JToken value in this.ChildrenTokens)
			{
				array.SetValue(value, arrayIndex + num);
				num++;
			}
		}

		// Token: 0x060009ED RID: 2541 RVA: 0x000293B8 File Offset: 0x000275B8
		internal static bool IsTokenUnchanged(JToken currentValue, [Nullable(2)] JToken newValue)
		{
			JValue jvalue = currentValue as JValue;
			if (jvalue == null)
			{
				return false;
			}
			if (newValue == null)
			{
				return jvalue.Type == JTokenType.Null;
			}
			return jvalue.Equals(newValue);
		}

		// Token: 0x060009EE RID: 2542 RVA: 0x000293E6 File Offset: 0x000275E6
		internal virtual void ValidateToken(JToken o, [Nullable(2)] JToken existing)
		{
			ValidationUtils.ArgumentNotNull(o, "o");
			if (o.Type == JTokenType.Property)
			{
				throw new ArgumentException("Can not add {0} to {1}.".FormatWith(CultureInfo.InvariantCulture, o.GetType(), base.GetType()));
			}
		}

		/// <summary>
		/// Adds the specified content as children of this <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <param name="content">The content to be added.</param>
		// Token: 0x060009EF RID: 2543 RVA: 0x0002941D File Offset: 0x0002761D
		[NullableContext(2)]
		public virtual void Add(object content)
		{
			this.TryAddInternal(this.ChildrenTokens.Count, content, false, true);
		}

		// Token: 0x060009F0 RID: 2544 RVA: 0x00029434 File Offset: 0x00027634
		[NullableContext(2)]
		internal bool TryAdd(object content)
		{
			return this.TryAddInternal(this.ChildrenTokens.Count, content, false, true);
		}

		// Token: 0x060009F1 RID: 2545 RVA: 0x0002944A File Offset: 0x0002764A
		internal void AddAndSkipParentCheck(JToken token)
		{
			this.TryAddInternal(this.ChildrenTokens.Count, token, true, true);
		}

		/// <summary>
		/// Adds the specified content as the first children of this <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <param name="content">The content to be added.</param>
		// Token: 0x060009F2 RID: 2546 RVA: 0x00029461 File Offset: 0x00027661
		[NullableContext(2)]
		public void AddFirst(object content)
		{
			this.TryAddInternal(0, content, false, true);
		}

		// Token: 0x060009F3 RID: 2547 RVA: 0x00029470 File Offset: 0x00027670
		[NullableContext(2)]
		internal bool TryAddInternal(int index, object content, bool skipParentCheck, bool copyAnnotations)
		{
			if (this.IsMultiContent(content))
			{
				IEnumerable enumerable = (IEnumerable)content;
				int num = index;
				foreach (object content2 in enumerable)
				{
					this.TryAddInternal(num, content2, skipParentCheck, copyAnnotations);
					num++;
				}
				return true;
			}
			JToken item = JContainer.CreateFromContent(content);
			return this.InsertItem(index, item, skipParentCheck, copyAnnotations);
		}

		// Token: 0x060009F4 RID: 2548 RVA: 0x000294F0 File Offset: 0x000276F0
		internal static JToken CreateFromContent([Nullable(2)] object content)
		{
			JToken jtoken = content as JToken;
			if (jtoken != null)
			{
				return jtoken;
			}
			return new JValue(content);
		}

		/// <summary>
		/// Creates a <see cref="T:Newtonsoft.Json.JsonWriter" /> that can be used to add tokens to the <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <returns>A <see cref="T:Newtonsoft.Json.JsonWriter" /> that is ready to have content written to it.</returns>
		// Token: 0x060009F5 RID: 2549 RVA: 0x0002950F File Offset: 0x0002770F
		public JsonWriter CreateWriter()
		{
			return new JTokenWriter(this);
		}

		/// <summary>
		/// Replaces the child nodes of this token with the specified content.
		/// </summary>
		/// <param name="content">The content.</param>
		// Token: 0x060009F6 RID: 2550 RVA: 0x00029517 File Offset: 0x00027717
		public void ReplaceAll(object content)
		{
			this.ClearItems();
			this.Add(content);
		}

		/// <summary>
		/// Removes the child nodes from this token.
		/// </summary>
		// Token: 0x060009F7 RID: 2551 RVA: 0x00029526 File Offset: 0x00027726
		public void RemoveAll()
		{
			this.ClearItems();
		}

		// Token: 0x060009F8 RID: 2552
		internal abstract void MergeItem(object content, [Nullable(2)] JsonMergeSettings settings);

		/// <summary>
		/// Merge the specified content into this <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <param name="content">The content to be merged.</param>
		// Token: 0x060009F9 RID: 2553 RVA: 0x0002952E File Offset: 0x0002772E
		[NullableContext(2)]
		public void Merge(object content)
		{
			if (content == null)
			{
				return;
			}
			this.ValidateContent(content);
			this.MergeItem(content, null);
		}

		/// <summary>
		/// Merge the specified content into this <see cref="T:Newtonsoft.Json.Linq.JToken" /> using <see cref="T:Newtonsoft.Json.Linq.JsonMergeSettings" />.
		/// </summary>
		/// <param name="content">The content to be merged.</param>
		/// <param name="settings">The <see cref="T:Newtonsoft.Json.Linq.JsonMergeSettings" /> used to merge the content.</param>
		// Token: 0x060009FA RID: 2554 RVA: 0x00029543 File Offset: 0x00027743
		[NullableContext(2)]
		public void Merge(object content, JsonMergeSettings settings)
		{
			if (content == null)
			{
				return;
			}
			this.ValidateContent(content);
			this.MergeItem(content, settings);
		}

		// Token: 0x060009FB RID: 2555 RVA: 0x00029558 File Offset: 0x00027758
		private void ValidateContent(object content)
		{
			if (content.GetType().IsSubclassOf(typeof(JToken)))
			{
				return;
			}
			if (this.IsMultiContent(content))
			{
				return;
			}
			throw new ArgumentException("Could not determine JSON object type for type {0}.".FormatWith(CultureInfo.InvariantCulture, content.GetType()), "content");
		}

		// Token: 0x060009FC RID: 2556 RVA: 0x000295A8 File Offset: 0x000277A8
		internal void ReadTokenFrom(JsonReader reader, [Nullable(2)] JsonLoadSettings options)
		{
			int depth = reader.Depth;
			if (!reader.Read())
			{
				throw JsonReaderException.Create(reader, "Error reading {0} from JsonReader.".FormatWith(CultureInfo.InvariantCulture, base.GetType().Name));
			}
			this.ReadContentFrom(reader, options);
			if (reader.Depth > depth)
			{
				throw JsonReaderException.Create(reader, "Unexpected end of content while loading {0}.".FormatWith(CultureInfo.InvariantCulture, base.GetType().Name));
			}
		}

		// Token: 0x060009FD RID: 2557 RVA: 0x00029618 File Offset: 0x00027818
		internal void ReadContentFrom(JsonReader r, [Nullable(2)] JsonLoadSettings settings)
		{
			ValidationUtils.ArgumentNotNull(r, "r");
			IJsonLineInfo lineInfo = r as IJsonLineInfo;
			JContainer jcontainer = this;
			for (;;)
			{
				JProperty jproperty = jcontainer as JProperty;
				if (jproperty != null && jproperty.Value != null)
				{
					if (jcontainer == this)
					{
						break;
					}
					jcontainer = jcontainer.Parent;
				}
				switch (r.TokenType)
				{
				case JsonToken.None:
					goto IL_1F2;
				case JsonToken.StartObject:
				{
					JObject jobject = new JObject();
					jobject.SetLineInfo(lineInfo, settings);
					jcontainer.Add(jobject);
					jcontainer = jobject;
					goto IL_1F2;
				}
				case JsonToken.StartArray:
				{
					JArray jarray = new JArray();
					jarray.SetLineInfo(lineInfo, settings);
					jcontainer.Add(jarray);
					jcontainer = jarray;
					goto IL_1F2;
				}
				case JsonToken.StartConstructor:
				{
					JConstructor jconstructor = new JConstructor(r.Value.ToString());
					jconstructor.SetLineInfo(lineInfo, settings);
					jcontainer.Add(jconstructor);
					jcontainer = jconstructor;
					goto IL_1F2;
				}
				case JsonToken.PropertyName:
				{
					JProperty jproperty2 = JContainer.ReadProperty(r, settings, lineInfo, jcontainer);
					if (jproperty2 != null)
					{
						jcontainer = jproperty2;
						goto IL_1F2;
					}
					r.Skip();
					goto IL_1F2;
				}
				case JsonToken.Comment:
					if (settings != null && settings.CommentHandling == CommentHandling.Load)
					{
						JValue jvalue = JValue.CreateComment(r.Value.ToString());
						jvalue.SetLineInfo(lineInfo, settings);
						jcontainer.Add(jvalue);
						goto IL_1F2;
					}
					goto IL_1F2;
				case JsonToken.Integer:
				case JsonToken.Float:
				case JsonToken.String:
				case JsonToken.Boolean:
				case JsonToken.Date:
				case JsonToken.Bytes:
				{
					JValue jvalue = new JValue(r.Value);
					jvalue.SetLineInfo(lineInfo, settings);
					jcontainer.Add(jvalue);
					goto IL_1F2;
				}
				case JsonToken.Null:
				{
					JValue jvalue = JValue.CreateNull();
					jvalue.SetLineInfo(lineInfo, settings);
					jcontainer.Add(jvalue);
					goto IL_1F2;
				}
				case JsonToken.Undefined:
				{
					JValue jvalue = JValue.CreateUndefined();
					jvalue.SetLineInfo(lineInfo, settings);
					jcontainer.Add(jvalue);
					goto IL_1F2;
				}
				case JsonToken.EndObject:
					if (jcontainer == this)
					{
						return;
					}
					jcontainer = jcontainer.Parent;
					goto IL_1F2;
				case JsonToken.EndArray:
					if (jcontainer == this)
					{
						return;
					}
					jcontainer = jcontainer.Parent;
					goto IL_1F2;
				case JsonToken.EndConstructor:
					if (jcontainer == this)
					{
						return;
					}
					jcontainer = jcontainer.Parent;
					goto IL_1F2;
				}
				goto Block_4;
				IL_1F2:
				if (!r.Read())
				{
					return;
				}
			}
			return;
			Block_4:
			throw new InvalidOperationException("The JsonReader should not be on a token of type {0}.".FormatWith(CultureInfo.InvariantCulture, r.TokenType));
		}

		// Token: 0x060009FE RID: 2558 RVA: 0x00029824 File Offset: 0x00027A24
		[NullableContext(2)]
		private static JProperty ReadProperty([Nullable(1)] JsonReader r, JsonLoadSettings settings, IJsonLineInfo lineInfo, [Nullable(1)] JContainer parent)
		{
			DuplicatePropertyNameHandling duplicatePropertyNameHandling = ((settings != null) ? settings.DuplicatePropertyNameHandling : DuplicatePropertyNameHandling.Replace);
			JObject jobject = (JObject)parent;
			string text = r.Value.ToString();
			JProperty jproperty = jobject.Property(text, StringComparison.Ordinal);
			if (jproperty != null)
			{
				if (duplicatePropertyNameHandling == DuplicatePropertyNameHandling.Ignore)
				{
					return null;
				}
				if (duplicatePropertyNameHandling == DuplicatePropertyNameHandling.Error)
				{
					throw JsonReaderException.Create(r, "Property with the name '{0}' already exists in the current JSON object.".FormatWith(CultureInfo.InvariantCulture, text));
				}
			}
			JProperty jproperty2 = new JProperty(text);
			jproperty2.SetLineInfo(lineInfo, settings);
			if (jproperty == null)
			{
				parent.Add(jproperty2);
			}
			else
			{
				jproperty.Replace(jproperty2);
			}
			return jproperty2;
		}

		// Token: 0x060009FF RID: 2559 RVA: 0x000298A0 File Offset: 0x00027AA0
		internal int ContentsHashCode()
		{
			int num = 0;
			foreach (JToken jtoken in this.ChildrenTokens)
			{
				num ^= jtoken.GetDeepHashCode();
			}
			return num;
		}

		// Token: 0x06000A00 RID: 2560 RVA: 0x000298F4 File Offset: 0x00027AF4
		string ITypedList.GetListName(PropertyDescriptor[] listAccessors)
		{
			return string.Empty;
		}

		// Token: 0x06000A01 RID: 2561 RVA: 0x000298FB File Offset: 0x00027AFB
		PropertyDescriptorCollection ITypedList.GetItemProperties(PropertyDescriptor[] listAccessors)
		{
			ICustomTypeDescriptor customTypeDescriptor = this.First as ICustomTypeDescriptor;
			return ((customTypeDescriptor != null) ? customTypeDescriptor.GetProperties() : null) ?? new PropertyDescriptorCollection(CollectionUtils.ArrayEmpty<PropertyDescriptor>());
		}

		// Token: 0x06000A02 RID: 2562 RVA: 0x00029922 File Offset: 0x00027B22
		int IList<JToken>.IndexOf(JToken item)
		{
			return this.IndexOfItem(item);
		}

		// Token: 0x06000A03 RID: 2563 RVA: 0x0002992B File Offset: 0x00027B2B
		void IList<JToken>.Insert(int index, JToken item)
		{
			this.InsertItem(index, item, false, true);
		}

		// Token: 0x06000A04 RID: 2564 RVA: 0x00029938 File Offset: 0x00027B38
		void IList<JToken>.RemoveAt(int index)
		{
			this.RemoveItemAt(index);
		}

		// Token: 0x170001C8 RID: 456
		JToken IList<JToken>.this[int index]
		{
			get
			{
				return this.GetItem(index);
			}
			set
			{
				this.SetItem(index, value);
			}
		}

		// Token: 0x06000A07 RID: 2567 RVA: 0x00029954 File Offset: 0x00027B54
		void ICollection<JToken>.Add(JToken item)
		{
			this.Add(item);
		}

		// Token: 0x06000A08 RID: 2568 RVA: 0x0002995D File Offset: 0x00027B5D
		void ICollection<JToken>.Clear()
		{
			this.ClearItems();
		}

		// Token: 0x06000A09 RID: 2569 RVA: 0x00029965 File Offset: 0x00027B65
		bool ICollection<JToken>.Contains(JToken item)
		{
			return this.ContainsItem(item);
		}

		// Token: 0x06000A0A RID: 2570 RVA: 0x0002996E File Offset: 0x00027B6E
		void ICollection<JToken>.CopyTo(JToken[] array, int arrayIndex)
		{
			this.CopyItemsTo(array, arrayIndex);
		}

		// Token: 0x170001C9 RID: 457
		// (get) Token: 0x06000A0B RID: 2571 RVA: 0x00029978 File Offset: 0x00027B78
		bool ICollection<JToken>.IsReadOnly
		{
			get
			{
				return false;
			}
		}

		// Token: 0x06000A0C RID: 2572 RVA: 0x0002997B File Offset: 0x00027B7B
		bool ICollection<JToken>.Remove(JToken item)
		{
			return this.RemoveItem(item);
		}

		// Token: 0x06000A0D RID: 2573 RVA: 0x00029984 File Offset: 0x00027B84
		[NullableContext(2)]
		private JToken EnsureValue(object value)
		{
			if (value == null)
			{
				return null;
			}
			JToken jtoken = value as JToken;
			if (jtoken != null)
			{
				return jtoken;
			}
			throw new ArgumentException("Argument is not a JToken.");
		}

		// Token: 0x06000A0E RID: 2574 RVA: 0x000299AC File Offset: 0x00027BAC
		[NullableContext(2)]
		int IList.Add(object value)
		{
			this.Add(this.EnsureValue(value));
			return this.Count - 1;
		}

		// Token: 0x06000A0F RID: 2575 RVA: 0x000299C3 File Offset: 0x00027BC3
		void IList.Clear()
		{
			this.ClearItems();
		}

		// Token: 0x06000A10 RID: 2576 RVA: 0x000299CB File Offset: 0x00027BCB
		[NullableContext(2)]
		bool IList.Contains(object value)
		{
			return this.ContainsItem(this.EnsureValue(value));
		}

		// Token: 0x06000A11 RID: 2577 RVA: 0x000299DA File Offset: 0x00027BDA
		[NullableContext(2)]
		int IList.IndexOf(object value)
		{
			return this.IndexOfItem(this.EnsureValue(value));
		}

		// Token: 0x06000A12 RID: 2578 RVA: 0x000299E9 File Offset: 0x00027BE9
		[NullableContext(2)]
		void IList.Insert(int index, object value)
		{
			this.InsertItem(index, this.EnsureValue(value), false, false);
		}

		// Token: 0x170001CA RID: 458
		// (get) Token: 0x06000A13 RID: 2579 RVA: 0x000299FC File Offset: 0x00027BFC
		bool IList.IsFixedSize
		{
			get
			{
				return false;
			}
		}

		// Token: 0x170001CB RID: 459
		// (get) Token: 0x06000A14 RID: 2580 RVA: 0x000299FF File Offset: 0x00027BFF
		bool IList.IsReadOnly
		{
			get
			{
				return false;
			}
		}

		// Token: 0x06000A15 RID: 2581 RVA: 0x00029A02 File Offset: 0x00027C02
		[NullableContext(2)]
		void IList.Remove(object value)
		{
			this.RemoveItem(this.EnsureValue(value));
		}

		// Token: 0x06000A16 RID: 2582 RVA: 0x00029A12 File Offset: 0x00027C12
		void IList.RemoveAt(int index)
		{
			this.RemoveItemAt(index);
		}

		// Token: 0x170001CC RID: 460
		[Nullable(2)]
		object IList.this[int index]
		{
			[NullableContext(2)]
			get
			{
				return this.GetItem(index);
			}
			[NullableContext(2)]
			set
			{
				this.SetItem(index, this.EnsureValue(value));
			}
		}

		// Token: 0x06000A19 RID: 2585 RVA: 0x00029A34 File Offset: 0x00027C34
		void ICollection.CopyTo(Array array, int index)
		{
			this.CopyItemsTo(array, index);
		}

		/// <summary>
		/// Gets the count of child JSON tokens.
		/// </summary>
		/// <value>The count of child JSON tokens.</value>
		// Token: 0x170001CD RID: 461
		// (get) Token: 0x06000A1A RID: 2586 RVA: 0x00029A3E File Offset: 0x00027C3E
		public int Count
		{
			get
			{
				return this.ChildrenTokens.Count;
			}
		}

		// Token: 0x170001CE RID: 462
		// (get) Token: 0x06000A1B RID: 2587 RVA: 0x00029A4B File Offset: 0x00027C4B
		bool ICollection.IsSynchronized
		{
			get
			{
				return false;
			}
		}

		// Token: 0x170001CF RID: 463
		// (get) Token: 0x06000A1C RID: 2588 RVA: 0x00029A4E File Offset: 0x00027C4E
		object ICollection.SyncRoot
		{
			get
			{
				if (this._syncRoot == null)
				{
					Interlocked.CompareExchange(ref this._syncRoot, new object(), null);
				}
				return this._syncRoot;
			}
		}

		// Token: 0x06000A1D RID: 2589 RVA: 0x00029A70 File Offset: 0x00027C70
		void IBindingList.AddIndex(PropertyDescriptor property)
		{
		}

		// Token: 0x06000A1E RID: 2590 RVA: 0x00029A74 File Offset: 0x00027C74
		object IBindingList.AddNew()
		{
			AddingNewEventArgs addingNewEventArgs = new AddingNewEventArgs();
			this.OnAddingNew(addingNewEventArgs);
			if (addingNewEventArgs.NewObject == null)
			{
				throw new JsonException("Could not determine new value to add to '{0}'.".FormatWith(CultureInfo.InvariantCulture, base.GetType()));
			}
			JToken jtoken = addingNewEventArgs.NewObject as JToken;
			if (jtoken == null)
			{
				throw new JsonException("New item to be added to collection must be compatible with {0}.".FormatWith(CultureInfo.InvariantCulture, typeof(JToken)));
			}
			this.Add(jtoken);
			return jtoken;
		}

		// Token: 0x170001D0 RID: 464
		// (get) Token: 0x06000A1F RID: 2591 RVA: 0x00029AE7 File Offset: 0x00027CE7
		bool IBindingList.AllowEdit
		{
			get
			{
				return true;
			}
		}

		// Token: 0x170001D1 RID: 465
		// (get) Token: 0x06000A20 RID: 2592 RVA: 0x00029AEA File Offset: 0x00027CEA
		bool IBindingList.AllowNew
		{
			get
			{
				return true;
			}
		}

		// Token: 0x170001D2 RID: 466
		// (get) Token: 0x06000A21 RID: 2593 RVA: 0x00029AED File Offset: 0x00027CED
		bool IBindingList.AllowRemove
		{
			get
			{
				return true;
			}
		}

		// Token: 0x06000A22 RID: 2594 RVA: 0x00029AF0 File Offset: 0x00027CF0
		void IBindingList.ApplySort(PropertyDescriptor property, ListSortDirection direction)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06000A23 RID: 2595 RVA: 0x00029AF7 File Offset: 0x00027CF7
		int IBindingList.Find(PropertyDescriptor property, object key)
		{
			throw new NotSupportedException();
		}

		// Token: 0x170001D3 RID: 467
		// (get) Token: 0x06000A24 RID: 2596 RVA: 0x00029AFE File Offset: 0x00027CFE
		bool IBindingList.IsSorted
		{
			get
			{
				return false;
			}
		}

		// Token: 0x06000A25 RID: 2597 RVA: 0x00029B01 File Offset: 0x00027D01
		void IBindingList.RemoveIndex(PropertyDescriptor property)
		{
		}

		// Token: 0x06000A26 RID: 2598 RVA: 0x00029B03 File Offset: 0x00027D03
		void IBindingList.RemoveSort()
		{
			throw new NotSupportedException();
		}

		// Token: 0x170001D4 RID: 468
		// (get) Token: 0x06000A27 RID: 2599 RVA: 0x00029B0A File Offset: 0x00027D0A
		ListSortDirection IBindingList.SortDirection
		{
			get
			{
				return ListSortDirection.Ascending;
			}
		}

		// Token: 0x170001D5 RID: 469
		// (get) Token: 0x06000A28 RID: 2600 RVA: 0x00029B0D File Offset: 0x00027D0D
		[Nullable(2)]
		PropertyDescriptor IBindingList.SortProperty
		{
			[NullableContext(2)]
			get
			{
				return null;
			}
		}

		// Token: 0x170001D6 RID: 470
		// (get) Token: 0x06000A29 RID: 2601 RVA: 0x00029B10 File Offset: 0x00027D10
		bool IBindingList.SupportsChangeNotification
		{
			get
			{
				return true;
			}
		}

		// Token: 0x170001D7 RID: 471
		// (get) Token: 0x06000A2A RID: 2602 RVA: 0x00029B13 File Offset: 0x00027D13
		bool IBindingList.SupportsSearching
		{
			get
			{
				return false;
			}
		}

		// Token: 0x170001D8 RID: 472
		// (get) Token: 0x06000A2B RID: 2603 RVA: 0x00029B16 File Offset: 0x00027D16
		bool IBindingList.SupportsSorting
		{
			get
			{
				return false;
			}
		}

		// Token: 0x06000A2C RID: 2604 RVA: 0x00029B1C File Offset: 0x00027D1C
		internal static void MergeEnumerableContent(JContainer target, IEnumerable content, [Nullable(2)] JsonMergeSettings settings)
		{
			switch ((settings != null) ? settings.MergeArrayHandling : MergeArrayHandling.Concat)
			{
			case MergeArrayHandling.Concat:
				using (IEnumerator enumerator = content.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						object content2 = enumerator.Current;
						target.Add(JContainer.CreateFromContent(content2));
					}
					return;
				}
				break;
			case MergeArrayHandling.Union:
				break;
			case MergeArrayHandling.Replace:
				goto IL_BC;
			case MergeArrayHandling.Merge:
				goto IL_108;
			default:
				goto IL_19E;
			}
			HashSet<JToken> hashSet = new HashSet<JToken>(target, JToken.EqualityComparer);
			using (IEnumerator enumerator = content.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					object content3 = enumerator.Current;
					JToken jtoken = JContainer.CreateFromContent(content3);
					if (hashSet.Add(jtoken))
					{
						target.Add(jtoken);
					}
				}
				return;
			}
			IL_BC:
			if (target == content)
			{
				return;
			}
			target.ClearItems();
			using (IEnumerator enumerator = content.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					object content4 = enumerator.Current;
					target.Add(JContainer.CreateFromContent(content4));
				}
				return;
			}
			IL_108:
			int num = 0;
			using (IEnumerator enumerator = content.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					object obj = enumerator.Current;
					if (num < target.Count)
					{
						JContainer jcontainer = target[num] as JContainer;
						if (jcontainer != null)
						{
							jcontainer.Merge(obj, settings);
						}
						else if (obj != null)
						{
							JToken jtoken2 = JContainer.CreateFromContent(obj);
							if (jtoken2.Type != JTokenType.Null)
							{
								target[num] = jtoken2;
							}
						}
					}
					else
					{
						target.Add(JContainer.CreateFromContent(obj));
					}
					num++;
				}
				return;
			}
			IL_19E:
			throw new ArgumentOutOfRangeException("settings", "Unexpected merge array handling when merging JSON.");
		}

		// Token: 0x0400037C RID: 892
		[Nullable(2)]
		internal ListChangedEventHandler _listChanged;

		// Token: 0x0400037D RID: 893
		[Nullable(2)]
		internal AddingNewEventHandler _addingNew;

		// Token: 0x0400037E RID: 894
		[Nullable(2)]
		internal NotifyCollectionChangedEventHandler _collectionChanged;

		// Token: 0x0400037F RID: 895
		[Nullable(2)]
		private object _syncRoot;

		// Token: 0x04000380 RID: 896
		private bool _busy;
	}
}
