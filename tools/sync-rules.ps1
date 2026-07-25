<#
.SYNOPSIS
    Generates the Cursor rules snapshot in the extension template from guides/*.md.

.DESCRIPTION
    The guides in this repo are the single source of truth: the get_extension_guide MCP tool serves
    them from embedded resources, and this script writes them into the template repo as .mdc rules so
    a freshly created extension repo carries them without the MCP server.

    Run it after editing anything under guides/, then commit the template repo.

.EXAMPLE
    powershell -NoProfile -File tools\sync-rules.ps1
    powershell -NoProfile -File tools\sync-rules.ps1 -Check
#>
[CmdletBinding()]
param(
    # Template repo checkout. Default: sibling directory of this repo.
    [string]$TemplateDir,
    # Compare only: exit 1 when the snapshot drifted from guides/.
    [switch]$Check
)

$ErrorActionPreference = 'Stop'

# $PSScriptRoot is not populated while parameter defaults are bound on PS 5.1, so resolve here.
$repoDir = Split-Path $PSScriptRoot -Parent
if ([string]::IsNullOrWhiteSpace($TemplateDir)) {
    $TemplateDir = Join-Path (Split-Path $repoDir -Parent) 'ency-extension-template'
}

$guidesDir = Join-Path $repoDir 'guides'
$rulesDir = Join-Path $TemplateDir '.cursor\rules'
if (-not (Test-Path $rulesDir)) { throw "rules dir not found: $rulesDir" }

$index = Get-Content (Join-Path $guidesDir '_index.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$drift = @()

# Cursor reads the frontmatter from byte 0, so the rules must be UTF-8 WITHOUT a BOM.
# Set-Content -Encoding utf8 on PS 5.1 writes one, hence the explicit encoder.
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

foreach ($g in $index.guides) {
    # -Encoding UTF8 matters: without it PS 5.1 reads the file as ANSI and mangles every dash.
    $body = Get-Content (Join-Path $guidesDir $g.file) -Raw -Encoding UTF8
    # Drop the guide's own frontmatter - the .mdc gets Cursor's one instead.
    $body = [regex]::Replace($body, '^---\r?\n.*?\r?\n---\r?\n', '', 'Singleline')

    $header = @"
---
description: $($g.description)
alwaysApply: false
---

<!-- Generated from guides/$($g.file) in ENCY-SOFTWARE-LTD/ency-extension-mcp.
     Edit it there and run tools/sync-rules.ps1 - changes made here are overwritten. -->

"@
    $text = $header + $body
    $target = Join-Path $rulesDir $g.cursorRule

    if ($Check) {
        if (-not (Test-Path $target)) {
            $drift += "$($g.cursorRule) (missing)"
        }
        elseif ([System.IO.File]::ReadAllText($target, $utf8NoBom) -ne $text) {
            $drift += $g.cursorRule
        }
    }
    else {
        [System.IO.File]::WriteAllText($target, $text, $utf8NoBom)
        Write-Output "wrote $($g.cursorRule)"
    }
}

if ($Check) {
    if ($drift.Count -gt 0) {
        Write-Output ("drift: " + ($drift -join ', '))
        exit 1
    }
    Write-Output 'rules are in sync'
}
else {
    Write-Output "$($index.guides.Count) rules written to $rulesDir"
}
