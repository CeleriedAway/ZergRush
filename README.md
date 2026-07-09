# ZergRush Workspace

This repository is now organized as a workspace for the ZergRush libraries.

## Projects

- `src/ZergRush.Reactive` - clean C# reactive library. It should stay focused on reactive primitives and their required infrastructure.
- `src/ZergRush.CodeGen.Abstractions` - shared codegen attributes, flags, interfaces, and runtime contracts.
- `src/ZergRush.CodeGen` - codegen parser and generator library.
  - `OriginalCodeGen` contains the old Unity-editor-era generator implementation moved out of the Unity package.
- `src/ZergRush.CodeGen.Cli` - command line wrapper for the codegen library.
- `packages/com.celeriedaway.zergrush` - Unity package containing Unity tools, editor integration, reactive UI, and samples.
  - `Runtime/UnityTools/Utils/Tools` contains useful general-purpose helpers that are not required by the clean reactive library.
- `legacy` - preserved old harnesses, generated references, Unity metadata, and source copies that should not be part of the clean project surface yet.

## Notes

This pass is structural only. The moved projects are intentionally not treated as compile-clean yet.
