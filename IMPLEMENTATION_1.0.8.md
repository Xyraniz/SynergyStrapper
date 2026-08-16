# SynergyStrapper 1.0.8 — implementación

## Alcance confirmado

La versión 1.0.8 implementa todos los puntos solicitados: 4.1, 4.3, 4.4, 4.5, 4.6, 4.7, 4.8, 4.9, 4.10, 4.11, 4.12, 4.13, 4.14, 4.15, 4.16, 4.17, 4.18, 4.19 y 4.22.

## Diseño común

Se añadirá `FeatureSettings` como sección persistida dentro de `Models/Persistable/Settings.cs`, con valores seguros por defecto, y `FeatureManager`/servicios especializados bajo `Integrations` y `Utility`. La UI será una página `FeaturesPage` con un `FeaturesViewModel`, organizada por mantenimiento, personalización, FastFlags, Studio, rendimiento y actividad. Las operaciones destructivas tendrán backup, validación, confirmación y rollback.

| Punto | Implementación | Criterio de aceptación |
| --- | --- | --- |
| 4.1 | `MultiInstanceWatcher` basado en mutex nombrado, política opt-in, detección de procesos y estado en bandeja. | Puede habilitarse/deshabilitarse; no altera el comportamiento por defecto; los handles se liberan al cerrar; el estado queda registrado. |
| 4.3 | Modo `StaticDirectory` opcional con manifest y copia segura desde el despliegue versionado. | La ruta se valida dentro de un directorio elegido; nunca se copia sobre una ruta arbitraria; puede volver al modo versionado. |
| 4.4 | `CleanerPolicy` configurable por categoría, edad y límite, con preview y contadores. | Protege reparse points, log activo, Windows y rutas fuera de raíz; preview no borra; limpieza real informa resultados. |
| 4.5 | Memory trimmer opt-in por umbral MB e intervalo, pausado sin Roblox activo, usando API nativa disponible. | No arranca por defecto; valida rango; registra procesos afectados y errores; se detiene al cancelar. |
| 4.6 | Terminación opcional y restaurable de `RobloxCrashHandler.exe`. | Está desactivado por defecto; solo actúa durante la sesión si el usuario lo habilita; deja registro y explicación. |
| 4.7 | Preferencias de tray y ciclo de vida separando ocultar ventana, watcher y cierre. | Cerrar/minimizar la ventana no mata el watcher ni RPC cuando la opción está habilitada. |
| 4.8 | `appStorage.json` con backup, esquema tolerante, opciones de tema/tray/autostart/visibilidad y restauración. | Nunca sobrescribe sin copia; un JSON inválido o esquema incompatible se restaura desde backup. |
| 4.9 | Sonido de muerte gestionado, validado y reversible. | Se aceptan únicamente formatos/tamaños razonables; se guarda en Modifications; restore elimina el override. |
| 4.10 | Cuatro ranuras de cursor independientes: Arrow, ArrowFar, IBeam y Shiftlock, con preview y estado. | Cada ranura se valida y aplica de forma independiente; presets no destruyen archivos personalizados; conflictos visibles. |
| 4.11 | ZIP de cursor set con manifest, importación segura y exportación. | Solo extrae nombres permitidos, bloquea traversal, valida PNG y permite rollback de importación. |
| 4.12 | Iconos Player/Studio independientes, gestión de archivos y uso en accesos directos. | Cada acceso directo puede usar icono propio; si no hay icono personalizado se conserva el predeterminado. |
| 4.13 | Catálogo de disponibilidad de FastFlags versionado, hash/fecha, estados y backup antes de aplicar. | Cada flag queda como Disponible/No disponible/Desconocida/No verificada; nunca se borra silenciosamente. |
| 4.14 | Tipado nativo (`bool`, `long`, `double`, `string`) preservado en import/export y perfiles. | JSON exportado conserva tipos; strings explícitas no se convierten; perfiles antiguos siguen cargando. |
| 4.15 | Guardia contra channel pins residuales con auditoría, valor detectado y excepción explícita. | Producción no hereda pin residual; canales elegidos explícitamente no se limpian; se puede revisar el informe. |
| 4.16 | Studio-first Mod Pack Manager con perfiles, manifest, apply incremental, DELETE confirmado y restore. | Player y Studio quedan separados; cada operación crea backup; archivos DELETE son reversibles. |
| 4.17 | Perfiles de rendimiento con flags, estado de cada flag y diff reversible. | Calidad/Equilibrado/Bajo consumo/Baja latencia/Compatibilidad son seleccionables; undo solo revierte lo aplicado. |
| 4.18 | Control FPS con validación, límites razonables y flag documentada. | Permite Auto/30/60/120/144/240/360; informa que Roblox/hardware pueden ignorar o limitar el valor. |
| 4.19 | Overlay informativo opcional sin inyección, con ventana click-through, ping/region/clock/dimmer. | Está desactivado por defecto; no modifica Roblox ni inyecta; se cierra con el watcher y respeta monitores/DPI razonablemente. |
| 4.22 | Historial ampliado con tiempo total aproximado, región y rejoin directo. | La estructura antigua migra sin pérdida; la UI muestra métricas y permite rejoin desde el registro. |

## Validación previa a publicación

Se ejecutarán validaciones estáticas locales, restauración/build/publicación mediante el workflow Windows del repositorio, revisión del artefacto `SynergyStrapper.exe`, checksum y smoke tests del ejecutable. El tag `v1.0.8` y el release se crearán únicamente después de que el build de Windows termine correctamente.
