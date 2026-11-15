using System.Net;
using System.Text;
using System.Text.Json;
using GakumasVR.Configurator;
using GakumasVR.Core;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        Application.EnableVisualStyles();
        VrUiText.Validate();
        (string Name, Action Run)[] tests =
        {
            ("GitHub metadata carries release notes and a trusted release URL", Metadata),
            ("Equal and prerelease versions do not trigger an update notice", VersionGate),
            ("Missing release notes do not block a valid update", MissingNotes),
            ("Update notice is localized and blocks installation while playing", Notice),
            ("Contact consent notice requires scrolling to the end in every GUI language", ConsentReading)
        };
        int failures = 0;
        foreach (var test in tests)
        {
            try { test.Run(); Console.WriteLine($"PASS {test.Name}"); }
            catch (Exception exception) { failures++; Console.WriteLine($"FAIL {test.Name}: {exception}"); }
        }
        if (args.Length == 1 && failures == 0) Render(args[0]);
        Console.WriteLine($"Executed {tests.Length} configurator tests; failures: {failures}");
        return failures == 0 ? 0 : 1;
    }

    private static void Metadata()
    {
        using UpdateService service = Service("v99.0.0", "## English\n- **Fixed** panels");
        AvailableUpdate update = service.CheckAsync(CancellationToken.None).GetAwaiter().GetResult()
            ?? throw new Exception("Expected a new version");
        Require(update.ReleaseNotes == "## English\n- **Fixed** panels", "Release body lost");
        Require(update.ReleaseUrl == "https://github.com/deadpixel134/gakumas-vr/releases/tag/v99.0.0", "Untrusted release URL");
    }

    private static void VersionGate()
    {
        using UpdateService current = Service("v" + UpdateService.CurrentVersion, "notes");
        Require(current.CheckAsync(CancellationToken.None).GetAwaiter().GetResult() is null, "Equal version accepted");
        using UpdateService prerelease = Service("v99.0.0", "notes", prerelease: true);
        Require(prerelease.CheckAsync(CancellationToken.None).GetAwaiter().GetResult() is null, "Prerelease accepted");
    }

    private static void MissingNotes()
    {
        using UpdateService service = Service("v99.0.0", null);
        AvailableUpdate update = service.CheckAsync(CancellationToken.None).GetAwaiter().GetResult()
            ?? throw new Exception("Missing notes blocked the update");
        using UpdateNoticeForm form = new(update, false, VrUiLanguage.Korean);
        Require(form.Controls.Find("ReleaseNotes", true).Single().Text == VrUiText.Get(VrUiLanguage.Korean, "UpdateNotesEmpty"), "Missing notes fallback absent");
    }

    private static void Notice()
    {
        foreach (VrUiLanguage language in Enum.GetValues<VrUiLanguage>())
        {
            using UpdateNoticeForm form = new(Sample(), true, language);
            Require(!form.Controls.Find("InstallUpdate", true).Single().Enabled, "Installation allowed during gameplay");
            Require(form.Controls.Find("ReleaseNotes", true).Single().Text.Contains(language switch
            {
                VrUiLanguage.Korean => "패널 조작", VrUiLanguage.Japanese => "パネル操作", _ => "panel controls"
            }), "Wrong note language");
            Require(form.CancelButton?.DialogResult == DialogResult.Cancel, "Postponement does not cancel");
            using UpdateNoticeForm stopped = new(Sample(), false, language);
            Require(stopped.Controls.Find("InstallUpdate", true).Single().Enabled, "Installation disabled with the game closed");
        }
    }

    private static UpdateService Service(string version, string? body, bool prerelease = false)
    {
        string json = JsonSerializer.Serialize(new
        {
            tag_name = version, draft = false, prerelease, body,
            html_url = "https://untrusted.example/ignored",
            assets = new object[]
            {
                new { name = "GakumasVR-v99.0.0.zip", browser_download_url = "https://example.com/update.zip", size = 1234, digest = "sha256:" + new string('a', 64) }
            }
        });
        return new UpdateService(new HttpClient(new ReleaseHandler(json)));
    }

    private static AvailableUpdate Sample() => new("99.0.0", "update.zip", "https://example.com", null, null,
        "## 한국어\n- **패널 조작을 개선했습니다.**\n- 업데이트 전에 변경 사항을 확인할 수 있습니다.\n\n## English\n- **Improved panel controls.**\n- Review changes before installing the update.\n\n## 日本語\n- **パネル操作を改善しました。**\n- 更新前に変更内容を確認できます。",
        "https://github.com/deadpixel134/gakumas-vr/releases/tag/v99.0.0");

    private static void Render(string directory)
    {
        Directory.CreateDirectory(directory);
        foreach (VrUiLanguage language in Enum.GetValues<VrUiLanguage>())
        {
            using UpdateNoticeForm form = new(Sample(), false, language);
            form.Opacity = 0;
            form.Show();
            Application.DoEvents();
            form.PerformLayout();
            using Bitmap image = new(form.Width, form.Height);
            form.DrawToBitmap(image, new Rectangle(Point.Empty, form.Size));
            image.Save(Path.Combine(directory, $"release-notes-{VrUiPreferences.Code(language)}.png"));
            form.Close();
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void ConsentReading()
    {
        var atEnd = typeof(MainForm).GetMethod("ContactNoticeAtEnd",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            ?? throw new Exception("Full build is missing the contact consent notice");
        Require(typeof(MainForm).GetMethod("ConfirmContactConsent",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic) is not null,
            "Full build is missing the one-time consent flow");
        foreach (VrUiLanguage language in Enum.GetValues<VrUiLanguage>())
        {
            using Form host = new() { ClientSize = new Size(650, 300), Opacity = 0 };
            using RichTextBox body = new()
            {
                Dock = DockStyle.Fill, ReadOnly = true, ScrollBars = RichTextBoxScrollBars.Vertical,
                Text = VrUiText.Get(language, "ContactConsentBody"), Font = new Font("Segoe UI", 10f)
            };
            host.Controls.Add(body);
            host.Show();
            Application.DoEvents();
            body.Select(0, 0);
            SendMessage(body.Handle, 0x0115, new IntPtr(6), IntPtr.Zero); // WM_VSCROLL / SB_TOP
            body.ScrollToCaret();
            Require(!(bool)atEnd.Invoke(null, new object[] { body })!, "Unread notice accepted: " + language);
            body.Select(body.TextLength, 0);
            SendMessage(body.Handle, 0x0115, new IntPtr(7), IntPtr.Zero); // WM_VSCROLL / SB_BOTTOM
            Application.DoEvents();
            Require((bool)atEnd.Invoke(null, new object[] { body })!, "Read notice not recognized: " + language);
            host.Close();
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);

    private sealed class ReleaseHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Require(request.RequestUri?.AbsoluteUri == "https://api.github.com/repos/deadpixel134/gakumas-vr/releases/latest", "Unexpected request");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }
    }
}
