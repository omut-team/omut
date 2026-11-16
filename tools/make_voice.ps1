# Озвучка реплик голосом Windows (Microsoft Pavel, русский, работает без интернета).
# 1) Unity: меню Omut -> Собрать сцену Main (или Omut -> Озвучка: выгрузить реплики) пишет tools/voice/lines.txt
# 2) Запуск из корня репозитория в Windows PowerShell:  powershell -ExecutionPolicy Bypass -File tools\make_voice.ps1
# Файлы ложатся в game/Assets/Resources/Voice/v<код>.wav. Нет файла для реплики: в игре будет "бормотание".
# Если голоса Pavel нет: Параметры Windows -> Время и язык -> Речь -> Добавить голоса -> Русский.

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$lines = Join-Path $root "tools\voice\lines.txt"
$out = Join-Path $root "game\Assets\Resources\Voice"
New-Item -ItemType Directory -Force $out | Out-Null

Add-Type -AssemblyName System.Runtime.WindowsRuntime
[Windows.Media.SpeechSynthesis.SpeechSynthesizer, Windows.Media.SpeechSynthesis, ContentType = WindowsRuntime] | Out-Null
[Windows.Media.SpeechSynthesis.SpeechSynthesisStream, Windows.Media.SpeechSynthesis, ContentType = WindowsRuntime] | Out-Null

$asTaskGeneric = ([System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object {
    $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1'
})[0]

function Await($operation, $type) {
    $task = $asTaskGeneric.MakeGenericMethod($type).Invoke($null, @($operation))
    $task.Wait(-1) | Out-Null
    return $task.Result
}

$synth = New-Object Windows.Media.SpeechSynthesis.SpeechSynthesizer
$pavel = [Windows.Media.SpeechSynthesis.SpeechSynthesizer]::AllVoices | Where-Object { $_.DisplayName -like "*Pavel*" } | Select-Object -First 1
if ($null -eq $pavel) { throw "Нет голоса Microsoft Pavel. Добавь русский голос в настройках Windows." }
$synth.Voice = $pavel

# Голос под характер: Тарас старый и медленный, Сымон молодой, письмо деда тихо и медленно.
function Prosody($speaker) {
    switch ($speaker) {
        "Тарас"       { return @{ pitch = "-18%"; rate = "-12%" } }
        "Михалыч"     { return @{ pitch = "-6%";  rate = "0%" } }
        "Сымон"       { return @{ pitch = "+12%"; rate = "+4%" } }
        default       { return @{ pitch = "-22%"; rate = "-18%" } }
    }
}

$count = 0
foreach ($row in Get-Content $lines -Encoding UTF8) {
    $parts = $row.Split('|', 3)
    if ($parts.Count -lt 3) { continue }
    $name, $speaker, $text = $parts
    $file = Join-Path $out ($name + ".wav")
    if (Test-Path $file) { continue }

    $p = Prosody $speaker
    $safe = [System.Security.SecurityElement]::Escape($text)
    $ssml = "<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='ru-RU'><prosody pitch='$($p.pitch)' rate='$($p.rate)'>$safe</prosody></speak>"

    $stream = Await ($synth.SynthesizeSsmlToStreamAsync($ssml)) ([Windows.Media.SpeechSynthesis.SpeechSynthesisStream])
    $src = [System.IO.WindowsRuntimeStreamExtensions]::AsStreamForRead($stream)
    $fs = [System.IO.File]::Create($file)
    $src.CopyTo($fs)
    $fs.Close()
    $src.Close()
    $count++
}

Write-Host "Озвучено новых реплик: $count (папка $out)"
