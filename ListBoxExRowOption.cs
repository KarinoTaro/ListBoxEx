using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace dive
{
    // オプション
    class ListBoxExRowOption : ListBoxExRow
    {
        private static Font _fontText = new Font("Tahoma", 11, FontStyle.Bold);
        private static Font _fontDesc = new Font("Tahoma", 7, FontStyle.Regular);

        private static int _paddingH = 8;           // 水平余白
        private static int _paddingV = 3;           // 垂直余白
        private static int _spacingV = 1;           // 行間空白

        private static int _imageSize = 64;         // チェックイメージの描画サイズ（縦、横）

        public string Group { get; set; }           // グループ
        public string Name { get; set; }            // 名称
        public string Text { get; set; }            // 表示名
        public string Description { get; set; }     // 説明
        public bool Value { get; set; }             // 値

        private int _heightText;
        private int _heightDesc;

        public ListBoxExRowOption()
            : this("", "", "", "", false)
        {
        }

        public ListBoxExRowOption(string group)
            : this(group, "", "", "", false)
        {
        }

        public ListBoxExRowOption(string group, string name)
            : this(group, name, "", "", false)
        {
        }

        public ListBoxExRowOption(string group, string name, string text)
            : this(group, name, text, "", false)
        {
        }

        public ListBoxExRowOption(string group, string name, string text, string description)
            : this(group, name, text, description, false)
        {
        }

        public ListBoxExRowOption(string group, string name, string text, bool value)
            : this(group, name, text, "", value)
        {
        }

        public ListBoxExRowOption(string group, string name, string text, string description, bool value)
        {
            Group = group;
            Name = name;
            Text = text;
            Description = description;
            Value = value;

            NewHeight();
        }

        public override int Width {
            get { return _width; }
            set { _width = value; }
        }

        private void NewHeight()
        {
            Graphics g = Graphics.FromImage(ListBoxEx.OffScreen);
            _heightText = (int)g.MeasureString(Text, _fontText).Height;
            _heightDesc = (int)g.MeasureString(Description, _fontDesc).Height;

            if (_imageSize + 1 < _heightText + _heightDesc + _paddingV * 2 + _spacingV + 1)
            {
                _height = _heightText + _heightDesc + _paddingV * 2 + _spacingV + 1;
            }
            else
            {
                _height = _imageSize + 1;
            }
        }

        public override bool DrawingSelectedBackground
        {
            get
            {
                return false;
            }
        }

        public override bool OnClick()
        {
            // 同一グループをfalseにする
            foreach (ListBoxExRow row in Parent.Items)
            {
                if (row.ToString().EndsWith("ListBoxExRowOption"))
                {
                    ListBoxExRowOption option = (ListBoxExRowOption)row;
                    if (option.Group == Group)
                    {
                        option.Value = false;
                    }
                }
            }

            Value = true;
            Parent.Invalidate();

            return false;
        }

        public override void Draw(Graphics g, int x, int y, bool tinydraw, bool selected)
        {
            int drawheight = _heightText;
            if (_heightDesc > 0)
            {
                drawheight += _heightDesc + _spacingV;
            }
            int drawtop = (_height - drawheight) / 2;

            // 選択時背景色塗りつぶし
            if (selected)
            {
                //g.FillRectangle(new SolidBrush(Color.Gray), x, y, _width, _height);
            }

            // 文字描画
            g.DrawString(Text, _fontText, new SolidBrush(Parent.ForeColor), x + _imageSize + _paddingH, y + drawtop);
            if (_heightDesc > 0)
            {
                g.DrawString(Description, _fontDesc, new SolidBrush(Parent.ForeColor), x + _imageSize + _paddingH, y + drawtop + _heightText + _spacingV);
            }
            
            // 選択イメージ描画
            int ellipseSize = (int)(_imageSize * 0.5);
            g.DrawEllipse(new Pen(Color.Black), x + (_imageSize - ellipseSize) / 2, y + (_height - ellipseSize) / 2, ellipseSize, ellipseSize);
            if (Value)
            {
                // 選択
                ellipseSize = (int)(_imageSize * 0.35);
                g.FillEllipse(new SolidBrush(Color.LightGreen), x + (_imageSize - ellipseSize) / 2, y + (_height - ellipseSize) / 2, ellipseSize, ellipseSize);
            }

            // 行を分ける線
            g.DrawLine(new Pen(Parent.LineColor), 0, y + _height - 1, _width, y + _height - 1);
        }
    }
}
