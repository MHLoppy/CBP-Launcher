/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;

namespace CBPpatriots
{
    class Program
    {
        private static readonly string CBPSInLocalMods = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), @"mods\Community Balance Patch\CBP Setup GUI.exe"));

        static void Main()
        {
            StartCBPLProcess();
        }

        private static void StartCBPLProcess()
        {
            //there won't be time to read this unless it fails, but it will be useful for troubleshooting when that does happen
            {
                // say path
                switch (Thread.CurrentThread.CurrentCulture.ToString().Substring(0, 2))
                {
                    //english, chinese, french, german, italian, japanese, korean, portugese, russian, spanish
                    //codes ref https://www.w3schools.com/tags/ref_language_codes.asp
                    case "en":
                        Console.WriteLine("Looking for CBP Setup GUI.exe at: " + CBPSInLocalMods + "\n:"); break;
                    case "zh":
                        Console.WriteLine("寻找CBP Setup GUI.exe，在: " + CBPSInLocalMods + "\n:"); break;
                    case "fr":
                        Console.WriteLine("Vous cherchez CBP Setup GUI.exe à: " + CBPSInLocalMods + "\n:"); break;
                    case "de":
                        Console.WriteLine("Suchen Sie CBP Setup GUI.exe unter at: " + CBPSInLocalMods + "\n:"); break;
                    case "it":
                        Console.WriteLine("Cercando CBP Setup GUI.exe a: " + CBPSInLocalMods + "\n:"); break;
                    case "ja":
                        Console.WriteLine("でCBP Setup GUI.exeを探しています: " + CBPSInLocalMods + "\n:"); break;
                    case "ko":
                        Console.WriteLine("다음에서 CBP Setup GUI.exe를 찾고 있습니다: " + CBPSInLocalMods + "\n:"); break;
                    case "pt":
                        Console.WriteLine("À procura de CBP Setup GUI.exe em: " + CBPSInLocalMods + "\n:"); break;
                    case "ru":
                        Console.WriteLine("Ищем CBP Setup GUI.exe по адресу: " + CBPSInLocalMods + "\n:"); break;
                    case "es":
                        Console.WriteLine("Buscando CBP Setup GUI.exe en:: " + CBPSInLocalMods + "\n:"); break;
                    default://default to the language of freedom, just like rms would've wanted
                        Console.WriteLine("Looking for CBP Setup GUI.exe at: " + CBPSInLocalMods + "\n:"); break;
                }
            }

            try
            {
                // I'm not actually sure if this whole shebang is necessary just to start it, but I've done it anyway
                _ = new ProcessStartInfo(CBPSInLocalMods)
                {
                    WorkingDirectory = CBPSInLocalMods + @"..\"
                };
                Process.Start(CBPSInLocalMods);
            }
            catch (Exception ex)
            {
                // say error
                switch (Thread.CurrentThread.CurrentCulture.ToString().Substring(0, 2))
                {
                    //english, chinese, french, german, italian, japanese, korean, portugese, russian, spanish
                    //codes ref https://www.w3schools.com/tags/ref_language_codes.asp
                    case "en":
                        Console.WriteLine("Error starting CBP Setup GUI:\n\n" + ex); break;
                    case "zh":
                        Console.WriteLine("启动CBP Setup GUI时出错:\n\n" + ex); break;
                    case "fr":
                        Console.WriteLine("Erreur lors du démarrage du CBP Setup GUI:\n\n" + ex); break;
                    case "de":
                        Console.WriteLine("Fehler beim Starten der CBP Setup GUI:\n\n" + ex); break;
                    case "it":
                        Console.WriteLine("Errore nell'avviare la GUI di CBP Setup:\n\n" + ex); break;
                    case "ja":
                        Console.WriteLine("CBP Setup GUIの起動エラー:\n\n" + ex); break;
                    case "ko":
                        Console.WriteLine("CBP Setup GUI 시작 중 오류 :\n\n" + ex); break;
                    case "pt":
                        Console.WriteLine("Erro ao iniciar o CBP Setup GUI:\n\n" + ex); break;
                    case "ru":
                        Console.WriteLine("Ошибка запуска CBP Setup GUI:\n\n" + ex); break;
                    case "es":
                        Console.WriteLine("CBP Setup GUI error de inicio:\n\n" + ex); break;
                    default://default to the language of freedom, just like rms would've wanted
                        Console.WriteLine("Error starting CBP Setup GUI:\n\n" + ex); break;
                }
                Console.ReadLine();
                Environment.Exit(-1);
            }
        }
    }
}
