# Changelog

## [1.0.4] — Synergy Strapper

### Correcciones críticas

Se corrigió el fallo que podía cerrar la aplicación con `TargetInvocationException` y `NullReferenceException` al abrir **Server Browser**. Los elementos `ComboBox` de orden y límite disparaban `SelectionChanged` durante `InitializeComponent`, antes de que `ServersGrid` existiera; ahora la actualización de la tabla es segura durante la construcción de la página. También se reorganizaron las filas del XAML para separar descripción, controles, tabla y acciones sin superposición.

Se endureció Server Browser frente a configuraciones antiguas con `LastServerPlaceId` nulo, respuestas JSON con servidores incompletos, Place IDs inválidos, cambios rápidos de texto, peticiones canceladas y errores de rate limit. Las peticiones ya no se lanzan con cada carácter escrito; se espera a que el usuario termine de introducir el ID. Los deeplinks de unión escapan sus parámetros y la página cancela y libera sus tareas al cerrarse.

Se corrigieron los bindings de todos los selectores `ComboBox` de configuración que estaban enlazados mediante `Text` en vez de `SelectedItem`. Esto afectaba especialmente al cursor, pero también a emojis, MSAA, calidad de textura, tema, idioma y estilo del bootstrapper. Los cambios seleccionados ahora llegan realmente a sus propiedades y tareas pendientes.

### Mods, cursores y restauración

Se verificó el problema de los cursores. La causa no era una broma: el selector estaba usando un binding incorrecto y podía mostrar la opción sin cambiar `CursorTypeTask.NewState`, por lo que al guardar no se aplicaba ningún archivo. El selector conserva los dos presets existentes, **From 2006** y **From 2013**, y ahora aplica correctamente `ArrowCursor.png` y `ArrowFarCursor.png` a las rutas de Roblox correspondientes. Se verificaron los cuatro recursos embebidos: son PNG válidos de `80×80` o `64×64` con transparencia.

Se corrigió la tarea genérica de presets enum para no abrir dos streams por recurso, reparar presets parcialmente aplicados, eliminar únicamente archivos cuyo hash coincide con un recurso conocido y limpiar variantes anteriores al cambiar de preset. Esto evita falsos estados “activado” y protege archivos externos que el usuario haya colocado en la carpeta de modificaciones.

Se aplicó la misma lógica de reparación parcial a los presets booleanos de sonidos y avatar. Las fuentes personalizadas ahora se copian mediante un archivo temporal, validan que el archivo fuente siga existiendo y no marcan la tarea como completada si la copia no terminó. El preset de emojis dejó de ser `async void`, descarga de forma controlada, reemplaza atómicamente, limpia temporales y conserva la tarea pendiente cuando GitHub no está disponible.

### Configuración y estabilidad

Se corrigió `FastFlagManager.SetValue`, que comparaba una clave con su propio valor en vez de comparar el valor anterior con el nuevo. También se sustituyó la comparación sensible al orden de los diccionarios por una comparación semántica de claves y valores normalizados, y se evitó modificar el diccionario mientras se enumeraba.

Se hizo seguro el guardado de tareas pendientes mediante una instantánea. Si una tarea falla, por ejemplo al descargar una fuente de emojis, ya no se borra silenciosamente el cambio pendiente ni se informa al usuario de que todo se guardó cuando no fue así.

`JsonManager` ahora limpia el hash y el objeto en memoria cuando un archivo no existe o se elimina, y `HasFileOnDiskChanged` ya no intenta abrir archivos ausentes ni oculta errores de acceso. El watcher de Roblox dejó de usar `First()` sobre una carpeta de logs vacía, espera de forma cancelable a que aparezca un log válido, comprueba que el archivo exista y rechaza IDs malformados sin lanzar excepciones.

### Verificación

La solución fue restaurada y compilada con el SDK exacto `.NET 6.0.428`, con el submódulo Wpf.Ui inicializado y `EnableWindowsTargeting=true`. La compilación Release local terminó con **0 errores y 0 advertencias**. También se ejecutaron comprobaciones estáticas de bindings XAML, recursos de cursor, formato de archivos, diff y estructura de la solución. La compilación Windows del workflow de GitHub se mantiene como validación adicional antes de publicar el ejecutable autocontenido.

## [1.0.3] — Synergy Strapper

### Experiencia de usuario y configuración

Se incorporó un editor de **Global Roblox Settings** para las preferencias que Roblox guarda en `GlobalBasicSettings_13.xml`. Permite revisar y modificar el límite de FPS, el nivel de calidad guardado, la sensibilidad del ratón, el tamaño de texto, el movimiento reducido y la preferencia de VR. La escritura conserva el atributo de solo lectura del archivo y muestra un error claro si Roblox mantiene el archivo bloqueado; el usuario puede recargar los valores sin reiniciar Synergy Strapper.

Se añadió un gestor de **canales de despliegue** para Player y Studio. El usuario puede seleccionar producción u otro canal compatible, restaurar producción con un clic y guardar la preferencia de actualización. El canal se escribe en las claves de registro que el bootstrapper ya consulta, por lo que no es una opción visual aislada. Los nombres se validan antes de tocar el registro y las opciones de prueba muestran una advertencia de estabilidad.

Se añadió un **navegador de servidores públicos**. El usuario introduce un Place ID, consulta servidores públicos, ordena por menor ping, menor ocupación o mayor ocupación, limita los resultados visibles y se une mediante un deeplink `roblox://`. La vista incluye copia del ID de servidor, doble clic para unirse, cancelación de peticiones y espera progresiva cuando Roblox responde con rate limit. No usa cookies, tokens ni credenciales de la cuenta.

El historial de partidas dejó de depender únicamente de la sesión actual. Ahora se registran localmente hasta 50 partidas terminadas, con nombre del juego cuando Roblox lo proporciona, Place ID, servidor, fecha y número de veces jugado. La nueva página **Recent Games** permite relanzar una entrada, abrir el mismo servidor cuando continúa disponible y borrar todo el historial. El registro no guarda credenciales, cookies ni tokens.

### FastFlags, migración y mantenimiento

El editor de FastFlags ahora admite **perfiles nombrados**. Cada perfil se guarda como un JSON independiente dentro de `SavedBackups`; el usuario puede guardar la configuración actual, cargarla reemplazando toda la lista o fusionándola con los flags existentes, y eliminar perfiles. Los nombres se sanitizan para impedir rutas arbitrarias y la operación se registra en el log de la aplicación.

El instalador incorpora migración opcional desde instalaciones detectadas de **Bloxstrap, Fishstrap o Voidstrap**. Copia configuraciones compatibles, temas y modificaciones sin borrar el origen y muestra una advertencia si no encuentra una instalación previa. La migración está aislada por archivo para que un problema con una fuente antigua no cancele toda la instalación de Synergy Strapper.

Se eliminaron de `Images/` los tres banners obsoletos de Bloxstrap que ya no se utilizan. Las referencias históricas a Bloxstrap dentro de la migración, la atribución MIT y la limpieza de instalaciones antiguas se conservaron deliberadamente porque siguen siendo necesarias para compatibilidad y licencia.

### Recursos visuales y distribución

Se mejoraron los cuatro recursos del botón **Cancel** usados por el diálogo clásico de lanzamiento —estado claro, hover claro, estado oscuro y hover oscuro— de `130×44` a `520×176` píxeles con escalado de alta calidad. Se conserva la proporción, transparencia y función de cada estado para que el botón sea más nítido en escalado DPI alto.

La versión de ensamblado, archivo y proyecto pasó a `1.0.3`. El workflow de release se actualizó para que las releases normales y los lanzamientos manuales publiquen un ejecutable `win-x64` single-file **autocontenido**, incluyendo `IncludeNativeLibrariesForSelfExtract`, además del checksum SHA-256. De este modo, la release v1.0.3 se compila desde el código fuente modificado en un runner Windows y no reutiliza el binario de la v1.0.2.

### Verificación

La solución fue restaurada y compilada con el SDK .NET 6.0.428 y `EnableWindowsTargeting=true` después de aplicar todos los cambios. La compilación Release local terminó con **0 errores y 0 advertencias**. La publicación final del ejecutable autocontenido se valida adicionalmente en el workflow Windows antes de crear o actualizar la release de GitHub.

## [1.0.2] — Synergy Strapper

### Mantenimiento y experiencia de usuario

Se añadió una herramienta manual para limpiar archivos antiguos desde la página de configuración. La acción elimina archivos con más de 30 días en las carpetas conocidas de logs y descargas de Synergy Strapper, los logs de Roblox y la caché temporal de Roblox. La operación se ejecuta fuera del hilo de la interfaz para que la ventana no se congele mientras revisa los archivos.

La limpieza conserva siempre el log activo, ignora archivos de sistema y puntos de reanálisis, comprueba que cada archivo esté dentro de una raíz permitida y limita la eliminación a 200 archivos por carpeta. Al finalizar muestra cuántos archivos se eliminaron, cuáles no pudieron eliminarse y cuáles se omitieron por ser recientes o no cumplir los límites de seguridad. Se añadieron textos localizados para español y se documentó el alcance en el README.

### Versión y distribución

Se actualizó la versión del proyecto y del ejecutable a `1.0.2`. La release mantiene el contrato de distribución existente: el workflow compila desde el código fuente en Windows y publica únicamente `SynergyStrapper.exe` y su checksum SHA-256 como assets de GitHub Release.

## [1.0.1] — Synergy Strapper

### Actualización y distribución

Se reforzó el autoactualizador para consultar releases publicadas, seleccionar exclusivamente `SynergyStrapper.exe`, aplicar un límite de tiempo al chequeo inicial y conservar la instalación actual cuando la red o el release no son válidos. Las descargas se escriben en un archivo temporal, se validan por tamaño y SHA-256 y se reemplazan de forma atómica antes del relanzamiento.

El workflow de release compila desde el código fuente en Windows y publica el ejecutable junto con su checksum como asset de GitHub Release. También puede regenerar los assets de una release existente mediante `workflow_dispatch`, por lo que la distribución no depende de mantener un `.exe` en la raíz del repositorio. Las releases generadas desde tags `vX.Y.Z` conservan el mismo contrato de assets que espera el cliente.

### Interfaz y compatibilidad

La página de configuración muestra la versión instalada, el estado de actualización y una acción manual para comprobar releases. El perfil de rendimiento sigue siendo conservador, reversible y orientado a reducir trabajo de GPU/VRAM sin prometer FPS concretos ni alterar protecciones del cliente.

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

La solución fue restaurada y compilada con el SDK .NET 6.0.428 y `EnableWindowsTargeting=true`: compilación Release completada con **0 errores**. El workflow `CI (Release)` de GitHub Actions también terminó correctamente en un runner Windows para el commit `6018cfb`. El ejecutable final de la raíz es un PE32+ GUI `win-x64`, generado desde Windows, de 11.4 MB; SHA-256: `66f1760b5ebde60e11a825e60e7711a69cb9f808119216882d23f22e44ab1f90`. La ejecución real de la interfaz y de Roblox todavía debe verificarse en Windows, ya que el entorno de análisis no puede iniciar una aplicación WPF ni el cliente Roblox.
