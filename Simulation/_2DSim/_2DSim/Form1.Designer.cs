
namespace _2DSim
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.sweepNUD = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.resNUD = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.pMap = new _2DSim.pMapSim();
            ((System.ComponentModel.ISupportInitialize)(this.sweepNUD)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.resNUD)).BeginInit();
            this.SuspendLayout();
            // 
            // sweepNUD
            // 
            this.sweepNUD.Location = new System.Drawing.Point(745, 12);
            this.sweepNUD.Maximum = new decimal(new int[] {
            360,
            0,
            0,
            0});
            this.sweepNUD.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.sweepNUD.Name = "sweepNUD";
            this.sweepNUD.Size = new System.Drawing.Size(120, 22);
            this.sweepNUD.TabIndex = 1;
            this.sweepNUD.Value = new decimal(new int[] {
            36,
            0,
            0,
            0});
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(689, 14);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(50, 17);
            this.label1.TabIndex = 2;
            this.label1.Text = "Sweep";
            // 
            // resNUD
            // 
            this.resNUD.Location = new System.Drawing.Point(745, 40);
            this.resNUD.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.resNUD.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.resNUD.Name = "resNUD";
            this.resNUD.Size = new System.Drawing.Size(120, 22);
            this.resNUD.TabIndex = 1;
            this.resNUD.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(664, 42);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(75, 17);
            this.label2.TabIndex = 2;
            this.label2.Text = "Resolution";
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.Location = new System.Drawing.Point(745, 68);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(120, 32);
            this.button1.TabIndex = 3;
            this.button1.Text = "LoadDXF";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // pMap
            // 
            this.pMap.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)));
            this.pMap.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.pMap.Detected = null;
            this.pMap.gDetected = null;
            this.pMap.gObstructions = null;
            this.pMap.gVolatile = null;
            this.pMap.Location = new System.Drawing.Point(12, 12);
            this.pMap.Name = "pMap";
            this.pMap.Obstructions = null;
            this.pMap.Size = new System.Drawing.Size(597, 608);
            this.pMap.TabIndex = 0;
            this.pMap.Volatile = null;
            this.pMap.MouseDown += new System.Windows.Forms.MouseEventHandler(this.dbpanel_MouseDown);
            this.pMap.MouseMove += new System.Windows.Forms.MouseEventHandler(this.dbpanel_MouseMove);
            this.pMap.MouseUp += new System.Windows.Forms.MouseEventHandler(this.dbpanel_MouseUp);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(877, 632);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.resNUD);
            this.Controls.Add(this.sweepNUD);
            this.Controls.Add(this.pMap);
            this.Name = "Form1";
            this.Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)(this.sweepNUD)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.resNUD)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        public pMapSim pMap;

        #endregion

        private System.Windows.Forms.NumericUpDown sweepNUD;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown resNUD;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button button1;
    }
}

