# FileEncodingConverter
Convierte todos los archivos de una carpeta y sus subcarpetas a una codificación determinada.

### v1.5.0 - Septiembre de 2016

- Añadida opción para generar UTF-8 con o sin BOM.
- OJO: ahora la codificación "UTF8" se refiere especificamente a **UTF-8 sin BOM**. Si la quieres con BOM debes utilizar "UTF8BOM" como codificación de destino.

## Introducción
Hace unos años creé una aplicación, pequeña pero muy útil, llamada **FileEncoding Converter**, cuyo código tienes en este repositorio.

Su objetivo es sencillo: le pasas una carpeta, y opcionalmente una codificación y un filtro de archivos y convierte todos los archivos a la codificación especificada.

Se trata de una **aplicación de consola** (línea de comandos) que toma los siguientes parámetros:

1.- **La ruta base** que contiene nuestros archivos a procesar. Acepta rutas relativas a la carpeta actual y rutas absolutas.

2.- **El tipo de codificación** al que queremos transformarlo (opcional).

Los tipos de codificación soportados son los siguientes:

- ANSI
- ASCII
- Unicode
- UnicodeBI (Big Indian)
- UTF32
- UTF7
- UTF8
- UTF8BOM (le añade [el BOM](https://es.wikipedia.org/wiki/Marca_de_orden_de_bytes) a la codificación UTF-8).

Este parámetro no distingue entre mayúsculas y minúsculas, por lo que podemos poner cualquiera de estos valores como queramos. Si omitimos este parámetro o usamos un valor no soportado la codificación que se usará por defecto será ANSI (la que tiene por defecto el sistema).

3.- El tercer parámetro (opcional) permite especificar **una o varias plantillas de nombres de archivo a buscar** para procesar. Permite el uso de comodines. Si no le pones nada buscará solamente archivos de texto y HTML (.htm o .html), pero puedes especificar, separados por comas, qué tipos de archivos quieres transformar. 

Si se quiere especificar este tercer parámetro es obligatorio especificar el segundo ya que se adquieren por posición en el orden especificado en este documento.


## Modificadores
Se pueden utilizar dos modificadores diferentes, en cualquier posición, que modifican el comportamiento por defecto del programa.

Se distingue entre mayúsculas y minúsculas, así que hay que especificarlos tal cual se indican aquí.

Son los siguientes:

#### “/f” o "-f": FORZAR CONVERSIÓN

Sirve para forzar la conversión siempre. Esto es muy útil cuando, por ejemplo, tienes archivos en formato UTF8 sin BOM. Si los conviertes también a UTF8, al detectar que ya están en UTF8 no los codifica. Forzando la conversión los grabará de nuevo como UTF8 pero esta vez poniéndoles el BOM, lo cual puede ser muy útil.

#### "/b" o "-b": MODO BATCH
El programa, por defecto, se detiene al terminar el procesamiento de los archivos esperando a que el usuario pulse alguna tecla. De este modo podemos ver qué archivos se han procesado y no se nos cierra la consola en caso de que lo hayamos ejecutado fuera de ésta. Si especificamos "/b" o "-b" forzaremos el modo "batch", en el que el programa no se detiene al final. Es útil para usar el programa en un proceso de tipo ".bat" en el que se ejecutan muchos otros comandos y así no se detiene la ejecución.

#### Ayuda
Para ver una ayuda rápida sobre su uso basta con poner /? o -? o ejecutarlo sin parámetro alguno.

## Codificación UTF-8 y preámbulos

Este es un dato importante...

Los archivos codificados según alguno de los tipos anteriores generalmente llevan delante una marca de tres bytes llamada **[preámbulo o BOM (Byte Order Mark)](https://es.wikipedia.org/wiki/Marca_de_orden_de_bytes)**. Aunque no es obligatorio en el caso de UTF-8, sí es muy útil puesto que nos indica de modo inequívoco de qué forma está codificado un archivo. Cuando se usa en un entorno cerrado (donde ya conocemos cuál es la codificación que se usa siempre) no hace falta, pero en el intercambio de archivos **en mi opinión debería usarse siempre**^* 

La cuestión es que en Windows muchos editores de texto le ponen el BOM a los archivos. Pero en Mac y Linux es al contrario y no se lo suelen poner. Por regla general si no llevan el BOM están codificados en ANSI o en UTF8. Lo malo es que la única forma de saberlo si no llevan el BOM es **utilizando un método heurístico** que consiste en tratar de identificar ciertas secuencias de bytes dentro del archivo, que te indicarán la presencia de la codificación con un alto grado de probabilidad.

Esta versión del conversor identifica los archivos UTF8 sin BOM usando este método heurístico, por lo que es capaz de trabajar con la mayoría de archivos que te puedas encontrar por ahí. Es importante saberlo.

A raíz de esta capacidad he incluido también **la opción de forzar la reconversión** que mencionaba antes (/f) para forzar la inclusión del BOM en los archivos UTF-8 (indicando la codificación "UTF8BOM" como codificación final) y facilitar su intercambio si lo necesitamos.

>^* Por ejemplo, el sencillo archivo ANSI.txt de la carpeta `Test` del repositorio de esta aplicación, es un archivo codificado como ANSI y solo tiene caracteres acentuados y eñes. Si intentas abrirlo, muchos editores no son capaces de identificar correctamente la codificación. Notepad++, por ejemplo, lo identifica como texto hebreo y no se ve cómo debiera. Brackets te dice que no es capaz de abrirlo aunque tengas la extensión de codificaciones instalada.... Curiosamente el bloc de notas de Windows lo identifica bien (probablemente porque considera que por defecto los archivos son ANSI), y también Visual Studio. Pero el algoritmo heurístico habitual para detectar UTF-8 (el utilizado en FileEncodingCovnerter) no lo reconoce correctamente y, de hecho, la codificación a UTF-8 falla en el caso concreto de dicho archivo. Es un caso poco frecuente y por regla general no hay problema para reconocer este tipo de archivos, pero por eso mismo opino que el BOM debería utilizarse siempre a pesar de que muchos digan que no es necesario porque se puede detctar con una gran probabilidad en la mayoría de los casos. Ese archivo es un buen contra-ejemplo de ello. Y si con el BOM siempre se detecta bien ¿para qué arriesgarse?.

## Ejemplos de uso

Lo que hace la utilidad es recorrer la carpeta base especificada y todas sus subcarpetas y transforma todos los archivos indicados a la codificación de destino especificada (por defecto ANSI).

Muestra un progreso de los archivos que va transformando, y al final muestra un resumen de lo que ha hecho.

Así, por ejemplo, para transformar todos los archivos con extensiones .htm, .html y .txt de una carpeta y sus subcarpetas de su codificación actual a Unicode Big Indian escribiríamos:

```
FileEncodingConverter C:\Micarpeta UnicodeBI`
```

o para convertir todos a ANSI valdría con poner simplemente (con todas las opciones por defecto):

```
FileEncodingConverter C:\Micarpeta
```

Para forzar la conversión (o re-conversión) a UTF-8 de todos los archivos XML cuyo nombre contenga las letras 'ES', además de todos los de texto así como los HTM (tanto .htm como .html), podrías escribir:

```
FileEncodingConverter C:\MisArchivosDedatos UTF8 *ES*.xml,*.txt,*.htm* /f
```

o

```
FileEncodingConverter C:\MisArchivosDedatos UTF8BOM *ES*.xml,*.txt,*.htm* /f
```
si queremos que la conversión les incluya el BOM inicial a los archivos como indicador de que son UTF-8.

## Información adicional
Este programa está escrito en C# usando la plataforma .NET, y he utilizado Visual Studio Community Edition.

Se incluye el archivo .sln de Visual Studio para facilitar su edición y compilación.

Lo he atado a la versión 2.0 de .NET para que sea mas fácil ejecutarlo en cualquier lugar.

Licencia [Apache 2.0](http://www.apache.org/licenses/LICENSE-2.0).