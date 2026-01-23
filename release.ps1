$dir = $PSScriptRoot + "/bin/"

$Folder = $dir + "out"

# Ensure the folder exists
if (-not (Test-Path $Folder -PathType Container)) {
    Write-Error "Folder '$Folder' does not exist."
    exit 1
}

# Get all zip files in the folder
$zipFiles = Get-ChildItem -Path $Folder -Filter '*.zip' | Where-Object { -not $_.PSIsContainer }

# Get current date in YYYY-MM-DD format
$currentDate = Get-Date -Format 'yyyy-MM-dd'

# Group files by prefix (before first '_')
$groups = $zipFiles | Group-Object { $_.BaseName.Split('_')[0] }

foreach ($group in $groups) {
    $prefix = $group.Name
    $archiveName = $dir + $prefix + "_Plugins_" + $currentDate + ".zip"

    Remove-Item $archiveName -ErrorAction SilentlyContinue

    # Add all files in the group to the new archive
    $filePaths = $group.Group | ForEach-Object { $_.FullName }
    Compress-Archive -Path $filePaths -DestinationPath $archiveName
    Write-Host "Created archive: $archiveName"
}