# Native TTS cleanup regression

Run `python tests/modules/AF.Module.Conversation/TtsNativeCleanupTests/run.py` from the repository.
The runner extracts current production cleanup and map cancellation methods unchanged, compiles them against deterministic Agent/Visuals/SoundEvent leaves, and checks death, cancellation identity, queued requests, mission exit and detach-before-stop ordering. Host lifecycle routing checks are static. This does not execute TaleWorlds.Native, WinMM or a real Mission.
Outputs: `.tmp/tts-native-cleanup-tests-20261103/`.
