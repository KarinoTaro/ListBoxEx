namespace ListBoxExSample
{
    partial class Form1
    {
        /// <summary>
        /// 必要なデザイナ変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.MainMenu mainMenu1;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージ リソースが破棄される場合 true、破棄されない場合は false です。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナで生成されたコード

        /// <summary>
        /// デザイナ サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディタで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.mainMenu1 = new System.Windows.Forms.MainMenu();
            this.listBoxEx1 = new System.Windows.Forms.ListBoxEx();
            this.contextMenu1 = new System.Windows.Forms.ContextMenu();
            this.menuItem1 = new System.Windows.Forms.MenuItem();
            this.menuItem2 = new System.Windows.Forms.MenuItem();
            this.menuItem3 = new System.Windows.Forms.MenuItem();
            this.SuspendLayout();
            // 
            // listBoxEx1
            // 
            this.listBoxEx1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.listBoxEx1.ContextMenu = this.contextMenu1;
            this.listBoxEx1.InertiaScroll = true;
            this.listBoxEx1.Location = new System.Drawing.Point(0, 0);
            this.listBoxEx1.Name = "listBoxEx1";
            this.listBoxEx1.ScreenUpdating = true;
            this.listBoxEx1.SelectedIndex = -1;
            this.listBoxEx1.Size = new System.Drawing.Size(480, 536);
            this.listBoxEx1.TabIndex = 0;
            this.listBoxEx1.Text = "listBoxEx1";
            this.listBoxEx1.Click += new System.EventHandler(this.listBoxEx1_Click);
            // 
            // contextMenu1
            // 
            this.contextMenu1.MenuItems.Add(this.menuItem1);
            this.contextMenu1.MenuItems.Add(this.menuItem2);
            this.contextMenu1.MenuItems.Add(this.menuItem3);
            // 
            // menuItem1
            // 
            this.menuItem1.Text = "メニュー１";
            // 
            // menuItem2
            // 
            this.menuItem2.Text = "メニュー２";
            // 
            // menuItem3
            // 
            this.menuItem3.Text = "メニュー３";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(192F, 192F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.AutoScroll = true;
            this.ClientSize = new System.Drawing.Size(480, 536);
            this.Controls.Add(this.listBoxEx1);
            this.Location = new System.Drawing.Point(0, 52);
            this.Menu = this.mainMenu1;
            this.Name = "Form1";
            this.Text = "ListBoxEx sample";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListBoxEx listBoxEx1;
        private System.Windows.Forms.ContextMenu contextMenu1;
        private System.Windows.Forms.MenuItem menuItem1;
        private System.Windows.Forms.MenuItem menuItem2;
        private System.Windows.Forms.MenuItem menuItem3;

    }
}

