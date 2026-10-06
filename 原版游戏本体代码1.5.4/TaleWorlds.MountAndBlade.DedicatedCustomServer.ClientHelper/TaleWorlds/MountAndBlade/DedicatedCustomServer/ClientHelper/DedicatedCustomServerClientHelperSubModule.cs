using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.Multiplayer;
using TaleWorlds.MountAndBlade.Multiplayer.GauntletUI;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade.DedicatedCustomServer.ClientHelper;

public class DedicatedCustomServerClientHelperSubModule : MBSubModuleBase
{
	private class LobbyStateListener : IGameStateListener
	{
		private LobbyState _lobbyState;

		public LobbyStateListener(LobbyState lobbyState)
		{
			_lobbyState = lobbyState;
		}

		private bool ServerSupportsDownloadPanel(GameServerEntry serverEntry)
		{
			return true;
		}

		private void OpenDownloadPanelForServer(GameServerEntry serverEntry)
		{
			Task.Run(async delegate
			{
				await new DCSHelperVM(serverEntry).OpenPopup();
			});
		}

		private List<CustomServerAction> ActionSupplier(GameServerEntry serverEntry)
		{
			List<CustomServerAction> list = new List<CustomServerAction>();
			if (ServerSupportsDownloadPanel(serverEntry))
			{
				CustomServerAction item = new CustomServerAction(delegate
				{
					OpenDownloadPanelForServer(serverEntry);
				}, serverEntry, new TextObject("{=ebuelCXT}Open Download Panel").ToString());
				list.Add(item);
			}
			return list;
		}

		private void HandleFailedServerJoinAttempt(GameServerEntry serverEntry)
		{
			if (ServerSupportsDownloadPanel(serverEntry))
			{
				OpenDownloadPanelForServer(serverEntry);
			}
		}

		public void OnActivate()
		{
			_lobbyState.RegisterForCustomServerAction(ActionSupplier);
			_lobbyState.ClientRefusedToJoinCustomServer += HandleFailedServerJoinAttempt;
		}

		public void OnDeactivate()
		{
			_lobbyState.UnregisterForCustomServerAction(ActionSupplier);
			_lobbyState.ClientRefusedToJoinCustomServer -= HandleFailedServerJoinAttempt;
		}

		public void OnFinalize()
		{
			_lobbyState = null;
		}

		public void OnInitialize()
		{
		}
	}

	private class StateManagerListener : IGameStateManagerListener
	{
		public void OnCreateState(GameState gameState)
		{
			if (gameState is LobbyState lobbyState)
			{
				lobbyState.RegisterListener(new LobbyStateListener(lobbyState));
			}
		}

		public void OnPopState(GameState gameState)
		{
		}

		public void OnPushState(GameState gameState, bool isTopGameState)
		{
		}

		public void OnCleanStates()
		{
		}

		public void OnSavedGameLoadFinished()
		{
		}
	}

	public const string ModuleName = "Multiplayer";

	public static readonly bool DebugMode;

	public static DedicatedCustomServerClientHelperSubModule Instance;

	private readonly HttpClient _httpClient;

	private const string CommandGroup = "dcshelper";

	private const string DownloadMapCommandName = "download_map";

	private const string GetMapListCommandName = "get_map_list";

	private const string OpenDownloadPanelCommandName = "open_download_panel";

	public DedicatedCustomServerClientHelperSubModule()
	{
		_httpClient = new HttpClient();
	}

	protected override void OnSubModuleLoad()
	{
		Instance = this;
		base.OnSubModuleLoad();
		TauntUsageManager.Initialize();
		ModLogger.Log("Loaded");
	}

	public override void OnMultiplayerGameStart(Game game, object _)
	{
		game.GameStateManager.RegisterListener(new StateManagerListener());
	}

	public async Task DownloadMapFromHost(string hostAddress, string mapName, bool replaceExisting = false, IProgress<ProgressUpdate> progress = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		string url = "http://" + hostAddress + "/maps/" + ((mapName == null) ? "current" : ("list/" + mapName));
		string text;
		try
		{
			ModLogger.Log("Downloading from '" + hostAddress + "' in thread...");
			text = await ModHelpers.DownloadToTempFile(_httpClient, url, progress, cancellationToken);
		}
		catch (Exception ex)
		{
			throw new Exception(new TextObject("{=lTiacca1}Failed to download map file '{MAP_NAME}'").SetTextVariable("MAP_NAME", mapName).ToString() + ": " + ex.Message, ex);
		}
		bool flag = true;
		string fullPath;
		bool flag2 = Utilities.TryGetFullFilePathOfScene(mapName, out fullPath);
		bool flag3 = flag2 && !ModHelpers.DoesSceneFolderAlreadyExist(mapName);
		string text3;
		try
		{
			string text2 = ModHelpers.ExtractZipToTempDirectory(text);
			File.Delete(text);
			text3 = ModHelpers.ReadSceneNameOfDirectory(text2);
			string text4 = System.IO.Path.Combine(ModHelpers.GetSceneObjRootPath(), text3);
			if (Directory.Exists(text4))
			{
				if (!replaceExisting)
				{
					Directory.Delete(text2, recursive: true);
					throw new Exception(new TextObject("{=5bbkOm7r}Map already exists at '{MAP_PATH}', delete this directory first if you want to re-download").SetTextVariable("MAP_PATH", text4).ToString());
				}
				flag = !flag2;
				ModLogger.Warn("Been told to replace existing map, deleting '" + text4 + "'");
				Directory.Delete(text4, recursive: true);
			}
			Utilities.ExecuteCommandLineCommand("resource.shader.invalidate_temp_shader_cache_of_scene Multiplayer/" + text3);
			Directory.Move(text2, text4);
			ModLogger.Log("Scene is available at '" + text4 + "'");
		}
		catch (Exception ex2)
		{
			throw new Exception(new TextObject("{=oaNqdada}Failed to save map scene '{MAP_NAME}'").SetTextVariable("MAP_NAME", mapName).ToString() + ": " + ex2.Message, ex2);
		}
		if (flag3)
		{
			throw new Exception(new TextObject("{=Urgon7l2}'{MAP_NAME}' was downloaded, but another module already has a scene with this name. To play the new scene, restart the game without that module/scene.").SetTextVariable("MAP_NAME", mapName).ToString());
		}
		if (flag)
		{
			try
			{
				Utilities.PairSceneNameToModuleName(text3, "Multiplayer");
				ModLogger.Log("RGL has been informed of the module pairing for scene '" + text3 + "'");
			}
			catch (Exception ex3)
			{
				throw new Exception(new TextObject("{=iRossEAk}Failed to inform game engine about the new scene '{SCENE_NAME}'").SetTextVariable("SCENE_NAME", text3).ToString() + ": " + ex3.Message, ex3);
			}
		}
	}

	public async Task<MapListResponse> GetMapListFromHost(string hostAddress)
	{
		try
		{
			string text = await HttpHelper.DownloadStringTaskAsync("http://" + hostAddress + "/maps/list");
			ModLogger.Log("'" + hostAddress + "' has a map list of: " + text);
			return JsonConvert.DeserializeObject<MapListResponse>(text);
		}
		catch (Exception ex)
		{
			throw new Exception(new TextObject("{=5ZkdGgnQ}Failed to retrieve map list of '{HOST_ADDRESS}'").SetTextVariable("HOST_ADDRESS", hostAddress).ToString() + ": " + ex.Message, ex);
		}
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("download_map", "dcshelper")]
	public static string DownloadMapCommand(List<string> strings)
	{
		string result = "Usage: dcshelper.download_map [host_address[:port]] [map_name]\nOmit map_name to download the currently played map";
		if (strings.Count == 0)
		{
			return result;
		}
		string hostAddress = strings[0];
		string mapArg = ((strings.Count > 1) ? strings[1] : null);
		Task.Run(async delegate
		{
			await Instance.DownloadMapFromHost(hostAddress, mapArg, replaceExisting: false, new Progress<ProgressUpdate>(delegate(ProgressUpdate update)
			{
				ModLogger.Log($"Progress: {update.BytesRead} / {update.TotalBytes} ({update.ProgressRatio * 100f}%)");
			}));
		});
		return "Attempting to download in the background...";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("get_map_list", "dcshelper")]
	public static string GetMapListCommand(List<string> strings)
	{
		string result = "Usage: dcshelper.get_map_list [host_address[:port]]";
		if (strings.Count != 1)
		{
			return result;
		}
		string hostAddress = strings[0];
		Task.Run(async delegate
		{
			await Instance.GetMapListFromHost(hostAddress);
		});
		return "The map list was printed to the debug console";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("open_download_panel", "dcshelper")]
	public static string OpenDownloadPanel(List<string> strings)
	{
		string result = "Usage: dcshelper.open_download_panel [host_address[:port]]";
		if (strings.Count != 1)
		{
			return result;
		}
		string hostAddress = strings[0];
		if (!(ScreenManager.TopScreen is MultiplayerLobbyGauntletScreen))
		{
			return "The download panel can only be opened while on the multiplayer lobby.";
		}
		Task.Run(async delegate
		{
			await new DCSHelperVM(hostAddress).OpenPopup();
		});
		return "Opening download panel for host '" + hostAddress + "'...";
	}
}
