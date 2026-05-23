using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Diamond.Lobby;

public abstract class MultiplayerLocalDataContainer<T> where T : MultiplayerLocalData
{
	private enum OperationType
	{
		Add,
		Insert,
		Remove
	}

	private struct ContainerOperation
	{
		public readonly OperationType OperationType;

		public readonly T Item;

		public readonly int Index;

		private ContainerOperation(OperationType type, T item, int index)
		{
			OperationType = type;
			Item = item;
			Index = index;
		}

		public static ContainerOperation CreateAsAdd(T item)
		{
			return new ContainerOperation(OperationType.Add, item, -1);
		}

		public static ContainerOperation CreateAsRemove(T item)
		{
			return new ContainerOperation(OperationType.Remove, item, -1);
		}

		public static ContainerOperation CreateAsInsert(T item, int index)
		{
			return new ContainerOperation(OperationType.Insert, item, index);
		}
	}

	private class ContainerOperationComparer : IComparer<ContainerOperation>
	{
		public int Compare(ContainerOperation x, ContainerOperation y)
		{
			return x.OperationType.CompareTo(y.OperationType);
		}
	}

	private readonly string _saveDirectoryName;

	private readonly string _saveFileName;

	private readonly List<ContainerOperation> _operationQueue;

	private readonly List<T> _dataList;

	private bool _isFileDirty;

	private bool _isCacheDirty;

	public MultiplayerLocalDataContainer()
	{
		_operationQueue = new List<ContainerOperation>();
		_dataList = new List<T>();
		_saveDirectoryName = GetSaveDirectoryName();
		_saveFileName = GetSaveFileName();
		_isCacheDirty = true;
	}

	protected abstract string GetSaveDirectoryName();

	protected abstract string GetSaveFileName();

	public void AddEntry(T item)
	{
		lock (_operationQueue)
		{
			ContainerOperation item2 = ContainerOperation.CreateAsAdd(item);
			_operationQueue.Add(item2);
		}
	}

	public void InsertEntry(T item, int index)
	{
		lock (_operationQueue)
		{
			ContainerOperation item2 = ContainerOperation.CreateAsInsert(item, index);
			_operationQueue.Add(item2);
		}
	}

	public void RemoveEntry(T item)
	{
		lock (_operationQueue)
		{
			ContainerOperation item2 = ContainerOperation.CreateAsRemove(item);
			_operationQueue.Add(item2);
		}
	}

	public MBReadOnlyList<T> GetEntries()
	{
		return new MBReadOnlyList<T>(_dataList);
	}

	internal async Task Tick(float dt)
	{
		if (_isCacheDirty)
		{
			await LoadFileAux();
			_isCacheDirty = false;
		}
		while (_operationQueue.Count > 0)
		{
			ContainerOperation operation = _operationQueue[0];
			HandleOperation(operation);
			_operationQueue.RemoveAt(0);
		}
		if (_isFileDirty)
		{
			await SaveFileAux();
			_isFileDirty = false;
		}
	}

	private void HandleOperation(ContainerOperation operation)
	{
		switch (operation.OperationType)
		{
		case OperationType.Add:
			AddEntryAux(operation.Item);
			break;
		case OperationType.Remove:
			RemoveEntryAux(operation.Item);
			break;
		case OperationType.Insert:
			InsertEntryAux(operation.Item, operation.Index);
			break;
		}
	}

	private void AddEntryAux(T item)
	{
		OnBeforeAddEntry(item, out var canAddEntry);
		if (!canAddEntry)
		{
			return;
		}
		bool flag = false;
		foreach (T data in _dataList)
		{
			if (data.HasSameContentWith(item))
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			_dataList.Add(item);
		}
		else
		{
			Debug.FailedAssert("Item is already in container: " + item, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Diamond\\Lobby\\MultiplayerLocalDataManager.cs", "AddEntryAux", 234);
		}
		_isFileDirty = true;
	}

	private void InsertEntryAux(T item, int index)
	{
		OnBeforeAddEntry(item, out var canAddEntry);
		if (!canAddEntry)
		{
			return;
		}
		bool flag = false;
		foreach (T data in _dataList)
		{
			if (data.HasSameContentWith(item))
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			if (index >= 0 && index < _dataList.Count)
			{
				_dataList.Insert(index, item);
			}
			else
			{
				_dataList.Add(item);
			}
		}
		else
		{
			Debug.FailedAssert("Item is already in container: " + item, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Diamond\\Lobby\\MultiplayerLocalDataManager.cs", "InsertEntryAux", 272);
		}
		_isFileDirty = true;
	}

	protected virtual void OnBeforeAddEntry(T item, out bool canAddEntry)
	{
		canAddEntry = true;
	}

	private void RemoveEntryAux(T item)
	{
		OnBeforeRemoveEntry(item, out var canRemoveEntry);
		if (!canRemoveEntry)
		{
			return;
		}
		int count = _dataList.Count;
		for (int num = _dataList.Count - 1; num >= 0; num--)
		{
			if (_dataList[num].HasSameContentWith(item))
			{
				_dataList.Remove(item);
			}
		}
		if (count == _dataList.Count)
		{
			Debug.FailedAssert("Item is not in container: " + item, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Diamond\\Lobby\\MultiplayerLocalDataManager.cs", "RemoveEntryAux", 304);
		}
		_isFileDirty = true;
	}

	protected virtual void OnBeforeRemoveEntry(T item, out bool canRemoveEntry)
	{
		canRemoveEntry = true;
	}

	private PlatformFilePath GetDataFilePath()
	{
		return new PlatformFilePath(new PlatformDirectoryPath(PlatformFileType.User, _saveDirectoryName), _saveFileName);
	}

	private async Task SaveFileAux()
	{
		try
		{
			await FileHelper.SaveFileAsync(GetDataFilePath(), Common.SerializeObjectAsJson(_dataList));
		}
		catch (Exception ex)
		{
			Debug.FailedAssert("An exception occured while trying to save " + GetType().Name + " data: " + ex.Message, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Diamond\\Lobby\\MultiplayerLocalDataManager.cs", "SaveFileAux", 331);
		}
	}

	private async Task LoadFileAux()
	{
		PlatformFilePath oldFilePath = GetCompatibilityFilePath();
		if (FileHelper.FileExists(oldFilePath))
		{
			string text = await FileHelper.GetFileContentStringAsync(oldFilePath);
			FileHelper.DeleteFile(oldFilePath);
			if (!string.IsNullOrEmpty(text))
			{
				_dataList.Clear();
				List<T> list = null;
				try
				{
					list = JsonConvert.DeserializeObject<List<T>>(text);
				}
				catch
				{
					try
					{
						list = DeserializeInCompatibilityMode(text);
					}
					catch
					{
						Debug.FailedAssert("Failed to load old data in compatibility mode", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Diamond\\Lobby\\MultiplayerLocalDataManager.cs", "LoadFileAux", 362);
					}
				}
				if (list != null)
				{
					foreach (T item in list)
					{
						_dataList.Add(item);
					}
				}
				_isFileDirty = true;
				return;
			}
		}
		PlatformFilePath dataFilePath = GetDataFilePath();
		if (!FileHelper.FileExists(dataFilePath))
		{
			return;
		}
		string text2 = await FileHelper.GetFileContentStringAsync(dataFilePath);
		if (string.IsNullOrEmpty(text2))
		{
			return;
		}
		_dataList.Clear();
		List<T> list2 = null;
		try
		{
			list2 = JsonConvert.DeserializeObject<List<T>>(text2);
		}
		catch
		{
			try
			{
				list2 = DeserializeInCompatibilityMode(text2);
				_isFileDirty = true;
			}
			catch
			{
				Debug.FailedAssert("Failed to load file in compatibility mode", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Diamond\\Lobby\\MultiplayerLocalDataManager.cs", "LoadFileAux", 403);
			}
		}
		if (list2 == null)
		{
			return;
		}
		foreach (T item2 in list2)
		{
			_dataList.Add(item2);
		}
	}

	protected virtual PlatformFilePath GetCompatibilityFilePath()
	{
		return new PlatformFilePath(new PlatformDirectoryPath(PlatformFileType.User, "DataOld"), "TmpData");
	}

	protected virtual List<T> DeserializeInCompatibilityMode(string serializedJson)
	{
		return null;
	}
}
