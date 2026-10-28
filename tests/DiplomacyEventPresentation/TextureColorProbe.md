# Native upload preparation audit

`TextureColorProbe.cs` is a separate .NET Framework console program, excluded from the net8 presentation runner. Compile it with the .NET Framework C# compiler and a reference to `System.Drawing.dll`.

Arguments, in order:

1. Built `AnimusForge.dll` for the API being checked.
2. The repository's `content/modules/AF.Module.Diplomacy/GUI/SpriteParts/af_diplomacy_event` directory.
3. A task-local temporary directory for the transparency/DPI fixture.
4. Assembly resolution directories, starting with the corresponding Bannerlord API reference directory, then the module's private runtime DLL directory.

Run in a fresh process for each API. It invokes the actual `DiplomacyEventSprites.ReadTextureBytes` from the product DLL, checks all nineteen packaged images, requires non-interlaced RGBA8 output and compares every decoded color/alpha byte and image dimension. A translucent RGBA fixture also checks the already-normalized path with nonstandard DPI. It does not launch the game, upload a GPU texture, or prove native rendering.
