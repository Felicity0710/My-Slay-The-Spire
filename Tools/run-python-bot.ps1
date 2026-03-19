$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "Resolve-Python.ps1")
Invoke-ProjectPython (Join-Path $PSScriptRoot "python\simple_bot.py")
