
if ($PSVersionTable.PSVersion.Major -lt 6) {
    Write-Error  "Powershell Core 6 or greater required" -Category InvalidOperation
    exit
}

$github_repo = "IllusionMods/KK_Plugins"

$topdir = $PSScriptRoot
if ($topdir -eq "") {
    $topdir = "."
}

$README = $topdir + '\README.md'

$script:WebSession = $null
function WaitRateLimit ($headers = $null) {
    Write-Output "check rate limit"
    if ($null -eq $headers) {
        $check_url = "https://api.github.com/"
        if ($null -eq $script:WebSession) {
            $result = Invoke-RestMethod -SkipHttpErrorCheck -SessionVariable TmpSession -Method Head -Uri $check_url -ResponseHeadersVariable headers
            $script:WebSession = $TmpSession
        } else {
            $result = Invoke-RestMethod -SkipHttpErrorCheck -WebSession $script:WebSession -Method Head -Uri $check_url -ResponseHeadersVariable headers
        }
    }
    $limit = [int]($headers.'X-RateLimit-Limit'[0])
    $used = [int]($headers.'X-RateLimit-Used'[0])
    $remain = [int]($headers.'X-RateLimit-Remaining'[0])
    $wait = [int]($headers.'X-RateLimit-Reset'[0]) - [int]((New-TimeSpan -Start (Get-Date "01/01/1970") -End (Get-Date)).TotalSeconds) + 1
    $wait = $wait / ($remain + 1)
    if ($used -lt ($limit / 2)) {
        $wait = $wait / 10;
    }

    if ($used -lt ($limit / 4)) {
        $wait = $wait / 5;
    }

    if ($wait -gt 1) {
        Write-Warning "rate limit delay: $wait"
        Start-Sleep $wait
    }
}

$script:Releases = $null;
function Get-All-Releases() {
    if ($null -ne $script:Releases) {
        return $script:Releases;
    }

    $tmpReleases = New-Object System.Collections.Generic.List[System.Object];


    $releases = "https://api.github.com/repos/$github_repo/releases"
    $page = 0
    $Headers = $null
    while(1 -eq 1) {
        $params = @{
            page = $page
        }
        $page++;
        $request = @{
        	Method = "GET"
	        Uri = $releases
	        Body = $params
            WebSession = $script:WebSession
        }

        WaitRateLimit -headers $Headers
        $response = Invoke-RestMethod @request  -ResponseHeadersVariable Headers

        if ($response.length -lt 1) {
            break
        }
        foreach ($entry in $response) {
            $tmpReleases.Add($entry)
        }
    }

    $script:Releases = $tmpReleases
    return $tmpReleases

}
function Get-Latest($Key) {
    $releases = Get-All-Releases
    foreach ($entry in $releases) {
        foreach ($asset in $entry.assets) {
            if ($asset.name -match "^$key") {
                return $asset
            }
        }
    }
}


function Update-Links ($readmePath) {
    Write-Output "Updating $readmePath"
    $startLines = Select-String -Path $readmePath "^\[//\].*Latest Links";
    if ($startLines.Length -lt 1) {
         return;
    }
    $start = $startLines[0].LineNumber
    $content = Get-Content -Path $readmePath
    $i = $start
    $updated = 0;
    while ($i -lt $content.Length) {
        $line = $content[$i]
        $i++
        if ($line -match '^\[') {
            $key  = $line -replace '^\[','' -replace '\].*',''
            $latest = Get-Latest -Key $key
            if ($latest) {
                $version = $latest.name -replace "^$key.","" -replace "\.[^\.]*$",""
                $url = $latest.browser_download_url
                $newLine = "[$key]: $url " + '"' + $version + '"'
                if ($line -eq $newLine) {
                    continue
                }
                $updated = 1
                $content[$i-1] = $newLine
                Write-Output "Updated: $newLine"
            }
        }
    }
    if ($updated -eq 0) {
        return
    }
    Out-File -Encoding utf8 -InputObject $content -FilePath $readmePath   
}

Update-Links $README

Get-ChildItem -Path ($topdir + '\src' ) -Depth 3 -Filter README.md | ForEach-Object {
    Update-Links $_
}






