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

# ZergRush Unity Wrapper Notes

This repository owns only the Unity wrapper package at `packages/com.celeriedaway.zergrush`.

Canonical source ownership:

- `Runtime/ZergRush.Reactive` is the `ZergRush.Reactive` Git submodule and owns the reactive runtime.
- `Runtime/ZergRush.CodeGen` is the `ZergRush.CodeGen` Git submodule and owns CodeGen abstractions, generator/CLI source, tests, and CodeGen samples.
- `Runtime/UnityTools` owns Unity-only runtime integration, reactive UI, and general Unity helpers.
- `Editor` owns Unity editor integration and the source-local CodeGen CLI bridge.
- Root `Samples~` contains only Unity-specific samples. Do not duplicate samples already owned by a submodule.

Do not recreate top-level `src`, `tests`, or `legacy` folders in this wrapper repository.

For core work, edit the appropriate submodule, run that repository's tests, commit and push the child repository, then commit the updated gitlink here. Preserve unrelated changes in both the wrapper and submodule worktrees.

The CodeGen engine and CLI stay under the CodeGen submodule's `Tools~` folder so Unity does not import Roslyn dependencies. Generated `*.gen.cs` files are reference material unless the user explicitly asks to modify or regenerate them.

NuGet and OpenUPM publishing are deferred until the source/submodule workflow is validated.
