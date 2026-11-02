# CommonLogicNodes

Logic nodes for Gira X1/L1 (Gira Logic Nodes SDK), C#, .NET Framework 4.0, classic csproj files.

## Build / test
`./build.ps1` builds all nodes unsigned, runs NUnit tests and writes ZIPs to `Zip\`.
Prerequisites: .NET SDK 8 (compiler), `packages\` (NuGet packages referenced by the csproj files, gitignored), `LogicNodesSDK\` (Gira SDK DLLs/tools from the Gira Logic Nodes SDK, gitignored).
Build one node: `dotnet build <Node>\<Node>.csproj -c Release "/p:PostBuildEvent="`.

## Rules
- Never read or commit certificates or keys (`*.p12`, `*.xdb`, password files). Signing only on explicit request via `./build.ps1 -Sign`; certificate path and password are read from `build.local.psd1` (gitignored; template: `build.local.psd1.template`). The csproj post-build event also signs; always override it with `/p:PostBuildEvent=`.
- Every node class must be listed in the node's `Manifest.json` and vice versa, otherwise `LogicNodeTool create` fails. Bump `Version` in Manifest.json for releases.
- Keep target framework v4.0; only use APIs available in the Gira runtime.
- Tag releases as `<Node>_<version>`, e.g. `HuaweiModbus_0.0.10`.

## Known state
- LogicNodesTest contains only the empty template test; node logic has no unit tests yet.
