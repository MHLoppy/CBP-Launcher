using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
//using static CBPSetupGUI.Container;
using CBPSetupGUI.Language;

namespace CBPSetupGUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            SetLanguageDictionary();
        }

        public static void SetLanguageDictionary()
        {
            switch (Thread.CurrentThread.CurrentCulture.ToString().Substring(0, 2))
            {
                //english, chinese, french, german, italian, japanese, korean, portugese, russian, spanish
                //codes ref https://www.w3schools.com/tags/ref_language_codes.asp
                case "en":
                    Language.Resources.Culture = new System.Globalization.CultureInfo("en");
                    break;
                case "zh":
                    Language.Resources.Culture = new System.Globalization.CultureInfo("zh");
                    break;
                case "fr":
                    Language.Resources.Culture = new System.Globalization.CultureInfo("fr");
                    break;
                case "de":
                    Language.Resources.Culture = new System.Globalization.CultureInfo("de");
                    break;
                case "it":
                    Language.Resources.Culture = new System.Globalization.CultureInfo("it");
                    break;
                case "ja":
                    Language.Resources.Culture = new System.Globalization.CultureInfo("ja");
                    break;
                case "ko":
                    Language.Resources.Culture = new System.Globalization.CultureInfo("ko");
                    break;
                case "pt":
                    Language.Resources.Culture = new System.Globalization.CultureInfo("pt");
                    break;
                case "ru":
                    Language.Resources.Culture = new System.Globalization.CultureInfo("ru");
                    break;
                case "es":
                    Language.Resources.Culture = new System.Globalization.CultureInfo("es");
                    break;
                default://default to the language of freedom, just like rms would've wanted
                    Language.Resources.Culture = new System.Globalization.CultureInfo("en");
                    break;
            }

        }
    }
}
