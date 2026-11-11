param([string]$Unity = 'C:/Program Files/Unity/Hub/Editor/6000.3.11f1/Editor/Unity.exe')
$ErrorActionPreference = 'Stop'
$project = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$validation = Join-Path $project 'Temp/GunterSwingValidation'
foreach ($folder in @('Assets/Editor','Packages','ProjectSettings')) {
    New-Item -ItemType Directory -Force -Path (Join-Path $validation $folder) | Out-Null
}
Copy-Item -LiteralPath (Join-Path $project 'Assets/Scripts') -Destination (Join-Path $validation 'Assets') -Recurse -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'GunterSwingRegression.cs') -Destination (Join-Path $validation 'Assets/Editor/GunterSwingRegression.cs') -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'DemonAnimationRegression.cs') -Destination (Join-Path $validation 'Assets/Editor/DemonAnimationRegression.cs') -Force
foreach ($folder in @('Assets/TestArt','Assets/TestPrefabs')) {
    New-Item -ItemType Directory -Force -Path (Join-Path $validation $folder) | Out-Null
}
$art = Join-Path $project 'Assets/Art/Tiny RPG Character Asset Pack 02 v1.01-Free Demon_A&Blood Monster_A/Tiny RPG Character Asset Pack 02 -Free Demon_A&Blood Monster_A/Characters(100x100 split)/Demon_A/Demon_A'
foreach ($action in @('Idle','Walk','Attack01','Attack02','Hurt','Death')) {
    foreach ($extension in @('.png','.png.meta')) {
        Copy-Item -LiteralPath (Join-Path $art ('Demon_A_' + $action + $extension)) -Destination (Join-Path $validation 'Assets/TestArt') -Force
    }
}
foreach ($prefab in @('Enemies/Enemy','Progression/ExperiencePickup')) {
    foreach ($extension in @('.prefab','.prefab.meta')) {
        Copy-Item -LiteralPath (Join-Path $project ('Assets/Prefabs/' + $prefab + $extension)) -Destination (Join-Path $validation 'Assets/TestPrefabs') -Force
    }
}
foreach ($file in @('ProjectVersion.txt','ProjectSettings.asset','TagManager.asset','Physics2DSettings.asset')) {
    Copy-Item -LiteralPath (Join-Path $project ('ProjectSettings/' + $file)) -Destination (Join-Path $validation 'ProjectSettings') -Force
}
$dependencies = @{}
foreach ($module in @('physics2d','physics','ui','uielements','imgui','jsonserialize','animation','audio','imageconversion')) {
    $dependencies['com.unity.modules.' + $module] = '1.0.0'
}
foreach ($name in @('com.unity.inputsystem','com.unity.ugui')) {
    $package = Get-ChildItem -LiteralPath (Join-Path $project 'Library/PackageCache') -Directory | Where-Object { $_.Name.StartsWith($name + '@') } | Select-Object -First 1
    if ($null -eq $package) { throw ('Missing cached package: ' + $name) }
    $dependencies[$name] = 'file:' + $package.FullName.Replace('\','/')
}
$encoding = New-Object System.Text.UTF8Encoding($false)
[IO.File]::WriteAllText((Join-Path $validation 'Packages/manifest.json'), (@{ dependencies = $dependencies } | ConvertTo-Json), $encoding)
$log = Join-Path $validation 'GunterSwingRegression.log'
$arguments = @('-batchmode','-nographics','-projectPath', ('"' + $validation + '"'), '-executeMethod','GunterSwingRegression.Begin','-logFile', ('"' + $log + '"'))
$process = Start-Process -FilePath $Unity -ArgumentList $arguments -WindowStyle Hidden -PassThru
Write-Output ('Validation PID: ' + $process.Id)
Write-Output ('Validation log: ' + $log)