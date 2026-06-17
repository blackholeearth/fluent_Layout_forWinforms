using System.Runtime.InteropServices;

namespace SmartLayoutEngine;



public class SmartFormResizable : NativeWindow
{
	private Form _form;
	private int _borderSize;
	private bool _smallCorners;

	private Control _titleBarContainer;
	private List<Control> _interactiveControls = new List<Control>();
	private Button _minBtn, _maxBtn, _closeBtn;

	// Hover efektleri için renkler
	public Color MinHoverColor { get; set; } = Color.FromArgb(230, 230, 230);
	public Color MaxHoverColor { get; set; } = Color.FromArgb(230, 230, 230);
	public Color CloseHoverColor { get; set; } = Color.FromArgb(232, 17, 35);
	public Color NormalForeColor { get; set; } = Color.FromArgb(32, 32, 32);

	private Button _currentHoveredBtn = null;
	private System.Windows.Forms.Timer _hoverTimer;

	public void AddInteractiveControl(Control c) => _interactiveControls.Add(c);

	public Control TitleBarContainer { get; private set; }
	public Control MinimizeButton { get; private set; }
	public Control MaximizeButton { get; private set; }
	public Control CloseButton { get; private set; }

	[DllImport("dwmapi.dll")]
	private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

	private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
	private const int DWMWCP_ROUND = 2;
	private const int DWMWCP_ROUNDSMALL = 3;

	[DllImport("user32.dll")]
	private static extern int GetSystemMetrics(int nIndex);

	[StructLayout(LayoutKind.Sequential)]
	private struct RECT
	{
		public int Left, Top, Right, Bottom;
	}



	public SmartFormResizable(Form form, int borderSize = 8, bool smallCorners = false)
	{
		_form = form;
		_borderSize = borderSize;
		_smallCorners = smallCorners;

		form.FormBorderStyle = FormBorderStyle.Sizable;
		form.MaximizeBox = true;
		form.MinimizeBox = true;

		if (form.IsHandleCreated)
		{
			this.AssignHandle(form.Handle);
			ApplyWin11RoundedCorners(form.Handle);
		}
		else
		{
			form.HandleCreated += (s, e) => {
				this.AssignHandle(form.Handle);
				ApplyWin11RoundedCorners(form.Handle);
			};
		}

		form.FormClosed += (s, e) => {
			_hoverTimer?.Stop();
			this.ReleaseHandle();
		};

		// Buton hover takibi için Timer başlat
		_hoverTimer = new System.Windows.Forms.Timer { Interval = 50 };
		_hoverTimer.Tick += HoverTimer_Tick;
		_hoverTimer.Start();
	}

	public void BindCaptionControls(Control titleBar, Control minBtn, Control maxBtn, Control closeBtn)
	{
		_titleBarContainer = titleBar;
		_minBtn = minBtn as Button;
		_maxBtn = maxBtn as Button;
		_closeBtn = closeBtn as Button;
	}

	private void HoverTimer_Tick(object sender, EventArgs e)
	{
		if (_form == null || _form.IsDisposed || !_form.Visible)
		{
			SetHover(null, Color.Empty);
			return;
		}

		Point screenPos = Cursor.Position;

		if (_titleBarContainer == null || !_titleBarContainer.RectangleToScreen(_titleBarContainer.ClientRectangle).Contains(screenPos))
		{
			SetHover(null, Color.Empty);
			return;
		}

		if (_closeBtn != null && _closeBtn.Visible && _closeBtn.RectangleToScreen(_closeBtn.ClientRectangle).Contains(screenPos))
			SetHover(_closeBtn, CloseHoverColor);
		else if (_maxBtn != null && _maxBtn.Visible && _maxBtn.RectangleToScreen(_maxBtn.ClientRectangle).Contains(screenPos))
			SetHover(_maxBtn, MaxHoverColor);
		else if (_minBtn != null && _minBtn.Visible && _minBtn.RectangleToScreen(_minBtn.ClientRectangle).Contains(screenPos))
			SetHover(_minBtn, MinHoverColor);
		else
			SetHover(null, Color.Empty);
	}

	private void SetHover(Button btn, Color hoverColor)
	{
		if (btn == _currentHoveredBtn) return;

		if (_currentHoveredBtn != null)
		{
			_currentHoveredBtn.BackColor = Color.Transparent;
			_currentHoveredBtn.ForeColor = NormalForeColor;
		}

		_currentHoveredBtn = btn;

		if (btn != null)
		{
			btn.BackColor = hoverColor;
			if (btn == _closeBtn) btn.ForeColor = Color.White;
		}
	}

	private void ApplyWin11RoundedCorners(IntPtr hwnd)
	{
		try
		{
			if (Environment.OSVersion.Version.Build >= 22000)
			{
				int preference = _smallCorners ? DWMWCP_ROUNDSMALL : DWMWCP_ROUND;
				DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));
			}
		}
		catch { }
	}

	protected override void WndProc(ref Message m)
	{
		const int WM_NCCALCSIZE = 0x0083;
		const int WM_NCHITTEST = 0x0084;
		const int WM_NCLBUTTONDOWN = 0xA1;

		const int HTTRANSPARENT = -1;
		const int HTLEFT = 10, HTRIGHT = 11, HTTOP = 12, HTTOPLEFT = 13, HTTOPRIGHT = 14;
		const int HTBOTTOM = 15, HTBOTTOMLEFT = 16, HTBOTTOMRIGHT = 17;
		const int HTCAPTION = 2, HTCLIENT = 1;
		const int HTMINBUTTON = 8, HTMAXBUTTON = 9, HTCLOSE = 20;

		//if (m.Msg == WM_NCCALCSIZE) { m.Result = IntPtr.Zero; return; }
		// 🌟 MAXIMIZE KESİLME SORUNU ÇÖZÜMÜ BURASI
		if (m.Msg == WM_NCCALCSIZE)
		{
			// Form maximize ediliyorsa, Windows'un eklediği görünmez kenarlıkları kırparız
			if (_form.WindowState == FormWindowState.Maximized && m.WParam != IntPtr.Zero)
			{
				int borderX = GetSystemMetrics(32) + GetSystemMetrics(92); // SM_CXSIZEFRAME + SM_CXPADDEDBORDER
				int borderY = GetSystemMetrics(33) + GetSystemMetrics(92); // SM_CYSIZEFRAME + SM_CXPADDEDBORDER

				RECT rect = (RECT)Marshal.PtrToStructure(m.LParam, typeof(RECT));
				rect.Left += borderX;
				rect.Top += borderY;
				rect.Right -= borderX;
				rect.Bottom -= borderY;
				Marshal.StructureToPtr(rect, m.LParam, false);
			}
			m.Result = IntPtr.Zero;
			return;
		}

		if (m.Msg == WM_NCHITTEST)
		{
			Point screenPoint = new Point(m.LParam.ToInt32());
			Point clientPoint = _form.PointToClient(screenPoint);
			int b = _borderSize;

			// 1. Kenarlardan boyutlandırma
			if (clientPoint.Y < b)
			{
				if (clientPoint.X < b) { m.Result = (IntPtr)HTTOPLEFT; return; }
				if (clientPoint.X > _form.Width - b) { m.Result = (IntPtr)HTTOPRIGHT; return; }
				m.Result = (IntPtr)HTTOP; return;
			}
			if (clientPoint.Y > _form.Height - b)
			{
				if (clientPoint.X < b) { m.Result = (IntPtr)HTBOTTOMLEFT; return; }
				if (clientPoint.X > _form.Width - b) { m.Result = (IntPtr)HTBOTTOMRIGHT; return; }
				m.Result = (IntPtr)HTBOTTOM; return;
			}
			if (clientPoint.X < b) { m.Result = (IntPtr)HTLEFT; return; }
			if (clientPoint.X > _form.Width - b) { m.Result = (IntPtr)HTRIGHT; return; }

			// 2. Başlık çubuğu alanı
			if (_titleBarContainer != null && _titleBarContainer.Visible)
			{
				Rectangle titleRect = _titleBarContainer.RectangleToScreen(_titleBarContainer.ClientRectangle);
				if (titleRect.Contains(screenPoint))
				{
					// Sistem Butonları (Hover ve Snap Layout için HT* döndürüyoruz)
					if (_closeBtn != null && _closeBtn.Visible &&
						_closeBtn.RectangleToScreen(_closeBtn.ClientRectangle).Contains(screenPoint))
					{
						m.Result = (IntPtr)HTCLOSE; return;
					}
					if (_maxBtn != null && _maxBtn.Visible &&
						_maxBtn.RectangleToScreen(_maxBtn.ClientRectangle).Contains(screenPoint))
					{
						m.Result = (IntPtr)HTMAXBUTTON; return;
					}
					if (_minBtn != null && _minBtn.Visible &&
						_minBtn.RectangleToScreen(_minBtn.ClientRectangle).Contains(screenPoint))
					{
						m.Result = (IntPtr)HTMINBUTTON; return;
					}

					// İnteraktif kontroller (Arama kutusu, hamburger menü vb.)
					foreach (var ctrl in _interactiveControls)
					{
						if (ctrl.Visible && ctrl.RectangleToScreen(ctrl.ClientRectangle).Contains(screenPoint))
						{
							m.Result = (IntPtr)HTCLIENT; return;
						}
					}

					// Geriye kalan her yer (ikon, başlık, boşluk)
					m.Result = (IntPtr)HTCAPTION; return;
				}
			}
		}
		else if (m.Msg == WM_NCLBUTTONDOWN)
		{
			// Butonlar HTTRANSPARENT olduğu için tıklama Form'a NC tıklama olarak geliyor.
			// HTMINBUTTON, HTMAXBUTTON, HTCLOSE için manuel tıklama olaylarını tetikliyoruz.
			int ht = m.WParam.ToInt32();
			if (ht == HTCLOSE) { _form.Close(); return; }
			if (ht == HTMAXBUTTON)
			{
				if (_form.WindowState == FormWindowState.Maximized) _form.WindowState = FormWindowState.Normal;
				else _form.WindowState = FormWindowState.Maximized;
				return;
			}
			if (ht == HTMINBUTTON) { _form.WindowState = FormWindowState.Minimized; return; }
		}

		base.WndProc(ref m);
	}

}



// --- 🗺️ %100 NATIVE BORDERLESS RESIZABLE VE GEÇİRGENLİK YAPISI (WM_NCCALCSIZE + HTTRANSPARENT) ---
// --- 🗺️ %100 NATIVE BORDERLESS RESIZABLE YAPISI (WM_NCCALCSIZE + HTTRANSPARENT + SNAP LAYOUTS) ---
// --- 🗺️ %100 NATIVE BORDERLESS RESIZABLE YAPISI (WM_NCCALCSIZE + SNAP LAYOUTS & STYLES) ---
public class SmartFormResizable_old : NativeWindow
{
	private Form _form;
	private int _borderSize;
	private bool _smallCorners;

	private Control _titleBarContainer;
	private List<Control> _interactiveControls = new List<Control>();
	private Button _minBtn, _maxBtn, _closeBtn;

	public void AddInteractiveControl(Control c) => _interactiveControls.Add(c);

	// Custom Buton Referansları
	public Control TitleBarContainer { get; private set; }
	public Control MinimizeButton { get; private set; }
	public Control MaximizeButton { get; private set; }
	public Control CloseButton { get; private set; }

	[DllImport("dwmapi.dll")]
	private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

	private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
	private const int DWMWCP_ROUND = 2;
	private const int DWMWCP_ROUNDSMALL = 3;

	public SmartFormResizable_old(Form form, int borderSize = 8, bool smallCorners = false)
	{
		_form = form;
		_borderSize = borderSize;
		_smallCorners = smallCorners;

		// 🌟 NATIVE BARI KORU VE SISTEME BU FORMUN MAXIMIZE EDILEBILIR OLDUGUNU SÖYLE!
		// Bu iki özellik kapalı olursa Windows 11 Snap Layouts menüsünü asla açmaz.
		form.FormBorderStyle = FormBorderStyle.Sizable;
		form.MaximizeBox = true;
		form.MinimizeBox = true;

		if (form.IsHandleCreated)
		{
			this.AssignHandle(form.Handle);
			ApplyWin11RoundedCorners(form.Handle);
		}
		else
		{
			form.HandleCreated += (s, e) =>
			{
				this.AssignHandle(form.Handle);
				ApplyWin11RoundedCorners(form.Handle);
			};
		}


		form.ControlAdded += (s, e) =>
		{
			var container = _titleBarContainer; // may be null initially
			RegisterChildRecursive(e.Control, container);
		};
		foreach (Control child in form.Controls)
		{
			RegisterChildRecursive(child);
		}


		form.FormClosed += (s, e) => this.ReleaseHandle();
	}

	public void BindCaptionControls(Control titleBar, Control minBtn, Control maxBtn, Control closeBtn)
	{
		_titleBarContainer = titleBar;
		_minBtn = minBtn as Button;
		_maxBtn = maxBtn as Button;
		_closeBtn = closeBtn as Button;
	}

	private void RegisterChildRecursive(Control child, Control titleBarContainer = null)
	{
		child.ControlAdded += (s, e) => RegisterChildRecursive(e.Control, titleBarContainer);
		foreach (Control subChild in child.Controls)
		{
			RegisterChildRecursive(subChild, titleBarContainer);
		}
	}



	private void ApplyWin11RoundedCorners(IntPtr hwnd)
	{
		try
		{
			if (Environment.OSVersion.Version.Build >= 22000)
			{
				int preference = _smallCorners ? DWMWCP_ROUNDSMALL : DWMWCP_ROUND;
				DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));
			}
		}
		catch { }
	}

	protected override void WndProc(ref Message m)
	{
		const int WM_NCCALCSIZE = 0x0083;
		const int WM_NCHITTEST = 0x0084;
		const int HTTRANSPARENT = -1;
		const int HTLEFT = 10, HTRIGHT = 11, HTTOP = 12, HTTOPLEFT = 13, HTTOPRIGHT = 14;
		const int HTBOTTOM = 15, HTBOTTOMLEFT = 16, HTBOTTOMRIGHT = 17;
		const int HTCAPTION = 2, HTCLIENT = 1;
		const int HTMINBUTTON = 8, HTMAXBUTTON = 9, HTCLOSE = 20;

		if (m.Msg == WM_NCCALCSIZE) { m.Result = IntPtr.Zero; return; }

		if (m.Msg == WM_NCHITTEST)
		{
			Point screenPoint = new Point(m.LParam.ToInt32());
			Point clientPoint = _form.PointToClient(screenPoint);
			int b = _borderSize;

			// 1. Border resizing (edges)
			if (clientPoint.Y < b)
			{
				if (clientPoint.X < b) { m.Result = (IntPtr)HTTOPLEFT; return; }
				if (clientPoint.X > _form.Width - b) { m.Result = (IntPtr)HTTOPRIGHT; return; }
				m.Result = (IntPtr)HTTOP; return;
			}
			if (clientPoint.Y > _form.Height - b)
			{
				if (clientPoint.X < b) { m.Result = (IntPtr)HTBOTTOMLEFT; return; }
				if (clientPoint.X > _form.Width - b) { m.Result = (IntPtr)HTBOTTOMRIGHT; return; }
				m.Result = (IntPtr)HTBOTTOM; return;
			}
			if (clientPoint.X < b) { m.Result = (IntPtr)HTLEFT; return; }
			if (clientPoint.X > _form.Width - b) { m.Result = (IntPtr)HTRIGHT; return; }

			// 2. Title bar area – only if we have a container
			if (_titleBarContainer != null && _titleBarContainer.Visible)
			{
				Rectangle titleRect = _titleBarContainer.RectangleToScreen(_titleBarContainer.ClientRectangle);
				if (titleRect.Contains(screenPoint))
				{
					// Caption buttons (their hit-test is transparent, so the Form gets the message)
					if (_closeBtn != null && _closeBtn.Visible &&
						_closeBtn.RectangleToScreen(_closeBtn.ClientRectangle).Contains(screenPoint))
					{
						m.Result = (IntPtr)HTCLOSE; return;
					}
					if (_maxBtn != null && _maxBtn.Visible &&
						_maxBtn.RectangleToScreen(_maxBtn.ClientRectangle).Contains(screenPoint))
					{
						m.Result = (IntPtr)HTMAXBUTTON; return;
					}
					if (_minBtn != null && _minBtn.Visible &&
						_minBtn.RectangleToScreen(_minBtn.ClientRectangle).Contains(screenPoint))
					{
						m.Result = (IntPtr)HTMINBUTTON; return;
					}

					// Interactive controls (search box, hamburger, etc.)
					foreach (var ctrl in _interactiveControls)
					{
						if (ctrl.Visible && ctrl.RectangleToScreen(ctrl.ClientRectangle).Contains(screenPoint))
						{
							m.Result = (IntPtr)HTCLIENT; return;
						}
					}

					// Everything else in the title bar → draggable
					m.Result = (IntPtr)HTCAPTION; return;
				}
			}
		}
		base.WndProc(ref m);
	}

}





//// --- 🌟 ÇOCUK KONTROLLER İÇİN MOUSE GEÇİRGENLİK FİLTRESİ ---
//public class SmartChildResizeFilter : NativeWindow
//{
//	private Form _form;
//	private int _borderSize;
//	private Control _control;
//	private Control _titleBarContainer;

//	public SmartChildResizeFilter(Form form, Control control, int borderSize, Control titleBarContainer = null)
//	{
//		_form = form;
//		_control = control;
//		_borderSize = borderSize;
//		_titleBarContainer = titleBarContainer;

//		if (control.IsHandleCreated) this.AssignHandle(control.Handle);
//		else control.HandleCreated += (s, e) => this.AssignHandle(control.Handle);

//		control.HandleDestroyed += (s, e) => this.ReleaseHandle();
//	}

//	protected override void WndProc(ref Message m)
//	{
//		const int WM_NCHITTEST = 0x0084;
//		const int HTTRANSPARENT = -1;

//		if (m.Msg == WM_NCHITTEST)
//		{
//			Point screenPoint = new Point(m.LParam.ToInt32());
//			Point clientPoint = _form.PointToClient(screenPoint);

//			// 1. Border resizing – always transparent so form can resize
//			if (clientPoint.X < _borderSize ||
//				clientPoint.X > _form.Width - _borderSize ||
//				clientPoint.Y > _form.Height - _borderSize)
//			{
//				m.Result = (IntPtr)HTTRANSPARENT;
//				return;
//			}

//			// 2. Title bar area – only if this control IS the title bar container
//			int titleHeight = (int)(48 * (_form.DeviceDpi / 96f)); // match default
//			if (clientPoint.Y < titleHeight)
//			{
//				if (_control == _titleBarContainer)
//				{
//					m.Result = (IntPtr)HTTRANSPARENT; // let form handle caption
//					return;
//				}
//			}
//		}
//		base.WndProc(ref m);
//	}
//}

public class SmartTransparentControlFilter : NativeWindow
{
	public SmartTransparentControlFilter(Control control)
	{
		if (control.IsHandleCreated)
			this.AssignHandle(control.Handle);
		else
			control.HandleCreated += (s, e) => this.AssignHandle(control.Handle);

		control.HandleDestroyed += (s, e) => this.ReleaseHandle();
	}

	protected override void WndProc(ref Message m)
	{
		const int WM_NCHITTEST = 0x0084;
		const int HTTRANSPARENT = -1;

		if (m.Msg == WM_NCHITTEST)
		{
			// The mouse goes right through – the parent Form will handle it
			m.Result = (IntPtr)HTTRANSPARENT;
			return;
		}
		base.WndProc(ref m);
	}
}

/// <summary>
/// --- 🌟 NATIVE SISTEM BUTONLARI GEÇİRGENLİK FİLTRESİ ---
/// </summary>
public class SmartCaptionControlFilter : NativeWindow
{
	public SmartCaptionControlFilter(Control control)
	{
		if (control.IsHandleCreated) this.AssignHandle(control.Handle);
		else control.HandleCreated += (s, e) => this.AssignHandle(control.Handle);
		control.HandleDestroyed += (s, e) => this.ReleaseHandle();
	}

	protected override void WndProc(ref Message m)
	{
		const int WM_NCHITTEST = 0x0084;
		const int HTTRANSPARENT = -1;

		if (m.Msg == WM_NCHITTEST)
		{
			m.Result = (IntPtr)HTTRANSPARENT; // Butonun mouse'u çalmasını önle, parent Form'a yönlendir!
			return;
		}
		base.WndProc(ref m);
	}
}
