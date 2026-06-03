using smartUI_fluent_Layout;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SmartLayoutEngine;

// --- 🌟 KURAL MOTORU (RAM Dostu, String kullanmaz) ---
public class LayoutProps
{
	public bool GrowW { get; set; }
	public bool GrowH { get; set; }
	public bool Spring { get; set; }
	public bool WrapText { get; set; }
	public int VAlign { get; set; } // 0: Top, 1: Middle, 2: Bottom
	public Control MatchWidthTarget { get; set; }
	public Control AlignRightTarget { get; set; }
	public Padding? CustomPadding { get; set; }
	public Padding? CustomMargin { get; set; }
	public int? ItemSpacing { get; set; }


	// 🌟 YENİ: Yuvarlak Köşe ve Çerçeve Kuralları
	public int? CornerRadius { get; set; }
	public Color? BorderColor { get; set; }
	public float BorderThickness { get; set; }
	public bool IsRoundedHooked { get; set; } // Olayı (Event) iki kez bağlamamak

	public bool IsTextChangeHooked { get; set; } // 🌟 Yeni Eklenti
}

public partial class SmartUI
{
	 
	[DllImport("user32.dll")]
	private static extern int SendMessage(IntPtr hWnd, Int32 wMsg, bool wParam, Int32 lParam);
	private const int WM_SETREDRAW = 11;

	private Form _form;
	private float _dpiScale = 1f;
	private float _zoomFactor = 1.0f;

	private List<RowResult> _rows = new List<RowResult>();
	private List<SmartSidePanel> _sidePanels = new List<SmartSidePanel>();

	private Dictionary<Control, float> _originalFonts = new Dictionary<Control, float>();
	private Dictionary<Control, Size> _originalSizes = new Dictionary<Control, Size>();

	private Button _hamburgerBtn;
	private int _collapseThreshold = 800;
	private bool _isSidebarFlyoutOpen = false;
	private bool _isPerformingLayout = false;

	public SmartUI(Form form)
	{
		_form = form;
		using (Graphics g = form.CreateGraphics())
		{
			_dpiScale = g.DpiX / 96f;
		}

		_form.Load += (s, e) => { RefreshLayout(); RefreshLayout(); };
		_form.Resize += (s, e) => RefreshLayout();
		_form.DoubleBuffered();
	}

	public int Scale(int value) => (int)(value * _dpiScale * _zoomFactor);
	private void RegisterControl(Control c)
	{
		// 1. Zoom Hafızası
		if (!_originalFonts.ContainsKey(c)) _originalFonts[c] = c.Font.Size;
		if (!(c is SmartGroup) && !_originalSizes.ContainsKey(c)) _originalSizes[c] = c.Size;

		// 2. 🌟 REAKTİF METİN GEÇİŞ SİHRİ (TextBox Muafiyetli Entegrasyon)
		var props = c.GetProps();
		if (!props.IsTextChangeHooked)
		{
			props.IsTextChangeHooked = true;
			c.TextChanged += (s, e) =>
			{
				if (_isPerformingLayout) return; // Sonsuz döngü koruması

				// 🌟 BANANA INTEGRATION: 
				// TextBox dışındaki (Label, CheckBox vb.) tüm kontrollerin metni değiştiğinde
				// otomatik boyut esnetmesi devrededir. TextBox'lar ise sabit alanını korur.
				// Bu sayede hem TextBox büzüşmesi engellenir hem de yazarken UI kilitlenmez!
				if (!(c is TextBox))
				{
					// Yeni metne göre ne kadar yer kaplaması gerektiğini Windows'a sor
					Size newPref = c.GetPreferredSize(Size.Empty);

					// Zoom hafızasını güncelle (Geriye bölerek ham boyutunu buluyoruz)
					_originalSizes[c] = new Size(
						(int)(newPref.Width / _zoomFactor),
						(int)(newPref.Height / _zoomFactor)
					);

					// Yeni boyutu kontrole ANINDA ver
					c.Width = newPref.Width;
					if (!(c is TextBox && !((TextBox)c).Multiline))
						c.Height = newPref.Height;
				}

				// Motoru ateşle! Tüm satırları ve grupları yeni genişliğe göre tekrar diz
				if (_form != null && _form.IsHandleCreated)
				{
					RefreshLayout();
				}
			};
		}
	}


	public void SetupResponsiveSidebar(Button btn, int threshold = 800)
	{
		_hamburgerBtn = btn;
		_collapseThreshold = threshold;
		_hamburgerBtn.Click += (s, e) => {
			_isSidebarFlyoutOpen = !_isSidebarFlyoutOpen;
			RefreshLayout();
		};
	}
	public void SetupResponsiveSidebar( int threshold = 800)
	{
		var btnMenu = CreateSidebar_MenuButton();
		_form.Controls.Add(btnMenu);
		
		SetupResponsiveSidebar(btnMenu,threshold);
	}
	private static Button CreateSidebar_MenuButton()
	{
		// 1. Create Menu Button.
		Button myBurger = new()
		{
			Text = SegoeMDL2Icons.HamburgerMenu,
			Font = SegoeMDL2Icons._Font12F,
			FlatStyle = FlatStyle.Flat,
			Size = new Size(40, 40)
		};
		myBurger.FlatStyle = FlatStyle.Flat;
		myBurger.FlatAppearance.BorderSize = 0;
		return myBurger;
	}

	// --- YAPISAL METOTLAR ---
	public RowResult Row(params Control[] controls)
	{
		// ESKİDEN: Panel rowPanel = new Panel();
		// ŞİMDİ: Satırımız da artık zeki bir SmartGroup!
		SmartGroup rowPanel = new SmartGroup
		{
			LayoutOrder = controls.ToList(),
			IsVertical = false,
			BackColor = Color.Transparent,
			Margin = new Padding(0)
		};

		// Kasa küçülmesin, formun sonuna kadar uzasın diye GrowW veriyoruz
		rowPanel.GetProps().GrowW = true;
		_form.Controls.Add(rowPanel);

		foreach (var c in controls)
		{
			c.Anchor = AnchorStyles.Top | AnchorStyles.Left;
			c.Dock = DockStyle.None;
			c.Margin = new Padding(0);
			RegisterControl(c);
			rowPanel.Controls.Add(c);
		}

		var res = new RowResult(this) { Container = rowPanel, ControlsInRow = controls.ToList() };
		_rows.Add(res);
		if (_form.IsHandleCreated) RefreshLayout();
		return res;
	}

	public void Gap(int size)
	{
		_rows.Add(new RowResult(this) { IsGap = true, GapSize = size });
		if (_form.IsHandleCreated) RefreshLayout();
	}

	/// <summary>
	/// expands fills the space.
	/// </summary>
	/// <returns></returns>
	public Control Spring()
	{
		var p = new Panel { Width = 0, Height = 0, BackColor = Color.Transparent, Margin = new Padding(0) };
		p.GetProps().Spring = true; // Kural motoruna bu bir "Yay" diyoruz
		return p;
	}

	public Control Space(int size)
	{
		return new Panel { Width = Scale(size), Height = Scale(size), BackColor = Color.Transparent, Margin = new Padding(0) };
	}

	public Control SidePanel(Side edge, int size, params Control[] controls)
	{
		SmartSidePanel sp = new SmartSidePanel { Edge = edge, BaseSize = size, Content = controls.ToList(), Margin = new Padding(0) };
		foreach (var c in controls)
		{
			c.Anchor = AnchorStyles.Top | AnchorStyles.Left;
			c.Margin = new Padding(0);
			sp.Controls.Add(c);
			RegisterControl(c);
		}
		_form.Controls.Add(sp);
		_sidePanels.Add(sp);
		RefreshLayout();
		return sp;
	}

	public Control Group(params Control[] controls) => CreateSmartGroup(false, controls);
	public Control Col(params Control[] controls) => CreateSmartGroup(true, controls);

	private Control CreateSmartGroup(bool isVertical, Control[] controls)
	{
		SmartGroup sg = new SmartGroup { LayoutOrder = controls.ToList(), IsVertical = isVertical, Margin = new Padding(0) };
		foreach (var c in controls)
		{
			c.Anchor = AnchorStyles.Top | AnchorStyles.Left;
			c.Dock = DockStyle.None;
			c.Margin = new Padding(0);
			RegisterControl(c);
			sg.Controls.Add(c);
		}
		Arrange(sg);
		return sg;
	}
	 
	// --- ZOOM MOTORU ---
	public void SetZoom(float zoomLevel)
	{
		_zoomFactor = Math.Max(0.5f, Math.Min(3.0f, zoomLevel));
		foreach (var kvp in _originalFonts)
			kvp.Key.Font = new Font(kvp.Key.Font.FontFamily, kvp.Value * _zoomFactor, kvp.Key.Font.Style);

		foreach (var kvp in _originalSizes)
		{
			kvp.Key.Width = (int)(kvp.Value.Width * _zoomFactor);
			if (!(kvp.Key is TextBox && !((TextBox)kvp.Key).Multiline))
				kvp.Key.Height = (int)(kvp.Value.Height * _zoomFactor);
		}
		RefreshLayout();
	}
	public void ZoomIn() => SetZoom(_zoomFactor + 0.1f);
	public void ZoomOut() => SetZoom(_zoomFactor - 0.1f);
	public void ResetZoom() => SetZoom(1.0f);


	// --- DİZİLİM YARDIMCILARI ---
	private void ApplyPaddingLogic(Control c)
	{
		var props = c.GetProps();
		if (props.CustomPadding.HasValue)
		{
			var p = props.CustomPadding.Value;
			c.Padding = new Padding(Scale(p.Left), Scale(p.Top), Scale(p.Right), Scale(p.Bottom));
		}
	}

	private Padding GetScaledMargin(Control c)
	{
		var props = c.GetProps();
		if (props.CustomMargin.HasValue)
		{
			var m = props.CustomMargin.Value;
			return new Padding(Scale(m.Left), Scale(m.Top), Scale(m.Right), Scale(m.Bottom));
		}
		return new Padding(0); // Zero-Base!
	}

	// --- İÇ İÇE (NESTED) DÜZENLEYİCİ ---
	public void Arrange(SmartGroup sg)
	{
		if (sg == null) return;
		ApplyPaddingLogic(sg);

		var sgProps = sg.GetProps();

		// 🌟 BANANA PARADOKS ÇÖZÜMÜ: 
		// Eğer grubun içinde bir Spring (yay) varsa, dairesel hesaplama döngüsünü 
		// engellemek için AutoSize özelliğini kesin olarak kapatıyoruz.
		bool hasSpring = sg.LayoutOrder.Any(child => child.GetProps().Spring);
		sg.AutoSize = !sgProps.GrowW && !hasSpring;

		int innerWidth = sg.Width - sg.Padding.Left - sg.Padding.Right;
		if (innerWidth <= 10) innerWidth = Scale(300);

		int itemSpacing = sgProps.ItemSpacing.HasValue ? Scale(sgProps.ItemSpacing.Value) : 0;

		foreach (var c in sg.LayoutOrder)
		{
			var p = c.GetProps();
			if (p.MatchWidthTarget != null) c.Width = p.MatchWidthTarget.Width;
		}

		int fixedW = 0, flexCount = 0;
		if (!sg.IsVertical)
		{
			foreach (var c in sg.LayoutOrder)
			{
				Padding m = GetScaledMargin(c);
				fixedW += m.Left + m.Right;
				var p = c.GetProps();
				if (p.GrowW || p.Spring) flexCount++;
				else fixedW += c.Width + itemSpacing;
			}
		}
		int flexW = flexCount > 0 ? Math.Max(0, (innerWidth - fixedW) / flexCount) : innerWidth;

		int currentX = sg.Padding.Left;
		int currentY = sg.Padding.Top;
		int maxWidth = 0, maxHeight = 0;

		for (int i = 0; i < sg.LayoutOrder.Count; i++)
		{
			var c = sg.LayoutOrder[i];
			ApplyPaddingLogic(c);
			Padding m = GetScaledMargin(c);
			var props = c.GetProps();

			int childTargetWidth = sg.IsVertical ? (innerWidth - m.Left - m.Right) : ((props.GrowW || props.Spring) ? flexW : c.Width);

			if (props.GrowW || props.Spring || sg.IsVertical)
			{
				c.Width = childTargetWidth;
				if (c is SmartGroup nested) { nested.AutoSize = false; nested.Width = childTargetWidth; }
			}

			if (c is Label lbl && props.WrapText)
			{
				lbl.AutoSize = false;
				lbl.MaximumSize = new Size(childTargetWidth, 0);
				lbl.AutoSize = true;
				Size preferred = lbl.GetPreferredSize(new Size(childTargetWidth, 0));
				lbl.Size = new Size(childTargetWidth, preferred.Height);
			}

			if (c is SmartGroup n) Arrange(n);

			c.Left = currentX + m.Left;
			c.Top = currentY + m.Top;

			if (props.AlignRightTarget != null)
			{
				c.Left = props.AlignRightTarget.Right - c.Width - m.Right;
			}

			int gap = (i == sg.LayoutOrder.Count - 1) ? 0 : itemSpacing;

			if (sg.IsVertical)
			{
				currentY += m.Top + c.Height + m.Bottom + gap;
				maxWidth = Math.Max(maxWidth, m.Left + c.Width + m.Right);
			}
			else
			{
				currentX += m.Left + c.Width + m.Right + gap;
				if (!props.Spring) maxHeight = Math.Max(maxHeight, m.Top + c.Height + m.Bottom);
			}
		}

		sg.Height = (sg.IsVertical ? currentY : maxHeight + sg.Padding.Top) + sg.Padding.Bottom;

		// 🌟 BANANA PARADOKS ÇÖZÜMÜ: 
		// İçerisinde yay olan grupların genişliğini otomatik büzüştürmüyoruz, bizim verdiğimiz sabit boyutta tutuyoruz.
		if (!sg.IsVertical && !sgProps.GrowW && !hasSpring)
		{
			sg.Width = currentX + sg.Padding.Right;
		}

		if (!sg.IsVertical)
		{
			foreach (var c in sg.LayoutOrder)
			{
				var p = c.GetProps();
				Padding m = GetScaledMargin(c);
				if (p.VAlign == 1) c.Top = sg.Padding.Top + (sg.Height - sg.Padding.Top - sg.Padding.Bottom - c.Height) / 2;
				else if (p.VAlign == 2) c.Top = sg.Height - sg.Padding.Bottom - c.Height - m.Bottom;
			}
		}
	}

	private void ArrangeSideContent(SmartSidePanel sp, bool vertical)
	{
		ApplyPaddingLogic(sp);
		int offset = vertical ? sp.Padding.Top : sp.Padding.Left;

		if (vertical)
		{
			// 🌟 ÖN HAZIRLIK GENİŞLİK SENKRONİZASYONU (BANANA FIX)
			// İçerideki SmartGroup'ların iç yerleşimini (Arrange) tetiklemeden önce, 
			// genişliklerini (Width) o anki sidebar genişliğine göre eşitliyoruz. 
			// Böylece Spring'ler ikonları görünmez kılacak şekilde dışarı fırlatmaz.
			foreach (var c in sp.Content)
			{
				if (c is SmartGroup sg)
				{
					if (sg.GetProps().GrowW)
					{
						Padding m = GetScaledMargin(sg);
						sg.Width = sp.Width - sp.Padding.Left - sp.Padding.Right - m.Left - m.Right;
					}
					Arrange(sg);
				}
			}

			int innerHeight = sp.Height - sp.Padding.Top - sp.Padding.Bottom;
			int fixedH = 0;
			int flexCount = 0;

			// Birinci geçiş: Sabit yükseklikleri hesapla
			foreach (var c in sp.Content)
			{
				Padding m = GetScaledMargin(c);
				var p = c.GetProps();
				if (p.GrowH || p.Spring)
				{
					flexCount++;
					fixedH += m.Top + m.Bottom;
				}
				else
				{
					fixedH += c.Height + m.Top + m.Bottom;
				}
			}

			// Kalan boşluğu esnek (Spring) elemanlara eşit olarak paylaştır
			int flexH = flexCount > 0 ? Math.Max(0, (innerHeight - fixedH) / flexCount) : 0;

			// İkinci geçiş: Elemanları yerleştir
			foreach (var c in sp.Content)
			{
				ApplyPaddingLogic(c);

				Padding m = GetScaledMargin(c);
				var p = c.GetProps();

				if (p.GrowH || p.Spring)
				{
					c.Height = flexH;
				}

				c.Location = new Point(sp.Padding.Left + m.Left, offset + m.Top);

				if (p.GrowW)
				{
					c.Width = sp.Width - sp.Padding.Left - sp.Padding.Right - m.Left - m.Right;
				}

				offset += c.Height + m.Top + m.Bottom;
			}
		}
		else
		{
			// Yatay yerleşim (Değişmedi)
			int availH = sp.Height - sp.Padding.Top - sp.Padding.Bottom;
			foreach (var c in sp.Content)
			{
				ApplyPaddingLogic(c);
				if (c is SmartGroup sg) Arrange(sg);

				Padding m = GetScaledMargin(c);
				c.Top = sp.Padding.Top + (availH - c.Height) / 2 + m.Top;
				c.Left = offset + m.Left;
				offset += c.Width + m.Left + m.Right;
			}
		}
	}

	// --- 🌟 NÜKLEER REFRESH LAYOUT MOTORU ---
	public void RefreshLayout()
	{
		// 1. GÜVENLİK KONTROLÜ
		if (_isPerformingLayout || _form == null || _form.IsDisposed || _form.WindowState == FormWindowState.Minimized)
			return;

		try
		{
			_isPerformingLayout = true;

			// 🌟 SESSİZ OPERASYON: Win32 ile ekranı dondur
			if (_form.IsHandleCreated) SendMessage(_form.Handle, WM_SETREDRAW, false, 0);

			_form.SuspendLayout();
			foreach (var row in _rows) if (row.Container != null) row.Container.SuspendLayout();
			foreach (var sp in _sidePanels) sp.SuspendLayout();

			RefreshLayoutCore();
		}
		finally
		{
			// 🌟 EKRANI SERBEST BIRAK VE ÇİZ
			foreach (var row in _rows) if (row.Container != null) row.Container.ResumeLayout(false);
			foreach (var sp in _sidePanels) sp.ResumeLayout(false);
			_form.ResumeLayout(false);

			if (_form.IsHandleCreated)
			{
				SendMessage(_form.Handle, WM_SETREDRAW, true, 0);
				_form.Refresh();
			}
			_isPerformingLayout = false;
		}
	}

	private void RefreshLayoutCore()
	{
		// --- AŞAMA 1: RESPONSIVE SIDEBAR & HAMBURGER ---
		bool isNarrow = _form.ClientSize.Width < _collapseThreshold;
		SmartSidePanel leftSidebar = _sidePanels.FirstOrDefault(s => s.Edge == Side.Left);

		if (_hamburgerBtn != null)
		{
			_hamburgerBtn.Visible = isNarrow;
			if (isNarrow)
			{
				_hamburgerBtn.Parent = (_isSidebarFlyoutOpen && leftSidebar != null) ? leftSidebar : _form;
				_hamburgerBtn.Location = new Point(Scale(10), Scale(10));
				_hamburgerBtn.BringToFront();
			}
		}

		Rectangle mainArea = new Rectangle(0, 0, _form.ClientSize.Width, _form.ClientSize.Height);

		foreach (var sp in _sidePanels)
		{
			if (sp.Edge == Side.Left)
			{
				sp.Visible = !isNarrow || _isSidebarFlyoutOpen;
				int sz = Scale(sp.BaseSize);
				if (sp.Visible)
				{
					sp.Bounds = new Rectangle(0, 0, sz, _form.ClientSize.Height);
					if (!isNarrow) { mainArea.X += sz; mainArea.Width -= sz; }
					else sp.BringToFront();
					ArrangeSideContent(sp, true);
				}
			}
			else if (sp.Edge == Side.Bottom)
			{
				if (!sp.Visible) continue;
				int sz = Scale(sp.BaseSize);
				sp.Bounds = new Rectangle(mainArea.X, _form.ClientSize.Height - sz, mainArea.Width, sz);
				mainArea.Height -= sz;
				ArrangeSideContent(sp, false);
			}
		}

		// --- AŞAMA 2: ÇİFT GEÇİŞLİ (DOUBLE-PASS) DİZİLİM ---
		for (int pass = 0; pass < 2; pass++)
		{
			int totalFixedHeight = 0;
			int growHCount = 0;

			// Dikey boşluk hesaplama (GrowH)
			foreach (var row in _rows)
			{
				if (!row.Container.Visible || row.IsGap)
				{
					if (row.IsGap) totalFixedHeight += Scale(row.GapSize);
					continue;
				}

				bool hasGrowH = row.ControlsInRow.Any(c => c.GetProps().GrowH);
				totalFixedHeight += Scale(row.RawTopMargin) + Scale(row.RawBottomMargin) + row.Container.Padding.Top + row.Container.Padding.Bottom;

				if (hasGrowH) growHCount++;
				else
				{
					int maxH = 0;
					foreach (var c in row.ControlsInRow) if (!c.GetProps().Spring) maxH = Math.Max(maxH, c.Height);
					totalFixedHeight += maxH;
				}
			}

			int heightPerGrow = growHCount > 0 ? Math.Max(10, (mainArea.Height - totalFixedHeight) / growHCount) : 0;
			int currentY = mainArea.Y;

			// --- AŞAMA 3: SATIRLARI DİZ (JİLET VERSİYON) ---
			foreach (var row in _rows)
			{
				if (row.IsGap) { currentY += Scale(row.GapSize); continue; }
				if (!row.Container.Visible) continue;

				// 🌟 Satırımız artık bir SmartGroup!
				SmartGroup sgRow = (SmartGroup)row.Container;

				// 1. Satırın dış kasasını (Genişlik ve Konum) belirle
				sgRow.Location = new Point(mainArea.X + Scale(row.RawLeftMargin), currentY + Scale(row.RawTopMargin));
				sgRow.Width = mainArea.Width - Scale(row.RawLeftMargin) - Scale(row.RawRightMargin);

				// 2. Satır içindeki GrowH elemanlarının yüksekliğini set et
				foreach (var c in row.ControlsInRow)
				{
					if (c.GetProps().GrowH) c.Height = heightPerGrow;
				}

				// 3. EFSANE HAMLE: Tüm esneklik, VAlign, Hizalama hesabını Arrange yapsın!
				Arrange(sgRow);

				// 4. Sonraki satıra geç (Yüksekliği Arrange metodu buldu)
				currentY += sgRow.Height + Scale(row.RawTopMargin) + Scale(row.RawBottomMargin);
			}
		}

		// Z-ORDER FİNAL
		if (_hamburgerBtn != null && _hamburgerBtn.Visible) _hamburgerBtn.BringToFront();
	}

	//
	public void FreezeRedraw()
	{
		if (_form != null && _form.IsHandleCreated)
			SendMessage(_form.Handle, WM_SETREDRAW, false, 0);
	}


	// --- 🌟 PÜRÜZSÜZ NATIVE SÜRÜKLEME MOTORU (AERO SNAP DESTEKLİ) ---
	[DllImport("user32.dll")]
	private static extern bool ReleaseCapture();
	[DllImport("user32.dll")]
	private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

	private const int WM_NCLBUTTONDOWN = 0xA1;
	private const int HT_CAPTION = 0x2;
	public void MakeDraggable(Control c)
	{
		c.MouseDown += (s, e) => {
			if (e.Button == MouseButtons.Left)
			{
				ReleaseCapture();
				SendMessage(_form.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0); // İşletim sistemine sürükleme emrini ilet
			}
		};
	}


}

// --- composite Kontrols ---
public partial class SmartUI
{
	/// <summary>
	/// task manager w11 deki
	/// </summary>
	public Control SidebarItem_v2(string iconCode, string text, bool isSelected = false,
	bool isExpanded = true, bool showIndicator = false, Color? selectedBgColor = null, bool growWidth = true)
	{
		// 1. Mavi gösterge çizgisi
		Panel indicator = null;
		if (showIndicator)
		{
			indicator = (Panel?)new Panel
			{
				Width = Scale(3),    // 🌟 High-DPI & Zoom Uyumu için Ölçeklendi
				Height = Scale(16),   // 🌟 High-DPI & Zoom Uyumu için Ölçeklendi
				BackColor = isSelected ? Color.FromArgb(0, 103, 192) : Color.Transparent
			}.Rounded(1);
		}

		// 2. İkon Tanımlaması
		Label ico = new Label
		{
			Text = iconCode,
			Font = new Font("Segoe Fluent Icons", 11),
			AutoSize = true,
			BackColor = Color.Transparent
		};

		if (ico.Font.Name != "Segoe Fluent Icons")
			ico.Font = new Font("Segoe MDL2 Assets", 11);

		// 3. Metin Tanımlaması
		Label lbl = new Label
		{
			Text = text,
			Font = new Font("Segoe UI Variable Display", 9.5f, isSelected ? FontStyle.Bold : FontStyle.Regular),
			AutoSize = true,
			BackColor = Color.Transparent,
			Visible = isExpanded
		};

		// 4. Grup Oluşturma (Esneklik Kontrolü)
		Control group;
		if (isExpanded)
		{
			// --- AÇIK SİDEBAR MODU ---
			if (showIndicator && indicator != null)
			{
				group = this.Group(
					indicator.VAlignMiddle(),
					this.Space(8),
					ico.VAlignMiddle(),
					this.Space(12),
					lbl.VAlignMiddle()
				);
			}
			else
			{
				// 🌟 BANANA HİZALAMA ÇÖZÜMÜ: 
				// Gösterge çizgisi yoksa bile, dikey hizalamanın milimetrik şaşmaması için
				// sol tarafa tam olarak aynı boşluğu koyuyoruz: Scale(3) + Scale(8) = Scale(11)
				group = this.Group(
					this.Space(11),
					ico.VAlignMiddle(),
					this.Space(12),
					lbl.VAlignMiddle()
				);
			}
		}
		else
		{
			// --- KAPALI SİDEBAR MODU (İkon Ortalanmış) ---
			if (showIndicator && indicator != null)
			{
				group = this.Group(
					indicator.VAlignMiddle(),
					this.Spring(),
					ico.VAlignMiddle(),
					this.Spring()
				);
			}
			else
			{
				group = this.Group(
					this.Spring(),
					ico.VAlignMiddle(),
					this.Spring()
				);
			}
		}

		// Akıllı Genişlik Sabitleme
		if (growWidth)
		{
			group.GrowW();
		}
		else
		{
			group.Width = Scale(40);
		}

		group.Padding(0, 8, 0, 8)
			 .Margin(4, 1, 4, 1)
			 .Rounded(4);

		if (isSelected)
		{
			Color selBg = selectedBgColor ?? Color.FromArgb(234, 234, 234);
			group.BackColor(selBg);
		}

		// Hover Olayları
		Action turnOnHover = () => {
			if (!isSelected) group.BackColor = Color.FromArgb(243, 243, 243);
		};

		Action turnOffHover = () => {
			if (!isSelected) group.BackColor = Color.Transparent;
		};

		group.MouseEnter += (s, e) => turnOnHover();
		group.MouseLeave += (s, e) => turnOffHover();

		foreach (Control child in group.Controls)
		{
			child.MouseEnter += (s, e) => turnOnHover();
			child.MouseLeave += (s, e) => turnOffHover();

			child.Click += (s, e) => {
				var onClickMethod = typeof(Control).GetMethod("OnClick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
				onClickMethod?.Invoke(group, new object[] { EventArgs.Empty });
			};
		}

		return group;
	}



	// Sidebar öğesi oluşturmak için yardımcı 
	public Control SidebarItem_v1(string iconCode, string text, bool isSelected = false)
	{
		Label ico = new Label
		{
			Text = iconCode,
			// Windows 11 ise "Segoe Fluent Icons", Windows 10 ise "Segoe MDL2 Assets"
			Font = new Font("Segoe Fluent Icons", 12),
			AutoSize = true,
			BackColor = Color.Transparent
		};

		// Eğer font hala yüklenmiyorsa Windows'un yedeğine (MD2) düşelim
		if (ico.Font.Name != "Segoe Fluent Icons")
			ico.Font = new Font("Segoe MDL2 Assets", 12);

		Label lbl = new Label
		{
			Text = text,
			Font = new Font("Segoe UI Variable Display", 10, isSelected ? FontStyle.Bold : FontStyle.Regular),
			AutoSize = true,
			BackColor = Color.Transparent
		};


		var group = this.Group(ico, lbl)
			 .GrowW()
			 .VAlignMiddle()
			 .Padding(10, 12, 10, 12)
			 .Margin(0);

		if (isSelected) group.BackColor(Color.White);


		// 🌟 HOVER MANTIĞINI BURADA TANIMLIYORUZ (Fonksiyon olarak)
		Action turnOnHover = () => {
			if (!isSelected) group.BackColor = Color.FromArgb(230, 230, 230);
		};

		Action turnOffHover = () => {
			if (!isSelected) group.BackColor = Color.Transparent;
		};

		// 1. Grubun kendi olayları
		group.MouseEnter += (s, e) => turnOnHover();
		group.MouseLeave += (s, e) => turnOffHover();

		// 2. TIKLAMA: Çocukların (İkon/Yazı) olaylarını Gruba yönlendir
		foreach (Control child in group.Controls)
		{
			child.MouseEnter += (s, e) => turnOnHover();
			child.MouseLeave += (s, e) => turnOffHover();

			// Opsiyonel: Çocuklara tıklandığında grubun Click eventini tetiklemek istersen:
			// child.Click += (s, e) => group_Click(group, e); 
		}

		return group;

	}

	//  --- Composite Controls . Reusable. i did this for Example .
	//   you can do it to .. share it here. if its general purpose.
	public RowResult CardView_v1(Label lbl_icon, Label lbl_title, Label lbl_desc, Control Control_atRightSide)
	{
		return
			this.Row
			(
				this.Group
				(
					lbl_icon.Padding(0, 0, 10, 0).VAlignMiddle().BackColor(Color.Transparent),
					this.Col(
						lbl_title
							/*.BackColor(Color.Orange)*/,
						lbl_desc.WrapText()
						//.BackColor(Color.Green)
						).GrowW().Padding(0)
				).VAlignMiddle().Padding(0).GrowW(),
				this.Space(12),
				Control_atRightSide.VAlignMiddle()
			)
			.BackColor(Color.White).Padding(18).Margin(30, 0, 30, 4)
			.Rounded(8, Color.FromArgb(229, 229, 229))
			 ;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="iconCode"> SegoeMDL2Icons. -  PUA icon codes</param>
	/// <param name="lbl_title"></param>
	/// <param name="lbl_desc"></param>
	/// <param name="Control_atRightSide"></param>
	/// <returns></returns>
	public RowResult CardView_v1(string iconCode, string title, string desc, Control Control_atRightSide)
	{
		// Fontlar
		Font mainFont = new Font("Segoe UI Variable Display", 10);
		Font boldFont = new Font("Segoe UI Variable Display", 10, FontStyle.Bold);
		Font iconFont = new Font("Segoe MDL2 Assets", 12);

		var lbl_icon = new Label { Text = iconCode, Font = iconFont, AutoSize = true };
		var lbl_title = new Label { Text = title, Font = boldFont, AutoSize = true };
		var lbl_desc = new Label { Text = desc, ForeColor = Color.Gray, AutoSize = true };


		return CardView_v1(lbl_icon, lbl_title, lbl_desc, Control_atRightSide);

		//return
		//	ui.Row
		//	(
		//		ui.Group
		//		(
		//			lbl_icon.Padding(0, 0, 10, 0).VAlignMiddle().BackColor(Color.Transparent),
		//			ui.Col(lbl_title, lbl_desc.WrapText()).GrowW().Padding(0)
		//		).VAlignMiddle().Padding(0).GrowW(),
		//		ui.Space(12),
		//		Control_atRightSide.VAlignMiddle()
		//	)
		//	.BackColor(Color.White).Padding(12).Margin(30, 0, 30, 4);
	}


	//---suggested by gemini

	/// <summary>
	/// Kart gruplarının üzerine konulan standart Windows 11 alt başlığı.
	/// </summary>
	public RowResult SectionHeader_v1(string title)
	{
		Label lblTitle = new Label
		{
			Text = title,
			Font = new Font("Segoe UI Variable Display", 10, FontStyle.Bold),
			AutoSize = true,
			ForeColor = Color.FromArgb(32, 32, 32)
		};

		return this.Row(lblTitle)
				   .Margin(30, 30, 30, 10) // Üstten biraz açık, karta yakın
										  //.Padding(0);
				   ;
	}

	/// <summary>
	/// İçinde ikon olan, renkli ve yuvarlak köşeli modern uyarı kutusu.
	/// </summary>
	public RowResult AlertBox_v1(string iconCode, string message, Color bgColor, Color textColor)
	{
		Label ico = new Label
		{
			Text = iconCode,
			Font = new Font("Segoe Fluent Icons", 14),
			AutoSize = true,
			ForeColor = textColor
		};

		Label msg = new Label
		{
			Text = message,
			Font = new Font("Segoe UI", 10),
			ForeColor = textColor
		};

		return this.Row(
			this.Group(
				ico.VAlignMiddle(),
				this.Space(8),
				msg.WrapText().VAlignMiddle().GrowW()
			).GrowW().Padding(0)
		)
		.BackColor(bgColor)
		.Padding(12, 12, 12, 12)
		.Margin(30, 10, 30, 10)
		.Rounded(6); // Jilet gibi yuvarlak köşeler
	}

	/// <summary>
	/// Araya ince, modern bir ayırıcı çizgi (Divider) çeker.
	/// </summary>
	public RowResult Divider_v1(int topMargin = 10, int bottomMargin = 10)
	{
		Panel line = new Panel { Height = 1, BackColor = Color.FromArgb(220, 220, 220) };

		return this.Row(line.GrowW())
				   .Margin(30, topMargin, 30, bottomMargin)
				   .Padding(0);
	}
}


// --- YARDIMCI SINIFLAR ---
public enum Side { Left, Right, Bottom }

public class SmartSidePanel : Panel
{
	public Side Edge { get; set; }
	public int BaseSize { get; set; }
	public List<Control> Content { get; set; }
	public SmartSidePanel() { 
		BackColor = Color.Transparent; 
		Margin = new Padding(0);
		this.DoubleBuffered = true; // 🌟 Pürüzsüz çizim için eklendi
	}

	// 🌟 CS1061 Hatasını çözen metot:
	public void PerformClick() => base.OnClick(EventArgs.Empty);
}

public class SmartGroup : Panel
{
	public List<Control> LayoutOrder { get; set; }
	public bool IsVertical { get; set; }
	public SmartGroup() { 
		BackColor = Color.Transparent;
		Margin = new Padding(0);
		this.DoubleBuffered = true; // 🌟 Pürüzsüz çizim için eklendi
	}

	// 🌟 CS1061 Hatasını çözen metot:
	public void PerformClick() => base.OnClick(EventArgs.Empty);
}

public class RowResult
{
	public Panel Container { get; set; }
	public List<Control> ControlsInRow { get; set; }
	public bool IsGap { get; set; }
	public int GapSize { get; set; }

	// Zero-Base (Sıfır Diktatörlük)
	public int RawTopMargin = 0;
	public int RawBottomMargin = 0;
	public int RawLeftMargin = 0;
	public int RawRightMargin = 0;
	public int ItemSpacing = 0;

	private SmartUI _parent;

	public RowResult(SmartUI parent) { _parent = parent; }
	public RowResult BackColor(Color c) { if (Container != null) Container.BackColor = c; return this; }
	public RowResult Padding(int all) { if (Container != null) Container.Padding = new Padding(_parent.Scale(all)); _parent.RefreshLayout(); return this; }
	public RowResult Padding(int l, int t, int r, int b) { if (Container != null) Container.Padding = new Padding(_parent.Scale(l), _parent.Scale(t), _parent.Scale(r), _parent.Scale(b)); _parent.RefreshLayout(); return this; }
	public RowResult Margin(int all) { RawLeftMargin = RawTopMargin = RawRightMargin = RawBottomMargin = all; _parent.RefreshLayout(); return this; }
	public RowResult Margin(int l, int t, int r, int b) { RawLeftMargin = l; RawTopMargin = t; RawRightMargin = r; RawBottomMargin = b; _parent.RefreshLayout(); return this; }
	public RowResult Spacing(int space) { ItemSpacing = space; _parent.RefreshLayout(); return this; }
	public RowResult Visible(bool v) { if (Container != null) Container.Visible = v; _parent.RefreshLayout(); return this; }

	public RowResult VAlignMiddle()
	{
		if (ControlsInRow != null) foreach (var c in ControlsInRow) c.GetProps().VAlign = 1;
		_parent.RefreshLayout();
		return this;
	}

	public RowResult Rounded(int radius, Color? borderColor = null, float thickness = 1.5f)
	{
		if (Container != null) Container.Rounded(radius, borderColor, thickness);
		return this;
	}
	 
}

// --- FLUENT API EXTENSIONLARI (String çöplüğü yok, RAM dostu) ---
public static class UIExtensions
{
	internal static ConditionalWeakTable<Control, LayoutProps> _rules = new ConditionalWeakTable<Control, LayoutProps>();
	internal static LayoutProps GetProps(this Control c) => _rules.GetOrCreateValue(c);


	//why its here in this class??
	public static SmartSidePanel AddContent(this SmartSidePanel sp, Control c)
	{
		if (sp.Content == null) sp.Content = new List<Control>();
		sp.Controls.Add(c);
		sp.Content.Add(c);
		return sp;
	}

	public static Control GrowW(this Control c) { c.GetProps().GrowW = true; return c; }
	public static Control GrowH(this Control c) { c.GetProps().GrowH = true; return c; }
	public static Control WrapText(this Control c) { c.GetProps().WrapText = true; return c; }
	public static Control Spring(this Control c) { c.GetProps().Spring = true; return c; }
	public static Control MatchWidth(this Control c, Control target) { c.GetProps().MatchWidthTarget = target; return c; }
	public static Control AlignRight(this Control c, Control target) { c.GetProps().AlignRightTarget = target; return c; }

	public static Control VAlignMiddle(this Control c)
	{
		c.GetProps().VAlign = 1;
		if (c is SmartGroup sg) foreach (var child in sg.LayoutOrder) child.GetProps().VAlign = 1;
		return c;
	}
	public static Control VAlignBottom(this Control c)
	{
		c.GetProps().VAlign = 2;
		if (c is SmartGroup sg) foreach (var child in sg.LayoutOrder) child.GetProps().VAlign = 2;
		return c;
	}

	public static Control Padding(this Control c, int all) { c.GetProps().CustomPadding = new Padding(all); return c; }
	public static Control Padding(this Control c, int l, int t, int r, int b) { c.GetProps().CustomPadding = new Padding(l, t, r, b); return c; }

	public static Control Margin(this Control c, int all) { c.GetProps().CustomMargin = new Padding(all); return c; }
	public static Control Margin(this Control c, int l, int t, int r, int b) { c.GetProps().CustomMargin = new Padding(l, t, r, b); return c; }
	public static Control MarginX(this Control c, int leftAndRight) => c.Margin(leftAndRight, c.Margin.Top, leftAndRight, c.Margin.Bottom);
	public static Control MarginY(this Control c, int topAndBottom) => c.Margin(c.Margin.Left, topAndBottom, c.Margin.Right, topAndBottom);

	public static Control Spacing(this Control c, int space) { c.GetProps().ItemSpacing = space; return c; }
	public static Control BackColor(this Control c, Color color) { c.BackColor = color; return c; }

	

	// Herhangi bir kontrolü yuvarlatır ve isteğe bağlı renkli çerçeve çizer
	public static Control Rounded(this Control c, int radius, Color? borderColor = null, float borderThickness = 1.5f)
	{
		var props = c.GetProps();
		props.CornerRadius = radius;
		props.BorderColor = borderColor;
		props.BorderThickness = borderThickness;

		// "Flat" (Düz) tasarım yapıyoruz ki WinForms'un o eski 3D sınırları gitsin
		if (c is Panel p) p.BorderStyle = BorderStyle.None;
		if (c is Button b) { b.FlatStyle = FlatStyle.Flat; b.FlatAppearance.BorderSize = 0; }

		// Eğer daha önce çizim olayına bağlanmadıysak bağlanalım
		if (!props.IsRoundedHooked)
		{
			props.IsRoundedHooked = true;

			// 1. KESME İŞLEMİ (Region): Kontrol boyut değiştirdikçe köşelerini kırp
			c.Resize += (s, e) => {
				Control ctrl = (Control)s;
				var p_props = ctrl.GetProps();
				if (p_props.CornerRadius.HasValue)
				{
					// Yüksek DPI ekranlarda köşe kıvrımı da düzgün görünsün diye Dpi ile çarpıyoruz
					int r = (int)(p_props.CornerRadius.Value * (ctrl.DeviceDpi / 96f));
					using (GraphicsPath path = GetRoundedPath(ctrl.ClientRectangle, r))
					{
						ctrl.Region = new Region(path); // Fazlalıkları makasla kes!
					}
				}
			};

			// 2. ÇERÇEVE ÇİZİMİ (Paint): Yuvarlatılmış bölgenin kenarına çizgi çek
			c.Paint += (s, e) => {
				Control ctrl = (Control)s;
				var p_props = ctrl.GetProps();

				if (p_props.CornerRadius.HasValue && p_props.BorderColor.HasValue)
				{
					e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; // Tırtıkları yok et (Jilet modu)

					int r = (int)(p_props.CornerRadius.Value * (ctrl.DeviceDpi / 96f));

					// Kenarlığı dışarı taşırmamak için alanı 1 piksel daraltıyoruz
					Rectangle rect = ctrl.ClientRectangle;
					rect.Width -= 1; rect.Height -= 1;

					using (GraphicsPath path = GetRoundedPath(rect, r))
					using (Pen pen = new Pen(p_props.BorderColor.Value, p_props.BorderThickness))
					{
						e.Graphics.DrawPath(pen, path);
					}
				}
			};
		}

		// İlk kesme ve çizim işlemini hemen tetikle
		c.Invalidate();
		return c;
	}
	private static GraphicsPath GetRoundedPath(Rectangle rect, int radius)
	{
		GraphicsPath path = new GraphicsPath();
		if (radius <= 0)
		{
			path.AddRectangle(rect);
			return path;
		}
		int d = radius * 2;
		// WinForms'un çizim sapmalarını (1px kayma) önlemek için -1 yapıyoruz
		Rectangle arc = new Rectangle(rect.X, rect.Y, d, d);
		path.AddArc(arc, 180, 90); // Sol Üst
		arc.X = rect.Right - d - 1;
		path.AddArc(arc, 270, 90); // Sağ Üst
		arc.Y = rect.Bottom - d - 1;
		path.AddArc(arc, 0, 90);   // Sağ Alt
		arc.X = rect.Left;
		path.AddArc(arc, 90, 90);  // Sol Alt
		path.CloseFigure();
		return path;
	}


	// textbox font textcolor(forecolor) extensions
	public static Control ForeColor(this Control c, Color color)
	{
		c.ForeColor = color;
		return c;
	}

	public static Control FontSize(this Control c, float size, FontStyle style = FontStyle.Regular)
	{
		c.Font = new Font(c.Font.FontFamily, size, style);
		return c;
	}
	public static Control Bold(this Control c)
	{
		c.Font = new Font(c.Font, c.Font.Style | FontStyle.Bold);
		return c;
	}

	public static Control Visible(this Control c,bool visible)
	{
		c.Visible = visible;
		return c;
	}

	public static Control HoverBackColor(this Control c, Color hoverColor)
	{
		Color originalColor = c.BackColor;

		Action enter = () => c.BackColor = hoverColor;
		Action leave = () => c.BackColor = originalColor;

		c.MouseEnter += (s, e) => enter();
		c.MouseLeave += (s, e) => leave();

		// İçerideki mevcut elemanlara bağla
		foreach (Control child in c.Controls)
		{
			child.MouseEnter += (s, e) => enter();
			child.MouseLeave += (s, e) => leave();
		}

		// Gelecekte eklenebilecek dinamik elemanlar için önlem
		c.ControlAdded += (s, e) => {
			e.Control.MouseEnter += (s2, e2) => enter();
			e.Control.MouseLeave += (s2, e2) => leave();
		};

		return c;
	}
	public static Control HoverTone(this Control c, float amount = 0.1f)
	{
		Color originalColor = c.BackColor;

		// YIQ Parlaklık Formülü ile arka planın açık mı koyu mu olduğunu tespit ediyoruz
		float brightness = (originalColor.R * 0.299f + originalColor.G * 0.587f + originalColor.B * 0.114f) / 255f;

		Color hoverColor;
		if (brightness > 0.5f)
		{
			// Arka plan açıksa rengi hafifçe KOYULAŞTIR
			int r = Math.Max(0, (int)(originalColor.R - (originalColor.R * amount)));
			int g = Math.Max(0, (int)(originalColor.G - (originalColor.G * amount)));
			int b = Math.Max(0, (int)(originalColor.B - (originalColor.B * amount)));
			hoverColor = Color.FromArgb(originalColor.A, r, g, b);
		}
		else
		{
			// Arka plan koyuysa rengi hafifçe AÇIKLAŞTIR
			int r = Math.Min(255, (int)(originalColor.R + (255 - originalColor.R) * amount));
			int g = Math.Min(255, (int)(originalColor.G + (255 - originalColor.G) * amount));
			int b = Math.Min(255, (int)(originalColor.B + (255 - originalColor.B) * amount));
			hoverColor = Color.FromArgb(originalColor.A, r, g, b);
		}

		// Sizin rename ettiğiniz HoverBackColor metodunu doğrudan tetikliyoruz
		return c.HoverBackColor(hoverColor);
	}

	// --- tooltip
	private static readonly ToolTip _sharedToolTip = new (){
		AutomaticDelay = 500, AutoPopDelay = 5000, InitialDelay = 500, ReshowDelay = 100 
	};
	public static Control ToolTip(this Control c, string text)
	{
		_sharedToolTip.SetToolTip(c, text);
		return c;
	}


	public static Control OnClick(this Control c, Action action)
	{
		if (action != null)
		{
			c.Click += (s, e) => action();
		}
		return c;
	}
	public static Control OnClick(this Control c, Action<object, EventArgs> action)
	{
		if (action != null)
		{
			c.Click += (s, e) => action(s, e);
		}
		return c;
	}

}


// --- 🗺️ %100 NATIVE BORDERLESS RESIZABLE VE GEÇİRGENLİK YAPISI (WM_NCCALCSIZE + HTTRANSPARENT) ---
public class SmartFormResizable : NativeWindow
{
	private Form _form;
	private int _borderSize;
	private bool _smallCorners;

	[DllImport("dwmapi.dll")]
	private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

	private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
	private const int DWMWCP_ROUND = 2;
	private const int DWMWCP_ROUNDSMALL = 3;

	public SmartFormResizable(Form form, int borderSize = 8, bool smallCorners = false)
	{
		_form = form;
		_borderSize = borderSize;
		_smallCorners = smallCorners;

		form.FormBorderStyle = FormBorderStyle.Sizable;

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

		// 🌟 AKILLI REKÜRSİF FILTRE: Formun içine eleman eklendikçe otomatik olarak geçirgenlik filtresi bağlar
		form.ControlAdded += (s, e) => RegisterChildRecursive(e.Control);

		// Mevcut tüm çocuk hiyerarşisini tara ve geçirgen kıl
		foreach (Control child in form.Controls)
		{
			RegisterChildRecursive(child);
		}

		form.FormClosed += (s, e) => this.ReleaseHandle();
	}

	private void RegisterChildRecursive(Control child)
	{
		// Çocuk kontrolü Windows mesaj geçirgenlik filtresine bağla
		new SmartChildResizeFilter(_form, child, _borderSize);

		// Eğer bu çocuğun içine de başka bir çocuk eklenirse (nested), onu da yakala
		child.ControlAdded += (s, e) => RegisterChildRecursive(e.Control);
		foreach (Control subChild in child.Controls)
		{
			RegisterChildRecursive(subChild);
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

		const int HTLEFT = 10;
		const int HTRIGHT = 11;
		const int HTTOP = 12;
		const int HTTOPLEFT = 13;
		const int HTTOPRIGHT = 14;
		const int HTBOTTOM = 15;
		const int HTBOTTOMLEFT = 16;
		const int HTBOTTOMRIGHT = 17;

		if (m.Msg == WM_NCCALCSIZE)
		{
			m.Result = IntPtr.Zero;
			return;
		}

		if (m.Msg == WM_NCHITTEST)
		{
			Point screenPoint = new Point(m.LParam.ToInt32());
			Point clientPoint = _form.PointToClient(screenPoint);

			if (clientPoint.Y < _borderSize)
			{
				if (clientPoint.X < _borderSize) { m.Result = (IntPtr)HTTOPLEFT; return; }
				if (clientPoint.X > _form.Width - _borderSize) { m.Result = (IntPtr)HTTOPRIGHT; return; }
				m.Result = (IntPtr)HTTOP;
				return;
			}
			if (clientPoint.Y > _form.Height - _borderSize)
			{
				if (clientPoint.X < _borderSize) { m.Result = (IntPtr)HTBOTTOMLEFT; return; }
				if (clientPoint.X > _form.Width - _borderSize) { m.Result = (IntPtr)HTBOTTOMRIGHT; return; }
				m.Result = (IntPtr)HTBOTTOM;
				return;
			}
			if (clientPoint.X < _borderSize)
			{
				m.Result = (IntPtr)HTLEFT;
				return;
			}
			if (clientPoint.X > _form.Width - _borderSize)
			{
				m.Result = (IntPtr)HTRIGHT;
				return;
			}
		}

		base.WndProc(ref m);
	}
}

// --- 🌟 ÇOCUK KONTROLLER İÇİN MOUSE GEÇİRGENLİK FİLTRESİ ---
public class SmartChildResizeFilter : NativeWindow
{
	private Form _form;
	private int _borderSize;

	public SmartChildResizeFilter(Form form, Control child, int borderSize)
	{
		_form = form;
		_borderSize = borderSize;

		if (child.IsHandleCreated) this.AssignHandle(child.Handle);
		else child.HandleCreated += (s, e) => this.AssignHandle(child.Handle);

		child.HandleDestroyed += (s, e) => this.ReleaseHandle();
	}

	protected override void WndProc(ref Message m)
	{
		const int WM_NCHITTEST = 0x0084;
		const int HTTRANSPARENT = -1;

		if (m.Msg == WM_NCHITTEST)
		{
			Point screenPoint = new Point(m.LParam.ToInt32());
			Point clientPoint = _form.PointToClient(screenPoint);

			// Eğer mouse formun sınırları içindeki esnetme alanındaysa, 
			// çocuk kontrolün mouse mesajını çalmasını önle, "şeffafım" de ve Form'a yönlendir!
			if (clientPoint.X < _borderSize ||
				clientPoint.X > _form.Width - _borderSize ||
				clientPoint.Y < _borderSize ||
				clientPoint.Y > _form.Height - _borderSize)
			{
				m.Result = (IntPtr)HTTRANSPARENT; // Windows'a mesajı parent'a iletmesini söyle
				return;
			}
		}
		base.WndProc(ref m);
	}
}