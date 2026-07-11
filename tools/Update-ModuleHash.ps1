# 콘텐츠 모듈의 sha256 해시를 다시 계산해 채워넣는 개발 도구.
# 규칙(스펙 확정): 낱말·문장 모듈은 items를 "`n"으로 이은 UTF-8, 긴글은 body의 UTF-8.
# 사용: powershell -File tools\Update-ModuleHash.ps1 <module.json> [<module2.json> ...]
param([Parameter(Mandatory = $true)][string[]]$Paths)

foreach ($p in $Paths) {
    $raw = [System.IO.File]::ReadAllText($p)
    $json = $raw | ConvertFrom-Json

    if ($null -ne $json.items) {
        $payload = ($json.items -join "`n")
    } elseif ($null -ne $json.body) {
        $payload = $json.body
    } else {
        Write-Error "items도 body도 없는 화일: $p"
        continue
    }

    $sha = [System.Security.Cryptography.SHA256]::Create()
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($payload)
    $hex = ([System.BitConverter]::ToString($sha.ComputeHash($bytes)) -replace '-', '').ToLower()
    $sha.Dispose()

    $newRaw = $raw -replace '"hash"\s*:\s*"[^"]*"', ('"hash": "sha256:' + $hex + '"')
    [System.IO.File]::WriteAllText($p, $newRaw, (New-Object System.Text.UTF8Encoding($false)))
    Write-Output "$([System.IO.Path]::GetFileName($p)) -> sha256:$hex"
}
