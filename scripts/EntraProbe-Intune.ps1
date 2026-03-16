<#
.SYNOPSIS
Runs EntraProbe from an Intune-oriented PowerShell wrapper.

.DESCRIPTION
Use this script as a starting point for Intune remediation or detection
workflows. It preserves EntraProbe's exit codes so Intune can branch on them
reliably. Use `department`, `dn`, or `department,dn` for `-Property`.
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
    20 {
        Write-Error "EntraProbe must run in the signed-in user's session, not as SYSTEM."
        exit 20
    }
    30 {
        Write-Error "EntraProbe is missing a valid tenant ID or client ID."
        exit 30
    }
    40 {
        Write-Error "EntraProbe could not acquire a silent delegated token."
        exit 40
    }
    50 {
        Write-Error "EntraProbe could not read the signed-in user profile from Microsoft Graph."
        exit 50
    }
    default {
        Write-Error "Unexpected failure from EntraProbe. Exit code: $exitCode"
        exit $exitCode
    }
}
