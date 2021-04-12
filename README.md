# SIncLib
Collection of tools for Software Inc.

# Setup
The project contains a Visual Studio solution file. The dependent dlls are not included in this project so you will need to remove and readd references to the following files. These can be found in the Steam folder, most likely `C:\Program Files (x86)\Steam\steamapps\common\Software Inc\Software Inc_Data\Managed` on Windows
* Assembly-CSharp.dll
* UnityEngine.CoreModule.dll
* UnityEngine.dll
* UnityEngine.UI.dll

The code is run as a run time compiled dll mod. This means the source code is copied into the game directory. See the CoreDumping Wiki for more details https://softwareinc.coredumping.com/wiki/index.php/Code_Modding. A batch file, `copySource.bat` is included in the project to copy the correct files to the correct location. You will need to modify the target directoy to the location of the game install.
