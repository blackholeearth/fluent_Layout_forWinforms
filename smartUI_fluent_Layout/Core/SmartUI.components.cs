using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections.Generic;

namespace SmartLayoutEngine
{
	public partial class SmartUI
	{
		// --- 11. WINDOWS 11 MODERN BUTON (FluentButton_v1) ---
		// Normal ve Accent (Belirgin Koyu Mavi) modlu, alt kenar derinlik gölgeli modern buton
		public Button FluentButton_v1(string text, bool isAccent = false, Action onClick = null)
		{
			Button btn = new Button
			{
				Text = text,
				Font = new Font("Segoe UI Semibold", 9f),
				Height = Scale(32),
				FlatStyle = FlatStyle.Flat,
				BackColor = isAccent ? Color.FromArgb(0, 103, 192) : Color.White,
				ForeColor = isAccent ? Color.White : Color.FromArgb(32, 32, 32),
				AutoSize = true
			};
			btn.FlatAppearance.BorderSize = 0;

			// Sınır Çizgisi Rengi (Accent ise koyu mavi, normal ise gri)
			Color borderColor = isAccent ? Color.FromArgb(0, 90, 170) : Color.FromArgb(218, 218, 218);
			btn.Rounded(6, borderColor, 1f);

			// Hover Durum Renklendirmesi
			if (isAccent)
			{
				btn.HoverBackColor(Color.FromArgb(0, 93, 172));
			}
			else
			{
				btn.HoverBackColor(Color.FromArgb(249, 249, 249));
			}

			if (onClick != null)
			{
				btn.OnClick(onClick);
			}

			return btn;
		}


		// --- 🌟 YARDIMCI METOT: PAYLAŞILAN YUVARLATILMIŞ GEOMETRİ ---
		private static GraphicsPath GetSharedRoundedPath(Rectangle rect, int radius)
		{
			GraphicsPath path = new GraphicsPath();
			if (radius <= 0) { path.AddRectangle(rect); return path; }
			int d = radius * 2;
			Rectangle arc = new Rectangle(rect.X, rect.Y, d, d);
			path.AddArc(arc, 180, 90);
			arc.X = rect.Right - d - 1;
			path.AddArc(arc, 270, 90);
			arc.Y = rect.Bottom - d - 1;
			path.AddArc(arc, 0, 90);
			arc.X = rect.Left;
			path.AddArc(arc, 90, 90);
			path.CloseFigure();
			return path;
		}

		// --- 1. WINDOWS 11 MODERN AÇILIR LİSTE (FluentComboBox_v1) ---
		public Control FluentComboBox_v1(string[] items, Action<int> onItemSelect)
		{
			int selectedIndex = 0;

			Label lblSelected = new Label
			{
				Text = items.Length > 0 ? items[0] : "",
				Font = new Font("Segoe UI", 9.5f),
				ForeColor = Color.FromArgb(32, 32, 32),
				AutoSize = true,
				BackColor = Color.Transparent
			};

			Label chevron = new Label
			{
				Text = "\uE70D",
				Font = new Font("Segoe Fluent Icons", 8f),
				ForeColor = Color.FromArgb(120, 120, 120),
				AutoSize = true,
				BackColor = Color.Transparent
			};
			if (chevron.Font.Name != "Segoe Fluent Icons") chevron.Font = new Font("Segoe MDL2 Assets", 8f);

			SmartGroup wrapper = (SmartGroup)this.Group(
				lblSelected.VAlignMiddle(),
				this.Spring(),
				chevron.VAlignMiddle()
			)
			.Padding(12, 6, 12, 6)
			.BackColor(Color.White)
			.Rounded(6, Color.FromArgb(218, 218, 218));

			wrapper.Cursor = Cursors.Hand;
			wrapper.Width = Scale(180);
			wrapper.HoverBackColor(Color.FromArgb(249, 249, 249));

			Action openDropdown = () => {
				Form popup = new Form
				{
					FormBorderStyle = FormBorderStyle.None,
					StartPosition = FormStartPosition.Manual,
					ShowInTaskbar = false,
					BackColor = Color.White,
					Size = new Size(wrapper.Width, Scale(items.Length * 32 + 8)),
					//DoubleBuffered = true
				};
				popup.DoubleBuffered();

				popup.Load += (s, e) => {
					using (GraphicsPath path = GetSharedRoundedPath(popup.ClientRectangle, Scale(8)))
						popup.Region = new Region(path);
				};

				popup.Paint += (s, e) => {
					e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
					using (Pen borderPen = new Pen(Color.FromArgb(218, 218, 218), 1))
					{
						e.Graphics.DrawPath(borderPen, GetSharedRoundedPath(new Rectangle(0, 0, popup.Width - 1, popup.Height - 1), Scale(8)));
					}
				};

				SmartUI popupUI = new SmartUI(popup);
				List<Control> optionButtons = new List<Control>();

				for (int i = 0; i < items.Length; i++)
				{
					int index = i;
					Label lblOpt = new Label { Text = items[i], Font = new Font("Segoe UI", 9.5f), AutoSize = true, BackColor = Color.Transparent };

					Control opt = popupUI.Group(lblOpt.VAlignMiddle())
						.Padding(12, 6, 12, 6)
						.GrowW()
						.Rounded(4);

					opt.Cursor = Cursors.Hand;
					opt.HoverBackColor(Color.FromArgb(243, 243, 243));

					Action selectItem = () => {
						selectedIndex = index;
						lblSelected.Text = items[index];
						onItemSelect?.Invoke(index);
						popup.Close();
					};

					opt.Click += (s, e) => selectItem();
					lblOpt.Click += (s, e) => selectItem();

					optionButtons.Add(opt);
				}

				popupUI.Col(optionButtons.ToArray()).Padding(4).GrowW().GrowH();

				Point screenPos = wrapper.PointToScreen(Point.Empty);
				popup.Location = new Point(screenPos.X, screenPos.Y + wrapper.Height + 2);

				popup.Deactivate += (s, e) => popup.Close();

				popup.Show();
				popup.Focus();
			};

			wrapper.Click += (s, e) => openDropdown();
			lblSelected.Click += (s, e) => openDropdown();
			chevron.Click += (s, e) => openDropdown();

			return wrapper;
		}

		// --- 2. WINDOWS 11 SEÇİM KUTUSU (FluentCheckBox_v1) ---
 
		public FluentCheckBox FluentCheckBox_v1(string text, bool isChecked = false, Action<bool> onCheckedChanged = null)
		{
			FluentCheckBox cb = new FluentCheckBox
			{
				Text = text,
				Checked = isChecked,
				Font = new Font("Segoe UI", 9.5f),
				ForeColor = Color.FromArgb(32, 32, 32),
				AutoSize = true,
				BackColor = Color.Transparent
			};

			//// 🌟 DEBUG: Boyutları kontrol et
			//Console.WriteLine($"FluentCheckBox Created: Text='{text}', PreferredSize={cb.GetPreferredSize(Size.Empty)}");

			if (onCheckedChanged != null)
			{
				cb.CheckedChanged += (s, e) => onCheckedChanged(cb.Checked);
			}

			return cb;
		}


		// --- 3. WINDOWS 11 SEKMELİ GEÇİŞ (FluentTabControl_v1) ---
		public Control FluentTabControl_v1(string[] tabNames, Action<int> onTabSelected)
		{
			List<Control> tabButtons = new List<Control>();
			int selectedIndex = 0;
			SmartGroup tabRow = null;

			Action<int> selectTab = (idx) => {
				selectedIndex = idx;
				for (int i = 0; i < tabButtons.Count; i++)
				{
					var btn = tabButtons[i];
					btn.ForeColor = (i == idx) ? Color.FromArgb(0, 103, 192) : Color.FromArgb(120, 120, 120);
					btn.Font = new Font("Segoe UI Semibold", 9.5f, (i == idx) ? FontStyle.Bold : FontStyle.Regular);
				}
				if (tabRow != null) tabRow.Invalidate();
				onTabSelected?.Invoke(idx);
			};

			for (int i = 0; i < tabNames.Length; i++)
			{
				int index = i;
				Label lbl = new Label
				{
					Text = tabNames[i],
					Font = new Font("Segoe UI Semibold", 9.5f),
					ForeColor = Color.FromArgb(120, 120, 120),
					AutoSize = true,
					BackColor = Color.Transparent,
					Cursor = Cursors.Hand
				};
				lbl.Click += (s, e) => selectTab(index);

				Control tabOpt = this.Group(lbl.VAlignMiddle()).Padding(12, 8, 12, 8);
				tabOpt.Click += (s, e) => selectTab(index);

				tabButtons.Add(tabOpt);
			}

			tabRow = (SmartGroup)this.Group(tabButtons.ToArray())
				.Padding(0)
				.Margin(16, 4, 16, 4);

			tabRow.Paint += (s, e) => {
				e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

				using (Pen linePen = new Pen(Color.FromArgb(225, 225, 225), 1))
				{
					e.Graphics.DrawLine(linePen, 0, tabRow.ClientRectangle.Bottom - 1, tabRow.ClientRectangle.Width, tabRow.ClientRectangle.Bottom - 1);
				}

				if (selectedIndex >= 0 && selectedIndex < tabButtons.Count)
				{
					Control selectedTab = tabButtons[selectedIndex];
					using (Pen activePen = new Pen(Color.FromArgb(0, 103, 192), Scale(2)))
					{
						e.Graphics.DrawLine(activePen, selectedTab.Left + Scale(8), tabRow.ClientRectangle.Bottom - 1, selectedTab.Right - Scale(8), tabRow.ClientRectangle.Bottom - 1);
					}
				}
			};

			selectTab(0);
			return tabRow;
		}

		// --- 4. WINDOWS 11 BİLGİ ROZETİ (FluentInfoBadge_v1) ---
		public Control FluentInfoBadge_v1(string text, Color bgColor, Color textColor)
		{
			Label lbl = new Label
			{
				Text = text,
				Font = new Font("Segoe UI Semibold", 8f),
				ForeColor = textColor,
				AutoSize = true,
				BackColor = Color.Transparent
			};

			Control badge = this.Group(lbl.VAlignMiddle())
				.Padding(8, 2, 8, 2)
				.BackColor(bgColor)
				.Rounded(10);

			return badge;
		}

		// --- 5. WINDOWS 11 AKILLI ARAMA KUTUSU (FluentSearchBox_v1) ---
		public Control FluentSearchBox_v1(string placeholder = "Ara...", int width = 240)
		{
			Label ico = new Label
			{
				Text = "\uE721",
				Font = new Font("Segoe Fluent Icons", 9.5f),
				ForeColor = Color.FromArgb(120, 120, 120),
				AutoSize = true,
				BackColor = Color.Transparent
			};
			if (ico.Font.Name != "Segoe Fluent Icons") ico.Font = new Font("Segoe MDL2 Assets", 9.5f);

			TextBox tb = new TextBox
			{
				BorderStyle = BorderStyle.None,
				Font = new Font("Segoe UI", 9.5f),
				BackColor = Color.White,
				Width = Scale(width) - Scale(38),
				Height = Scale(20)
			};

			tb.Placeholder(placeholder);

			SmartGroup wrapper = (SmartGroup)this.Group(
				ico.VAlignMiddle(),
				this.Space(6),
				tb.VAlignMiddle()
			)
			.Padding(8, 6, 8, 6)
			.BackColor(Color.White)
			.Rounded(6, Color.FromArgb(218, 218, 218));

			wrapper.Width = Scale(width);

			bool isFocused = false;
			tb.Enter += (s, e) => { isFocused = true; wrapper.Invalidate(); };
			tb.Leave += (s, e) => { isFocused = false; wrapper.Invalidate(); };

			wrapper.Paint += (s, e) =>
			{
				if (isFocused)
				{
					e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
					using (Pen activePen = new Pen(Color.FromArgb(0, 103, 192), Scale(2)))
					{
						e.Graphics.DrawLine(activePen, Scale(6), wrapper.ClientRectangle.Bottom - 1, wrapper.ClientRectangle.Width - Scale(6), wrapper.ClientRectangle.Bottom - 1);
					}
				}
			};

			wrapper.Click += (s, e) => tb.Focus();
			return wrapper;
		}

		// --- 6. WINDOWS 11 DÖNEN YÜKLEME HALKASI (FluentLoadingRing_v1) ---
		public Control FluentLoadingRing_v1(int size = 24)
		{
			return new FluentLoadingRing { Size = new Size(Scale(size), Scale(size)) };
		}

		// --- 7. WINDOWS 11 MODERN TEXTBOX (FluentTextBox_v1) ---
		public Control FluentTextBox_v1(string placeholder = "", int width = 200)
		{
			TextBox tb = new TextBox
			{
				BorderStyle = BorderStyle.None,
				Font = new Font("Segoe UI", 9.5f),
				BackColor = Color.White,
				Width = Scale(width) - Scale(16),
				Height = Scale(20)
			};

			if (!string.IsNullOrEmpty(placeholder))
			{
				tb.Placeholder(placeholder);
			}

			SmartGroup wrapper = (SmartGroup)this.Group(tb.VAlignMiddle())
				.Padding(8, 6, 8, 6)
				.BackColor(Color.White)
				.Rounded(6, Color.FromArgb(218, 218, 218));

			wrapper.Width = Scale(width);

			bool isFocused = false;
			tb.Enter += (s, e) => { isFocused = true; wrapper.Invalidate(); };
			tb.Leave += (s, e) => { isFocused = false; wrapper.Invalidate(); };

			wrapper.Paint += (s, e) =>
			{
				if (isFocused)
				{
					e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
					using (Pen activePen = new Pen(Color.FromArgb(0, 103, 192), Scale(2)))
					{
						e.Graphics.DrawLine(activePen,
							Scale(6),
							wrapper.ClientRectangle.Bottom - 1,
							wrapper.ClientRectangle.Width - Scale(6),
							wrapper.ClientRectangle.Bottom - 1);
					}
				}
			};

			wrapper.Click += (s, e) => tb.Focus();
			return wrapper;
		}

		// --- 8. WINDOWS 11 MODERN GRUP İÇİ KART (FluentCard_v1) ---
		public Control FluentCard_v1(string iconCode, string title, string desc, Control controlAtRight)
		{
			Font boldFont = new Font("Segoe UI Variable Display", 10, FontStyle.Bold);
			Font iconFont = new Font("Segoe Fluent Icons", 12);
			if (iconFont.Name != "Segoe Fluent Icons") iconFont = new Font("Segoe MDL2 Assets", 12);

			var lbl_icon = new Label { Text = iconCode, Font = iconFont, AutoSize = true, BackColor = Color.Transparent };
			var lbl_title = new Label { Text = title, Font = boldFont, AutoSize = true, BackColor = Color.Transparent };
			var lbl_desc = new Label { Text = desc, ForeColor = Color.Gray, AutoSize = true, BackColor = Color.Transparent };

			Control card = this.Group(
				this.Group(
					lbl_icon.Padding(0, 0, 10, 0).VAlignMiddle(),
					this.Col(lbl_title, lbl_desc.WrapText()).GrowW().Padding(0)
				).VAlignMiddle().Padding(0).GrowW(),
				this.Space(12),
				controlAtRight.VAlignMiddle()
			)
			.Padding(18)
			.GrowW();

			return card;
		}

		// --- 9. AYAR GRUBU KAPSAYICISI (FluentCardGroup_v1) ---
		public Control FluentCardGroup_v1(params Control[] cards)
		{
			List<Control> items = new List<Control>();

			for (int i = 0; i < cards.Length; i++)
			{
				items.Add(cards[i]);

				if (i < cards.Length - 1)
				{
					Control divider = new Panel
					{
						Height = 1,
						BackColor = Color.FromArgb(240, 240, 240),
						Margin = new Padding(Scale(16), 0, Scale(16), 0)
					}.GrowW();
					items.Add(divider);
				}
			}

			Control cardGroup = this.Col(items.ToArray())
				.BackColor(Color.White)
				.Padding(0)
				.Margin(16, 4, 16, 4)
				.Rounded(8, Color.FromArgb(229, 229, 229));

			return cardGroup;
		}

		// --- 10. MODERN PROGRESS BAR (FluentProgressBar_v1) ---
		public Control FluentProgressBar_v1(float initialValue = 0f, Color? progressColor = null)
		{
			FluentProgressBar bar = new FluentProgressBar
			{
				Value = initialValue,
				ProgressColor = progressColor ?? Color.FromArgb(0, 103, 192),
				Height = Scale(4)
			};

			return bar;
		}




		// --- 12. WINDOWS 11 MODERN BAŞLIK ÇUBUĞU (SetupCustomTitleBar_v1) ---
		// Yüksekliği ayarlanabilen, içine istenen kontrol yerleştirilebilen, pürüzsüz sürükleme destekli modern başlık
		public Control SetupCustomTitleBar_v1(string windowTitle, Control centerControl = null, int height = 48)
		{
			// A. Uygulama Logosu ve Başlık (Sol taraf)
			Label lblIcon = new Label
			{
				Text = "\uE9D2",// "\uE809", // Görev yöneticisi standart simgesi
				Font = new Font("Segoe Fluent Icons", 10),
				AutoSize = true,
				BackColor = Color.Transparent,
				ForeColor = Color.FromArgb(32, 32, 32)
			};
			if (lblIcon.Font.Name != "Segoe Fluent Icons") lblIcon.Font = new Font("Segoe MDL2 Assets", 10);

			Label lblTitle = new Label
			{
				Text = windowTitle,
				Font = new Font("Segoe UI Semibold", 9f),
				AutoSize = true,
				BackColor = Color.Transparent,
				ForeColor = Color.FromArgb(32, 32, 32)
			};

			// B. Windows 11 Kapatma, Küçültme, Tam Ekran Butonları (Sağ taraf)
			Button btnMin = CreateCaptionButton("\uE921", () => _form.WindowState = FormWindowState.Minimized);
			Button btnMax = CreateCaptionButton("\uE922", () => {
				_form.WindowState = _form.WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
			});
			Button btnClose = CreateCaptionButton("\uE8BB", () => _form.Close(), isClose: true);

			// C. Deklaratif Yerleşim (Tüm elemanlar esnek yaylarla kusursuz hizalanır)
			Control titleBarGroup = this.Group(
				this.Group(lblIcon.VAlignMiddle(), this.Space(8), lblTitle.VAlignMiddle()).VAlignMiddle(),
				this.Spring(), // Sol boşluğu doldurur, merkez kontrolü ortalar
				centerControl != null ? centerControl.VAlignMiddle() : this.Space(0),
				this.Spring(), // Sağ boşluğu doldurur
				this.Group(btnMin.VAlignMiddle(), btnMax.VAlignMiddle(), btnClose.VAlignMiddle()).VAlignMiddle()
			)
			.Padding(12, 7, 0,7) // Butonların üst/alta sıfır oturması için dikey padding verilmez
			.BackColor(Color.FromArgb(243, 243, 243)) // Başlık çubuğu rengi
			.GrowW();

			titleBarGroup.Height = Scale(height);

			// Sürükleme olaylarını başlık çubuğuna ve içindeki yazılara bağla (Butonlar hariç)
			MakeDraggable(titleBarGroup);
			//lblIcon.MouseDown += (s, e) => titleBarGroup.PerformClick(); // Çizim kararlılığı için tık yönlendirici
			//lblTitle.MouseDown += (s, e) => titleBarGroup.PerformClick();
			MakeDraggable(lblIcon);
			MakeDraggable(lblTitle);

			MakeFormResizable_v1();


			return titleBarGroup;
		}


		/// <summary>
		/// native burron  maximize show snap to tiles
		/// </summary>
		private SmartFormResizable _resizableHelper; // 🌟 Sistem referansını hafızada tutuyoruz
													 // --- 12. WINDOWS 11 MODERN BAŞLIK ÇUBUĞU (SetupCustomTitleBar_v1) ---
		public Control SetupCustomTitleBar_v2(string windowTitle, Control centerControl = null, int height = 48)
		{
			Label lblIcon = new Label
			{
				Text = "\uE809",
				Font = new Font("Segoe Fluent Icons", 10),
				AutoSize = true,
				BackColor = Color.Transparent,
				ForeColor = Color.FromArgb(32, 32, 32)
			};
			if (lblIcon.Font.Name != "Segoe Fluent Icons")
				lblIcon.Font = new Font("Segoe MDL2 Assets", 10);

			Label lblTitle = new Label
			{
				Text = windowTitle,
				Font = new Font("Segoe UI Semibold", 9f),
				AutoSize = true,
				BackColor = Color.Transparent,
				ForeColor = Color.FromArgb(32, 32, 32)
			};

			Button btnMin = CreateCaptionButton("\uE921", null);
			Button btnMax = CreateCaptionButton("\uE922", null);
			Button btnClose = CreateCaptionButton("\uE8BB", null, isClose: true);

			Control leftGroup = this.Group(lblIcon.VAlignMiddle(), this.Space(8), lblTitle.VAlignMiddle()).VAlignMiddle();
			Control rightGroup = this.Group(btnMin.VAlignMiddle(), btnMax.VAlignMiddle(), btnClose.VAlignMiddle());

			Control titleBarGroup = this.Group(
				leftGroup,
				this.Spring(),
				centerControl != null ? centerControl.MarginY(7) : this.Space(0),
				this.Spring(),
				rightGroup
			)
			.Padding(12, 0, 0, 0)
			.BackColor(Color.FromArgb(243, 243, 243))
			.GrowW();

			titleBarGroup.Height = Scale(height);

			// ============================================================
			// HIT-TEST TRANSPARENCY — RECURSIVE APPROACH
			// ============================================================

			// 1. Ana title bar'ı şeffaf yapıyoruz
			new SmartTransparentControlFilter(titleBarGroup);

			// 2. centerControl (Arama kutusu) HARİÇ tüm çocukları şeffaf yapıyoruz.
			MakeTitleBarChildrenTransparent(titleBarGroup, centerControl);

			// 3. Eğer başlık çubuğunda bir "hamburger menü" varsa, onu da interactive olarak ekleyin!
			// Örnek: _resizableHelper?.AddInteractiveControl(hamburgerBtn);
			if (_resizableHelper != null)
			{
				_resizableHelper.BindCaptionControls(titleBarGroup, btnMin, btnMax, btnClose);

				if (centerControl != null)
					_resizableHelper.AddInteractiveControl(centerControl);
			}

			return titleBarGroup;
		}
		private static void MakeTitleBarChildrenTransparent(Control parent, Control exceptionSubtree)
		{
			foreach (Control child in parent.Controls)
			{
				if (child == exceptionSubtree) continue;

				// Çocuk kontrolü şeffaf yap (Sürükleme olaylarının Form'a geçmesi için)
				new SmartTransparentControlFilter(child);

				// İç içe olan çocukları da recursive olarak şeffaf yap
				MakeTitleBarChildrenTransparent(child, exceptionSubtree);
			}
		}

		public Control SetupCustomTitleBar_v2_old(string windowTitle, Control centerControl = null, int height = 48)
		{
			Label lblIcon = new Label { Text = "\uE809", Font = new Font("Segoe Fluent Icons", 10), AutoSize = true, BackColor = Color.Transparent, ForeColor = Color.FromArgb(32, 32, 32) };
			if (lblIcon.Font.Name != "Segoe Fluent Icons") lblIcon.Font = new Font("Segoe MDL2 Assets", 10);
			Label lblTitle = new Label { Text = windowTitle, Font = new Font("Segoe UI Semibold", 9f), AutoSize = true, BackColor = Color.Transparent, ForeColor = Color.FromArgb(32, 32, 32) };

			Button btnMin = CreateCaptionButton("\uE921", null);
			Button btnMax = CreateCaptionButton("\uE922", null);
			Button btnClose = CreateCaptionButton("\uE8BB", null, isClose: true);

			Control titleBarGroup = this.Group(
				this.Group(lblIcon.VAlignMiddle(), this.Space(8), lblTitle.VAlignMiddle()).VAlignMiddle(),
				this.Spring(),
				centerControl != null ? centerControl.MarginY(7) : this.Space(0),
				this.Spring(),
				this.Group(btnMin.VAlignMiddle(), btnMax.VAlignMiddle(), btnClose.VAlignMiddle())
			)
			.Padding(12, 0, 0, 0)
			.BackColor(Color.FromArgb(243, 243, 243))
			.GrowW();

			titleBarGroup.Height = Scale(height);



			// ============================================================
			// 🌟 3. ATTACH HIT-TEST FILTERS (THIS FIXES EVERYTHING)
			// ============================================================

			// Make the whole title bar container transparent to hits → draggable
			new SmartTransparentControlFilter(titleBarGroup);

			//// Also make the icon and title labels transparent
			new SmartTransparentControlFilter(lblIcon);
			new SmartTransparentControlFilter(lblTitle);

			// Make caption buttons transparent → so the Form can detect them
			new SmartCaptionControlFilter(btnMin);
			new SmartCaptionControlFilter(btnMax);
			new SmartCaptionControlFilter(btnClose);

			// If we have a resizing helper, bind the buttons and register interactive controls
			if (_resizableHelper != null)
			{
				_resizableHelper.BindCaptionControls(titleBarGroup, btnMin, btnMax, btnClose);

				// Center control (search box) must be interactive so it can get focus
				if (centerControl != null)
					_resizableHelper.AddInteractiveControl(centerControl);
			}

			// 4. (Optional) If your hamburger menu is placed inside the title bar, register it too
			// For example, if you have a `hamburgerBtn` that lives inside titleBarGroup:
			// _resizableHelper?.AddInteractiveControl(hamburgerBtn);

			return titleBarGroup;
		}
		// --- BAŞLIK ÇUBUĞU PENCERE BUTONU YAPICI ---
		private Button CreateCaptionButton(string icon, Action onClick, bool isClose = false)
		{
			Button btn = new Button
			{
				Text = icon,
				Font = new Font("Segoe Fluent Icons", 8.5f),
				Width = Scale(46), // Windows 11 standart sistem kontrol butonu genişliği
				Height = Scale(32),
				FlatStyle = FlatStyle.Flat,
				BackColor = Color.Transparent,
				ForeColor = Color.FromArgb(32, 32, 32)
			};
			if (btn.Font.Name != "Segoe Fluent Icons") btn.Font = new Font("Segoe MDL2 Assets", 8.5f);
			btn.FlatAppearance.BorderSize = 0;

			if (isClose)
			{
				btn.HoverBackColor(Color.FromArgb(232, 17, 35)); // Win11 kapatma kırmızısı
				btn.MouseEnter += (s, e) => btn.ForeColor = Color.White;
				btn.MouseLeave += (s, e) => btn.ForeColor = Color.FromArgb(32, 32, 32);
			}
			else
			{
				btn.HoverBackColor(Color.FromArgb(230, 230, 230));
			}

			if (onClick != null) btn.OnClick(onClick);

			return btn;
		}

		// --- 🌟 TEK SATIRDA BOYUTLANDIRMA BAĞLANTISI ---
		public void MakeFormResizable_v1(int borderSize = 8, bool smallCorners = false)
		{
			// NativeWindow sınıfını arka planda bu formun Hwnd'sine bağlar
			_resizableHelper = new SmartFormResizable(_form, borderSize, smallCorners);
		}



	}

	// --- 🌟 WINDOWS 11 SEÇİM KUTUSU ÇİZİM SINIFI ---
	// --- 🌟 WINDOWS 11 SEÇİM KUTUSU ÇİZİM SINIFI (GÜNCELLENMİŞ) ---
	public class FluentCheckBox_old1 : CheckBox
	{
		// 🌟 BANANA FOCUS FIX: Windows 11'in yazı etrafına çizdiği mavi odak çizgisini kesin olarak kapatır!
		protected override bool ShowFocusCues => false;

		public Color CheckedColor { get; set; } = Color.FromArgb(0, 103, 192);

		public FluentCheckBox_old1()
		{

			// 🌟 BANANA FIX: ControlStyles.UserPaint özelliğini aktif ediyoruz!
			// Böylece Windows arkadan kendi kaba odak çizgilerini (focus cues) çizip yazımızı kirletemez.
			this.SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
			this.Cursor = Cursors.Hand;
		}

		// Akıllı şeffaf atlatıcı
		private Color GetRealParentBackColor()
		{
			Control p = Parent;
			int depth = 0;
			while (p != null && (p.BackColor == Color.Transparent || p.BackColor.A == 0) && depth < 10)
			{
				p = p.Parent;
				depth++;
			}
			return p?.BackColor ?? SystemColors.Control;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

			Color parentColor = GetRealParentBackColor();
			using (SolidBrush bgBrush = new SolidBrush(parentColor))
			{
				e.Graphics.FillRectangle(bgBrush, this.ClientRectangle);
			}

			float scale = this.DeviceDpi / 96f;
			int boxSize = (int)(16 * scale);
			int boxY = (this.Height - boxSize) / 2;

			Rectangle boxRect = new Rectangle(1, boxY, boxSize, boxSize);

			if (this.Checked)
			{
				using (SolidBrush fillBrush = new SolidBrush(CheckedColor))
				using (GraphicsPath path = GetRoundedPath(boxRect, (int)(3 * scale)))
				{
					e.Graphics.FillPath(fillBrush, path);
				}

				string checkIcon = "\uE73E";
				using (Font iconFont = new Font("Segoe Fluent Icons", 8.5f * scale))
				{
					Font resolvedFont = iconFont.Name == "Segoe Fluent Icons" ? iconFont : new Font("Segoe MDL2 Assets", 8.5f * scale);
					TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter;
					TextRenderer.DrawText(e.Graphics, checkIcon, resolvedFont, boxRect, Color.White, flags);
				}
			}
			else
			{
				using (Pen borderPen = new Pen(Color.FromArgb(135, 135, 135), 1.5f * scale))
				using (GraphicsPath path = GetRoundedPath(boxRect, (int)(3 * scale)))
				{
					e.Graphics.DrawPath(borderPen, path);
				}
			}

			Rectangle textRect = new Rectangle(boxSize + (int)(8 * scale), 0, this.Width - boxSize - (int)(8 * scale), this.Height);
			TextFormatFlags textFlags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
			TextRenderer.DrawText(e.Graphics, this.Text, this.Font, textRect, this.ForeColor, textFlags);
		}

		private static GraphicsPath GetRoundedPath(Rectangle rect, int radius)
		{
			GraphicsPath path = new GraphicsPath();
			if (radius <= 0) { path.AddRectangle(rect); return path; }
			int d = radius * 2;
			Rectangle arc = new Rectangle(rect.X, rect.Y, d, d);
			path.AddArc(arc, 180, 90);
			arc.X = rect.Right - d - 1;
			path.AddArc(arc, 270, 90);
			arc.Y = rect.Bottom - d - 1;
			path.AddArc(arc, 0, 90);
			arc.X = rect.Left;
			path.AddArc(arc, 90, 90);
			path.CloseFigure();
			return path;
		}
	}

	// --- 🌟 FLUENT CHECKBOX - CONTROL'DAN TÜRETİLMİŞ (ODAK ÇİZGİSİZ) ---
	// --- 🌟 WINDOWS 11 SEÇİM KUTUSU - CONTROL'DAN TÜRETİLMİŞ (ODAK ÇİZGİSİZ) ---
	// --- 🌟 WINDOWS 11 SEÇİM KUTUSU - CONTROL'DAN TÜRETİLMİŞ (ODAK ÇİZGİSİZ) ---
	public class FluentCheckBox : Control
	{
		private bool _checked;
		public bool Checked
		{
			get => _checked;
			set
			{
				if (_checked != value)
				{
					_checked = value;
					Invalidate();
					CheckedChanged?.Invoke(this, EventArgs.Empty);
				}
			}
		}

		public event EventHandler CheckedChanged;
		public Color CheckedColor { get; set; } = Color.FromArgb(0, 103, 192);

		public FluentCheckBox()
		{
			this.SetStyle(
				ControlStyles.UserPaint |
				ControlStyles.OptimizedDoubleBuffer |
				ControlStyles.AllPaintingInWmPaint |
				ControlStyles.ResizeRedraw |
				ControlStyles.StandardClick |
				ControlStyles.SupportsTransparentBackColor,
				true
			);

			this.Cursor = Cursors.Hand;
			this.AutoSize = true;
			this.BackColor = Color.Transparent;
		}

		// 🌟 BANANA FIX: GetPreferredSize override - BOYUT HESAPLAMA
		public override Size GetPreferredSize(Size proposedSize)
		{
			float scale = DeviceDpi / 96f;
			int boxSize = (int)(16 * scale);
			int spacing = (int)(8 * scale);

			// Metin boyutunu ölç
			Size textSize = TextRenderer.MeasureText(
				this.Text,
				this.Font,
				new Size(int.MaxValue, int.MaxValue),
				TextFormatFlags.Left | TextFormatFlags.VerticalCenter
			);

			// Genişlik: kare + boşluk + metin + sağ boşluk
			int width = boxSize + spacing + textSize.Width + (int)(4 * scale);

			// Yükseklik: kare ve metinden hangisi büyükse
			int height = Math.Max(boxSize, textSize.Height) + (int)(4 * scale);

			return new Size(width, height);
		}

		private Color GetRealParentBackColor()
		{
			Control p = Parent;
			int depth = 0;
			while (p != null && (p.BackColor == Color.Transparent || p.BackColor.A == 0) && depth < 10)
			{
				p = p.Parent;
				depth++;
			}
			return p?.BackColor ?? SystemColors.Control;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

			Color clearColor = (BackColor == Color.Transparent || BackColor.A == 0)
				? GetRealParentBackColor()
				: BackColor;

			e.Graphics.Clear(clearColor);

			float scale = this.DeviceDpi / 96f;
			int boxSize = (int)(16 * scale);
			int boxY = (this.Height - boxSize) / 2;

			Rectangle boxRect = new Rectangle(1, boxY, boxSize, boxSize);

			// --- CHECKBOX KARESİ ---
			if (this.Checked)
			{
				using (SolidBrush fillBrush = new SolidBrush(CheckedColor))
				using (GraphicsPath path = GetRoundedPath(boxRect, (int)(3 * scale)))
				{
					e.Graphics.FillPath(fillBrush, path);
				}

				// 🌟 BANANA FIX v2: DrawString + StringFormat = gerçek merkez
				string checkIcon = "\uE73E";
				using (Font iconFont = new Font("Segoe Fluent Icons", 8.5f * scale))
				{
					Font resolvedFont = iconFont.Name == "Segoe Fluent Icons" ? iconFont : new Font("Segoe MDL2 Assets", 8.5f * scale);

					using (StringFormat sf = new StringFormat())
					{
						sf.Alignment = StringAlignment.Center;
						sf.LineAlignment = StringAlignment.Center;
						sf.FormatFlags = StringFormatFlags.NoWrap;

						using (SolidBrush textBrush = new SolidBrush(Color.White))
						{
							e.Graphics.DrawString(checkIcon, resolvedFont, textBrush, boxRect, sf);
						}
					}
				}
			}
			else
			{
				using (Pen borderPen = new Pen(Color.FromArgb(135, 135, 135), 1.5f * scale))
				using (GraphicsPath path = GetRoundedPath(boxRect, (int)(3 * scale)))
				{
					e.Graphics.DrawPath(borderPen, path);
				}
			}

			// --- METİN ---
			Rectangle textRect = new Rectangle(
				boxSize + (int)(8 * scale),
				0,
				this.Width - boxSize - (int)(8 * scale),
				this.Height
			);

			TextFormatFlags textFlags = TextFormatFlags.Left |
										TextFormatFlags.VerticalCenter |
										TextFormatFlags.EndEllipsis;

			TextRenderer.DrawText(e.Graphics, this.Text, this.Font, textRect, this.ForeColor, textFlags);
		}

		protected override void OnClick(EventArgs e)
		{
			base.OnClick(e);
			this.Checked = !this.Checked;
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
			{
				this.Checked = !this.Checked;
				e.Handled = true;
			}
			base.OnKeyDown(e);
		}

		private static GraphicsPath GetRoundedPath(Rectangle rect, int radius)
		{
			GraphicsPath path = new GraphicsPath();
			if (radius <= 0) { path.AddRectangle(rect); return path; }
			int d = radius * 2;
			Rectangle arc = new Rectangle(rect.X, rect.Y, d, d);
			path.AddArc(arc, 180, 90);
			arc.X = rect.Right - d - 1;
			path.AddArc(arc, 270, 90);
			arc.Y = rect.Bottom - d - 1;
			path.AddArc(arc, 0, 90);
			arc.X = rect.Left;
			path.AddArc(arc, 90, 90);
			path.CloseFigure();
			return path;
		}
	}



	// --- 🌟 WINDOWS 11 MODERN PROGRESS BAR SIZINTISIZ ÇİZİM SINIFI ---
	public class FluentProgressBar : Panel
	{
		private float _value = 0f;
		public float Value
		{
			get => _value;
			set { _value = Math.Max(0f, Math.Min(100f, value)); this.Invalidate(); }
		}

		public Color ProgressColor { get; set; } = Color.FromArgb(0, 103, 192);

		public FluentProgressBar()
		{
			this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
			this.BackColor = Color.FromArgb(224, 224, 224);
		}

		// 🌟 AKILLI ŞEFFAF ATLATICI BROWSER YÖNTEMİ
		private Color GetRealParentBackColor()
		{
			Control p = Parent;
			int depth = 0;
			while (p != null && (p.BackColor == Color.Transparent || p.BackColor.A == 0) && depth < 10)
			{
				p = p.Parent;
				depth++;
			}
			return p?.BackColor ?? SystemColors.Control;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

			// 🌟 ESKİ: Color parentColor = this.Parent?.BackColor ...
			// 🌟 YENİ: Gerçek rengi yukarı doğru arayarak sızıntıyı önlüyoruz!
			Color parentColor = GetRealParentBackColor();
			using (SolidBrush bgBrush = new SolidBrush(parentColor))
			{
				e.Graphics.FillRectangle(bgBrush, this.ClientRectangle);
			}

			Rectangle rect = this.ClientRectangle;
			rect.Width -= 1; rect.Height -= 1;

			int radius = this.Height / 2;
			using (GraphicsPath path = GetRoundedPath(rect, radius))
			{
				using (SolidBrush trackBrush = new SolidBrush(Color.FromArgb(224, 224, 224)))
				{
					e.Graphics.FillPath(trackBrush, path);
				}

				float progressWidth = (this.Width * (_value / 100f));
				if (progressWidth > 0)
				{
					// 🌟 SIZINTI VE KIRPIŞMAYI SIFIRA İNDİREN SETCLIP MANTIĞI:
					e.Graphics.SetClip(path);

					Rectangle progressRect = new Rectangle(0, 0, (int)progressWidth, this.Height);
					using (SolidBrush brush = new SolidBrush(ProgressColor))
					{
						e.Graphics.FillRectangle(brush, progressRect);
					}

					e.Graphics.ResetClip();
				}
			}
		}

		private static GraphicsPath GetRoundedPath(Rectangle rect, int radius)
		{
			GraphicsPath path = new GraphicsPath();
			if (radius <= 0) { path.AddRectangle(rect); return path; }
			int d = radius * 2;
			Rectangle arc = new Rectangle(rect.X, rect.Y, d, d);
			path.AddArc(arc, 180, 90);
			arc.X = rect.Right - d - 1;
			path.AddArc(arc, 270, 90);
			arc.Y = rect.Bottom - d - 1;
			path.AddArc(arc, 0, 90);
			arc.X = rect.Left;
			path.AddArc(arc, 90, 90);
			path.CloseFigure();
			return path;
		}
	}

	// --- 🌟 DÖNEN YÜKLEME HALKASI SIZINTISIZ ÇİZİM SINIFI ---
	public class FluentLoadingRing : Panel
	{
		private float _angle = 0f;
		private System.Windows.Forms.Timer _timer;

		public FluentLoadingRing()
		{
			this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
			_timer = new System.Windows.Forms.Timer { Interval = 16 };
			_timer.Tick += (s, e) => { _angle = (_angle + 6f) % 360f; this.Invalidate(); };
			_timer.Start();
		}

		// 🌟 AKILLI ŞEFFAF ATLATICI BROWSER YÖNTEMİ
		private Color GetRealParentBackColor()
		{
			Control p = Parent;
			int depth = 0;
			while (p != null && (p.BackColor == Color.Transparent || p.BackColor.A == 0) && depth < 10)
			{
				p = p.Parent;
				depth++;
			}
			return p?.BackColor ?? SystemColors.Control;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

			// 🌟 Çift arabellek temizliğini katı renk bulup yapıyoruz
			Color parentColor = GetRealParentBackColor();
			using (SolidBrush bgBrush = new SolidBrush(parentColor))
			{
				e.Graphics.FillRectangle(bgBrush, this.ClientRectangle);
			}

			Rectangle rect = this.ClientRectangle;
			rect.Width -= 3; rect.Height -= 3;
			rect.X += 1; rect.Y += 1;

			using (Pen bgPen = new Pen(Color.FromArgb(224, 224, 224), 2.5f))
			using (Pen activePen = new Pen(Color.FromArgb(0, 103, 192), 2.5f))
			{
				e.Graphics.DrawArc(bgPen, rect, 0, 360);
				e.Graphics.DrawArc(activePen, rect, _angle, 100);
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing) _timer?.Dispose();
			base.Dispose(disposing);
		}
	}

	// --- 🌟 PORTABLE TEXTBOX PLACEHOLDER EXTENSIONS ---
	public static class TextBoxPlaceholderExtensions
	{
		public static TextBox Placeholder(this TextBox tb, string placeholderText)
		{
			tb.Text = placeholderText;
			tb.ForeColor = Color.Gray;

			tb.Enter += (s, e) => {
				if (tb.Text == placeholderText)
				{
					tb.Text = "";
					tb.ForeColor = Color.FromArgb(32, 32, 32);
				}
			};

			tb.Leave += (s, e) => {
				if (string.IsNullOrWhiteSpace(tb.Text))
				{
					tb.Text = placeholderText;
					tb.ForeColor = Color.Gray;
				}
			};

			return tb;
		}
	}
}