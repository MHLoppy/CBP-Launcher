using System.Threading;
using System.Windows;
using CBPSetupGUI.Language;

namespace CBPSetupGUI
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Only targeting Windows; not below Win7")]
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

        public static bool LangFallback = false;

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
                    LangFallback = true;
                    break;
            }

        }
    }
}
