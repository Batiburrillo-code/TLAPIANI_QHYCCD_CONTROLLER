# Pasos para Compilar y Ejecutar el Proyecto

## Requisitos Previos
- Visual Studio 2022 (o cualquier versión con MSBuild)
- .NET Framework 3.5

## Pasos para Compilar

### 1. Abrir PowerShell o CMD
Abre una terminal (PowerShell o CMD) y navega al directorio del proyecto:

```powershell
cd "C:\Users\serra\Downloads\CsharpSDKdemo (3)\SDK Demo for C#"
```

### 2. Encontrar MSBuild (si no está en el PATH)
Si MSBuild no está en tu PATH, encuentra su ubicación:

```powershell
Get-ChildItem "C:\Program Files*" -Recurse -Filter "MSBuild.exe" -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName
```

O usa la ruta común:
- Visual Studio 2022: `C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\amd64\MSBuild.exe`
- Visual Studio 2019: `C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe`

### 3. Compilar el Proyecto

**Opción A: Si MSBuild está en el PATH:**
```powershell
msbuild SdkDemo08.sln /p:Configuration=Debug /p:Platform=x64 /t:Build
```

**Opción B: Usando la ruta completa de MSBuild:**
```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\amd64\MSBuild.exe" "SdkDemo08.sln" /p:Configuration=Debug /p:Platform=x64 /t:Build
```

**Parámetros explicados:**
- `/p:Configuration=Debug` - Compila en modo Debug (puedes usar `Release` para producción)
- `/p:Platform=x64` - Compila para plataforma x64 (puedes usar `x86` o `AnyCPU`)
- `/t:Build` - Ejecuta el target Build

### 4. Verificar la Compilación
Si la compilación es exitosa, verás:
- `Compilación del proyecto terminada... -- ÉXITO.`
- El ejecutable estará en: `SdkDemo08\bin\x64\Debug\SdkDemo08.exe`

## Pasos para Ejecutar

### Opción 1: Ejecutar desde la Terminal
```powershell
.\SdkDemo08\bin\x64\Debug\SdkDemo08.exe
```

### Opción 2: Ejecutar desde el Explorador de Archivos
Navega a: `SdkDemo08\bin\x64\Debug\` y haz doble clic en `SdkDemo08.exe`

## Solución de Problemas

### Error: "El archivo está siendo utilizado en otro proceso"
**Solución:** Cierra la aplicación si está ejecutándose antes de compilar:
```powershell
# Encontrar y cerrar el proceso
Get-Process SdkDemo08 -ErrorAction SilentlyContinue | Stop-Process -Force
```

### Error: "MSBuild no se reconoce"
**Solución:** Usa la ruta completa de MSBuild o agrega MSBuild al PATH del sistema.

### Compilar en Modo Release
```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\amd64\MSBuild.exe" "SdkDemo08.sln" /p:Configuration=Release /p:Platform=x64 /t:Build
```

## Comandos Rápidos

### Compilar y Ejecutar en un solo paso (PowerShell)
```powershell
# Compilar
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\amd64\MSBuild.exe" "SdkDemo08.sln" /p:Configuration=Debug /p:Platform=x64 /t:Build

# Si compiló exitosamente, ejecutar
if ($LASTEXITCODE -eq 0) {
    Start-Process ".\SdkDemo08\bin\x64\Debug\SdkDemo08.exe"
}
```

### Limpiar y Recompilar
```powershell
# Limpiar
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\amd64\MSBuild.exe" "SdkDemo08.sln" /p:Configuration=Debug /p:Platform=x64 /t:Clean

# Recompilar
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\amd64\MSBuild.exe" "SdkDemo08.sln" /p:Configuration=Debug /p:Platform=x64 /t:Build
```

## Notas Importantes

1. **Cierra la aplicación antes de compilar** para evitar errores de archivo bloqueado
2. **El ejecutable compilado** está en `SdkDemo08\bin\x64\Debug\SdkDemo08.exe`
3. **Las advertencias** sobre campos no asignados son normales y no afectan la funcionalidad
4. **El proyecto usa .NET Framework 3.5**, asegúrate de tenerlo instalado

