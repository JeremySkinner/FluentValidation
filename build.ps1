param(
  [string]$configuration = 'Release',
  [string]$path = $PSScriptRoot,
  [string[]]$targets = 'default'
)

$ErrorActionPreference = "Stop"

# Boostrap posh-build
$build_dir = Join-Path $path ".build"
if (! (Test-Path (Join-Path $build_dir "Posh-Build.ps1"))) {
  Write-Host "Installing posh-build..."; New-Item -Type Directory $build_dir -ErrorAction Ignore | Out-Null;
  (New-Object Net.WebClient).DownloadFile('https://raw.githubusercontent.com/jeremyskinner/posh-build/master/Posh-Build.ps1', "$build_dir/Posh-Build.ps1")
}
. (Join-Path $build_dir "Posh-Build.ps1")

# Set these variables as desired
$packages_dir = Join-Path $build_dir "packages"
$output_dir = Join-Path $build_dir $configuration
$solution_file = Join-Path $path "FluentValidation.sln"
$nuget_key = Resolve-Path "~/Dropbox/nuget-access-key.txt" -ErrorAction Ignore

target default -depends compile, test, deploy

target compile {
  Invoke-Dotnet build $solution_file -c $configuration --no-incremental
}

target test {
  Invoke-Dotnet test $solution_file -c $configuration --no-build
}

target deploy {
  Remove-Item $packages_dir -Force -Recurse -ErrorAction Ignore 2> $null
  Remove-Item $output_dir -Force -Recurse -ErrorAction Ignore 2> $null

  Invoke-Dotnet pack $solution_file -c $configuration
}

target publish {
  $interactive = $true
  $key = $Env:NUGET_API_KEY

  if ($key) {
    $interactive = $false
  }
  elseif (test-path "$nuget_key") {
    $key = get-content $nuget_key
    $interactive = $true
  }
  else {
    throw "NUGET_API_KEY or local key not set"
  }

  Nuget-Push -directory $packages_dir -key $key -prompt $interactive
}

Start-Build $targets
