# 获取用户输入的版本号  
$version = Read-Host "请输入版本号 (格式为 x.x.x)"  

# 验证版本号格式  
if ($version -notmatch '^\d+\.\d+\.\d+$') {  
    Write-Error "版本号格式不正确，应为 x.x.x 格式"  
    exit 1  
}  

# 设置路径常量  
$releaseDir = "bin\Release"  
$buildOutputDir = "$releaseDir\net9.0-windows10.0.22621.0\win-x64"  
$newFolderName = "ChatOpenAI"  
$newFolderPath = "$buildOutputDir\$newFolderName"  
$zipFileName = "ChatOpenAI_${version}_x64.zip"  

# 步骤1: 清理历史构建文件  
Write-Host "正在清理历史构建文件..." -ForegroundColor Cyan  
if (Test-Path $releaseDir) {  
    Remove-Item -Path $releaseDir -Recurse -Force  
    Write-Host "已清理: $releaseDir" -ForegroundColor Green  
} else {  
    Write-Host "无需清理: $releaseDir 不存在" -ForegroundColor Yellow  
}  

# 步骤2: 执行dotnet构建命令  
Write-Host "正在执行构建..." -ForegroundColor Cyan  
try {  
    $buildOutput = dotnet build --configuration Release --runtime win-x64
    Write-Host "构建成功!" -ForegroundColor Green  
} catch {  
    Write-Error "构建失败: $_"  
    exit 1  
}  

# 确保构建输出目录存在  
if (-not (Test-Path $buildOutputDir)) {  
    Write-Error "构建输出目录不存在: $buildOutputDir"  
    exit 1  
}  

# 步骤3: 创建新文件夹  
Write-Host "正在创建文件夹: $newFolderName" -ForegroundColor Cyan  
if (Test-Path $newFolderPath) {  
    Remove-Item -Path $newFolderPath -Recurse -Force  
    Write-Host "已移除旧文件夹: $newFolderPath" -ForegroundColor Yellow  
}  
New-Item -Path $newFolderPath -ItemType Directory | Out-Null  
Write-Host "已创建文件夹: $newFolderPath" -ForegroundColor Green  

# 步骤4: 复制指定文件和文件夹到新文件夹  
Write-Host "正在复制指定文件和文件夹..." -ForegroundColor Cyan  

# 指定要复制的文件  
$filesToCopy = @(  
    "$buildOutputDir\Community.PowerToys.Run.Plugin.ChatOpenAI.deps.json",  
    "$buildOutputDir\Community.PowerToys.Run.Plugin.ChatOpenAI.dll",  
    "$buildOutputDir\plugin.json"  
)  

# 指定要复制的文件夹  
$foldersToCopy = @(  
    "$buildOutputDir\Images"  
)  

# 复制文件  
$copiedFiles = 0  
foreach ($file in $filesToCopy) {  
    if (Test-Path $file) {  
        Copy-Item -Path $file -Destination $newFolderPath  
        Write-Host "已复制文件: $file" -ForegroundColor Green  
        $copiedFiles++  
    } else {  
        Write-Warning "文件不存在，无法复制: $file"  
    }  
}  

# 复制文件夹  
$copiedFolders = 0  
foreach ($folder in $foldersToCopy) {  
    if (Test-Path $folder) {  
        Copy-Item -Path $folder -Destination $newFolderPath -Recurse  
        Write-Host "已复制文件夹: $folder" -ForegroundColor Green  
        $copiedFolders++  
    } else {  
        Write-Warning "文件夹不存在，无法复制: $folder"  
    }  
}  

Write-Host "已复制 $copiedFiles 个文件和 $copiedFolders 个文件夹" -ForegroundColor Green  

# 步骤5: 创建zip压缩包  
Write-Host "正在创建zip压缩包: $zipFileName" -ForegroundColor Cyan  
try {  
    Compress-Archive -Path $newFolderPath -DestinationPath $zipFileName -Force  
    Write-Host "已创建zip压缩包: $zipFileName" -ForegroundColor Green  
} catch {  
    Write-Error "创建zip压缩包失败: $_"  
    exit 1  
}  

Write-Host "任务完成!" -ForegroundColor Green  
Write-Host "打包输出: $zipFileName" -ForegroundColor Cyan