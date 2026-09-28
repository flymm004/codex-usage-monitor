using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Web.Script.Serialization;
using System.Globalization;

[assembly: AssemblyTitle("Codex Usage Monitor")]
[assembly: AssemblyDescription("Local, read-only Codex account usage viewer")]
[assembly: AssemblyCompany("Codex Usage Monitor contributors")]
[assembly: AssemblyProduct("Codex Usage Monitor")]
[assembly: AssemblyVersion("0.1.0.0")]
[assembly: System.Runtime.Versioning.TargetFramework(".NETFramework,Version=v4.8", FrameworkDisplayName = ".NET Framework 4.8")]

namespace CodexUsageMonitor
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            bool created;
            // Keep this identity stable so renamed versions still share one process instance.
            using (Mutex mutex = new Mutex(true, "Local\\CodexUsageBarMvp", out created))
            {
                if (!created) return;
                Application.Run(new UsageApplicationContext());
            }
        }
    }

    internal sealed class UsageApplicationContext : ApplicationContext
    {
        private readonly NotifyIcon tray;
        private readonly UsageWindow window;
        private readonly System.Windows.Forms.Timer refreshTimer;
        private Icon currentTrayIcon;
        private int refreshInProgress;
        private bool wasLow;
        private readonly ToolStripMenuItem showMenuItem;
        private readonly ToolStripMenuItem refreshMenuItem;
        private readonly ToolStripMenuItem languageMenuItem;
        private readonly ToolStripMenuItem exitMenuItem;
        private readonly List<ToolStripMenuItem> languageChoices = new List<ToolStripMenuItem>();
        private UsageResult latestResult;
        private bool lastReadFailed;

        public UsageApplicationContext()
        {
            window = new UsageWindow(RefreshUsage, ChangeLanguage);
            tray = new NotifyIcon();
            currentTrayIcon = TrayIconFactory.Create(null, false);
            tray.Icon = currentTrayIcon;
            tray.Text = UiText.Get("trayReading");
            tray.Visible = true;
            tray.MouseClick += delegate(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left) ToggleWindow();
            };

            ContextMenuStrip menu = new ContextMenuStrip();
            menu.BackColor = Palette.Panel;
            menu.ForeColor = Palette.Text;
            menu.Renderer = new DarkMenuRenderer();
            showMenuItem = new ToolStripMenuItem(null, null, delegate { ShowWindow(); });
            refreshMenuItem = new ToolStripMenuItem(null, null, delegate { RefreshUsage(); });
            languageMenuItem = new ToolStripMenuItem();
            foreach (string code in UiText.LanguageCodes)
                AddLanguageChoice(languageMenuItem, code, UiText.LanguageName(code));
            languageMenuItem.DropDownItems.Add(new ToolStripSeparator());
            AddLanguageChoice(languageMenuItem, "system", null);
            exitMenuItem = new ToolStripMenuItem(null, null, delegate { ExitApplication(); });
            menu.Items.Add(showMenuItem);
            menu.Items.Add(refreshMenuItem);
            menu.Items.Add(languageMenuItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(exitMenuItem);
            ApplyMenuLanguage();
            tray.ContextMenuStrip = menu;

            refreshTimer = new System.Windows.Forms.Timer();
            refreshTimer.Interval = 120000;
            refreshTimer.Tick += delegate { RefreshUsage(); };
            refreshTimer.Start();

            ShowWindow();
            RefreshUsage();
        }

        private void AddLanguageChoice(ToolStripMenuItem parent, string code, string name)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(name);
            item.Tag = code;
            item.Click += delegate { ChangeLanguage((string)item.Tag); };
            parent.DropDownItems.Add(item);
            languageChoices.Add(item);
        }

        private void ApplyMenuLanguage()
        {
            showMenuItem.Text = UiText.Get("menuShow");
            refreshMenuItem.Text = UiText.Get("menuRefresh");
            languageMenuItem.Text = UiText.Get("menuLanguage");
            exitMenuItem.Text = UiText.Get("menuExit");
            foreach (ToolStripMenuItem item in languageChoices)
            {
                if ((string)item.Tag == "system") item.Text = UiText.Get("systemLanguage");
                item.Checked = (string)item.Tag == UiText.Preference;
            }
        }

        private void ChangeLanguage(string code)
        {
            UiText.Select(code);
            ApplyMenuLanguage();
            window.ApplyLanguage();
            if (latestResult != null) UpdateTray(latestResult);
            else tray.Text = UiText.Get(lastReadFailed ? "trayFailed" : "trayReading");
        }

        private void ToggleWindow()
        {
            if (window.Visible) window.Hide();
            else ShowWindow();
        }

        private void ShowWindow()
        {
            Screen screen = Screen.FromPoint(Cursor.Position);
            Rectangle area = screen.WorkingArea;
            window.Location = new Point(Math.Max(area.Left, area.Right - window.Width - 8),
                Math.Max(area.Top, area.Bottom - window.Height - 8));
            window.Show();
            window.BringToFront();
            window.Activate();
        }

        private void RefreshUsage()
        {
            if (Interlocked.Exchange(ref refreshInProgress, 1) != 0) return;
            if (!window.IsDisposed) window.BeginRefresh();
            ThreadPool.QueueUserWorkItem(delegate
            {
                UsageResult result = null;
                UsageReadException error = null;
                try { result = CodexReader.Read(); }
                catch (UsageReadException ex) { error = ex; }
                catch (Exception) { error = new UsageReadException("errorGeneric"); }

                if (!window.IsDisposed && window.IsHandleCreated)
                {
                    try
                    {
                        window.BeginInvoke((MethodInvoker)delegate
                        {
                            if (result != null)
                            {
                                latestResult = result;
                                lastReadFailed = false;
                                window.ShowUsage(result);
                                UpdateTray(result);
                                bool low = result.Rows.Exists(delegate(UsageRow row) { return row.RemainingPercent < 20; });
                                if (low && !wasLow)
                                {
                                    tray.BalloonTipTitle = UiText.Get("lowTitle");
                                    tray.BalloonTipText = UiText.Get("lowBody");
                                    tray.BalloonTipIcon = ToolTipIcon.Warning;
                                    tray.ShowBalloonTip(4500);
                                }
                                wasLow = low;
                            }
                            else
                            {
                                latestResult = null;
                                lastReadFailed = true;
                                window.ShowError(error ?? new UsageReadException("errorGeneric"));
                                SetTrayIcon(null, true);
                                tray.Text = UiText.Get("trayFailed");
                                wasLow = false;
                            }
                        });
                    }
                    catch (InvalidOperationException) { }
                }
                Interlocked.Exchange(ref refreshInProgress, 0);
            });
        }

        private void UpdateTray(UsageResult result)
        {
            UsageRow shortRow = result.Rows.Find(delegate(UsageRow row) { return row.LimitId == "codex" && row.Period == "primary"; });
            UsageRow longRow = result.Rows.Find(delegate(UsageRow row) { return row.LimitId == "codex" && row.Period == "secondary"; });
            SetTrayIcon(shortRow == null ? (int?)null : shortRow.RemainingPercent, false);
            string text = UiText.Get("title");
            if (shortRow != null) text += " · " + UiText.Window(shortRow.WindowMinutes) + " " + shortRow.RemainingPercent + "%";
            if (longRow != null) text += " · " + UiText.Window(longRow.WindowMinutes) + " " + longRow.RemainingPercent + "%";
            if (text.Length > 63) text = text.Substring(0, 60) + "...";
            tray.Text = text;
        }

        private void SetTrayIcon(int? remainingPercent, bool error)
        {
            Icon next = TrayIconFactory.Create(remainingPercent, error);
            Icon previous = currentTrayIcon;
            currentTrayIcon = next;
            tray.Icon = next;
            if (previous != null) previous.Dispose();
        }

        private void ExitApplication()
        {
            refreshTimer.Stop();
            tray.Visible = false;
            tray.Dispose();
            if (currentTrayIcon != null) currentTrayIcon.Dispose();
            window.CloseForExit();
            ExitThread();
        }
    }

    internal sealed class UsageResult
    {
        public string PlanType;
        public int? AvailableResetCount;
        public List<UsageRow> Rows = new List<UsageRow>();
    }

    internal sealed class UsageRow
    {
        public string LimitId;
        public string Period;
        public string Label;
        public int WindowMinutes;
        public int UsedPercent;
        public int RemainingPercent;
        public long ResetsAt;
    }

    internal sealed class UsageReadException : Exception
    {
        public readonly string Key;
        public readonly string Detail;
        public UsageReadException(string key, string detail = null) : base(key)
        {
            Key = key;
            Detail = detail;
        }
    }

    internal static class CodexReader
    {
        private const int TimeoutSeconds = 15;

        public static UsageResult Read()
        {
            Process process = null;
            Queue<string> lines = new Queue<string>();
            object queueLock = new object();
            AutoResetEvent lineArrived = new AutoResetEvent(false);
            try
            {
                string codexPath = FindCodexExecutable();
                if (String.IsNullOrEmpty(codexPath))
                    throw new UsageReadException("errorCliMissing");

                ProcessStartInfo start = new ProcessStartInfo();
                start.FileName = codexPath;
                start.Arguments = "app-server --listen stdio://";
                start.UseShellExecute = false;
                start.CreateNoWindow = true;
                start.RedirectStandardInput = true;
                start.RedirectStandardOutput = true;
                start.RedirectStandardError = true;

                try { process = Process.Start(start); }
                catch (Win32Exception)
                {
                    throw new UsageReadException("errorCliStart");
                }
                if (process == null) throw new UsageReadException("errorServerStart");

                process.OutputDataReceived += delegate(object sender, DataReceivedEventArgs e)
                {
                    if (e.Data == null) return;
                    lock (queueLock) lines.Enqueue(e.Data);
                    lineArrived.Set();
                };
                process.ErrorDataReceived += delegate { };
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.StandardInput.AutoFlush = true;

                Request(process, lines, queueLock, lineArrived, "initialize", 0,
                    "\"params\":{\"clientInfo\":{\"name\":\"codex-usage-monitor\",\"title\":\"Codex Usage Monitor\",\"version\":\"0.1.0\"}}");
                process.StandardInput.WriteLine("{\"method\":\"initialized\",\"params\":{}}");

                Dictionary<string, object> accountResult = Request(process, lines, queueLock, lineArrived,
                    "account/read", 1, "\"params\":{\"refreshToken\":false}");
                Dictionary<string, object> account = GetDictionary(accountResult, "account");
                if (account == null)
                    throw new UsageReadException("errorNoAccount");
                string accountType = GetString(account, "type");
                if (!String.Equals(accountType, "chatgpt", StringComparison.OrdinalIgnoreCase))
                    throw new UsageReadException("errorApiKey");

                Dictionary<string, object> limitsResult = Request(process, lines, queueLock, lineArrived,
                    "account/rateLimits/read", 2, null);
                UsageResult result = new UsageResult();
                result.PlanType = GetString(account, "planType");
                Dictionary<string, object> rateLimits = GetDictionary(limitsResult, "rateLimits");
                if (String.IsNullOrEmpty(result.PlanType) && rateLimits != null)
                    result.PlanType = GetString(rateLimits, "planType");
                if (String.IsNullOrEmpty(result.PlanType)) result.PlanType = "ChatGPT";

                Dictionary<string, object> buckets = GetDictionary(limitsResult, "rateLimitsByLimitId");
                if (buckets != null)
                {
                    foreach (KeyValuePair<string, object> item in buckets)
                        AddBucketRows(result.Rows, item.Key, GetDictionary(item.Value));
                }
                if (result.Rows.Count == 0 && rateLimits != null)
                    AddBucketRows(result.Rows, GetString(rateLimits, "limitId") ?? "codex", rateLimits);
                if (result.Rows.Count == 0)
                    throw new UsageReadException("errorNoLimits");

                Dictionary<string, object> resetCredits = GetDictionary(limitsResult, "rateLimitResetCredits");
                object count;
                if (resetCredits != null && resetCredits.TryGetValue("availableCount", out count))
                {
                    int parsed;
                    if (Int32.TryParse(Convert.ToString(count, CultureInfo.InvariantCulture), NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out parsed)) result.AvailableResetCount = Math.Max(0, parsed);
                }
                return result;
            }
            finally
            {
                if (process != null)
                {
                    try { process.StandardInput.Close(); } catch { }
                    try
                    {
                        if (!process.WaitForExit(900)) process.Kill();
                    }
                    catch { }
                    process.Dispose();
                }
                lineArrived.Dispose();
            }
        }

        private static string FindCodexExecutable()
        {
            string path = Environment.GetEnvironmentVariable("PATH") ?? "";
            string[] pathEntries = path.Split(Path.PathSeparator);
            for (int i = 0; i < pathEntries.Length; i++)
            {
                string entry = pathEntries[i].Trim().Trim('"');
                if (entry.Length == 0) continue;
                string candidate = Path.Combine(entry, "codex.exe");
                if (File.Exists(candidate)) return candidate;
            }

            string installRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OpenAI", "Codex", "bin");
            string direct = Path.Combine(installRoot, "codex.exe");
            if (File.Exists(direct)) return direct;
            if (!Directory.Exists(installRoot)) return null;

            string newest = null;
            DateTime newestWrite = DateTime.MinValue;
            string[] versionDirectories = Directory.GetDirectories(installRoot);
            for (int i = 0; i < versionDirectories.Length; i++)
            {
                string candidate = Path.Combine(versionDirectories[i], "codex.exe");
                if (!File.Exists(candidate)) continue;
                DateTime writeTime = File.GetLastWriteTimeUtc(candidate);
                if (writeTime > newestWrite)
                {
                    newest = candidate;
                    newestWrite = writeTime;
                }
            }
            return newest;
        }

        private static Dictionary<string, object> Request(Process process, Queue<string> lines,
            object queueLock, AutoResetEvent lineArrived, string method, int requestId, string parameters)
        {
            string request = "{\"method\":\"" + method + "\",\"id\":" + requestId;
            if (!String.IsNullOrEmpty(parameters)) request += "," + parameters;
            request += "}";
            process.StandardInput.WriteLine(request);

            DateTime deadline = DateTime.UtcNow.AddSeconds(TimeoutSeconds);
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            while (DateTime.UtcNow < deadline)
            {
                string line = null;
                lock (queueLock)
                {
                    if (lines.Count > 0) line = lines.Dequeue();
                }
                if (line == null)
                {
                    lineArrived.WaitOne(100);
                    continue;
                }
                Dictionary<string, object> message;
                try { message = serializer.Deserialize<Dictionary<string, object>>(line); }
                catch { continue; }
                if (message == null) continue;
                object idValue;
                if (!message.TryGetValue("id", out idValue) ||
                    Convert.ToString(idValue, CultureInfo.InvariantCulture) != requestId.ToString(CultureInfo.InvariantCulture)) continue;
                object error;
                if (message.TryGetValue("error", out error))
                {
                    Dictionary<string, object> errorObject = GetDictionary(error);
                    string detail = errorObject == null ? null : GetString(errorObject, "message");
                    throw new UsageReadException("errorServer", detail);
                }
                return GetDictionary(message, "result") ?? new Dictionary<string, object>();
            }
            throw new UsageReadException("errorTimeout");
        }

        private static void AddBucketRows(List<UsageRow> rows, string key, Dictionary<string, object> bucket)
        {
            if (bucket == null) return;
            string limitId = GetString(bucket, "limitId") ?? key;
            string limitName = GetString(bucket, "limitName");
            AddWindowRow(rows, limitId, limitName, "primary", GetDictionary(bucket, "primary"));
            AddWindowRow(rows, limitId, limitName, "secondary", GetDictionary(bucket, "secondary"));
        }

        private static void AddWindowRow(List<UsageRow> rows, string limitId, string limitName,
            string period, Dictionary<string, object> value)
        {
            if (value == null) return;
            int used;
            int minutes;
            long resetsAt;
            if (!TryGetInt(value, "usedPercent", out used) || !TryGetInt(value, "windowDurationMins", out minutes) ||
                !TryGetLong(value, "resetsAt", out resetsAt) || minutes <= 0 || resetsAt <= 0) return;
            used = Math.Max(0, Math.Min(100, used));
            string label = String.IsNullOrEmpty(limitName) ? limitId.Replace('_', ' ') : limitName;
            UsageRow row = new UsageRow();
            row.LimitId = limitId;
            row.Period = period;
            row.Label = label;
            row.WindowMinutes = minutes;
            row.UsedPercent = used;
            row.RemainingPercent = 100 - used;
            row.ResetsAt = resetsAt;
            rows.Add(row);
        }

        private static bool TryGetInt(Dictionary<string, object> data, string key, out int value)
        {
            object raw;
            double parsed;
            if (data != null && data.TryGetValue(key, out raw) &&
                Double.TryParse(Convert.ToString(raw, CultureInfo.InvariantCulture), NumberStyles.Float,
                    CultureInfo.InvariantCulture, out parsed))
            {
                value = (int)Math.Round(parsed, MidpointRounding.AwayFromZero);
                return true;
            }
            value = 0;
            return false;
        }

        private static bool TryGetLong(Dictionary<string, object> data, string key, out long value)
        {
            object raw;
            if (data != null && data.TryGetValue(key, out raw) &&
                Int64.TryParse(Convert.ToString(raw, CultureInfo.InvariantCulture), NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out value)) return true;
            value = 0;
            return false;
        }

        private static string GetString(Dictionary<string, object> data, string key)
        {
            object value;
            return data != null && data.TryGetValue(key, out value) && value != null ? Convert.ToString(value) : null;
        }

        private static Dictionary<string, object> GetDictionary(Dictionary<string, object> data, string key)
        {
            object value;
            return data != null && data.TryGetValue(key, out value) ? GetDictionary(value) : null;
        }

        private static Dictionary<string, object> GetDictionary(object value)
        {
            return value as Dictionary<string, object>;
        }
    }

    internal sealed class UsageWindow : Form
    {
        private readonly Action refreshAction;
        private readonly Action<string> languageAction;
        private readonly Label titleLabel;
        private readonly Label planLabel;
        private readonly Label connectionLabel;
        private readonly Label resetLabel;
        private readonly Label updatedLabel;
        private readonly StatusDot statusDot;
        private readonly FlowLayoutPanel rowsPanel;
        private readonly Button refreshButton;
        private readonly Button infoButton;
        private readonly Button languageButton;
        private readonly Button closeButton;
        private readonly ContextMenuStrip languageMenu;
        private readonly ToolTip toolTip;
        private UsageResult lastResult;
        private UsageReadException lastError;
        private DateTime lastUpdated;
        private bool allowClose;
        private bool languageMenuOpen;

        public UsageWindow(Action refresh, Action<string> changeLanguage)
        {
            SuspendLayout();
            refreshAction = refresh;
            languageAction = changeLanguage;
            Text = "Codex Usage";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Palette.Panel;
            ForeColor = Palette.Text;
            ClientSize = new Size(326, 284);
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            DoubleBuffered = true;

            Panel brandIcon = new Panel();
            brandIcon.SetBounds(18, 15, 29, 29);
            brandIcon.BackColor = Color.FromArgb(35, 65, 55);
            brandIcon.Paint += delegate(object sender, PaintEventArgs e)
            {
                using (Font f = new Font("Segoe UI", 14F, FontStyle.Bold, GraphicsUnit.Point))
                using (Brush b = new SolidBrush(Palette.Green))
                using (StringFormat format = new StringFormat())
                {
                    format.Alignment = StringAlignment.Center;
                    format.LineAlignment = StringAlignment.Center;
                    e.Graphics.DrawString("C", f, b, brandIcon.ClientRectangle, format);
                }
            };
            Controls.Add(brandIcon);

            titleLabel = AddLabel(UiText.Get("title"), 55, 16, 150, 25, 12F, Palette.Text, true);
            closeButton = new Button();
            closeButton.Text = "×";
            closeButton.SetBounds(298, 15, 18, 23);
            closeButton.FlatStyle = FlatStyle.Flat;
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.BackColor = Color.Transparent;
            closeButton.ForeColor = Palette.Muted;
            closeButton.Font = new Font("Segoe UI", 13F, FontStyle.Regular, GraphicsUnit.Point);
            closeButton.Cursor = Cursors.Hand;
            closeButton.Click += delegate { Hide(); };
            Controls.Add(closeButton);

            languageMenu = new ContextMenuStrip();
            languageMenu.BackColor = Palette.Panel;
            languageMenu.ForeColor = Palette.Text;
            languageMenu.Renderer = new DarkMenuRenderer();
            foreach (string code in UiText.LanguageCodes)
                AddLanguageItem(code, UiText.LanguageName(code));
            languageMenu.Items.Add(new ToolStripSeparator());
            AddLanguageItem("system", null);
            languageMenu.Closed += delegate { languageMenuOpen = false; };
            languageButton = new Button();
            languageButton.Text = "";
            languageButton.SetBounds(269, 17, 22, 22);
            languageButton.FlatStyle = FlatStyle.Flat;
            languageButton.FlatAppearance.BorderSize = 0;
            languageButton.BackColor = Color.Transparent;
            languageButton.ForeColor = Palette.Muted;
            languageButton.Cursor = Cursors.Hand;
            languageButton.Paint += delegate(object sender, PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                float size = Math.Min(languageButton.Width, languageButton.Height) * 0.55F;
                float x = (languageButton.Width - size) / 2F;
                float y = (languageButton.Height - size) / 2F;
                using (Pen pen = new Pen(Palette.Muted, Math.Max(1F, DeviceDpi / 96F)))
                {
                    e.Graphics.DrawEllipse(pen, x, y, size, size);
                    e.Graphics.DrawEllipse(pen, x + size * .28F, y, size * .44F, size);
                    e.Graphics.DrawLine(pen, x, y + size / 2F, x + size, y + size / 2F);
                }
            };
            languageButton.Click += delegate
            {
                UpdateLanguageMenu();
                languageMenuOpen = true;
                languageMenu.Show(languageButton, new Point(0, languageButton.Height));
            };
            Controls.Add(languageButton);

            statusDot = new StatusDot();
            statusDot.SetBounds(19, 54, 7, 7);
            Controls.Add(statusDot);
            connectionLabel = AddLabel(UiText.Get("connecting"), 33, 48, 174, 20, 8F, Palette.Muted, false);
            planLabel = AddLabel(UiText.Get("reading"), 206, 17, 56, 23, 8F, Palette.Muted, true);
            planLabel.TextAlign = ContentAlignment.MiddleRight;
            updatedLabel = AddLabel(UiText.Get("waiting"), 211, 48, 97, 20, 7F, Palette.Dim, false);
            updatedLabel.TextAlign = ContentAlignment.MiddleRight;

            rowsPanel = new FlowLayoutPanel();
            rowsPanel.SetBounds(18, 76, 290, 158);
            rowsPanel.FlowDirection = FlowDirection.TopDown;
            rowsPanel.WrapContents = false;
            rowsPanel.AutoScroll = true;
            rowsPanel.BackColor = Palette.Panel;
            rowsPanel.Margin = new Padding(0);
            rowsPanel.Padding = new Padding(0);
            Controls.Add(rowsPanel);
            ShowEmpty(UiText.Get("loadingUsage"), false);

            resetLabel = AddLabel(UiText.Get("readingResets"), 18, 242, 150, 25, 7F, Palette.Muted, false);

            refreshButton = CreateButton(UiText.Get("refresh"), 241, 240, 67, 30, true);
            refreshButton.Click += delegate { refreshAction(); };
            Controls.Add(refreshButton);
            infoButton = CreateButton(UiText.Get("info"), 176, 240, 58, 30, false);
            infoButton.Click += delegate
            {
                MessageBox.Show(this, UiText.Get("aboutBody"), UiText.Get("aboutTitle"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            Controls.Add(infoButton);

            toolTip = new ToolTip();
            ApplyToolTips();

            Deactivate += delegate { if (Visible && !languageMenuOpen) Hide(); };
            FormClosing += delegate(object sender, FormClosingEventArgs e)
            {
                if (!allowClose && e.CloseReason == CloseReason.UserClosing)
                {
                    e.Cancel = true;
                    Hide();
                }
            };
            SizeChanged += delegate
            {
                if (ClientSize.Width > 0 && ClientSize.Height > 0)
                    Region = RoundedRegion(ClientSize.Width, ClientSize.Height,
                        Math.Max(14, ClientSize.Width * 14 / 326));
            };
            brandIcon.SizeChanged += delegate
            {
                if (brandIcon.Width > 0 && brandIcon.Height > 0)
                    brandIcon.Region = RoundedRegion(brandIcon.Width, brandIcon.Height,
                        Math.Max(8, brandIcon.Width * 8 / 29));
            };

            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ResumeLayout(false);
            PerformLayout();
            Region = RoundedRegion(ClientSize.Width, ClientSize.Height,
                Math.Max(14, ClientSize.Width * 14 / 326));
            brandIcon.Region = RoundedRegion(brandIcon.Width, brandIcon.Height,
                Math.Max(8, brandIcon.Width * 8 / 29));
        }

        public void BeginRefresh()
        {
            refreshButton.Enabled = false;
            updatedLabel.Text = UiText.Get("updating");
        }

        public void ShowUsage(UsageResult result, bool updateTime = true)
        {
            lastResult = result;
            lastError = null;
            if (updateTime) lastUpdated = DateTime.Now;
            statusDot.IsError = false;
            statusDot.Invalidate();
            connectionLabel.Text = UiText.Get("connected");
            string plan = String.IsNullOrEmpty(result.PlanType) ? "ChatGPT" : result.PlanType;
            planLabel.Text = Char.ToUpperInvariant(plan[0]) + plan.Substring(1);
            planLabel.ForeColor = Palette.Muted;
            rowsPanel.SuspendLayout();
            ClearRows();
            float scale = DeviceDpi / 96F;
            for (int i = 0; i < result.Rows.Count; i++)
            {
                UsageRow row = result.Rows[i];
                UsageRowControl rowControl = new UsageRowControl(row, scale);
                rowControl.Width = rowsPanel.ClientSize.Width;
                rowsPanel.Controls.Add(rowControl);
            }
            rowsPanel.ResumeLayout();
            if (result.AvailableResetCount.HasValue && result.AvailableResetCount.Value > 0)
                resetLabel.Text = UiText.ResetCredits(result.AvailableResetCount.Value);
            else
                resetLabel.Text = UiText.Get("noResets");
            updatedLabel.Text = UiText.Format("updatedAt", lastUpdated.ToString("HH:mm"));
            refreshButton.Enabled = true;
        }

        public void ShowError(UsageReadException error)
        {
            lastResult = null;
            lastError = error;
            statusDot.IsError = true;
            statusDot.Invalidate();
            connectionLabel.Text = UiText.Get("readFailed");
            planLabel.Text = UiText.Get("notConnected");
            planLabel.ForeColor = Palette.Amber;
            ShowEmpty(UiText.Error(error) + "\r\n\r\n" + UiText.Get("retryHint"), true);
            resetLabel.Text = UiText.Get("readFailed");
            updatedLabel.Text = UiText.Get("readFailed");
            refreshButton.Enabled = true;
        }

        public void ApplyLanguage()
        {
            titleLabel.Text = UiText.Get("title");
            refreshButton.Text = UiText.Get("refresh");
            infoButton.Text = UiText.Get("info");
            foreach (Control control in new Control[] { titleLabel, connectionLabel, planLabel,
                updatedLabel, resetLabel, refreshButton, infoButton })
            {
                Font old = control.Font;
                control.Font = new Font(UiText.FontName, old.SizeInPoints, old.Style, GraphicsUnit.Point);
                old.Dispose();
            }
            ApplyToolTips();
            UpdateLanguageMenu();
            if (lastResult != null) ShowUsage(lastResult, false);
            else if (lastError != null) ShowError(lastError);
            else
            {
                connectionLabel.Text = UiText.Get("connecting");
                planLabel.Text = UiText.Get("reading");
                updatedLabel.Text = UiText.Get("waiting");
                resetLabel.Text = UiText.Get("readingResets");
                ShowEmpty(UiText.Get("loadingUsage"), false);
            }
        }

        private void AddLanguageItem(string code, string label)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(label);
            item.Tag = code;
            item.Click += delegate { languageAction(code); };
            languageMenu.Items.Add(item);
        }

        private void UpdateLanguageMenu()
        {
            foreach (ToolStripItem menuItem in languageMenu.Items)
            {
                ToolStripMenuItem item = menuItem as ToolStripMenuItem;
                if (item == null) continue;
                if ((string)item.Tag == "system") item.Text = UiText.Get("systemLanguage");
                item.Checked = (string)item.Tag == UiText.Preference;
            }
        }

        private void ApplyToolTips()
        {
            toolTip.SetToolTip(languageButton, UiText.Get("languageTooltip"));
            toolTip.SetToolTip(closeButton, UiText.Get("closeTooltip"));
            toolTip.SetToolTip(refreshButton, UiText.Get("refreshTooltip"));
            toolTip.SetToolTip(infoButton, UiText.Get("infoTooltip"));
        }

        public void CloseForExit()
        {
            allowClose = true;
            Close();
        }

        private void ShowEmpty(string message, bool error)
        {
            rowsPanel.SuspendLayout();
            ClearRows();
            Label label = new Label();
            label.Text = message;
            label.ForeColor = error ? Palette.Error : Palette.Muted;
            label.Font = new Font(UiText.FontName, 9F, FontStyle.Regular, GraphicsUnit.Point);
            label.SetBounds(0, 4, Math.Max(1, rowsPanel.ClientSize.Width - 18), rowsPanel.ClientSize.Height - 8);
            label.AutoSize = false;
            label.TextAlign = ContentAlignment.TopLeft;
            rowsPanel.Controls.Add(label);
            rowsPanel.ResumeLayout();
        }

        private void ClearRows()
        {
            while (rowsPanel.Controls.Count > 0)
            {
                Control child = rowsPanel.Controls[0];
                rowsPanel.Controls.RemoveAt(0);
                child.Dispose();
            }
        }

        private Label AddLabel(string text, int x, int y, int width, int height, float size, Color color, bool bold)
        {
            Label label = new Label();
            label.Text = text;
            label.SetBounds(x, y, width, height);
            label.ForeColor = color;
            label.BackColor = Color.Transparent;
            label.Font = new Font(UiText.FontName, size, bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Point);
            label.AutoSize = false;
            label.TextAlign = ContentAlignment.MiddleLeft;
            Controls.Add(label);
            return label;
        }

        private Button CreateButton(string text, int x, int y, int width, int height, bool primary)
        {
            Button button = new Button();
            button.Text = text;
            button.SetBounds(x, y, width, height);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = primary ? 1 : 0;
            button.FlatAppearance.BorderColor = primary ? Palette.Green : Palette.Panel;
            button.BackColor = Palette.Panel;
            button.ForeColor = primary ? Palette.Green : Palette.Muted;
            button.Font = new Font(UiText.FontName, 8.5F, FontStyle.Bold, GraphicsUnit.Point);
            button.Cursor = Cursors.Hand;
            return button;
        }

        private static Region RoundedRegion(int width, int height, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            path.AddArc(0, 0, diameter, diameter, 180, 90);
            path.AddArc(width - diameter - 1, 0, diameter, diameter, 270, 90);
            path.AddArc(width - diameter - 1, height - diameter - 1, diameter, diameter, 0, 90);
            path.AddArc(0, height - diameter - 1, diameter, diameter, 90, 90);
            path.CloseFigure();
            Region region = new Region(path);
            path.Dispose();
            return region;
        }
    }

    internal sealed class UsageRowControl : Panel
    {
        private readonly UsageRow row;
        private readonly ProgressTrack progress;

        public UsageRowControl(UsageRow value, float scale)
        {
            row = value;
            Func<int, int> px = delegate(int coordinate) { return (int)Math.Round(coordinate * scale); };
            Width = px(272);
            Height = px(76);
            Margin = new Padding(0);
            Padding = new Padding(0);
            BackColor = Palette.Panel;
            Label title = MakeLabel(UiText.WindowLabel(value.WindowMinutes), 0, px(4), px(155), px(22), 9F, Palette.Text, true);
            Controls.Add(title);
            Label remaining = MakeLabel(value.RemainingPercent + "%", px(167), px(4), px(105), px(22),
                9F,
                value.RemainingPercent < 20 ? Palette.Amber : Palette.Green, true);
            remaining.TextAlign = ContentAlignment.MiddleRight;
            remaining.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            Controls.Add(remaining);
            progress = new ProgressTrack(value.RemainingPercent, value.RemainingPercent < 20);
            progress.SetBounds(0, px(37), px(272), Math.Max(2, px(5)));
            progress.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(progress);
            Label reset = MakeLabel(UiText.Format("resetAt", UiText.ResetTime(value.ResetsAt)), 0, px(49), px(272), px(18), 7F, Palette.Muted, false);
            reset.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(reset);
            Paint += delegate(object sender, PaintEventArgs e)
            {
                using (Pen pen = new Pen(Palette.Line)) e.Graphics.DrawLine(pen, 0, Height - 1, Width, Height - 1);
            };
        }

        private static Label MakeLabel(string text, int x, int y, int width, int height, float size, Color color, bool bold)
        {
            Label label = new Label();
            label.Text = text;
            label.SetBounds(x, y, width, height);
            label.ForeColor = color;
            label.BackColor = Palette.Panel;
            label.Font = new Font(UiText.FontName, size, bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Point);
            label.AutoSize = false;
            label.TextAlign = ContentAlignment.MiddleLeft;
            return label;
        }

    }

    internal sealed class ProgressTrack : Control
    {
        private readonly int percent;
        private readonly bool low;

        public ProgressTrack(int value, bool isLow)
        {
            percent = Math.Max(0, Math.Min(100, value));
            low = isLow;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            BackColor = Palette.Panel;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle track = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(3, Height - 1));
            using (Brush background = new SolidBrush(Palette.Track))
                FillRounded(e.Graphics, background, track, 3);
            int fillWidth = (int)Math.Round(track.Width * (percent / 100.0));
            if (fillWidth > 0)
            {
                Rectangle fill = new Rectangle(0, 0, Math.Min(track.Width, Math.Max(5, fillWidth)), track.Height);
                using (Brush foreground = new SolidBrush(low ? Palette.Amber : Palette.GreenStrong))
                    FillRounded(e.Graphics, foreground, fill, 3);
            }
            base.OnPaint(e);
        }

        private static void FillRounded(Graphics graphics, Brush brush, Rectangle rect, int radius)
        {
            using (GraphicsPath path = new GraphicsPath())
            {
                int d = Math.Min(radius * 2, rect.Height);
                path.AddArc(rect.X, rect.Y, d, d, 180, 90);
                path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
                path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
                path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
                path.CloseFigure();
                graphics.FillPath(brush, path);
            }
        }
    }

    internal sealed class StatusDot : Control
    {
        public bool IsError;
        public StatusDot() { SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true); }
        protected override void OnPaint(PaintEventArgs e)
        {
            using (Brush brush = new SolidBrush(IsError ? Palette.Error : Palette.GreenStrong))
                e.Graphics.FillEllipse(brush, 1, 1, Width - 2, Height - 2);
        }
    }

    internal static class TrayIconFactory
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr handle);

        public static Icon Create(int? remainingPercent, bool error)
        {
            using (Bitmap bitmap = new Bitmap(32, 32))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Color accent = error ? Palette.Error :
                    remainingPercent.HasValue && remainingPercent.Value < 20 ? Palette.Amber : Palette.Green;
                using (Brush background = new SolidBrush(Color.FromArgb(25, 43, 36)))
                    graphics.FillRoundedRectangle(background, new Rectangle(1, 1, 30, 30), 8);
                using (Pen border = new Pen(accent, 1.2F))
                    graphics.DrawRoundedRectangle(border, new Rectangle(1, 1, 30, 30), 8);
                string text = remainingPercent.HasValue ? remainingPercent.Value.ToString(CultureInfo.InvariantCulture) : error ? "!" : "C";
                float fontSize = text.Length >= 3 ? 13F : text == "C" ? 19F : 18F;
                using (Font font = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel))
                using (Brush foreground = new SolidBrush(accent))
                using (StringFormat format = new StringFormat())
                {
                    format.Alignment = StringAlignment.Center;
                    format.LineAlignment = StringAlignment.Center;
                    graphics.DrawString(text, font, foreground, new RectangleF(1, 1, 30, 30), format);
                }
                IntPtr handle = bitmap.GetHicon();
                try { return (Icon)Icon.FromHandle(handle).Clone(); }
                finally { DestroyIcon(handle); }
            }
        }
    }

    internal static class GraphicsExtensions
    {
        public static void FillRoundedRectangle(this Graphics graphics, Brush brush, Rectangle bounds, int radius)
        {
            using (GraphicsPath path = RoundedPath(bounds, radius)) graphics.FillPath(brush, path);
        }

        public static void DrawRoundedRectangle(this Graphics graphics, Pen pen, Rectangle bounds, int radius)
        {
            using (GraphicsPath path = RoundedPath(bounds, radius)) graphics.DrawPath(pen, path);
        }

        private static GraphicsPath RoundedPath(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    internal static class Palette
    {
        public static readonly Color Panel = Color.FromArgb(20, 28, 35);
        public static readonly Color Button = Color.FromArgb(35, 45, 52);
        public static readonly Color ButtonText = Color.FromArgb(16, 35, 27);
        public static readonly Color Text = Color.FromArgb(239, 245, 245);
        public static readonly Color Muted = Color.FromArgb(169, 183, 189);
        public static readonly Color Dim = Color.FromArgb(122, 139, 148);
        public static readonly Color Line = Color.FromArgb(51, 63, 70);
        public static readonly Color Track = Color.FromArgb(47, 60, 66);
        public static readonly Color Green = Color.FromArgb(171, 233, 198);
        public static readonly Color GreenStrong = Color.FromArgb(113, 216, 158);
        public static readonly Color Amber = Color.FromArgb(239, 197, 125);
        public static readonly Color Error = Color.FromArgb(226, 140, 130);
    }

    internal sealed class DarkMenuColors : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground { get { return Palette.Panel; } }
        public override Color ImageMarginGradientBegin { get { return Palette.Panel; } }
        public override Color ImageMarginGradientMiddle { get { return Palette.Panel; } }
        public override Color ImageMarginGradientEnd { get { return Palette.Panel; } }
        public override Color MenuItemSelected { get { return Palette.Button; } }
        public override Color MenuItemSelectedGradientBegin { get { return Palette.Button; } }
        public override Color MenuItemSelectedGradientEnd { get { return Palette.Button; } }
        public override Color MenuItemPressedGradientBegin { get { return Palette.Button; } }
        public override Color MenuItemPressedGradientEnd { get { return Palette.Button; } }
        public override Color MenuItemBorder { get { return Palette.Line; } }
        public override Color MenuBorder { get { return Palette.Line; } }
        public override Color SeparatorDark { get { return Palette.Line; } }
        public override Color SeparatorLight { get { return Palette.Panel; } }
        public override Color CheckBackground { get { return Palette.Button; } }
        public override Color CheckSelectedBackground { get { return Palette.Button; } }
        public override Color CheckPressedBackground { get { return Palette.Button; } }
    }

    internal sealed class DarkMenuRenderer : ToolStripProfessionalRenderer
    {
        public DarkMenuRenderer() : base(new DarkMenuColors()) { }

        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            Rectangle box = e.ImageRectangle;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen pen = new Pen(Palette.Green, 2F))
            {
                e.Graphics.DrawLines(pen, new Point[] {
                    new Point(box.Left + box.Width * 2 / 10, box.Top + box.Height * 5 / 10),
                    new Point(box.Left + box.Width * 4 / 10, box.Top + box.Height * 7 / 10),
                    new Point(box.Left + box.Width * 8 / 10, box.Top + box.Height * 3 / 10)
                });
            }
        }
    }

}
