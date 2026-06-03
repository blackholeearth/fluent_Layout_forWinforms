using SmartLayoutEngine;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace smartUI_fluent_Layout.examples
{
	// 🌟 Sınıf ismi ve yapısı istediğiniz gibi partial class olarak güncellendi.
	public partial class frm_TaskManager2 : Form
	{
		private SmartUI ui;
		private SmartSidePanel leftSidePanel;
		private Panel _sidebarSpacer;
		private bool _isSidebarExpanded = false; // Kapalı (Resim 1) başlar

		// 🌟 Kullanıcının kolon boyutlarını sürükleyebileceği modern Win11 tablosu
		private DataGridView dgvProcesses;

		public frm_TaskManager2()
		{
			this.Text = "Görev Yöneticisi";
			this.Size = new Size(1000, 700);
			this.MinimumSize = new Size(800, 500);
			this.BackColor = Color.FromArgb(243, 243, 243); // Win11 Açık Tema Arka Planı

			ui = new SmartUI(this);

			// --- 1. SIDEBAR KURULUMU (Hamburger Toggles) ---
			leftSidePanel = (SmartSidePanel)ui.SidePanel(Side.Left, 60, new Control[] { });
			leftSidePanel.BackColor = Color.FromArgb(249, 249, 249);
			PopulateSidebar(leftSidePanel);


			// ---2.ÜST ARAMA VE AKSİYON ALANI ---
		 //  TextBox txtSearch = new TextBox
		 //  {
			//   BorderStyle = BorderStyle.None,
			//   Font = new Font("Segoe UI", 9.5f),
			//   Width = 260,
			//   BackColor = Color.White
		 //  };
			//// Eklediğiniz managed placeholder desteğiyle arama metnini bağlıyoruz
			////txtSearch.Placeholder("Aramak için bir ad, yayımcı veya PID girin...");

			//Panel searchContainer = new Panel()
			//{
			//	Width = 290,
			//	Height = 32,
			//	BackColor = Color.White
			//};
			//searchContainer.Rounded(6, Color.FromArgb(218, 218, 218));

			//txtSearch.Location = new Point(10, 7);
			//searchContainer.Controls.Add(txtSearch);



			// Solunda büyüteç ikonu hazır gelen arama kutusu
			Control searchBox = ui.FluentSearchBox_v1("İşlemlerde ara...", width: 280)
				.Padding(8)
				.MarginY(4);
			//var searchRow = ui.Row(
			//	ui.Spring(),
			//	searchBox.VAlignMiddle(),
			//	ui.Spring()
			//	);

			this.FormBorderStyle = FormBorderStyle.None;
			// --- 🌟 2. ADIM: BAŞLIK ÇUBUĞU İÇİN ARAMA KUTUSU OLUŞTURMA ---
			// Solunda büyüteç ikonu hazır gelen, genişliği 400px olan akıllı arama kutusu
			Control titleSearch = ui.FluentSearchBox_v1("Aramak için bir ad, yayımcı veya PID girin...", width: 280);

			// --- 🌟 3. ADIM: CUSTOM TALL TITLE BAR KURULUMU ---
			// Arama kutusunu tam ortasına gömdüğümüz 48px yüksekliğinde modern başlık çubuğu
			Control customTitleBar = ui.SetupCustomTitleBar_v1("Görev Yöneticisi", titleSearch, height: 48*2);


			// Başlık çubuğunu formun en üst satırı (Row 1) olarak deklare ediyoruz
			ui.Row(customTitleBar.GrowW()).Margin(0);


			var btnNewTask = CreateHeaderButton("\uE710", "Yeni görevi çalıştır");
			var btnEndTask = CreateHeaderButton("\uE711", "Görevi sonlandır");
			var btnEcoMode = CreateHeaderButton(SegoeMDL2Icons.Leaf, "Verimlilik modu");
			var btnMore = CreateHeaderButton("\uE712", "");

			var headerRow = ui.Row(
				ui.Spring(), // Geri kalan elemanları en sağa iter
				btnNewTask.VAlignMiddle(),
				ui.Space(6),
				btnEndTask.VAlignMiddle(),
				ui.Space(6),
				btnEcoMode.VAlignMiddle(),
				ui.Space(6),
				btnMore.VAlignMiddle()
			)
			.Padding(7)
			.Margin(1, 1, 1, 1)
			.BackColor(Color.White)
			;

			// --- 3. AKILLI TABLONUN (GRID) BAĞLANMASI ---
			SetupProcessGrid();

			// Tabloyu SmartUI Row içine gömüp GrowH ve GrowW veriyoruz.
			// Böylece form büyüdükçe tablo da otomatik esneyecektir.
			ui.Row(dgvProcesses.GrowW().GrowH())
			  .Margin(1, 1, 1, 16)
			  .Padding(10, 0, 0, 0)
			  .BackColor(Color.White);
			  ;

			// Verileri yükle
			LoadMockData();
		}

		// --- MODERN WIN11 DATA GRID VIEW TASARIMI ---

		// --- 1. TABLO KURULUMU (SetupProcessGrid) ---
		private void SetupProcessGrid()
		{
			dgvProcesses = new DataGridView
			{
				BackgroundColor = Color.White,
				BorderStyle = BorderStyle.None,
				CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
				GridColor = Color.FromArgb(240, 240, 240),
				RowHeadersVisible = false,
				SelectionMode = DataGridViewSelectionMode.FullRowSelect,
				MultiSelect = false,
				AllowUserToAddRows = false,
				AllowUserToDeleteRows = false,
				AllowUserToOrderColumns = true,
				AllowUserToResizeRows = false,
				RowTemplate = { Height = 36 },
				EnableHeadersVisualStyles = false,
				ColumnHeadersHeight = 55, // 🌟 2 Satırlı başlık sığması için yükseklik 46px yapıldı
				ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
			};

			// Çift arabelleği açıyoruz (Titreme önleme)
			typeof(DataGridView).InvokeMember("DoubleBuffered",
				BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
				null, dgvProcesses, new object[] { true });

			// Kolon Başlığı varsayılan stilleri
			dgvProcesses.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(249, 249, 249);
			dgvProcesses.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(32, 32, 32);
			dgvProcesses.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5f); // Küçük alt font
			dgvProcesses.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(249, 249, 249);

			// Normal Satır varsayılan stilleri
			dgvProcesses.DefaultCellStyle.BackColor = Color.White;
			dgvProcesses.DefaultCellStyle.ForeColor = Color.FromArgb(32, 32, 32);
			dgvProcesses.DefaultCellStyle.Font = new Font("Segoe UI", 9f);
			dgvProcesses.DefaultCellStyle.SelectionBackColor = Color.FromArgb(234, 244, 252);
			dgvProcesses.DefaultCellStyle.SelectionForeColor = Color.Black;

			// Kolonları oluştur (Kullanıcı dilediği gibi sürükleyip boyutlandırabilir)
			var colName = new DataGridViewTextBoxColumn { Name = "Ad", HeaderText = "Ad", Width = 280, MinimumWidth = 100 };
			var colStatus = new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Durum", Width = 90, MinimumWidth = 50 };
			var colCpu = new DataGridViewTextBoxColumn { Name = "CPU", HeaderText = "% CPU", Width = 100, MinimumWidth = 60 };
			var colMem = new DataGridViewTextBoxColumn { Name = "Mem", HeaderText = "% Bellek", Width = 110, MinimumWidth = 60 };
			var colDisk = new DataGridViewTextBoxColumn { Name = "Disk", HeaderText = "% Disk", Width = 110, MinimumWidth = 60 };
			var colNet = new DataGridViewTextBoxColumn { Name = "Net", HeaderText = "% Ağ", Width = 100, MinimumWidth = 60 };

			// 🌟 GÖRSEL 1 GİBİ SAYISAL VERİLERİ SAĞA YASLIYORUZ
			colCpu.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
			colMem.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
			colDisk.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
			colNet.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

			dgvProcesses.Columns.AddRange(colName, colStatus, colCpu, colMem, colDisk, colNet);

			dgvProcesses.CellPainting += DgvProcesses_CellPainting;
		}

		// --- 2. AKILLI HÜCRE VE BAŞLIK ÇİZİMİ (DgvProcesses_CellPainting) ---
		private void DgvProcesses_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
		{
			// 🌟 A. KOLON BAŞLIĞI BOYAMA (e.RowIndex == -1)
			// 🌟 1. DİNAMİK KOLON BAŞLIĞI BOYAMA (e.RowIndex == -1)
			if (e.RowIndex == -1)
			{
				if (e.ColumnIndex >= 0)
				{
					e.Handled = true; // WinForms kaba siyah çizgilerini engelle

					// Başlık Arka Planı (Açık Gri)
					using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(249, 249, 249)))
					{
						e.Graphics.FillRectangle(bgBrush, e.CellBounds);
					}

					// İnce ve Tatlı Gri Sınır Çizgisi
					using (Pen borderPen = new Pen(Color.FromArgb(225, 225, 225)))
					{
						e.Graphics.DrawLine(borderPen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
						e.Graphics.DrawLine(borderPen, e.CellBounds.Right - 1, e.CellBounds.Top, e.CellBounds.Right - 1, e.CellBounds.Bottom - 2);
					}

					// Font Tanımlamaları
					using (Font topFont = new Font("Segoe UI Semibold", 10f))
					using (Font bottomFont = new Font("Segoe UI", 8.25f))
					{
						// 🌟 BANANA DİNAMİK YÜKSEKLİK MATEMATİĞİ
						int topHeight = topFont.Height;         // Üst satırın gerçek font yüksekliği
						int bottomHeight = bottomFont.Height;   // Alt satırın gerçek font yüksekliği
						int totalTextHeight = topHeight + bottomHeight;

						// Yazı bloğunu hücre yüksekliği (e.CellBounds.Height) içinde dikeyde mükemmel ortala
						int verticalPadding = (e.CellBounds.Height - totalTextHeight) / 2;

						if (e.ColumnIndex == 0 || e.ColumnIndex == 1)
						{
							// "Ad" ve "Durum" kelimelerini diğer sütunların alt satırıyla (Baseline) milimetrik eşitliyoruz
							string name = e.Value?.ToString() ?? "";
							Rectangle bottomBounds = new Rectangle(
								e.CellBounds.Left + 8,
								e.CellBounds.Top + verticalPadding + topHeight, // Diğer alt satırlarla aynı hizaya kilitler
								e.CellBounds.Width - 12,
								bottomHeight
							);
							TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
							TextRenderer.DrawText(e.Graphics, name, bottomFont, bottomBounds, Color.FromArgb(32, 32, 32), flags);
						}
						else if (e.ColumnIndex >= 2 && e.ColumnIndex <= 5)
						{
							string topLineText = "";
							string bottomLineText = "";

							if (e.ColumnIndex == 2) { topLineText = "%6"; bottomLineText = "CPU"; }
							else if (e.ColumnIndex == 3) { topLineText = "%88"; bottomLineText = "Bellek"; }
							else if (e.ColumnIndex == 4) { topLineText = "%1"; bottomLineText = "Disk"; }
							else if (e.ColumnIndex == 5) { topLineText = "%0"; bottomLineText = "Ağ"; }

							// Üst Satır (% Değeri)
							Rectangle topBounds = new Rectangle(
								e.CellBounds.Left,
								e.CellBounds.Top + verticalPadding,
								e.CellBounds.Width - 12,
								topHeight
							);
							TextRenderer.DrawText(e.Graphics, topLineText, topFont, topBounds, Color.FromArgb(32, 32, 32), TextFormatFlags.Right | TextFormatFlags.VerticalCenter);

							// 🌟 ALT SATIR (Kolon Adı): Üst satırın bittiği sınırın (topBounds.Bottom) hemen altından başlar!
							Rectangle bottomBounds = new Rectangle(
								e.CellBounds.Left,
								topBounds.Bottom, // Çakışma veya boşluk kalma riski sıfıra indirildi
								e.CellBounds.Width - 12,
								bottomHeight
							);
							TextRenderer.DrawText(e.Graphics, bottomLineText, bottomFont, bottomBounds, Color.FromArgb(120, 120, 120), TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
						}

						// 🌟 DİNAMİK SIRA GÖSTERGESİ (SORT INDICATOR):
						bool isSortedColumn = (dgvProcesses.SortedColumn != null && dgvProcesses.SortedColumn.Index == e.ColumnIndex) || (dgvProcesses.SortedColumn == null && e.ColumnIndex == 3);
						SortOrder sortOrder = dgvProcesses.SortedColumn != null ? dgvProcesses.SortOrder : SortOrder.Descending;

						if (isSortedColumn)
						{
							string sortIcon = sortOrder == SortOrder.Ascending ? "\uE70E" : "\uE70D"; // Chevron Up / Down
							Font sortFont = new Font("Segoe Fluent Icons", 6.5f);
							if (sortFont.Name != "Segoe Fluent Icons") sortFont = new Font("Segoe MDL2 Assets", 6.5f);

							// İkon dikeyde tam ortalama mesafesine göre dinamik olarak en tepeye çizilir
							Rectangle sortBounds = new Rectangle(e.CellBounds.Right - 36, e.CellBounds.Top + verticalPadding - 8, 16, 12);
							TextRenderer.DrawText(e.Graphics, sortIcon, sortFont, sortBounds, Color.FromArgb(120, 120, 120), TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
						}
					}
				}
				return;
			}

			// 🌟 B. NORMAL SATIR BOYAMA (e.RowIndex >= 0)
			// CPU, RAM, Disk ve Net hücreleri (2, 3, 4 ve 5. kolonlar)
			if (e.ColumnIndex >= 2 && e.ColumnIndex <= 5)
			{
				string cellVal = e.Value?.ToString() ?? "";
				double val = ParseToDouble(cellVal);

				Color cellBgColor = GetCellColor(e.ColumnIndex, val);

				if (cellBgColor != Color.Transparent)
				{
					e.Handled = true; // WinForms varsayılan hücre çizimini devral

					// Hücre arka planını boya
					using (SolidBrush bgBrush = new SolidBrush(cellBgColor))
					{
						e.Graphics.FillRectangle(bgBrush, e.CellBounds);
					}

					// Izgara çizgilerini çiz
					using (Pen gridPen = new Pen(dgvProcesses.GridColor))
					{
						e.Graphics.DrawLine(gridPen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
						e.Graphics.DrawLine(gridPen, e.CellBounds.Right - 1, e.CellBounds.Top, e.CellBounds.Right - 1, e.CellBounds.Bottom);
					}

					// 🌟 Sayısal hücre verilerini de 12px sağ boşlukla sağa hizalıyoruz
					TextFormatFlags flags = TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
					Rectangle cellBounds = new Rectangle(e.CellBounds.Left, e.CellBounds.Top, e.CellBounds.Width - 12, e.CellBounds.Height);
					TextRenderer.DrawText(e.Graphics, cellVal, e.CellStyle.Font, cellBounds, Color.FromArgb(32, 32, 32), flags);
				}
			}
			// Eco Sütununda Yaprak İkonu Çizimi
			else if (e.ColumnIndex == 1)
			{
				string cellValue = e.Value?.ToString() ?? "";
				if (cellValue == "Eco")
				{
					e.Handled = true;
					e.PaintBackground(e.CellBounds, true);

					string leafIcon = SegoeMDL2Icons.Leaf;
					Font iconFont = new Font("Segoe Fluent Icons", 9.5f);
					if (iconFont.Name != "Segoe Fluent Icons") iconFont = new Font("Segoe MDL2 Assets", 9.5f);

					TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter;
					TextRenderer.DrawText(e.Graphics, leafIcon, iconFont, e.CellBounds, Color.FromArgb(0, 138, 0), flags);
				}
			}
		}


		// --- GÖREV YÖNETİCİSİ RENK YOĞUNLUĞU VE ÖZEL ÇİZİM SİSTEMİ ---
		// --- RENK TANIMLAMALARI (BANANA FLUENT PALETTE) ---
		private readonly Color colorHigh = Color.FromArgb(89, 199, 255);     // #59c7ff
		private readonly Color colorMedium = Color.FromArgb(198, 235, 255);   // #c6ebff
		private readonly Color colorLow = Color.FromArgb(226, 244, 255);      // #e2f4ff (Çok hafif pastel mavi)

		// --- AKILLI DEĞER AYRIŞTIRICI (CULTURE-INVARIANT PARSER) ---
		// "1.675,9 MB" veya "%1,4" gibi verileri işletim sistemi dilinden bağımsız olarak temizleyip Double'a çevirir
		private double ParseToDouble(string value)
		{
			if (string.IsNullOrEmpty(value)) return 0;

			string cleaned = value.Replace("%", "")
								  .Replace("MB/sn", "")
								  .Replace("MB", "")
								  .Replace(" ", "")
								  .Trim();

			// Türkçe (1.675,9) veya İngilizce (1,675.9) ayrımını çözmek için
			if (cleaned.Contains(".") && cleaned.Contains(","))
			{
				cleaned = cleaned.Replace(".", "").Replace(",", ".");
			}
			else if (cleaned.Contains(","))
			{
				cleaned = cleaned.Replace(",", ".");
			}

			if (double.TryParse(cleaned, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double result))
			{
				return result;
			}
			return 0;
		}

		// --- AKILLI RENK SEÇİCİ ---
		// Kullanıcının belirttiği tam limitlere göre hücre rengini belirler
		private Color GetCellColor(int columnIndex, double val)
		{
			if (val <= 0) return Color.Transparent;

			if (columnIndex == 2) // CPU Kolonu
			{
				if (val > 10.0) return colorHigh;
				if (val > 1.0) return colorMedium;
				return colorLow;
			}
			else if (columnIndex == 3) // RAM Kolonu
			{
				if (val > 1000.0) return colorHigh;
				if (val > 100.0) return colorMedium;
				return colorLow;
			}
			else if (columnIndex == 4) // Disk Kolonu
			{
				if (val > 5.0) return colorHigh;
				if (val > 1.0) return colorMedium;
				return colorLow;
			}
			else if (columnIndex == 5) // Ağ (Net) Kolonu (Mock datada değerler düşük olduğu için limitleri buna göre bakar)
			{
				if (val > 2.0) return colorHigh;
				if (val > 0.5) return colorMedium;
				return colorLow;
			}

			return Color.Transparent;
		}

		// --- HÜCRE BOYAMA OLAYI (CELL PAINTING) ---
		private void DgvProcesses_CellPainting_old2(object sender, DataGridViewCellPaintingEventArgs e)
		{
			// 🌟 1. KOLON BAŞLIĞI BOYAMA (e.RowIndex == -1)
			if (e.RowIndex == -1)
			{
				if (e.ColumnIndex >= 0)
				{
					e.Handled = true; // WinForms'un kaba siyah çizgiler çizen çizimini durdur

					// Başlık Arka Planı (Açık Gri)
					using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(249, 249, 249)))
					{
						e.Graphics.FillRectangle(bgBrush, e.CellBounds);
					}

					// 🌟 İnce ve Tatlı Gri Sınır Çizgisi (Black yerine modern Windows 11 grisi)
					using (Pen borderPen = new Pen(Color.FromArgb(225, 225, 225)))
					{
						// Başlığın altındaki yatay çizgi
						e.Graphics.DrawLine(borderPen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
						// Kolonlar arasındaki dikey ayırıcı çizgi
						e.Graphics.DrawLine(borderPen, e.CellBounds.Right - 1, e.CellBounds.Top, e.CellBounds.Right - 1, e.CellBounds.Bottom - 2);
					}

					// Başlık Yazısı (Sola yaslı, dikeyde ortalı ve 8px iç boşluklu)
					if (e.Value != null)
					{
						TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
						Rectangle textBounds = new Rectangle(e.CellBounds.Left + 8, e.CellBounds.Top, e.CellBounds.Width - 12, e.CellBounds.Height);
						TextRenderer.DrawText(e.Graphics, e.Value.ToString(), e.CellStyle.Font, textBounds, e.CellStyle.ForeColor, flags);
					}
				}
				return;
			}

			// 🌟 2. NORMAL SATIR BOYAMA (e.RowIndex >= 0)
			// CPU, RAM, Disk ve Net hücreleri (2, 3, 4 ve 5. kolonlar)
			if (e.ColumnIndex >= 2 && e.ColumnIndex <= 5)
			{
				string cellVal = e.Value?.ToString() ?? "";
				double val = ParseToDouble(cellVal);

				Color cellBgColor = GetCellColor(e.ColumnIndex, val);

				if (cellBgColor != Color.Transparent)
				{
					e.Handled = true; // WinForms varsayılan çizimini devral

					// Hücre arka planını boya
					using (SolidBrush bgBrush = new SolidBrush(cellBgColor))
					{
						e.Graphics.FillRectangle(bgBrush, e.CellBounds);
					}

					// Izgara çizgilerini çiz
					using (Pen gridPen = new Pen(dgvProcesses.GridColor))
					{
						e.Graphics.DrawLine(gridPen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
						e.Graphics.DrawLine(gridPen, e.CellBounds.Right - 1, e.CellBounds.Top, e.CellBounds.Right - 1, e.CellBounds.Bottom);
					}

					// Metni tam ortalayarak çiz
					TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
					TextRenderer.DrawText(e.Graphics, cellVal, e.CellStyle.Font, e.CellBounds, Color.FromArgb(32, 32, 32), flags);
				}
			}
			// Eco Sütununda Yaprak İkonu Çizimi
			else if (e.ColumnIndex == 1)
			{
				string cellValue = e.Value?.ToString() ?? "";
				if (cellValue == "Eco")
				{
					e.Handled = true;
					e.PaintBackground(e.CellBounds, true);

					string leafIcon = "\uE70E";
					Font iconFont = new Font("Segoe Fluent Icons", 9.5f);
					if (iconFont.Name != "Segoe Fluent Icons") iconFont = new Font("Segoe MDL2 Assets", 9.5f);

					TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter;
					TextRenderer.DrawText(e.Graphics, leafIcon, iconFont, e.CellBounds, Color.FromArgb(0, 138, 0), flags);
				}
			}
		}

		// --- VERİ DOLDURUCU (MOCK DATA) ---
		private void LoadMockData()
		{
			dgvProcesses.Rows.Add("zerotier-one_64.exe",	"", "%0", "4,3 MB", "0 MB/sn", "0 MB/sn");
			dgvProcesses.Rows.Add("Başlat (2)",				"", "%0", "4,3 MB", "0 MB/sn", "0 MB/sn");
			dgvProcesses.Rows.Add("Firefox (27)",			"", "%0,5", "1.675,9 MB", "0,1 MB/sn", "0 MB/sn");
			dgvProcesses.Rows.Add("GitHubDesktop.exe (4)",	"Eco", "%0,1", "141,2 MB", "0,2 MB/sn", "0 MB/sn");
			dgvProcesses.Rows.Add("Sublime Text (4)",		"", "%0", "11,5 MB", "0 MB/sn", "0 MB/sn");
			dgvProcesses.Rows.Add("System",					"", "%1,4", "0,1 MB", "5,6 MB/sn", "0 MB/sn");
			dgvProcesses.Rows.Add("Paint (2)",				"", "%0", "143,9 MB", "0,1 MB/sn", "0 MB/sn");
			dgvProcesses.Rows.Add("Everything",				"", "%0", "60,5 MB", "0 MB/sn", "0 MB/sn");
		}

	 
		private void PopulateSidebar(SmartSidePanel sp)
		{
			//// 1. Hamburger Menü Butonu (Artık tamamen evrensel SidebarItem_v1 ile üretiliyor ve kusursuz ortalanıyor!)
			//var btnHamburger = ui.SidebarItem_v2("\uE700", "", isSelected: false, isExpanded: _isSidebarExpanded, showIndicator: false);
			// 🌟 Hamburger Menü Butonu (Yazısız kalacağı ve esnemeyeceği için isExpanded: false, growWidth: false geçiyoruz)
			var btnHamburger = ui.SidebarItem_v2("\uE700", "", 
				isSelected: false, isExpanded: false, showIndicator: false, growWidth: false);

			sp.BaseSize = _isSidebarExpanded ? 230 : 48;
			btnHamburger.Click += (s, e) => {
				ui.FreezeRedraw(); // Silme ve yeniden ekleme başlamadan ÖNCE çizimi dondur!

				_isSidebarExpanded = !_isSidebarExpanded;
				sp.BaseSize = _isSidebarExpanded ? 230 : 48; // Genişlik Windows 11 standardı olan 48px yapıldı

				sp.Controls.Clear();
				sp.Content.Clear();
				PopulateSidebar(sp);

				ui.RefreshLayout();
			};

			sp.AddContent(btnHamburger);
			sp.AddContent(ui.Space(8));

			// Menü Elemanları
			var menuItems = new List<(string Icon, string Text, bool IsSelected)> {
				(SegoeMDL2Icons.AppIconDefault, "İşlemler", true),
				("\uE9D9", "Performans", false),
				("\uE81C", "Uygulama geçmişi", false),
				(SegoeMDL2Icons.SpeedHigh, "Başlangıç uygulamaları", false),
				("\uE716", "Kullanıcılar", false),
				("\uE14C", "Ayrıntılar", false),
				(SegoeMDL2Icons.Puzzle, "Hizmetler", false)
			};

			foreach (var item in menuItems)
			{
				var sidebarBtn = ui.SidebarItem_v2(item.Icon, item.Text, item.IsSelected, _isSidebarExpanded, showIndicator: true);
				sp.AddContent(sidebarBtn);
			}

			// Dikey Yay (Settings'i aşağıya iter)
			sp.AddContent(ui.Spring());

			// En alttaki Ayarlar butonu
			var settingsBtn = ui.SidebarItem_v2("\uE713", "Ayarlar", false, _isSidebarExpanded, showIndicator: false);
			sp.AddContent(settingsBtn);
		}

		private Control CreateHeaderButton(string icon, string text)
		{
			// 1. İkon Tanımlaması (Segoe Fluent)
			Label ico = new Label
			{
				Text = icon,
				Font = new Font("Segoe Fluent Icons", 10f),
				AutoSize = true,
				BackColor = Color.Transparent,
				ForeColor = Color.FromArgb(32, 32, 32)
			};
			if (ico.Font.Name != "Segoe Fluent Icons") ico.Font = new Font("Segoe MDL2 Assets", 10f);

			// 2. Metin Tanımlaması (Segoe UI - İkon fontundan bağımsız, temiz ve okunaklı)
			Label lbl = null;
			if (!string.IsNullOrEmpty(text))
			{
				lbl = new Label
				{
					Text = text,
					Font = new Font("Segoe UI", 9f),
					AutoSize = true,
					BackColor = Color.Transparent,
					ForeColor = Color.FromArgb(32, 32, 32)
				};
			}

			// 3. Deklaratif Grup Oluşturma
			Control btnGroup;
			if (lbl != null)
			{
				btnGroup = ui.Group(
					ico.VAlignMiddle(),
					ui.Space(8), // İkon ve yazı arasındaki 8px boşluk
					lbl.VAlignMiddle()
				);
			}
			else
			{
				btnGroup = ui.Group(ico.VAlignMiddle());
			}

			// 4. Windows 11 Fluent Tasarım Sınırları ve Hover Efekti
			btnGroup
				.Padding(12, 6+4, 12, 6+4)
				.BackColor(Color.White)
				//.Rounded(6, Color.FromArgb(218, 218, 218), 1.5f) // İnce şık Win11 kenarlığı
				.Rounded(6) // İnce şık Win11 kenarlığı
				.HoverBackColor(Color.FromArgb(245, 245, 245));

			btnGroup.Cursor = Cursors.Hand;

			// 🌟 Tip-Güvenli Tıklama Yönlendirmesi (Çocuk elemanlara tıklansa bile butonu tetikler)
			foreach (Control child in btnGroup.Controls)
			{
				child.Cursor = Cursors.Hand;
				child.Click += (s, e) => {
					if (btnGroup is SmartGroup sg) sg.PerformClick();
				};
			}

			return btnGroup;
		}


	}
}