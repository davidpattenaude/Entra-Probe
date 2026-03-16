<#
.SYNOPSIS
Runs EntraProbe from a Tanium-oriented PowerShell wrapper.

.DESCRIPTION
This example preserves EntraProbe's exit codes and passes stdout through
unchanged so Tanium can consume the executable's output contract directly. Use
`department`, `dn`, or `department,dn` for `-Property`.
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

if ($exitCode -eq 0) {
    Write-Output $result
    exit 0
}

Write-Error "EntraProbe failed. Exit code: $exitCode"
exit $exitCode
