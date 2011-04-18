using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace dive
{

    // 次ページ表示
    class ListBoxExRowLabel : ListBoxExRow
    {
        static Font _defaultFont = new Font("Tahoma", 8, FontStyle.Bold);
        static int _paddingWidth = 10;
        static int _paddingHeight = 3;

        private string _text = "Label";
        Font _font;
        Color _foreColor = Color.White;
        Color _backColor = Color.Black;

        public ListBoxExRowLabel(string text)
        {
            _font = _defaultFont;

            _text = text;

            NewHeight();
        }

        public override int Width {
            get { return _width; }
            set
            {
                _width = value;

                // 横幅によって高さが変わる場合はここで _height を計算しなおす
            }
        }

        public string Text
        {
            get { return _text; }
            set {
                _text = value;

                // データによって高さが変わる場合はここで _height を計算しなおす
            }
        }

        public Font Font
        {
            get { return _font; }
            set { _font = value; }
        }

        public Color ForeColor
        {
            get { return _foreColor; }
            set { _foreColor = value; }
        }

        public Color BackColor
        {
            get { return _backColor; }
            set { _backColor = value; }
        }

        private void NewHeight()
        {
            SizeF textsize = Graphics.FromImage(ListBoxEx.OffScreen).MeasureString(_text, _font);

            _height = (int)textsize.Height + _paddingHeight * 2 + 1;
        }

        public override bool DrawingSelectedBackground
        {
            get
            {
                return false;
            }
        }

        public override void Draw(Graphics g, int x, int y, bool tinydraw, bool selected)
        {

            // 背景
            g.FillRectangle(new SolidBrush(_backColor), x, y, _width, _height);

            // ラベル
            g.DrawString(_text, _font, new SolidBrush(_foreColor), x + _paddingWidth, y + _paddingHeight);

            // ライン
            g.DrawLine(new Pen(Parent.LineColor), 0, y + _height - 1, _width, y + _height - 1);
        }

    }
}
