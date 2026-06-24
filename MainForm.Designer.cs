using System.Drawing;
using System.Windows.Forms;
using Ephemera.NBagOfUis;
using Ephemera.IconicSelector;


namespace Trebuchet
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
            selector = new Selector();
            SuspendLayout();
            // 
            // selector
            // 
            selector.AutoScroll = true;
            selector.BorderStyle = BorderStyle.FixedSingle;
            selector.Dock = DockStyle.Fill;
            selector.Location = new Point(0, 0);
            selector.Name = "selector";
            selector.Size = new Size(1104, 379);
            selector.TabIndex = 7;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 19F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1104, 379);
            Controls.Add(selector);
            Name = "MainForm";
            Text = "booga";
            ResumeLayout(false);
        }
        #endregion

        private Selector selector;
    }
}
