using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

internal sealed class SceneAudioLipSyncController
{
 private readonly SceneAudioLipSyncPort _ports;
 private volatile bool _retired;
 internal readonly object _requestLock = new object();
 internal SceneAudioLipSyncController(SceneAudioLipSyncPort ports) { _ports = ports; }
 internal bool IsSpeaking(int index) { lock (_speakingLock) return _speakingAgentIndices.Contains(index); }
 internal bool HasSpeakingAgents { get { lock (_speakingLock) return _speakingAgentIndices.Count>0; } }
 internal void MarkPlaybackFinished(int index) { lock (_speakingLock) _speakingAgentIndices.Remove(index); }
 internal bool ClearPauseForNativeReply() { bool wasPaused=_ttsPausedByInterruption || _ttsPausedByShoutUi; _ttsPausedByInterruption=false; _ttsPausedByShoutUi=false; return wasPaused; }
 internal SceneAudioResourceSnapshot GetResourceSnapshot(int index)
 {
  lock (_speakingLock) return new SceneAudioResourceSnapshot { Speaking=_speakingAgentIndices.Contains(index), HasSound=_agentSoundEvents.ContainsKey(index), HasWav=_agentWavPaths.ContainsKey(index), HasXml=_agentXmlPaths.ContainsKey(index) };
 }
 internal bool IsSubscribed { get { lock (_ttsEventSubLock) return ReferenceEquals(_ttsEventSubscribedOwner, this); } }
 internal void ResetRequestOwnership() { lock (_requestLock) { _ttsPlaybackOwners.Clear(); _activeTtsPlaybackRequests.Clear(); } }
	internal void TrackTtsPlaybackRequest(TtsEngine.PlaybackRequest request, Action prepareSceneOutput = null)
	{
		if (request == null || request.IsCancellationRequested) { return; }
		lock (_requestLock)
		{
			_ttsPlaybackOwners[request.RequestId] = new TtsPlaybackOwner
			{
				Request = request,
				RuntimeGeneration = SaveRuntimeGuard.CaptureGeneration(),
				SceneSessionId = _ports.SceneSessionId(),
				ConversationEpoch = _ports.ConversationEpoch(),
				Mission = Mission.Current,
				PrepareSceneOutput = prepareSceneOutput
			};
		}
	}

	internal bool IsTtsPlaybackRequestCurrent(TtsEngine.PlaybackRequest request, bool allowCancelled = false)
	{
		if (_retired || request == null || (!allowCancelled && request.IsCancellationRequested)) { return false; }
		lock (_requestLock)
		{
			return _ttsPlaybackOwners.TryGetValue(request.RequestId, out TtsPlaybackOwner owner)
				&& ReferenceEquals(owner.Request, request)
				&& SaveRuntimeGuard.IsCurrentGeneration(owner.RuntimeGeneration)
				&& owner.SceneSessionId == _ports.SceneSessionId()
				&& owner.ConversationEpoch == _ports.ConversationEpoch()
				&& ReferenceEquals(owner.Mission, Mission.Current);
		}
	}

	internal bool PrepareTtsPlaybackRequest(TtsEngine.PlaybackRequest request)
	{
		Action prepare = null;
		lock (_requestLock)
		{
			if (!IsTtsPlaybackRequestCurrent(request)) { return false; }
			TtsPlaybackOwner owner = _ttsPlaybackOwners[request.RequestId];
			if (owner.Prepared) { return true; }
			owner.Prepared = true;
			prepare = owner.PrepareSceneOutput;
			owner.PrepareSceneOutput = null;
			_activeTtsPlaybackRequests[request.AgentIndex] = request.RequestId;
		}
		// The event consumer is on the game thread. Preparing only the active request
		// preserves multiple legitimate FIFO jobs for the same agent without replacing their waits.
		prepare?.Invoke();
		return IsTtsPlaybackRequestCurrent(request);
	}

	internal bool IsActiveTtsPlaybackRequest(TtsEngine.PlaybackRequest request, bool allowCancelled = false)
	{
		lock (_requestLock)
		{
			return IsTtsPlaybackRequestCurrent(request, allowCancelled)
				&& _activeTtsPlaybackRequests.TryGetValue(request.AgentIndex, out long activeId)
				&& activeId == request.RequestId;
		}
	}

	internal void RetireTtsPlaybackRequest(TtsEngine.PlaybackRequest request)
	{
		if (request == null) { return; }
		lock (_requestLock)
		{
			_ttsPlaybackOwners.Remove(request.RequestId);
			if (_activeTtsPlaybackRequests.TryGetValue(request.AgentIndex, out long activeId) && activeId == request.RequestId)
			{
				_activeTtsPlaybackRequests.Remove(request.AgentIndex);
			}
		}
	}

	internal static void RunTtsMainThreadEventStep(Action action)
	{
		// Only called from request-scoped handlers already dispatched to _mainThreadActions.
		// Do not enqueue again: an old finish must drain before the next FIFO job is activated.
		action?.Invoke();
	}



	internal void SubscribeTtsPlaybackEvents()
	{
		try
		{
			lock (_speakingLock)
			{
				_speakingAgentIndices.Clear();
				_agentLipSyncDetachedForSafety.Clear();
			}
			_agentSoundEvents.Clear();
			_agentWavPaths.Clear();
			_agentXmlPaths.Clear();
			_ports.ClearPending();
			TtsEngine instance = TtsEngine.Instance;
			if (instance == null)
			{
				return;
			}
			lock (_ttsEventSubLock)
			{
				if (_ttsEventSubscribedOwner != null && _ttsEventSubscribedOwner != this)
				{
					_ttsEventSubscribedOwner.UnsubscribeTtsPlaybackEventsInternal(instance, clearOwnerIfMatch: false);
					Logger.Log("LipSync", "[WARN] 检测到旧 ShoutBehavior 残留订阅，已自动解绑旧实例");
				}
				UnsubscribeTtsPlaybackEventsInternal(instance, clearOwnerIfMatch: false);
				_retired=false;
				_ttsOnAudioFileReadyHandler = delegate(TtsEngine.PlaybackRequest request, string wavPath, string xmlPath, float durationSecs)
				{
					_ports.Dispatch(delegate
					{
						if (!IsTtsPlaybackRequestCurrent(request)) { QueueDeferredCleanup(null, wavPath, xmlPath, "stale_tts_audio", request?.AgentIndex ?? -1); return; }
						int agentIndex = request.AgentIndex;
						if (!PrepareTtsPlaybackRequest(request) || !IsActiveTtsPlaybackRequest(request)) { return; }
						bool nativeConversationWait = _ports.IsNativeWait(request);
						if (nativeConversationWait)
						{
							ConversationHelper.AdjustTypewriterDuration(durationSecs);
						}
						if (agentIndex < 0)
						{
							bool mapTableauQueued = false;
							if (!string.IsNullOrWhiteSpace(wavPath))
							{
								mapTableauQueued = _ports.QueueMapPlayback(request, wavPath, xmlPath, durationSecs);
							}
							if (!mapTableauQueued && nativeConversationWait)
							{
								ConversationHelper.StartTypewriterPlaybackIfWaiting(durationSecs);
							}
							Report("AudioDurationReady.NativeNoAgent", agentIndex, $"duration={durationSecs:F2};wav={System.IO.Path.GetFileName(wavPath)};xml={System.IO.Path.GetFileName(xmlPath)};mapTableauQueued={mapTableauQueued}");
							return;
						}
						if (nativeConversationWait)
						{
							ConversationHelper.StartTypewriterPlaybackIfWaiting(durationSecs);
						}
						if (agentIndex >= 0)
						{
							Logger.Log("LipSync", $"[OnAudioFileReady] agentIndex={agentIndex}, wav={wavPath}, xml={xmlPath}, dur={durationSecs:F2}s");
							_ports.AudioDuration(agentIndex, durationSecs);
							Report("AudioFileReady", agentIndex, $"duration={durationSecs:F2};wav={System.IO.Path.GetFileName(wavPath)};xml={System.IO.Path.GetFileName(xmlPath)}");
							bool flag = false;
							flag = _ports.HasPlaybackStarted(agentIndex);
							if (flag)
							{
								RunTtsMainThreadEventStep(delegate
								{
									if (!IsTtsPlaybackRequestCurrent(request)) { return; }
									try
									{
										if (!_ports.DispatchBubble(agentIndex, false))
										{
											_ports.ClearOrphanDuration(agentIndex);
										}
									}
									catch
									{
									}
								});
							}
							RunTtsMainThreadEventStep(delegate
							{
									if (!IsTtsPlaybackRequestCurrent(request)) { QueueDeferredCleanup(null, wavPath, xmlPath, "stale_tts_audio_main", agentIndex); return; }
								try
								{
									Report("AudioFileReady.MainThreadStart", agentIndex, $"wavExists={(!string.IsNullOrWhiteSpace(wavPath) && File.Exists(wavPath))};xmlExists={(!string.IsNullOrWhiteSpace(xmlPath) && File.Exists(xmlPath))}");
									if (IsInEscapeTransitionWindow())
									{
										try
										{
											if (!string.IsNullOrEmpty(wavPath) && File.Exists(wavPath))
											{
												File.Delete(wavPath);
											}
										}
										catch
										{
										}
										try
										{
											if (!string.IsNullOrEmpty(xmlPath) && File.Exists(xmlPath))
											{
												File.Delete(xmlPath);
											}
											return;
										}
										catch
										{
											return;
										}
									}
									Mission mission = Mission.Current;
									if (_enableRhubarbSoundEventPlayback && mission?.Scene != null)
									{
										Agent agent = mission.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex);
										if (agent != null && agent.IsActive())
										{
											CleanupPreviousLipSyncPlaybackForReplacement("OnAudioFileReady.ReplaceExisting");
									LogLipSyncNativeProbe("CreateEventFromExternalFile.Before", agentIndex, "wav=" + System.IO.Path.GetFileName(wavPath));
									SoundEvent soundEvent = SoundEvent.CreateEventFromExternalFile("event:/Extra/voiceover", wavPath, mission.Scene, is3d: false, isBlocking: false);
									if (soundEvent == null)
									{
										Logger.Log("LipSync", $"[WARN] SoundEvent.CreateEventFromExternalFile 返回 null, agentIndex={agentIndex}");
										Report("AudioFileReady.CreateSoundEventNull", agentIndex, $"wav={System.IO.Path.GetFileName(wavPath)}");
										QueueDeferredCleanup(null, wavPath, xmlPath, "OnAudioFileReady.CreateSoundEventNull", agentIndex);
											}
									else
									{
										Report("AudioFileReady.SoundEventCreated", agentIndex, $"wav={System.IO.Path.GetFileName(wavPath)}");
										LogLipSyncNativeProbe("SoundEvent.SetPosition.Before", agentIndex);
										soundEvent.SetPosition(agent.Position);
										LogLipSyncNativeProbe("SoundEvent.SetPosition.After", agentIndex);
										LogLipSyncNativeProbe("SoundEvent.Play.Before", agentIndex);
										soundEvent.Play();
										LogLipSyncNativeProbe("SoundEvent.Play.After", agentIndex);
										float num = 0f;
										bool flag = true;
										try
										{
											flag = DuelSettings.GetSettings()?.TtsSceneUseWinmmAudible ?? true;
													num = DuelSettings.GetSettings()?.TtsLipSyncSoundEventVolume ?? 0f;
												}
												catch
												{
													flag = true;
													num = 0f;
												}
												if (flag)
												{
													num = 0f;
												}
												if (num < 0f)
												{
													num = 0f;
												}
												if (num > 1f)
												{
													num = 1f;
												}
												try
												{
													soundEvent.SetParameter("Volume", num);
												}
												catch (Exception ex2)
										{
											Logger.Log("LipSync", "[WARN] 设置 SoundEvent 音量失败(Volume): " + ex2.Message);
										}
										LogLipSyncNativeProbe("SoundEvent.GetSoundId.Before", agentIndex);
										int soundId = soundEvent.GetSoundId();
										LogLipSyncNativeProbe("SoundEvent.GetSoundId.After", agentIndex, "soundId=" + soundId);
										if (soundId <= 0)
										{
											Logger.Log("LipSync", $"[WARN] SoundEvent.GetSoundId 非法({soundId})，跳过 StartRhubarbRecord, agentIndex={agentIndex}");
											SafeStopAndReleaseSoundEvent(soundEvent);
											QueueDeferredCleanup(null, wavPath, xmlPath, "OnAudioFileReady.InvalidSoundId", agentIndex);
										}
												else
												{
													string text = "";
													bool flag2 = _ports.CanLipSync(agent, out text);
													Logger.Log("LipSync", $"[Rhubarb] SoundEvent created, vol={num:F2}, agentIndex={agentIndex}, soundId={soundId}, lipSyncSafe={flag2}, reason={text}");
													lock (_speakingLock)
													{
														if (flag2)
														{
															_agentLipSyncDetachedForSafety.Remove(agentIndex);
														}
														else
														{
															_agentLipSyncDetachedForSafety.Add(agentIndex);
														}
														_agentSoundEvents[agentIndex] = soundEvent;
														_agentWavPaths[agentIndex] = wavPath;
														_agentXmlPaths[agentIndex] = xmlPath;
													}
													if (flag2)
													{
														PrepareAgentForTrueLipSyncIfPossible(agent);
														LogLipSyncNativeProbe("StartRhubarbRecord.Before", agentIndex, $"soundId={soundId};xml={System.IO.Path.GetFileName(xmlPath)}");
														agent.AgentVisuals.StartRhubarbRecord(xmlPath, soundId);
														LogLipSyncNativeProbe("StartRhubarbRecord.After", agentIndex, "soundId=" + soundId);
														Logger.Log("LipSync", $"[Rhubarb] StartRhubarbRecord 调用成功, agentIndex={agentIndex}, soundId={soundId}");
													}
													else
													{
														Logger.Log("LipSync", $"[SAFEGUARD] Skip StartRhubarbRecord for unsafe scene agent. agentIndex={agentIndex}, reason={text}");
														Report("AudioFileReady.LipSyncDetached", agentIndex, "reason=" + text);
													}
											}
										}
										}
										else
										{
											Report("AudioFileReady.AgentUnavailable", agentIndex, $"agentMissing={(agent == null)};active={(agent != null && agent.IsActive())}");
											QueueDeferredCleanup(null, wavPath, xmlPath, "OnAudioFileReady.AgentUnavailable", agentIndex);
										}
									}
									Report("AudioFileReady.MainThreadEnd", agentIndex);
								}
								catch (Exception ex3)
								{
									QueueDeferredCleanup(null, wavPath, xmlPath, "OnAudioFileReady.MainThreadFailed", agentIndex);
									Logger.Log("LipSync", "[ERROR] OnAudioFileReady 主线程处理失败: " + ex3.Message);
									Report("AudioFileReady.MainThreadFailed", agentIndex, "error=" + ex3.Message);
									BannerlordExceptionSentinel.ReportObservedException("LipSync.OnAudioFileReady.MainThread", ex3, "agentIndex=" + agentIndex);
								}
							});
						}

					});
				};
				_ttsOnPlaybackStartedHandler = delegate(TtsEngine.PlaybackRequest request)
				{
					_ports.Dispatch(delegate
					{
						if (!IsTtsPlaybackRequestCurrent(request)) { return; }
						int agentIndex = request.AgentIndex;
						if (!PrepareTtsPlaybackRequest(request) || !IsActiveTtsPlaybackRequest(request)) { return; }
						if (_ports.IsNativeWait(request))
						{
							if (agentIndex >= 0 || !_ports.UseMapPlayback())
							{
								ConversationHelper.StartTypewriterPlaybackIfWaiting();
							}
						}
						if (agentIndex >= 0)
						{
							if (!_ports.CanParticipate(agentIndex))
							{
								_ports.ClearAgentPending(agentIndex);
								Report("PlaybackStarted.InvalidAgent", agentIndex);
								return;
							}
							string text = "";
							bool flag2 = _ports.CanLipSyncIndex(agentIndex, out text);
							lock (_speakingLock)
							{
								if (flag2)
								{
									_agentLipSyncDetachedForSafety.Remove(agentIndex);
									_speakingAgentIndices.Add(agentIndex);
								}
								else
								{
									_speakingAgentIndices.Remove(agentIndex);
									_agentLipSyncDetachedForSafety.Add(agentIndex);
								}
							}
							Logger.Log("LipSync", $"[OnPlaybackStarted] agentIndex={agentIndex}, lipSyncSafe={flag2}, reason={text}");
							Report("PlaybackStarted", agentIndex, $"lipSyncSafe={flag2};reason={text}");
							_ports.MarkPlaybackStarted(agentIndex);
							RunTtsMainThreadEventStep(delegate
							{
									if (!IsTtsPlaybackRequestCurrent(request)) { return; }
								try
								{
									bool flag3 = _ports.DispatchBubble(agentIndex, false);
									if (!flag3)
									{
										_ports.ScheduleBubbleFallback(request);
									}
								}
								catch (Exception ex4)
								{
									Logger.Log("LipSync", $"[ERROR] PlaybackStarted bubble dispatch failed, agentIndex={agentIndex}, error={ex4.Message}");
									Report("PlaybackStarted.BubbleDispatchFailed", agentIndex, "error=" + ex4.Message);
									BannerlordExceptionSentinel.ReportObservedException("LipSync.PlaybackStarted.BubbleDispatch", ex4, "agentIndex=" + agentIndex);
								}
							});
						}

					});
				};
				_ttsOnPlaybackFinishedHandler = delegate(TtsEngine.PlaybackRequest request)
				{
					_ports.Dispatch(delegate
					{
						try
						{
						if (!IsTtsPlaybackRequestCurrent(request)) { return; }
						int agentIndex = request.AgentIndex;
						if (!PrepareTtsPlaybackRequest(request) || !IsActiveTtsPlaybackRequest(request)) { return; }
						_ports.CompleteNativeWait(request, "playback_finished");
						if (agentIndex >= 0)
						{
							FreezeWatchdog.Mark("SceneTts.playback_finished.event", "agent=" + agentIndex + " thread=" + Thread.CurrentThread.ManagedThreadId, immediate: true);
							RunTtsMainThreadEventStep(delegate
							{
									if (!IsTtsPlaybackRequestCurrent(request)) { return; }
								_ports.Finished(agentIndex);
							});
						}

						}
						finally { RetireTtsPlaybackRequest(request); }

					});
				};
				_ttsOnPlaybackFailedHandler = delegate(TtsEngine.PlaybackRequest request, string errorMessage)
				{
					_ports.Dispatch(delegate
					{
						try
						{
						if (!IsTtsPlaybackRequestCurrent(request)) { return; }
						int agentIndex = request.AgentIndex;
						HandleTtsPlaybackFailed(request, errorMessage);

						}
						finally { RetireTtsPlaybackRequest(request); }

					});
				};
				_ttsOnPlaybackCancelledHandler = _ports.Cancelled;
				instance.OnRequestPlaybackCancelled += _ttsOnPlaybackCancelledHandler;
				instance.OnRequestAudioFileReady += _ttsOnAudioFileReadyHandler;
				instance.OnRequestPlaybackStarted += _ttsOnPlaybackStartedHandler;
				instance.OnRequestPlaybackFinished += _ttsOnPlaybackFinishedHandler;
				instance.OnRequestPlaybackFailed += _ttsOnPlaybackFailedHandler;
				_ttsEventSubscribedOwner = this;
				Logger.Log("LipSync", $"[INFO] TTS 播放事件订阅完成 owner={GetHashCode()}");
			}
		}
		catch (Exception ex)
		{
			Logger.Log("LipSync", "[ERROR] SubscribeTtsPlaybackEvents failed: " + ex.Message);
		}
	}

	internal void UnsubscribeTtsPlaybackEventsInternal(TtsEngine tts, bool clearOwnerIfMatch)
	{
  _retired=true; ResetRequestOwnership();
		if (tts == null)
		{
			return;
		}
		try
		{
			if (_ttsOnPlaybackCancelledHandler != null) { tts.OnRequestPlaybackCancelled -= _ttsOnPlaybackCancelledHandler; }
			if (_ttsOnAudioFileReadyHandler != null)
			{
				tts.OnRequestAudioFileReady -= _ttsOnAudioFileReadyHandler;
			}
			if (_ttsOnPlaybackStartedHandler != null)
			{
				tts.OnRequestPlaybackStarted -= _ttsOnPlaybackStartedHandler;
			}
			if (_ttsOnPlaybackFinishedHandler != null)
			{
				tts.OnRequestPlaybackFinished -= _ttsOnPlaybackFinishedHandler;
			}
			if (_ttsOnPlaybackFailedHandler != null)
			{
				tts.OnRequestPlaybackFailed -= _ttsOnPlaybackFailedHandler;
			}
		}
		catch (Exception ex)
		{
			Logger.Log("LipSync", "[WARN] 取消订阅 TTS 事件失败: " + ex.Message);
		}
		if (clearOwnerIfMatch && _ttsEventSubscribedOwner == this)
		{
			_ttsEventSubscribedOwner = null;
		}
	}

	internal void UnsubscribeTtsPlaybackEvents()
	{
  _retired=true; ResetRequestOwnership();
		try
		{
			TtsEngine instance = TtsEngine.Instance;
			if (instance == null)
			{
				return;
			}
			lock (_ttsEventSubLock)
			{
				UnsubscribeTtsPlaybackEventsInternal(instance, clearOwnerIfMatch: true);
			}
		}
		catch
		{
		}
	}





	internal void CleanupSceneLipSyncAfterPlaybackFinished(int agentIndex)
	{
		Report("PlaybackFinished.MainThreadCleanupStart", agentIndex);
		LogLipSyncNativeProbe("PlaybackFinished.CleanupEnter", agentIndex);
		SoundEvent soundEvent = null;
		string wavPath = null;
		string xmlPath = null;
		bool hadSoundEvent = false;
		bool hadWav = false;
		bool hadXml = false;
		bool wasDetached = false;
		lock (_speakingLock)
		{
			hadSoundEvent = _agentSoundEvents.TryGetValue(agentIndex, out soundEvent);
			if (hadSoundEvent)
			{
				_agentSoundEvents.Remove(agentIndex);
			}
			hadWav = _agentWavPaths.TryGetValue(agentIndex, out wavPath);
			if (hadWav)
			{
				_agentWavPaths.Remove(agentIndex);
			}
			hadXml = _agentXmlPaths.TryGetValue(agentIndex, out xmlPath);
			if (hadXml)
			{
				_agentXmlPaths.Remove(agentIndex);
			}
			wasDetached = _agentLipSyncDetachedForSafety.Remove(agentIndex);
		}
		bool hasNativeState = hadSoundEvent || hadWav || hadXml || wasDetached;
		if (hasNativeState)
		{
			StopAgentRhubarbRecordIfPossible(agentIndex, "PlaybackFinished.NaturalCleanup");
		}
		if (soundEvent != null || !string.IsNullOrWhiteSpace(wavPath) || !string.IsNullOrWhiteSpace(xmlPath))
		{
			string source = wasDetached ? "PlaybackFinished.DetachedSafety" : "PlaybackFinished.NaturalCleanup";
			QueueDeferredCleanup(soundEvent, wavPath, xmlPath, source, agentIndex);
			Logger.Log("LipSync", $"[Rhubarb] queued native lipsync cleanup after playback finished, agentIndex={agentIndex}, detached={wasDetached}, hasSe={(soundEvent != null)}, hasWav={(!string.IsNullOrWhiteSpace(wavPath))}, hasXml={(!string.IsNullOrWhiteSpace(xmlPath))}");
			Report("PlaybackFinished.CleanupQueued", agentIndex, $"detached={wasDetached};hasSe={(soundEvent != null)};hasWav={(!string.IsNullOrWhiteSpace(wavPath))};hasXml={(!string.IsNullOrWhiteSpace(xmlPath))}");
			LogLipSyncNativeProbe("PlaybackFinished.CleanupQueued", agentIndex, $"detached={wasDetached};hasSe={(soundEvent != null)};hasWav={(!string.IsNullOrWhiteSpace(wavPath))};hasXml={(!string.IsNullOrWhiteSpace(xmlPath))}");
		}
		else
		{
			Logger.Log("LipSync", $"[Rhubarb] no native lipsync state to cleanup after playback finished, agentIndex={agentIndex}, hadSe={hadSoundEvent}, hadWav={hadWav}, hadXml={hadXml}, detached={wasDetached}");
			Report("PlaybackFinished.CleanupNoState", agentIndex, $"hadSe={hadSoundEvent};hadWav={hadWav};hadXml={hadXml};detached={wasDetached}");
			LogLipSyncNativeProbe("PlaybackFinished.CleanupNoState", agentIndex, $"hadSe={hadSoundEvent};hadWav={hadWav};hadXml={hadXml};detached={wasDetached}");
		}
		Report("PlaybackFinished.MainThreadCleanupEnd", agentIndex);
	}

	internal static bool IsMissionSceneReadyForSoundOps()
	{
		try
		{
			return Mission.Current?.Scene != null;
		}
		catch
		{
			return false;
		}
	}

	[DllImport("user32.dll")]
	internal static extern IntPtr GetForegroundWindow();

	[DllImport("user32.dll")]
	internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

	internal static bool IsGameWindowFocused()
	{
		try
		{
			IntPtr foregroundWindow = GetForegroundWindow();
			if (foregroundWindow == IntPtr.Zero)
			{
				return true;
			}
			GetWindowThreadProcessId(foregroundWindow, out var processId);
			if (processId == 0)
			{
				return true;
			}
			return processId == _currentProcessId;
		}
		catch
		{
			return true;
		}
	}

	internal bool IsInEscapeTransitionWindow()
	{
		try
		{
			long num = Interlocked.Read(ref _lastEscapePressedUtcTicks);
			if (num <= 0)
			{
				return false;
			}
			TimeSpan timeSpan = DateTime.UtcNow - new DateTime(num, DateTimeKind.Utc);
			return timeSpan.TotalMilliseconds >= 0.0 && timeSpan.TotalSeconds <= 6.0;
		}
		catch
		{
			return false;
		}
	}

	internal bool HasInterruptionDebounceElapsed()
	{
		try
		{
			long num = Interlocked.Read(ref _lastEscapePressedUtcTicks);
			if (num <= 0)
			{
				return true;
			}
			return (DateTime.UtcNow - new DateTime(num, DateTimeKind.Utc)).TotalMilliseconds >= 250.0;
		}
		catch
		{
			return true;
		}
	}

	internal void ApplyTtsPauseState()
	{
		bool flag = _ttsPausedByShoutUi || _ttsPausedByInterruption;
		try
		{
			_ports.PauseTyping(flag);
		}
		catch
		{
		}
		if (flag)
		{
			try
			{
				TtsEngine.Instance?.PausePlayback();
			}
			catch
			{
			}
			lock (_speakingLock)
			{
				foreach (KeyValuePair<int, SoundEvent> agentSoundEvent in _agentSoundEvents)
				{
					try
					{
						agentSoundEvent.Value?.Pause();
					}
					catch
					{
					}
				}
			}
			return;
		}
		try
		{
			TtsEngine.Instance?.ResumePlayback();
		}
		catch
		{
		}
		lock (_speakingLock)
		{
			foreach (KeyValuePair<int, SoundEvent> agentSoundEvent2 in _agentSoundEvents)
			{
				try
				{
					agentSoundEvent2.Value?.Resume();
				}
				catch
				{
				}
			}
		}
	}

	internal void PauseTtsForShoutUi()
	{
		if (!_ttsPausedByShoutUi)
		{
			_ttsPausedByShoutUi = true;
			ApplyTtsPauseState();
			Logger.Log("LipSync", "[ShoutUI] TTS paused");
		}
	}

	internal void ResumeTtsAfterShoutUi()
	{
		if (_ttsPausedByShoutUi)
		{
			_ttsPausedByShoutUi = false;
			ApplyTtsPauseState();
			Logger.Log("LipSync", "[ShoutUI] TTS resumed");
		}
	}

	internal void TryResumeInterruptionPauseIfPossible()
	{
		if (_ttsPausedByInterruption)
		{
			if (!HasInterruptionDebounceElapsed())
			{
				return;
			}
			if (!IsGameWindowFocused())
			{
				return;
			}
			float num = 1f;
			try
			{
				num = (Mission.Current?.Scene?.TimeSpeed ?? 1f);
			}
			catch
			{
				num = 1f;
			}
			if (num <= 0.001f)
			{
				return;
			}
			_ttsPausedByInterruption = false;
			ApplyTtsPauseState();
			Logger.Log("LipSync", "[INTERRUPT] TTS resumed after interruption");
		}
	}

	internal void QueueDeferredCleanup(SoundEvent se, string wavPath, string xmlPath, string source = "Unknown", int agentIndex = -1)
	{
		if (se == null && string.IsNullOrWhiteSpace(wavPath) && string.IsNullOrWhiteSpace(xmlPath))
		{
			return;
		}
		long itemId;
		try
		{
			itemId = DateTime.UtcNow.Ticks;
		}
		catch
		{
			itemId = Stopwatch.GetTimestamp();
		}
		DeferredCleanupItem deferredCleanupItem = new DeferredCleanupItem
		{
			ItemId = itemId,
			SoundEvent = se,
			WavPath = (string.IsNullOrWhiteSpace(wavPath) ? null : wavPath),
			XmlPath = (string.IsNullOrWhiteSpace(xmlPath) ? null : xmlPath),
			EnqueuedUtcTicks = DateTime.UtcNow.Ticks,
			Source = (string.IsNullOrWhiteSpace(source) ? "Unknown" : source),
			AgentIndex = agentIndex
		};
		int num2 = 0;
		lock (_speakingLock)
		{
			_deferredCleanupQueue.Add(deferredCleanupItem);
			num2 = _deferredCleanupQueue.Count;
		}
		try
		{
			Interlocked.Exchange(ref _deferredCleanupStableSinceUtcTicks, 0L);
		}
		catch
		{
		}
		Logger.Log("LipSync", $"[DeferredCleanup][ENQUEUE] id={deferredCleanupItem.ItemId}, src={deferredCleanupItem.Source}, agent={deferredCleanupItem.AgentIndex}, hasSe={(deferredCleanupItem.SoundEvent != null)}, hasWav={(!string.IsNullOrWhiteSpace(deferredCleanupItem.WavPath))}, hasXml={(!string.IsNullOrWhiteSpace(deferredCleanupItem.XmlPath))}, queue={num2}");
	}

	internal bool IsDeferredCleanupWindowStable()
	{
		if (!IsMissionSceneReadyForSoundOps())
		{
			return false;
		}
		if (IsInEscapeTransitionWindow())
		{
			return false;
		}
		if (!IsGameWindowFocused())
		{
			return false;
		}
		if (_ttsPausedByInterruption || _ttsPausedByShoutUi)
		{
			return false;
		}
		float num = 1f;
		try
		{
			num = (Mission.Current?.Scene?.TimeSpeed ?? 1f);
		}
		catch
		{
			num = 1f;
		}
		return num > 0.001f;
	}

	internal void ProcessDeferredCleanup()
	{
		long ticks = DateTime.UtcNow.Ticks;
		int num = 0;
		List<DeferredCleanupItem> list = new List<DeferredCleanupItem>();
		lock (_speakingLock)
		{
			num = _deferredCleanupQueue.Count;
			if (num > 0)
			{
				for (int i = _deferredCleanupQueue.Count - 1; i >= 0; i--)
				{
					DeferredCleanupItem deferredCleanupItem = _deferredCleanupQueue[i];
					if (deferredCleanupItem == null)
					{
						_deferredCleanupQueue.RemoveAt(i);
						continue;
					}
					TimeSpan timeSpan = new TimeSpan(Math.Max(0L, ticks - deferredCleanupItem.EnqueuedUtcTicks));
					if (timeSpan.TotalSeconds > DeferredCleanupMaxAgeSeconds)
					{
						list.Add(deferredCleanupItem);
						_deferredCleanupQueue.RemoveAt(i);
					}
				}
				num = _deferredCleanupQueue.Count;
			}
		}
		if (list.Count > 0)
		{
			for (int j = 0; j < list.Count; j++)
			{
				DeferredCleanupItem deferredCleanupItem2 = list[j];
				TimeSpan timeSpan2 = new TimeSpan(Math.Max(0L, ticks - deferredCleanupItem2.EnqueuedUtcTicks));
				try
				{
					if (!string.IsNullOrEmpty(deferredCleanupItem2.WavPath) && File.Exists(deferredCleanupItem2.WavPath))
					{
						File.Delete(deferredCleanupItem2.WavPath);
					}
				}
				catch
				{
				}
				try
				{
					if (!string.IsNullOrEmpty(deferredCleanupItem2.XmlPath) && File.Exists(deferredCleanupItem2.XmlPath))
					{
						File.Delete(deferredCleanupItem2.XmlPath);
					}
				}
				catch
				{
				}
				Logger.Log("LipSync", $"[DeferredCleanup][TIMEOUT_DROP] id={deferredCleanupItem2.ItemId}, src={deferredCleanupItem2.Source}, agent={deferredCleanupItem2.AgentIndex}, ageMs={(int)timeSpan2.TotalMilliseconds}");
			}
		}
		if (num <= 0)
		{
			try
			{
				Interlocked.Exchange(ref _deferredCleanupStableSinceUtcTicks, 0L);
			}
			catch
			{
			}
			return;
		}
		if (!IsDeferredCleanupWindowStable())
		{
			try
			{
				Interlocked.Exchange(ref _deferredCleanupStableSinceUtcTicks, 0L);
			}
			catch
			{
			}
			return;
		}
		long num2 = 0L;
		try
		{
			num2 = Interlocked.Read(ref _deferredCleanupStableSinceUtcTicks);
		}
		catch
		{
			num2 = 0L;
		}
		if (num2 <= 0)
		{
			try
			{
				Interlocked.Exchange(ref _deferredCleanupStableSinceUtcTicks, ticks);
			}
			catch
			{
			}
			return;
		}
		if ((new TimeSpan(ticks - num2)).TotalSeconds < DeferredCleanupStableWindowSeconds)
		{
			return;
		}
		List<DeferredCleanupItem> list2 = new List<DeferredCleanupItem>();
		lock (_speakingLock)
		{
			if (_deferredCleanupQueue.Count <= 0)
			{
				return;
			}
			for (int k = 0; k < _deferredCleanupQueue.Count; k++)
			{
				DeferredCleanupItem deferredCleanupItem3 = _deferredCleanupQueue[k];
				if (deferredCleanupItem3 == null)
				{
					continue;
				}
				if ((new TimeSpan(Math.Max(0L, ticks - deferredCleanupItem3.EnqueuedUtcTicks))).TotalSeconds < DeferredCleanupMinAgeSeconds)
				{
					continue;
				}
				list2.Add(deferredCleanupItem3);
				if (list2.Count >= DeferredCleanupBatchSize)
				{
					break;
				}
			}
			if (list2.Count > 0)
			{
				for (int l = 0; l < list2.Count; l++)
				{
					_deferredCleanupQueue.Remove(list2[l]);
				}
			}
		}
		if (list2.Count <= 0)
		{
			return;
		}
		for (int m = 0; m < list2.Count; m++)
		{
			DeferredCleanupItem deferredCleanupItem4 = list2[m];
			TimeSpan timeSpan3 = new TimeSpan(Math.Max(0L, ticks - deferredCleanupItem4.EnqueuedUtcTicks));
			try
			{
				SafeStopAndReleaseSoundEvent(deferredCleanupItem4.SoundEvent);
			}
			catch (Exception ex)
			{
				BannerlordExceptionSentinel.ReportObservedException("LipSync.DeferredCleanup.SoundEvent", ex, $"agentIndex={deferredCleanupItem4.AgentIndex};itemId={deferredCleanupItem4.ItemId};source={deferredCleanupItem4.Source}");
			}
			try
			{
				if (!string.IsNullOrEmpty(deferredCleanupItem4.WavPath) && File.Exists(deferredCleanupItem4.WavPath))
				{
					File.Delete(deferredCleanupItem4.WavPath);
				}
			}
			catch
			{
			}
			try
			{
				if (!string.IsNullOrEmpty(deferredCleanupItem4.XmlPath) && File.Exists(deferredCleanupItem4.XmlPath))
				{
					File.Delete(deferredCleanupItem4.XmlPath);
				}
			}
			catch
			{
			}
			Logger.Log("LipSync", $"[DeferredCleanup][PROCESS] id={deferredCleanupItem4.ItemId}, src={deferredCleanupItem4.Source}, agent={deferredCleanupItem4.AgentIndex}, ageMs={(int)timeSpan3.TotalMilliseconds}");
		}
		int num3 = 0;
		lock (_speakingLock)
		{
			num3 = _deferredCleanupQueue.Count;
		}
		Logger.Log("LipSync", $"[DeferredCleanup][SUMMARY] processed={list2.Count}, timeoutDropped={list.Count}, remaining={num3}");
	}

	internal void HandleEscapePressedForAudioSafety(string reason = "ESC")
	{
		try
		{
			Interlocked.Exchange(ref _lastEscapePressedUtcTicks, DateTime.UtcNow.Ticks);
		}
		catch
		{
		}
		try
		{
			Interlocked.Exchange(ref _deferredCleanupStableSinceUtcTicks, 0L);
		}
		catch
		{
		}
		if (!_ttsPausedByInterruption)
		{
			_ttsPausedByInterruption = true;
			ApplyTtsPauseState();
			Logger.Log("LipSync", $"[{reason}] TTS paused by interruption");
		}
	}

	internal bool HasLipSyncOrSceneSpeechWork()
	{
		if (_ports.SpeechBusy())
		{
			return true;
		}
		lock (_speakingLock)
		{
			if (_speakingAgentIndices.Count > 0 || _agentSoundEvents.Count > 0 || _agentWavPaths.Count > 0 || _agentXmlPaths.Count > 0 || _deferredCleanupQueue.Count > 0)
			{
				return true;
			}
		}
		return false;
	}

	internal void HandleCriticalUiTransitionForLipSyncSafety(string reason = "UI_TRANSITION")
	{
		try
		{
			Interlocked.Exchange(ref _lastEscapePressedUtcTicks, DateTime.UtcNow.Ticks);
		}
		catch
		{
		}
		try
		{
			Interlocked.Exchange(ref _deferredCleanupStableSinceUtcTicks, 0L);
		}
		catch
		{
		}
		if (!HasLipSyncOrSceneSpeechWork())
		{
			return;
		}
		_ports.ClearSpeechQueue();
		Logger.Log("LipSync", $"[{reason}] critical UI transition detected, forcing native lipsync teardown");
		LogLipSyncNativeProbe("CriticalUiTransition.StopAll.Before", -1, reason);
		StopAllLipSyncPlaybackAndCleanup();
		LogLipSyncNativeProbe("CriticalUiTransition.StopAll.After", -1, reason);
	}

	internal static void SafeStopAndReleaseSoundEvent(SoundEvent se)
	{
		if (se == null)
		{
			Logger.Log("LipSync", "[SoundEvent] SafeStopAndRelease skipped: null");
			return;
		}
		int soundId = -1;
		try
		{
			soundId = se.GetSoundId();
		}
		catch
		{
			soundId = -1;
		}
		Logger.Log("LipSync", $"[SoundEvent] SafeStopAndRelease start, soundId={soundId}");
		LogLipSyncNativeProbe("SafeStopAndRelease.Enter", -1, "soundId=" + soundId);
		bool isValid = false;
		try
		{
			isValid = se.IsValid;
		}
		catch (Exception ex)
		{
			Logger.Log("LipSync", $"[SoundEvent] IsValid probe failed, soundId={soundId}, error={ex.Message}");
			BannerlordExceptionSentinel.ReportObservedException("LipSync.SoundEvent.IsValid", ex, "soundId=" + soundId);
			return;
		}
		if (!isValid)
		{
			Logger.Log("LipSync", $"[SoundEvent] SafeStopAndRelease skipped: invalid soundId={soundId}");
			LogLipSyncNativeProbe("SafeStopAndRelease.SkipInvalid", -1, "soundId=" + soundId);
			return;
		}
		try
		{
			LogLipSyncNativeProbe("SoundEvent.Stop.Before", -1, "soundId=" + soundId);
			se.Stop();
			LogLipSyncNativeProbe("SoundEvent.Stop.After", -1, "soundId=" + soundId);
		}
		catch (Exception ex2)
		{
			Logger.Log("LipSync", $"[SoundEvent] Stop failed, soundId={soundId}, error={ex2.Message}");
			BannerlordExceptionSentinel.ReportObservedException("LipSync.SoundEvent.Stop", ex2, "soundId=" + soundId);
		}
		Logger.Log("LipSync", $"[SoundEvent] SafeStopAndRelease end, soundId={soundId}");
	}

	internal void StopAllLipSyncPlaybackAndCleanup()
	{
		try
		{
			TtsEngine.Instance?.StopPlayback();
		}
		catch
		{
		}
		_ports.ClearPending();
		try
		{
			_ports.PauseTyping(false);
			_ports.StopTyping();
		}
		catch
		{
		}
		_ttsPausedByInterruption = false;
		_ttsPausedByShoutUi = false;
		List<int> list = null;
		lock (_speakingLock)
		{
			list = _agentSoundEvents.Keys.ToList();
			_speakingAgentIndices.Clear();
			_agentLipSyncDetachedForSafety.Clear();
			foreach (KeyValuePair<int, SoundEvent> agentSoundEvent in _agentSoundEvents)
			{
				_ = agentSoundEvent.Value;
			}
			_agentSoundEvents.Clear();
			foreach (KeyValuePair<int, string> agentWavPath in _agentWavPaths)
			{
				try
				{
					if (!string.IsNullOrEmpty(agentWavPath.Value) && File.Exists(agentWavPath.Value))
					{
						File.Delete(agentWavPath.Value);
					}
				}
				catch
				{
				}
			}
			foreach (KeyValuePair<int, string> agentXmlPath in _agentXmlPaths)
			{
				try
				{
					if (!string.IsNullOrEmpty(agentXmlPath.Value) && File.Exists(agentXmlPath.Value))
					{
						File.Delete(agentXmlPath.Value);
					}
				}
				catch
				{
				}
			}
			_agentWavPaths.Clear();
			_agentXmlPaths.Clear();
			foreach (DeferredCleanupItem deferredCleanupItem in _deferredCleanupQueue)
			{
				try
				{
					SafeStopAndReleaseSoundEvent(deferredCleanupItem?.SoundEvent);
				}
				catch
				{
				}
				try
				{
					if (!string.IsNullOrEmpty(deferredCleanupItem?.WavPath) && File.Exists(deferredCleanupItem.WavPath))
					{
						File.Delete(deferredCleanupItem.WavPath);
					}
				}
				catch
				{
				}
				try
				{
					if (!string.IsNullOrEmpty(deferredCleanupItem?.XmlPath) && File.Exists(deferredCleanupItem.XmlPath))
					{
						File.Delete(deferredCleanupItem.XmlPath);
					}
				}
				catch
				{
				}
			}
			_deferredCleanupQueue.Clear();
			try
			{
				Interlocked.Exchange(ref _deferredCleanupStableSinceUtcTicks, 0L);
			}
			catch
			{
			}
		}
		if (list != null)
		{
			for (int i = 0; i < list.Count; i++)
			{
				StopAgentRhubarbRecordIfPossible(list[i], "StopAllLipSyncPlaybackAndCleanup");
			}
		}
	}

	internal void TickLipSyncAnimations(float dt)
	{
		List<int> list;
		lock (_speakingLock)
		{
			if (_speakingAgentIndices.Count == 0)
			{
				return;
			}
			list = new List<int>(_speakingAgentIndices);
		}
		Mission mission = Mission.Current;
		var agents = mission?.Agents;
		if (agents == null)
		{
			return;
		}
		foreach (int agentIndex in list)
		{
			try
			{
				Agent agent = agents.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex);
				if (agent == null || !agent.IsActive())
				{
					Report("AgentInvalidDuringLipSync", agentIndex);
					CancelAgentSpeechForRemoval(agentIndex, "agent_invalid_during_lipsync");
					continue;
				}
				string reason = "";
				if (_ports.CanLipSync(agent, out reason))
				{
					continue;
				}
				DetachAgentLipSyncForSafety(agentIndex, reason);
			}
			catch
			{
			}
		}
	}

	internal HashSet<int> GetSpeakingAgentIndicesSnapshot()
	{
		lock (_speakingLock)
		{
			return new HashSet<int>(_speakingAgentIndices);
		}
	}

	internal sealed class DeferredCleanupItem
	{
		public long ItemId;

		public SoundEvent SoundEvent;

		public string WavPath;

		public string XmlPath;

		public long EnqueuedUtcTicks;

		public string Source;

		public int AgentIndex;
	}

	internal sealed class TtsPlaybackOwner
	{
		public TtsEngine.PlaybackRequest Request;
		public long RuntimeGeneration;
		public int SceneSessionId;
		public int ConversationEpoch;
		public Mission Mission;
		public Action PrepareSceneOutput;
		public bool Prepared;
	}

	internal void CleanupPreviousLipSyncPlaybackForReplacement(string reason)
	{
		List<int> list = new List<int>();
		List<SoundEvent> list2 = new List<SoundEvent>();
		List<string> list3 = new List<string>();
		List<string> list4 = new List<string>();
		lock (_speakingLock)
		{
			HashSet<int> hashSet = new HashSet<int>();
			foreach (int key in _agentSoundEvents.Keys)
			{
				hashSet.Add(key);
			}
			foreach (int key2 in _agentWavPaths.Keys)
			{
				hashSet.Add(key2);
			}
			foreach (int key3 in _agentXmlPaths.Keys)
			{
				hashSet.Add(key3);
			}
			foreach (int item in hashSet)
			{
				SoundEvent value = null;
				string value2 = null;
				string value3 = null;
				if (_agentSoundEvents.TryGetValue(item, out value))
				{
					_agentSoundEvents.Remove(item);
				}
				if (_agentWavPaths.TryGetValue(item, out value2))
				{
					_agentWavPaths.Remove(item);
				}
				if (_agentXmlPaths.TryGetValue(item, out value3))
				{
					_agentXmlPaths.Remove(item);
				}
				_agentLipSyncDetachedForSafety.Remove(item);
				if (value != null || !string.IsNullOrWhiteSpace(value2) || !string.IsNullOrWhiteSpace(value3))
				{
					list.Add(item);
					list2.Add(value);
					list3.Add(value2);
					list4.Add(value3);
				}
			}
		}
		for (int i = 0; i < list.Count; i++)
		{
			int num = list[i];
			StopAgentRhubarbRecordIfPossible(num, reason);
			if (!IsInEscapeTransitionWindow())
			{
				SafeStopAndReleaseSoundEvent(list2[i]);
			}
			QueueDeferredCleanup(null, list3[i], list4[i], reason, num);
		}
	}

	internal static void PrepareAgentForTrueLipSyncIfPossible(Agent agent)
	{
		if (agent == null || !agent.IsHuman)
		{
			return;
		}
		try
		{
			string defaultFaceIdle = "";
			try
			{
				Type type = AppDomain.CurrentDomain.GetAssemblies().Select((Assembly a) => a?.GetType("TaleWorlds.CampaignSystem.CharacterHelper", throwOnError: false) ?? a?.GetType("TaleWorlds.CampaignSystem.Helpers.CharacterHelper", throwOnError: false)).FirstOrDefault((Type t) => t != null);
				MethodInfo methodInfo = type?.GetMethod("GetDefaultFaceIdle", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[1] { typeof(CharacterObject) }, null);
				defaultFaceIdle = (methodInfo?.Invoke(null, new object[1] { agent.Character as CharacterObject }) as string) ?? "";
			}
			catch
			{
				defaultFaceIdle = "";
			}
			if (!string.IsNullOrWhiteSpace(defaultFaceIdle))
			{
				agent.SetAgentFacialAnimation(Agent.FacialAnimChannel.Mid, defaultFaceIdle, true);
			}
			agent.SetAgentFacialAnimation(Agent.FacialAnimChannel.High, "", false);
			LogLipSyncNativeProbe("PrepareTrueLipSyncFaceIdle", agent.Index, "idle=" + (defaultFaceIdle ?? ""));
		}
		catch (Exception ex)
		{
			Logger.Log("LipSync", $"[WARN] PrepareAgentForTrueLipSyncIfPossible failed, agentIndex={agent.Index}, error={ex.Message}");
			BannerlordExceptionSentinel.ReportObservedException("LipSync.PrepareTrueLipSyncFaceIdle", ex, "agentIndex=" + agent.Index);
		}
	}

	internal static void LogLipSyncNativeProbe(string stage, int agentIndex, string extra = null)
	{
		try
		{
			int managedThreadId = -1;
			try
			{
				managedThreadId = Thread.CurrentThread.ManagedThreadId;
			}
			catch
			{
			}
			string sceneName = "";
			try
			{
				sceneName = Mission.Current?.SceneName ?? "";
			}
			catch
			{
			}
			string suffix = string.IsNullOrWhiteSpace(extra) ? string.Empty : ", " + extra;
			Logger.LogVerbose("LipSyncProbe", "probe:" + (stage ?? "") + ":" + agentIndex, () => $"stage={stage}, agentIndex={agentIndex}, thread={managedThreadId}, scene={sceneName}{suffix}", 2.0);
		}
		catch
		{
		}
	}



	internal void StopAgentRhubarbRecordIfPossible(int agentIndex, string reason = "Unknown")
	{
		Mission mission = Mission.Current;
		var agents = mission?.Agents;
		if (agentIndex < 0 || agents == null)
		{
			Report("StopRhubarbRecord.Skip", agentIndex, "reason=" + reason + ";missionNull=" + (mission == null));
			return;
		}
		try
		{
			Agent agent = agents.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex && a.IsActive());
			if (agent == null || agent.AgentVisuals == null)
			{
				Report("StopRhubarbRecord.Skip", agentIndex, $"reason={reason};agentMissing={(agent == null)};visualsMissing={(agent != null && agent.AgentVisuals == null)}");
				return;
			}
			LogLipSyncNativeProbe("StopRhubarbRecord.Before", agentIndex, "reason=" + reason);
			agent.AgentVisuals.StartRhubarbRecord("", -1);
			LogLipSyncNativeProbe("StopRhubarbRecord.After", agentIndex, "reason=" + reason);
			Logger.Log("LipSync", $"[Rhubarb] StopRhubarbRecord called, agentIndex={agentIndex}, reason={reason}");
			Report("StopRhubarbRecord", agentIndex, "reason=" + reason);
		}
		catch (Exception ex)
		{
			Logger.Log("LipSync", $"[WARN] StopRhubarbRecord failed, agentIndex={agentIndex}, reason={reason}, error={ex.Message}");
			Report("StopRhubarbRecord.Failed", agentIndex, $"reason={reason};error={ex.Message}");
			BannerlordExceptionSentinel.ReportObservedException("LipSync.StopRhubarbRecord", ex, $"agentIndex={agentIndex};reason={reason}");
		}
	}

	internal void DetachAgentLipSyncForSafety(int agentIndex, string reason)
	{
		if (agentIndex < 0)
		{
			return;
		}
		bool hadSpeakingEntry = false;
		bool alreadyDetached = false;
		lock (_speakingLock)
		{
			hadSpeakingEntry = _speakingAgentIndices.Remove(agentIndex);
			alreadyDetached = !_agentLipSyncDetachedForSafety.Add(agentIndex);
		}
		StopAgentRhubarbRecordIfPossible(agentIndex, "SafetyDetach:" + reason);
		Logger.Log("LipSync", $"[SAFEGUARD] Detached scene lipsync for agentIndex={agentIndex}, reason={reason}, hadSpeaking={hadSpeakingEntry}, alreadyDetached={alreadyDetached}");
		Report("LipSyncSafeguard.Detached", agentIndex, $"reason={reason};hadSpeaking={hadSpeakingEntry};alreadyDetached={alreadyDetached}");
	}

	internal void InterruptAgentSpeechForCombat(int agentIndex, string reason)
	{
		if (agentIndex < 0)
		{
			return;
		}
		bool speaking = false;
		SoundEvent soundEvent = null;
		string wavPath = null;
		string xmlPath = null;
		lock (_speakingLock)
		{
			speaking = _speakingAgentIndices.Remove(agentIndex);
			if (_agentSoundEvents.TryGetValue(agentIndex, out soundEvent))
			{
				_agentSoundEvents.Remove(agentIndex);
			}
			if (_agentWavPaths.TryGetValue(agentIndex, out wavPath))
			{
				_agentWavPaths.Remove(agentIndex);
			}
			if (_agentXmlPaths.TryGetValue(agentIndex, out xmlPath))
			{
				_agentXmlPaths.Remove(agentIndex);
			}
			_agentLipSyncDetachedForSafety.Remove(agentIndex);
		}
		_ports.ClearAgentPending(agentIndex);
		_ports.ClearAgentInteraction(agentIndex);
		bool interrupted = false;
		try
		{
			interrupted = TtsEngine.Instance?.InterruptCurrentPlaybackForAgent(agentIndex, reason) ?? false;
		}
		catch
		{
			interrupted = false;
		}
		if (!speaking && soundEvent == null && string.IsNullOrWhiteSpace(wavPath) && string.IsNullOrWhiteSpace(xmlPath) && !interrupted)
		{
			if (_ports.SuppressMeetingControl())
			{
				_ports.ClearSpeechQueue();
				_ports.ClearMeetingControl();
			}
			return;
		}
		Report("InterruptAgentSpeechForCombat", agentIndex, $"reason={reason};speaking={speaking};interrupted={interrupted};hadSe={(soundEvent != null)};hadWav={(!string.IsNullOrWhiteSpace(wavPath))};hadXml={(!string.IsNullOrWhiteSpace(xmlPath))}");
		StopAgentRhubarbRecordIfPossible(agentIndex, "Interrupt:" + reason);
		QueueDeferredCleanup(soundEvent, wavPath, xmlPath, "Interrupt:" + reason, agentIndex);
		if (_ports.SuppressMeetingControl())
		{
			_ports.ClearSpeechQueue();
			_ports.ClearMeetingControl();
		}
	}

	internal void CancelAgentSpeechForRemoval(int agentIndex, string reason)
	{
		if (agentIndex < 0)
		{
			return;
		}
		bool speaking = false;
		SoundEvent soundEvent = null;
		string wavPath = null;
		string xmlPath = null;
		lock (_speakingLock)
		{
			speaking = _speakingAgentIndices.Remove(agentIndex);
			if (_agentSoundEvents.TryGetValue(agentIndex, out soundEvent))
			{
				_agentSoundEvents.Remove(agentIndex);
			}
			if (_agentWavPaths.TryGetValue(agentIndex, out wavPath))
			{
				_agentWavPaths.Remove(agentIndex);
			}
			if (_agentXmlPaths.TryGetValue(agentIndex, out xmlPath))
			{
				_agentXmlPaths.Remove(agentIndex);
			}
			_agentLipSyncDetachedForSafety.Remove(agentIndex);
		}
		_ports.ClearAgentPending(agentIndex);
		_ports.ClearAgentInteraction(agentIndex);
		bool interrupted = false;
		try
		{
			interrupted = TtsEngine.Instance?.InterruptCurrentPlaybackForAgent(agentIndex, reason) ?? false;
		}
		catch
		{
			interrupted = false;
		}
		if (!speaking && soundEvent == null && string.IsNullOrWhiteSpace(wavPath) && string.IsNullOrWhiteSpace(xmlPath) && !interrupted)
		{
			Report("CancelAgentSpeechForRemoval.Noop", agentIndex, "reason=" + reason);
			return;
		}
		Report("CancelAgentSpeechForRemoval", agentIndex, $"reason={reason};speaking={speaking};interrupted={interrupted};hadSe={(soundEvent != null)};hadWav={(!string.IsNullOrWhiteSpace(wavPath))};hadXml={(!string.IsNullOrWhiteSpace(xmlPath))}");
		StopAgentRhubarbRecordIfPossible(agentIndex, "Removed:" + reason);
		QueueDeferredCleanup(soundEvent, wavPath, xmlPath, "Removed:" + reason, agentIndex);
	}



	internal readonly HashSet<int> _speakingAgentIndices = new HashSet<int>();

	internal readonly object _speakingLock = new object();

	internal readonly Dictionary<int, SoundEvent> _agentSoundEvents = new Dictionary<int, SoundEvent>();

	internal readonly Dictionary<int, string> _agentWavPaths = new Dictionary<int, string>();

	internal readonly Dictionary<int, string> _agentXmlPaths = new Dictionary<int, string>();

	internal readonly HashSet<int> _agentLipSyncDetachedForSafety = new HashSet<int>();

	internal Action<TtsEngine.PlaybackRequest, string, string, float> _ttsOnAudioFileReadyHandler;

	internal Action<TtsEngine.PlaybackRequest> _ttsOnPlaybackStartedHandler;

	internal Action<TtsEngine.PlaybackRequest> _ttsOnPlaybackFinishedHandler;

	internal Action<TtsEngine.PlaybackRequest, string> _ttsOnPlaybackFailedHandler;

	internal Action<TtsEngine.PlaybackRequest> _ttsOnPlaybackCancelledHandler;

	internal long _lastEscapePressedUtcTicks = 0L;

	internal bool _wasGameWindowFocused = true;

	internal bool _ttsPausedByShoutUi = false;

	internal bool _ttsPausedByInterruption = false;

	internal readonly List<DeferredCleanupItem> _deferredCleanupQueue = new List<DeferredCleanupItem>();

	internal long _deferredCleanupStableSinceUtcTicks = 0L;

	internal const double DeferredCleanupStableWindowSeconds = 1.5;

	internal const double DeferredCleanupMinAgeSeconds = 0.5;

	internal const double DeferredCleanupMaxAgeSeconds = 20.0;

	internal const int DeferredCleanupBatchSize = 8;

	internal static readonly object _ttsEventSubLock = new object();

	internal static SceneAudioLipSyncController _ttsEventSubscribedOwner = null;

	internal static readonly uint _currentProcessId = (uint)Process.GetCurrentProcess().Id;

	internal readonly bool _enableRhubarbSoundEventPlayback = true;

	internal readonly Dictionary<long, TtsPlaybackOwner> _ttsPlaybackOwners = new Dictionary<long, TtsPlaybackOwner>();

	internal readonly Dictionary<int, long> _activeTtsPlaybackRequests = new Dictionary<int, long>();

 private void Report(string stage, int agentIndex, string extra = null) => _ports.Report(stage, agentIndex, extra);
	internal void HandleTtsPlaybackFailed(TtsEngine.PlaybackRequest request, string errorMessage)
	{
		if (!IsTtsPlaybackRequestCurrent(request)) { return; }
		if (!PrepareTtsPlaybackRequest(request) || !IsActiveTtsPlaybackRequest(request)) { return; }
		int agentIndex = request.AgentIndex;
		bool releaseNativeTypewriter = _ports.CompleteNativeWait(request, "playback_failed");
		if (releaseNativeTypewriter)
		{
			ConversationHelper.StartTypewriterPlaybackIfWaiting();
		}
		bool hasAgent = agentIndex >= 0;
		SceneAudioFailureOutput output = _ports.FailureOutput(agentIndex);
		long interactionToken = output.InteractionToken;
		bool hasInteractionToken = output.HasInteractionToken;
		if (hasAgent)
		{
			lock (_speakingLock)
			{
				_speakingAgentIndices.Remove(agentIndex);
				_agentLipSyncDetachedForSafety.Remove(agentIndex);
			}
			_ports.UnmarkPlaybackStarted(agentIndex);
		}
		SceneAudioFailureOutput bubble = output.HasBubble ? output : null;
		float typingDuration = output.TypingDuration;
		bool hasBubble = hasAgent && output.HasBubble;
		bool suppressUserErrorMessage = (errorMessage ?? "").IndexOf("Scene speech target became unavailable", StringComparison.OrdinalIgnoreCase) >= 0;
		string failText = string.IsNullOrWhiteSpace(errorMessage) ? "语音合成失败，已切换为文字气泡。" : ("语音合成失败：" + errorMessage);
		Logger.Log("LipSync", $"[OnPlaybackFailed] agentIndex={agentIndex}, error={errorMessage}");
		Report("PlaybackFailed", agentIndex, $"error={errorMessage};hasInteractionToken={hasInteractionToken};hasBubble={hasBubble};typingDuration={typingDuration:F2}");
		RunTtsMainThreadEventStep(delegate
		{
			if (!IsTtsPlaybackRequestCurrent(request)) { return; }
			try
			{
				if (hasAgent)
				{
					_ports.ConvertDialogueFeed(agentIndex, (typingDuration > 0f) ? typingDuration : ((hasBubble && bubble != null) ? _ports.EstimateTypingDuration(bubble.UiContent) : 0f));
				}
				if (hasBubble && bubble != null)
				{
					_ports.ShowBubble(bubble.Agent, bubble.UiContent, typingDuration);
					if (hasInteractionToken)
					{
						_ports.ScheduleInteractionTimeout(agentIndex, interactionToken, (typingDuration > 0f) ? typingDuration : _ports.EstimateTypingDuration(bubble.UiContent));
					}
				}
				_ports.FlushFailedSpeechEffects(agentIndex, (typingDuration > 0f) ? typingDuration : 0f);
			}
			catch
			{
			}
			try
			{
				if (!suppressUserErrorMessage)
				{
					InformationManager.DisplayMessage(new InformationMessage("[TTS错误] " + failText, new Color(1f, 0.35f, 0.35f)));
				}
			}
			catch
			{
			}
			try
			{
				if (hasAgent)
				{
					StopAgentRhubarbRecordIfPossible(agentIndex, "PlaybackFailed");
					SoundEvent value = null;
					string value2 = null;
					string value3 = null;
					lock (_speakingLock)
					{
						if (_agentSoundEvents.TryGetValue(agentIndex, out value))
						{
							_agentSoundEvents.Remove(agentIndex);
						}
						if (_agentWavPaths.TryGetValue(agentIndex, out value2))
						{
							_agentWavPaths.Remove(agentIndex);
						}
						if (_agentXmlPaths.TryGetValue(agentIndex, out value3))
						{
							_agentXmlPaths.Remove(agentIndex);
						}
						_agentLipSyncDetachedForSafety.Remove(agentIndex);
					}
					QueueDeferredCleanup(value, value2, value3, "PlaybackFailed", agentIndex);
					Report("PlaybackFailed.CleanupQueued", agentIndex, $"hasSe={(value != null)};hasWav={(!string.IsNullOrWhiteSpace(value2))};hasXml={(!string.IsNullOrWhiteSpace(value3))}");
				}
			}
			catch
			{
			}
		});
	}
}

internal delegate bool SceneLipSyncAgentCheck(Agent agent, out string reason);
internal delegate bool SceneLipSyncIndexCheck(int agentIndex, out string reason);
internal sealed class SceneAudioLipSyncPort
{
 internal Func<int> SceneSessionId, ConversationEpoch;
 internal Action<Action> Dispatch;
 internal Action<TtsEngine.PlaybackRequest> Cancelled, ScheduleBubbleFallback;
 internal Func<TtsEngine.PlaybackRequest, string, bool> CompleteNativeWait;
 internal Action<int> UnmarkPlaybackStarted;
 internal Func<int, SceneAudioFailureOutput> FailureOutput;
 internal Action<int, float> ConvertDialogueFeed, FlushFailedSpeechEffects;
 internal Action<Agent, string, float> ShowBubble;
 internal Action<int, long, float> ScheduleInteractionTimeout;
 internal Func<string, float> EstimateTypingDuration;
 internal Action<int> Finished, ClearAgentPending, ClearAgentInteraction, ClearOrphanDuration, MarkPlaybackStarted;
 internal Action<string, int, string> Report;
 internal Action<int, float> AudioDuration;
 internal Func<int, bool, bool> DispatchBubble;
 internal Func<int, bool> HasPlaybackStarted, CanParticipate;
 internal Func<TtsEngine.PlaybackRequest, bool> IsNativeWait;
 internal Func<TtsEngine.PlaybackRequest, string, string, float, bool> QueueMapPlayback;
 internal Func<bool> UseMapPlayback, SpeechBusy, SuppressMeetingControl;
 internal SceneLipSyncAgentCheck CanLipSync;
 internal SceneLipSyncIndexCheck CanLipSyncIndex;
 internal Action<bool> PauseTyping;
 internal Action StopTyping, ClearPending, ClearSpeechQueue, ClearMeetingControl;
}

internal sealed class SceneAudioFailureOutput
{
 internal bool HasInteractionToken, HasBubble;
 internal long InteractionToken;
 internal Agent Agent;
 internal string UiContent;
 internal float TypingDuration;
}

internal struct SceneAudioResourceSnapshot { internal bool Speaking, HasSound, HasWav, HasXml; }
