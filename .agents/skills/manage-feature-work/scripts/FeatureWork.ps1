[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Preflight', 'RefreshReservation', 'PublishReservation', 'VerifyImplementation')]
    [string]$Action,

    [string]$WorkFile,

    [string]$CommitMessage
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

function Write-Result {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Result,
        [string]$Reason
    )

    Write-Output "RESULT=$Result"
    if ($Reason) {
        Write-Output "REASON=$Reason"
    }
}

function Stop-Workflow {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Reason,
        [int]$Code = 20
    )

    Write-Result -Result 'BLOCKED' -Reason $Reason
    exit $Code
}

function Get-RepoRoot {
    $result = Invoke-Git -Arguments @('rev-parse', '--show-toplevel') -AllowFailure
    if ($result.ExitCode -ne 0 -or $result.Output.Count -eq 0) {
        Stop-Workflow -Reason 'NOT_A_GIT_REPOSITORY'
    }

    return ($result.Output[0] -replace '\\', '/')
}

function Get-Upstream {
    $result = Invoke-Git -Arguments @('rev-parse', '--abbrev-ref', '--symbolic-full-name', '@{u}') -AllowFailure
    if ($result.ExitCode -ne 0 -or $result.Output.Count -eq 0) {
        Stop-Workflow -Reason 'UPSTREAM_NOT_CONFIGURED'
    }

    return $result.Output[0]
}

function Get-Branch {
    $result = Invoke-Git -Arguments @('symbolic-ref', '--quiet', '--short', 'HEAD') -AllowFailure
    if ($result.ExitCode -ne 0 -or $result.Output.Count -eq 0) {
        Stop-Workflow -Reason 'DETACHED_HEAD'
    }

    return $result.Output[0]
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
            if ($path -and -not $paths.Contains($path)) {
                $paths.Add(($path -replace '\\', '/'))
            }
        }
    }

    return @($paths)
}

function Assert-Clean {
    $paths = @(Get-ChangedPaths)
    if ($paths.Count -gt 0) {
        foreach ($path in $paths) {
            Write-Output "LOCAL_CHANGE=$path"
        }
        Stop-Workflow -Reason 'WORKTREE_NOT_CLEAN'
    }
}

function Resolve-WorkFile {
    param([Parameter(Mandatory = $true)][string]$Path)

    $repoRoot = Get-RepoRoot
    $fullPath = [System.IO.Path]::GetFullPath((Join-Path ($repoRoot -replace '/', '\') $Path))
    $activeRoot = [System.IO.Path]::GetFullPath((Join-Path ($repoRoot -replace '/', '\') 'Docs\Work\Active'))
    $relative = $fullPath.Substring(($repoRoot -replace '/', '\').Length).TrimStart('\') -replace '\\', '/'

    if (-not $fullPath.StartsWith($activeRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
        Stop-Workflow -Reason 'WORK_FILE_MUST_BE_UNDER_DOCS_WORK_ACTIVE'
    }
    if ([System.IO.Path]::GetExtension($fullPath) -ne '.md' -or [System.IO.Path]::GetFileName($fullPath) -eq 'README.md') {
        Stop-Workflow -Reason 'INVALID_WORK_FILE'
    }

    return [pscustomobject]@{ FullPath = $fullPath; RelativePath = $relative }
}

function Assert-OnlyWorkFileChanged {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    $paths = @(Get-ChangedPaths)
    if ($paths.Count -ne 1 -or $paths[0] -ne $RelativePath) {
        foreach ($path in $paths) {
            Write-Output "LOCAL_CHANGE=$path"
        }
        Stop-Workflow -Reason 'RESERVATION_MUST_BE_THE_ONLY_LOCAL_CHANGE'
    }
}

function Get-AheadBehind {
    $result = Invoke-Git -Arguments @('rev-list', '--left-right', '--count', 'HEAD...@{u}')
    $parts = $result.Output[0] -split '\s+'
    return [pscustomobject]@{ Ahead = [int]$parts[0]; Behind = [int]$parts[1] }
}

function Invoke-Fetch {
    $result = Invoke-Git -Arguments @('fetch', '--prune') -AllowFailure
    if ($result.ExitCode -ne 0) {
        Stop-Workflow -Reason 'FETCH_FAILED'
    }
}

function Write-RemoteActiveFiles {
    $result = Invoke-Git -Arguments @('ls-tree', '-r', '--name-only', '@{u}', '--', 'Docs/Work/Active')
    foreach ($path in $result.Output) {
        if ($path -and $path -ne 'Docs/Work/Active/README.md') {
            Write-Output "ACTIVE_FILE=$path"
        }
    }
}

function Write-ChangedFiles {
    param(
        [Parameter(Mandatory = $true)][string]$Before,
        [Parameter(Mandatory = $true)][string]$After
    )

    if ($Before -eq $After) {
        return
    }

    $result = Invoke-Git -Arguments @('diff', '--name-only', $Before, $After, '--')
    foreach ($path in $result.Output) {
        if ($path) {
            Write-Output "CHANGED_FILE=$path"
        }
    }
}

function Sync-FastForward {
    $before = (Invoke-Git -Arguments @('rev-parse', 'HEAD')).Output[0]
    Invoke-Fetch
    $counts = Get-AheadBehind

    if ($counts.Ahead -gt 0 -and $counts.Behind -gt 0) {
        Stop-Workflow -Reason 'BRANCH_DIVERGED'
    }
    if ($counts.Ahead -gt 0) {
        Stop-Workflow -Reason 'LOCAL_COMMITS_NOT_PUBLISHED'
    }
    if ($counts.Behind -gt 0) {
        $pull = Invoke-Git -Arguments @('pull', '--ff-only') -AllowFailure
        if ($pull.ExitCode -ne 0) {
            Stop-Workflow -Reason 'FAST_FORWARD_PULL_FAILED'
        }
    }

    $after = (Invoke-Git -Arguments @('rev-parse', 'HEAD')).Output[0]
    return [pscustomobject]@{ Before = $before; After = $after }
}

$repoRoot = Get-RepoRoot
Set-Location ($repoRoot -replace '/', '\')
$branch = Get-Branch
$upstream = Get-Upstream

Write-Output "ACTION=$Action"
Write-Output "BRANCH=$branch"
Write-Output "UPSTREAM=$upstream"

switch ($Action) {
    'Preflight' {
        Assert-Clean
        $sync = Sync-FastForward
        Assert-Clean
        Write-ChangedFiles -Before $sync.Before -After $sync.After
        Write-RemoteActiveFiles
        Write-Output "BASE_COMMIT=$($sync.After)"
        Write-Result -Result 'READY'
    }

    'RefreshReservation' {
        if (-not $WorkFile) { Stop-Workflow -Reason 'WORK_FILE_REQUIRED' }
        $work = Resolve-WorkFile -Path $WorkFile
        if (-not (Test-Path -LiteralPath $work.FullPath -PathType Leaf)) {
            Stop-Workflow -Reason 'WORK_FILE_NOT_FOUND'
        }
        Assert-OnlyWorkFileChanged -RelativePath $work.RelativePath
        $sync = Sync-FastForward
        Assert-OnlyWorkFileChanged -RelativePath $work.RelativePath
        Write-ChangedFiles -Before $sync.Before -After $sync.After
        Write-RemoteActiveFiles
        Write-Output "BASE_COMMIT=$($sync.After)"
        Write-Result -Result 'AWAITING_USER_APPROVAL'
    }

    'PublishReservation' {
        if (-not $WorkFile) { Stop-Workflow -Reason 'WORK_FILE_REQUIRED' }
        $work = Resolve-WorkFile -Path $WorkFile
        if (-not (Test-Path -LiteralPath $work.FullPath -PathType Leaf)) {
            Stop-Workflow -Reason 'WORK_FILE_NOT_FOUND'
        }
        $content = Get-Content -LiteralPath $work.FullPath -Raw -Encoding UTF8
        if ($content -notmatch '(?m)^Status:\s*Reserved\s*$' -or $content -notmatch '(?m)^- User Approval:\s*(?!Pending).+$') {
            Stop-Workflow -Reason 'USER_APPROVAL_NOT_RECORDED'
        }
        Assert-OnlyWorkFileChanged -RelativePath $work.RelativePath
        Invoke-Fetch
        $counts = Get-AheadBehind
        if ($counts.Ahead -ne 0 -or $counts.Behind -ne 0) {
            Stop-Workflow -Reason 'REMOTE_CHANGED_AFTER_APPROVAL'
        }
        Invoke-Git -Arguments @('add', '--', $work.RelativePath) | Out-Null
        $staged = @((Invoke-Git -Arguments @('diff', '--cached', '--name-only')).Output)
        if ($staged.Count -ne 1 -or $staged[0] -ne $work.RelativePath) {
            Stop-Workflow -Reason 'STAGED_SCOPE_MISMATCH'
        }
        if (-not $CommitMessage) {
            $shortName = [System.IO.Path]::GetFileNameWithoutExtension($work.FullPath)
            $CommitMessage = "chore(work): reserve $shortName"
        }
        $commit = Invoke-Git -Arguments @('commit', '-m', $CommitMessage) -AllowFailure
        if ($commit.ExitCode -ne 0) {
            Stop-Workflow -Reason 'RESERVATION_COMMIT_FAILED'
        }
        $push = Invoke-Git -Arguments @('push') -AllowFailure
        if ($push.ExitCode -ne 0) {
            Stop-Workflow -Reason 'RESERVATION_PUSH_FAILED'
        }
        Invoke-Fetch
        $counts = Get-AheadBehind
        if ($counts.Ahead -ne 0 -or $counts.Behind -ne 0) {
            Stop-Workflow -Reason 'RESERVATION_PUSH_NOT_SYNCHRONIZED'
        }
        $remoteFile = Invoke-Git -Arguments @('cat-file', '-e', "@{u}:$($work.RelativePath)") -AllowFailure
        if ($remoteFile.ExitCode -ne 0) {
            Stop-Workflow -Reason 'RESERVATION_NOT_FOUND_ON_REMOTE'
        }
        Write-Output "RESERVATION_COMMIT=$((Invoke-Git -Arguments @('rev-parse', 'HEAD')).Output[0])"
        Write-Result -Result 'PUBLISHED'
    }

    'VerifyImplementation' {
        if (-not $WorkFile) { Stop-Workflow -Reason 'WORK_FILE_REQUIRED' }
        $work = Resolve-WorkFile -Path $WorkFile
        Assert-Clean
        $sync = Sync-FastForward
        Assert-Clean
        Write-ChangedFiles -Before $sync.Before -After $sync.After
        $remoteFile = Invoke-Git -Arguments @('cat-file', '-e', "@{u}:$($work.RelativePath)") -AllowFailure
        if ($remoteFile.ExitCode -ne 0) {
            Stop-Workflow -Reason 'RESERVATION_NOT_FOUND_ON_REMOTE'
        }
        $remoteContent = (Invoke-Git -Arguments @('show', "@{u}:$($work.RelativePath)")).Output -join "`n"
        if ($remoteContent -notmatch '(?m)^Status:\s*Reserved\s*$') {
            Stop-Workflow -Reason 'REMOTE_RESERVATION_NOT_RESERVED'
        }
        Write-RemoteActiveFiles
        Write-Output "IMPLEMENTATION_BASE=$($sync.After)"
        Write-Result -Result 'READY_TO_IMPLEMENT'
    }
}
