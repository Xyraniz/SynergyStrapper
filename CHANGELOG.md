# Changelog

## [1.0.0] — Synergy Strapper

### Identidad y distribución

Se integró el código base de Bloxstrap en el repositorio `Xyraniz/SynergyStrapper` y se renombraron el proyecto, la solución, la carpeta principal, los namespaces, los recursos, los iconos y los nombres de configuración a `SynergyStrapper`. El nombre visible de la aplicación se presenta como **Synergy Strapper**, mientras que los identificadores internos y el nombre del ejecutable permanecen sin espacios para conservar compatibilidad con Windows.

Se actualizaron el propietario, el repositorio, los enlaces de releases, soporte y ayuda, el nombre del producto de Windows, los accesos directos, la clave de desinstalación y el flujo de actualización. Las referencias históricas a `Bloxstrap` dentro de migraciones antiguas se conservaron deliberadamente para no romper la limpieza de instalaciones existentes de Bloxstrap. También se mantuvieron las atribuciones y licencias de código derivado y dependencias de terceros.

### Diseño

Se sustituyó el branding visual violeta heredado por un símbolo geométrico monocromático propio, logotipos en versiones oscura y clara, e iconos `.ico` para la aplicación y los estilos del bootstrapper. Se añadió una paleta de acento global en grises neutros sobre el tema oscuro de WPF UI para evitar que los controles adopten automáticamente el color de acento del sistema.

### Rendimiento y privacidad

Se añadió un **Performance preset** visible y reversible en la página de FastFlags. El preset utiliza valores conservadores para reducir el uso de GPU y VRAM mediante una calidad de texturas moderada y MSAA 1x. El usuario puede desactivarlo, restablecer la configuración y modificar los valores desde el editor existente.

La analítica quedó desactivada por defecto. Además, el fork no envía logs ni métricas a los servicios del upstream: el endpoint remoto de métricas se dejó neutralizado hasta que exista una infraestructura propia y documentada.

### Integraciones y limpieza

Se retiraron los scripts auxiliares de traducción `.py` y los artefactos de pruebas del árbol principal. La página de supporters se mantiene operativa con un estado vacío local, sin intentar descargar configuración desde un servicio externo inexistente. Los recursos opcionales de emojis conservan su URL real de terceros para no romper esa función.

El workflow de WinGet heredado se eliminó porque utilizaba un identificador de paquete que no corresponde a Synergy Strapper. El workflow de release se rehízo para compilar en Windows, publicar un ejecutable único `win-x64`, subir el artefacto y crear releases de GitHub sin depender de credenciales de firma heredadas.

### Verificación

La solución fue restaurada y compilada con el SDK .NET 6.0.428 y `EnableWindowsTargeting=true`: compilación Release completada con **0 errores**. También se generó una publicación autocontenida `win-x64` como ejecutable único. La validación en el entorno disponible confirma que el archivo es un ejecutable PE32+ para Windows; la ejecución real de la interfaz y de Roblox debe verificarse en Windows, ya que el entorno de compilación no puede iniciar una aplicación WPF ni el cliente Roblox.
