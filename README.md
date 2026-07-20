# ZergRush Unity Tools

This repository is the Unity wrapper for the ZergRush libraries. Core C# source lives in independent repositories and is pinned here as Git submodules.

## Source repositories

- [ZergRush.Reactive](https://github.com/CeleriedAway/ZergRush.Reactive) owns the reactive runtime.
- [ZergRush.CodeGen](https://github.com/CeleriedAway/ZergRush.CodeGen) owns CodeGen abstractions, the generator, CLI, tests, and CodeGen samples.
- `packages/com.celeriedaway.zergrush` owns Unity editor integration, reactive UI, Unity-only utilities, and Unity samples.

## Clone for development

```sh
git clone --branch rework --recurse-submodules https://github.com/CeleriedAway/ZergRush.git
```

For an existing checkout:

```sh
git submodule update --init --recursive
```

## Layout

```text
packages/com.celeriedaway.zergrush/
├── Runtime/ZergRush.Reactive   -> ZergRush.Reactive submodule
├── Runtime/ZergRush.CodeGen    -> ZergRush.CodeGen submodule
├── Runtime/UnityTools          -> Unity-specific runtime integration
├── Editor                      -> Unity editor integration and local CLI bridge
└── Samples~                    -> Unity-specific samples
```

## Development workflow

Make core changes inside the relevant submodule, commit and push them there, then commit the updated submodule pointer in this repository. Do not copy core source back into the wrapper.

The Unity editor bridge builds the CodeGen CLI from the CodeGen submodule into its ignored `Tools~/.build` directory. NuGet and OpenUPM publishing remain deferred while the source/submodule workflow is being validated.
