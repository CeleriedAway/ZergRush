<!-- codebase-memory-mcp:start -->
# Codebase Knowledge Graph (codebase-memory-mcp)

This project uses codebase-memory-mcp to maintain a knowledge graph of the codebase.
ALWAYS prefer MCP graph tools over grep/glob/file-search for code discovery.

## Priority Order
1. `search_graph` - find functions, classes, routes, variables by pattern
2. `trace_path` - trace who calls a function or what it calls
3. `get_code_snippet` - read specific function/class source code
4. `query_graph` - run Cypher queries for complex patterns
5. `get_architecture` - high-level project summary

## When to fall back to grep/glob
- Searching for string literals, error messages, config values
- Searching non-code files (Dockerfiles, shell scripts, configs)
- When MCP tools return insufficient results
<!-- codebase-memory-mcp:end -->

# ZergRush Workspace Notes

The workspace is being reshaped into three publishable parts:

- `ZergRush.Reactive` is the clean C# reactive library.
- `ZergRush.CodeGen` is the generator library, with `ZergRush.CodeGen.Cli` as the dotnet tool entrypoint.
- `packages/com.celeriedaway.zergrush` is the Unity package for Unity tools, editor integration, reactive UI, and samples.

`ZergRush.CodeGen.Abstractions` is a supporting library for attributes, flags, interfaces, and runtime contracts that generated user code can reference without depending on the full generator.

`src/ZergRush.CodeGen/OriginalCodeGen` contains the original `CodeGen*.cs`, `ConsoleGen`, `EnumTable`, and context builder implementation that used to live in the Unity package editor folder. The newer refactor parser/model files (`ZRCodeParser.cs`, `ZRTypes.cs`) remain directly under `src/ZergRush.CodeGen`.

The current restructure is intentionally folder-first. Do not assume these projects compile until a later cleanup pass fixes references, namespaces, asmdefs, and package dependencies.

Generated `*.gen.cs` files are reference material unless the user explicitly asks to modify or regenerate them.

Keep `src/ZergRush.Reactive` focused on the required reactive API and infrastructure. General-purpose helpers such as container extensions, CSV parsing, random helpers, cycle buffers, and smoothing filters live in the Unity package under `packages/com.celeriedaway.zergrush/Runtime/UnityTools/Utils/Tools`.

Legacy material is preserved under `legacy/`:

- `OldCodeGenConsole` is the old console launcher from the Unity package.
- `OldPureCSharpCodeGenCore` is the previous `Assets/ZergRush/PureCSharp/CodeGenCore` copy.
- `GeneratedReference` contains generated folders moved out of clean C# source projects.
- `UnityMeta` contains Unity metadata moved out of clean C# source projects.
- `ZRRefactorHarness` is the old temporary linked-file harness.
- `ZRNewCodeGenOriginalProject` preserves the old standalone project file and build artifacts from the refactor checkout.
