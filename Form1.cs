using System;

using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using dive;

namespace ListBoxExSample
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // リストボックス描画停止
            listBoxEx1.ScreenUpdating = true;

            listBoxEx1.Border.Top = true;
            
            ListBoxExRowText row;
            ListBoxExRowLabel rowLabel;

            // イベント指定
            rowLabel = new ListBoxExRowLabel("行のクリックイベント");
            listBoxEx1.Items.Add(rowLabel);

            row = new ListBoxExRowText("行イベント１");
            row.Click += new EventHandler(row_Click);
            listBoxEx1.Items.Add(row);

            row = new ListBoxExRowText("行イベント２");
            row.Click += new EventHandler(delegate { MessageBox.Show("クリックされました。"); });
            listBoxEx1.Items.Add(row);

            // 複数行
            rowLabel = new ListBoxExRowLabel("複数行");
            listBoxEx1.Items.Add(rowLabel);

            ListBoxExRowTextMultiLine rowml;
            rowml = new ListBoxExRowTextMultiLine("1:メッセージ(改行コード)\n2:メッセージ");
            listBoxEx1.Items.Add(rowml);

            rowml = new ListBoxExRowTextMultiLine("メッセージ：メッセージ：長文メッセージは途中で改行されます。縦横切り替え時には計算して行の高さが変わります。");
            listBoxEx1.Items.Add(rowml);

            // チェックボックス
            rowLabel = new ListBoxExRowLabel("チェックボックス");
            listBoxEx1.Items.Add(rowLabel);

            ListBoxExRowCheckBox checkboxRow;

            checkboxRow = new ListBoxExRowCheckBox("CheckBox1", "チェックボックス１", "テストチェックON", true);
            listBoxEx1.Items.Add(checkboxRow);

            checkboxRow = new ListBoxExRowCheckBox("CheckBox2", "チェックボックス２", "テストチェックOFF", false);
            listBoxEx1.Items.Add(checkboxRow);

            checkboxRow = new ListBoxExRowCheckBox("CheckBox3", "チェックボックス３", true);
            listBoxEx1.Items.Add(checkboxRow);

            // オプション
            rowLabel = new ListBoxExRowLabel("オプション(Group1)");
            listBoxEx1.Items.Add(rowLabel);

            ListBoxExRowOption optionRow;

            optionRow = new ListBoxExRowOption("Group1", "Option1", "オプション１", "テストチェックON", true);
            listBoxEx1.Items.Add(optionRow);

            optionRow = new ListBoxExRowOption("Group1", "Option2", "オプション２", "テストチェックON");
            listBoxEx1.Items.Add(optionRow);

            optionRow = new ListBoxExRowOption("Group1", "Option3", "オプション３");
            listBoxEx1.Items.Add(optionRow);

            rowLabel = new ListBoxExRowLabel("オプション(Group2)");
            listBoxEx1.Items.Add(rowLabel);

            optionRow = new ListBoxExRowOption("Group2", "OptionA", "オプションA", "テストチェックON", true);
            listBoxEx1.Items.Add(optionRow);

            optionRow = new ListBoxExRowOption("Group2", "OptionB", "オプションB", "テストチェックON");
            listBoxEx1.Items.Add(optionRow);

            optionRow = new ListBoxExRowOption("Group2", "OptionC", "オプションC");
            listBoxEx1.Items.Add(optionRow);

            rowLabel = new ListBoxExRowLabel("ただのラベル");
            listBoxEx1.Items.Add(rowLabel);

            row = new ListBoxExRowText("リスト");
            listBoxEx1.Items.Add(row);

            ListBoxExRowTwoLine.FontHeight();
            ListBoxExRowTwoLine row2 = new ListBoxExRowTwoLine("ファーストRemoveItem", "セカンドRemoveItem");
            listBoxEx1.Items.Add(row2);

            row2 = new ListBoxExRowTwoLine("ファーストA", "セカンドB");
            listBoxEx1.Items.Add(row2);

            listBoxEx1.Items.Add(new ListBoxExRowDown("Down1", "ダウンボックス1", "テスト"));
            listBoxEx1.Items.Add(new ListBoxExRowDown("Down1", "ダウンボックス2"));


            rowLabel = new ListBoxExRowLabel("繰り返し");
            listBoxEx1.Items.Add(rowLabel);

            for (int i = 0; i < 100; i++)
            {
                row = new ListBoxExRowText(i.ToString() + "行目");
                listBoxEx1.Items.Add(row);
            }

            // 削除
            listBoxEx1.Items.RemoveAt(25);

            row = new ListBoxExRowText("途中に追加");
            listBoxEx1.Items.Insert(30, row);

            // リストボックス描画再開
            listBoxEx1.ScreenUpdating = true;

            // 表示位置を先頭に
            listBoxEx1.ScrollTop = 0;
        }

        void row_Click(object sender, EventArgs e)
        {
            MessageBox.Show("クリックされました。");
        }

        private void listBoxEx1_Click(object sender, EventArgs e)
        {
            int idx = listBoxEx1.SelectedIndex;
            if (idx != -1)
            {
                ListBoxExRow row = listBoxEx1.Items[idx];
                if (row.ToString().EndsWith("ListBoxExRowText"))
                {
                    MessageBox.Show(string.Format("click:{0}", (row as ListBoxExRowText).Text));
                    return;
                }
            }
        }


    }
}