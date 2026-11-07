# Build from source

The build requires Python, the .NET Framework C# compiler, and a local installation of R.E.P.O. build 26407089 or 26294889. The script reads the original game asset or a matching backup; it does not modify the game. Game files and generated resources remain local.

```powershell
py -m venv .venv
.\.venv\Scripts\python.exe -m pip install -r requirements.txt
.\build.ps1 -Python .\.venv\Scripts\python.exe -GamePath "C:\Program Files (x86)\Steam\steamapps\common\REPO"
```

The result is `dist/R.E.P.O. Change Font.exe`. Each executable supports only the game build used to generate its template; the script prints the selected build. The 1.3.0 release targets build 26407089. Players on build 26294889 can continue using 1.2.1 or build a separate executable from that build's original assets. Generated build files remain local and are excluded from Git. If the original file does not match the supported SHA-256, restore or verify the game files and try again.
