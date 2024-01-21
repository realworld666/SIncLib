REM remove all .cs files from target directory
del "D:\Steam\steamapps\common\Software Inc\DLLMods\SIncLib\*.cs"

robocopy *.cs ./ "D:\Steam\steamapps\common\Software Inc\DLLMods\SIncLib"
REM robocopy *.csproj ./ "D:\Steam\steamapps\common\Software Inc\DLLMods\SIncLib"
pause