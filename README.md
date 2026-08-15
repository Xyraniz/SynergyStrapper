# Synergy Strapper

<p align="center">
    <img src="https://github.com/Xyraniz/SynergyStrapper/raw/main/Images/SynergyStrapper-full-dark.png#gh-dark-mode-only" width="520">
    <img src="https://github.com/Xyraniz/SynergyStrapper/raw/main/Images/SynergyStrapper-full-light.png#gh-light-mode-only" width="520">
</p>

<p align="center">
  Bootstrapper alternativo para Roblox con una interfaz propia en tonos grises y negros.
</p>

> Synergy Strapper es un fork independiente y no oficial. Las únicas fuentes de distribución de este proyecto son este repositorio y sus releases de GitHub.

## Funciones principales

Synergy Strapper instala y actualiza Roblox Player y Roblox Studio, ofrece una ventana de configuración, estilos de bootstrapper personalizables, gestión reversible de FastFlags, accesos directos, integraciones opcionales y herramientas de diagnóstico. El proyecto está diseñado para equipos Windows 10 o posteriores.

El perfil de rendimiento incluido aplica únicamente ajustes conservadores y reversibles para reducir el uso de GPU y VRAM. No garantiza una cantidad concreta de FPS: Roblox puede cambiar o retirar FastFlags, y el resultado depende del hardware, la versión del cliente y la experiencia ejecutada. El perfil se puede desactivar desde la página de FastFlags.

## Compilación

La solución requiere el SDK de .NET 6.0.428 indicado en `global.json`, además de un entorno capaz de compilar WPF para Windows. El submódulo `wpfui` debe estar inicializado antes de restaurar dependencias.

```powershell
git clone https://github.com/Xyraniz/SynergyStrapper.git
cd SynergyStrapper
git submodule update --init --recursive
dotnet restore
dotnet build SynergyStrapper.sln -c Release
dotnet publish SynergyStrapper/SynergyStrapper.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

La publicación `--self-contained false` requiere .NET 6 Desktop Runtime en el equipo de destino. Para producir un ejecutable que incluya el runtime, se puede utilizar `--self-contained true`, aunque el artefacto será más grande. La publicación recomendada para distribución debe realizarse en Windows para conservar correctamente los recursos del apphost y firmarse con un certificado controlado por el distribuidor.

## Atribución y licencias

Synergy Strapper contiene código derivado de Bloxstrap, publicado originalmente bajo la licencia MIT. Se conserva la atribución y el texto de la licencia correspondiente en `LICENSE`. También se incluyen dependencias de terceros con sus propias licencias; consulta los archivos de licencia distribuidos junto al ejecutable.

Synergy Strapper no está afiliado a Roblox Corporation ni pretende ser una herramienta oficial de Roblox. No descargues binarios desde sitios que no estén controlados por el propietario de este repositorio.

## Estado del proyecto

El código se mantiene como un fork independiente. Los cambios de rendimiento se aplican de forma conservadora para priorizar estabilidad, reversibilidad y compatibilidad con futuras versiones de Roblox.
