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
