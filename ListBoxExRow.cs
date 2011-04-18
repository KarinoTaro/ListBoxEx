using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace dive
{
    class ListBoxExRow
    {
        protected int _width;
        protected int _height;

        public ListBoxExRow()
        {
        }

        public virtual int Width
        {
            get { return _width; }
            set { _width = value; }
        }

        public virtual int Height
        {
            get { return _height; }
            set { _height = value; }
        }

        // 画面描画 継承先で設定する。
        public virtual void Draw(Graphics g, int x, int y, bool tinydraw, bool selected)
        {
            ;
        }

        protected Color CalcTextColor(Color backgroundColor)
        {
            if (backgroundColor.Equals(Color.Empty))
                return Color.Black;

            int sum = backgroundColor.R + backgroundColor.G + backgroundColor.B;

            if (sum > 256)
                return Color.Black;
            else
                return Color.White;
        }

    }
}
