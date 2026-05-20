using System.Drawing;
using System.Windows.Forms;
using Ephemera.NBagOfUis;
using Ephemera.IconicSelector;


namespace WinStart
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        #region Windows Form Designer generated code
        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            rtbTell = new RichTextBox();
            selector = new Selector();
            SuspendLayout();
            // 
            // rtbTell
            // 
            rtbTell.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            rtbTell.BorderStyle = BorderStyle.FixedSingle;
            rtbTell.Location = new Point(431, 5);
            rtbTell.Name = "rtbTell";
            rtbTell.Size = new Size(516, 448);
            rtbTell.TabIndex = 0;
            rtbTell.Text = "";
            // 
            // selector
            // 
            selector.AllowExternalDrop = false;
            selector.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            selector.AutoScroll = true;
            selector.BorderStyle = BorderStyle.FixedSingle;
            selector.DrawFont = new Font("Calibri", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            selector.ImageSize = new Size(32, 32);
            selector.IndicatorColor = Color.Purple;
            selector.Location = new Point(3, 5);
            selector.Mode = OpMode.Click;
            selector.Name = "selector";
            selector.NumColumns = 1;
            selector.Pad = 4;
            selector.Size = new Size(111, 448);
            selector.Spacing = 10;
            selector.Style = SelectorStyle.Icon;
            selector.TabIndex = 7;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 19F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(959, 460);
            Controls.Add(selector);
            Controls.Add(rtbTell);
            Name = "MainForm";
            Text = "1";
            ResumeLayout(false);
        }
        #endregion

        private Selector selector;
        private RichTextBox rtbTell;
    }
}
