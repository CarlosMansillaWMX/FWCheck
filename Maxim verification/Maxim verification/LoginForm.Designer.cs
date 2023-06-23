namespace Maxim_verification
{
    partial class LoginForm
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
            this.tbuser = new System.Windows.Forms.TextBox();
            this.tblog = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.tboff = new System.Windows.Forms.Button();
            this.tbpassword = new System.Windows.Forms.TextBox();
            this.tbate = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tbuser
            // 
            this.tbuser.Location = new System.Drawing.Point(133, 182);
            this.tbuser.Margin = new System.Windows.Forms.Padding(4);
            this.tbuser.Name = "tbuser";
            this.tbuser.Size = new System.Drawing.Size(132, 22);
            this.tbuser.TabIndex = 0;
            // 
            // tblog
            // 
            this.tblog.Location = new System.Drawing.Point(133, 281);
            this.tblog.Margin = new System.Windows.Forms.Padding(4);
            this.tblog.Name = "tblog";
            this.tblog.Size = new System.Drawing.Size(100, 28);
            this.tblog.TabIndex = 1;
            this.tblog.Text = "LOG IN";
            this.tblog.UseVisualStyleBackColor = true;
            this.tblog.Click += new System.EventHandler(this.tblog_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(373, 281);
            this.button2.Margin = new System.Windows.Forms.Padding(4);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(100, 28);
            this.button2.TabIndex = 2;
            this.button2.Text = "Exit";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(133, 159);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(85, 16);
            this.label1.TabIndex = 3;
            this.label1.Text = "Employee ID";
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.SystemColors.ActiveBorder;
            this.panel1.Controls.Add(this.tboff);
            this.panel1.Controls.Add(this.tbpassword);
            this.panel1.Controls.Add(this.tbate);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Location = new System.Drawing.Point(613, 137);
            this.panel1.Margin = new System.Windows.Forms.Padding(4);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(364, 241);
            this.panel1.TabIndex = 6;
            // 
            // tboff
            // 
            this.tboff.Location = new System.Drawing.Point(45, 193);
            this.tboff.Margin = new System.Windows.Forms.Padding(4);
            this.tboff.Name = "tboff";
            this.tboff.Size = new System.Drawing.Size(100, 28);
            this.tboff.TabIndex = 3;
            this.tboff.Text = "Log Offline";
            this.tboff.UseVisualStyleBackColor = true;
            this.tboff.Click += new System.EventHandler(this.tboff_Click);
            // 
            // tbpassword
            // 
            this.tbpassword.Location = new System.Drawing.Point(45, 146);
            this.tbpassword.Margin = new System.Windows.Forms.Padding(4);
            this.tbpassword.Name = "tbpassword";
            this.tbpassword.Size = new System.Drawing.Size(132, 22);
            this.tbpassword.TabIndex = 2;
            // 
            // tbate
            // 
            this.tbate.Location = new System.Drawing.Point(45, 78);
            this.tbate.Margin = new System.Windows.Forms.Padding(4);
            this.tbate.Name = "tbate";
            this.tbate.Size = new System.Drawing.Size(132, 22);
            this.tbate.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(41, 22);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(88, 16);
            this.label2.TabIndex = 0;
            this.label2.Text = "Offline Station";
            // 
            // LoginForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1024, 447);
            this.ControlBox = false;
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.tblog);
            this.Controls.Add(this.tbuser);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "LoginForm";
            this.Text = "560-000003_V06-21-2023-0_ReFlash";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox tbuser;
        private System.Windows.Forms.Button tblog;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button tboff;
        private System.Windows.Forms.TextBox tbpassword;
        private System.Windows.Forms.TextBox tbate;
        private System.Windows.Forms.Label label2;
    }
}