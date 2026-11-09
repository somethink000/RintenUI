# Rinten UI

A retained-mode interface toolkit for .NET: panels styled with CSS, laid out with flexbox, written in Razor or C#,
text by Skia and RichTextKit, drawn as signed-distance boxes on the GPU.

It is the interface of the [Rinten](https://github.com/somethink000/Rinten-Engine) engine and editor, and it builds on its own
for native apps.

## Inside

| Folder | What | Assembly |
|---|---|---|
| `Core/` | vectors, colour, `Rect`, `Length`, the html parser, attributes, logging | `Rinten.Core` |
| `Filesystem/` | the files stylesheets, fonts and pictures come from, watched for changes | `Rinten.Filesystem` |
| `Layout/` | flexbox layout | `Rinten.Layout` |
| `UI/` | panels, stylesheets, Razor, controls, text, transitions, input, the panel renderer | `Rinten.UI` |
| `ThirdParty/` | RichTextKit, and the types Razor's compiler calls into | |
| `Native/` | a window for an app: SDL3 for the window and input, OpenGL 3.3 for drawing, the engine's box shader in GLSL | `Rinten.UI.Native` |
| `Razor/` | Rinten's fork of Razor and `RazorGen`, which compiles an app's `.razor` the way the engine compiles a game's | build tool |

## Build

You need the .NET 10 SDK.

```
dotnet build Rinten.UI.slnx
```

## One source, two hosts

The sources are compiled two ways. `*.csproj` here build the library for an app. Rinten compiles the same files into its own
assemblies, with its halves of the partial classes beside them - a panel's `Scene` and `GameObject`, the type library, the mouse.

What differs between the two is said in `UI/Host/`: `UiPlatform` and `UiContext` are partial classes whose members each host
implements - Rinten in `engine/Rinten.Engine/Systems/UI`, an app in `UI/Standalone/` (and `Core/Standalone/`). Folders named
`Standalone` are left out of Rinten's build. An app's backend plugs its textures, device and clipboard into `UiHost`.

## An app

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework></PropertyGroup>
  <ItemGroup><ProjectReference Include="../Rinten-UI/Native/Rinten.UI.Native.csproj" /></ItemGroup>
  <Import Project="../Rinten-UI/Razor/Rinten.UI.Razor.targets" />
</Project>
```

```csharp
using Rinten.UI.Native;

App.Run( "My app", () => new MyRoot() );   // MyRoot : AppRoot, written in Razor or C#
```

- `Microsoft.NET.Sdk`, not `.Razor`: `Rinten.UI.Razor.targets` compiles the `.razor` files with Rinten's Razor. The SDK's writes child
  components as calls the toolkit's render tree leaves empty.
- The app's files are its output folder: fonts in `fonts/`, stylesheets wherever `[StyleSheet( "/path.scss" )]` says.
- `AppRoot` is a root panel the size of the window, scaled with the display, with `OnStart`, `OnUpdate` and `OnDestroy`.
- `AppWindow` is the window as the engine's `AppWindow` puts it: title, frameless with drag areas, minimise and maximise, the system's file boxes.
  `Input.Keyboard.Down/Pressed` reads keys by their bind names.
- `RINTEN_UI_SHOT=frame.png` (and `RINTEN_UI_SHOT_SECONDS`, 2 by default) writes one frame and closes, for looking at an app without a person at it.
- Linux x64 for now: `Native/runtimes/linux-x64/native/libSDL3.so` is the SDL the engine ships. Not drawn yet: `backdrop-filter`, and panels that
  wear the world.

LocalDesk is built this way.

## Rinten

Rinten has this repository as a submodule at `engine/Rinten-UI`.

## License

MIT, see [LICENSE.md](LICENSE.md). Third-party parts keep their own licences, see [NOTICE.md](NOTICE.md).
