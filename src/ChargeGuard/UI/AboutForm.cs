using System.Windows.Forms;

namespace ChargeGuard.UI;

/// <summary>
/// About dialog for ChargeGuard.
/// </summary>
public partial class AboutForm : Form
{
    public AboutForm()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.SuspendLayout();

        // Form properties
        this.Text = "About ChargeGuard";
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.StartPosition = FormStartPosition.CenterParent;
        this.ClientSize = new Size(400, 300);
        this.BackColor = Color.White;

        // Title label
        var titleLabel = new Label
        {
            Text = "ChargeGuard",
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            Location = new Point(20, 20),
            Size = new Size(360, 30),
            TextAlign = ContentAlignment.MiddleCenter
        };

        // Version label
        var versionLabel = new Label
        {
            Text = $"Version {GetApplicationVersion()}",
            Font = new Font("Segoe UI", 10),
            Location = new Point(20, 60),
            Size = new Size(360, 20),
            TextAlign = ContentAlignment.MiddleCenter
        };

        // Description label
        var descriptionLabel = new Label
        {
            Text = "A lightweight Windows battery charging alert utility.\n\n" +
                   "ChargeGuard monitors your laptop's battery and alerts you\n" +
                   "when it reaches a configured charging percentage.\n\n" +
                   "Note: ChargeGuard only monitors battery status.\n" +
                   "It does not control or stop charging.",
            Font = new Font("Segoe UI", 9),
            Location = new Point(20, 100),
            Size = new Size(360, 120),
            TextAlign = ContentAlignment.MiddleCenter
        };

        // OK button
        var okButton = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Location = new Point(160, 240),
            Size = new Size(80, 30),
            UseVisualStyleBackColor = true
        };

        // Add controls
        this.Controls.Add(titleLabel);
        this.Controls.Add(versionLabel);
        this.Controls.Add(descriptionLabel);
        this.Controls.Add(okButton);

        this.ResumeLayout(false);
    }

    private static string GetApplicationVersion()
    {
        return System.Reflection.Assembly.GetExecutingAssembly()
            .GetName().Version?.ToString(3) ?? "1.0.0";
    }
}
