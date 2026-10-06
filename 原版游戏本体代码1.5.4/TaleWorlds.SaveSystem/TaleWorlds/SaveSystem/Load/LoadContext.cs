using System;
using System.Collections.Generic;
using System.Globalization;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem.Definition;

namespace TaleWorlds.SaveSystem.Load;

public class LoadContext
{
	private int _objectCount;

	private int _stringCount;

	private int _containerCount;

	private ObjectHeaderLoadData[] _objectHeaderLoadDatas;

	private ContainerHeaderLoadData[] _containerHeaderLoadDatas;

	private string[] _strings;

	public static bool EnableLoadStatistics => false;

	public object RootObject { get; private set; }

	public DefinitionContext DefinitionContext { get; private set; }

	public ISaveDriver Driver { get; private set; }

	public LoadContext(DefinitionContext definitionContext, ISaveDriver driver)
	{
		DefinitionContext = definitionContext;
		_objectHeaderLoadDatas = null;
		_containerHeaderLoadDatas = null;
		_strings = null;
		Driver = driver;
	}

	internal static ObjectLoadData CreateLoadData(LoadData loadData, int i, ObjectHeaderLoadData header)
	{
		ArchiveDeserializer archiveDeserializer = new ArchiveDeserializer();
		archiveDeserializer.LoadFrom(loadData.GameData.ObjectData[i]);
		SaveEntryFolder rootFolder = archiveDeserializer.RootFolder;
		ObjectLoadData objectLoadData = new ObjectLoadData(header);
		SaveEntryFolder childFolder = rootFolder.GetChildFolder(new FolderId(i, SaveFolderExtension.Object));
		objectLoadData.InitializeReaders(childFolder);
		objectLoadData.FillCreatedObject();
		objectLoadData.Read();
		objectLoadData.FillObject();
		return objectLoadData;
	}

	public bool Load(LoadData loadData, bool loadAsLateInitialize)
	{
		bool flag = false;
		try
		{
			using (new PerformanceTestBlock("LoadContext::Load Headers"))
			{
				using (new PerformanceTestBlock("LoadContext::Load And Create Header"))
				{
					ArchiveDeserializer archiveDeserializer = new ArchiveDeserializer();
					archiveDeserializer.LoadFrom(loadData.GameData.Header);
					SaveEntryFolder headerRootFolder = archiveDeserializer.RootFolder;
					BinaryReader binaryReader = headerRootFolder.GetEntry(new EntryId(-1, SaveEntryExtension.Config)).GetBinaryReader();
					_objectCount = binaryReader.ReadInt();
					_stringCount = binaryReader.ReadInt();
					_containerCount = binaryReader.ReadInt();
					_objectHeaderLoadDatas = new ObjectHeaderLoadData[_objectCount];
					_containerHeaderLoadDatas = new ContainerHeaderLoadData[_containerCount];
					_strings = new string[_stringCount];
					if (EnableLoadStatistics)
					{
						for (int i = 0; i < _objectCount; i++)
						{
							ObjectHeaderLoadData objectHeaderLoadData = new ObjectHeaderLoadData(this, i);
							SaveEntryFolder childFolder = headerRootFolder.GetChildFolder(new FolderId(i, SaveFolderExtension.Object));
							objectHeaderLoadData.InitialieReaders(childFolder);
							_objectHeaderLoadDatas[i] = objectHeaderLoadData;
						}
						for (int j = 0; j < _containerCount; j++)
						{
							ContainerHeaderLoadData containerHeaderLoadData = new ContainerHeaderLoadData(this, j);
							SaveEntryFolder childFolder2 = headerRootFolder.GetChildFolder(new FolderId(j, SaveFolderExtension.Container));
							containerHeaderLoadData.InitialieReaders(childFolder2);
							_containerHeaderLoadDatas[j] = containerHeaderLoadData;
						}
					}
					else
					{
						TWParallel.ForWithoutRenderThread(0, _objectCount, delegate(int startInclusive, int endExclusive)
						{
							for (int k = startInclusive; k < endExclusive; k++)
							{
								ObjectHeaderLoadData objectHeaderLoadData5 = new ObjectHeaderLoadData(this, k);
								SaveEntryFolder childFolder4 = headerRootFolder.GetChildFolder(new FolderId(k, SaveFolderExtension.Object));
								objectHeaderLoadData5.InitialieReaders(childFolder4);
								_objectHeaderLoadDatas[k] = objectHeaderLoadData5;
							}
						});
						TWParallel.ForWithoutRenderThread(0, _containerCount, delegate(int startInclusive, int endExclusive)
						{
							for (int k = startInclusive; k < endExclusive; k++)
							{
								ContainerHeaderLoadData containerHeaderLoadData3 = new ContainerHeaderLoadData(this, k);
								SaveEntryFolder childFolder4 = headerRootFolder.GetChildFolder(new FolderId(k, SaveFolderExtension.Container));
								containerHeaderLoadData3.InitialieReaders(childFolder4);
								_containerHeaderLoadDatas[k] = containerHeaderLoadData3;
							}
						});
					}
				}
				using (new PerformanceTestBlock("LoadContext::Create Objects"))
				{
					ObjectHeaderLoadData[] objectHeaderLoadDatas = _objectHeaderLoadDatas;
					foreach (ObjectHeaderLoadData objectHeaderLoadData2 in objectHeaderLoadDatas)
					{
						objectHeaderLoadData2.CreateObject();
						if (objectHeaderLoadData2.Id == 0)
						{
							RootObject = objectHeaderLoadData2.Target;
						}
					}
					ContainerHeaderLoadData[] containerHeaderLoadDatas = _containerHeaderLoadDatas;
					foreach (ContainerHeaderLoadData containerHeaderLoadData2 in containerHeaderLoadDatas)
					{
						if (containerHeaderLoadData2.GetObjectTypeDefinition())
						{
							containerHeaderLoadData2.CreateObject();
						}
					}
				}
			}
			GC.Collect();
			using (new PerformanceTestBlock("LoadContext::Load Strings"))
			{
				ArchiveDeserializer archiveDeserializer2 = new ArchiveDeserializer();
				archiveDeserializer2.LoadFrom(loadData.GameData.Strings);
				for (int num2 = 0; num2 < _stringCount; num2++)
				{
					string text = LoadString(archiveDeserializer2, num2);
					_strings[num2] = text;
				}
			}
			GC.Collect();
			using (new PerformanceTestBlock("LoadContext::Resolve Objects"))
			{
				for (int num3 = 0; num3 < _objectHeaderLoadDatas.Length; num3++)
				{
					ObjectHeaderLoadData objectHeaderLoadData3 = _objectHeaderLoadDatas[num3];
					TypeDefinition typeDefinition = objectHeaderLoadData3.TypeDefinition;
					if (typeDefinition != null)
					{
						object loadedObject = objectHeaderLoadData3.LoadedObject;
						if (typeDefinition.CheckIfRequiresAdvancedResolving(loadedObject))
						{
							ObjectLoadData objectLoadData = CreateLoadData(loadData, num3, objectHeaderLoadData3);
							objectHeaderLoadData3.AdvancedResolveObject(loadData.MetaData, objectLoadData);
						}
						else
						{
							objectHeaderLoadData3.ResolveObject();
						}
					}
				}
			}
			GC.Collect();
			using (new PerformanceTestBlock("LoadContext::Load Object Datas"))
			{
				if (EnableLoadStatistics)
				{
					for (int num4 = 0; num4 < _objectCount; num4++)
					{
						ObjectHeaderLoadData objectHeaderLoadData4 = _objectHeaderLoadDatas[num4];
						if (objectHeaderLoadData4.Target == objectHeaderLoadData4.LoadedObject)
						{
							CreateLoadData(loadData, num4, objectHeaderLoadData4);
						}
					}
				}
				else
				{
					TWParallel.ForWithoutRenderThread(0, _objectCount, delegate(int startInclusive, int endExclusive)
					{
						for (int k = startInclusive; k < endExclusive; k++)
						{
							ObjectHeaderLoadData objectHeaderLoadData5 = _objectHeaderLoadDatas[k];
							if (objectHeaderLoadData5.Target == objectHeaderLoadData5.LoadedObject)
							{
								CreateLoadData(loadData, k, objectHeaderLoadData5);
							}
						}
					});
				}
			}
			using (new PerformanceTestBlock("LoadContext::Load Container Datas"))
			{
				if (EnableLoadStatistics)
				{
					for (int num5 = 0; num5 < _containerCount; num5++)
					{
						byte[] binaryArchive = loadData.GameData.ContainerData[num5];
						ArchiveDeserializer archiveDeserializer3 = new ArchiveDeserializer();
						archiveDeserializer3.LoadFrom(binaryArchive);
						SaveEntryFolder rootFolder = archiveDeserializer3.RootFolder;
						ContainerLoadData containerLoadData = new ContainerLoadData(_containerHeaderLoadDatas[num5]);
						SaveEntryFolder childFolder3 = rootFolder.GetChildFolder(new FolderId(num5, SaveFolderExtension.Container));
						containerLoadData.InitializeReaders(childFolder3);
						containerLoadData.FillCreatedObject();
						containerLoadData.Read();
						containerLoadData.FillObject();
					}
				}
				else
				{
					TWParallel.ForWithoutRenderThread(0, _containerCount, delegate(int startInclusive, int endExclusive)
					{
						for (int k = startInclusive; k < endExclusive; k++)
						{
							byte[] binaryArchive2 = loadData.GameData.ContainerData[k];
							ArchiveDeserializer archiveDeserializer4 = new ArchiveDeserializer();
							archiveDeserializer4.LoadFrom(binaryArchive2);
							SaveEntryFolder rootFolder2 = archiveDeserializer4.RootFolder;
							ContainerLoadData containerLoadData2 = new ContainerLoadData(_containerHeaderLoadDatas[k]);
							SaveEntryFolder childFolder4 = rootFolder2.GetChildFolder(new FolderId(k, SaveFolderExtension.Container));
							containerLoadData2.InitializeReaders(childFolder4);
							containerLoadData2.FillCreatedObject();
							containerLoadData2.Read();
							containerLoadData2.FillObject();
						}
					});
				}
			}
			GC.Collect();
			if (!loadAsLateInitialize)
			{
				LoadCallbackInitializator loadCallbackInitializator = CreateLoadCallbackInitializator(loadData);
				loadCallbackInitializator.InitializeObjects();
				loadCallbackInitializator.AfterInitializeObjects();
			}
			return true;
		}
		catch (Exception ex)
		{
			Debug.Print(ex.Message);
			return false;
		}
	}

	internal LoadCallbackInitializator CreateLoadCallbackInitializator(LoadData loadData)
	{
		return new LoadCallbackInitializator(loadData, _objectHeaderLoadDatas, _objectCount);
	}

	private static string LoadString(ArchiveDeserializer saveArchive, int id)
	{
		return saveArchive.RootFolder.GetChildFolder(new FolderId(-1, SaveFolderExtension.Strings)).GetEntry(new EntryId(id, SaveEntryExtension.Txt)).GetBinaryReader()
			.ReadString();
	}

	public static bool TryConvertType(Type sourceType, Type targetType, ref object data)
	{
		if (isNum(sourceType) && isNum(targetType))
		{
			try
			{
				data = Convert.ChangeType(data, targetType);
				return true;
			}
			catch
			{
				return false;
			}
		}
		if (isNum(sourceType) && targetType == typeof(string))
		{
			if (isInt(sourceType))
			{
				data = Convert.ToInt64(data).ToString();
			}
			else if (isFloat(sourceType))
			{
				data = Convert.ToDouble(data).ToString(CultureInfo.InvariantCulture);
			}
			return true;
		}
		if (sourceType.IsGenericType && sourceType.GetGenericTypeDefinition() == typeof(List<>) && targetType.IsGenericType)
		{
			_ = targetType.GetGenericTypeDefinition() == typeof(MBList<>);
		}
		return false;
		static bool isFloat(Type type)
		{
			if (!(type == typeof(double)))
			{
				return type == typeof(float);
			}
			return true;
		}
		static bool isInt(Type type)
		{
			if (!(type == typeof(long)) && !(type == typeof(int)) && !(type == typeof(short)) && !(type == typeof(ulong)) && !(type == typeof(uint)))
			{
				return type == typeof(ushort);
			}
			return true;
		}
		static bool isNum(Type type)
		{
			if (!isInt(type))
			{
				return isFloat(type);
			}
			return true;
		}
	}

	public ObjectHeaderLoadData GetObjectWithId(int id)
	{
		ObjectHeaderLoadData result = null;
		if (id != -1)
		{
			result = _objectHeaderLoadDatas[id];
		}
		return result;
	}

	public ContainerHeaderLoadData GetContainerWithId(int id)
	{
		ContainerHeaderLoadData result = null;
		if (id != -1)
		{
			result = _containerHeaderLoadDatas[id];
		}
		return result;
	}

	public string GetStringWithId(int id)
	{
		string result = null;
		if (id != -1)
		{
			result = _strings[id];
		}
		return result;
	}
}
