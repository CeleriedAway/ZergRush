# ZergRush

This repository packages the ZergRush libraries for Unity and owns a small set of reusable C# utilities shared with backend projects.

## Source repositories

- [ZergRush.Reactive](https://github.com/CeleriedAway/ZergRush.Reactive) owns the reactive runtime.
- [ZergRush.CodeGen](https://github.com/CeleriedAway/ZergRush.CodeGen) owns CodeGen abstractions, the generator, CLI, tests, and CodeGen samples.
- `packages/com.celeriedaway.zergrush` owns shared C# utilities, Unity integration, and Unity samples.

## Clone for development

```sh
git clone --branch main --recurse-submodules https://github.com/CeleriedAway/ZergRush.git
```

For an existing checkout:

```sh
git submodule update --init --recursive
```

## Layout

```text
packages/com.celeriedaway.zergrush/
|-- Runtime/Core                 -> pure C# utilities and backend project
|-- Runtime/Unity                -> Unity-specific runtime integration
|-- Runtime/ZergRush.Reactive    -> ZergRush.Reactive submodule
|-- Runtime/ZergRush.CodeGen     -> ZergRush.CodeGen submodule
|-- Editor                       -> Unity editor integration and local CLI bridge
`-- Samples~                     -> Unity-specific samples
```

## Development workflow

Make Reactive and CodeGen changes inside the relevant submodule, commit and push them there, then commit the updated submodule pointer in this repository. Shared general-purpose helpers belong in `Runtime/Core`; code that references Unity belongs in `Runtime/Unity`.

The Unity editor bridge builds the CodeGen CLI from the CodeGen submodule into a temporary build cache. Coordinated NuGet preview packages are published by the CodeGen repository release workflow. OpenUPM publication remains deferred.

For preview.4, update the wrapper and both submodules and regenerate consumer code. See [serialization upgrade and migration instructions](packages/com.celeriedaway.zergrush/Runtime/ZergRush.CodeGen/UPGRADING-preview.4.md). The wrapper uses runtime-owned primitive serializers, so old consumer helpers no longer conflict.

Run `dotnet run --project packages/com.celeriedaway.zergrush/Tests~/SerializationCompatibility` and repeat with `-p:CombineCoreSources=true` to validate separate and combined assembly layouts. Before publishing a new version, pass `-p:ZergRushReactiveProjectPath=<absolute path to Runtime/ZergRush.Reactive/src/ZergRush.Reactive/ZergRush.Reactive.csproj>` to use the local dependency.
