# EntraProbe

`EntraProbe` is a small .NET 8 console utility for Windows and macOS that reads the currently signed-in user's Microsoft Entra attributes through delegated Microsoft Graph access and writes only the requested value to standard output. The default output is the user's `department`. It is designed for PowerShell, Intune, Tanium, and similar endpoint-management workflows where clean stdout and stable exit codes matter more than rich UI or background-service behavior.

## Purpose

- Return a single user attribute with no decorative stdout output.
- Fail fast when delegated user lookup is not viable, especially in `SYSTEM`, headless, or non-interactive execution contexts.
- Use silent-only MSAL authentication. The tool does not open browsers, show prompts, or fall back to interactive consent flows.
- Stay small and maintainable for packaging, scripting, and IT Operations ownership.

## Operating Model

- Default property: `department`
- Optional property: `dn`, which maps to `onPremisesDistinguishedName`
- Multiple output: request `department,dn` to emit one `name=value` line per requested field, in that order
- Authentication mode: delegated public-client auth with silent token acquisition only
- Graph scope: delegated `User.Read`
- Graph query: `GET /me?$select=department,onPremisesDistinguishedName`

## Support Matrix

| Platform | Status | Notes |
| --- | --- | --- |
| Windows x64 | Supported | Uses broker-backed MSAL configuration for silent SSO when available. |
| macOS arm64 | Supported | Uses the macOS broker in silent-only mode and requires Company Portal-backed sign-in state. |
| Linux | Not supported | The current silent-only design does not provide a viable enterprise sign-in bootstrap path on Linux. |
| Windows `SYSTEM` context | Not supported | Delegated `/me` lookup must run as the signed-in user. |
| Headless, SSH, or non-interactive sessions | Not supported | The tool intentionally fails instead of attempting interactive auth. |

## Architecture Summary

| Area | Responsibility | Primary files |
| --- | --- | --- |
| Composition root | Wires together concrete services without a DI container | `src/EntraProbe/Program.cs` |
| Application orchestration | Validates options, checks context, acquires token, calls Graph, maps failures to exit codes | `src/EntraProbe/App/ApplicationRunner.cs` |
| Configuration | Merges CLI, environment, and `appsettings.json` values in a fixed precedence order | `src/EntraProbe/Configuration/OptionsLoader.cs` |
| Execution context | Detects supported user-session execution per platform | `src/EntraProbe/Execution/*` |
| Authentication | Acquires silent delegated tokens through MSAL | `src/EntraProbe/Identity/*` |
| Graph access | Calls `/me` and parses only the required properties | `src/EntraProbe/Graph/*` |
| Tests | Covers configuration precedence, orchestration, context evaluation, and Graph failure handling | `tests/EntraProbe.Tests/*` |

For the rationale behind delegated user-context authentication, see [DESIGN-NOTE.md](/Users/david/Documents/Identity/DESIGN-NOTE.md). For day-two ownership guidance, see [OPERATIONS.md](/Users/david/Documents/Identity/OPERATIONS.md).

## Prerequisites

- Windows x64 or macOS arm64 for runtime use
- .NET 8 SDK for local builds
- A Microsoft Entra app registration configured as a public client / desktop application
- Delegated Microsoft Graph permission `User.Read`
- A signed-in user session with reusable silent sign-in state
- On macOS, Company Portal-enrolled broker support for the signed-in user session
- Network access to Microsoft Entra ID and Microsoft Graph

## Microsoft Entra App Registration

1. Open `Identity > Applications > App registrations`.
2. Create a new single-tenant app registration unless you have a specific multitenant requirement.
3. Record the `Application (client) ID` and `Directory (tenant) ID`.
4. Under `Authentication`, add the `Mobile and desktop applications` platform.
5. Add these custom redirect URIs:
   - `ms-appx-web://Microsoft.AAD.BrokerPlugin/<client-id>` for Windows broker support
   - `msauth.com.msauth.unsignedapp://auth` for the unsigned macOS console binary
6. Enable public client flows.
7. Under `API permissions`, add Microsoft Graph delegated `User.Read`.
8. Grant admin consent if your tenant requires pre-consent for the app to run broadly.

Do not add a client secret or certificate for this utility. The app is intentionally implemented as a delegated public client.

## Configuration

Configuration precedence is fixed:

1. Command-line arguments
2. Environment variables
3. `appsettings.json`

### Command-Line Arguments

- `--tenant-id`
- `--client-id`
- `--property department|dn|department,dn`
- `--verbose`
- `--help`

### Environment Variables

| Variable | Purpose |
| --- | --- |
| `ENTRA_TENANT_ID` | Tenant ID GUID |
| `ENTRA_CLIENT_ID` | Client ID GUID |
| `ENTRA_PROPERTY` | `department`, `dn`, or `department,dn` |
| `ENTRA_VERBOSE` | `true` to enable stderr diagnostics |
| `ENTRA_HELP` | `true` to emit help text |

### Example `appsettings.json`

Use [appsettings.json.example](/Users/david/Documents/Identity/appsettings.json.example) as the template:

```json
{
  "Entra": {
    "TenantId": "00000000-0000-0000-0000-000000000000",
    "ClientId": "00000000-0000-0000-0000-000000000000",
    "Property": "department"
  }
}
```

`appsettings.json` is loaded from the executable directory, not the current working directory. That keeps wrapper behavior predictable when the process is launched by management tools.

## Build and Test

From the repository root:

```powershell
dotnet restore .\EntraProbe.sln
dotnet build .\EntraProbe.sln -c Release
dotnet test .\EntraProbe.sln -c Release
```

## Publish

### Framework-Dependent Publish

Windows x64:

```powershell
dotnet publish .\src\EntraProbe\EntraProbe.csproj `
  -c Release `
  -r win-x64 `
  --self-contained false `
  -o .\artifacts\publish\win-x64-framework
```

macOS arm64:

```bash
dotnet publish ./src/EntraProbe/EntraProbe.csproj \
  -c Release \
  -r osx-arm64 \
  --self-contained false \
  -o ./artifacts/publish/osx-arm64-framework
```

### Self-Contained Publish

Windows x64 self-contained directory publish:

```powershell
dotnet publish .\src\EntraProbe\EntraProbe.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  /p:PublishSingleFile=false `
  -o .\artifacts\publish\win-x64
```

macOS arm64 self-contained single-file publish:

```bash
dotnet publish ./src/EntraProbe/EntraProbe.csproj \
  -c Release \
  -r osx-arm64 \
  --self-contained true \
  /p:PublishSingleFile=true \
  -o ./artifacts/publish/osx-arm64
```

### Deployment Tradeoffs

- Framework-dependent: smaller package, requires a matching .NET runtime on the target device.
- Self-contained directory publish: larger package, but avoids the Windows single-file apphost bundle and its embedded `RT_RCDATA` resource.
- Self-contained single-file: simplest packaging on platforms where the bundled executable is acceptable.

For managed Windows platforms, the self-contained directory publish is the safer operational default when AV tooling is sensitive to packed executables.

## Running the Tool

Default `department` output:

```powershell
.\EntraProbe.exe --tenant-id "<tenant-guid>" --client-id "<client-guid>"
```

Return `dn` instead:

```powershell
.\EntraProbe.exe --tenant-id "<tenant-guid>" --client-id "<client-guid>" --property dn
```

Return both values as named lines:

```powershell
.\EntraProbe.exe --tenant-id "<tenant-guid>" --client-id "<client-guid>" --property department,dn
```

Verbose diagnostics go to stderr only:

```powershell
.\EntraProbe.exe --tenant-id "<tenant-guid>" --client-id "<client-guid>" --verbose
```

macOS example:

```bash
./EntraProbe --tenant-id "<tenant-guid>" --client-id "<client-guid>" --property department,dn
```

### PowerShell Consumption

Capture the default department:

```powershell
$department = & ".\EntraProbe.exe" --tenant-id "<tenant-guid>" --client-id "<client-guid>"
if ($LASTEXITCODE -eq 0) {
    $department
}
```

Capture both values as a keyed map:

```powershell
$result = & ".\EntraProbe.exe" --tenant-id "<tenant-guid>" --client-id "<client-guid>" --property department,dn
$exitCode = $LASTEXITCODE

if ($exitCode -eq 0 -or $exitCode -eq 10) {
    $values = $result | ConvertFrom-StringData
}

if ($exitCode -eq 0) {
    "$($values.department) | $($values.dn)"
}
```

When `--property dn` is used, stdout contains the full distinguished name exactly as returned by Graph, with no additional delimiters or quoting. When multiple properties are requested, each output line is self-describing, for example:

```text
department=Finance
dn=OU=Users,DC=contoso,DC=com
```

## Output Contract

| Scenario | stdout | stderr | Exit code |
| --- | --- | --- | --- |
| Success, default property | Department only | Empty unless `--verbose` is used | `0` |
| Success, `--property dn` | Distinguished name only | Empty unless `--verbose` is used | `0` |
| Success, `--property department,dn` | One `name=value` line per requested property, in request order | Empty unless `--verbose` is used | `0` |
| Single requested property missing | Empty | Optional concise verbose message | `10` |
| Multi-property request with one or more missing values | The output still contains one `name=value` line per requested property, with blank values for missing fields | Optional concise verbose message | `10` |
| Unsupported execution context | Empty | Concise error | `20` |
| Invalid configuration | Empty | Concise error | `30` |
| Authentication failure | Empty | Concise error | `40` |
| Graph failure | Empty | Concise error | `50` |
| Unexpected error | Empty | Concise error | `99` |

## Exit Code Reference

| Exit code | Meaning |
| --- | --- |
| `0` | Success |
| `10` | Requested property missing or empty |
| `20` | Unsupported execution context |
| `30` | Invalid or missing configuration |
| `40` | Authentication failure |
| `50` | Microsoft Graph failure |
| `99` | Unexpected error |

## Wrapper Scripts

Example wrappers are provided in [scripts](/Users/david/Documents/Identity/scripts):

- [EntraProbe-Local.ps1](/Users/david/Documents/Identity/scripts/EntraProbe-Local.ps1)
- [EntraProbe-Intune.ps1](/Users/david/Documents/Identity/scripts/EntraProbe-Intune.ps1)
- [EntraProbe-Tanium.ps1](/Users/david/Documents/Identity/scripts/EntraProbe-Tanium.ps1)

These wrappers:

- resolve the platform-specific `EntraProbe` binary relative to the script path instead of the current working directory
- preserve the executable's exit codes
- expose `department`, `dn`, and `department,dn` without editing script internals

## Intune Guidance

- Run `EntraProbe` in user context when you need delegated Microsoft Graph `/me`.
- Avoid `SYSTEM` context for detection or remediation if the goal is to read the signed-in user's attribute.
- Prefer the self-contained Windows x64 directory publish for packaging.
- Keep any tenant-specific branching logic in the PowerShell wrapper, not in the executable.

### Intune User-Context Deployment

For Intune PowerShell scripts, Microsoft documents the `Run this script using the logged on credentials` setting for user-context execution. In practice, use:

1. `Devices > Scripts and remediations > Platform scripts` for one-time or periodic script deployment, or `Devices > Scripts and remediations > Remediations` for detection/remediation pairs.
2. Upload a wrapper script that launches `EntraProbe.exe`.
3. Set `Run this script using the logged on credentials` to `Yes`.
4. Set `Run script in 64-bit PowerShell host` to `Yes` on 64-bit Windows endpoints.
5. Assign the script where a real user session is expected. `HKCU` writes and delegated `/me` lookups both require the signed-in user context.

Microsoft Learn references used:

- [Use PowerShell Scripts on Windows Devices in Intune](https://learn.microsoft.com/en-us/intune/intune-service/apps/powershell-scripts)
- [Use Remediations to detect and fix support issues](https://learn.microsoft.com/en-us/mem/analytics/proactive-remediations)

Example Intune remediation script that writes the default department to `HKCU`:

```powershell
$exe = Join-Path $PSScriptRoot 'EntraProbe.exe'
$department = & $exe --tenant-id '<tenant-guid>' --client-id '<client-guid>'
$exitCode = $LASTEXITCODE

if ($exitCode -ne 0) {
    Write-Error "EntraProbe failed with exit code $exitCode."
    exit $exitCode
}

$registryPath = 'HKCU:\Software\Contoso\EntraProbe'
New-Item -Path $registryPath -Force | Out-Null
Set-ItemProperty -Path $registryPath -Name 'Department' -Value $department -Type String
exit 0
```

Example Intune remediation script that writes both `department` and `dn` to `HKCU`:

```powershell
$exe = Join-Path $PSScriptRoot 'EntraProbe.exe'
$result = & $exe --tenant-id '<tenant-guid>' --client-id '<client-guid>' --property department,dn
$exitCode = $LASTEXITCODE

if ($exitCode -ne 0 -and $exitCode -ne 10) {
    Write-Error "EntraProbe failed with exit code $exitCode."
    exit $exitCode
}

$values = $result | ConvertFrom-StringData
$registryPath = 'HKCU:\Software\Contoso\EntraProbe'
New-Item -Path $registryPath -Force | Out-Null
Set-ItemProperty -Path $registryPath -Name 'Department' -Value ($values.department ?? '') -Type String
Set-ItemProperty -Path $registryPath -Name 'DistinguishedName' -Value ($values.dn ?? '') -Type String
exit $exitCode
```

## Tanium Guidance

- Run in the logged-in user's session when the deployment method allows it.
- Keep the executable's stdout contract intact and adapt Tanium-specific formatting in the wrapper.
- Treat exit codes as the primary contract for package success or failure handling.

### Tanium User-Context Deployment

Tanium execution terminology varies by module and version, but the operational requirement is the same: the action must run in the logged-in user's session, not as `SYSTEM`, if you expect delegated Microsoft Graph `/me` and `HKCU` writes to work.

Recommended pattern:

1. Package `EntraProbe.exe` and a PowerShell wrapper together.
2. Configure the Tanium action to run in the logged-in user's context, or use the package option that targets the interactive user session.
3. Do not rewrite the executable's stdout contract in the binary itself. Keep any registry writes or additional logging in the wrapper.
4. If multiple properties are requested, parse the returned `name=value` lines with `ConvertFrom-StringData`.

Example Tanium wrapper that writes `dn` to `HKCU`:

```powershell
$exe = Join-Path $PSScriptRoot 'EntraProbe.exe'
$dn = & $exe --tenant-id '<tenant-guid>' --client-id '<client-guid>' --property dn
$exitCode = $LASTEXITCODE

if ($exitCode -ne 0) {
    Write-Error "EntraProbe failed with exit code $exitCode."
    exit $exitCode
}

$registryPath = 'HKCU:\Software\Contoso\EntraProbe'
New-Item -Path $registryPath -Force | Out-Null
Set-ItemProperty -Path $registryPath -Name 'DistinguishedName' -Value $dn -Type String
exit 0
```

Example Tanium wrapper that writes both `department` and `dn` to `HKCU`:

```powershell
$exe = Join-Path $PSScriptRoot 'EntraProbe.exe'
$result = & $exe --tenant-id '<tenant-guid>' --client-id '<client-guid>' --property department,dn
$exitCode = $LASTEXITCODE

if ($exitCode -ne 0 -and $exitCode -ne 10) {
    Write-Error "EntraProbe failed with exit code $exitCode."
    exit $exitCode
}

$values = $result | ConvertFrom-StringData
$registryPath = 'HKCU:\Software\Contoso\EntraProbe'
New-Item -Path $registryPath -Force | Out-Null
Set-ItemProperty -Path $registryPath -Name 'Department' -Value ($values.department ?? '') -Type String
Set-ItemProperty -Path $registryPath -Name 'DistinguishedName' -Value ($values.dn ?? '') -Type String
exit $exitCode
```

## GitHub Actions

Two workflows are included:

- [ci.yml](/Users/david/Documents/Identity/.github/workflows/ci.yml): runs on `push` and `pull_request`, restores, builds, tests, performs a framework-dependent publish for Windows x64 and macOS arm64, stages the publish output with `README.md` and `appsettings.json.example`, and uploads one workflow artifact per platform.
- [release.yml](/Users/david/Documents/Identity/.github/workflows/release.yml): runs on semantic version tags such as `v1.0.0`, restores, builds, tests, performs a self-contained directory publish for Windows x64 and a self-contained single-file publish for macOS arm64, stages the package with `README.md` and `appsettings.json.example`, creates one zip per platform, ensures the GitHub Release exists for the tag, and uploads both zip assets.

### CI Artifacts

The `ci.yml` workflow uploads these workflow artifacts:

- `EntraProbe-ci-win-x64`
- `EntraProbe-ci-osx-arm64`

Each CI artifact contains:

- the framework-dependent publish output for that platform
- the sample wrapper scripts in `scripts/`
- `README.md`
- `appsettings.json.example`

### Release Assets

Push a semantic version tag:

```powershell
git tag v1.0.0
git push origin v1.0.0
```

The `release.yml` workflow first uploads these intermediate workflow artifacts:

- `release-win-x64`
- `release-osx-arm64`

It then attaches these final GitHub Release assets to the tag:

- `EntraProbe-win-x64-v1.0.0.zip`
- `EntraProbe-osx-arm64-v1.0.0.zip`

Each release asset contains:

- the self-contained publish output for that platform
- the sample wrapper scripts in `scripts/`
- `README.md`
- `appsettings.json.example`
- any runtime support files generated by `dotnet publish`

## Verification After Packaging

1. Download the GitHub Release asset for the target platform.
2. Extract the zip on the target platform.
3. Confirm the extracted directory contains the platform binary, `scripts/`, `README.md`, and `appsettings.json.example`.
4. Run the executable in a normal signed-in user session.
5. Verify stdout contains only the expected value and confirm the process exit code.

For CI workflow artifacts rather than release assets:

1. Open the workflow run in GitHub Actions.
2. Download `EntraProbe-ci-win-x64` or `EntraProbe-ci-osx-arm64`.
3. Extract the artifact.
4. Confirm the extracted contents include the platform publish output, `scripts/`, `README.md`, and `appsettings.json.example` before internal redistribution.

## Operations Handoff

For routine maintenance, deployment, and incident response guidance, use [OPERATIONS.md](/Users/david/Documents/Identity/OPERATIONS.md). That document is intended for day-two ownership by IT Operations and covers:

- deployment checklists
- support boundaries
- failure triage
- routine dependency and release hygiene
- where to change behavior safely in the repository

## Known Limitations

- Delegated Microsoft Graph `/me` lookup requires execution in the signed-in user's context.
- Windows `SYSTEM` execution is intentionally unsupported.
- macOS support is limited to broker-backed silent sign-in and requires Company Portal-managed sign-in state for the target user.
- Interactive auth fallback is intentionally disabled.
- `dn` output is empty for cloud-only users or unsynchronized identities where `onPremisesDistinguishedName` is not populated.

## Troubleshooting

- `This tool must run in an interactive user session`: launch it in the signed-in user's session, not as `SYSTEM`.
- `This tool is supported on Windows and macOS only.`: use the Windows or macOS build. Linux is intentionally unsupported.
- Windows AV flags on the release EXE: use the Windows self-contained directory publish or release asset. The project intentionally avoids Windows single-file bundling to reduce `RT_RCDATA`-style false positives.
- `Missing required tenant ID` or `Missing required client ID`: provide valid GUID values through CLI, environment variables, or `appsettings.json`.
- `Missing value for --tenant-id`, `--client-id`, or `--property`: correct the CLI invocation. The tool rejects incomplete switches instead of falling back to defaults.
- `Unknown argument: ...`: remove the unsupported switch or positional argument. The tool accepts named options only.
- `Authentication failed: no cached signed-in account is available`: the endpoint does not have reusable silent sign-in state for this app.
- `Authentication failed: multiple cached signed-in accounts are available...`: on macOS, reduce the cached broker account set or run in a user session with only the intended Entra account available to the broker.
- Persistent auth failures on macOS: confirm Company Portal enrollment, broker availability, and that the app registration includes `msauth.com.msauth.unsignedapp://auth`.
- `Authentication failed: network unavailable`: the device cannot reach Microsoft Entra endpoints.
- `Microsoft Graph query failed: network unavailable`: the device cannot reach Microsoft Graph.
- `Microsoft Graph returned an empty response`: Graph returned no body; treat this as an upstream service or network anomaly.
- Exit code `10` with no stdout: the requested property is not populated for that user.
- Exit code `10` with `--property department,dn`: one or more requested fields are missing. Any available fields are still emitted as `name=value` lines, and missing fields are emitted with empty values.
