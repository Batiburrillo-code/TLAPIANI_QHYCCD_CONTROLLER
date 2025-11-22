# Script para compilar y ejecutar el proyecto SdkDemo08
# Uso: .\compilar-y-ejecutar.ps1

# Configuración
$SolutionPath = "SdkDemo08.sln"
$Configuration = "Debug"
$Platform = "x64"
$MSBuildPath = "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\amd64\MSBuild.exe"

# Verificar si MSBuild existe
if (-not (Test-Path $MSBuildPath)) {
    Write-Host "MSBuild no encontrado en: $MSBuildPath" -ForegroundColor Red
    Write-Host "Buscando MSBuild..." -ForegroundColor Yellow
    
    $foundMSBuild = Get-ChildItem "C:\Program Files*" -Recurse -Filter "MSBuild.exe" -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName
    
    if ($foundMSBuild) {
        $MSBuildPath = $foundMSBuild
        Write-Host "MSBuild encontrado en: $MSBuildPath" -ForegroundColor Green
    } else {
        Write-Host "Error: No se pudo encontrar MSBuild. Por favor, instala Visual Studio." -ForegroundColor Red
        exit 1
    }
}

# Cerrar la aplicación si está ejecutándose
Write-Host "Verificando si la aplicación está ejecutándose..." -ForegroundColor Yellow
$process = Get-Process SdkDemo08 -ErrorAction SilentlyContinue
if ($process) {
    Write-Host "Cerrando aplicación existente..." -ForegroundColor Yellow
    $process | Stop-Process -Force
    Start-Sleep -Seconds 2
}

# Compilar
Write-Host "`nCompilando proyecto..." -ForegroundColor Cyan
Write-Host "Configuración: $Configuration | Plataforma: $Platform" -ForegroundColor Gray

& $MSBuildPath $SolutionPath /p:Configuration=$Configuration /p:Platform=$Platform /t:Build /v:minimal

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nCompilación exitosa!" -ForegroundColor Green
    
    # Ejecutar
    $exePath = "SdkDemo08\bin\$Platform\$Configuration\SdkDemo08.exe"
    if (Test-Path $exePath) {
        Write-Host "`nEjecutando aplicación..." -ForegroundColor Cyan
        Start-Process $exePath
    } else {
        Write-Host "Error: No se encontró el ejecutable en: $exePath" -ForegroundColor Red
    }
} else {
    Write-Host "`nError en la compilación. Revisa los mensajes de error arriba." -ForegroundColor Red
    exit 1
}

