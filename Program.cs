using System.Drawing;

namespace TestsigmaDemoApp
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Login window and Dashboard window are two separate top-level
            // windows. Each runs its own Application.Run() cycle, so control
            // returns here every time one of them closes. That makes it easy
            // to chain Login -> Dashboard -> (logout) -> Login again.
            while (true)
            {
                bool loggedIn;
                string username;

                using (var login = new LoginForm())
                {
                    Application.Run(login);
                    loggedIn = login.LoginSucceeded;
                    username = login.Username;
                }

                if (!loggedIn)
                {
                    break; // user closed the Login window without signing in
                }

                bool loggedOut;
                using (var dashboard = new DashboardForm(username))
                {
                    Application.Run(dashboard);
                    loggedOut = dashboard.RequestedLogout;
                }

                if (!loggedOut)
                {
                    break; // user closed the Dashboard window directly
                }
                // else: loop back around and show the Login window again
            }
        }
    }

    /// <summary>
    /// Small opacity-animation helper that makes a Form "wink" in or out of
    /// view instead of appearing/disappearing instantly. Uses only a
    /// System.Windows.Forms.Timer + Form.Opacity, so it needs no extra
    /// packages beyond the Windows Forms runtime itself.
    /// </summary>
    internal static class FadeHelper
    {
        private const int DefaultDurationMs = 220;
        private const int IntervalMs = 15;

        private static void AnimateOpacity(Form form, double from, double to, int durationMs, Action? onComplete = null)
        {
            int steps = Math.Max(1, durationMs / IntervalMs);
            double stepSize = (to - from) / steps;
            int currentStep = 0;
            form.Opacity = from;

            var timer = new System.Windows.Forms.Timer { Interval = IntervalMs };
            timer.Tick += (s, e) =>
            {
                currentStep++;
                form.Opacity = currentStep >= steps ? to : from + (stepSize * currentStep);

                if (currentStep >= steps)
                {
                    timer.Stop();
                    timer.Dispose();
                    onComplete?.Invoke();
                }
            };
            timer.Start();
        }

        /// <summary>Call once in a Form's constructor. Fades 0 -> 1 the moment the form is first shown.</summary>
        public static void FadeInOnShow(Form form, int durationMs = DefaultDurationMs)
        {
            form.Opacity = 0;
            form.Shown += (s, e) => AnimateOpacity(form, 0, 1, durationMs);
        }

        /// <summary>Winks the same window out, runs swapAction (e.g. change Text + content), then winks back in.</summary>
        public static void SwapWithFade(Form form, Action swapAction, int durationMs = DefaultDurationMs)
        {
            AnimateOpacity(form, form.Opacity, 0, durationMs / 2, () =>
            {
                swapAction();
                AnimateOpacity(form, 0, 1, durationMs / 2);
            });
        }

        /// <summary>Fades the window out, then closes it.</summary>
        public static void FadeOutThenClose(Form form, int durationMs = DefaultDurationMs)
        {
            AnimateOpacity(form, form.Opacity, 0, durationMs, form.Close);
        }
    }

    public class LoginForm : Form
    {
        private readonly ComboBox _environmentDropdown = new()
        {
            Name = "EnvironmentComboBox",
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        private readonly TextBox _usernameBox = new() { Name = "UsernameTextBox" };
        private readonly TextBox _passwordBox = new() { Name = "PasswordTextBox", PasswordChar = '*' };
        private readonly CheckBox _rememberMeCheckBox = new() { Name = "RememberMeCheckBox", Text = "Remember me" };
        private readonly LinkLabel _forgotPasswordLink = new() { Name = "ForgotPasswordLink", Text = "Forgot password?" };
        private readonly Button _loginButton = new() { Name = "LoginButton", Text = "Log In" };
        private readonly Label _statusLabel = new() { Name = "StatusLabel", ForeColor = Color.Firebrick };
        private readonly Label _testNoticeLabel = new()
        {
            Name = "TestNoticeLabel",
            ForeColor = Color.FromArgb(0x85, 0x4F, 0x0B),
            Font = new Font("Segoe UI", 8.5F, FontStyle.Italic)
        };

        /// <summary>True once the demo credentials have been accepted.</summary>
        public bool LoginSucceeded { get; private set; }

        /// <summary>The username entered, passed on to the Dashboard.</summary>
        public string Username { get; private set; } = string.Empty;

        public LoginForm()
        {
            Name = "LoginForm";
            Text = "Sign In — Testsigma Demo App";
            ClientSize = new Size(420, 430);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = Color.White;

            BuildLayout();
            FadeHelper.FadeInOnShow(this);
        }

        private void BuildLayout()
        {
            var banner = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = ColorTranslator.FromHtml("#5B2A86") };
            var bannerLabel = new Label
            {
                Text = "TESTSIGMA DEMO",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            banner.Controls.Add(bannerLabel);

            var envLabel = new Label { Text = "Environment", Left = 40, Top = 88, Width = 340 };
            _environmentDropdown.Left = 40; _environmentDropdown.Top = 110; _environmentDropdown.Width = 340;
            _environmentDropdown.Items.AddRange(new object[] { "QA", "Staging", "Production" });
            _environmentDropdown.SelectedIndex = 0;

            var userLabel = new Label { Text = "Username", Left = 40, Top = 148, Width = 340 };
            _usernameBox.Left = 40; _usernameBox.Top = 170; _usernameBox.Width = 340; _usernameBox.Text = "demo";

            var passLabel = new Label { Text = "Password", Left = 40, Top = 203, Width = 340 };
            _passwordBox.Left = 40; _passwordBox.Top = 225; _passwordBox.Width = 340; _passwordBox.Text = "demo";

            _rememberMeCheckBox.Left = 40; _rememberMeCheckBox.Top = 258; _rememberMeCheckBox.Width = 160;

            _forgotPasswordLink.Left = 220; _forgotPasswordLink.Top = 258; _forgotPasswordLink.Width = 160;
            _forgotPasswordLink.TextAlign = ContentAlignment.MiddleRight;
            _forgotPasswordLink.LinkClicked += OnForgotPasswordClicked;

            _loginButton.Left = 40; _loginButton.Top = 292; _loginButton.Width = 340; _loginButton.Height = 34;
            _loginButton.BackColor = ColorTranslator.FromHtml("#5B2A86");
            _loginButton.ForeColor = Color.White;
            _loginButton.FlatStyle = FlatStyle.Flat;
            _loginButton.FlatAppearance.BorderSize = 0;
            _loginButton.Click += OnLoginClicked;

            _statusLabel.Left = 40; _statusLabel.Top = 334; _statusLabel.Width = 340;

            // Makes clear this is a test/demo app, not a real login screen.
            _testNoticeLabel.Left = 40; _testNoticeLabel.Top = 362; _testNoticeLabel.Width = 340; _testNoticeLabel.Height = 56;
            _testNoticeLabel.Text = "Demo credentials only — any non-empty username and password " +
                                     "will sign in. This is a test application, not connected to a " +
                                     "real system.";

            Controls.Add(banner);
            Controls.Add(envLabel);
            Controls.Add(_environmentDropdown);
            Controls.Add(userLabel);
            Controls.Add(_usernameBox);
            Controls.Add(passLabel);
            Controls.Add(_passwordBox);
            Controls.Add(_rememberMeCheckBox);
            Controls.Add(_forgotPasswordLink);
            Controls.Add(_loginButton);
            Controls.Add(_statusLabel);
            Controls.Add(_testNoticeLabel);

            AcceptButton = _loginButton;
        }

        private void OnLoginClicked(object? sender, EventArgs e)
        {
            if (_usernameBox.Text.Trim().Length == 0 || _passwordBox.Text.Trim().Length == 0)
            {
                _statusLabel.Text = "Enter a username and password.";
                return;
            }

            LoginSucceeded = true;
            Username = _usernameBox.Text.Trim();
            _loginButton.Enabled = false;
            FadeHelper.FadeOutThenClose(this);
        }

        private void OnForgotPasswordClicked(object? sender, LinkLabelLinkClickedEventArgs e)
        {
            // A second, independent flow: opens a small modal dialog rather
            // than navigating within this window. Good for exercising
            // Testsigma's handling of popup/dialog windows.
            using var dialog = new ForgotPasswordForm();
            dialog.ShowDialog(this);
        }
    }

    public class ForgotPasswordForm : Form
    {
        private readonly TextBox _emailBox = new() { Name = "ResetEmailTextBox" };
        private readonly Button _sendButton = new() { Name = "SendResetLinkButton", Text = "Send reset link" };
        private readonly Label _messageLabel = new() { Name = "ResetMessageLabel", ForeColor = Color.Firebrick };

        public ForgotPasswordForm()
        {
            Name = "ForgotPasswordForm";
            Text = "Reset Password — Testsigma Demo App";
            ClientSize = new Size(360, 200);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;

            var infoLabel = new Label
            {
                Text = "Enter your email and we'll send a reset link.",
                Left = 20,
                Top = 20,
                Width = 320,
                Height = 34
            };

            _emailBox.Left = 20; _emailBox.Top = 60; _emailBox.Width = 320;

            _sendButton.Left = 20; _sendButton.Top = 96; _sendButton.Width = 320; _sendButton.Height = 32;
            _sendButton.BackColor = ColorTranslator.FromHtml("#5B2A86");
            _sendButton.ForeColor = Color.White;
            _sendButton.FlatStyle = FlatStyle.Flat;
            _sendButton.FlatAppearance.BorderSize = 0;
            _sendButton.Click += OnSendClicked;

            _messageLabel.Left = 20; _messageLabel.Top = 136; _messageLabel.Width = 320; _messageLabel.Height = 34;

            var noteLabel = new Label
            {
                Name = "ResetNoticeLabel",
                Text = "Test application — no email is actually sent.",
                Left = 20,
                Top = 172,
                Width = 320,
                ForeColor = Color.FromArgb(0x85, 0x4F, 0x0B),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic)
            };

            Controls.Add(infoLabel);
            Controls.Add(_emailBox);
            Controls.Add(_sendButton);
            Controls.Add(_messageLabel);
            Controls.Add(noteLabel);

            AcceptButton = _sendButton;
            FadeHelper.FadeInOnShow(this);
        }

        private void OnSendClicked(object? sender, EventArgs e)
        {
            if (_emailBox.Text.Trim().Length == 0 || !_emailBox.Text.Contains('@'))
            {
                _messageLabel.ForeColor = Color.Firebrick;
                _messageLabel.Text = "Enter a valid email address.";
                return;
            }

            _messageLabel.ForeColor = ColorTranslator.FromHtml("#3B6D11");
            _messageLabel.Text = $"Reset link sent to {_emailBox.Text.Trim()} (simulated).";
            _sendButton.Enabled = false;
        }
    }

    public class DashboardForm : Form
    {
        private static readonly string[] Pages = { "Dashboard", "Test Plans", "Test Cases", "Reports", "Profile", "Settings" };

        private readonly string _username;
        private readonly Panel _contentPanel = new() { Dock = DockStyle.Fill, BackColor = Color.White };
        private readonly Label _pageTitleLabel = new() { Name = "PageTitleLabel", Dock = DockStyle.Top, Height = 44 };
        private readonly Panel _sidebar = new() { Dock = DockStyle.Left, Width = 170, BackColor = ColorTranslator.FromHtml("#F4F1FA") };
        private readonly Panel _testBanner = new() { Dock = DockStyle.Bottom, Height = 26, BackColor = ColorTranslator.FromHtml("#FAEEDA") };

        /// <summary>True if the user clicked Logout (vs. closing the window directly).</summary>
        public bool RequestedLogout { get; private set; }

        public DashboardForm(string username)
        {
            _username = username;
            Name = "DashboardForm";
            ClientSize = new Size(760, 460);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;

            BuildLayout();
            NavigateTo("Dashboard", animate: false);
            FadeHelper.FadeInOnShow(this);
        }

        private void BuildLayout()
        {
            int top = 20;
            foreach (var page in Pages)
            {
                var btn = new Button
                {
                    Text = page,
                    Name = $"Nav{page.Replace(" ", string.Empty)}Button",
                    Left = 15,
                    Top = top,
                    Width = 140,
                    Height = 34,
                    FlatStyle = FlatStyle.Flat,
                    TextAlign = ContentAlignment.MiddleLeft
                };
                btn.Click += (s, e) => NavigateTo(page);
                _sidebar.Controls.Add(btn);
                top += 42;
            }

            var logoutButton = new Button
            {
                Text = "Logout",
                Name = "LogoutButton",
                Left = 15,
                Top = 400,
                Width = 140,
                Height = 34,
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorTranslator.FromHtml("#5B2A86"),
                ForeColor = Color.White
            };
            logoutButton.FlatAppearance.BorderSize = 0;
            logoutButton.Click += OnLogoutClicked;
            _sidebar.Controls.Add(logoutButton);

            _pageTitleLabel.Font = new Font("Segoe UI", 13, FontStyle.Bold);
            _pageTitleLabel.TextAlign = ContentAlignment.MiddleLeft;
            _pageTitleLabel.Padding = new Padding(20, 0, 0, 0);
            _pageTitleLabel.BackColor = Color.White;

            var bannerLabel = new Label
            {
                Name = "TestBannerLabel",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = ColorTranslator.FromHtml("#854F0B"),
                Text = "Test application — sample data only, not connected to a real system."
            };
            _testBanner.Controls.Add(bannerLabel);

            // Add order matters for docking: Fill first, then the outer-edge
            // docked panels last, so the sidebar spans the full height and
            // the banner spans the full width beneath the content area.
            Controls.Add(_contentPanel);
            Controls.Add(_pageTitleLabel);
            Controls.Add(_testBanner);
            Controls.Add(_sidebar);
        }

        /// <summary>
        /// Switches the visible page. This updates the window's Text (title)
        /// every time, and — unless animate is false — winks the window out
        /// and back in around the swap.
        /// </summary>
        private void NavigateTo(string page, bool animate = true)
        {
            void ApplyPage()
            {
                Text = $"{page} — Testsigma Demo App";
                _pageTitleLabel.Text = page == "Dashboard" ? $"Welcome, {_username}" : page;
                _contentPanel.Controls.Clear();
                _contentPanel.Controls.Add(BuildPageContent(page));
            }

            if (!animate)
            {
                ApplyPage();
                return;
            }

            FadeHelper.SwapWithFade(this, ApplyPage);
        }

        private Control BuildPageContent(string page)
        {
            var container = new Panel { Dock = DockStyle.Fill, Name = $"{page.Replace(" ", string.Empty)}Panel" };
            var info = new Label
            {
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(20, 10, 0, 0),
                Text = page switch
                {
                    "Test Plans" => "Sample Test Plans (placeholder data for automation).",
                    "Test Cases" => "Sample Test Cases (placeholder data for automation).",
                    "Reports" => "Sample Reports summary (placeholder data for automation).",
                    "Profile" => "Sample profile settings (placeholder controls for automation).",
                    "Settings" => "Sample application settings (placeholder controls for automation).",
                    _ => "This is the Dashboard home page."
                }
            };
            container.Controls.Add(info);

            if (page == "Settings")
            {
                var themeLabel = new Label { Text = "Theme", Left = 20, Top = 58, Width = 100 };
                var themeDropdown = new ComboBox
                {
                    Name = "ThemeComboBox",
                    Left = 20,
                    Top = 80,
                    Width = 220,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                themeDropdown.Items.AddRange(new object[] { "Light", "Dark", "System" });
                themeDropdown.SelectedIndex = 0;

                var notifyCheck = new CheckBox { Text = "Enable notifications", Name = "NotificationsCheckBox", Left = 20, Top = 118 };
                var darkModeCheck = new CheckBox { Text = "Compact layout", Name = "CompactLayoutCheckBox", Left = 20, Top = 148 };
                container.Controls.Add(themeLabel);
                container.Controls.Add(themeDropdown);
                container.Controls.Add(notifyCheck);
                container.Controls.Add(darkModeCheck);
            }
            else if (page == "Profile")
            {
                var roleLabel = new Label { Text = "Role", Left = 20, Top = 58, Width = 100 };
                var roleDropdown = new ComboBox
                {
                    Name = "RoleComboBox",
                    Left = 20,
                    Top = 80,
                    Width = 220,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                roleDropdown.Items.AddRange(new object[] { "Admin", "Tester", "Viewer" });
                roleDropdown.SelectedIndex = 0;

                var saveButton = new Button
                {
                    Text = "Save",
                    Name = "SaveProfileButton",
                    Left = 20,
                    Top = 118,
                    Width = 100,
                    Height = 30,
                    FlatStyle = FlatStyle.Flat
                };
                var savedLabel = new Label
                {
                    Name = "ProfileSavedLabel",
                    Left = 130,
                    Top = 124,
                    Width = 200,
                    ForeColor = ColorTranslator.FromHtml("#3B6D11"),
                    Text = string.Empty
                };
                saveButton.Click += (s, e) => savedLabel.Text = $"Saved — role set to {roleDropdown.SelectedItem}.";

                container.Controls.Add(roleLabel);
                container.Controls.Add(roleDropdown);
                container.Controls.Add(saveButton);
                container.Controls.Add(savedLabel);
            }
            else if (page is "Test Plans" or "Test Cases" or "Reports")
            {
                var list = new ListBox
                {
                    Left = 20,
                    Top = 60,
                    Width = 500,
                    Height = 320,
                    Name = $"{page.Replace(" ", string.Empty)}ListBox"
                };
                for (int i = 1; i <= 5; i++)
                {
                    list.Items.Add($"{page} Item {i}");
                }
                container.Controls.Add(list);
            }

            return container;
        }

        private void OnLogoutClicked(object? sender, EventArgs e)
        {
            RequestedLogout = true;
            FadeHelper.FadeOutThenClose(this);
        }
    }
}
