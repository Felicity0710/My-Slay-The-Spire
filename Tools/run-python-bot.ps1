$ErrorActionPreference = "Stop"

$python = "C:\Users\Administrator\AppData\Local\Python\pythoncore-3.14-64\python.exe"
& $python (Join-Path $PSScriptRoot "python\simple_bot.py")
