param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\src\KinectKids\Assets\Voice')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Runtime.WindowsRuntime
[Windows.Media.SpeechSynthesis.SpeechSynthesizer, Windows.Media.SpeechSynthesis, ContentType = WindowsRuntime] | Out-Null
[Windows.Media.SpeechSynthesis.SpeechSynthesisStream, Windows.Media.SpeechSynthesis, ContentType = WindowsRuntime] | Out-Null

function Wait-WinRtOperation($Operation, [Type]$ResultType) {
    $method = [System.WindowsRuntimeSystemExtensions].GetMethods() |
        Where-Object { $_.Name -eq 'AsTask' -and $_.IsGenericMethod -and $_.GetParameters().Count -eq 1 } |
        Select-Object -First 1
    $task = $method.MakeGenericMethod($ResultType).Invoke($null, @($Operation))
    $task.Wait()
    return $task.Result
}

$prompts = [ordered]@{
    prompt_math = 'Vad blir'
    plus = 'plus'
    minus = 'minus'
    prompt_starts = 'Vilken bokstav börjar ordet'
    prompt_with = 'med?'
    prompt_missing = 'Vilken bokstav saknas i ordet'
    shape_circle = 'Hitta cirkeln.'
    shape_triangle = 'Hitta triangeln.'
    shape_square = 'Hitta kvadraten.'
    shape_star = 'Hitta stjärnan.'
    pattern_0 = 'Cirkel, kvadrat, cirkel, kvadrat. Vad kommer sedan?'
    pattern_1 = 'Triangel, triangel, stjärna, triangel, triangel. Vad kommer sedan?'
    pattern_2 = 'Ett, två, tre, fyra. Vad kommer sedan?'
    pattern_3 = 'Två, fyra, sex, åtta. Vad kommer sedan?'
    pattern_4 = 'A, B, A, B. Vad kommer sedan?'
    pattern_5 = 'Liten, stor, liten, stor. Vad kommer sedan?'
    simon_hands_up = 'Simon säger: händerna över huvudet!'
    simon_arms_out = 'Simon säger: armarna rakt ut!'
    simon_hands_together = 'Simon säger: händerna tillsammans!'
    simon_duck = 'Simon säger: ducka!'
    simon_ready = 'Gör dig redo!'
    correct = 'Rätt! Bra jobbat.'
    retry = 'Bra försök! Prova ett annat svar.'
    great_next = 'Snyggt! Nästa rörelse.'
    new_movement = 'Nästan! Vi tar en ny rörelse.'
    balloon_instruction = 'Rör en handring in i ballongerna för att smälla dem!'
    word_sol = 'sol'
    word_mane = 'måne'
    word_boll = 'boll'
    word_hus = 'hus'
    word_katt = 'katt'
    word_hund = 'hund'
    word_fisk = 'fisk'
    word_fagel = 'fågel'
    word_glass = 'glass'
    word_banan = 'banan'
    word_apple = 'äpple'
    word_ost = 'ost'
    word_bat = 'båt'
    word_bil = 'bil'
    word_tag = 'tåg'
    word_cykel = 'cykel'
}
0..20 | ForEach-Object { $prompts['number_' + $_] = $_.ToString() }

$target = [IO.Path]::GetFullPath($OutputDirectory)
[IO.Directory]::CreateDirectory($target) | Out-Null
$synthesizer = New-Object Windows.Media.SpeechSynthesis.SpeechSynthesizer
$voice = [Windows.Media.SpeechSynthesis.SpeechSynthesizer]::AllVoices |
    Where-Object { $_.Language -eq 'sv-SE' } | Select-Object -First 1
if ($null -eq $voice) { throw 'Ingen svensk Windows-röst hittades.' }
$synthesizer.Voice = $voice

foreach ($entry in $prompts.GetEnumerator()) {
    $stream = Wait-WinRtOperation ($synthesizer.SynthesizeTextToStreamAsync($entry.Value)) `
        ([Windows.Media.SpeechSynthesis.SpeechSynthesisStream])
    $input = [System.IO.WindowsRuntimeStreamExtensions]::AsStreamForRead($stream)
    $path = Join-Path $target ($entry.Key + '.wav')
    $output = [IO.File]::Create($path)
    try { $input.CopyTo($output) }
    finally {
        $output.Dispose()
        $input.Dispose()
        $stream.Dispose()
    }
}
$synthesizer.Dispose()
Write-Host "Skapade $($prompts.Count) svenska röstfiler i $target"
