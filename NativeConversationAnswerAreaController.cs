using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace AnimusForge;

public static class NativeConversationAnswerAreaController
{
	private static readonly object Sync = new object();

	private static readonly List<RootState> Roots = new List<RootState>();

	private static bool _patched;

	private static bool _suppressed;

	public static void EnsurePatched()
	{
		if (_patched)
		{
			return;
		}
		_patched = true;
		try
		{
			Harmony harmony = new Harmony("AnimusForge.nativeconversation.answerarea");
			var method = AccessTools.Method(typeof(GauntletLayer), nameof(GauntletLayer.LoadMovie), new[] { typeof(string), typeof(ViewModel) });
			var postfix = new HarmonyMethod(typeof(NativeConversationAnswerAreaController), nameof(LoadMoviePostfix));
			if (method != null)
			{
				harmony.Patch(method, postfix: postfix);
				Logger.LogTrace("NativeConversationUI", "Native conversation answer area capture patch applied.");
			}
		}
		catch (Exception ex)
		{
			Logger.Log("NativeConversationUI", "[WARN] Failed to patch native conversation answer area capture: " + ex.Message);
		}
	}

	public static void LoadMoviePostfix(string movieName, GauntletMovieIdentifier __result)
	{
		if (!IsConversationMovie(movieName))
		{
			return;
		}
		try
		{
			Widget root = __result?.Movie?.RootWidget;
			if (root == null)
			{
				return;
			}
			lock (Sync)
			{
				if (!Roots.Exists(r => ReferenceEquals(r.Root, root)))
				{
					Roots.Add(new RootState(root));
				}
			}
			ApplySuppressionToAll();
		}
		catch (Exception ex)
		{
			Logger.Log("NativeConversationUI", "[WARN] Failed to capture native conversation root: " + ex.Message);
		}
	}

	public static void SetSuppressed(bool suppressed)
	{
		bool changed;
		lock (Sync)
		{
			changed = _suppressed != suppressed;
			_suppressed = suppressed;
		}
		if (changed)
		{
			ApplySuppressionToAll();
		}
	}

	public static void ForceRestoreAll()
	{
		List<RootState> snapshot;
		lock (Sync)
		{
			_suppressed = false;
			snapshot = new List<RootState>(Roots);
		}
		List<RootState> failedRoots = null;
		foreach (RootState root in snapshot)
		{
			try
			{
				root.ForceRestore();
			}
			catch (Exception ex)
			{
				Logger.Log("NativeConversationUI", "[WARN] Failed to force-restore native answer area: " + ex.Message);
				(failedRoots ??= new List<RootState>()).Add(root);
			}
		}
		RemoveFailedRoots(failedRoots);
	}

	public static void OnApplicationTick()
	{
		if (_suppressed)
		{
			ApplySuppressionToAll();
		}
	}

	private static bool IsConversationMovie(string movieName)
	{
		return string.Equals(movieName, "SPConversation", StringComparison.Ordinal)
			|| string.Equals(movieName, "MapConversation", StringComparison.Ordinal);
	}

	private static void ApplySuppressionToAll()
	{
		List<RootState> snapshot;
		bool suppressed;
		lock (Sync)
		{
			snapshot = new List<RootState>(Roots);
			suppressed = _suppressed;
		}
		List<RootState> failedRoots = null;
		foreach (RootState root in snapshot)
		{
			try
			{
				root.Apply(suppressed);
			}
			catch (Exception ex)
			{
				Logger.Log("NativeConversationUI", "[WARN] Failed to update native answer area: " + ex.Message);
				(failedRoots ??= new List<RootState>()).Add(root);
			}
		}
		RemoveFailedRoots(failedRoots);
	}

	private static void RemoveFailedRoots(List<RootState> failedRoots)
	{
		if (failedRoots == null || failedRoots.Count == 0)
		{
			return;
		}
		lock (Sync)
		{
			foreach (RootState root in failedRoots)
			{
				Roots.Remove(root);
			}
		}
	}

	private sealed class RootState
	{
		private static readonly ReferenceWidgetComparer WidgetComparer = new ReferenceWidgetComparer();

		private readonly List<WidgetState> _states = new List<WidgetState>();

		private readonly HashSet<Widget> _trackedWidgets = new HashSet<Widget>(WidgetComparer);

		private Widget _answerList;

		private int _lastAnswerChildCount = -1;

		private int _answerRefreshCountdown;

		private bool _hasStoredState;

		public RootState(Widget root)
		{
			Root = root;
		}

		public Widget Root { get; }

		public void Apply(bool suppressed)
		{
			if (Root == null)
			{
				return;
			}
			if (suppressed)
			{
				StoreStateIfNeeded();
				CaptureAnswerDescendantStatesIfNeeded();
				for (int i = _states.Count - 1; i >= 0; i--)
				{
					WidgetState state = _states[i];
					Widget widget = state.Widget;
					if (widget == null)
					{
						continue;
					}
					if (!state.TryApplySuppressed())
					{
						_states.RemoveAt(i);
						_trackedWidgets.Remove(widget);
					}
				}
				return;
			}
			RestoreState();
		}

		public void ForceRestore()
		{
			RestoreState();
			ReleaseCurrentNativeAnswerInteraction();
		}

		private void StoreStateIfNeeded()
		{
			if (_hasStoredState)
			{
				return;
			}
			_states.Clear();
			_trackedWidgets.Clear();
			_answerList = Root.FindChild("AnswerList", includeAllChildren: true);
			_lastAnswerChildCount = -1;
			_answerRefreshCountdown = 0;
			AddState(_answerList, preserveLayout: true);
			AddState(Root.FindChild("ContinueButton", includeAllChildren: true), preserveLayout: false);
			_hasStoredState = true;
		}

		private void CaptureAnswerDescendantStatesIfNeeded()
		{
			if (_answerList == null)
			{
				return;
			}
			int childCount = GetChildCountSafe(_answerList);
			if (_answerRefreshCountdown > 0 && childCount == _lastAnswerChildCount)
			{
				_answerRefreshCountdown--;
				return;
			}
			_lastAnswerChildCount = childCount;
			_answerRefreshCountdown = 15;
			AddDescendantStates(_answerList);
		}

		private void AddDescendantStates(Widget widget)
		{
			if (widget == null)
			{
				return;
			}
			int count = GetChildCountSafe(widget);
			for (int i = 0; i < count; i++)
			{
				Widget child;
				try
				{
					child = widget.GetChild(i);
				}
				catch
				{
					continue;
				}
				AddState(child, preserveLayout: false);
				AddDescendantStates(child);
			}
		}

		private void AddState(Widget widget, bool preserveLayout)
		{
			if (widget != null && _trackedWidgets.Add(widget))
			{
				try
				{
					_states.Add(new WidgetState(widget, preserveLayout));
				}
				catch
				{
					_trackedWidgets.Remove(widget);
				}
			}
		}

		private void RestoreState()
		{
			if (!_hasStoredState)
			{
				return;
			}
			for (int i = 0; i < _states.Count; i++)
			{
				_states[i].TryRestore();
			}
			_states.Clear();
			_trackedWidgets.Clear();
			_answerList = null;
			_lastAnswerChildCount = -1;
			_answerRefreshCountdown = 0;
			_hasStoredState = false;
		}

		private void ReleaseCurrentNativeAnswerInteraction()
		{
			if (Root == null)
			{
				return;
			}
			Widget answerListContainer = Root.FindChild("AnswerListContainer", includeAllChildren: true);
			Widget answerList = Root.FindChild("AnswerList", includeAllChildren: true);
			Widget continueButton = Root.FindChild("ContinueButton", includeAllChildren: true);
			ReleaseContainer(answerListContainer, makeVisible: true);
			ReleaseContainer(answerList, makeVisible: true);
			ReleaseAnswerDescendants(answerList);
			if (continueButton != null)
			{
				int answerCount = GetChildCountSafe(answerList);
				if (answerCount <= 0)
				{
					ReleaseButtonOrContainer(continueButton, makeVisible: true);
				}
				else
				{
					continueButton.DoNotAcceptEvents = false;
				}
			}
		}

		private static void ReleaseAnswerDescendants(Widget widget)
		{
			if (widget == null)
			{
				return;
			}
			int count = GetChildCountSafe(widget);
			for (int i = 0; i < count; i++)
			{
				Widget child;
				try
				{
					child = widget.GetChild(i);
				}
				catch
				{
					continue;
				}
				if (child == null)
				{
					continue;
				}
				if (child is ButtonWidget || IsLikelyAnswerContainer(child))
				{
					ReleaseButtonOrContainer(child, makeVisible: true);
				}
				ReleaseAnswerDescendants(child);
			}
		}

		private static bool IsLikelyAnswerContainer(Widget widget)
		{
			if (widget == null || GetChildCountSafe(widget) <= 0)
			{
				return false;
			}
			string typeName = widget.GetType()?.Name ?? "";
			if (typeName.IndexOf("Text", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return false;
			}
			string id = widget.Id ?? "";
			return id.IndexOf("Answer", StringComparison.OrdinalIgnoreCase) >= 0
				|| id.IndexOf("Option", StringComparison.OrdinalIgnoreCase) >= 0
				|| typeName.IndexOf("Conversation", StringComparison.OrdinalIgnoreCase) >= 0
				|| typeName.IndexOf("Option", StringComparison.OrdinalIgnoreCase) >= 0
				|| typeName.IndexOf("List", StringComparison.OrdinalIgnoreCase) >= 0
				|| typeName.IndexOf("Panel", StringComparison.OrdinalIgnoreCase) >= 0;
		}

		private static void ReleaseContainer(Widget widget, bool makeVisible)
		{
			if (widget == null)
			{
				return;
			}
			widget.IsEnabled = true;
			widget.DoNotAcceptEvents = false;
			if (makeVisible)
			{
				widget.IsVisible = true;
				widget.AlphaFactor = 1f;
			}
		}

		private static void ReleaseButtonOrContainer(Widget widget, bool makeVisible)
		{
			ReleaseContainer(widget, makeVisible);
		}

		private static int GetChildCountSafe(Widget widget)
		{
			try
			{
				return widget?.ChildCount ?? -1;
			}
			catch
			{
				return -1;
			}
		}
	}

	private sealed class ReferenceWidgetComparer : IEqualityComparer<Widget>
	{
		public bool Equals(Widget x, Widget y)
		{
			return ReferenceEquals(x, y);
		}

		public int GetHashCode(Widget obj)
		{
			return obj == null ? 0 : RuntimeHelpers.GetHashCode(obj);
		}
	}

	private sealed class WidgetState
	{
		private readonly bool _isVisible;

		private readonly bool _isEnabled;

		private readonly bool _doNotAcceptEvents;

		private readonly float _alphaFactor;

		private readonly SizePolicy _heightSizePolicy;

		private readonly float _suggestedHeight;

		public WidgetState(Widget widget, bool preserveLayout)
		{
			Widget = widget;
			_isVisible = widget.IsVisible;
			_isEnabled = widget.IsEnabled;
			_doNotAcceptEvents = widget.DoNotAcceptEvents;
			_alphaFactor = widget.AlphaFactor;
			_heightSizePolicy = widget.HeightSizePolicy;
			_suggestedHeight = widget.SuggestedHeight;
			PreserveLayout = preserveLayout;
			LayoutHeight = ResolveLayoutHeight(widget);
		}

		public Widget Widget { get; }

		public bool PreserveLayout { get; }

		public float LayoutHeight { get; }

		public bool TryApplySuppressed()
		{
			try
			{
				Widget widget = Widget;
				if (widget == null)
				{
					return false;
				}
				if (PreserveLayout)
				{
					widget.IsVisible = _isVisible;
					widget.HeightSizePolicy = SizePolicy.Fixed;
					widget.SuggestedHeight = LayoutHeight;
				}
				else
				{
					widget.IsVisible = false;
				}
				widget.IsEnabled = false;
				widget.DoNotAcceptEvents = true;
				return true;
			}
			catch
			{
				return false;
			}
		}

		public bool TryRestore()
		{
			try
			{
				Widget widget = Widget;
				if (widget == null)
				{
					return false;
				}
				widget.IsVisible = _isVisible;
				widget.IsEnabled = _isEnabled;
				widget.DoNotAcceptEvents = _doNotAcceptEvents;
				widget.AlphaFactor = _alphaFactor;
				widget.HeightSizePolicy = _heightSizePolicy;
				widget.SuggestedHeight = _suggestedHeight;
				return true;
			}
			catch
			{
				return false;
			}
		}

		private static float ResolveLayoutHeight(Widget widget)
		{
			if (widget == null)
			{
				return 120f;
			}
			float height = widget.Size.Y;
			if (height < 20f)
			{
				height = widget.MeasuredSize.Y;
			}
			if (height < 20f)
			{
				height = widget.SuggestedHeight;
			}
			if (height < 80f)
			{
				height = 120f;
			}
			return height;
		}
	}
}
