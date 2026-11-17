# Packs existing Dread Rooms tiles without resizing, rotating, or recolouring them.
param([string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path)
Add-Type -AssemblyName System.Drawing
$source = Join-Path $ProjectRoot 'Tools/ArtSources/Packs/DreadRooms3/16x16/room-builder/floors/hardwood'
$tiles = @('floor.png', 'floor_alt.png', 'floor_damaged.png') | ForEach-Object {
    [System.Drawing.Bitmap]::new((Join-Path $source $_))
}
$canvas = [System.Drawing.Bitmap]::new(512, 512)
$graphics = [System.Drawing.Graphics]::FromImage($canvas)
$random = [System.Random]::new(7319)
$damage = [bool[,]]::new(32, 32)
$counts = @(0, 0, 0)
try {
    for ($y = 0; $y -lt 32; $y++) {
        for ($x = 0; $x -lt 32; $x++) {
            $roll = $random.Next(100)
            $index = if ($roll -lt 22) { 1 } else { 0 }
            # Keep damage sparse, separated, and away from the repeating texture seam.
            if ($roll -ge 98 -and $x -gt 0 -and $y -gt 0 -and $x -lt 31 -and $y -lt 31) {
                if (-not $damage[($x-1),$y] -and -not $damage[$x,($y-1)]) { $index = 2; $damage[$x,$y] = $true }
            }
            $graphics.DrawImageUnscaled($tiles[$index], $x * 16, $y * 16)
            $counts[$index]++
        }
    }
    $canvas.Save((Join-Path $ProjectRoot 'Assets/Art/Environment/HouseWoodFloor.png'), [System.Drawing.Imaging.ImageFormat]::Png)
    "Tiles: base=$($counts[0]), alternate=$($counts[1]), damaged=$($counts[2])"
}
finally {
    $graphics.Dispose()
    $canvas.Dispose()
    foreach ($tile in $tiles) { $tile.Dispose() }
}
