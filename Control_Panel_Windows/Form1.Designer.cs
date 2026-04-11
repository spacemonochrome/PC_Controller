namespace Control_Panel_Windows
{
    partial class Form1
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
            button1 = new Button();
            checkBox1 = new CheckBox();
            checkBox2 = new CheckBox();
            linkLabel1 = new LinkLabel();
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            label4 = new Label();
            label5 = new Label();
            label6 = new Label();
            label7 = new Label();
            label8 = new Label();
            SuspendLayout();
            // 
            // button1
            // 
            button1.Location = new Point(88, 200);
            button1.Name = "button1";
            button1.Size = new Size(75, 23);
            button1.TabIndex = 1;
            button1.Text = "Tamam";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // checkBox1
            // 
            checkBox1.AutoSize = true;
            checkBox1.Location = new Point(63, 139);
            checkBox1.Name = "checkBox1";
            checkBox1.Size = new Size(128, 19);
            checkBox1.TabIndex = 2;
            checkBox1.Text = "Başlangıçta Çalıştır.";
            checkBox1.UseVisualStyleBackColor = true;
            // 
            // checkBox2
            // 
            checkBox2.AutoSize = true;
            checkBox2.Location = new Point(63, 164);
            checkBox2.Name = "checkBox2";
            checkBox2.Size = new Size(100, 19);
            checkBox2.TabIndex = 2;
            checkBox2.Text = "Tray icon aktif";
            checkBox2.UseVisualStyleBackColor = true;
            // 
            // linkLabel1
            // 
            linkLabel1.AutoSize = true;
            linkLabel1.Location = new Point(63, 239);
            linkLabel1.Name = "linkLabel1";
            linkLabel1.Size = new Size(121, 15);
            linkLabel1.TabIndex = 3;
            linkLabel1.TabStop = true;
            linkLabel1.Text = "Source Code (Github)";
            linkLabel1.LinkClicked += linkLabel1_LinkClicked;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(63, 71);
            label1.Name = "label1";
            label1.Size = new Size(99, 15);
            label1.TabIndex = 4;
            label1.Text = "GPU Temp (Max):";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(63, 40);
            label2.Name = "label2";
            label2.Size = new Size(62, 15);
            label2.TabIndex = 4;
            label2.Text = "CPU Load:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(63, 9);
            label3.Name = "label3";
            label3.Size = new Size(99, 15);
            label3.TabIndex = 4;
            label3.Text = "CPU Temp (Max):";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(63, 102);
            label4.Name = "label4";
            label4.Size = new Size(62, 15);
            label4.TabIndex = 4;
            label4.Text = "GPU Load:";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(188, 71);
            label5.Name = "label5";
            label5.Size = new Size(28, 15);
            label5.TabIndex = 4;
            label5.Text = "? °C";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(188, 102);
            label6.Name = "label6";
            label6.Size = new Size(23, 15);
            label6.TabIndex = 4;
            label6.Text = "%0";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(188, 40);
            label7.Name = "label7";
            label7.Size = new Size(23, 15);
            label7.TabIndex = 4;
            label7.Text = "%0";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(188, 9);
            label8.Name = "label8";
            label8.Size = new Size(28, 15);
            label8.TabIndex = 4;
            label8.Text = "? °C";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(282, 268);
            Controls.Add(label8);
            Controls.Add(label3);
            Controls.Add(label7);
            Controls.Add(label2);
            Controls.Add(label6);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(label1);
            Controls.Add(linkLabel1);
            Controls.Add(checkBox2);
            Controls.Add(checkBox1);
            Controls.Add(button1);
            Name = "Form1";
            Text = "Windows Controller Panel";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private Button button1;
        private CheckBox checkBox1;
        private CheckBox checkBox2;
        private LinkLabel linkLabel1;
        private Label label1;
        private Label label2;
        private Label label3;
        private Label label4;
        private Label label5;
        private Label label6;
        private Label label7;
        private Label label8;
    }
}
