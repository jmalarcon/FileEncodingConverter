/*
     _   _    ____         __ _                    
    | | / \  / ___|  ___  / _| |_   ___  _ __ __ _ 
 _  | |/ _ \ \___ \ / _ \| |_| __| / _ \| '__/ _` |
| |_| / ___ \ ___) | (_) |  _| |_ | (_) | | | (_| |
 \___/_/   \_|____/ \___/|_|  \__(_\___/|_|  \__, |
                                             |___/ 
	FILEENCODINGCONVERTER V1.4.1
    por José Manuel Alarcón [www.jasoft.org]
	
	Aplicación de consola con un sencillo objetivo: le pasas una carpeta, 
	y opcionalmente una codificación y un filtro de archivos y 
	convierte todos los archivos a la codificación especificada.
	Detecta el tipo de codificación de los archivos aunque no tengan BOM.
*/
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace FileEncodingConverter
{
	class Program
	{
		private static int numArchivos = 0;
		private static int NumArchivoConvertidos = 0;
		private static int numCarpetas = 0;
		private static string[] tiposArchivo = { "*.txt", "*.htm"};

		private static EncodingInfo[] encodingsWithPreamble = null;

		static void Main(string[] args)
		{
			if (args.Length == 0 || (args[0] == "-?") || (args[0] == "/?"))
			{
				MuestraAyuda();
				return;
			}

			//Averiguamos la ruta de la carpeta
			string ruta = Path.GetFullPath(args[0]);
 
			//Comprobamos que existe
			if (!Directory.Exists(ruta))
			{
				Console.WriteLine("La carpeta \"{0}\" no existe.", ruta);
				return;
			}

			//Establecemos la codificación apropiada de destino
			string sCodif;
			Encoding codifDest;

			if (args.Length > 1 && !EsModificador(args[1]))
			{
				sCodif = args[1].ToLower();
				switch (sCodif)
				{
					case "ansi":
						codifDest = Encoding.Default;
						break;
					case "ascii":
						codifDest = Encoding.ASCII;
						break;
					case "unicode":
						codifDest = Encoding.Unicode;
						break;
					case "unicodebi":
						codifDest = Encoding.BigEndianUnicode;
						break;
					case "utf32":
						codifDest = Encoding.UTF32;
						break;
					case "utf7":
						codifDest = Encoding.UTF7;
						break;
					case "utf8":
						codifDest = Encoding.UTF8;
						break;
					default:
						//No se especifica una correcta: se usa la codif por defecto
						codifDest = Encoding.Default;
						Console.WriteLine("Utilizando la codificación por defecto...\n\n");
						return;
				}
			}
			else
			{
				//No se especifica: se usa la codif por defecto
				codifDest = Encoding.Default;
			}

			if (args.Length > 2 && !EsModificador(args[2]))
			{
				tiposArchivo = args[2].Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
			}

			//Averiguamos si se ha especificado el modo "forzar" o el modo "batch"
			bool batch = (Array.IndexOf(args, "/b") >= 0 || Array.IndexOf(args, "-b") >= 0);
			bool force = (Array.IndexOf(args, "/f") >= 0 || Array.IndexOf(args, "-f") >= 0);

			//El modo "forzar" (-f o /f) fuerza la recodificación aunque no sea necesaria a priori
			if(force)
			{
				//Comenzamos a procesar los archivos
				ProcesaCarpeta(ruta, codifDest, true);
			}
			else
			{
				//Comenzamos a procesar los archivos
				ProcesaCarpeta(ruta, codifDest);
			}

			Console.WriteLine("¡Terminado!. Se han procesado {0} archivos en {1} carpetas. Se han convertido {2} archivos.", numArchivos, numCarpetas, NumArchivoConvertidos);

			//Si no se indica lo contrario se detiene para asegurar que se ve el resultado por pantalla (y no se cierra la consola inmediatamente)
			//Si se le pone un parámetro "/b" o "-b" (de batch) entonces se termina el programa inmediatamente, para dejar que sigan ejecutándose otras instrucciones.
			if (!batch)
			{
				Console.ReadLine();
			}
			//}
		}

		#region auxiliares
		private static void MuestraAyuda()
		{
			Console.WriteLine("Convierte todos los archivos del tipo indicado en una carpeta y sus subcarpetas a la codificación especificada.");
			Console.WriteLine("\nUso: FileEncodingConverter carpeta_inicial [codificación] [tipos archivo separados por comas] [/f] [/b]");
			Console.WriteLine("\nEj: FileEncodingConverter c:\\Misdatos UnicodeBI *.txt,*.xml /f");
			Console.WriteLine("\nEj: FileEncodingConverter c:\\Misdatos UTF8 *.txt,*.xml /nf /b");
			Console.WriteLine("\nLa codificación se refiere a la codificación final a la que deseamos convertir todos los archivos.");
			Console.WriteLine("\nLos valores válidos son: ANSI, ASCII, Unicode, UnicodeBI (Big Indian), UTF32, UTF7, UTF8. Si se omite se codifica con ANSI, valor por defecto del sistema. Detecta también UTF8 sin BOM.");
			Console.WriteLine("\nSi no se especifican los tipos de archivo se usarán *.txt y *.htm.");
			Console.WriteLine("\nEl cuarto parámetro, /f, se utiliza para forzar la recodificación de los archivos. Esto hará que, por ejemplo, archivos codificados como UTF8 sin BOM se codifiquen como UTF8 de nuevo pero con BOM, que es lo que hace por defecto. Si no queremos usarlo podemos poner cualquier otro, por ejemplo /nf, para poder usar el siguiente.");
			Console.WriteLine("\nEl quinto parámetro, /b, se utiliza para indicar que estamos ejecutando en un 'batch' y que por lo tanto no debe detenerse la ejecución, permitiendo que sigan ejecutándose otros programas.");
			Console.WriteLine("\n(c) José M. Alarcón [www.jasoft.org]");
		}

		/// <summary>
		/// Indica si el argumento de línea de comandos que se le pasa es un modificador o no, es decir
		/// si empieza o no por "/" o "-"
		/// </summary>
		/// <param name="arg"></param>
		/// <returns></returns>
		private static bool EsModificador(string arg)
		{
			return (arg.StartsWith("/") || arg.StartsWith("-"));
		}

		/// <summary>
		/// Procesa la carpeta indicada en busca de archivos para procesar. Recorre recursivamente las subcarpetas.
		/// </summary>
		/// <param name="RutaCarpeta"></param>
		private static void ProcesaCarpeta(string RutaCarpeta, Encoding codifFinal, bool forzarRecodificacion = false)
		{
			numCarpetas++;
			DirectoryInfo di = new DirectoryInfo(RutaCarpeta);
			foreach (string t in tiposArchivo)
			{
				//Procesamos archivos de texto
				FileInfo[] archivos = di.GetFiles(t);
				foreach (FileInfo arch in archivos)
				{
					ConvertFile(arch.FullName, codifFinal, forzarRecodificacion);
				}
			}

			//Ahora recorremos los subdirectorios pertinentes
			DirectoryInfo[] carpetas = di.GetDirectories();
			foreach (DirectoryInfo carpeta in carpetas)
			{
				ProcesaCarpeta(carpeta.FullName, codifFinal, forzarRecodificacion);
			}
		}

		/// <summary>
		/// Lee el contenido de un archivo (usando la codificación especificada)
		/// y devuelve sus contenidos como texto.
		/// </summary>
		/// <param name="PhisicalPath"></param>
		/// <returns></returns>
		public static string ReadFile(string PhisicalPath, Encoding codif)
		{
			//Si no existe generamos una excepción
			if (!File.Exists(PhisicalPath))
				throw new FileNotFoundException();

			//Si existe leemos el archivo
			StreamReader sr = new StreamReader(PhisicalPath, codif);
			string res = sr.ReadToEnd();
			sr.Close();
			return res;
		}

		/// <summary>
		/// Guarda un contenido con la codificación especificada sobreescribiendo el archivo en caso de ser necesario
		/// </summary>
		/// <param name="contenido">Contenido del archivo</param>
		/// <param name="rutaArch">Ruta física del archivo</param>
		/// <param name="codifFinal">Codificación</param>
		private static void WriteFile(string contenido, string rutaArch, Encoding codifFinal)
		{
			//File.WriteAllText(rutaArch, contenido, codifFinal);
			StreamWriter sw = new StreamWriter(rutaArch, false, codifFinal);
			sw.Write(contenido);
			sw.Close();
		}

		/// <summary>
		/// Convierte el archivo indicado a la codificación indicada si no lo está ya
		/// </summary>
		/// <param name="file"></param>
		/// <param name="tipo"></param>
		private static void ConvertFile(string file, Encoding tipoDest, bool forzarRecodificacion = false)
		{
			numArchivos++;
			Encoding tipoAct = GetFileEncoding(file);
			//Sólo se codifica si se fuerza o si la codif original es diferente a la final
			if (forzarRecodificacion || !tipoAct.Equals(tipoDest))
			{
				Console.WriteLine("Convirtiendo {0}", file);
				string contenido = ReadFile(file, tipoAct);
				WriteFile(contenido, file, tipoDest);
				NumArchivoConvertidos++;
			}
		}

		/// <summary>
		/// Selecciona las codificaciones que se pueden detectar mediante preámbulos (porque meten un preámbulo en el archivo)
		/// Sin este preámbulo no podemos determinar el tipo de codificación.
		/// Lo hace sólo una vez, para el primer archivo. Luego ya lo reutiliza.
		/// </summary>
		/// <remarks>Sólo hay 5 en principio con el preámbulo en cuestión, por lo que se podrían meter a mano,
		/// pero de este modo es automático y si en el futuro se ampliase se reconocerían las nuevas también.</remarks>
		private static void PreFilterEncodingsWithPreamble()
		{
			//Si ya está precalculado no lo hacemos de nuevo
			if (encodingsWithPreamble != null)
				return;

			//Averiguo todas las codificaciones que soporta la plataforma
			EncodingInfo[] UnicodeEncodings = Encoding.GetEncodings();

			//Colección para albergar loas resultados
			List<EncodingInfo> codificaciones = new List<EncodingInfo>();

			foreach (EncodingInfo ei in UnicodeEncodings)
			{
				byte[] Preamble = ei.GetEncoding().GetPreamble();
				if (Preamble.Length > 0)
				{
					codificaciones.Add(ei);
				}
			}
			encodingsWithPreamble = codificaciones.ToArray();
		}

#region Detección heurística de UTF8

////////////////////////////////////////////////////////////////
//	OPEN SOURCE CODE
//	Based on the Utf8Checker class found here: http://utf8checker.codeplex.com/
////////////////////////////////////////////////////////////////
		private static bool checkUTFWithoutBOM(string PhysicalPath)
		{
			BufferedStream fstream = new BufferedStream(File.OpenRead(PhysicalPath));
			bool res = IsUtf8(fstream);
			fstream.Close();
			return res;
		}

		/// <summary>
		/// Check if stream is utf8 encoded.
		/// Notice: stream is read completely in memory!
		/// </summary>
		/// <param name="stream">Stream to read from.</param>
		/// <returns>True if the whole stream is utf8 encoded.</returns>
		private static bool IsUtf8(Stream stream)
		{
			int count = 4 * 1024;
			byte[] buffer;
			int read;
			while (true)
			{
				buffer = new byte[count];
				stream.Seek(0, SeekOrigin.Begin);
				read = stream.Read(buffer, 0, count);
				if (read < count)
				{
					break;
				}
				buffer = null;
				count *= 2;
			}
			return IsUtf8(buffer, read);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="length"></param>
		/// <returns></returns>
		private static bool IsUtf8(byte[] buffer, int length)
		{
			int position = 0;
			int bytes = 0;
			while (position < length)
			{
				if (!IsValid(buffer, position, length, ref bytes))
				{
					return false;
				}
				position += bytes;
			}
			return true;
		}

		private static bool IsValid(byte[] buffer, int position, int length, ref int bytes)
		{
			if (length > buffer.Length)
			{
				throw new ArgumentException("Invalid length");
			}

			if (position > length - 1)
			{
				bytes = 0;
				return true;
			}

			byte ch = buffer[position];

			if (ch <= 0x7F)
			{
				bytes = 1;
				return true;
			}

			if (ch >= 0xc2 && ch <= 0xdf)
			{
				if (position >= length - 2)
				{
					bytes = 0;
					return false;
				}
				if (buffer[position + 1] < 0x80 || buffer[position + 1] > 0xbf)
				{
					bytes = 0;
					return false;
				}
				bytes = 2;
				return true;
			}

			if (ch == 0xe0)
			{
				if (position >= length - 3)
				{
					bytes = 0;
					return false;
				}

				if (buffer[position + 1] < 0xa0 || buffer[position + 1] > 0xbf ||
					buffer[position + 2] < 0x80 || buffer[position + 2] > 0xbf)
				{
					bytes = 0;
					return false;
				}
				bytes = 3;
				return true;
			}


			if (ch >= 0xe1 && ch <= 0xef)
			{
				if (position >= length - 3)
				{
					bytes = 0;
					return false;
				}

				if (buffer[position + 1] < 0x80 || buffer[position + 1] > 0xbf ||
					buffer[position + 2] < 0x80 || buffer[position + 2] > 0xbf)
				{
					bytes = 0;
					return false;
				}

				bytes = 3;
				return true;
			}

			if (ch == 0xf0)
			{
				if (position >= length - 4)
				{
					bytes = 0;
					return false;
				}

				if (buffer[position + 1] < 0x90 || buffer[position + 1] > 0xbf ||
					buffer[position + 2] < 0x80 || buffer[position + 2] > 0xbf ||
					buffer[position + 3] < 0x80 || buffer[position + 3] > 0xbf)
				{
					bytes = 0;
					return false;
				}

				bytes = 4;
				return true;
			}

			if (ch == 0xf4)
			{
				if (position >= length - 4)
				{
					bytes = 0;
					return false;
				}

				if (buffer[position + 1] < 0x80 || buffer[position + 1] > 0x8f ||
					buffer[position + 2] < 0x80 || buffer[position + 2] > 0xbf ||
					buffer[position + 3] < 0x80 || buffer[position + 3] > 0xbf)
				{
					bytes = 0;
					return false;
				}

				bytes = 4;
				return true;
			}

			if (ch >= 0xf1 && ch <= 0xf3)
			{
				if (position >= length - 4)
				{
					bytes = 0;
					return false;
				}

				if (buffer[position + 1] < 0x80 || buffer[position + 1] > 0xbf ||
					buffer[position + 2] < 0x80 || buffer[position + 2] > 0xbf ||
					buffer[position + 3] < 0x80 || buffer[position + 3] > 0xbf)
				{
					bytes = 0;
					return false;
				}

				bytes = 4;
				return true;
			}

			return false;
		}
////////////////////////////////////////////////////////////////
//	END OF OPEN SOURCE CODE
////////////////////////////////////////////////////////////////
#endregion

		/// <summary>
		/// Indica en texto el nombre del tipo de codificación de un archivo.
		/// </summary>
		/// <param name="FileName"></param>
		/// <returns></returns>
		private static Encoding GetFileEncoding(String FileName)
		{
			Encoding Result = null;

			FileInfo FI = new FileInfo(FileName);

			FileStream FS = null;

			try
			{
				FS = FI.OpenRead();

				PreFilterEncodingsWithPreamble();   //obtener las codificaciones válidas
				EncodingInfo[] UnicodeEncodings = encodingsWithPreamble;

				for (int i = 0; Result == null && i < UnicodeEncodings.Length; i++)
				{
					FS.Position = 0;

					byte[] Preamble = UnicodeEncodings[i].GetEncoding().GetPreamble();

					bool PreamblesAreEqual = true;

					for (int j = 0; PreamblesAreEqual && j < Preamble.Length; j++)
					{
						PreamblesAreEqual = Preamble[j] == FS.ReadByte();
					}

					if (PreamblesAreEqual)
					{
						Result = UnicodeEncodings[i].GetEncoding();
					}
				}
			}
			catch (System.IO.IOException)
			{
			}
			finally
			{
				if (FS != null)
				{
					FS.Close();
				}
			}

			if (Result == null)
			{
				Result = Encoding.Default;
			}

			//Verificamos si es un archivo UTF-8 sin BOM (algo muy común en sistemas no Windows)
			//Para ello usamos detección heurística de secuencias de caracteres
			if (Result == Encoding.Default)
			{
				if (checkUTFWithoutBOM(FileName))
					Result = Encoding.UTF8;
			}

			return Result;
		}

		#endregion
	}
}
