using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace dive
{
    // 複数行表示
    class ListBoxExRowTextMultiLine : ListBoxExRow
    {
        const int _padding = 10;

        private Font _font = new Font("Tahoma", 9, FontStyle.Bold);
        private SizeF _textDrawSize;

        private string _text;

        public ListBoxExRowTextMultiLine(string text)
        {
            _height = 60;
            _text = text;
            
            NewHeight();
        }

        public override int Width {
            get { return _width; }
            set
            {
                _width = value;

                // 横幅によって高さが変わる場合はここで Height を計算しなおす
                NewHeight();
            }
        }

        public string Text
        {
            get { return _text; }
            set {
                _text = value;

                // データによって高さが変わる場合はここで Height を計算しなおす
                NewHeight();
            }
        }

        private void NewHeight()
        {
            int textwidth = base.Width - _padding * 2;

            // 幅指定で文字の描画サイズを求める
            _textDrawSize = GraphicExtentions.MeasureString(_text, _font, new Rectangle(0, 0, textwidth, 1));
            _textDrawSize.Width = textwidth;

            int newHeight = (int)_textDrawSize.Height + _padding * 2;       // 名前と時間の行分
            base.Height = newHeight;        
        }
        
        public override void Draw(Graphics g, int x, int y, bool tinydraw, bool selected)
        {
            // NewHeightで求めたサイズで文字列を描画する
            GraphicExtentions.DrawText(g, _text, _font, Color.Black, new Rectangle(x + _padding, y + _padding, (int)_textDrawSize.Width, (int)_textDrawSize.Height));
            
            // 行を分ける線
            g.DrawLine(new Pen(Parent.LineColor), 0, y + _height - 1, _width, y + _height - 1);
        }
    }
}
