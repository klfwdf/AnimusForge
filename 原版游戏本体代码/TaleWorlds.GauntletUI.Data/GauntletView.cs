using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Numerics;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.PrefabSystem;
using TaleWorlds.Library;

namespace TaleWorlds.GauntletUI.Data
{
	// Token: 0x02000005 RID: 5
	public class GauntletView : WidgetComponent
	{
		// Token: 0x1700000A RID: 10
		// (get) Token: 0x0600001F RID: 31 RVA: 0x000024A8 File Offset: 0x000006A8
		// (set) Token: 0x06000020 RID: 32 RVA: 0x000024B0 File Offset: 0x000006B0
		internal bool AddedToChildren { get; private set; }

		// Token: 0x1700000B RID: 11
		// (get) Token: 0x06000021 RID: 33 RVA: 0x000024B9 File Offset: 0x000006B9
		// (set) Token: 0x06000022 RID: 34 RVA: 0x000024C1 File Offset: 0x000006C1
		public GauntletMovie GauntletMovie { get; private set; }

		// Token: 0x1700000C RID: 12
		// (get) Token: 0x06000023 RID: 35 RVA: 0x000024CA File Offset: 0x000006CA
		// (set) Token: 0x06000024 RID: 36 RVA: 0x000024D2 File Offset: 0x000006D2
		public ItemTemplateUsageWithData ItemTemplateUsageWithData { get; internal set; }

		// Token: 0x1700000D RID: 13
		// (get) Token: 0x06000025 RID: 37 RVA: 0x000024DC File Offset: 0x000006DC
		public BindingPath ViewModelPath
		{
			get
			{
				if (this.Parent == null)
				{
					return this._viewModelPath;
				}
				if (this._viewModelPath != null)
				{
					return this.Parent.ViewModelPath.Append(this._viewModelPath);
				}
				return this.Parent.ViewModelPath;
			}
		}

		// Token: 0x1700000E RID: 14
		// (get) Token: 0x06000026 RID: 38 RVA: 0x00002528 File Offset: 0x00000728
		public string ViewModelPathString
		{
			get
			{
				MBStringBuilder mbstringBuilder = default(MBStringBuilder);
				mbstringBuilder.Initialize(16, "ViewModelPathString");
				this.WriteViewModelPathToStringBuilder(ref mbstringBuilder);
				return mbstringBuilder.ToStringAndRelease();
			}
		}

		// Token: 0x06000027 RID: 39 RVA: 0x0000255C File Offset: 0x0000075C
		private void WriteViewModelPathToStringBuilder(ref MBStringBuilder sb)
		{
			if (this.Parent == null)
			{
				if (this._viewModelPath != null)
				{
					sb.Append<string>(this._viewModelPath.Path);
					return;
				}
			}
			else
			{
				this.Parent.WriteViewModelPathToStringBuilder(ref sb);
				if (this._viewModelPath != null)
				{
					sb.Append<string>("\\");
					sb.Append<string>(this._viewModelPath.Path);
				}
			}
		}

		// Token: 0x06000028 RID: 40 RVA: 0x000025CA File Offset: 0x000007CA
		internal void InitializeViewModelPath(BindingPath path)
		{
			this._viewModelPath = path;
		}

		// Token: 0x1700000F RID: 15
		// (get) Token: 0x06000029 RID: 41 RVA: 0x000025D3 File Offset: 0x000007D3
		// (set) Token: 0x0600002A RID: 42 RVA: 0x000025DB File Offset: 0x000007DB
		public GauntletView Parent { get; private set; }

		// Token: 0x0600002B RID: 43 RVA: 0x000025E4 File Offset: 0x000007E4
		internal GauntletView(GauntletMovie gauntletMovie, GauntletView parent, Widget target, int childCount = 64)
			: base(target)
		{
			this.GauntletMovie = gauntletMovie;
			this.Parent = parent;
			this._children = new MBList<GauntletView>(childCount);
			this._bindDataInfosByPath = new Dictionary<string, List<ViewBindDataInfo>>();
			this._bindCommandInfos = new Dictionary<string, ViewBindCommandInfo>();
			this._items = new List<GauntletView>(childCount);
		}

		// Token: 0x0600002C RID: 44 RVA: 0x00002636 File Offset: 0x00000836
		public void AddChild(GauntletView child)
		{
			this._children.Add(child);
			child.AddedToChildren = true;
		}

		// Token: 0x0600002D RID: 45 RVA: 0x0000264B File Offset: 0x0000084B
		public void RemoveChild(GauntletView child)
		{
			base.Target.OnBeforeRemovedChild(child.Target);
			base.Target.RemoveChild(child.Target);
			this._children.Remove(child);
			child.ClearEventHandlersWithChildren();
		}

		// Token: 0x0600002E RID: 46 RVA: 0x00002684 File Offset: 0x00000884
		public void SwapChildrenAtIndeces(GauntletView child1, GauntletView child2)
		{
			int index = this._children.IndexOf(child1);
			int index2 = this._children.IndexOf(child2);
			base.Target.SwapChildren(this._children[index].Target, this._children[index2].Target);
			GauntletView value = this._children[index];
			this._children[index] = this._children[index2];
			this._children[index2] = value;
		}

		// Token: 0x0600002F RID: 47 RVA: 0x0000270C File Offset: 0x0000090C
		public void RefreshBinding()
		{
			object viewModelAtPath = this.GauntletMovie.GetViewModelAtPath(this.ViewModelPath, this.ItemTemplateUsageWithData != null && this.ItemTemplateUsageWithData.ItemTemplateUsage != null);
			this.ClearEventHandlersWithChildren();
			if (viewModelAtPath is IViewModel)
			{
				this._viewModel = viewModelAtPath as IViewModel;
				if (this._viewModel == null)
				{
					goto IL_1F6;
				}
				this._viewModel.PropertyChanged += this.OnViewModelPropertyChanged;
				this._viewModel.PropertyChangedWithValue += this.OnViewModelPropertyChangedWithValue;
				this._viewModel.PropertyChangedWithBoolValue += this.OnViewModelPropertyChangedWithValue;
				this._viewModel.PropertyChangedWithColorValue += this.OnViewModelPropertyChangedWithValue;
				this._viewModel.PropertyChangedWithDoubleValue += this.OnViewModelPropertyChangedWithValue;
				this._viewModel.PropertyChangedWithFloatValue += this.OnViewModelPropertyChangedWithValue;
				this._viewModel.PropertyChangedWithIntValue += this.OnViewModelPropertyChangedWithValue;
				this._viewModel.PropertyChangedWithUIntValue += this.OnViewModelPropertyChangedWithValue;
				this._viewModel.PropertyChangedWithVec2Value += this.OnViewModelPropertyChangedWithValue;
				using (Dictionary<string, List<ViewBindDataInfo>>.Enumerator enumerator = this._bindDataInfosByPath.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						KeyValuePair<string, List<ViewBindDataInfo>> keyValuePair = enumerator.Current;
						List<ViewBindDataInfo> value = keyValuePair.Value;
						for (int i = 0; i < value.Count; i++)
						{
							ViewBindDataInfo viewBindDataInfo = value[i];
							object propertyValue = this._viewModel.GetPropertyValue(viewBindDataInfo.Path.LastNode);
							this.SetData(viewBindDataInfo.Property, propertyValue);
						}
					}
					goto IL_1F6;
				}
			}
			if (viewModelAtPath is IMBBindingList)
			{
				this._bindingList = viewModelAtPath as IMBBindingList;
				if (this._bindingList != null)
				{
					this._bindingList.ListChanged += this.OnViewModelBindingListChanged;
					for (int j = 0; j < this._bindingList.Count; j++)
					{
						this.AddItemToList(j);
					}
				}
			}
			IL_1F6:
			base.Target.EventFire += this.OnEventFired;
			base.Target.PropertyChanged += this.OnViewObjectPropertyChanged;
			base.Target.boolPropertyChanged += this.OnViewObjectPropertyChanged;
			base.Target.ColorPropertyChanged += this.OnViewObjectPropertyChanged;
			base.Target.doublePropertyChanged += this.OnViewObjectPropertyChanged;
			base.Target.floatPropertyChanged += this.OnViewObjectPropertyChanged;
			base.Target.intPropertyChanged += this.OnViewObjectPropertyChanged;
			base.Target.uintPropertyChanged += this.OnViewObjectPropertyChanged;
			base.Target.Vec2PropertyChanged += this.OnViewObjectPropertyChanged;
			base.Target.Vector2PropertyChanged += this.OnViewObjectPropertyChanged;
		}

		// Token: 0x06000030 RID: 48 RVA: 0x00002A08 File Offset: 0x00000C08
		private void OnEventFired(Widget widget, string commandName, object[] args)
		{
			this.OnCommand(commandName, args);
		}

		// Token: 0x06000031 RID: 49 RVA: 0x00002A14 File Offset: 0x00000C14
		public void RefreshBindingWithChildren()
		{
			this.RefreshBinding();
			for (int i = 0; i < this._children.Count; i++)
			{
				this._children[i].RefreshBindingWithChildren();
			}
		}

		// Token: 0x06000032 RID: 50 RVA: 0x00002A50 File Offset: 0x00000C50
		private void ReleaseBinding()
		{
			if (this._viewModel != null)
			{
				this._viewModel.PropertyChanged -= this.OnViewModelPropertyChanged;
				this._viewModel.PropertyChangedWithValue -= this.OnViewModelPropertyChangedWithValue;
				this._viewModel.PropertyChangedWithBoolValue -= this.OnViewModelPropertyChangedWithValue;
				this._viewModel.PropertyChangedWithColorValue -= this.OnViewModelPropertyChangedWithValue;
				this._viewModel.PropertyChangedWithDoubleValue -= this.OnViewModelPropertyChangedWithValue;
				this._viewModel.PropertyChangedWithFloatValue -= this.OnViewModelPropertyChangedWithValue;
				this._viewModel.PropertyChangedWithIntValue -= this.OnViewModelPropertyChangedWithValue;
				this._viewModel.PropertyChangedWithUIntValue -= this.OnViewModelPropertyChangedWithValue;
				this._viewModel.PropertyChangedWithVec2Value -= this.OnViewModelPropertyChangedWithValue;
				return;
			}
			if (this._bindingList != null)
			{
				this._bindingList.ListChanged -= this.OnViewModelBindingListChanged;
			}
		}

		// Token: 0x06000033 RID: 51 RVA: 0x00002B58 File Offset: 0x00000D58
		public void ReleaseBindingWithChildren()
		{
			this.ReleaseBinding();
			for (int i = 0; i < this._children.Count; i++)
			{
				this._children[i].ReleaseBindingWithChildren();
			}
		}

		// Token: 0x06000034 RID: 52 RVA: 0x00002B94 File Offset: 0x00000D94
		internal void ClearEventHandlersWithChildren()
		{
			this.ClearEventHandlers();
			for (int i = 0; i < this._children.Count; i++)
			{
				this._children[i].ClearEventHandlersWithChildren();
			}
		}

		// Token: 0x06000035 RID: 53 RVA: 0x00002BD0 File Offset: 0x00000DD0
		private void ClearEventHandlers()
		{
			if (this._viewModel != null)
			{
				this._viewModel.PropertyChanged -= this.OnViewModelPropertyChanged;
				this._viewModel.PropertyChangedWithValue -= this.OnViewModelPropertyChangedWithValue;
				this._viewModel.PropertyChangedWithBoolValue -= this.OnViewModelPropertyChangedWithValue;
				this._viewModel.PropertyChangedWithColorValue -= this.OnViewModelPropertyChangedWithValue;
				this._viewModel.PropertyChangedWithDoubleValue -= this.OnViewModelPropertyChangedWithValue;
				this._viewModel.PropertyChangedWithFloatValue -= this.OnViewModelPropertyChangedWithValue;
				this._viewModel.PropertyChangedWithIntValue -= this.OnViewModelPropertyChangedWithValue;
				this._viewModel.PropertyChangedWithUIntValue -= this.OnViewModelPropertyChangedWithValue;
				this._viewModel.PropertyChangedWithVec2Value -= this.OnViewModelPropertyChangedWithValue;
				this._viewModel = null;
			}
			if (this._bindingList != null)
			{
				this.OnListReset();
				this._bindingList.ListChanged -= this.OnViewModelBindingListChanged;
				this._bindingList = null;
			}
			base.Target.EventFire -= this.OnEventFired;
			base.Target.PropertyChanged -= this.OnViewObjectPropertyChanged;
			base.Target.boolPropertyChanged -= this.OnViewObjectPropertyChanged;
			base.Target.ColorPropertyChanged -= this.OnViewObjectPropertyChanged;
			base.Target.doublePropertyChanged -= this.OnViewObjectPropertyChanged;
			base.Target.floatPropertyChanged -= this.OnViewObjectPropertyChanged;
			base.Target.intPropertyChanged -= this.OnViewObjectPropertyChanged;
			base.Target.uintPropertyChanged -= this.OnViewObjectPropertyChanged;
			base.Target.Vec2PropertyChanged -= this.OnViewObjectPropertyChanged;
			base.Target.Vector2PropertyChanged -= this.OnViewObjectPropertyChanged;
		}

		// Token: 0x06000036 RID: 54 RVA: 0x00002DD0 File Offset: 0x00000FD0
		public void BindData(string property, BindingPath path)
		{
			ViewBindDataInfo viewBindDataInfo = new ViewBindDataInfo(this, property, path);
			if (!this._bindDataInfosByPath.ContainsKey(path.Path))
			{
				this._bindDataInfosByPath.Add(path.Path, new List<ViewBindDataInfo>(1) { viewBindDataInfo });
			}
			else
			{
				this._bindDataInfosByPath[path.Path].Add(viewBindDataInfo);
			}
			if (this._viewModel != null)
			{
				object propertyValue = this._viewModel.GetPropertyValue(path.LastNode);
				this.SetData(viewBindDataInfo.Property, propertyValue);
			}
		}

		// Token: 0x06000037 RID: 55 RVA: 0x00002E58 File Offset: 0x00001058
		internal void BindCommand(string command, BindingPath path, string parameterValue = null)
		{
			ViewBindCommandInfo value = new ViewBindCommandInfo(this, command, path, parameterValue);
			if (!this._bindCommandInfos.ContainsKey(command))
			{
				this._bindCommandInfos.Add(command, value);
				return;
			}
			this._bindCommandInfos[command] = value;
		}

		// Token: 0x06000038 RID: 56 RVA: 0x00002E9C File Offset: 0x0000109C
		private void OnViewModelBindingListChanged(object sender, ListChangedEventArgs e)
		{
			switch (e.ListChangedType)
			{
			case ListChangedType.Reset:
				this.OnListReset();
				return;
			case ListChangedType.Sorted:
				this.OnListSorted();
				return;
			case ListChangedType.ItemAdded:
				this.OnItemAddedToList(e.NewIndex);
				return;
			case ListChangedType.ItemBeforeDeleted:
				this.OnBeforeItemRemovedFromList(e.NewIndex);
				break;
			case ListChangedType.ItemDeleted:
				this.OnItemRemovedFromList(e.NewIndex);
				return;
			case ListChangedType.ItemChanged:
				break;
			default:
				return;
			}
		}

		// Token: 0x06000039 RID: 57 RVA: 0x00002F03 File Offset: 0x00001103
		private void OnViewModelPropertyChangedWithValue(object sender, PropertyChangedWithValueEventArgs e)
		{
			this.OnPropertyChanged(e.PropertyName, e.Value);
		}

		// Token: 0x0600003A RID: 58 RVA: 0x00002F19 File Offset: 0x00001119
		private void OnViewModelPropertyChangedWithValue(object sender, PropertyChangedWithBoolValueEventArgs e)
		{
			this.OnPropertyChanged(e.PropertyName, e.Value);
		}

		// Token: 0x0600003B RID: 59 RVA: 0x00002F34 File Offset: 0x00001134
		private void OnViewModelPropertyChangedWithValue(object sender, PropertyChangedWithFloatValueEventArgs e)
		{
			this.OnPropertyChanged(e.PropertyName, e.Value);
		}

		// Token: 0x0600003C RID: 60 RVA: 0x00002F4F File Offset: 0x0000114F
		private void OnViewModelPropertyChangedWithValue(object sender, PropertyChangedWithColorValueEventArgs e)
		{
			this.OnPropertyChanged(e.PropertyName, e.Value);
		}

		// Token: 0x0600003D RID: 61 RVA: 0x00002F6A File Offset: 0x0000116A
		private void OnViewModelPropertyChangedWithValue(object sender, PropertyChangedWithDoubleValueEventArgs e)
		{
			this.OnPropertyChanged(e.PropertyName, e.Value);
		}

		// Token: 0x0600003E RID: 62 RVA: 0x00002F85 File Offset: 0x00001185
		private void OnViewModelPropertyChangedWithValue(object sender, PropertyChangedWithIntValueEventArgs e)
		{
			this.OnPropertyChanged(e.PropertyName, e.Value);
		}

		// Token: 0x0600003F RID: 63 RVA: 0x00002FA0 File Offset: 0x000011A0
		private void OnViewModelPropertyChangedWithValue(object sender, PropertyChangedWithUIntValueEventArgs e)
		{
			this.OnPropertyChanged(e.PropertyName, e.Value);
		}

		// Token: 0x06000040 RID: 64 RVA: 0x00002FBB File Offset: 0x000011BB
		private void OnViewModelPropertyChangedWithValue(object sender, PropertyChangedWithVec2ValueEventArgs e)
		{
			this.OnPropertyChanged(e.PropertyName, e.Value);
		}

		// Token: 0x06000041 RID: 65 RVA: 0x00002FD8 File Offset: 0x000011D8
		private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			object propertyValue = this._viewModel.GetPropertyValue(e.PropertyName);
			this.OnPropertyChanged(e.PropertyName, propertyValue);
		}

		// Token: 0x06000042 RID: 66 RVA: 0x00003004 File Offset: 0x00001204
		private object ConvertCommandParameter(object parameter)
		{
			object result = parameter;
			if (parameter is Widget)
			{
				Widget widget = (Widget)parameter;
				GauntletView gauntletView = this.GauntletMovie.FindViewOf(widget);
				if (gauntletView != null)
				{
					if (gauntletView._viewModel != null)
					{
						result = gauntletView._viewModel;
					}
					else
					{
						result = gauntletView._bindingList;
					}
				}
				else
				{
					result = null;
				}
			}
			return result;
		}

		// Token: 0x06000043 RID: 67 RVA: 0x00003050 File Offset: 0x00001250
		private void OnPropertyChanged(string propertyName, object value)
		{
			if (value is ViewModel || value is IMBBindingList)
			{
				MBStringBuilder mbstringBuilder = default(MBStringBuilder);
				mbstringBuilder.Initialize(16, "OnPropertyChanged");
				this.WriteViewModelPathToStringBuilder(ref mbstringBuilder);
				mbstringBuilder.Append<string>("\\");
				mbstringBuilder.Append<string>(propertyName);
				string path = mbstringBuilder.ToStringAndRelease();
				for (int i = 0; i < this._children.Count; i++)
				{
					GauntletView gauntletView = this._children[i];
					if (BindingPath.IsRelatedWithPathAsString(path, gauntletView.ViewModelPathString))
					{
						gauntletView.RefreshBindingWithChildren();
					}
				}
				return;
			}
			List<ViewBindDataInfo> list;
			if (this._bindDataInfosByPath.Count > 0 && this._bindDataInfosByPath.TryGetValue(propertyName, out list))
			{
				for (int j = 0; j < list.Count; j++)
				{
					ViewBindDataInfo viewBindDataInfo = list[j];
					this.SetData(viewBindDataInfo.Property, value);
				}
				return;
			}
			if (value == null)
			{
				for (int k = 0; k < this._children.Count; k++)
				{
					GauntletView gauntletView2 = this._children[k];
					BindingPath viewModelPath = gauntletView2._viewModelPath;
					if (((viewModelPath != null) ? viewModelPath.ToString() : null) == propertyName)
					{
						gauntletView2.RefreshBindingWithChildren();
					}
				}
			}
		}

		// Token: 0x06000044 RID: 68 RVA: 0x00003180 File Offset: 0x00001380
		private void OnCommand(string command, object[] args)
		{
			ViewBindCommandInfo viewBindCommandInfo;
			if (this._bindCommandInfos.TryGetValue(command, out viewBindCommandInfo))
			{
				object[] array;
				if (viewBindCommandInfo.Parameter != null)
				{
					array = new object[args.Length + 1];
					array[args.Length] = viewBindCommandInfo.Parameter;
				}
				else
				{
					array = new object[args.Length];
				}
				for (int i = 0; i < args.Length; i++)
				{
					object parameter = args[i];
					object obj = this.ConvertCommandParameter(parameter);
					array[i] = obj;
				}
				BindingPath parentPath = viewBindCommandInfo.Path.ParentPath;
				BindingPath bindingPath = this.ViewModelPath;
				if (parentPath != null)
				{
					bindingPath = bindingPath.Append(parentPath);
				}
				BindingPath path = bindingPath.Simplify();
				IViewModel viewModel = this.GauntletMovie.ViewModel;
				string lastNode = viewBindCommandInfo.Path.LastNode;
				ViewModel viewModel2 = viewModel.GetViewModelAtPath(path, viewBindCommandInfo.Owner.ItemTemplateUsageWithData != null && viewBindCommandInfo.Owner.ItemTemplateUsageWithData.ItemTemplateUsage != null) as ViewModel;
				if (viewModel2 != null)
				{
					viewModel2.ExecuteCommand(lastNode, array);
				}
			}
		}

		// Token: 0x06000045 RID: 69 RVA: 0x0000327C File Offset: 0x0000147C
		private List<ViewBindDataInfo> GetBindDataInfosOfProperty(string propertyName)
		{
			foreach (KeyValuePair<string, List<ViewBindDataInfo>> keyValuePair in this._bindDataInfosByPath)
			{
				List<ViewBindDataInfo> value = keyValuePair.Value;
				for (int i = 0; i < value.Count; i++)
				{
					if (value[i].Property == propertyName)
					{
						return value;
					}
				}
			}
			return null;
		}

		// Token: 0x06000046 RID: 70 RVA: 0x00003300 File Offset: 0x00001500
		private void OnListSorted()
		{
			List<GauntletView> list = new List<GauntletView>(this._items.Capacity);
			for (int i = 0; i < this._bindingList.Count; i++)
			{
				object bindingObject = this._bindingList[i];
				GauntletView item = this._items.Find((GauntletView gauntletView) => gauntletView._viewModel == bindingObject);
				list.Add(item);
			}
			this._items = list;
			for (int j = 0; j < this._items.Count; j++)
			{
				BindingPath path = new BindingPath(j);
				GauntletView gauntletView2 = this._items[j];
				gauntletView2.Target.SetSiblingIndex(j, false);
				gauntletView2.InitializeViewModelPath(path);
			}
		}

		// Token: 0x06000047 RID: 71 RVA: 0x000033B8 File Offset: 0x000015B8
		private void OnListReset()
		{
			GauntletView[] array = this._items.ToArray();
			this._items.Clear();
			foreach (GauntletView gauntletView in array)
			{
				base.Target.OnBeforeRemovedChild(gauntletView.Target);
				this._children.Remove(gauntletView);
				base.Target.RemoveChild(gauntletView.Target);
				gauntletView.ClearEventHandlersWithChildren();
			}
		}

		// Token: 0x06000048 RID: 72 RVA: 0x00003423 File Offset: 0x00001623
		private void OnItemAddedToList(int index)
		{
			this.AddItemToList(index).RefreshBindingWithChildren();
		}

		// Token: 0x06000049 RID: 73 RVA: 0x00003434 File Offset: 0x00001634
		private int ClampIndex(int index)
		{
			int num = MBMath.ClampInt(index, 0, this._items.Count);
			if (index != num)
			{
				Debug.FailedAssert("Invalid index for list", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI.Data\\GauntletView.cs", "ClampIndex", 641);
				index = num;
			}
			return index;
		}

		// Token: 0x0600004A RID: 74 RVA: 0x00003478 File Offset: 0x00001678
		private GauntletView AddItemToList(int index)
		{
			index = this.ClampIndex(index);
			for (int i = index; i < this._items.Count; i++)
			{
				this._items[i]._viewModelPath = new BindingPath(i + 1);
			}
			BindingPath path = new BindingPath(index);
			WidgetCreationData widgetCreationData = new WidgetCreationData(this.GauntletMovie.Context, this.GauntletMovie.WidgetFactory, base.Target);
			widgetCreationData.AddExtensionData(this.GauntletMovie);
			widgetCreationData.AddExtensionData(this);
			bool flag = false;
			GauntletView gauntletView;
			if (this._items.Count == 0 && this.ItemTemplateUsageWithData.FirstItemTemplate != null)
			{
				gauntletView = this.ItemTemplateUsageWithData.FirstItemTemplate.Instantiate(widgetCreationData, this.ItemTemplateUsageWithData.GivenParameters).GetGauntletView();
			}
			else if (this._items.Count == index && this._items.Count > 0 && this.ItemTemplateUsageWithData.LastItemTemplate != null)
			{
				BindingPath viewModelPath = this._items[this._items.Count - 1]._viewModelPath;
				GauntletView gauntletView2 = this.ItemTemplateUsageWithData.DefaultItemTemplate.Instantiate(widgetCreationData, this.ItemTemplateUsageWithData.GivenParameters).GetGauntletView();
				this._items.Insert(this._items.Count, gauntletView2);
				this.RemoveItemFromList(this._items.Count - 2);
				gauntletView2.InitializeViewModelPath(viewModelPath);
				gauntletView2.RefreshBindingWithChildren();
				flag = true;
				gauntletView = this.ItemTemplateUsageWithData.LastItemTemplate.Instantiate(widgetCreationData, this.ItemTemplateUsageWithData.GivenParameters).GetGauntletView();
			}
			else
			{
				gauntletView = this.ItemTemplateUsageWithData.DefaultItemTemplate.Instantiate(widgetCreationData, this.ItemTemplateUsageWithData.GivenParameters).GetGauntletView();
			}
			gauntletView.InitializeViewModelPath(path);
			this._items.Insert(index, gauntletView);
			for (int j = (flag ? (index - 1) : index); j < this._items.Count; j++)
			{
				this._items[j].Target.SetSiblingIndex(j, flag);
			}
			return gauntletView;
		}

		// Token: 0x0600004B RID: 75 RVA: 0x0000368C File Offset: 0x0000188C
		private void OnItemRemovedFromList(int index)
		{
			this.RemoveItemFromList(index);
		}

		// Token: 0x0600004C RID: 76 RVA: 0x00003698 File Offset: 0x00001898
		private void RemoveItemFromList(int index)
		{
			index = this.ClampIndex(index);
			GauntletView gauntletView = this._items[index];
			this._items.RemoveAt(index);
			this._children.Remove(gauntletView);
			base.Target.RemoveChild(gauntletView.Target);
			gauntletView.ClearEventHandlersWithChildren();
			for (int i = index; i < this._items.Count; i++)
			{
				this._items[i].Target.SetSiblingIndex(i, false);
			}
			BindingPath viewModelPath = gauntletView._viewModelPath;
			for (int j = index; j < this._items.Count; j++)
			{
				GauntletView gauntletView2 = this._items[j];
				BindingPath viewModelPath2 = gauntletView2._viewModelPath;
				gauntletView2._viewModelPath = viewModelPath;
				viewModelPath = viewModelPath2;
			}
		}

		// Token: 0x0600004D RID: 77 RVA: 0x00003753 File Offset: 0x00001953
		private void OnBeforeItemRemovedFromList(int index)
		{
			this.PreviewRemoveItemFromList(index);
		}

		// Token: 0x0600004E RID: 78 RVA: 0x0000375C File Offset: 0x0000195C
		private void PreviewRemoveItemFromList(int index)
		{
			index = this.ClampIndex(index);
			GauntletView gauntletView = this._items[index];
			base.Target.OnBeforeRemovedChild(gauntletView.Target);
		}

		// Token: 0x0600004F RID: 79 RVA: 0x00003790 File Offset: 0x00001990
		private void SetData(string path, object value)
		{
			WidgetExtensions.SetWidgetAttribute(this.GauntletMovie.Context, base.Target, path, value);
		}

		// Token: 0x06000050 RID: 80 RVA: 0x000037AC File Offset: 0x000019AC
		private void OnViewPropertyChanged(string propertyName, object value)
		{
			if (this._viewModel != null)
			{
				List<ViewBindDataInfo> bindDataInfosOfProperty = this.GetBindDataInfosOfProperty(propertyName);
				if (bindDataInfosOfProperty != null)
				{
					for (int i = 0; i < bindDataInfosOfProperty.Count; i++)
					{
						ViewBindDataInfo viewBindDataInfo = bindDataInfosOfProperty[i];
						if (viewBindDataInfo.IsValid)
						{
							this._viewModel.SetPropertyValue(viewBindDataInfo.Path.LastNode, value);
						}
					}
				}
			}
		}

		// Token: 0x17000010 RID: 16
		// (get) Token: 0x06000051 RID: 81 RVA: 0x00003804 File Offset: 0x00001A04
		public string DisplayName
		{
			get
			{
				string text = "";
				if (base.Target != null)
				{
					text = text + base.Target.Id + "!" + base.Target.Tag.ToString();
				}
				if (this.ViewModelPath != null)
				{
					text = text + "@" + this.ViewModelPath.Path;
				}
				return text;
			}
		}

		// Token: 0x06000052 RID: 82 RVA: 0x0000386C File Offset: 0x00001A6C
		private void OnViewObjectPropertyChanged(PropertyOwnerObject propertyOwnerObject, string propertyName, object value)
		{
			this.OnViewPropertyChanged(propertyName, value);
		}

		// Token: 0x06000053 RID: 83 RVA: 0x00003876 File Offset: 0x00001A76
		private void OnViewObjectPropertyChanged(PropertyOwnerObject propertyOwnerObject, string propertyName, bool value)
		{
			this.OnViewPropertyChanged(propertyName, value);
		}

		// Token: 0x06000054 RID: 84 RVA: 0x00003885 File Offset: 0x00001A85
		private void OnViewObjectPropertyChanged(PropertyOwnerObject propertyOwnerObject, string propertyName, float value)
		{
			this.OnViewPropertyChanged(propertyName, value);
		}

		// Token: 0x06000055 RID: 85 RVA: 0x00003894 File Offset: 0x00001A94
		private void OnViewObjectPropertyChanged(PropertyOwnerObject propertyOwnerObject, string propertyName, Color value)
		{
			this.OnViewPropertyChanged(propertyName, value);
		}

		// Token: 0x06000056 RID: 86 RVA: 0x000038A3 File Offset: 0x00001AA3
		private void OnViewObjectPropertyChanged(PropertyOwnerObject propertyOwnerObject, string propertyName, double value)
		{
			this.OnViewPropertyChanged(propertyName, value);
		}

		// Token: 0x06000057 RID: 87 RVA: 0x000038B2 File Offset: 0x00001AB2
		private void OnViewObjectPropertyChanged(PropertyOwnerObject propertyOwnerObject, string propertyName, int value)
		{
			this.OnViewPropertyChanged(propertyName, value);
		}

		// Token: 0x06000058 RID: 88 RVA: 0x000038C1 File Offset: 0x00001AC1
		private void OnViewObjectPropertyChanged(PropertyOwnerObject propertyOwnerObject, string propertyName, uint value)
		{
			this.OnViewPropertyChanged(propertyName, value);
		}

		// Token: 0x06000059 RID: 89 RVA: 0x000038D0 File Offset: 0x00001AD0
		private void OnViewObjectPropertyChanged(PropertyOwnerObject propertyOwnerObject, string propertyName, Vec2 value)
		{
			this.OnViewPropertyChanged(propertyName, value);
		}

		// Token: 0x0600005A RID: 90 RVA: 0x000038DF File Offset: 0x00001ADF
		private void OnViewObjectPropertyChanged(PropertyOwnerObject propertyOwnerObject, string propertyName, Vector2 value)
		{
			this.OnViewPropertyChanged(propertyName, value);
		}

		// Token: 0x0400000E RID: 14
		private BindingPath _viewModelPath;

		// Token: 0x04000010 RID: 16
		private Dictionary<string, List<ViewBindDataInfo>> _bindDataInfosByPath;

		// Token: 0x04000011 RID: 17
		private Dictionary<string, ViewBindCommandInfo> _bindCommandInfos;

		// Token: 0x04000012 RID: 18
		private IViewModel _viewModel;

		// Token: 0x04000013 RID: 19
		private IMBBindingList _bindingList;

		// Token: 0x04000014 RID: 20
		private MBList<GauntletView> _children;

		// Token: 0x04000015 RID: 21
		private List<GauntletView> _items;
	}
}
