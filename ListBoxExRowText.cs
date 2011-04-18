using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace dive
{
    // テキスト表示
    class ListBoxExRowText : ListBoxExRow
    {
        private string _text;

        public ListBoxExRowText(string text)
        {
            _height = 60;
            _text = text;
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

        public override void Draw(Graphics g, int x, int y, bool tinydraw, bool selected)
        {
            Font font;

            // 選択時背景色塗りつぶし
            if (selected)
            {
                //g.FillRectangle(new SolidBrush(Color.Gray), x, y, _width, _height);
            }

            font = new Font("Tahoma", 9, FontStyle.Bold);
            Size fsize = g.MeasureString(_text, font).ToSize();
            g.DrawString(_text, new Font("Tahoma", 10, FontStyle.Bold), new SolidBrush(Color.DarkGray), x + (_width - fsize.Width) / 2, y + (_height - fsize.Height) / 2);
            
            // イメージとかの描画もここで行う

            // 行を分ける線
            g.DrawLine(new Pen(Parent.LineColor), 0, y + _height - 1, _width, y + _height - 1);
        }
    }
}
