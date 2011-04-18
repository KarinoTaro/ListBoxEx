using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace dive
{

    // チェックボックス
    class ListBoxExRowDown : ListBoxExRow
    {
        private static Font _fontText = new Font("Tahoma", 11, FontStyle.Bold);
        private static Font _fontDesc = new Font("Tahoma", 7, FontStyle.Regular);

        private static int _paddingH = 8;       // 水平余白
        private static int _paddingV = 3;       // 垂直余白
        private static int _spacingV = 1;       // 行間空白

        private static int _imageSize = 64;     // チェックイメージの描画サイズ（縦、横）

        private string _name;                   // 名称
        private string _text;                   // 表示名称
        private string _desc;                   // 説明
        //private bool _value;                    // 値

        private int _heightText;                // 表示名称 描画高さ
        private int _heightDesc;                // 説明 描画高さ

        public ListBoxExRowDown()
            : this("", "", "")
        {
        }

        public ListBoxExRowDown(string name)
            : this(name, "", "")
        {
        }

        public ListBoxExRowDown(string name, string text)
            : this(name, text, "")
        {
        }

        public ListBoxExRowDown(string name, string text, string description)
        {
            _name = name;
            _text = text;
            _desc = description;
            //_value = value;

            NewHeight();
        }

        /// <summary>
        /// コントロール名
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// 表示
        /// </summary>
        public string Text
        {
            get { return _text; }
            set { _text = value; }
        }

        /// <summary>
        /// 説明
        /// </summary>
        public string Description
        {
            get { return _desc; }
            set { _desc = value; }
        }

        /// <summary>
        /// 値
        /// </summary>
        //public bool Value
        //{
        //    get { return _value; }
        //    set { _value = value; }
        //}

        public override int Width {
            get { return _width; }
            set { _width = value; }
        }

        private void NewHeight()
        {
            Graphics g = Graphics.FromImage(ListBoxEx.OffScreen);

            _heightText = (int)g.MeasureString(_text, _fontText).Height;
            _heightDesc = (int)g.MeasureString(_desc, _fontDesc).Height;

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

        //public override bool OnClick()
        //{
        //    _value = !_value;
        //    Parent.Invalidate();

        //    return false;
        //}

        public override void Draw(Graphics g, int x, int y, bool tinydraw, bool selected)
        {
            int drawheight = _heightText;
            if (_heightDesc > 0)
            {
                drawheight += _heightDesc + _spacingV;
            }
            int drawtop = (_height - drawheight) / 2;

            // 文字描画
            g.DrawString(_text, _fontText, new SolidBrush(Parent.ForeColor), x + _imageSize + _paddingH, y + drawtop);
            if (_heightDesc > 0)
            {
                g.DrawString(_desc, _fontDesc, new SolidBrush(Parent.ForeColor), x + _imageSize + _paddingH, y + drawtop + _heightText + _spacingV);
            }
            
            // チェックイメージ描画
            int boxSize = (int)(_imageSize * 0.4);

            Point[] points = new Point[3];
            points[0] = new Point(x + (_imageSize - boxSize) / 2, y + (_height - boxSize) / 2);
            points[1] = new Point(x + _imageSize / 2, y + _height - (_height - boxSize) / 2);
            points[2] = new Point(x + (_imageSize - boxSize) / 2 + boxSize, y + (_height - boxSize) / 2);

            g.FillPolygon(new SolidBrush(Color.Black), points);

            // 行を分ける線
            g.DrawLine(new Pen(Parent.LineColor), 0, y + _height - 1, _width, y + _height - 1);

        }

    }
}
