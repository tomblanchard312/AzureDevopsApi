param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$controllersPath = Join-Path $RepoRoot "ADOApi/Controllers"
$programPath = Join-Path $RepoRoot "ADOApi/Program.cs"
$outputPath = Join-Path $RepoRoot "API_CATALOG.md"

if (-not (Test-Path $controllersPath)) {
    throw "Controllers path not found: $controllersPath"
}

function Get-PolicyFromAuthorizeAttribute {
    param([string]$AuthorizeLine)

    if ($AuthorizeLine -match 'Policy\s*=\s*"([^"]+)"') {
        return $Matches[1]
    }

    return $null
}

function Get-LikelyPolicyText {
    param(
        [string]$MethodAuthorizeLine,
        [string]$ClassAuthorizeLine
    )

    $methodPolicy = $null
    $classPolicy = $null

    if ($MethodAuthorizeLine) {
        $methodPolicy = Get-PolicyFromAuthorizeAttribute -AuthorizeLine $MethodAuthorizeLine
        if ($methodPolicy) {
            return "$methodPolicy (method-level)"
        }

        return "Authenticated user (method-level [Authorize])"
    }

    if ($ClassAuthorizeLine) {
        $classPolicy = Get-PolicyFromAuthorizeAttribute -AuthorizeLine $ClassAuthorizeLine
        if ($classPolicy) {
            return "$classPolicy (class-level)"
        }

        return "Authenticated user (class-level [Authorize])"
    }

    return "Authenticated user (prod fallback)"
}

function Resolve-RouteTemplate {
    param(
        [string]$ClassRoute,
        [string]$MethodRoute,
        [string]$ControllerName
    )

    $resolvedClassRoute = $ClassRoute -replace '\[controller\]', $ControllerName

    if ([string]::IsNullOrWhiteSpace($MethodRoute)) {
        return "/" + $resolvedClassRoute.Trim("/")
    }

    if ($MethodRoute.StartsWith("/")) {
        return $MethodRoute
    }

    return "/" + ($resolvedClassRoute.Trim("/") + "/" + $MethodRoute.Trim("/"))
}

function Normalize-ControllerNameFromFile {
    param([string]$FileName)
    return [System.IO.Path]::GetFileNameWithoutExtension($FileName) -replace 'Controller$', ''
}

function Escape-Pipe {
    param([string]$Value)
    if ($null -eq $Value) { return "" }
    return $Value -replace '\|', '\|'
}

$controllers = Get-ChildItem -Path $controllersPath -Filter "*.cs" | Sort-Object Name
$endpoints = New-Object System.Collections.Generic.List[object]

foreach ($controllerFile in $controllers) {
    $controllerName = Normalize-ControllerNameFromFile -FileName $controllerFile.Name
    $lines = Get-Content -Path $controllerFile.FullName

    $classRoute = $null
    $classAuthorizeLine = $null

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i].Trim()
        if (-not $classRoute -and $line -match '^\[Route\("([^"]+)"\)\]$') {
            $classRoute = $Matches[1]
        }
        if (-not $classAuthorizeLine -and $line -match '^\[Authorize(?:\(.+\))?\]$') {
            $classAuthorizeLine = $line
        }
        if ($line -match 'class\s+\w+Controller') {
            break
        }
    }

    if (-not $classRoute) {
        continue
    }

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i].Trim()
        if ($line -notmatch '^\[Http(Get|Post|Put|Delete|Patch|Head|Options)(?:\("([^"]*)"\))?\]$') {
            continue
        }

        $httpMethod = $Matches[1].ToUpperInvariant()
        $methodRoute = $Matches[2]

        $methodAuthorizeLine = $null

        # Search contiguous attribute block below this Http attribute.
        for ($j = $i + 1; $j -lt $lines.Count; $j++) {
            $next = $lines[$j].Trim()
            if ([string]::IsNullOrWhiteSpace($next)) {
                continue
            }

            if ($next -match '^\[[A-Za-z]') {
                if ($next -match '^\[Authorize(?:\(.+\))?\]$') {
                    $methodAuthorizeLine = $next
                }
                continue
            }

            break
        }

        # Search contiguous attribute block above this Http attribute.
        if (-not $methodAuthorizeLine) {
            for ($j = $i - 1; $j -ge 0; $j--) {
                $prev = $lines[$j].Trim()
                if ([string]::IsNullOrWhiteSpace($prev)) {
                    continue
                }

                if ($prev -match '^\[[A-Za-z]') {
                    if ($prev -match '^\[Authorize(?:\(.+\))?\]$') {
                        $methodAuthorizeLine = $prev
                    }
                    continue
                }

                break
            }
        }

        $route = Resolve-RouteTemplate -ClassRoute $classRoute -MethodRoute $methodRoute -ControllerName $controllerName
        $likelyPolicy = Get-LikelyPolicyText -MethodAuthorizeLine $methodAuthorizeLine -ClassAuthorizeLine $classAuthorizeLine

        $endpoints.Add([PSCustomObject]@{
            Method = $httpMethod
            Route = $route
            Controller = "$controllerName" + "Controller"
            LikelyAuthPolicy = $likelyPolicy
        })
    }
}

$sortedEndpoints = $endpoints | Sort-Object Route, Method
$date = Get-Date -Format "yyyy-MM-dd"

$usesFallbackAuth = (Test-Path $programPath) -and ((Get-Content $programPath -Raw) -match 'FallbackPolicy')
$fallbackNote = if ($usesFallbackAuth) {
    'Production fallback policy in `ADOApi/Program.cs` requires authenticated users by default.'
} else {
    'No fallback authorization policy inferred from `ADOApi/Program.cs`.'
}

$linesOut = New-Object System.Collections.Generic.List[string]
$linesOut.Add("# API Catalog")
$linesOut.Add("")
$linesOut.Add("Auto-generated from controller attributes in ADOApi/Controllers on $date.")
$linesOut.Add("")
$linesOut.Add("## Inference Rules For Likely Auth Policy")
$linesOut.Add("")
$linesOut.Add("1. Method-level Authorize attribute overrides class-level auth.")
$linesOut.Add("2. Else class-level Authorize attribute is used.")
$linesOut.Add("3. Else fallback is Authenticated user (prod fallback).")
$linesOut.Add("")
$linesOut.Add($fallbackNote)
$linesOut.Add("")
$linesOut.Add("## Endpoint Catalog")
$linesOut.Add("")
$linesOut.Add("| Method | Route | Controller | Likely Auth Policy |")
$linesOut.Add("|---|---|---|---|")

foreach ($ep in $sortedEndpoints) {
    $method = Escape-Pipe $ep.Method
    $route = Escape-Pipe $ep.Route
    $controller = Escape-Pipe $ep.Controller
    $policy = Escape-Pipe $ep.LikelyAuthPolicy
    $linesOut.Add("| $method | $route | $controller | $policy |")
}

$linesOut.Add("")
$linesOut.Add("## Regeneration")
$linesOut.Add("")
$linesOut.Add("Run from repo root:")
$linesOut.Add("")
$linesOut.Add("powershell -ExecutionPolicy Bypass -File .\scripts\Generate-ApiCatalog.ps1")

Set-Content -Path $outputPath -Value $linesOut -Encoding utf8
Write-Host ("Generated " + $outputPath + " with " + $sortedEndpoints.Count + " endpoints.")
