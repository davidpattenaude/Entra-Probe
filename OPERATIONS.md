# Operations Runbook

This document is for day-two ownership of `EntraProbe` by IT Operations, endpoint engineering, or packaging teams.

## What the Tool Does

- Reads the signed-in user's Microsoft Entra profile through Microsoft Graph `/me`
- Returns `department` by default
- Optionally returns `onPremisesDistinguishedName` as `dn`
- Can return both values by requesting `department,dn`, which emits one `name=value` line per requested field
- Uses silent-only delegated authentication

## What the Tool Does Not Do

- It does not run as a background service.
- It does not support `SYSTEM` for delegated user lookup.
- It does not show prompts or browsers.
- It does not use client secrets, certificates, or app-only Graph access.

## Deployment Checklist

1. Confirm the Microsoft Entra app registration exists and is configured as a public client.
2. Confirm the app has delegated Microsoft Graph `User.Read`.
3. Confirm the binary is published for the target platform.
4. Confirm the deployment runs in the signed-in user's context when user attributes are required.
5. Confirm outbound access to Microsoft Entra ID and Microsoft Graph is allowed.
6. Confirm the device already has reusable silent sign-in state for the app.
7. For macOS, confirm Company Portal enrollment and broker-backed sign-in state for the target user session.

## Recommended Packaging Approach

- Windows enterprise deployment: publish self-contained `win-x64` with `PublishSingleFile=false`
- macOS enterprise deployment: publish self-contained `osx-arm64` and use `PublishSingleFile=true` if a single-file artifact is preferred
- Keep tenant-specific logic in the wrapper script, not in the executable
- Keep `appsettings.json` optional; prefer CLI arguments or environment variables in managed deployments

## Standard Validation Steps

After publishing or repackaging:

1. Run `EntraProbe --help` and confirm help text is emitted without crashing.
2. Run the tool with valid tenant and client IDs from a signed-in user session.
3. Confirm stdout contains only the requested value.
4. Confirm stderr is empty unless `--verbose` is enabled or the run fails.
5. Confirm the process exit code matches the expected path.
6. Confirm malformed CLI input fails clearly instead of silently defaulting to `department`.

## HKCU Usage Pattern

If the deployment needs to persist the returned value into the user's registry hive:

1. Run the wrapper in the signed-in user's context.
2. Capture stdout from `EntraProbe`.
3. Check `$LASTEXITCODE` before writing registry values.
4. Write to a vendor-controlled path such as `HKCU:\Software\Contoso\EntraProbe`.
5. For multi-property output, parse the `name=value` lines with `ConvertFrom-StringData`.

Example pattern:

```powershell
$result = & $exe --tenant-id '<tenant-guid>' --client-id '<client-guid>' --property department,dn
$exitCode = $LASTEXITCODE

if ($exitCode -eq 0 -or $exitCode -eq 10) {
    $values = $result | ConvertFrom-StringData
    $registryPath = 'HKCU:\Software\Contoso\EntraProbe'
    New-Item -Path $registryPath -Force | Out-Null
    Set-ItemProperty -Path $registryPath -Name 'Department' -Value ($values.department ?? '') -Type String
    Set-ItemProperty -Path $registryPath -Name 'DistinguishedName' -Value ($values.dn ?? '') -Type String
}

exit $exitCode
```

Do not attempt this from `SYSTEM` if the expectation is to write to the current user's `HKCU`.

## Common Failure Modes

| Symptom | Likely cause | Operational response |
| --- | --- | --- |
| Exit code `20` | Running as `SYSTEM` or outside a usable interactive session | Re-run in the signed-in user's context |
| Exit code `30` | Missing or invalid tenant/client ID | Correct CLI arguments, environment variables, or `appsettings.json` |
| Exit code `40` | No reusable silent sign-in state or auth path blocked | Verify app registration, user sign-in state, and tenant policy |
| Exit code `50` with network message | No network path to Graph or Entra | Check proxy, firewall, and endpoint connectivity |
| Exit code `10` | Requested attribute not populated | Confirm the user object contains `department` or `onPremisesDistinguishedName` |
| `Authentication failed: multiple cached signed-in accounts are available...` | Ambiguous macOS broker account cache | Reduce cached accounts or use a session with a single intended Entra account |

When `--property department,dn` is used, the tool emits one `name=value` line per requested field. That lets PowerShell consume the output with `ConvertFrom-StringData` without splitting the distinguished name string.

## Where to Change Behavior Safely

| Change | File or area |
| --- | --- |
| Add or change CLI/environment/appsettings options | `src/EntraProbe/Configuration/*` |
| Change output formatting or combined output behavior | `src/EntraProbe/App/ProfileOutputFormatter.cs` |
| Change exit-code mapping or orchestration | `src/EntraProbe/App/ApplicationRunner.cs` |
| Change platform execution rules | `src/EntraProbe/Execution/*` |
| Change authentication behavior | `src/EntraProbe/Identity/*` |
| Change Graph properties or request shape | `src/EntraProbe/Graph/*` |
| Change example deployment wrappers | `scripts/*` |
| Change CI or release packaging | `.github/workflows/*` |

## Routine Maintenance

Recommended recurring checks:

1. Run `dotnet test EntraProbe.sln -c Release`
2. Run `dotnet list EntraProbe.sln package --vulnerable --include-transitive`
3. Review MSAL and .NET package updates
4. Validate GitHub Actions release assets after the first tagged release following any workflow change
5. Reconfirm the Entra app registration settings after major tenant-policy changes

## Release Checklist

1. Restore, build, and test the solution
2. Publish the target RID
3. Verify the packaged output contains the binary, the `scripts/` wrappers, `README.md`, and `appsettings.json.example`
4. Smoke-test the binary in a normal user session
5. Push a semantic version tag if using GitHub Releases
6. Download the release asset and verify it matches the expected name and contents

## Support Boundaries

Escalate design changes rather than patching locally when the requested change would:

- require interactive sign-in prompts
- require `SYSTEM`-context delegated user lookup
- require token reuse from another application
- require additional Graph permissions beyond `User.Read`
- change the stdout contract in a way that could break existing wrappers

Those changes are architectural, not routine operations adjustments.
