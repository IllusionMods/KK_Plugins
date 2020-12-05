$array = @("KK", "EC", "AI", "HS", "HS2", "PH", "PC")
$dir = $PSScriptRoot + "/bin/"
$copy = $dir + "/copy/BepInEx"

Remove-Item -Force -Path ($dir + "/copy") -Recurse -ErrorAction SilentlyContinue
Remove-Item -Force -Path ($dir + "/out") -Recurse -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path ($copy + "/plugins")
New-Item -ItemType Directory -Force -Path ($dir + "/out")

foreach ($element in $array) 
{
	foreach ($filepath in [System.IO.Directory]::EnumerateFiles($dir,"*.dll","AllDirectories"))
	{
		$filename = $filepath.Replace($dir, "")

		if ($filename.StartsWith($element)) 
		{
			Remove-Item -Force -Path ($copy) -Recurse
			New-Item -ItemType Directory -Force -Path ($copy + "/plugins/" + $element + "_Plugins/")
			
			Copy-Item -Path ($filepath) -Destination ($copy + "/plugins/" + $element + "_Plugins/") -Force
			
			$filepathxml = get-childitem $dir -Recurse -Force -include $filename.Replace(".dll", ".xml") -ErrorAction SilentlyContinue
			if ($filepathxml)
			{
				Copy-Item -Path ($filepathxml) -Destination ($copy + "/plugins/" + $element + "_Plugins/") -Force
			}
			
			$version = "v" + (Get-ChildItem -Path ($filepath) -Filter "*.dll" -Force)[0].VersionInfo.FileVersion.ToString()
			$zipfilename = $filename.Replace(".dll", " " + $version + ".zip")
			
			"Creating archive: " + $zipfilename
			Compress-Archive -Path $copy -Force -CompressionLevel "Optimal" -DestinationPath ($dir + "out/" + $zipfilename)
		}
	}
}

Remove-Item -Force -Path ($dir + "/copy") -Recurse
