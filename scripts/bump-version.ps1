#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Bump version across all Lintelligent projects
.DESCRIPTION
    Updates version in all csproj files and creates a git commit
.PARAMETER Version
    The new version to set (e.g., "1.3.0")
.PARAMETER Prerelease
    Optional prerelease suffix (e.g., "beta", "rc.1")
.EXAMPLE
    ./scripts/bump-version.ps1 -Version 1.3.0
.EXAMPLE
    ./scripts/bump-version.ps1 -Version 1.3.0 -Prerelease beta
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$Version,
    
    [Parameter(Mandatory=$false)]
    [string]$Prerelease = ""
)

$ErrorActionPreference = "Stop"

# Construct full version
$fullVersion = if ($Prerelease) { "$Version-$Prerelease" } else { $Version }

Write-Host "🔄 Bumping version to $fullVersion" -ForegroundColor Cyan

# Projects to update
$projects = @(
    "src/Lintelligent.Analyzers/Lintelligent.Analyzers.csproj"
)

foreach ($project in $projects) {
    $path = Join-Path $PSScriptRoot ".." $project
    
    if (!(Test-Path $path)) {
        Write-Warning "⚠️  Project not found: $project"
        continue
    }
    
    Write-Host "  📝 Updating $project" -ForegroundColor Gray
    
    # Read content
    $content = Get-Content $path -Raw
    
    # Update <Version> tag
    $content = $content -replace '<Version>.*?</Version>', "<Version>$fullVersion</Version>"
    
    # Write back
    Set-Content $path $content -NoNewline
}

Write-Host "✅ Version updated in all projects" -ForegroundColor Green

# Update CHANGELOG.md
$changelogPath = Join-Path $PSScriptRoot ".." "CHANGELOG.md"
if (Test-Path $changelogPath) {
    Write-Host "  📝 Updating CHANGELOG.md" -ForegroundColor Gray
    
    $date = Get-Date -Format "yyyy-MM-dd"
    $changelog = Get-Content $changelogPath -Raw
    
    # Add new version header after ## [Unreleased]
    $newEntry = @"

## [$fullVersion] - $date

### Added
- 

### Changed
- 

### Fixed
- 

"@
    
    $changelog = $changelog -replace '(## \[Unreleased\])', "`$1$newEntry"
    Set-Content $changelogPath $changelog -NoNewline
    
    Write-Host "  ℹ️  Please edit CHANGELOG.md to add release notes" -ForegroundColor Yellow
}

# Commit changes
Write-Host ""
Write-Host "📋 Git status:" -ForegroundColor Cyan
git status --short

Write-Host ""
$commit = Read-Host "Commit these changes? (y/N)"

if ($commit -eq 'y' -or $commit -eq 'Y') {
    git add $projects
    if (Test-Path $changelogPath) {
        git add $changelogPath
    }
    git commit -m "chore(analyzers): bump version to $fullVersion"
    
    Write-Host "✅ Changes committed" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "  1. Edit src/Lintelligent.Analyzers/CHANGELOG.md to add release notes"
    Write-Host "  2. git add src/Lintelligent.Analyzers/CHANGELOG.md && git commit --amend --no-edit"
    Write-Host "  3. git push origin main"
    Write-Host "  4. git tag analyzers/v$fullVersion && git push origin analyzers/v$fullVersion"
    Write-Host "  5. Workflow will auto-publish to NuGet"
} else {
    Write-Host "⏭️  Skipping commit" -ForegroundColor Yellow
}
