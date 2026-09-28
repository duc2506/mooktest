[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Email,
    [string]$DisplayName = 'Trainer',
    [System.Security.SecureString]$Password,
    [switch]$SkipMigrations
)
$ErrorActionPreference = 'Stop'
$taskProject = Join-Path $PSScriptRoot '../backend/MookTest/MookTest.csproj'
$Email = $Email.Trim()
$DisplayName = $DisplayName.Trim()
try { $taskAddress = [System.Net.Mail.MailAddress]::new($Email) }
catch { throw 'Invalid email address. Example: trainer@example.com.' }
if ($taskAddress.Address -ne $Email -or $Email.Length -gt 254) {
    throw 'Enter an email address only, without a display name (maximum 254 characters).'
}
if ($DisplayName.Length -lt 2 -or $DisplayName.Length -gt 100) {
    throw 'DisplayName must contain 2-100 characters.'
}
if ($null -ne $Password -and ($Password.Length -lt 10 -or $Password.Length -gt 128)) {
    throw 'Password must contain 10-128 characters.'
}
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw '.NET SDK 10 is required. Install it, then reopen this terminal.'
}

# Build separately so build/restore errors cannot be mistaken for account errors.
Write-Host 'Step 1: Building backend...'
dotnet build $taskProject --nologo
if ($LASTEXITCODE -ne 0) {
    throw 'Backend build failed. Fix the build/NuGet error above before creating a trainer. If MookTest.exe is locked, stop the running backend first.'
}

$taskOwnsPassword = $null -eq $Password
$taskPassword = $Password
$taskPreviousEnvironment = @{}
foreach ($taskName in @('Trainer__Email', 'Trainer__DisplayName', 'Trainer__Password')) {
    $taskPreviousEnvironment[$taskName] = [Environment]::GetEnvironmentVariable($taskName, 'Process')
}
try {
    if ($taskOwnsPassword) {
        do {
            if ($null -ne $taskPassword) { $taskPassword.Dispose() }
            $taskPassword = Read-Host 'Trainer password (10-128 characters)' -AsSecureString
            $taskValidLength = $taskPassword.Length -ge 10 -and $taskPassword.Length -le 128
            if (-not $taskValidLength) { Write-Warning 'Password must contain 10-128 characters. Please try again.' }
        } until ($taskValidLength)
    }
    $env:Trainer__Email = $Email
    $env:Trainer__DisplayName = $DisplayName
    $env:Trainer__Password = [pscredential]::new($Email, $taskPassword).GetNetworkCredential().Password
    $taskArguments = @('run', '--project', $taskProject, '--no-build', '--launch-profile', 'http', '--')
    if (-not $SkipMigrations) {
        Write-Host 'Step 2: Applying pending migrations and creating trainer...'
        $taskArguments += '--migrate'
    } else { Write-Host 'Step 2: Creating trainer using the existing database schema...' }
    $taskArguments += '--create-trainer'
    & dotnet @taskArguments
    $taskExitCode = $LASTEXITCODE
    if ($taskExitCode -ne 0) {
        throw "Trainer was not created (exit code $taskExitCode). The ERROR message immediately above explains the cause."
    }
} finally {
    foreach ($taskName in $taskPreviousEnvironment.Keys) {
        [Environment]::SetEnvironmentVariable($taskName, $taskPreviousEnvironment[$taskName], 'Process')
    }
    if ($taskOwnsPassword -and $null -ne $taskPassword) { $taskPassword.Dispose() }
}
