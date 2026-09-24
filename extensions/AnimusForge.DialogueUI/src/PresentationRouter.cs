using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using HarmonyLib;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.GauntletUI.PrefabSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using AnimusForge.DialogueUI.Native;
using AnimusForge.DialogueUI.Shout;

namespace AnimusForge.DialogueUI
{
    internal static class PresentationRouter
    {
        [ThreadStatic] private static bool _loading;
        [ThreadStatic] private static GauntletMovie _constructingMovie;
        private static readonly FieldInfo MoviePrefabField = AccessTools.Field(typeof(GauntletMovie), "_moviePrefab");
        private static readonly FieldInfo MovieRootField = AccessTools.Field(typeof(GauntletMovie), "_movieRootNode");
        private static readonly MethodInfo ResourceChangedMethod = AccessTools.Method(typeof(GauntletMovie), "OnResourceChanged");
        private static readonly Dictionary<IGauntletMovie, IViewModel> OwnedMovies = new Dictionary<IGauntletMovie, IViewModel>();
        private static readonly HashSet<string> FailedPresentations = new HashSet<string>(StringComparer.Ordinal);

        internal static void Install(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(GauntletMovie), nameof(GauntletMovie.Load)),
                prefix: new HarmonyMethod(typeof(PresentationRouter), nameof(LoadPrefix)));
            if (MoviePrefabField == null || MovieRootField == null || ResourceChangedMethod == null)
                throw new MissingMemberException("Gauntlet partial-load cleanup members are unavailable.");
            harmony.Patch(AccessTools.Constructor(typeof(GauntletMovie), new[] { typeof(string), typeof(UIContext), typeof(WidgetFactory), typeof(IViewModel), typeof(bool) }),
                postfix: new HarmonyMethod(typeof(PresentationRouter), nameof(MovieConstructedPostfix)));
            // Resource refresh calls this private overload, bypassing the public string/VM entry.
            harmony.Patch(AccessTools.Method(typeof(GauntletLayer), nameof(GauntletLayer.LoadMovie), new[] { typeof(GauntletMovieIdentifier) }),
                postfix: new HarmonyMethod(typeof(PresentationRouter), nameof(LayerLoadPostfix)));
            harmony.Patch(AccessTools.Method(typeof(GauntletMovie), nameof(GauntletMovie.Release)),
                prefix: new HarmonyMethod(typeof(PresentationRouter), nameof(MovieReleasePrefix)));
            harmony.Patch(AccessTools.Method(typeof(GauntletLayer), nameof(GauntletLayer.ReleaseMovie)),
                prefix: new HarmonyMethod(typeof(PresentationRouter), nameof(LayerReleasePrefix)));
        }

        // Leave the OUTER movieName and datasource unchanged: other modules (notably Illustrator)
        // must observe the original logical movie and attach to the actual returned widget tree.
        private static bool LoadPrefix(UIContext context, WidgetFactory widgetFactory, string movieName,
            IViewModel datasource, bool hotReloadEnabled, ref IGauntletMovie __result)
        {
            if (_loading || !DialogueUiRuntime.Enabled || Mission.Current == null || FailedPresentations.Contains(movieName)) return true;
            string replacement;
            if (movieName == "ShoutTextInputPopup") replacement = "AFDialogueShout";
            else if (movieName == "AnimusForgeNativeConversationOverlay") replacement = "AFDialogueNativeOverlay";
            else if (movieName == "SPConversation") replacement = "AFDialogueConversation";
            else return true;

            ViewModel wrapper = null;
            IGauntletMovie loaded = null;
            try
            {
                if (!PreparePrefab(widgetFactory, replacement) || !DialogueUiSprites.EnsureLoaded()) return true;
                bool wrapped = movieName == "ShoutTextInputPopup"
                    ? ShoutUiAdapter.TryWrap(datasource, out wrapper)
                    : movieName == "AnimusForgeNativeConversationOverlay"
                        ? NativeUiAdapter.TryWrap(datasource, out wrapper)
                        : datasource is TaleWorlds.CampaignSystem.ViewModelCollection.Conversation.MissionConversationVM;
                if (!wrapped) return true;
                _loading = true;
                _constructingMovie = null;
                loaded = GauntletMovie.Load(context, widgetFactory, replacement, wrapper ?? datasource, true, false);
                if (loaded == null || !loaded.IsLoaded || loaded.RootWidget == null)
                    throw new InvalidOperationException("Replacement prefab did not instantiate: " + replacement);
                OwnedMovies.Add(loaded, datasource);
                __result = loaded;
                return false;
            }
            catch (Exception ex)
            {
                FailedPresentations.Add(movieName);
                CleanupFailedMovie(loaded ?? _constructingMovie);
                ShoutUiAdapter.Release(datasource);
                NativeUiAdapter.Release(datasource);
                DialogueUiRuntime.Log("Retained original " + movieName + ": " + ex.Message);
                return true;
            }
            finally { _loading = false; _constructingMovie = null; }
        }

        private static void MovieConstructedPostfix(GauntletMovie __instance)
        {
            if (_loading) _constructingMovie = __instance;
        }

        private static void CleanupFailedMovie(IGauntletMovie candidate)
        {
            if (candidate == null || candidate.IsReleased) return;
            if (candidate is not GauntletMovie movie)
            {
                try { candidate.Release(); } catch (Exception ex) { DialogueUiRuntime.Log("Failed movie cleanup: " + ex.Message); }
                return;
            }
            // Static Load does not return its movie if Instantiate/binding throws. The constructor
            // capture makes that half-loaded owner reachable so its subscriptions and root cannot leak.
            try { movie.RootView?.ReleaseBindingWithChildren(); } catch { }
            try
            {
                if (MoviePrefabField.GetValue(movie) is WidgetPrefab prefab)
                {
                    try { prefab.OnRelease(); } finally { movie.WidgetFactory.OnUnload(movie.MovieName); }
                }
            }
            catch { }
            try
            {
                var changed = (Action)Delegate.CreateDelegate(typeof(Action), movie, ResourceChangedMethod);
                movie.WidgetFactory.PrefabChange -= changed;
                movie.BrushFactory.BrushChange -= changed;
            }
            catch { }
            try
            {
                if (MovieRootField.GetValue(movie) is TaleWorlds.GauntletUI.BaseTypes.Widget root)
                    root.ParentWidget = null;
            }
            catch { }
            try { movie.Context.OnMovieReleased(movie.MovieName); } catch { }
        }

        private static bool PreparePrefab(WidgetFactory factory, string name)
        {
            string file = Path.Combine(DialogueUiRuntime.ModuleRoot, "GUI", "Prefabs", name + ".xml");
            if (!File.Exists(file)) return false;
            if (factory.IsCustomType(name))
            {
                string registered = Path.GetFullPath(factory.GetCustomTypePath(name)).TrimEnd('\\', '/');
                string ownDirectory = Path.GetDirectoryName(file).TrimEnd('\\', '/');
                if (!string.Equals(registered, ownDirectory, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Prefab name belongs to another module: " + name);
                return true;
            }
            // Validation is on first registration, never a frame loop.
            var document = new XmlDocument { XmlResolver = null };
            document.Load(file);
            if (document.DocumentElement?.Name != "Prefab") throw new InvalidDataException("Invalid UI prefab: " + name);
            // WidgetFactory stores the directory, and CustomWidgetType appends name + ".xml".
            factory.AddCustomType(name, Path.GetDirectoryName(file).Replace('\\', '/') + "/");
            return true;
        }

        private static void LayerLoadPostfix(GauntletMovieIdentifier identifier)
        {
            if (!DialogueUiRuntime.Enabled || Mission.Current == null || identifier?.Movie?.RootWidget == null) return;
            string movieName = identifier.MovieName;
            if (movieName == "ShoutTextInputPopup" && OwnedMovies.ContainsKey(identifier.Movie))
            {
                foreach (string id in new[] { "AFDialogueShoutHistory", "AFDialogueShoutSubmit", "AFDialogueShoutCancel" })
                {
                    try
                    {
                        DialogueUiButtons.StylePlate(identifier.Movie.RootWidget.FindChild(id, true) as TaleWorlds.GauntletUI.BaseTypes.ButtonWidget);
                    }
                    catch (Exception ex) { DialogueUiRuntime.Log("Shout button styling failed for " + id + ": " + ex); }
                }
                return;
            }
            bool isNativeConversation = movieName == "SPConversation" || movieName == "AFDialogueConversation";
            if (!isNativeConversation && movieName != "AnimusForgeNativeConversationOverlay") return;
            if (!OwnedMovies.ContainsKey(identifier.Movie)) return;
            try
            {
                if (DialogueUiSprites.EnsureLoaded())
                {
                    NativeUiAdapter.OnMovieLoaded(isNativeConversation ? "SPConversation" : movieName, identifier.Movie.RootWidget, identifier.DataSource);
                    if (movieName == "AnimusForgeNativeConversationOverlay")
                    {
                        foreach (string id in new[] { "AFDialogueHistory", "AFDialogueGift", "AnimusForgeConversationIllustrateButton", "AFDialogueSwitch", "AFDialogueLeave" })
                        {
                            try
                            {
                                DialogueUiButtons.StyleParchmentTab(identifier.Movie.RootWidget.FindChild(id, true) as TaleWorlds.GauntletUI.BaseTypes.ButtonWidget);
                            }
                            catch (Exception ex) { DialogueUiRuntime.Log("Conversation tab styling failed for " + id + ": " + ex); }
                        }
                    }
                }
            }
            catch (Exception ex) { DialogueUiRuntime.LogOnce("native-load", "Native presentation retained/restored: " + ex); }
        }

        private static void LayerReleasePrefix(GauntletMovieIdentifier identifier)
        {
            if (identifier?.Movie?.RootWidget == null) return;
            NativeUiAdapter.ReleaseRoot(identifier.Movie.RootWidget);
            ReleaseOwned(identifier.Movie);
        }

        private static void MovieReleasePrefix(GauntletMovie __instance)
        {
            NativeUiAdapter.ReleaseRoot(__instance.RootWidget);
            ReleaseOwned(__instance);
        }

        private static void ReleaseOwned(IGauntletMovie movie)
        {
            if (movie != null && OwnedMovies.TryGetValue(movie, out IViewModel original))
            {
                OwnedMovies.Remove(movie);
                // Shout's original Popup.Close owns wrapper disposal. Retaining it through a resource
                // refresh preserves the captured target, record drawer and original draft VM.
                NativeUiAdapter.Release(original);
            }
        }

        internal static void Shutdown()
        {
            // Owners release their movies; only remove our state and handlers here.
            OwnedMovies.Clear();
            ShoutUiAdapter.Shutdown();
            NativeUiAdapter.Shutdown();
        }
    }
}
