using System.Collections;
using System.Threading;
using System.Drawing;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace System.Windows.Forms
{

#if PocketPC
    //[ComVisible(true)]
    public enum SelectionMode
    {
        None = 0,
        One = 1,
        MultiSimple = 2,
        MultiExtended = 3,
    }
#endif

    class ListBoxEx : Control
    {
        const int MinScrollBarHeight = 30;  // 最小スクロールバー長
        const int FirstScrollWidth = 50;    // 高速スクロール領域幅（右端）
        const int ClickRadius = 16;         // クリック有効半径

        static Bitmap _offscreen;           // 描画バッファ
        static Graphics _graphics;          // _offscreen の Graphics

        // コントロールデータ
        ObjectCollection _items;            // アイテムデータ

        int _selectedIndex;                 // インデックス
        Border _border;                     // ボーダー
        Bitmap _backgroundImage = null;     // 背景画像
        int _backgroundAlignment = 0;       // 背景画像配置指定 0:左上、1:中央上、2:右上、3:左中、4:中央中、5:右中、6:左下、7:中央下、8:右下
        int _backgroundX = 0;               // 背景画像配置座標 X
        int _backgroundY = 0;               // 背景画像配置座標 Y
        SelectionMode _selectionMode;       // 選択モード

        // コントロール描画
        Color _selectedForeColor;           // 選択文字色
        Color _selectedBackColor;           // 選択背景色
        Color _lineColor;                   // ライン色
        Color _scrollBarColor;              // スクロールバーカラー
        int _height;                        // コントロール高さ
        int _rowsHeight;                    // 領域全長
        int _scrollTop;                     // 表示位置トップ
        int _scrollTopLast;                 // ドラッグ前の表示位置トップ
        bool _screenupdating;               // 画面更新しない
        int _setWidth = -1;                 // 前回のSetWidth値

        // スクロール
        bool _dragging;                     // ドラッグ中
        bool _dragon;                       // Windows用ドラッグ処理用
        bool _dragBar;                      // 高速スクロールフラグ
        Point _startDrag = new Point();     // ドラッグ開始位置

        bool _inertiaScroll = false;        // 慣性スクロール有効フラグ
        int _scrolling;                     // 慣性スクロール 0:静止 1:慣性スクロール 2:スムーススクロール
        float _scrollHeight;                // 慣性スクロール移動量
        int _newtop;                        // スムーススクロールの移動先
        const int _smoothMove = 80;         // スムーススクロールの移動量

#if PocketPC
        CFTime _time = new CFTime(DateTime.Now);
#endif

        const int _mouseMoveCount = 5;
        int _mouseMoveIndex = -1;
        int[] _mouseMoveY = new int[_mouseMoveCount];
        long[] _mouseMoveTime = new long[_mouseMoveCount];

        bool _onXcrawl = false;             // ES Xcrawlフラグ
        int _xcrawlMoveHeight = 100;        // xcrawl移動量

        Timer _timerScrolling;              // スクロール用のタイマー
        bool _timerScrollDrawing = false;   // タイマー描画処理中フラグ

        List<string> msgs;

#if !PocketPC
        bool _pressAndHold = false;
        DateTime _lastPress;
#endif

        public ListBoxEx() {
            msgs = new List<string>();

            _items = new ObjectCollection();
            _items.Parent = this;

            _border = new Border();

            _selectedBackColor = Color.FromArgb(49, 199, 214);
            _lineColor = Color.Gray;
            _scrollBarColor = Color.Gray;

            _scrollTop = 0;

            if (_offscreen == null)
            {
#if PocketPC
                _offscreen = new Bitmap(1024, 2048, System.Drawing.Imaging.PixelFormat.Format16bppRgb565);
#else
                _offscreen = new Bitmap(2048, 2048, System.Drawing.Imaging.PixelFormat.Format16bppRgb565);
#endif
                _graphics = Graphics.FromImage(_offscreen);
            }

            _selectedIndex = -1;
            _selectionMode = SelectionMode.One;
            _screenupdating = true;

            // タップ、左クリック
            WndProcHooker.HookWndProc(this, new WndProcHooker.WndProcCallback(this.WM_LButton_Down), Win32.WM_LBUTTONDOWN);
            
            // 右クリック
            WndProcHooker.HookWndProc(this, new WndProcHooker.WndProcCallback(this.WM_LButton_Down), Win32.WM_RBUTTONDOWN);

            // ジェスチャ
            WndProcHooker.HookWndProc(this, new WndProcHooker.WndProcCallback(this.WM_Gesture), Win32.WM_GESTURE);

            // マウスホイール
            WndProcHooker.HookWndProc(this, new WndProcHooker.WndProcCallback(this.WM_MouseWheel), Win32.WM_MOUSEWHEEL);

            // 慣性スクロールタイマー
            _timerScrolling = new Timer();
            _timerScrolling.Interval = 100;
            _timerScrolling.Tick += new EventHandler(TimerScrolling);
            _timerScrolling.Enabled = true;
        }

        /// <summary>
        /// マウスホイール処理
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="msg"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <param name="handled"></param>
        /// <returns></returns>
        int WM_MouseWheel(IntPtr hwnd, uint msg, uint wParam, int lParam, ref bool handled)
        {
            int top = _scrollTop;
            int delta = (Int16)(wParam >> 16);

            top -= delta / 3;
            if (top > _rowsHeight - this.Height)
            {
                top = _rowsHeight - this.Height;
            }
            if (top < 0)
            {
                top = 0;
            }

            _scrollTop = top;
            this.Invalidate();
            handled = true;
            return 0;
        }

        /// <summary>
        /// 背景画像プロパティ
        /// </summary>
        public Bitmap BackGroundImage
        {
            get { return _backgroundImage; }
            set
            {
                _backgroundImage = value;
                BackGroundPosition();
            }
        }

        /// <summary>
        /// 背景画像表示位置
        /// </summary>
        public int BackGroundAlignment
        {
            get
            {
                return _backgroundAlignment;
            }
            set
            {
                _backgroundAlignment = value;
                BackGroundPosition();
            }
        }

        /// <summary>
        /// 背景画像表示座標計算処理
        /// </summary>
        private void BackGroundPosition()
        {
            if (_backgroundImage == null)
            {
                return;
            }

            switch (_backgroundAlignment)
            {
                case 0:
                case 3:
                case 6:
                    // 左
                    _backgroundX = 0;
                    break;
                case 1:
                case 4:
                case 7:
                    // 中央
                    _backgroundX = (this.Width - _backgroundImage.Width) / 2;
                    break;
                case 2:
                case 5:
                case 8:
                    // 右
                    _backgroundX = this.Width - _backgroundImage.Width;
                    break;
            }

            switch (_backgroundAlignment)
            {
                case 0:
                case 1:
                case 2:
                    // 左
                    _backgroundY = 0;
                    break;
                case 3:
                case 4:
                case 5:
                    // 中央
                    _backgroundY = (this.Height - _backgroundImage.Height) / 2;
                    break;
                case 6:
                case 7:
                case 8:
                    // 右
                    _backgroundY = this.Height - _backgroundImage.Height;
                    break;
            }
        }

        /// <summary>
        /// 枠線プロパティ
        /// </summary>
        public Border Border
        {
            get { return _border; }
            set { _border = value; }
        }

        /// <summary>
        /// 描画用バッファ
        /// </summary>
        public static Bitmap OffScreen
        {
            get { return _offscreen; }
            set {
                _offscreen = value;
                _graphics = Graphics.FromImage(_offscreen);
            }
        }

        /// <summary>
        /// 選択文字色
        /// </summary>
        public Color SelectedForeColor
        {
            get { return _selectedForeColor; }
            set { _selectedForeColor = value; }
        }

        /// <summary>
        /// 選択背景色
        /// </summary>
        public Color SelectedBackColor
        {
            get { return _selectedBackColor; }
            set { _selectedBackColor = value; }
        }

        /// <summary>
        /// ライン色
        /// </summary>
        public Color LineColor
        {
            get { return _lineColor; }
            set { _lineColor = value; }
        }

        /// <summary>
        /// スクロールバー色
        /// </summary>
        public Color ScrollBarColor
        {
            get { return _scrollBarColor; }
            set { _scrollBarColor = value; }
        }

        /// <summary>
        /// 慣性スクロールの有効、無効を示します。
        /// </summary>
        public bool InertiaScroll
        {
            get { return _inertiaScroll; }
            set { _inertiaScroll = value; }
        }

        /// <summary>
        /// 画面更新の有効、無効を示します。
        /// </summary>
        public bool ScreenUpdating
        {
            get { return _screenupdating; }
            set { _screenupdating = value; }
        }

        /// <summary>
        /// リストを返します。
        /// </summary>
        public ObjectCollection Items
        {
            get
            {
                return _items;
            }
        }

        /// <summary>
        /// 選択モード
        /// </summary>
        public SelectionMode SelectionMode
        {
            get { return _selectionMode; }
            set { _selectionMode = value; }
        }

        /// <summary>
        /// アイテム全ての高さ
        /// </summary>
        public int RowsHeight
        {
            get { return _rowsHeight; }
        }

        /// <summary>
        /// 選択の解除
        /// </summary>
        public void ClearSelected()
        {
            _selectedIndex = -1;
            for (int i = 0; i < _items.Count; i++)
            {
                _items[i].Selected = false;
            }
            this.Invalidate();
        }

        /// <summary>
        /// 幅
        /// </summary>
        public new int Width
        {
            get { return base.Width; }
            set
            {
                SetWidth(value);
            }
        }

        /// <summary>
        /// リスト幅設定します。全アイテムの幅を設定して変更される高さを取得します。
        /// </summary>
        /// <param name="width"></param>
        private void SetWidth(int width)
        {
            if (_setWidth == width)
            {
                return;
            }

            int lastIndex = GetSelectedIndex(_scrollTop);
            int newScrollTop = 0;
            for (int i = 0; i < _items.Count; i++)
            {
                _items[i].Width = width;
            }
            if (0 <= lastIndex && lastIndex < _items.Count)
            {
                newScrollTop = _items[lastIndex].Top;
            }
            _scrollTop = newScrollTop;
            Invoke(new FormInvokeControl(delegate
            {
                this.Invalidate();
            }));

            base.Width = width;
            _setWidth = width;
        }
        delegate void FormInvokeControl();

        /// <summary>
        /// 各アイテムの描画位置のTopを再計算します。
        /// </summary>
        public void SetItemTops()
        {
            int top = 0;
            _rowsHeight = 0;
            for (int i = 0; i < _items.Count; i++)
            {
                ListBoxExRow obj = _items[i] as ListBoxExRow;
                obj.Top = top;
                top += obj.Height;
                _rowsHeight += obj.Height;
            }
        }

        /// <summary>
        /// SelectedIndexChangedイベント
        /// </summary>
        public event EventHandler SelectedIndexChanged;
        protected virtual void OnSelectedIndexChanged(EventArgs e)
        {
            if (this.SelectedIndexChanged != null)
            {
                this.SelectedIndexChanged(this, e);
            }
        }

        /// <summary>
        /// インデックス
        /// </summary>
        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                _selectedIndex = value;
                if (_selectedIndex != -1 && _dragging == false)
                {
                    ListBoxExRow row = ((ListBoxExRow)_items[_selectedIndex]);
                    // 選択した行が表示上部にかかっていたら全て表示するように移動
                    if (row.Top < _scrollTop)
                    {
                        _scrollTop = row.Top;
                    }

                    // 選択した行が表示下部にかかっていたら全て表示するように移動
                    if (row.Top + row.Height > _scrollTop + this.Height)
                    {
                        _scrollTop = row.Top + row.Height - this.Height;
                    }
                }

                if (this.SelectedIndexChanged != null)
                {
                    OnSelectedIndexChanged(new EventArgs());
                }
            }
        }

#if !PocketPC
        // Windows用キー入力処理
        protected override bool ProcessDialogKey(Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Left:
                case Keys.Right:
                case Keys.Up:
                case Keys.Down:
                    KeyEventArgs e = new KeyEventArgs(keyData);
                    OnKeyDown(e);
                    return e.Handled;
            }

            return base.ProcessDialogKey(keyData);
        }
#endif

        /// <summary>
        /// キーダウンイベント
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            int top;
            int idx;
            if (e.KeyValue == 131)
            {
                _onXcrawl = true;
            }
            else
            {
                ListBoxExRow row;
                switch (e.KeyCode)
                {
                    case Keys.PageUp:
#if PocketPC
                    case Keys.Left:
#endif
                    // ページアップ
                        top = _scrollTop - (int)(this.Height * 0.9);
                        if (top < 0)
                        {
                            top = 0;
                        }
                        //_scrollTop = top;
                        _newtop = top;
                        _scrolling = 2;

                        _selectedIndex = -1;
                        this.Invalidate();
                        break;
                    case Keys.PageDown:
#if PocketPC
                    case Keys.Right:
#endif
                    // ページダウン
                        top = _scrollTop + (int)(this.Height * 0.9);
                        if (top > _rowsHeight - this.Height)
                        {
                            top = _rowsHeight - this.Height;
                        }
                        if (top < 0)
                        {
                            top = 0;
                        }
                        //_scrollTop = top;
                        _newtop = top;
                        _scrolling = 2;

                        _selectedIndex = -1;
                        this.Invalidate();
                        break;
                    case Keys.Home:
                        this.SelectedIndex = 0;
                        this.Invalidate();
                        break;
                    case Keys.End:
                        this.SelectedIndex = _items.Count - 1;
                        this.Invalidate();
                        break;
                    case Keys.Down:
                        if (_onXcrawl == true)
                        {
                            // Xcrawlページダウン
                            top = _scrollTop + _xcrawlMoveHeight;
                            _selectedIndex = -1;
                        }
                        else
                        {
                            // 次行
                            if (_selectedIndex == -1)
                            {
                                // 現在表示されているページの（完全に表示されている）最初の行を選択行にする
                                idx = GetSelectedIndex(_scrollTop);
                                if (idx != -1)
                                {
                                    if (((ListBoxExRow)_items[idx]).Top < _scrollTop)
                                    {
                                        idx++;
                                    }
                                }
                                _selectedIndex = idx;
                            }
                            else
                            {
                                // 次の行
                                _selectedIndex++;
                            }

                            if (_selectedIndex >= _items.Count)
                            {
                                _selectedIndex = _items.Count - 1;
                            }

                            if (_selectedIndex != -1) {
                                row = ((ListBoxExRow)_items[_selectedIndex]);
                                if (_scrollTop + this.Height < row.Top + row.Height)
                                {
                                    top = row.Top + row.Height - this.Height;
                                }
                                else
                                {
                                    top = _scrollTop;
                                }
                            }
                            else
                            {
                                top = _scrollTop;
                            }
                        }
                        if (top > _rowsHeight - this.Height)
                        {
                            top = _rowsHeight - this.Height;
                        }
                        if (top < 0)
                        {
                            top = 0;
                        }
                        _scrollTop = top;
                        this.Invalidate();
                        break;
                    case Keys.Up:
                        if (_onXcrawl == true)
                        {
                            // Xcrawlページアップ
                            top = _scrollTop - _xcrawlMoveHeight;
                            _selectedIndex = -1;
                        }
                        else
                        {
                            // 前行
                            if (_selectedIndex == -1)
                            {
                                // 現在表示されているページの（完全に表示されている）最後の行を選択行にする
                                idx = GetSelectedIndex(_scrollTop + this.Height);
                                if (idx == -1)
                                {
                                    idx = _items.Count - 1;
                                }

                                if (idx != -1)
                                {
                                    row = ((ListBoxExRow)_items[idx]);
                                    if (row.Top + row.Height > _scrollTop + this.Height)
                                    {
                                        idx--;
                                    }
                                }
                                _selectedIndex = idx;
                            }
                            else
                            {
                                // 前の行
                                _selectedIndex--;
                            }

                            if (_items.Count > 0 && _selectedIndex < 0)
                            {
                                _selectedIndex = 0;
                            }

                            if (_selectedIndex != -1 && _scrollTop > ((ListBoxExRow)_items[_selectedIndex]).Top)
                            {
                                top = ((ListBoxExRow)_items[_selectedIndex]).Top;
                            }
                            else
                            {
                                top = _scrollTop;
                            }
                        }

                        if (top < 0)
                        {
                            top = 0;
                        }
                        _scrollTop = top;
                        this.Invalidate();
                        break;
                }

            }
            base.OnKeyDown(e);
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);
        }

        /// <summary>
        /// キーアップイベント
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyUp(KeyEventArgs e)
        {
            //int top;
            //int idx;
            if (e.KeyValue == 131)
            {
                _onXcrawl = false;
            }
            else
            {
                switch (e.KeyCode)
                {
                    case Keys.Enter:
                        if (_selectedIndex != -1)
                        {
                            if (_selectionMode == SelectionMode.MultiSimple)
                            {
                                _items[_selectedIndex].Selected = !_items[_selectedIndex].Selected;
                                this.Invalidate();
                            }
                            this.OnClick(new EventArgs());
                        }
                        break;
                    case Keys.Space:
                        if (_selectedIndex != -1)
                        {
                            // コンテキストメニューの表示
                            base.ContextMenu.Show(this, new Point(this.Width, _items[_selectedIndex].Top));
                        }
                        break;
                }
            }
        }

        int WM_Gesture(IntPtr hwnd, uint msg, uint wParam, int lParam, ref bool handled)
        {
            //Win32.GESTUREINFO;
            //Win32.GESTUREMETRICS;
            //switch (wParam)
            //{
            //    case Win32.GID_BEGIN:
            //        break;
            //    case Win32.GID_END:
            //        break;
            //    case Win32.GID_PAN:         // MOVE
            //        break;
            //    case Win32.GID_SCROLL:
            //        break;
            //    case Win32.GID_HOLD:
            //        break;
            //    case Win32.GID_SELECT:
            //        break;
            //    case Win32.GID_DOUBLESELECT:
            //        break;
            //}
            //MessageBox.Show("Gesture Message Hook!!");
            handled = true;

            return -1;
        }

        /// <summary>
        /// マウスダウンイベント
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="msg"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <param name="handled"></param>
        /// <returns></returns>
        int WM_LButton_Down(IntPtr hwnd, uint msg, uint wParam, int lParam, ref bool handled)
        {
            int x = lParam & 0x0ffff;
            int y = (lParam >> 16) & 0x0ffff;

            _dragging = true;
            _scrollTopLast = _scrollTop;
            _scrollHeight = 0;

            _startDrag.X = x;
            _startDrag.Y = y;

            if (x >= Width - FirstScrollWidth)
            {
                _dragBar = true;
            }
            else
            {
                _dragBar = false;
                if (_scrolling == 0)
                {
                    this.SelectedIndex = GetSelectedIndex(_scrollTop + y);
                }
            }

            _dragon = true;

            this.Invalidate();

            AddEventY(y);

#if !PocketPC
            _pressAndHold = true;
            _lastPress = DateTime.Now;
#endif

            return -1;
        }

        /// <summary>
        /// マウス移動イベント
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_dragon == false)
            {
                // WindowsのHover
                return;
            }

            double d = Math.Sqrt((_startDrag.X - e.X) * (_startDrag.X - e.X) + (_startDrag.Y - e.Y) * (_startDrag.Y - e.Y));
            if (d > ClickRadius)
            {
                // 選択解除
                this.SelectedIndex = -1;
#if !PocketPC
                _pressAndHold = false;
#endif
            }

            if (_dragBar == true)
            {
                // スクロールバー ドラッグ
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
                if (_scrollTop < 0)
                {
                    _scrollTop = 0;
                }
            }
            else
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

                AddEventY(e.Y);
            }

            this.Invalidate();
        }

        /// <summary>
        /// マウスアップイベント
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            _dragging = false;
            _dragon = false;
#if !PocketPC
            _pressAndHold = false;
#endif

            if (_dragBar == true)
            {
                _dragBar = false;
                this.Invalidate();
                return;
            }

            if (_selectedIndex != -1)
            {
                if (_selectionMode == SelectionMode.MultiSimple)
                {
                    _items[_selectedIndex].Selected = !_items[_selectedIndex].Selected;
                }
            }

            if (_inertiaScroll == true)
            {
                _scrolling = 1;
                _height = this.Height;

                AddEventY(e.Y);

                // _mouseEventY から慣性力を求める
                int vector = 0;

#if PocketPC
                long time_start = _time.Now.Ticks - 3000000;
#else
                long time_start = DateTime.Now.Ticks - 3000000;
#endif
                int last = _mouseMoveY[_mouseMoveIndex];
                int idx = 0;
                for (int i = 1; i < _mouseMoveCount; i++)
                {
                    idx = (_mouseMoveCount + _mouseMoveIndex - i) % _mouseMoveCount;
                    if(_mouseMoveTime[idx] < time_start) {
                        break;
                    }
                    vector += _mouseMoveY[idx] - last;
                }

                _scrollHeight = vector;
            }
            else
            {
                _scrolling = 0;
            }

            this.Invalidate();
        }

        /// <summary>
        /// クリックイベント
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClick(EventArgs e)
        {
            bool handled = false;
#if !PocketPC
            _pressAndHold = false;
#endif
            if (_selectedIndex != -1)
            {
                handled = (_items[_selectedIndex] as ListBoxExRow).OnClick();
            }

            if (handled == false)
            {
                base.OnClick(e);
            }
        }

        /// <summary>
        /// 指定したＹ座標の行を取得する
        /// </summary>
        /// <param name="clickTop"></param>
        /// <returns></returns>
        private int GetSelectedIndex(int clickTop)
        {
            if (_items.Count > 0 && clickTop >= 0 && clickTop <= _rowsHeight)
            {
                int idxLeft = 0;
                int idxRight = _items.Count - 1;
                int idx = (idxLeft + idxRight) / 2;

                ListBoxExRow row;
                while (true)
                {
                    row = _items[idx] as ListBoxExRow;
                    if (row.Top <= clickTop && clickTop < row.Top + row.Height)
                    {
                        return idx;
                    }
                    if (idxLeft == idxRight)
                    {
//                        selectedIndex = idxRight;
                        break;
                    }
                    if (row.Top > clickTop)
                    {
                        idxRight = idx - 1;
                        idx = (idxLeft + idxRight) / 2;
                    }
                    else
                    {
                        idxLeft = idx + 1;
                        idx = (idxLeft + idxRight) / 2;
                    }
                }
            }

            return -1;
        }

        delegate void lbeItemsRefresh();

        const int maxmove = 120;
        /// <summary>
        /// スクロール用タイマー処理
        /// Windowsプレス＆ホールド処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void TimerScrolling(object sender, EventArgs e)
        {
            bool lastDraw = false;

#if !PocketPC
            if (_pressAndHold)
            {
                if (_lastPress.AddSeconds(1) < DateTime.Now)
                {
                    _pressAndHold = false;
                    _dragging = false;
                    _dragon = false;
                    // 長押し
                    if (this.ContextMenu != null)
                    {
                        // ContextMenu
                        base.ContextMenu.Show(this, new Point(_startDrag.X, _startDrag.Y));
                        //handled = true;
                    }
                }
            }
#endif

            if (_scrolling == 1)
            {
                // 慣性スクロール処理
                if (Math.Abs(_scrollHeight) > 5)
                {
                    if (Math.Abs(_scrollHeight) > maxmove)
                    {
                        if (_scrollHeight > 0)
                        {
                            _scrollTop += maxmove;
                        }
                        else
                        {
                            _scrollTop -= maxmove;
                        }
                    }
                    else
                    {
                        _scrollTop += (int)_scrollHeight;
                    }
                    // 下端
                    if (_scrollTop > _rowsHeight - _height)
                    {
                        _scrollTop = _rowsHeight - _height;
                        lastDraw = true;
                        _scrollHeight = 0;
                    }
                    // 上端
                    if (_scrollTop < 0)
                    {
                        _scrollTop = 0;
                        lastDraw = true;
                        _scrollHeight = 0;
                    }

                    _scrollHeight *= 0.92F;
                }
                else {
                    lastDraw = true;
                }
            }
            else if (_scrolling == 2)
            {
                // ページスクロール処理
                if (Math.Abs(_scrollTop - _newtop) < _smoothMove)
                {
                    _scrollTop = _newtop;
                    lastDraw = true;
                }
                else
                {
                    if (_scrollTop > _newtop)
                    {
                        _scrollTop -= _smoothMove;
                    }
                    else
                    {
                        _scrollTop += _smoothMove;
                    }
                }
            }

            // 描画中なので終了
            if (_timerScrollDrawing)
            {
                return;
            }
            _timerScrollDrawing = true;

            if (_scrolling != 0)
            {
                if (lastDraw)
                {
                    _scrolling = 0;
                }
#if !DEBUG
                try
                {
#endif
                    this.Invalidate();
#if !DEBUG
                }
                catch (ObjectDisposedException ode) { }
                catch { }   // スクロール中にフォームを閉じるとエラーになるのでスルーさせる
#endif
            }
            _timerScrollDrawing = false;
        }

        /// <summary>
        /// 表示先頭座標
        /// </summary>
        public int ScrollTop
        {
            get { return _scrollTop; }
            set { _scrollTop = value; }
        }

        /// <summary>
        /// オブジェクト描画処理
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            if (_screenupdating == false)
            {
                return;
            }

            int scrollTop = _scrollTop;     // 途中で変更されると描画がおかしくなるので以後保存した値を使用する

#if PocketPC
            long time_start = _time.Now.Ticks;      // 性能測定
#else
            long time_start = DateTime.Now.Ticks;
#endif
            // 何番目のアイテムから表示するか
            int viewitem = GetSelectedIndex(scrollTop);
            int hideheight = 0;
            if (viewitem == -1)
            {
                scrollTop = 0;
                hideheight = 0;
                viewitem = 0;
            }
            else
            {
                if (_items.Count > 0)
                {
                    hideheight = scrollTop - _items[viewitem].Top;
                }
            }

            Graphics g = _graphics;

            // 背景色塗りつぶし
            g.FillRectangle(new SolidBrush(base.BackColor), 0, hideheight, this.Width, this.Height);

            // 背景画貼り付け
            if (_backgroundImage != null)
            {
#if !DEBUG
                try
                {
#endif
                    g.DrawImage(_backgroundImage, _backgroundX, hideheight + _backgroundY);
#if !DEBUG
                }
                catch { }
#endif
            }

            int topoffset = 0;
            if (Border.Top || Border.Value)
            {
                topoffset = 1;
            }
            int drawtop = topoffset;
            if (_items.Count > 0)
            {
                // 表示アイテム分処理する
                ListBoxExRow item;
                bool tinydraw = _dragging | (_scrolling != 0);                  // スクロール中のときに簡易表示にする。
                for (int i = viewitem; i < _items.Count; i++)
                {
                    item = _items[i] as ListBoxExRow;
                    bool selected = false;
                    switch (_selectionMode)
                    {
                        case SelectionMode.One:
                            selected = (_selectedIndex == i) ? true : false;
                            break;
                        case SelectionMode.MultiSimple:
                        case SelectionMode.MultiExtended:
                            selected = item.Selected;
                            break;
                    }

                    // 選択時の背景色塗りつぶし
                    if (selected == true && item.DrawingSelectedBackground)
                    {
                        if (_backgroundImage == null)
                        {
                            // 背景画像指定なし
                            g.FillRectangle(new SolidBrush(_selectedBackColor), 0, drawtop, Width, item.Height);
                        }
                        else
                        {
                            // 背景画像指定あり（半透明で描画）
                            //GraphicExtentions.FillRectangleAlpha(g, new SolidBrush(_selectedBackColor), 0, drawtop, Width, item.Height, 128);
#if PocketPC
                            g.FillRectangle(new SolidBrush(_selectedBackColor), 0, drawtop, Width, item.Height);
#else
                            g.FillRectangle(new SolidBrush(Color.FromArgb(160, _selectedBackColor.R, _selectedBackColor.G, _selectedBackColor.B)), 0, drawtop, Width, item.Height);
#endif
                        }
                    }

                    // アイテム描画処理呼び出し
                    item.Draw(g, 0, drawtop, tinydraw, selected);

                    // カーソル
                    if (_selectionMode != SelectionMode.One && _selectedIndex == i)
                    {
                        Pen linepen = new Pen(_lineColor);
                        linepen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                        g.DrawRectangle(linepen, 1, drawtop + 1, item.Width - 3, item.Height - 4);
                    }

                    drawtop += item.Height;
                    // 描画終了判定
                    if (drawtop - hideheight > Height)
                    {
                        break;
                    }
                }
            }

            // クリック中はスクロールバーも描画
            int barwidth = 8;
            SolidBrush brush = new SolidBrush(_scrollBarColor);
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

            if (_rowsHeight > 0)
            {
                y = (int)((float)(Height - d2 - 2) * ((float)scrollTop / (float)_rowsHeight)) + hideheight;

                if (_dragging || (_scrolling != 0))
                {
                    // スクロールバー
                    x = Width - barwidth - 4;

                    g.FillEllipse(brush, x, y, barwidth, barwidth);
                    g.FillRectangle(brush, x, y + barwidth / 2, barwidth + 1, d);
                    g.FillEllipse(brush, x, y + d, barwidth, barwidth);
                }
                else
                {
                    // スクロールバー
                    x = Width - 1;

                    g.DrawLine(new Pen(_scrollBarColor, 2), x, y, x, y + d + barwidth);
                }
            }

            // 枠描画
            // 外周
            if (_border.Value)
            {
                g.DrawRectangle(new Pen(_lineColor), new Rectangle(0, hideheight, Width - 1, Height - 1));
            }
            else
            {
                // 上線
                if (_border.Top)
                {
                    g.DrawLine(new Pen(_lineColor, 1), 0, hideheight, Width - 1, hideheight);
                }

                // 下線
                if (_border.Bottom)
                {
                    g.DrawLine(new Pen(_lineColor, 1), 0, hideheight + Height - 1, Width - 1, hideheight + Height - 1);
                }

                // 左線
                if (_border.Left)
                {
                    g.DrawLine(new Pen(_lineColor, 1), 0, hideheight - 1, 0, hideheight + Height - 1);
                }

                // 右線
                if (_border.Right)
                {
                    g.DrawLine(new Pen(_lineColor, 1), Width - 1, hideheight - 1, Width - 1, hideheight + Height - 1);
                }
            }

            //g.Dispose();

            e.Graphics.DrawImage(_offscreen, 0, -hideheight);
#if PocketPC
            long elapsed = (int)((_time.Now.Ticks - time_start) / 10000);          // 性能測定
#else
            long elapsed = (int)((DateTime.Now.Ticks - time_start) / 10000);          // 性能測定
#endif
            // debug
            // イベント履歴
            //string msg = string.Join("\n", msgs.ToArray());
            //e.Graphics.DrawString(msg, new Font(System.Windows.Forms.Control.DefaultFont.Name, 7, FontStyle.Regular), new SolidBrush(Color.Red), new RectangleF(3, 3, this.Width, this.Height));
            // 描画時間
            //e.Graphics.DrawString(string.Format("e={0} s={1}", elapsed, _scrollHeight), new Font(this.Font.Name, 10, FontStyle.Regular), new SolidBrush(_lineColor), new RectangleF(10, Height - 30, 600, 30));
        }

        /// <summary>
        /// バックグラウンドの描画
        /// これを定義しないと描画ごとに背景描画処理が入る
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            ;
        }

        /// <summary>
        /// オブジェクトのリサイズ
        /// </summary>
        /// <param name="e"></param>
        protected override void OnResize(EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;

            SetWidth(base.Width);
            if (_scrollTop < 0)
            {
                _scrollTop = 0;
            }
            else if (_scrollTop > _rowsHeight - this.Height)
            {
                _scrollTop = _rowsHeight - this.Height;
                if (_scrollTop < 0)
                {
                    _scrollTop = 0;
                }
            }
            BackGroundPosition();
            
            this.Invalidate();

            Cursor.Current = Cursors.Default;
        }

        /// <summary>
        /// ドラッグ中のマウス座標の記録
        /// </summary>
        /// <param name="y"></param>
        private void AddEventY(int y)
        {
#if PocketPC
            long now = _time.Now.Ticks;
#else
            long now = DateTime.Now.Ticks;
#endif
            _mouseMoveIndex = (_mouseMoveIndex + 1) % _mouseMoveCount;
            _mouseMoveY[_mouseMoveIndex] = y;
            _mouseMoveTime[_mouseMoveIndex] = now;
        }

        private void AddMsg(string msg)
        {
            msgs.Add(msg);
            if (msgs.Count > 20)
            {
                msgs.RemoveAt(0);
            }
        }
    }

    /// <summary>
    /// 行アイテムの基本クラス
    /// </summary>
    class ListBoxExRow
    {
        private string _id;
        private bool _selected;
        
        protected int _width;
        protected int _height;
        protected int _top;

        protected ListBoxEx _parent;

        public ListBoxExRow()
        {
            _selected = false;
        }

        public string Id
        {
            get { return _id; }
            set { _id = value; }
        }

        public bool Selected
        {
            get { return _selected; }
            set { _selected = value; }
        }

        public virtual int Width
        {
            get { return _width; }
            set { _width = value; }
        }

        public virtual int Height
        {
            get { return _height; }
            set
            {
                _height = value;
                if (_parent != null)
                {
                    _parent.SetItemTops();
                }
            }
        }

        public virtual int Top
        {
            get { return _top; }
            set { _top = value; }
        }

        public virtual ListBoxEx Parent
        {
            get { return _parent; }
            set { _parent = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>処理を行ったらtrueを返す</returns>
        public virtual bool OnClick()
        {
            if (this.Click != null)
            {
                this.Click(this, new EventArgs());
                return true;
            }
            else
            {
                return false;
            }
        }
        public event EventHandler Click;

        /// <summary>
        /// 選択時にListBoxExが背景色を描画するか
        /// </summary>
        /// <returns></returns>
        public virtual bool DrawingSelectedBackground
        {
            get { return true; }
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

    class ObjectCollection
    {
        private List<ListBoxExRow> _object;
        private ListBoxEx _parent;

        public ObjectCollection()
        {
            _object = new List<ListBoxExRow>();
        }

        public ListBoxExRow this[int index]
        {
            get
            {
                if (index >= 0 && index < _object.Count)
                {
                    return _object[index];
                }
                else
                {
                    return null;
                }
            }
        }

        public void Add(ListBoxExRow item)
        {
            // 幅の指定
#if !DEBUG
            try
            {
#endif
                item.Width = _parent.Width;
#if !DEBUG
            }
            catch { }
#endif
            // アイテムを追加
            _object.Add(item);
            item.Parent = _parent;
            // 高さの更新
            _parent.SetItemTops();
            _parent.Invalidate();
        }

        public int Count
        {
            get
            {
                return _object.Count;
            }
        }

        public void Clear()
        {
            // アイテムの削除
            _object.Clear();
            // 高さの更新
            _parent.ScrollTop = 0;
            _parent.SelectedIndex = -1;
            _parent.SetItemTops();
            _parent.Invalidate();
        }

        public void Insert(int index, ListBoxExRow item)
        {

            // 幅の指定
#if !DEBUG
            try
            {
#endif
                item.Width = _parent.Width;
                // 座標調整
                if (index < _object.Count)
                {
                    if (_parent.Items[index].Top < _parent.ScrollTop)
                    {
                        _parent.ScrollTop += item.Height;
                    }
                }
#if !DEBUG
            }
            catch { }
#endif
            // アイテムの挿入
            _object.Insert(index, item);
            item.Parent = _parent;
            // 高さの更新
            _parent.SetItemTops();
            _parent.Invalidate();
        }

        public void Remove(ListBoxExRow item)
        {
            if (item.Top < _parent.ScrollTop)
            {
                _parent.ScrollTop -= item.Height;
            }

            // アイテムの削除
            _object.Remove(item);
            // 高さの更新
            _parent.SetItemTops();
            _parent.Invalidate();
        }

        public void RemoveAt(int index)
        {
            if (index < _object.Count)
            {
                if (_parent.Items[index].Top < _parent.ScrollTop)
                {
                    _parent.ScrollTop -= _parent.Items[index].Height;
                }
            }

            // アイテムの削除
            _object.RemoveAt(index);
            // 高さの更新
            _parent.SetItemTops();
            _parent.Invalidate();
        }

        public void Sort()
        {
            _object.Sort();
        }

        public void Sort(Comparison<ListBoxExRow> comparison)
        {
            _object.Sort(comparison);
        }

        public void Sort(IComparer comparer)
        {
            _object.Sort((IComparer<ListBoxExRow>)comparer);
        }

        public List<ListBoxExRow>.Enumerator GetEnumerator()
        {
            return _object.GetEnumerator();
        }

        public ListBoxEx Parent
        {
            get { return _parent; }
            set { _parent = value; }
        }
    }

    // 罫線クラス
    class Border
    {
        enum BorderValue
        {
            BorderTop = 1,                      // 枠線：上
            BorderBottom = 2,                   // 枠線：下
            BorderLeft = 4,                     // 枠線：左
            BorderRight = 8,                    // 枠線：右
        };

        bool _borderValue = false;
        bool _borderTop = false;
        bool _borderBottom = false;
        bool _borderLeft = false;
        bool _borderRight = false;

        public Border()
        {

        }

        public bool Value
        {
            get { return _borderValue; }
            set { _borderValue = value; }
        }

        public bool Top
        {
            get { return _borderTop; }
            set { _borderTop = value; }
        }

        public bool Bottom
        {
            get { return _borderBottom; }
            set { _borderBottom = value; ; }
        }

        public bool Left
        {
            get { return _borderLeft; }
            set { _borderLeft = value; }
        }

        public bool Right
        {
            get { return _borderRight; }
            set { _borderRight = value; }
        }

    }

#if PocketPC
    // タイムクラス
    class CFTime
    {
        private DateTime _date;
        private double _start;
        private long _frq = Frq;

        public DateTime Now
        {
            get
            {
                long time = (long)((Time - _start) * 10000000.0 / _frq);
                return new DateTime(time + _date.Ticks);
            }
        }

        public static long Time
        {
            get
            {
                long cnt = 0;
                QueryPerformanceCounter(ref cnt);
                return cnt;
            }
        }

        public static long Frq
        {
            get
            {
                long frq = 0;
                QueryPerformanceFrequency(ref frq);
                return frq;
            }
        }

        public CFTime(DateTime date)
        {
            _start = Time;
            _date = date;
        }

        [DllImport("CoreDll.dll")]
        extern static short QueryPerformanceCounter(ref long x);

        [DllImport("CoreDll.dll")]
        extern static short QueryPerformanceFrequency(ref long x);

    }
#endif

}
