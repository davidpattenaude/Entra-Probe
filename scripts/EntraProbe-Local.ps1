<#
.SYNOPSIS
Runs EntraProbe locally and returns the requested value to the pipeline.

.DESCRIPTION
This wrapper is intended for ad hoc use from a PowerShell session. It keeps
stdout clean on success and surfaces failures through PowerShell errors and
process exit codes. Use `department`, `dn`, or `department,dn` for `-Property`.
Multi-property output is returned as one `name=value` line per field.
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$TenantId,

    [Parameter(Mandatory = $true)]
    [string]$ClientId,

    [string]$Property = 'department',

    [string]$ExecutablePath = (Join-Path $PSScriptRoot $(if ($env:OS -eq 'Windows_NT') { 'EntraProbe.exe' } else { 'EntraProbe' }))
)

$arguments = @(
    '--tenant-id', $TenantId,
    '--client-id', $ClientId,
    '--property', $Property
)

$result = & $ExecutablePath @arguments
$exitCode = $LASTEXITCODE

switch ($exitCode) {
    0 {
        Write-Output $result
        exit 0
    }
    10 {
        Write-Error "The requested Entra attribute was not present for the signed-in user."
        exit 10
    }
    default {
        Write-Error "EntraProbe failed with exit code $exitCode."
        exit $exitCode
    }
}
