namespace PCcontroller
{
    partial class Main
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Main));
            progressBarPil = new ProgressBar();
            trackBarSes = new TrackBar();
            trackBarEkran = new TrackBar();
            buttonKayit = new Button();
            buttonDuraklat = new Button();
            buttonDurdur = new Button();
            buttonBaglan = new Button();
            label1 = new Label();
            label2 = new Label();
            labelses = new Label();
            labelekran = new Label();
            labelpil = new Label();
            label7 = new Label();
            UartGelen = new Label();
            UartGiden = new Label();
            label4 = new Label();
            progressBarCPUusage = new ProgressBar();
            labelCPUUsage = new Label();
            label6 = new Label();
            progressBarGPUUsage = new ProgressBar();
            labelGPUUsage = new Label();
            label9 = new Label();
            label5 = new Label();
            comboBoxCOMPORT = new ComboBox();
            OBS_Durum = new Label();
            labelStatusConnect = new Label();
            labelCPUcelcius = new Label();
            labelGPUcelcius = new Label();
            checkBox1 = new CheckBox();
            buttonOBSbaglan = new Button();
            ((System.ComponentModel.ISupportInitialize)trackBarSes).BeginInit();
            ((System.ComponentModel.ISupportInitialize)trackBarEkran).BeginInit();
            SuspendLayout();
            // 
            // progressBarPil
            // 
            progressBarPil.Location = new Point(12, 126);
            progressBarPil.Name = "progressBarPil";
            progressBarPil.Size = new Size(340, 15);
            progressBarPil.TabIndex = 0;
            // 
            // trackBarSes
            // 
            trackBarSes.Location = new Point(12, 12);
            trackBarSes.Maximum = 100;
            trackBarSes.Name = "trackBarSes";
            trackBarSes.Size = new Size(340, 45);
            trackBarSes.TabIndex = 1;
            trackBarSes.Scroll += trackBarSes_Scroll;
            // 
            // trackBarEkran
            // 
            trackBarEkran.Location = new Point(12, 63);
            trackBarEkran.Maximum = 100;
            trackBarEkran.Name = "trackBarEkran";
            trackBarEkran.Size = new Size(340, 45);
            trackBarEkran.TabIndex = 1;
            trackBarEkran.Scroll += trackBarEkran_Scroll;
            // 
            // buttonKayit
            // 
            buttonKayit.Location = new Point(12, 156);
            buttonKayit.Name = "buttonKayit";
            buttonKayit.Size = new Size(75, 23);
            buttonKayit.TabIndex = 2;
            buttonKayit.Text = "Kayıt";
            buttonKayit.UseVisualStyleBackColor = true;
            buttonKayit.Click += buttonKayit_Click;
            // 
            // buttonDuraklat
            // 
            buttonDuraklat.Location = new Point(93, 156);
            buttonDuraklat.Name = "buttonDuraklat";
            buttonDuraklat.Size = new Size(75, 23);
            buttonDuraklat.TabIndex = 2;
            buttonDuraklat.Text = "Duraklat";
            buttonDuraklat.UseVisualStyleBackColor = true;
            buttonDuraklat.Click += buttonDuraklat_Click;
            // 
            // buttonDurdur
            // 
            buttonDurdur.Location = new Point(174, 156);
            buttonDurdur.Name = "buttonDurdur";
            buttonDurdur.Size = new Size(75, 23);
            buttonDurdur.TabIndex = 2;
            buttonDurdur.Text = "Durdur";
            buttonDurdur.UseVisualStyleBackColor = true;
            buttonDurdur.Click += buttonDurdur_Click;
            // 
            // buttonBaglan
            // 
            buttonBaglan.Enabled = false;
            buttonBaglan.Location = new Point(139, 282);
            buttonBaglan.Name = "buttonBaglan";
            buttonBaglan.Size = new Size(97, 23);
            buttonBaglan.TabIndex = 2;
            buttonBaglan.Text = "Baglan";
            buttonBaglan.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(407, 22);
            label1.Name = "label1";
            label1.Size = new Size(68, 15);
            label1.TabIndex = 3;
            label1.Text = "Ses Seviyesi";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(407, 72);
            label2.Name = "label2";
            label2.Size = new Size(79, 15);
            label2.TabIndex = 3;
            label2.Text = "Ekran seviyesi";
            // 
            // labelses
            // 
            labelses.AutoSize = true;
            labelses.Location = new Point(358, 22);
            labelses.Name = "labelses";
            labelses.Size = new Size(29, 15);
            labelses.TabIndex = 3;
            labelses.Text = "%00";
            // 
            // labelekran
            // 
            labelekran.AutoSize = true;
            labelekran.Location = new Point(358, 72);
            labelekran.Name = "labelekran";
            labelekran.Size = new Size(29, 15);
            labelekran.TabIndex = 3;
            labelekran.Text = "%00";
            // 
            // labelpil
            // 
            labelpil.AutoSize = true;
            labelpil.Location = new Point(358, 126);
            labelpil.Name = "labelpil";
            labelpil.Size = new Size(29, 15);
            labelpil.TabIndex = 3;
            labelpil.Text = "%00";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(125, 192);
            label7.Name = "label7";
            label7.Size = new Size(43, 15);
            label7.TabIndex = 3;
            label7.Text = "Uart Tx";
            // 
            // UartGelen
            // 
            UartGelen.AutoSize = true;
            UartGelen.Location = new Point(61, 192);
            UartGelen.Name = "UartGelen";
            UartGelen.Size = new Size(44, 15);
            UartGelen.TabIndex = 3;
            UartGelen.Text = "Uart Rx";
            // 
            // UartGiden
            // 
            UartGiden.AutoSize = true;
            UartGiden.Location = new Point(174, 192);
            UartGiden.Name = "UartGiden";
            UartGiden.Size = new Size(43, 15);
            UartGiden.TabIndex = 3;
            UartGiden.Text = "Uart Tx";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(407, 126);
            label4.Name = "label4";
            label4.Size = new Size(64, 15);
            label4.TabIndex = 3;
            label4.Text = "Pil Seviyesi";
            // 
            // progressBarCPUusage
            // 
            progressBarCPUusage.Location = new Point(12, 220);
            progressBarCPUusage.Name = "progressBarCPUusage";
            progressBarCPUusage.Size = new Size(340, 15);
            progressBarCPUusage.TabIndex = 0;
            // 
            // labelCPUUsage
            // 
            labelCPUUsage.AutoSize = true;
            labelCPUUsage.Location = new Point(358, 220);
            labelCPUUsage.Name = "labelCPUUsage";
            labelCPUUsage.Size = new Size(29, 15);
            labelCPUUsage.TabIndex = 3;
            labelCPUUsage.Text = "%00";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(407, 220);
            label6.Name = "label6";
            label6.Size = new Size(82, 15);
            label6.TabIndex = 3;
            label6.Text = "CPU kullanımı";
            // 
            // progressBarGPUUsage
            // 
            progressBarGPUUsage.Location = new Point(12, 254);
            progressBarGPUUsage.Name = "progressBarGPUUsage";
            progressBarGPUUsage.Size = new Size(340, 15);
            progressBarGPUUsage.TabIndex = 0;
            // 
            // labelGPUUsage
            // 
            labelGPUUsage.AutoSize = true;
            labelGPUUsage.Location = new Point(358, 254);
            labelGPUUsage.Name = "labelGPUUsage";
            labelGPUUsage.Size = new Size(29, 15);
            labelGPUUsage.TabIndex = 3;
            labelGPUUsage.Text = "%00";
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(407, 254);
            label9.Name = "label9";
            label9.Size = new Size(83, 15);
            label9.TabIndex = 3;
            label9.Text = "GPU Kullanımı";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(12, 192);
            label5.Name = "label5";
            label5.Size = new Size(44, 15);
            label5.TabIndex = 3;
            label5.Text = "Uart Rx";
            // 
            // comboBoxCOMPORT
            // 
            comboBoxCOMPORT.FormattingEnabled = true;
            comboBoxCOMPORT.Location = new Point(12, 283);
            comboBoxCOMPORT.Name = "comboBoxCOMPORT";
            comboBoxCOMPORT.Size = new Size(121, 23);
            comboBoxCOMPORT.TabIndex = 4;
            // 
            // OBS_Durum
            // 
            OBS_Durum.AutoSize = true;
            OBS_Durum.Location = new Point(369, 160);
            OBS_Durum.Name = "OBS_Durum";
            OBS_Durum.Size = new Size(76, 15);
            OBS_Durum.TabIndex = 3;
            OBS_Durum.Text = "OBS Durumu";
            // 
            // labelStatusConnect
            // 
            labelStatusConnect.AutoSize = true;
            labelStatusConnect.Location = new Point(242, 286);
            labelStatusConnect.Name = "labelStatusConnect";
            labelStatusConnect.Size = new Size(66, 15);
            labelStatusConnect.TabIndex = 3;
            labelStatusConnect.Text = "Disconnect";
            // 
            // labelCPUcelcius
            // 
            labelCPUcelcius.AutoSize = true;
            labelCPUcelcius.Location = new Point(345, 192);
            labelCPUcelcius.Name = "labelCPUcelcius";
            labelCPUcelcius.Size = new Size(56, 15);
            labelCPUcelcius.TabIndex = 3;
            labelCPUcelcius.Text = "CPU --°C";
            // 
            // labelGPUcelcius
            // 
            labelGPUcelcius.AutoSize = true;
            labelGPUcelcius.Location = new Point(430, 192);
            labelGPUcelcius.Name = "labelGPUcelcius";
            labelGPUcelcius.Size = new Size(56, 15);
            labelGPUcelcius.TabIndex = 3;
            labelGPUcelcius.Text = "GPU --°C";
            // 
            // checkBox1
            // 
            checkBox1.AutoSize = true;
            checkBox1.Location = new Point(369, 287);
            checkBox1.Name = "checkBox1";
            checkBox1.Size = new Size(123, 19);
            checkBox1.TabIndex = 5;
            checkBox1.Text = "Başlangıçta çalıştır";
            checkBox1.UseVisualStyleBackColor = true;
            checkBox1.CheckedChanged += checkBox1_CheckedChanged;
            // 
            // buttonOBSbaglan
            // 
            buttonOBSbaglan.Location = new Point(255, 156);
            buttonOBSbaglan.Name = "buttonOBSbaglan";
            buttonOBSbaglan.Size = new Size(108, 23);
            buttonOBSbaglan.TabIndex = 2;
            buttonOBSbaglan.Text = "OBS Port Bağlan";
            buttonOBSbaglan.UseVisualStyleBackColor = true;
            buttonOBSbaglan.Click += buttonOBSbaglan_Click;
            // 
            // Main
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(498, 319);
            Controls.Add(checkBox1);
            Controls.Add(comboBoxCOMPORT);
            Controls.Add(label9);
            Controls.Add(labelGPUUsage);
            Controls.Add(OBS_Durum);
            Controls.Add(labelGPUcelcius);
            Controls.Add(labelCPUcelcius);
            Controls.Add(label6);
            Controls.Add(labelCPUUsage);
            Controls.Add(label4);
            Controls.Add(labelpil);
            Controls.Add(labelekran);
            Controls.Add(UartGiden);
            Controls.Add(UartGelen);
            Controls.Add(label7);
            Controls.Add(label5);
            Controls.Add(labelStatusConnect);
            Controls.Add(labelses);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(buttonBaglan);
            Controls.Add(buttonOBSbaglan);
            Controls.Add(buttonDurdur);
            Controls.Add(buttonDuraklat);
            Controls.Add(buttonKayit);
            Controls.Add(progressBarGPUUsage);
            Controls.Add(trackBarEkran);
            Controls.Add(progressBarCPUusage);
            Controls.Add(trackBarSes);
            Controls.Add(progressBarPil);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            HelpButton = true;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            Name = "Main";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "PC Controller";
            ((System.ComponentModel.ISupportInitialize)trackBarSes).EndInit();
            ((System.ComponentModel.ISupportInitialize)trackBarEkran).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ProgressBar progressBarPil;
        private TrackBar trackBarSes;
        private TrackBar trackBarEkran;
        private Button buttonKayit;
        private Button buttonDuraklat;
        private Button buttonDurdur;
        private Button buttonBaglan;
        private Label label1;
        private Label label2;
        private Label labelses;
        private Label labelekran;
        private Label labelpil;
        private Label label7;
        private Label UartGelen;
        private Label UartGiden;
        private Label label4;
        private ProgressBar progressBarCPUusage;
        private Label labelCPUUsage;
        private Label label6;
        private ProgressBar progressBarGPUUsage;
        private Label labelGPUUsage;
        private Label label9;
        private Label label5;
        private ComboBox comboBoxCOMPORT;
        private Label OBS_Durum;
        private Label labelStatusConnect;
        private Label labelCPUcelcius;
        private Label labelGPUcelcius;
        private CheckBox checkBox1;
        private Button buttonOBSbaglan;
    }
}
