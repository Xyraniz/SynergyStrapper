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

Desde la v1.0.4, Synergy Strapper incluye perfiles nombrados de FastFlags, un editor de Global Settings para las preferencias XML de Roblox, selector de canales para Player y Studio, navegador de servidores públicos, historial local de las últimas 50 partidas y migración opcional desde Bloxstrap, Fishstrap o Voidstrap. Estas funciones no almacenan cookies, tokens ni credenciales de Roblox. La 1.0.4 corrige bindings de selectores, la apertura de Server Browser, la restauración de mods y el manejo de errores del watcher.

La página de Synergy Strapper también mantiene una limpieza manual de mantenimiento. La acción elimina archivos con más de 30 días únicamente en las carpetas conocidas de logs y caché de Synergy Strapper y Roblox, conserva el log activo, ignora archivos de sistema y limita la operación a 200 archivos por carpeta.

Los perfiles de FastFlags se guardan como JSON dentro de la instalación y permiten guardar, fusionar, reemplazar o borrar configuraciones. El navegador de servidores utiliza únicamente el endpoint público de Roblox y aplica espera progresiva cuando la API limita solicitudes. El canal se escribe en las claves de registro que utiliza el bootstrapper, y la importación de otra instalación nunca elimina la fuente.

## Compilación

La solución requiere el SDK de .NET 6.0.428 indicado en `global.json`, además de un entorno capaz de compilar WPF para Windows. El submódulo `wpfui` debe estar inicializado antes de restaurar dependencias.

```powershell
git clone https://github.com/Xyraniz/SynergyStrapper.git
cd SynergyStrapper
git submodule update --init --recursive
dotnet restore
dotnet build SynergyStrapper.sln -c Release
dotnet publish SynergyStrapper/SynergyStrapper.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

La publicación oficial v1.0.4 es autocontenida: incluye el runtime de .NET 6 Desktop para reducir la posibilidad de que el usuario tenga que instalar dependencias manualmente. El artefacto es más grande y la publicación recomendada debe realizarse en Windows para conservar correctamente los recursos del apphost y firmarse con un certificado controlado por el distribuidor.

## Atribución y licencias

Synergy Strapper contiene código derivado de Bloxstrap, publicado originalmente bajo la licencia MIT. Se conserva la atribución y el texto de la licencia correspondiente en `LICENSE`. También se incluyen dependencias de terceros con sus propias licencias; consulta los archivos de licencia distribuidos junto al ejecutable.

Synergy Strapper no está afiliado a Roblox Corporation ni pretende ser una herramienta oficial de Roblox. No descargues binarios desde sitios que no estén controlados por el propietario de este repositorio.

## Estado del proyecto

El código se mantiene como un fork independiente. Los cambios de rendimiento se aplican de forma conservadora para priorizar estabilidad, reversibilidad y compatibilidad con futuras versiones de Roblox.

## Distribución y actualizaciones

Los ejecutables publicados deben distribuirse como **assets de GitHub Releases**, no como enlaces a archivos arbitrarios ni como binarios descargados desde terceros. El cliente consulta la última release publicada, selecciona exclusivamente el asset `SynergyStrapper.exe`, comprueba su tamaño y valida su SHA-256 antes de relanzar el proceso de actualización.

El workflow `CI (Release)` compila siempre desde el código fuente en un runner Windows. Para una release normal, crea un tag semántico con el formato `vX.Y.Z` y súbelo a GitHub; el workflow compilará el ejecutable autocontenido single-file `win-x64` y adjuntará `SynergyStrapper.exe` junto con `SynergyStrapper.exe.sha256` a una release publicada. El tag y la versión de `SynergyStrapper/SynergyStrapper.csproj` deben coincidir.

El mismo workflow se puede ejecutar manualmente desde **Actions → CI (Release)**. El parámetro `release_tag` permite crear o reemplazar una release existente, y `source_ref` indica la rama o commit que se debe compilar. Esta modalidad se utiliza para corregir o regenerar los assets de una release ya publicada, por ejemplo `v1.0.1`, sin crear una nueva versión. El repositorio ya no necesita almacenar un `.exe` en la raíz.

> El actualizador no fuerza una actualización cuando GitHub no responde, cuando no existe una release publicada compatible o cuando la integridad del asset no se puede comprobar. En esos casos, la instalación actual permanece intacta y el usuario puede abrir la página oficial de releases.

## Rendimiento y compatibilidad

Synergy Strapper no puede garantizar más FPS que el cliente original porque el rendimiento final depende del hardware, la versión de Roblox y la experiencia ejecutada. Por seguridad, el perfil de rendimiento se mantiene reversible y conservador; evita flags no documentadas o cambios que desactiven protecciones del cliente. Las mejoras del bootstrapper se concentran en no duplicar procesos, reutilizar descargas verificadas y evitar trabajo de red o disco innecesario.
