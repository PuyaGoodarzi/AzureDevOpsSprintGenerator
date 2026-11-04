# IterationPlanner

Initial local scheduling implementation, ASP.NET Core / Blazor Server, .NET 10 LTS.

Run from this directory in PowerShell:

```powershell
$env:DOTNET_CLI_HOME=$PWD.Path
dotnet run --project IterationPlanner
dotnet run --project IterationPlanner.Tests
```

SDK pinned to installed 10.0.302. Restore may require NuGet connectivity for SDK framework assets.

Implemented: Persian conversion, inclusive durations, forward weekday adjustment requiring acceptance, day cadence rotation warning, branch/project filtering, independent continuation and number suggestions, read-only existing sprint context, future overlap blocking, exact-match skip, JSON preview.

Existing data input is a JSON array of Sprint records: Project, Path (slash-separated), Start and Finish (ISO dates). Only dated sprint leaves belong in this input; it is not an Azure tree importer.

Limitations: Azure connection/tree retrieval/publication, SQLite profiles/drafts, hierarchy generation, parent date conflict resolution, editing and CSV/report exports are pending. No Azure mutations are performed. Gap acceptance is displayed but there is no publication flow to enforce it. Multi-project validation currently requires selecting and previewing each project separately. This is an initial implementation, not the complete requested MVP.

API reference for subsequent adapter implementation: https://learn.microsoft.com/en-us/rest/api/azure/devops/wit/classification-nodes/create-or-update?view=azure-devops-rest-7.1 . No server compatibility has been tested.

Suggested commit: feat: add Persian sprint cadence and continuation preview

## Azure API configuration

Edit `IterationPlanner/appsettings.json` (or the deployed appsettings.json beside the application):

```json
"AzureDevOps": {
  "CollectionUrl": "https://azure-server/tfs/DefaultCollection",
  "Pat": "",
  "ApiVersion": "7.0",
  "TimeoutSeconds": 30
}
```

CollectionUrl includes the collection, not a project or `_apis` suffix. An empty URL allows offline preview. Choose an API version supported by your Server installation; the default is configurable and does not imply tested compatibility. Restart the application after changing these settings.

Environment variables override JSON without rebuilding:

```powershell
$env:AzureDevOps__CollectionUrl = 'https://azure-server/tfs/DefaultCollection'
$env:AzureDevOps__ApiVersion = '7.0'
$env:AzureDevOps__TimeoutSeconds = '60'
dotnet run --project IterationPlanner
```

The Pat field accepts your personal access token in the deployed appsettings.json. This stores the token as plaintext; restrict file access and do not commit a populated file. The checked-in default is empty. You can alternatively override it using AzureDevOps__Pat. Never include the PAT in the URL. These settings are bound and validated on startup for server-side consumption using `IOptions<AzureDevOpsOptions>`. The Azure HTTP adapter and publication remain pending; configuring these values does not yet connect the local preview to Azure.


Naming patterns support `{n}` (required sprint number) and `{month}` (Persian month name of the sprint start date). Example: `Sprint {month} {n}`. A sprint crossing a month boundary retains its starting month name.

Existing sprint JSON can be pasted into the existing-sprints text area as an array with Project, Path, Start, Finish fields. Dates must use Gregorian ISO yyyy-MM-dd. File-upload import and profile import are not yet implemented. The generated preview envelope is not the same format as this input array.

Naming presets now include both `Sprint {n} - هفته {week} {month}` and `هفته {week} {month} - Sprint {n}`, plus Persian sprint labels and year/month alternatives. `{week}` is the ordinal bucket of the Persian start day: 1–7 first, 8–14 second, 15–21 third, 22–28 fourth, 29–31 fifth. It is not an ISO or weekday-aligned calendar week. `{year}` is the Persian start year. The UI shows a live name example using the effective aligned start. Multiweek sprints use their starting week. Custom patterns still require `{n}`.

Project loading is now available via the Azure button in the generation window. Configure CollectionUrl, Pat and ApiVersion, restart, then load and explicitly select a Project. Requests run server-side and include pagination; redirects are disabled so credentials are not followed to a login destination. Browser sign-in is not used by the app. Iteration retrieval and publication are still pending.

Azure iteration loading and explicit create-only publication are now implemented. Select the Project, load its iteration tree, enter its target branch (relative slash-separated path), preview, resolve adjustments/gaps, then approve and publish. Parents are created without dates; existing parents are reused without date updates. Existing dated leaf overlaps and parent date bounds block creation. State is re-read before publication and between sprint operations. Unknown write outcomes stop execution and require a refreshed tree; completed writes remain. Team iteration selection is not performed. Real Server connectivity and mutation permissions still require operator verification. API shape follows Microsoft Classification Nodes documentation: https://learn.microsoft.com/en-us/rest/api/azure/devops/wit/classification-nodes/create-or-update?view=azure-devops-rest-7.1 . API version comes from configuration, including 6.0.

Build verified in a separate output folder because the running application locks its normal binaries. Initial publication implementation does not yet include cancellation, durable execution history, bounded read retries or atomic protection against concurrent external writers. Use a single operator and review results after partial failures.

Hierarchy selection now supports Year/Sprint, Year/Quarter/Sprint, Year/Month/Sprint, Year/Quarter/Month/Sprint and custom parent paths. UI defaults to the complete hierarchy. Quarter labels Q1–Q4 follow Persian months (Q1 Farvardin–Khordad), not Gregorian quarters. Parents are grouping-only. Sprint start dates determine grouping, including crossing sprints. Optional parent prefix is empty by default to avoid repeating the year. Existing published sprints are never moved; overlaps with old flat sprints must be resolved before publishing new hierarchical proposals. Preview includes a recursive tree.

English UI: use the English / فارسی button on the planner. Selection lasts for the current connected session. English switches the application layout to LTR. Persian calendar dates, month names, stored naming templates and published Azure iteration names stay unchanged. Some detailed server-generated diagnostics and dynamic count summaries remain Persian. Refresh resets the language to Persian.

The public app is now named IterationPlanner. Run `dotnet run --project IterationPlanner`; checks: `dotnet run --project IterationPlanner.Tests`. Machine-specific connection credentials are preserved in `IterationPlanner/appsettings.Local.json`, excluded by .gitignore. Public appsettings.json contains empty URL/PAT defaults. Environment variables override private settings. Never upload the private file. Old locked build artifacts are excluded from source control.
