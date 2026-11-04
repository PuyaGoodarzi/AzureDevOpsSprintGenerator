# Developer changes

Created the initial Blazor application in an empty workspace. No AGENTS.md was present in the workspace or immediate parent.

Planner.cs contains a pure date-only schedule engine. Home.razor separates cadence and generation window and displays existing/proposed records separately. The console test project exercises alignment, continuation, gaps, future overlap, project isolation, preservation and idempotency without Azure credentials or test packages.

Existing Sprint records are immutable; generation returns new proposals and cannot mutate earlier dates or assignments. Work-item operations are absent.

Build validation initially failed because NuGet access was unavailable. See execution output for subsequent attempts. Remaining scope is listed in README.md.

Added externally configurable AzureDevOps collection URL, API version and timeout in appsettings.json, with typed options and startup validation. Documented JSON and environment overrides. No credentials are stored. Azure calls remain unimplemented.

Added a Pat field to appsettings.json at the operator’s request and bound it through AzureDevOpsOptions. The repository value is empty; JSON serialization excludes the token property. A populated configuration file contains a plaintext secret and must remain private. Authentication transport remains pending.
