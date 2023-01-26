Param(
    [string]
    $version = ""
)

if (Test-Path "./Release")
{
    rm -R -Force ./Release
}

$runtimes = @{ win32 = "win-x64"; win64 = "win-x64"; linux64 = "linux-x64"; osx64 = "osx-x64" }

$runtimes.GetEnumerator() | ForEach-Object {
    dotnet publish MpvScreenshotSorter -c Release -r $_.Value --no-self-contained -p:PublishSingleFile=true -o "./Release/$($_.Key)"
    dotnet publish MpvScreenshotSorter.Cli -c Release -r $_.Value --no-self-contained -p:PublishSingleFile=true -o "./Release/$($_.Key)"
}

if ([string]::IsNullOrWhiteSpace($version))
{
    7z a ./Release/NFFileUtils-win32.zip ./Release/win32/mpvshotsort.exe ./Release/win32/mpvshotsortw.exe
    7z a ./Release/NFFileUtils-win64.zip ./Release/win64/mpvshotsort.exe ./Release/win64/mpvshotsortw.exe
    7z a ./Release/NFFileUtils-linux64.zip ./Release/linux64/mpvshotsort ./Release/linux64/mpvshotsortw
    7z a ./Release/NFFileUtils-osx64.zip ./Release/osx64/mpvshotsort ./Release/osx64/mpvshotsortw
}
else
{
    7z a "./Release/NFFileUtils-win32-v$($version).zip" ./Release/win32/mpvshotsort.exe ./Release/win32/mpvshotsortw.exe
    7z a "./Release/NFFileUtils-win64-v$($version).zip" ./Release/win64/mpvshotsort.exe ./Release/win64/mpvshotsortw.exe
    7z a "./Release/NFFileUtils-linux64-v$($version).zip" ./Release/linux64/mpvshotsort ./Release/linux64/mpvshotsortw
    7z a "./Release/NFFileUtils-osx64-v$($version).zip" ./Release/osx64/mpvshotsort ./Release/osx64/mpvshotsortw
}

rm -R -Force ./Release/win32
rm -R -Force ./Release/win64
rm -R -Force ./Release/linux64
rm -R -Force ./Release/osx64