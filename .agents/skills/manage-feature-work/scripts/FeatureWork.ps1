[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('CheckStart', 'CheckReservation', 'VerifyReservation')]
    [string]$Action,

    [string]$WorkFile
)

$ErrorActionPreference = 'Stop'

function Invoke-Git {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,
        [switch]$AllowFailure
    )

    $previousPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = & git @Arguments 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousPreference
    }

    if ($exitCode -ne 0 -and -not $AllowFailure) {
        throw "git $($Arguments -join ' ') failed:`n$($output -join [Environment]::NewLine)"
    }

    return [pscustomobject]@{
        ExitCode = $exitCode
        Output = @($output | ForEach-Object { "$_" })
    }
}

function Stop-Workflow {
    param(
        [Parameter(Mandatory = $true)][string]$Result,
        [Parameter(Mandatory = $true)][string]$Reason,
        [int]$Code = 20
    )

    Write-Output "RESULT=$Result"
    Write-Output "REASON=$Reason"
    exit $Code
}

function Get-RepoRoot {
    $result = Invoke-Git -Arguments @('rev-parse', '--show-toplevel') -AllowFailure
    if ($result.ExitCode -ne 0 -or $result.Output.Count -eq 0) {
        Stop-Workflow -Result 'BLOCKED' -Reason 'NOT_A_GIT_REPOSITORY'
    }
    return $result.Output[0]
}

function Get-Branch {
    $result = Invoke-Git -Arguments @('symbolic-ref', '--quiet', '--short', 'HEAD') -AllowFailure
    if ($result.ExitCode -ne 0 -or $result.Output.Count -eq 0) {
        Stop-Workflow -Result 'BLOCKED' -Reason 'DETACHED_HEAD'
    }
    return $result.Output[0]
}

function Get-Upstream {
    $result = Invoke-Git -Arguments @('rev-parse', '--abbrev-ref', '--symbolic-full-name', '@{u}') -AllowFailure
    if ($result.ExitCode -ne 0 -or $result.Output.Count -eq 0) {
        Stop-Workflow -Result 'BLOCKED' -Reason 'UPSTREAM_NOT_CONFIGURED'
    }
    return $result.Output[0]
}

function Invoke-Fetch {
    $result = Invoke-Git -Arguments @('fetch', '--prune') -AllowFailure
    if ($result.ExitCode -ne 0) {
        Stop-Workflow -Result 'BLOCKED' -Reason 'FETCH_FAILED'
    }
}

function Get-AheadBehind {
    $result = Invoke-Git -Arguments @('rev-list', '--left-right', '--count', 'HEAD...@{u}')
    $parts = $result.Output[0] -split '\s+'
    return [pscustomobject]@{ Ahead = [int]$parts[0]; Behind = [int]$parts[1] }
}

function Assert-Synchronized {
    $counts = Get-AheadBehind
    Write-Output "AHEAD=$($counts.Ahead)"
    Write-Output "BEHIND=$($counts.Behind)"

    if ($counts.Ahead -gt 0 -and $counts.Behind -gt 0) {
        Stop-Workflow -Result 'BLOCKED' -Reason 'BRANCH_DIVERGED'
    }
    if ($counts.Behind -gt 0) {
        Stop-Workflow -Result 'PULL_REQUIRED' -Reason 'UPSTREAM_HAS_NEW_COMMITS'
    }
    if ($counts.Ahead -gt 0) {
        Stop-Workflow -Result 'PUSH_REQUIRED' -Reason 'LOCAL_COMMITS_NOT_PUSHED'
    }
}

function Get-ChangedPaths {
    $paths = New-Object System.Collections.Generic.List[string]
    foreach ($arguments in @(
        @('diff', '--name-only'),
        @('diff', '--cached', '--name-only'),
        @('ls-files', '--others', '--exclude-standard')
    )) {
        $result = Invoke-Git -Arguments $arguments
        foreach ($path in $result.Output) {
            $normalized = $path -replace '\\', '/'
            if ($normalized -and -not $paths.Contains($normalized)) {
                $paths.Add($normalized)
            }
        }
    }
    return @($paths)
}

function Assert-Clean {
    $paths = @(Get-ChangedPaths)
    if ($paths.Count -gt 0) {
        foreach ($path in $paths) { Write-Output "LOCAL_CHANGE=$path" }
        Stop-Workflow -Result 'BLOCKED' -Reason 'WORKTREE_NOT_CLEAN'
    }
}

function Resolve-WorkFile {
    param([Parameter(Mandatory = $true)][string]$Path)

    $repoRoot = [System.IO.Path]::GetFullPath((Get-RepoRoot))
    $fullPath = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $Path))
    $activeRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot 'Docs\Work\Active'))
    if (-not $fullPath.StartsWith($activeRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
        Stop-Workflow -Result 'BLOCKED' -Reason 'WORK_FILE_OUTSIDE_ACTIVE_DIRECTORY'
    }
    if ([System.IO.Path]::GetExtension($fullPath) -ne '.md' -or [System.IO.Path]::GetFileName($fullPath) -eq 'README.md') {
        Stop-Workflow -Result 'BLOCKED' -Reason 'INVALID_WORK_FILE'
    }

    $relative = $fullPath.Substring($repoRoot.Length).TrimStart('\') -replace '\\', '/'
    return [pscustomobject]@{ FullPath = $fullPath; RelativePath = $relative }
}

function Assert-OnlyWorkFileChanged {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    $paths = @(Get-ChangedPaths)
    if ($paths.Count -ne 1 -or $paths[0] -ne $RelativePath) {
        foreach ($path in $paths) { Write-Output "LOCAL_CHANGE=$path" }
        Stop-Workflow -Result 'BLOCKED' -Reason 'ACTIVE_DOCUMENT_MUST_BE_ONLY_CHANGE'
    }
}

function Get-ReservationData {
    param([Parameter(Mandatory = $true)][string]$Content)

    if ($Content -notmatch '(?m)^Status:\s*Reserved\s*$') {
        Stop-Workflow -Result 'BLOCKED' -Reason 'RESERVATION_STATUS_INVALID'
    }
    if ($Content -notmatch '(?m)^- Base Commit:\s*([0-9a-fA-F]{7,40})\s*$') {
        Stop-Workflow -Result 'BLOCKED' -Reason 'BASE_COMMIT_MISSING'
    }
    return [pscustomobject]@{ BaseCommit = $Matches[1] }
}

function Write-RemoteActiveFiles {
    $result = Invoke-Git -Arguments @('ls-tree', '-r', '--name-only', '@{u}', '--', 'Docs/Work/Active')
    foreach ($path in $result.Output) {
        if ($path -and $path -ne 'Docs/Work/Active/README.md') {
            Write-Output "ACTIVE_FILE=$path"
        }
    }
}

function Write-ChangesSince {
    param([Parameter(Mandatory = $true)][string]$BaseCommit)

    $validCommit = Invoke-Git -Arguments @('cat-file', '-e', "$BaseCommit`^{commit}") -AllowFailure
    if ($validCommit.ExitCode -ne 0) {
        Stop-Workflow -Result 'BLOCKED' -Reason 'BASE_COMMIT_NOT_FOUND'
    }
    $ancestor = Invoke-Git -Arguments @('merge-base', '--is-ancestor', $BaseCommit, 'HEAD') -AllowFailure
    if ($ancestor.ExitCode -ne 0) {
        Stop-Workflow -Result 'BLOCKED' -Reason 'BASE_COMMIT_NOT_ANCESTOR'
    }
    $result = Invoke-Git -Arguments @('diff', '--name-only', $BaseCommit, 'HEAD', '--')
    foreach ($path in $result.Output) {
        if ($path) { Write-Output "CHANGED_FILE=$path" }
    }
}

$repoRoot = Get-RepoRoot
Set-Location $repoRoot
$branch = Get-Branch
$upstream = Get-Upstream

Write-Output "ACTION=$Action"
Write-Output "BRANCH=$branch"
Write-Output "UPSTREAM=$upstream"

switch ($Action) {
    'CheckStart' {
        Assert-Clean
        Invoke-Fetch
        Assert-Synchronized
        Write-RemoteActiveFiles
        Write-Output "BASE_COMMIT=$((Invoke-Git -Arguments @('rev-parse', 'HEAD')).Output[0])"
        Write-Output 'RESULT=READY_TO_CHECK_CONFLICTS'
    }

    'CheckReservation' {
        if (-not $WorkFile) { Stop-Workflow -Result 'BLOCKED' -Reason 'WORK_FILE_REQUIRED' }
        $work = Resolve-WorkFile -Path $WorkFile
        if (-not (Test-Path -LiteralPath $work.FullPath -PathType Leaf)) {
            Stop-Workflow -Result 'BLOCKED' -Reason 'WORK_FILE_NOT_FOUND'
        }
        $content = Get-Content -LiteralPath $work.FullPath -Raw -Encoding UTF8
        $reservation = Get-ReservationData -Content $content
        Assert-OnlyWorkFileChanged -RelativePath $work.RelativePath
        Invoke-Fetch
        Assert-Synchronized
        Write-RemoteActiveFiles
        Write-ChangesSince -BaseCommit $reservation.BaseCommit
        Write-Output 'RESULT=RESERVATION_READY_TO_PUSH'
    }

    'VerifyReservation' {
        if (-not $WorkFile) { Stop-Workflow -Result 'BLOCKED' -Reason 'WORK_FILE_REQUIRED' }
        $work = Resolve-WorkFile -Path $WorkFile
        $paths = @(Get-ChangedPaths)
        if ($paths -contains $work.RelativePath) {
            Stop-Workflow -Result 'PUSH_REQUIRED' -Reason 'RESERVATION_HAS_LOCAL_CHANGES'
        }
        Assert-Clean
        Invoke-Fetch
        Assert-Synchronized
        $remoteFile = Invoke-Git -Arguments @('cat-file', '-e', "@{u}:$($work.RelativePath)") -AllowFailure
        if ($remoteFile.ExitCode -ne 0) {
            Stop-Workflow -Result 'PUSH_REQUIRED' -Reason 'RESERVATION_NOT_FOUND_ON_UPSTREAM'
        }
        $remoteContent = (Invoke-Git -Arguments @('show', "@{u}:$($work.RelativePath)")).Output -join "`n"
        $reservation = Get-ReservationData -Content $remoteContent
        Write-RemoteActiveFiles
        Write-ChangesSince -BaseCommit $reservation.BaseCommit
        Write-Output "IMPLEMENTATION_BASE=$((Invoke-Git -Arguments @('rev-parse', 'HEAD')).Output[0])"
        Write-Output 'RESULT=READY_TO_IMPLEMENT'
    }
}
