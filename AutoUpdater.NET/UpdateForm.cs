using AutoUpdaterDotNET.Properties;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Markup;
using System.Xml.Serialization;
using static System.Reflection.Assembly;

namespace AutoUpdaterDotNET;

internal sealed partial class UpdateForm : Form
{
    private readonly UpdateInfoEventArgs _args;
    CultureInfo cultureInfo;
    System.ComponentModel.ComponentResourceManager resources;
    ResourceManager resourceManager;
    public UpdateForm(UpdateInfoEventArgs args)
    {
        cultureInfo = new CultureInfo("ar");
        // Thread.CurrentThread.CurrentUICulture = new CultureInfo("a   `r");
        Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("ar");
        _args = args;
        InitializeComponent();
        InitializeBrowserControl();
        TopMost = AutoUpdater.TopMost;

        if (AutoUpdater.Icon != null)
        {
            pictureBoxIcon.Image = AutoUpdater.Icon;
            Icon = Icon.FromHandle(AutoUpdater.Icon.GetHicon());
        }
        // CultureInfo.CurrentCulture = new CultureInfo("ar");
        // Thread.CurrentThread.CurrentUICulture = new CultureInfo("ar");
        // Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = new CultureInfo("ar");// CultureInfo.CreateSpecificCulture("ar");

        // resources = new System.ComponentModel.ComponentResourceManager(typeof(UpdateForm));
        //   ComponentResourceManager rm = new ComponentResourceManager("YourNamespace.Resource", typeof(Program).Assembly);
        resourceManager = new System.Resources.ResourceManager("AutoUpdaterDotNET.Properties.Resources", typeof(Resources).Assembly);
        // تحميل النصوص بناءً على الثقافة الحالية
        //  string welcomeMessage = rm.GetString("WelcomeMessage");

        buttonSkip.Visible = AutoUpdater.ShowSkipButton;
        buttonRemindLater.Visible = AutoUpdater.ShowRemindLaterButton;
        // var resources = new ComponentResourceManager(typeof(UpdateForm));
        CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
        resources = new System.ComponentModel.ComponentResourceManager(typeof(UpdateForm));
        resources.ApplyResources(this.buttonUpdate, "buttonUpdate", cultureInfo);
     // var fff=  resources.GetResourceSet(cultureInfo,true,false);//CultureInfo.CurrentCulture
      //  fff?.GetString("buttonUpdate.Text");
        Text = string.Format(resources.GetString("$this.Text", cultureInfo)!,
            AutoUpdater.AppTitle, _args.CurrentVersion);
        labelUpdate.Text = string.Format(resources.GetString("labelUpdate.Text", CultureInfo.CurrentCulture)!,
            AutoUpdater.AppTitle);
        labelDescription.Text =
            string.Format(resources.GetString("labelDescription.Text", CultureInfo.CurrentCulture)!,
                AutoUpdater.AppTitle, _args.CurrentVersion, _args.InstalledVersion);

        if (AutoUpdater.Mandatory && AutoUpdater.UpdateMode == Mode.Forced)
        {
            ControlBox = false;
        }
        SetLanguage();
        
    // MessageBox.Show( CultureInfo.CurrentCulture.Name+ resources.BaseName + resourceManager?.GetString("BtnUpdate")+"--"+ resources.GetString("$this.Text", cultureInfo));
    }
    void SetLanguage()
    {
        this.buttonUpdate.Text = resourceManager?.GetString("BtnUpdate") ?? resources.GetString("buttonUpdate.Text", cultureInfo);
        // this.buttonUpdate.Text = resourceManager?.GetString("BtnUpdate") ?? resources.GetString("buttonUpdate.Text", cultureInfo);
        // resources.ApplyResources(this, "$this", cultureInfo);
        //resources.ApplyResources(this.buttonSkip, "buttonSkip", cultureInfo);
        //resources.ApplyResources(this.pictureBoxIcon, "pictureBoxIcon", cultureInfo);
        //resources.ApplyResources(this.buttonUpdate, "buttonUpdate", cultureInfo);
        //resources.ApplyResources(this.labelReleaseNotes, "labelReleaseNotes", cultureInfo);
        //// resources.ApplyResources(this.labelDescription, "labelDescription", ff);
        //resources.ApplyResources(this.buttonRemindLater, "buttonRemindLater", cultureInfo);
        //Text = string.Format(resources.GetString("$this.Text", cultureInfo/* CultureInfo.CurrentCulture*/),
        //   AutoUpdater.AppTitle, _args.CurrentVersion);
        this.buttonUpdate.Anchor=AnchorStyles.Top|AnchorStyles.Right;
        this.buttonUpdate.Visible=true;
    }
  
    private async void InitializeBrowserControl()
    {
        if (string.IsNullOrEmpty(_args.ChangelogURL))
        {
            int reduceHeight = labelReleaseNotes.Height + webBrowser.Height;
            labelReleaseNotes.Hide();
            webBrowser.Hide();
            webView2.Hide();
            Height -= reduceHeight;
        }
        else
        {
            var webView2RuntimeFound = false;
            try
            {
                string availableBrowserVersion = CoreWebView2Environment.GetAvailableBrowserVersionString(null);
                var requiredMinBrowserVersion = "86.0.616.0";
                if (!string.IsNullOrEmpty(availableBrowserVersion)
                    && CoreWebView2Environment.CompareBrowserVersions(availableBrowserVersion,
                        requiredMinBrowserVersion) >= 0)
                {
                    webView2RuntimeFound = true;
                }
            }
            catch (Exception)
            {
                // ignored
            }

            if (webView2RuntimeFound)
            {
                webBrowser.Hide();
               

                
                webView2.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;
                //await webView2.EnsureCoreWebView2Async(null);

                await webView2.EnsureCoreWebView2Async(
                    await CoreWebView2Environment.CreateAsync(null, Path.GetTempPath()));
            }
            else
            {
                UseLatestIE();
                if (null != AutoUpdater.BasicAuthChangeLog)
                {
                    
                    webBrowser.Navigate(_args.ChangelogURL, "", null,
                        $"Authorization: {AutoUpdater.BasicAuthChangeLog}");
                }
                else
                {
                    try
                    {
                        var uri = getChangeLogUri();
                        webBrowser.Navigate(uri);
                    }
                    catch (Exception)
                    {
                        webBrowser.Navigate(_args.ChangelogURL);
                    }
                }
            }
        }
    }

    private void WebView_CoreWebView2InitializationCompleted(object sender,
        CoreWebView2InitializationCompletedEventArgs e)
    {
        if (!e.IsSuccess)
        {
            if (AutoUpdater.ReportErrors)
            {
                MessageBox.Show(this, e.InitializationException.Message, e.InitializationException.GetType().ToString(),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return;
        }

        webView2.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        webView2.CoreWebView2.Settings.IsStatusBarEnabled = false;
        webView2.CoreWebView2.Settings.AreDevToolsEnabled = Debugger.IsAttached;
        webView2.CoreWebView2.Settings.UserAgent = AutoUpdater.GetUserAgent();
        webView2.CoreWebView2.Profile.ClearBrowsingDataAsync();
        webView2.Show();
        webView2.BringToFront();
        if (null != AutoUpdater.BasicAuthChangeLog)
        {
            webView2.CoreWebView2.BasicAuthenticationRequested += delegate(
                object _,
                CoreWebView2BasicAuthenticationRequestedEventArgs args)
            {
                args.Response.UserName = ((BasicAuthentication)AutoUpdater.BasicAuthChangeLog).Username;
                args.Response.Password = ((BasicAuthentication)AutoUpdater.BasicAuthChangeLog).Password;
            };
        }

        //LoadLargeHtml();
        //webView2.CoreWebView2.NavigateToString(htmlContent);
        //Task.Run(async () =>
        //{
            //LoadLargeHtml();
            //await webView2.EnsureCoreWebView2Async(null);

            //webView2.CoreWebView2.NavigateToString(htmlContent);
        //});
        //webView2.CoreWebView2.Navigate("file:///D:/TikhahSoft/AutoUpdateFile/tikhah-soft/whatsapp/Note.html");
        var uri = getChangeLogUri();
        webView2.CoreWebView2.Navigate(uri);
    }

    private  void LoadLargeHtml()
    {
        if (!AutoUpdater.ChangeLogAsStringHtml)
        {
            webView2.CoreWebView2.Navigate(_args.ChangelogURL);
            return;
        }
        string html=string.Empty;
        var BaseUri = new Uri(_args.ChangelogURL);
        using (MyWebClient client = AutoUpdater.GetWebClient(BaseUri, AutoUpdater.BasicAuthXML))
        {
            html = client.DownloadString(BaseUri);
        }

        //var http = new HttpClient();
        //var html = await http.GetStringAsync(
        //    "https://raw.githubusercontent.com/username/repo/main/file.html"
        //);
        //string html = await File.ReadAllTextAsync("index.html");

        string tempPath = Path.Combine(Path.GetTempPath(), "pdfviewer.html");
        tempPath=tempPath.Replace("\\", "/");
        File.WriteAllText(tempPath, html);

        webView2.CoreWebView2.Navigate($"file:///{tempPath}");
    }

    private string getChangeLogUri()
    {
        if (!AutoUpdater.ChangeLogAsStringHtml)
        {
            return _args.ChangelogURL;
        }
        string html = string.Empty;
        var BaseUri = new Uri(_args.ChangelogURL);
        using (MyWebClient client = AutoUpdater.GetWebClient(BaseUri, AutoUpdater.BasicAuthXML))
        {
            html = client.DownloadString(BaseUri);
        }

        string tempPath = Path.Combine(Path.GetTempPath(), "pdfviewer.html");
        tempPath = tempPath.Replace("\\", "/");
        File.WriteAllText(tempPath, html);

       return $"file:///{tempPath}";
    }


    private void UseLatestIE()
    {
        int ieValue = webBrowser.Version.Major switch
        {
            11 => 11001,
            10 => 10001,
            9 => 9999,
            8 => 8888,
            7 => 7000,
            _ => 0
        };

        if (ieValue == 0)
        {
            return;
        }

        try
        {
            using RegistryKey registryKey =
                Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION",
                    true);
            registryKey?.SetValue(
                Path.GetFileName(Process.GetCurrentProcess().MainModule?.FileName ??
                                 GetEntryAssembly()?.Location ?? Application.ExecutablePath),
                ieValue,
                RegistryValueKind.DWord);
        }
        catch (Exception)
        {
            // ignored
        }
    }

    private void UpdateFormLoad(object sender, EventArgs e)
    {
        var labelSize = new Size(webBrowser.Width, 0);
        labelDescription.MaximumSize = labelUpdate.MaximumSize = labelSize;
    }

    private void ButtonUpdateClick(object sender, EventArgs e)
    {
        if (AutoUpdater.OpenDownloadPage)
        {
            
            var processStartInfo = new ProcessStartInfo(_args.DownloadURL);
#if NETCOREAPP
            // for .NET Core, UseShellExecute must be set to true, otherwise
            // opening URLs via Process.Start() fails 
            processStartInfo.UseShellExecute = true;
#endif
            Process.Start(processStartInfo);

            DialogResult = DialogResult.OK;
        }
        else
        {
            if (AutoUpdater.DownloadUpdate(_args))
            {
                DialogResult = DialogResult.OK;
            }
        }
    }

    private void ButtonSkipClick(object sender, EventArgs e)
    {
        AutoUpdater.PersistenceProvider.SetSkippedVersion(new Version(_args.CurrentVersion));
    }

    private void ButtonRemindLaterClick(object sender, EventArgs e)
    {
        if (AutoUpdater.LetUserSelectRemindLater)
        {
            using var remindLaterForm = new RemindLaterForm();
            DialogResult dialogResult = remindLaterForm.ShowDialog(this);

            switch (dialogResult)
            {
                case DialogResult.OK:
                    AutoUpdater.RemindLaterTimeSpan = remindLaterForm.RemindLaterFormat;
                    AutoUpdater.RemindLaterAt = remindLaterForm.RemindLaterAt;
                    break;
                case DialogResult.Abort:
                    ButtonUpdateClick(sender, e);
                    return;
                default:
                    return;
            }
        }

        AutoUpdater.PersistenceProvider.SetSkippedVersion(null);

        DateTime remindLaterDateTime = AutoUpdater.RemindLaterTimeSpan switch
        {
            RemindLaterFormat.Days => DateTime.Now + TimeSpan.FromDays(AutoUpdater.RemindLaterAt),
            RemindLaterFormat.Hours => DateTime.Now + TimeSpan.FromHours(AutoUpdater.RemindLaterAt),
            RemindLaterFormat.Minutes => DateTime.Now + TimeSpan.FromMinutes(AutoUpdater.RemindLaterAt),
            _ => DateTime.Now
        };

        AutoUpdater.PersistenceProvider.SetRemindLater(remindLaterDateTime);
        AutoUpdater.SetTimer(remindLaterDateTime);

        DialogResult = DialogResult.Cancel;
    }

    private void UpdateForm_FormClosed(object sender, FormClosedEventArgs e)
    {
        AutoUpdater.Running = false;
    }

    private void UpdateForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        if (AutoUpdater.Mandatory && AutoUpdater.UpdateMode == Mode.Forced)
        {
            AutoUpdater.Exit();
        }
    }
}