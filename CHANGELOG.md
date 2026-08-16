# Changelog

## [1.1.0] — Synergy Strapper

### Correcciones y estabilidad

Se corrigió el cierre de la aplicación al abrir **Mods** cuando faltaban recursos embebidos de los cursores competitivos. La carga de recursos ahora informa los faltantes de forma segura y se reparó el comando de configuración de compatibilidad de esa página.

### Versiones e interfaz

La versión del producto quedó centralizada en una única variable de compilación. Se eliminó la página obsoleta de Features asociada a 1.0.8 y se retiró su referencia de la interfaz.

## [1.0.9] — Synergy Strapper

### Estabilidad y Mods

Se corrigió la excepción `TargetInvocationException` y `InvalidOperationException` (Sequence contains no matching element) que cerraba el bootstrapper al abrir la pestaña de modificadores (`ModsPage`). Se añadió una comprobación de seguridad en `MarkdownTextBlock` para evitar fallos al procesar bloques de texto nulos o vacíos.

### Interfaz y Configuración

Se eliminó la pestaña de información innecesaria de la versión 1.0.8 ("Synergy 1.0.8 Features") del panel de configuración y se reordenaron las pestañas de configuración para ofrecer una navegación más limpia y lógica.

## [1.0.8] — Synergy Strapper

### Deployment y ciclo de vida

Se añadió un sistema de **multiinstancia controlada** con política opt-in, watcher dedicado y guardas para mantener las actualizaciones serializadas. Se incorporó un **directorio estático opcional** con manifest de versión y validación de rutas, además del modo versionado existente. Las preferencias de bandeja separan minimizar u ocultar la ventana de mantener activos el watcher, Discord Rich Presence y las integraciones.

Se añadió una guardia contra **channel pins residuales**. Cuando producción está seleccionado y no existe una elección explícita de canal alternativo, Synergy elimina el pin heredado del registro y deja constancia en el log para evitar que una instalación termine en un canal de pruebas inesperado.

### Mantenimiento y recuperación

El limpiador ahora permite elegir categorías de logs y cache, antigüedad y máximo de archivos por directorio. Incluye modo de previsualización, contadores de candidatos, eliminados, fallidos y omitidos, y conserva las protecciones contra reparse points, traversal, Windows y el log activo.

Se añadió un **memory trimmer** opcional con umbral e intervalo, pausado cuando Roblox no está activo, además de una opción avanzada para cerrar `RobloxCrashHandler.exe`. Ambas funciones están desactivadas por defecto y registran sus acciones sin prometer una reducción garantizada de uso real de memoria o una mejora de rendimiento.

### Roblox App y personalización

Se añadió gestión respaldada de `appStorage.json` para tema, bandeja, inicio automático y visibilidad de detalles de versión o producción. Los cambios se validan como JSON, crean backup y restauran el archivo original si la actualización falla.

La personalización de mods incluye **sonido de muerte** reversible, cuatro ranuras de cursor independientes —`Arrow`, `ArrowFar`, `IBeam` y `Shiftlock`—, previews gestionados, importación y exportación de cursor sets ZIP con manifest y validación contra traversal. Player y Studio pueden usar iconos ICO independientes en sus accesos directos sin modificar el icono predeterminado si no existe un asset personalizado.

### FastFlags y rendimiento

FastFlag Editor conserva los tipos nativos de JSON para booleanos, números y cadenas, y los backups antiguos siguen siendo compatibles. Se incorporó una comprobación de disponibilidad frente a una allowlist versionada con revisión, fecha, hash SHA-256 y estados **Disponible**, **No disponible**, **Desconocida** y **No verificada**. Las flags no disponibles se reportan, pero no se eliminan silenciosamente.

Se añadieron perfiles de rendimiento **Quality**, **Balanced**, **LowPower**, **LowLatency** y **Compatibility**, con diff y backup antes de aplicar. También se incorporó un control de límite FPS con valores Auto, 30, 60, 120, 144, 240 y 360, dejando claro que Roblox, el renderer, la experiencia y el hardware pueden ignorar o limitar el valor.

### Studio y actividad

Se añadió una página Studio-first de **mod packs** con perfiles separados, `manifest.json`, aplicación incremental, archivos `DELETE ` con backup y restauración. Player y Studio mantienen sus modificaciones separadas.

El historial de partidas conserva datos antiguos y añade región, duración de la última sesión, tiempo total aproximado, contador de partidas y rejoin directo. También se incorporó un overlay informativo opcional, click-through y sin inyección, con reloj, región, ping y dimmer. Permanece desactivado por defecto y se cierra con el watcher.

## [1.0.7] — Synergy Strapper

### Branding y calidad visual

Se corrigieron los logos mostrados en la ventana principal, el menú de lanzamiento y las ventanas auxiliares. Las interfaces WPF cargan ahora el PNG transparente de alta resolución y los iconos nativos incluyen tamaños múltiples para evitar fondos opacos y pérdida de nitidez.

## [1.0.6] — Synergy Strapper

### FastFlag Health Check

Se añadió un chequeo integrado en FastFlag Editor que valida nombres, tipos, filtros y valores problemáticos. El chequeo muestra errores y advertencias, además de un diff de las modificaciones pendientes desde el último guardado, sin aplicar cambios automáticamente.

### Cursores

Se añadió una previsualización de los cursores incluidos y de los cursores personalizados. Los PNG personalizados se validan y se copian a una ubicación administrada por Synergy Strapper para que el preset no dependa de que el archivo original permanezca en una ruta externa.

## [1.0.5] — Synergy Strapper

### Navegación y organización

Se reorganizó la ventana de configuración en las secciones **Core**, **Configuration** y **Tools**, con encabezados y separadores visuales. Se añadió **FastFlag Editor** como una pestaña dedicada y visible junto a **FastFlags**. La ventana recuerda la última página seleccionada y ofrece búsqueda global con resaltado de coincidencias.

### FastFlag Editor

Se amplió el editor con eliminación total protegida por confirmación, guardado de JSON a archivo, contador de flags, etiquetas por tipo, indicador de flags asociadas a presets e historial de cambios de la sesión. La edición inline valida el nombre completo antes del renombrado y las operaciones actualizan las estadísticas.

### Ayuda y web

Se sustituyeron los destinos antiguos de GitHub Wiki por la wiki oficial alojada en la aplicación y plantillas enlazadas. Los avisos de la página web emplean marcas geométricas dibujadas con CSS en lugar de símbolos de texto y se eliminó el emoji de verificación de Discord.

## [1.0.4] — Synergy Strapper

### Correcciones críticas

Se corrigió el fallo que cerraba la aplicación con `TargetInvocationException` y `NullReferenceException` al abrir **Server Browser**. Los elementos `ComboBox` de orden y límite ya no disparan `SelectionChanged` durante `InitializeComponent`. Se endureció Server Browser frente a configuraciones antiguas, Place IDs inválidos y errores de rate limit. Se corrigieron los bindings de los selectores `ComboBox` para que enlacen correctamente mediante `SelectedItem`.

### Mods y cursores

Se corrigió el binding y la aplicación de los cursores **From 2006** y **From 2013**, aplicando correctamente `ArrowCursor.png` y `ArrowFarCursor.png` a las rutas de Roblox. Se mejoró la tarea genérica de presets enum y booleanos para reparar estados parcialmente aplicados y proteger archivos externos.

### Configuración y estabilidad

Se corrigió `FastFlagManager.SetValue` para comparar correctamente los valores anteriores y nuevos, evitando modificaciones concurrentes del diccionario. `JsonManager` limpia correctamente el hash y el objeto en memoria cuando un archivo no existe.

## [1.0.3] — Synergy Strapper

### Experiencia de usuario y configuración

Se incorporó un editor de **Global Roblox Settings** para las preferencias guardadas en `GlobalBasicSettings_13.xml`. Se añadió un gestor de canales de despliegue para Player y Studio, un navegador de servidores públicos mediante Place ID y un registro local de hasta 50 partidas recientes.

### FastFlags y migración

El editor de FastFlags admite perfiles nombrados guardados en `SavedBackups`. El instalador incorpora migración opcional desde instalaciones detectadas de Bloxstrap, Fishstrap o Voidstrap.

## [1.0.2] — Synergy Strapper

### Mantenimiento y experiencia de usuario

Se añadió una herramienta manual para limpiar archivos antiguos de logs y descargas de Synergy Strapper y Roblox con más de 30 días de antigüedad, protegiendo el log activo y limitando las operaciones por seguridad.

## [1.0.1] — Synergy Strapper

### Actualización y distribución

Se reforzó el autoactualizador para consultar releases publicadas, validar descargas por tamaño y SHA-256 y reemplazar el ejecutable de forma atómica antes del relanzamiento.

## [1.0.0] — Synergy Strapper

### Identidad y distribución

Se integró el código base original adaptando namespaces, recursos y configuraciones bajo la identidad de **Synergy Strapper**. Se implementó un branding visual monocromático propio con logotipos oscuros y claros, un preset de rendimiento configurable y la analítica desactivada por defecto.
