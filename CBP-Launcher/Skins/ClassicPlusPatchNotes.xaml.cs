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
    /// WARNING
    public partial class ClassicPlusPatchNotes : UserControl//TODO: check for patch notes file before continuing
    {
        // tried to reduce memory usage by assigning this a single time here (except for again in the catch exception) instead of once in each block of relevant code, but it didn't seem to change memory usage
        FlowDocument myFlowDocument = new FlowDocument();

        public ClassicPlusPatchNotes()
        {
            InitializeComponent();
        }

        private void FDViewer_Initialized(object sender, EventArgs e)
        {
            if (IsInDesignMode() == false)
            {
                try
                {
                    LoadFormattedPatchNotes();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading patch notes: " + ex);
                }
            }
            else
            {
                //designtime baybeee

                myFlowDocument.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#E2363636");
                FDViewer.Document = myFlowDocument;
            }
        }

        private DependencyObject dummy = new DependencyObject();

        private bool IsInDesignMode()
        {
            return DesignerProperties.GetIsInDesignMode(dummy);
        }

        void PatchNotes_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
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

        private void LoadFormattedPatchNotes()
        {
            // this version is used when rendering directly in HTML (e.g. webbrowser control or cefsharp)
            //string formattedPatchNotes = "<html><body style='background-color: #404040; font-family: sans-serif; color: #f8f8f8; font-size:90%;'>" + ProcessBBCodeFromTxtFile(patchnotes) + "</body></html>";

            //WebBrowserControl.NavigateToString(formattedPatchNotes);//this is the integrated browser, which unfortunately doesn't render properly in transparent windows:
            //https://web.archive.org/web/20150415020527/http://blogs.msdn.com/b/changov/archive/2009/01/19/webbrowser-control-on-transparent-wpf-window.aspx
            //hence the unfortunate need to swap to another option e.g. CEF (much, much heavier than the integrated one though)

            //Console.WriteLine(formattedPatchNotes);

            //first check if file exists
            if (File.Exists(Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @"mods/Community Balance Patch/patchnotes.txt"))))
            {
                try
                {
                    // manually adding the hyperlink (and associated text) at the top (because flowdocuments don't handle URLs by default, even though they do display as if they do)
                    // https://stackoverflow.com/questions/2288999/how-can-i-get-a-flowdocument-hyperlink-to-launch-browser-and-go-to-url-in-a-wpf
                    Paragraph paragraph = new Paragraph();
                    myFlowDocument.Blocks.Add(paragraph);
                    Run normaltext1 = new Run("These are only summaries with the most important details. Check the ");
                    paragraph.Inlines.Add(normaltext1);
                    Run linktext = new Run("Steam Workshop");
                    Hyperlink workshoplink = new Hyperlink(linktext);
                    workshoplink.NavigateUri = new Uri("https://steamcommunity.com/sharedfiles/filedetails/changelog/2287791153");
                    workshoplink.RequestNavigate += new RequestNavigateEventHandler(Workshoplink_RequestNavigate);
                    paragraph.Inlines.Add(workshoplink); //ensure to add linkname, not linktextname
                    Run normaltext2 = new Run(" for links to the full patch notes.");
                    paragraph.Inlines.Add(normaltext2);

                    // patch notes - the main part of the flowdocument
                    string patchnotes = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @"mods/Community Balance Patch/patchnotes.txt"));//relies on CBP Launcher being in root folder (as expected)
                    string formattedPatchNotes = "<html><body style='background-color: #4B101010; font-family: sans-serif; color: #C8C8C8;'>" + ProcessBBCodeFromTxtFile(patchnotes) + "</body></html>";

                    string xaml = HtmlToXamlConverter.ConvertHtmlToXaml(formattedPatchNotes, false);
                    myFlowDocument.Blocks.Add((Section)XamlReader.Parse(xaml));

                    //because the document is larger than the pure HTML page was (in terms of visual space), the background needs to be set a bit differentl in order to cover the whole area:
                    myFlowDocument.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#E2363636");
                    myFlowDocument.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#EEEEEE");
                    myFlowDocument.FontFamily = new FontFamily("Segoe UI");
                    myFlowDocument.FontSize = 15;
                    myFlowDocument.PagePadding = new Thickness(10, 5, 10, 5);

                    FDViewer.Document = myFlowDocument;
                }
                catch (Exception ex)
                {
                    // not sure if this catch will work, but it doesn't hurt to try
                    FlowDocument myFlowDocument = new FlowDocument();

                    Paragraph myParagraph = new Paragraph();
                    myParagraph.Inlines.Add(new Run("Patch notes could not be loaded.\n\n" + ex));
                    myFlowDocument.Blocks.Add(myParagraph);

                    FDViewer.Document = myFlowDocument;
                }
            }
            else
            {
                Paragraph paragraph = new Paragraph();
                myFlowDocument.Blocks.Add(paragraph);
                Run normaltext1 = new Run("Unable to load patch notes file.");
                paragraph.Inlines.Add(normaltext1);

                myFlowDocument.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#E2363636");
                myFlowDocument.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#EEEEEE");
                myFlowDocument.FontFamily = new FontFamily("Segoe UI");
                myFlowDocument.FontSize = 15;
                myFlowDocument.PagePadding = new Thickness(10, 5, 10, 5);

                FDViewer.Document = myFlowDocument;
            }

            //CWB.LoadHtml(formattedPatchNotes);//throws a debugging error on exit, but for now I simply don't care enough to fix it (I assume it's because cwb isn't being shut down correctly)
        }

        private void Workshoplink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

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
