using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;

namespace dive
{
    class ListBoxEx : Control
    {
        const int MinScrollBarHeight = 30;      // 最小スクロールバー長
        const int FirstScrollWidth = 50;        // 高速スクロール領域幅（右端）
        const int ClickRadius = 16;             // クリック有効半径

        // コントロールデータ
        ArrayList _items;                   // アイテムデータ
        int _selectIndex;                   // インデックス

        // コントロール描画
        int _height;                        // コントロール高さ
        Dictionary<int, int> _itemHeights;  // 各アイテムの高さ
        Bitmap _offscreen;                  // 描画バッファ
        int _rowsHeight;                    // 領域全長
        int _scrollTop;                     // 表示位置トップ
        int _scrollTopLast;                 // ドラッグ前の表示位置トップ
        bool _screenupdating;               // 画面更新しない
        int _setWidth = -1;                 // 前回のSetWidth値

        // スクロール
        bool _dragging;                     // ドラッグ中
        bool _dragBar;                      // 高速スクロールフラグ
        Point _startDrag = new Point();     // ドラッグ開始位置
        bool _clickCancel;                  // MouseUp時 True:クリックではない False:クリック

        bool _inertiaScroll = false;        // 慣性スクロール有効フラグ
        bool _scrolling;                    // 慣性スクロール
        float _scrollHeight;                // 慣性スクロール移動量
        Point _lastDrag = new Point();      // 慣性スクロール用

        bool _onXcrawl = false;             // ES Xcrawlフラグ
        int _xcrawlMoveHeight = 100;        // xcrawl移動量

        public ListBoxEx() {
            _items = new ArrayList();
            _itemHeights = new Dictionary<int, int>();

            _scrollTop = 0;

            //_offscreen = new Bitmap(this.Width, this.Height * 2);
            _offscreen = new Bitmap(1024, 1024, System.Drawing.Imaging.PixelFormat.Format16bppRgb565);

            //DrawTextTestArea = new Bitmap(this.Width * 2, this.Height * 2);

            _selectIndex = -1;

            _screenupdating = true;
        }

        /// <summary>
        /// 慣性スクロールプロパティ
        /// </summary>
        public bool InertiaScroll
        {
            get { return _inertiaScroll; }
            set { _inertiaScroll = value; }
        }

        public bool ScreenUpdating
        {
            get { return _screenupdating; }
            set { _screenupdating = value; }
        }

        public void Clear()
        {
            _items.Clear();
            _scrollTop = 0;
            _rowsHeight = 0;
            _selectIndex = -1;
            this.Refresh();
        }

        public void AddItem(ListBoxExRow item)
        {
            item.Width = this.Width;
            _rowsHeight += item.Height;
            _items.Add(item);
            this.Refresh();
        }

        public void InsertItem(ListBoxExRow item, int idx)
        {
            item.Width = this.Width;
            _rowsHeight += item.Height;
            _items.Insert(idx, item);

            int tmpheight = 0;
            for (int i = 0; i < _items.Count; i++)
            {
                if (tmpheight + ((ListBoxExRow)_items[i]).Height > _scrollTop + Height)
                {
                    if (i > idx)
                    {
                        _scrollTop += item.Height;
                    }
                    break;
                }
                tmpheight += ((ListBoxExRow)_items[i]).Height;
            }
            
            this.Refresh();
        }

        public void RemoveItem(ListBoxExRow item)
        {
            _items.Remove(item);
            _rowsHeight = 0;
            for (int i = 0; i < _items.Count; i++)
            {
                _rowsHeight += ((ListBoxExRow)_items[i]).Height;
            }
            this.Refresh();
        }

        public void RemoveAtItem(int idx) {
            _items.RemoveAt(idx);
            _rowsHeight = 0;
            for (int i = 0; i < _items.Count; i++)
            {
                _rowsHeight += ((ListBoxExRow)_items[i]).Height;
            }
            this.Refresh();
        }

        // アイテムの追加に使ってはいけない
        public ArrayList Items
        {
            get
            {
                return _items;
            }
        }

        // リスト横幅の変更。縦サイズを再計算する。
        public void SetWidth(int width)
        {
            if (_setWidth == width)
            {
                return;
            }

            this.Width = width;
            _rowsHeight = 0;
            for(int i = 0; i < _items.Count; i++)
            {
                ListBoxExRow obj = (ListBoxExRow)_items[i];
                obj.Width = this.Width;
                _itemHeights[i] = obj.Height;
                _rowsHeight += obj.Height;
            }
            _setWidth = width;
        }

        public event EventHandler SelectedIndexChanged;
        protected virtual void OnSelectedIndexChanged(EventArgs e)
        {
            if (this.SelectedIndexChanged != null)
                this.SelectedIndexChanged(this, e);
        }

        public int SelectedIndex
        {
            get { return _selectIndex; }
            set
            {
                _selectIndex = value;
                this.Refresh();
                if (this.SelectedIndexChanged != null)
                {
                    this.SelectedIndexChanged(this, EventArgs.Empty);
                }
            }
        }
        
        // 
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyValue == 131)
            {
                _onXcrawl = true;
            }
            else
            {
                switch (e.KeyCode)
                {
                    case Keys.PageDown:
                        break;
                    case Keys.PageUp:
                        break;
                    case Keys.Home:
                        this.SelectedIndex = 0;
                        break;
                    case Keys.End:
                        this.SelectedIndex = _items.Count - 1;
                        break;
                }

            }
            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            int top;
            if (e.KeyValue == 131)
            {
                _onXcrawl = false;
            }
            else
            {
                switch (e.KeyCode)
                {
                    case Keys.Down:
                        if (_onXcrawl == true)
                        {
                            top = _scrollTop + _xcrawlMoveHeight;
                        }
                        else
                        {
                            top = _scrollTop + (int)(this.Height * 0.9);
                        }
                        if (top > _rowsHeight - this.Height)
                        {
                            top = _rowsHeight - this.Height;
                        }
                        _scrollTop = top;
                        this.Refresh();
                        break;
                    case Keys.Up:
                        if (_onXcrawl == true)
                        {
                            top = _scrollTop - _xcrawlMoveHeight;
                        }
                        else
                        {
                            top = _scrollTop - (int)(this.Height * 0.9);
                        }
                        if (top < 0)
                        {
                            top = 0;
                        }
                        _scrollTop = top;
                        this.Refresh();

                        break;

                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            _dragging = true;
            _clickCancel = false;
            _scrolling = false;
            _scrollTopLast = _scrollTop;
            _scrollHeight = 0;

            _startDrag.X = e.X;
            _startDrag.Y = e.Y;

            _lastDrag.Y = e.Y;
            int clickTop = _scrollTop + e.Y;

            if (e.X >= Width - FirstScrollWidth)
            {
                _dragBar = true;
            }
            else
            {
                _dragBar = false;
            }

            this.Refresh();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {

            double d = Math.Sqrt((_startDrag.X - e.X) * (_startDrag.X - e.X) + (_startDrag.Y - e.Y) * (_startDrag.Y - e.Y));
            if (d > ClickRadius)
            {
                _clickCancel = true;
            }

            if (_dragBar == true)
            {
                if (e.Y <= FirstScrollWidth)
                {
                    _scrollTop = 0;
                }
                else
                {
                    if (e.Y >= Height - FirstScrollWidth)
                    {
                        _scrollTop = _rowsHeight - Height;
                    }
                    else
                    {
                        _scrollTop = (int)((float)(_rowsHeight - Height) * ((float)(e.Y - FirstScrollWidth) / (float)(Height - FirstScrollWidth * 2)));
                    }
                }

                this.Refresh();
                return;
            }

            if (_dragging == true)
            {
                _scrollTop = _scrollTopLast + (_startDrag.Y - e.Y);
                if (_scrollTop > _rowsHeight - this.Height)
                {
                    _scrollTop = _rowsHeight - this.Height;
                }
                if (_scrollTop < 0)
                {
                    _scrollTop = 0;
                }

                _scrollHeight = (_lastDrag.Y - e.Y) * 1.2F;
                _lastDrag.Y = e.Y;

                this.Refresh();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _dragging = false;

            int clickTop = _scrollTop + e.Y;
            int tmpheight = 0;
            int tmpselected = -1;

            if (_dragBar == true)
            {
                _dragBar = false;
                this.Refresh();
                return;
            }

            _scrolling = true;
            double d = Math.Sqrt((_startDrag.X - e.X) * (_startDrag.X - e.X) + (_startDrag.Y - e.Y) * (_startDrag.Y - e.Y));
            if (d <= ClickRadius && _clickCancel == false)
            {
                for (int i = 0; i < _items.Count; i++)
                {
                    if (tmpheight + ((ListBoxExRow)_items[i]).Height > clickTop)
                    {
                        tmpselected = i;
                        _scrolling = false;
                        break;
                    }
                    tmpheight += ((ListBoxExRow)_items[i]).Height;
                }
                this.SelectedIndex = tmpselected;
            }

            _height = this.Height;

            if (_inertiaScroll == true)
            {
                Thread t = new Thread(new ThreadStart(ThreadScrolling));
                t.IsBackground = true;
                t.Start();
            }
            else
            {
                _scrolling = false;
            }

            this.Refresh();
        }


        delegate void lbeItemsRefresh();

        public void ThreadScrolling() {

            try
            {
                while (Math.Abs(_scrollHeight) > 3 && _scrolling == true)
                {
                    _scrollTop += (int)_scrollHeight;
                    if (_scrollTop > _rowsHeight - _height)
                    {
                        _scrollTop = _rowsHeight - _height;
                        _scrolling = false;
                        _scrollHeight = 0;
                    }
                    if (_scrollTop < 0)
                    {
                        _scrollTop = 0;
                        _scrolling = false;
                        _scrollHeight = 0;
                    }

                    _scrollHeight *= 0.85F;

                    Invoke(new lbeItemsRefresh(this.Refresh));
                    Thread.Sleep(20);
                }
                _scrolling = false;
                Invoke(new lbeItemsRefresh(this.Refresh));
            }
            catch
            {
                // スクロール中にフォームを閉じるとエラーになるのでスルーさせる
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (_screenupdating == false)
            {
                return;
            }

            // 何番目のアイテムから表示するか
            int viewitem = 0;
            int tmpheight = 0;
            int hideheight = 0;
            for (int i = 0; i < _items.Count; i++)
            {
                if (tmpheight + ((ListBoxExRow)_items[i]).Height > _scrollTop)
                {
                    viewitem = i;
                    hideheight = _scrollTop - tmpheight;
                    break;
                }
                tmpheight += ((ListBoxExRow)_items[i]).Height;
            }

            Graphics g = Graphics.FromImage(this._offscreen);
            g.FillRectangle(new SolidBrush(Color.White), 0, 0, this.Width, (1000 + this.Height) / 2);

            if (_items.Count > 0)
            {
                // 表示アイテム分処理する
                int drawtop = 0;
                ListBoxExRow item;
                int firstHeight = ((ListBoxExRow)_items[viewitem]).Height;
                for (int i = viewitem; i < _items.Count; i++)
                {
                    item = (ListBoxExRow)_items[i];
                    bool tinydraw = _dragging | _scrolling;
                    bool selected = (_selectIndex == i) ? true : false;
                    item.Draw(g, 0, drawtop, tinydraw, selected);

                    drawtop += item.Height;
                    if (drawtop - hideheight > Height)
                    {
                        break;
                    }
                }
            }

            g.Dispose();

            // バッファに描画した内容を表示画面に貼り付ける
            e.Graphics.DrawImage(_offscreen , 0, -hideheight + 1);
            e.Graphics.DrawLine(new Pen(Color.Gray), 0, 0, Width, 0);

            int barwidth = 8;

            // クリック中はスクロールバーも描画
            SolidBrush brush = new SolidBrush(Color.Gray);
            int x, y, d, d2;
            d2 = 0;
            if (_rowsHeight > Height)
            {
                d = (int)((float)(Height - 2) * (float)Height / (float)_rowsHeight) - barwidth;
                if (d < MinScrollBarHeight)
                {
                    d2 = MinScrollBarHeight - d;
                    d = MinScrollBarHeight;
                }
            }
            else
            {
                d = Height - barwidth;
            }
            y = (int)((float)(Height - d2 - 2) * ((float)_scrollTop / (float)_rowsHeight)) + 1;

            if (_dragging || _scrolling)
            {
                x = Width - barwidth - 4;

                e.Graphics.FillEllipse(brush, x, y, barwidth, barwidth);
                e.Graphics.FillEllipse(brush, x, y + d, barwidth, barwidth);
                e.Graphics.FillRectangle(brush, x, y + barwidth / 2, barwidth + 1, d);
            }
            //else
            //{
            //    x = Width - 1;
            //    e.Graphics.DrawLine(new Pen(Color.Gray, 1), x, y, x, y + d + barwidth);
            //}
        }

        // 
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // 
        }

        protected override void OnResize(EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;

            SetWidth(this.Width);
            
            Cursor.Current = Cursors.Default;

        }

    }
}
