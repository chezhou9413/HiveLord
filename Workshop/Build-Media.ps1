param(
    [string]$SourceDirectory = 'E:\gifrimw',
    [string]$FFmpeg = 'E:\ModdevMics\Tools\FFmpeg\runtime\ffmpeg-9.0.1-essentials_build\bin\ffmpeg.exe'
)

$ErrorActionPreference = 'Stop'
$mediaDirectory = Join-Path $PSScriptRoot 'Media'
New-Item -ItemType Directory -Path $mediaDirectory -Force | Out-Null

#调用媒体转换工具并将失败结果作为构建错误报告。
function Invoke-MediaConversion {
    param([string[]]$Arguments)
    & $FFmpeg @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "媒体转换失败，退出码：$LASTEXITCODE"
    }
}

#选取连贯动作片段，通过缩放、抽帧和调色板压缩生成循环演示。
function Export-AnimatedShowcase {
    param($Clip)
    $sourcePath = Join-Path $SourceDirectory $Clip.Source
    $destinationPath = Join-Path $mediaDirectory $Clip.Output
    $filter = "[0:v]setpts=(PTS-STARTPTS)/$($Clip.Speed),fps=8,scale=$($Clip.Width):-1:flags=area,hqdn3d=4:3:6:4,split[a][b];[a]palettegen=max_colors=32:stats_mode=full[p];[b][p]paletteuse=dither=none:diff_mode=rectangle"
    Invoke-MediaConversion @('-hide_banner', '-loglevel', 'error',
        '-ss', [string]$Clip.Start, '-t', [string]$Clip.Duration,
        '-i', $sourcePath, '-filter_complex', $filter, '-loop', '0', '-y', $destinationPath)
}

$clips = @(
    @{ Source = '摧毁孢子喷涌.gif'; Output = 'spore-spewer-destruction.gif'; Start = 0; Duration = 10.17; Speed = 1.25; Width = 480 },
    @{ Source = '技能.gif'; Output = 'hive-lord-summoning.gif'; Start = 16; Duration = 16; Speed = 2; Width = 400 },
    @{ Source = '战斗.gif'; Output = 'hive-lord-battle.gif'; Start = 24; Duration = 14; Speed = 2; Width = 376 }
)

foreach ($clip in $clips) {
    Export-AnimatedShowcase $clip
}

Copy-Item -LiteralPath (Join-Path $PSScriptRoot '..\HiveLord\About\Preview.png') `
    -Destination (Join-Path $mediaDirectory 'hive-lord-cover.png') -Force
Copy-Item -LiteralPath (Join-Path $SourceDirectory '奖励.png') `
    -Destination (Join-Path $mediaDirectory 'hive-lord-rewards.png') -Force
Invoke-MediaConversion @('-hide_banner', '-loglevel', 'error',
    '-i', (Join-Path $SourceDirectory '展示图.png'), '-vf', 'scale=1000:-1:flags=lanczos',
    '-frames:v', '1', '-compression_level', '9', '-y', (Join-Path $mediaDirectory 'hive-lord-showcase.png'))

#限制每份输出小于一百万字节，避免更换原素材后无意发布超大图片。
Get-ChildItem -LiteralPath $mediaDirectory -File | ForEach-Object {
    if ($_.Length -ge 1000000) {
        throw "素材超出大小目标，请调整片段和压缩参数：$($_.Name)，$($_.Length) 字节"
    }
    Write-Output "$($_.Name)：$($_.Length) 字节"
}
