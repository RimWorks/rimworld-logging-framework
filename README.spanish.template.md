# RimLogging: visor de logs y envío de reportes para RimWorld

Lee el log de RimWorld dentro del juego. Fíltralo hasta el error rojo que de verdad te importa y compártelo como enlace con un clic. RimLogging también es un framework de logging estructurado para autores de mods. Sustituye `Verse.Log` y `UnityEngine.Debug.Log` por una única tubería que comparten todos los mods.

## Cómo compartir tu log de RimWorld

Abre el visor, pulsa el botón de compartir y RimLogging sube un paquete y copia un enlace a tu portapapeles. Pégalo en Discord o en una incidencia de GitHub. El paquete lleva tu lista de mods, la versión del juego y las entradas capturadas. RimLogging lo recorta para que quepa en el límite de subida, así que una sesión larga también pasa.

## ¿Qué falla en la ventana de log del juego base?

Es una lista plana, se trunca y no hay forma de esconder el ruido. El error rojo del mod que estás persiguiendo está entre cuatro mil líneas que no necesitas.

RimLogging la sustituye por un visor de tres paneles: un árbol de canales a la izquierda, conmutadores de nivel en vivo y un DSL de filtros por expresión. El panel de detalle se acopla abajo, se acopla a la derecha o sale en su propia ventana.

Como intercepta `Verse.Log`, captura la salida de mods que nunca se apuntaron. No se escapa nada.

## ¿En qué se diferencia del publish log de HugsLib?

HugsLib sube el fichero de log entero como un muro de texto y te da un enlace. Funciona, y luego te toca desplazarte.

RimLogging te deja acotar antes. Elige el canal de un mod, quita todo por debajo de `Warn`, escribe una expresión de filtro y mira lo que queda. Eso es lo que envías. Quien lea tu reporte recibe el log y la lista de mods sin tener que buscar en un fichero la única línea que importa.

## Para jugadores

Instálalo como dependencia compartida. Si estás suscrito a un mod que requiere RimLogging, esto es lo que se instala. Captura y organiza la salida del log para que los reportes de fallos salgan limpios y fáciles de compartir.

## Para autores de mods

- Canales jerárquicos (defs XML o transitorios) con resolución por prefijo.
- Mensajes con plantilla al estilo Serilog, con marcadores con nombre y argumentos posicionales.
- Contexto estructurado por objeto anónimo adjunto a cualquier entrada.
- Seis niveles: `Trace`, `Debug`, `Info`, `Warn`, `Error`, `Fatal`.
- Salida a múltiples destinos: escritura al log de Verse, fichero de texto rotativo, fichero NDJSON rotativo, memoria, más una API de destinos por plugin.
- Cola MPSC sin bloqueos con drenaje en segundo plano. Paso síncrono directo para `Error` y `Fatal`.
- Un analizador de Roslyn que marca las llamadas a `Verse.Log` que queden, en tiempo de compilación, con corrección automática.

Añádelo a tu proyecto desde NuGet:

```
dotnet add package RimWorks.RimLogging
```

Después declara el elemento del Workshop como dependencia en tu `About.xml` para que tus suscriptores reciban la DLL compartida automáticamente.

## Enlaces

- Código, documentación e incidencias: https://github.com/RimWorks/rimworld-logging-framework

## Más herramientas de modding de RimWorks

- [Pickle](https://steamcommunity.com/sharedfiles/filedetails/?id=3791648678): ejecuta pruebas Gherkin contra una sesión de RimWorld en marcha.
- [Quickstarts](https://steamcommunity.com/sharedfiles/filedetails/?id=3793646067): entra directo en una colonia ya configurada desde el menú de quicktest.

Licencia MIT.
