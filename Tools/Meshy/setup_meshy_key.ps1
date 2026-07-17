$ErrorActionPreference = 'Stop'

Write-Host ''
Write-Host 'Meshy MCP key setup for Codex' -ForegroundColor Cyan
Write-Host 'Create/copy the key from https://www.meshy.ai/settings/api' -ForegroundColor DarkGray
Write-Host 'The entered value is masked and is not written to this project.' -ForegroundColor DarkGray
Write-Host ''

$secureKey = Read-Host 'Paste MESHY_API_KEY, then press Enter' -AsSecureString
$ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureKey)

try {
    $plainKey = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr)
    if ([string]::IsNullOrWhiteSpace($plainKey) -or -not $plainKey.StartsWith('msy')) {
        throw 'The value does not look like a Meshy API key (expected an msy... prefix).'
    }

    [Environment]::SetEnvironmentVariable('MESHY_API_KEY', $plainKey, 'User')
    $env:MESHY_API_KEY = $plainKey

    $flag = Join-Path $HOME '.codex\meshy-key-configured.flag'
    [IO.File]::WriteAllText($flag, (Get-Date).ToString('o'))

    Write-Host ''
    Write-Host 'Meshy API key saved to the Windows user environment.' -ForegroundColor Green
    Write-Host 'Close and reopen Codex so the Meshy MCP process inherits it.' -ForegroundColor Yellow
    Write-Host ''
    Read-Host 'Press Enter to close this window'
}
finally {
    if ($ptr -ne [IntPtr]::Zero) {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr)
    }
    $plainKey = $null
    $secureKey = $null
}
