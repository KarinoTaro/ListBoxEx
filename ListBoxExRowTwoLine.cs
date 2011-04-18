using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace dive
{

    // 次ページ表示
    class ListBoxExRowTwoLine : ListBoxExRow
    {
        private  static Font _fontFirstLine = new Font("Tahoma", 15, FontStyle.Bold);
        private static Font _fontSecondLine = new Font("Tahoma", 11, FontStyle.Regular);

        private static int _fontHeightFirst;
        private static int _fontHeightSecond;

        private static int _paddingH = 8;       // 水平余白
        private static int _paddingV = 6;       // 垂直余白
        private static int _spacingV = 2;       // 行間空白

        private string _textFirst;
        private string _textSecond;

        public ListBoxExRowTwoLine(string text1, string text2)
        {
            _textFirst = text1;
            _textSecond = text2;

            _height = _fontHeightFirst + _fontHeightSecond + _paddingH * 3;
        }

        public static void FontHeight()
        {
            _fontHeightFirst = (int)Graphics.FromImage(ListBoxEx.OffScreen).MeasureString("A", _fontFirstLine).Height;
            _fontHeightSecond = (int)Graphics.FromImage(ListBoxEx.OffScreen).MeasureString("A", _fontSecondLine).Height;
        }

        public override int Width {
            get { return _width; }
            set
            {
                _width = value;

                // 横幅によって高さが変わる場合はここで _height を計算しなおす

            }

        }

        public override void Draw(Graphics g, int x, int y, bool tinydraw, bool selected)
        {
            // テキスト描画
            g.DrawString(_textFirst, _fontFirstLine, new SolidBrush(Color.Black), x + _paddingH, y + _paddingV);
            g.DrawString(_textSecond, _fontSecondLine, new SolidBrush(Color.Gray), x + _paddingH, y + _paddingV + _fontHeightFirst + _spacingV);
            
            // 行を分ける線
            g.DrawLine(new Pen(Color.Gray), 0, y + _height - 1, _width, y + _height - 1);

        }

    }
}
