namespace Hourado
{
    partial class FTakePhoto
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FTakePhoto));
            this.pcbTakePhoto = new System.Windows.Forms.PictureBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnTakePhoto = new System.Windows.Forms.Button();
            this.btnTry = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pcbTakePhoto)).BeginInit();
            this.SuspendLayout();
            // 
            // pcbTakePhoto
            // 
            this.pcbTakePhoto.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.pcbTakePhoto.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pcbTakePhoto.Dock = System.Windows.Forms.DockStyle.Top;
            this.pcbTakePhoto.Location = new System.Drawing.Point(0, 0);
            this.pcbTakePhoto.Margin = new System.Windows.Forms.Padding(1);
            this.pcbTakePhoto.Name = "pcbTakePhoto";
            this.pcbTakePhoto.Size = new System.Drawing.Size(427, 347);
            this.pcbTakePhoto.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pcbTakePhoto.TabIndex = 19;
            this.pcbTakePhoto.TabStop = false;
            // 
            // btnSave
            // 
            this.btnSave.BackColor = System.Drawing.Color.DarkSeaGreen;
            this.btnSave.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSave.Location = new System.Drawing.Point(0, 350);
            this.btnSave.Margin = new System.Windows.Forms.Padding(1);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(215, 57);
            this.btnSave.TabIndex = 18;
            this.btnSave.Text = "ذخیره";
            this.btnSave.UseVisualStyleBackColor = false;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnTakePhoto
            // 
            this.btnTakePhoto.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.btnTakePhoto.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnTakePhoto.Location = new System.Drawing.Point(212, 350);
            this.btnTakePhoto.Margin = new System.Windows.Forms.Padding(1);
            this.btnTakePhoto.Name = "btnTakePhoto";
            this.btnTakePhoto.Size = new System.Drawing.Size(215, 57);
            this.btnTakePhoto.TabIndex = 17;
            this.btnTakePhoto.Text = "عکس گرفتن";
            this.btnTakePhoto.UseVisualStyleBackColor = false;
            this.btnTakePhoto.Click += new System.EventHandler(this.btnTakePhoto_Click);
            // 
            // btnTry
            // 
            this.btnTry.BackColor = System.Drawing.Color.Gainsboro;
            this.btnTry.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnTry.Location = new System.Drawing.Point(212, 350);
            this.btnTry.Margin = new System.Windows.Forms.Padding(1);
            this.btnTry.Name = "btnTry";
            this.btnTry.Size = new System.Drawing.Size(215, 57);
            this.btnTry.TabIndex = 16;
            this.btnTry.Text = "سعی دوباره";
            this.btnTry.UseVisualStyleBackColor = false;
            this.btnTry.Click += new System.EventHandler(this.btnTry_Click);
            // 
            // FTakePhoto
            // 
            this.AcceptButton = this.btnTakePhoto;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(427, 406);
            this.Controls.Add(this.pcbTakePhoto);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.btnTakePhoto);
            this.Controls.Add(this.btnTry);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(1);
            this.MaximizeBox = false;
            this.Name = "FTakePhoto";
            this.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "عکس برداری";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.FTakePhoto_FormClosed);
            this.Load += new System.EventHandler(this.FTakePhoto_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pcbTakePhoto)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox pcbTakePhoto;
        private System.Windows.Forms.Button btnSave;
        public System.Windows.Forms.Button btnTakePhoto;
        public System.Windows.Forms.Button btnTry;
    }
}