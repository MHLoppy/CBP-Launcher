using CodeKicker.BBCode;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
//using CefSharp;
//using CefSharp.Wpf;
using HTMLConverter;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Diagnostics;
using System.Windows.Navigation;

namespace CBPLauncher.Skins
{
    /// WARNING
    /// DO NOT use messagebox.show here - it will interrupt the flowdocument and crash it
    /// also certain stuff related to the flowdoc MUST be done on the main thread or it will crash
    /// WARNING
    public partial class ClassicPlusPatchNotes : UserControl
    {
        public ClassicPlusPatchNotes()
        {
            InitializeComponent();
        }

        private DependencyObject dummy = new DependencyObject();

        private bool IsInDesignMode()
        {
            return DesignerProperties.GetIsInDesignMode(dummy);
        }

        void PatchNotes_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void FDViewer_Initialized(object sender, EventArgs e)
        {
            if (IsInDesignMode() == false)
            {
                // Placeholder while loading
                var placeholder = new FlowDocument();
                placeholder.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#E2363636");
                placeholder.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#E2363636");//make the text illegible

                placeholder.FontFamily = (FontFamily)Application.Current.Resources["DefaultFont"];
                placeholder.FontSize = 15;
                placeholder.PagePadding = new Thickness(10, 10, 10, 5); // hyperlink height is higher lol
                placeholder.TextAlignment = TextAlignment.Left;

                // replicate top so it looks similar
                Paragraph paragraph = new Paragraph();
                placeholder.Blocks.Add(paragraph);
                Run text1 = new Run("For explanations and more details about these changes, check the full patch notes.");
                paragraph.Inlines.Add(text1);
                string placeholderHtml = "<html><body style='background-color: #4B101010; font-family: sans-serif; color: #C8C8C8;'>";

                // Add blank lines to fill space
                for (int i = 0; i < 50; i++)
                {
                    placeholderHtml += "<br>&nbsp;</br>";
                }
                placeholderHtml += "</body></html>";

                string placeholderXaml = HtmlToXamlConverter.ConvertHtmlToXaml(placeholderHtml, false);
                placeholder.Blocks.Add((Section)XamlReader.Parse(placeholderXaml));

                FDViewer.Document = placeholder;

                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += (s, ev) =>
                {
                    try
                    {
                        // The doc (?) is fussy about being done on a background thread, so just do what we can in background
                        string patchnotes = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @"CBP/patchnotes.txt"));

                        if (File.Exists(patchnotes))
                        {
                            string formattedPatchNotes = "<html><body style='background-color: #4B101010; font-family: sans-serif; color: #C8C8C8;'>" + ProcessBBCodeFromTxtFile(patchnotes) + "</body></html>";
                            string xaml = HtmlToXamlConverter.ConvertHtmlToXaml(formattedPatchNotes, false);
                            ev.Result = xaml;
                        }
                        else
                        {
                            ev.Result = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        ev.Result = ex;
                    }
                };
                worker.RunWorkerCompleted += (s, ev) =>
                {
                    // All UI element creation on UI thread
                    try
                    {
                        if (ev.Result is Exception ex)
                        {
                            FDViewer.Document = CreateErrorDocument("Error loading patch notes: " + ex);
                        }
                        else if (ev.Result is string xaml)
                        {
                            FDViewer.Document = LoadFormattedPatchNotes(xaml);
                        }
                        else
                        {
                            FDViewer.Document = LoadFormattedPatchNotes(null);
                        }
                    }
                    catch (Exception ex)
                    {
                        FDViewer.Document = CreateErrorDocument("Error building document: " + ex);
                    }
                };
                worker.RunWorkerAsync();
            }
            else
            {
                //designtime baybeee
                var doc = new FlowDocument();
                doc.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#E2363636");
                FDViewer.Document = doc;
            }
        }

        // Now takes pre-processed XAML string so we can offload better
        private FlowDocument LoadFormattedPatchNotes(string xaml)
        {
            // this version is used when rendering directly in HTML (e.g. webbrowser control or cefsharp)
            //string formattedPatchNotes = "<html><body style='background-color: #404040; font-family: sans-serif; color: #f8f8f8; font-size:90%;'>" + ProcessBBCodeFromTxtFile(patchnotes) + "</body></html>";

            //WebBrowserControl.NavigateToString(formattedPatchNotes);//this is the integrated browser, which unfortunately doesn't render properly in transparent windows:
            //https://web.archive.org/web/20150415020527/http://blogs.msdn.com/b/changov/archive/2009/01/19/webbrowser-control-on-transparent-wpf-window.aspx
            //hence the unfortunate need to swap to another option e.g. CEF (much, much heavier than the integrated one though)

            var doc = new FlowDocument();

            // manually adding the hyperlink (and associated text) at the top (because flowdocuments don't handle URLs by default, even though they do display as if they do)
            // https://stackoverflow.com/questions/2288999/how-can-i-get-a-flowdocument-hyperlink-to-launch-browser-and-go-to-url-in-a-wpf
            Paragraph paragraph = new Paragraph();
            doc.Blocks.Add(paragraph);
            Run normaltext1 = new Run("For explanations and more details about these changes, check the ");
            paragraph.Inlines.Add(normaltext1);
            Run linktext = new Run("full patch notes");
            Hyperlink workshoplink = new Hyperlink(linktext);
            workshoplink.NavigateUri = new Uri("https://mhloppy.com/cbp-latest-patch");
            workshoplink.Foreground = new SolidColorBrush(Color.FromRgb(229, 213, 142));
            workshoplink.RequestNavigate += new RequestNavigateEventHandler(PatchNotes_RequestNavigate);
            paragraph.Inlines.Add(workshoplink);
            Run normaltext2 = new Run(".");
            paragraph.Inlines.Add(normaltext2);

            // add patch notes - the main part of the flowdocument
            if (xaml != null)
            {
                doc.Blocks.Add((Section)XamlReader.Parse(xaml));
            }
            else
            {
                Paragraph errorParagraph = new Paragraph();
                errorParagraph.Inlines.Add(new Run("Unable to load patch notes file (maybe CBP isn't loaded)."));
                doc.Blocks.Add(errorParagraph);
            }

            doc.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#E2363636");
            doc.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#EEEEEE");
            doc.FontFamily = (FontFamily)Application.Current.Resources["DefaultFont"];
            doc.FontSize = 15;
            doc.PagePadding = new Thickness(10, 5, 10, 5);
            doc.TextAlignment = TextAlignment.Left;

            return doc;
        }

        private FlowDocument CreateErrorDocument(string errorMessage)
        {
            var doc = new FlowDocument();
            Paragraph myParagraph = new Paragraph();
            myParagraph.Inlines.Add(new Run(errorMessage));
            doc.Blocks.Add(myParagraph);
            return doc;
        }

        /*private void CWB_IsBrowserInitializedChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (CWB.IsInitialized)
            {
                LoadFormattedPatchNotes();
            }
            else
            {
                //not initialized
            }
        }*/

        private string ProcessBBCodeFromTxtFile(string txtfile)
        {
            string text = File.ReadAllText(txtfile);

            text = Regex.Replace(text, @"- ", @"[*] ");
            text = Regex.Replace(text, @"--> ", @"[*] --> ");

            var bbTags = new List<BBTag>()
            {
                /*new BBTag("h1", "<h1>", "</h1>"),
                new BBTag("h2", "<h2>", "</h2>"),
                new BBTag("h3", "<h3>", "</h3>"),
                new BBTag("h4", "<h4>", "</h4>"),*/

                // headers are too big with defaults in flowdocument since it doesn't accept the 90% text scaling
                new BBTag("h1", "<h2>", "</h2>"),
                new BBTag("h2", "<h3>", "</h3>"),
                new BBTag("h3", "<h4>", "</h4>"),
                new BBTag("h4", "<h5>", "</h5>"),//does h5 exist?

                new BBTag("b", "<strong>", "</strong>"),
                new BBTag("i", "<em>", "</em>"),
                new BBTag("u", "<span style=\"text-decoration: line-underline\">", "</span>"),//doesn't seem to work, I guess maybe not supported by the built-in browser
                new BBTag("s", "<span style=\"text-decoration: line-through\">", "</span>"),

                new BBTag("list", "<ul>", "</ul>") { SuppressFirstNewlineAfter = true },//the true/false new line suppression doesn't seem to have any effect for my specific formatting
                new BBTag("li", "<li>", "</li>", true, false),
                new BBTag("*", "<li>", "</li>", true, false),

                new BBTag("url", "<a href=\"${url}\">", "</a>",
                    new BBAttribute("url", "")),

                new BBTag("code", "<pre class=\"prettyprint\">", "</pre>")
                {
                    StopProcessing = true,
                    SuppressFirstNewlineAfter = true
                },

                new BBTag("hr", "<hr>", "</hr>"),
            };
            var parser = new BBCodeParser(bbTags);

            text = parser.ToHtml(text);

            return text;
        }
    }
}
