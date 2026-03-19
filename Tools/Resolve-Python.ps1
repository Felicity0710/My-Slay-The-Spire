function Get-ProjectPythonCommand {
    if ($script:ProjectPythonCommand) {
        return $script:ProjectPythonCommand
    }

    if ($env:SLAY_THE_HS_PYTHON) {
        $script:ProjectPythonCommand = [pscustomobject]@{
            File = $env:SLAY_THE_HS_PYTHON
            PrefixArgs = @()
            Display = $env:SLAY_THE_HS_PYTHON
        }
        return $script:ProjectPythonCommand
    }

    $python = Get-Command python -ErrorAction SilentlyContinue
    if ($python) {
        $script:ProjectPythonCommand = [pscustomobject]@{
            File = $python.Source
            PrefixArgs = @()
            Display = $python.Source
        }
        return $script:ProjectPythonCommand
    }

    $pyLauncher = Get-Command py -ErrorAction SilentlyContinue
    if ($pyLauncher) {
        $script:ProjectPythonCommand = [pscustomobject]@{
            File = $pyLauncher.Source
            PrefixArgs = @("-3")
            Display = "$($pyLauncher.Source) -3"
        }
        return $script:ProjectPythonCommand
    }

    throw @"
Python was not found.

Install Python 3 and ensure either `python` or `py` is available in PATH.
You can also point this project to a specific interpreter with:

    `$env:SLAY_THE_HS_PYTHON = 'C:\Path\To\python.exe'
"@
}

function Invoke-ProjectPython {
    param(
        [Parameter(Mandatory = $true, ValueFromRemainingArguments = $true)]
        [string[]]$Arguments
    )

    $command = Get-ProjectPythonCommand
    & $command.File @($command.PrefixArgs + $Arguments)
    if ($LASTEXITCODE -ne 0) {
        throw "Python command failed with exit code $LASTEXITCODE."
    }
}
