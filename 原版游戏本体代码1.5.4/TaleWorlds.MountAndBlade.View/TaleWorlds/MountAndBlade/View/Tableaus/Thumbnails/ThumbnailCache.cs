using System;
using System.Collections.Generic;
using System.Text;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;

public abstract class ThumbnailCache<T> : IThumbnailCache where T : ThumbnailCreationData
{
	protected int _capacity;

	protected ThumbnailCreatorView _thumbnailCreatorView;

	protected Dictionary<string, ThumbnailCacheNode> _map;

	protected NodeComparer _nodeComparer = new NodeComparer();

	protected Dictionary<string, RenderCallbackCollection> _renderCallbacks;

	public int Count { get; private set; }

	public int RenderCallbackCount => _renderCallbacks.Count;

	public ThumbnailCache(int capacity)
	{
		_capacity = capacity;
		_map = new Dictionary<string, ThumbnailCacheNode>(capacity);
		_renderCallbacks = new Dictionary<string, RenderCallbackCollection>();
		Count = 0;
	}

	protected virtual void OnInitialize()
	{
	}

	protected virtual void OnFinalize()
	{
	}

	protected virtual void OnTick(float dt)
	{
	}

	protected virtual void OnClear()
	{
	}

	protected virtual void OnImguiTick()
	{
	}

	protected virtual void OnRequestCancelled(string renderId)
	{
	}

	protected abstract TextureCreationInfo OnCreateTexture(T thumbnailCreationData);

	protected abstract bool OnReleaseTexture(T thumbnailCreationData);

	bool IThumbnailCache.OnThumbnailRenderCompleted(string renderId, Texture renderTarget)
	{
		if (_renderCallbacks.ContainsKey(renderId))
		{
			foreach (Action<Texture> setAction in _renderCallbacks[renderId].SetActions)
			{
				setAction?.Invoke(renderTarget);
			}
			_renderCallbacks.Remove(renderId);
			return true;
		}
		return false;
	}

	public TextureCreationInfo CreateTexture(ThumbnailCreationData thumbnailCreationData)
	{
		if (thumbnailCreationData is T thumbnailCreationData2)
		{
			TextureCreationInfo result = OnCreateTexture(thumbnailCreationData2);
			thumbnailCreationData.IsProcessed = true;
			return result;
		}
		return default(TextureCreationInfo);
	}

	public bool ReleaseTexture(ThumbnailCreationData thumbnailCreationData)
	{
		bool result = false;
		if (thumbnailCreationData is T thumbnailCreationData2 && OnReleaseTexture(thumbnailCreationData2))
		{
			result = true;
		}
		return result;
	}

	void IThumbnailCache.Initialize(ThumbnailCreatorView thumbnailCreatorView)
	{
		_thumbnailCreatorView = thumbnailCreatorView;
		OnInitialize();
	}

	void IThumbnailCache.Destroy()
	{
		OnFinalize();
		_capacity = 0;
		Count = 0;
		_thumbnailCreatorView = null;
		_nodeComparer = null;
		_map.Clear();
		_map = null;
	}

	void IThumbnailCache.Clear(bool releaseImmediately)
	{
		foreach (KeyValuePair<string, ThumbnailCacheNode> item in _map)
		{
			string key = item.Key;
			RemoveRenderCallbacksForKey(key);
			_thumbnailCreatorView.CancelRequest(key);
			if (releaseImmediately)
			{
				item.Value.Value?.ReleaseImmediately();
			}
			else
			{
				item.Value.Value?.Release();
			}
			item.Value.Value = null;
			Count--;
			OnRequestCancelled(key);
		}
		_map.Clear();
		_renderCallbacks.Clear();
		Count = 0;
		OnClear();
	}

	bool IThumbnailCache.GetValue(string key, out Texture texture)
	{
		texture = null;
		if (_map.TryGetValue(key, out var value))
		{
			value.FrameNo = Utilities.EngineFrameNo;
			texture = value.Value;
			return true;
		}
		return false;
	}

	void IThumbnailCache.Add(string key, Texture value)
	{
		if (_map.TryGetValue(key, out var value2))
		{
			if (value2.Value != null && value2.Value != value)
			{
				if (value2.Value != null && !value2.Value.IsReleased)
				{
					Debug.FailedAssert("Setting a texture to a node without clearing the old one", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.View\\Tableaus\\Thumbnails\\ThumbnailCache.cs", "Add", 266);
				}
				ThumbnailCacheNode value3 = new ThumbnailCacheNode(key, value, Utilities.EngineFrameNo);
				_map[key] = value3;
				Count++;
			}
			else
			{
				value2.Value = value;
				value2.FrameNo = Utilities.EngineFrameNo;
			}
		}
		else
		{
			ThumbnailCacheNode value4 = new ThumbnailCacheNode(key, value, Utilities.EngineFrameNo);
			_map[key] = value4;
			Count++;
		}
	}

	bool IThumbnailCache.AddReference(string key)
	{
		if (_map.TryGetValue(key, out var value))
		{
			value.ReferenceCount++;
			return true;
		}
		return false;
	}

	bool IThumbnailCache.RemoveReference(string key)
	{
		if (_map.TryGetValue(key, out var value))
		{
			value.ReferenceCount--;
			if (value.ReferenceCount < 0)
			{
				Debug.FailedAssert("Thumbnail cache reference count is below 0", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.View\\Tableaus\\Thumbnails\\ThumbnailCache.cs", "RemoveReference", 304);
			}
			return true;
		}
		return false;
	}

	void IThumbnailCache.ClearUnusedCache()
	{
		if (Count <= _capacity)
		{
			return;
		}
		int num = Count - Math.Max(_capacity / 2, 1);
		List<ThumbnailCacheNode> list = new List<ThumbnailCacheNode>();
		List<string> list2 = new List<string>();
		foreach (KeyValuePair<string, ThumbnailCacheNode> item in _map)
		{
			if (item.Value.ReferenceCount <= 0)
			{
				list.Add(item.Value);
				list2.Add(item.Key);
				num--;
			}
			if (num == 0)
			{
				break;
			}
		}
		for (int i = 0; i < list.Count; i++)
		{
			RemoveThumbnailCacheNode(list[i]);
		}
	}

	void IThumbnailCache.Tick(float dt)
	{
		if (Count > _capacity)
		{
			int num = Count - Math.Max(_capacity / 2, 1);
			List<ThumbnailCacheNode> list = new List<ThumbnailCacheNode>();
			List<string> list2 = new List<string>();
			foreach (KeyValuePair<string, ThumbnailCacheNode> item in _map)
			{
				if (item.Value.ReferenceCount <= 0)
				{
					list.Add(item.Value);
					list2.Add(item.Key);
					num--;
				}
				if (num == 0)
				{
					break;
				}
			}
			for (int i = 0; i < list.Count; i++)
			{
				RemoveThumbnailCacheNode(list[i]);
			}
		}
		OnTick(dt);
	}

	protected void RemoveThumbnailCacheNode(ThumbnailCacheNode node, bool releaseTexture = true)
	{
		string key = node.Key;
		RemoveRenderCallbacksForKey(key);
		_map.Remove(key);
		_thumbnailCreatorView.CancelRequest(key);
		if (releaseTexture && node.Value != null)
		{
			node.Value.Release();
			node.Value = null;
		}
		Count--;
		OnRequestCancelled(key);
	}

	private void RemoveRenderCallbacksForKey(string renderId)
	{
		if (_renderCallbacks.TryGetValue(renderId, out var value))
		{
			for (int i = 0; i < value.CancelActions.Count; i++)
			{
				value.CancelActions[i]?.Invoke();
			}
			value.SetActions.Clear();
			_renderCallbacks.Remove(renderId);
		}
	}

	void IThumbnailCache.PrintToImgui()
	{
		int totalMemorySize = GetTotalMemorySize();
		Imgui.Text(GetType().Name);
		Imgui.NextColumn();
		Imgui.Text(Count.ToString());
		Imgui.NextColumn();
		Imgui.Text(ByteWidthToString(totalMemorySize));
		Imgui.NextColumn();
		Imgui.Text(_renderCallbacks.Count.ToString());
		Imgui.NextColumn();
		OnImguiTick();
	}

	protected static Camera CreateCamera(float left, float right, float bottom, float top, float near, float far)
	{
		Camera camera = Camera.CreateCamera();
		MatrixFrame identity = MatrixFrame.Identity;
		identity.origin.z = 400f;
		camera.Frame = identity;
		camera.LookAt(new Vec3(0f, 0f, 400f), new Vec3(0f, 0f, 0f, -1f), new Vec3(0f, 1f));
		camera.SetViewVolume(perspective: false, left, right, bottom, top, near, far);
		return camera;
	}

	protected static string CreateDebugIdFrom(string renderId, string typeId, string additionalInfo = "")
	{
		string value = Common.CreateNanoIdFrom(renderId);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("uit");
		stringBuilder.Append('_');
		stringBuilder.Append(typeId);
		stringBuilder.Append('_');
		stringBuilder.Append(value);
		stringBuilder.Append('_');
		stringBuilder.Append(additionalInfo);
		string text = stringBuilder.ToString();
		if (text.Length > 127)
		{
			text = text.Substring(0, 127);
		}
		return text;
	}

	protected int GetTotalMemorySize()
	{
		int num = 0;
		foreach (ThumbnailCacheNode value in _map.Values)
		{
			num += value.Value?.MemorySize ?? 0;
		}
		return num;
	}

	protected static string ByteWidthToString(int bytes)
	{
		double num = Math.Log(bytes);
		if (bytes == 0)
		{
			num = 0.0;
		}
		int num2 = (int)(num / Math.Log(1024.0));
		char c = " KMGTPE"[num2];
		return ((double)bytes / Math.Pow(1024.0, num2)).ToString("0.00") + " " + c + "      ";
	}
}
