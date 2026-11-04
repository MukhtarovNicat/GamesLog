using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace GamesLog
{
    public class GameItem
    {
        public Guid Id { get; set; }
        public string Ad { get; set; }
        public decimal QiymetAzn { get; set; }
        public bool IsOrtalamaQiymet { get; set; } // Ortalama (təxmini) qiymət olub-olmamasını saxlayır
        public DateTime? AlinmaTarixi { get; set; }
        public decimal SatisQiymetiAzn { get; set; }
        public DateTime? SatisTarixi { get; set; }
        public string Platforma { get; set; }
        public string AlinanYer { get; set; }
        public string Status { get; set; }

        public GameItem()
        {
            Id = Guid.NewGuid();
            Ad = string.Empty;
            QiymetAzn = 0m;
            IsOrtalamaQiymet = false;
            AlinmaTarixi = DateTime.Today;
            SatisQiymetiAzn = 0m;
            SatisTarixi = null;
            Platforma = "PlayStation 4";
            AlinanYer = "Aga Playstation";
            Status = "Oynanılır";
        }
    }

    public static class JsonHelper
    {
        private static string Quote(string value)
        {
            return "\"" + EscapeJson(value) + "\"";
        }

        public static string SerializeGames(List<GameItem> games)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("[");
            for (int i = 0; i < games.Count; i++)
            {
                GameItem g = games[i];
                sb.AppendLine("  {");
                sb.AppendLine("    \"Id\": " + Quote(g.Id.ToString()) + ",");
                sb.AppendLine("    \"Ad\": " + Quote(g.Ad) + ",");
                sb.AppendLine("    \"QiymetAzn\": " + g.QiymetAzn.ToString("0.00", CultureInfo.InvariantCulture) + ",");
                sb.AppendLine("    \"IsOrtalamaQiymet\": " + g.IsOrtalamaQiymet.ToString().ToLower() + ",");

                string alinmaTarixiStr = g.AlinmaTarixi.HasValue ? Quote(g.AlinmaTarixi.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)) : "null";
                sb.AppendLine("    \"AlinmaTarixi\": " + alinmaTarixiStr + ",");

                sb.AppendLine("    \"SatisQiymetiAzn\": " + g.SatisQiymetiAzn.ToString("0.00", CultureInfo.InvariantCulture) + ",");

                string satisTarixiStr = g.SatisTarixi.HasValue ? Quote(g.SatisTarixi.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)) : "null";
                sb.AppendLine("    \"SatisTarixi\": " + satisTarixiStr + ",");

                sb.AppendLine("    \"Platforma\": " + Quote(g.Platforma) + ",");
                sb.AppendLine("    \"AlinanYer\": " + Quote(g.AlinanYer) + ",");
                sb.AppendLine("    \"Status\": " + Quote(g.Status));

                if (i < games.Count - 1)
                    sb.AppendLine("  },");
                else
                    sb.AppendLine("  }");
            }
            sb.Append("]");
            return sb.ToString();
        }

        public static List<GameItem> DeserializeGames(string json)
        {
            List<GameItem> result = new List<GameItem>();
            if (string.IsNullOrWhiteSpace(json))
                return result;

            string[] lines = json.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            GameItem current = null;

            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();
                if (line == "{")
                {
                    current = new GameItem();
                }
                else if (line == "}" || line == "},")
                {
                    if (current != null && !string.IsNullOrWhiteSpace(current.Ad))
                    {
                        result.Add(current);
                    }
                    current = null;
                }
                else if (current != null)
                {
                    int colonIndex = line.IndexOf(':');
                    if (colonIndex > 0)
                    {
                        string key = line.Substring(0, colonIndex).Trim().Trim('\"');
                        string val = line.Substring(colonIndex + 1).Trim();

                        if (val.EndsWith(","))
                            val = val.Substring(0, val.Length - 1).Trim();

                        bool isNullVal = (val.ToLower() == "null");

                        if (!isNullVal && val.Length >= 2 && val.StartsWith("\"") && val.EndsWith("\""))
                            val = UnescapeJson(val.Substring(1, val.Length - 2));

                        if (key == "Id")
                        {
                            Guid parsedGuid;
                            if (Guid.TryParse(val, out parsedGuid))
                                current.Id = parsedGuid;
                        }
                        else if (key == "Ad")
                        {
                            current.Ad = val;
                        }
                        else if (key == "QiymetAzn")
                        {
                            decimal parsedPrice;
                            if (decimal.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out parsedPrice))
                                current.QiymetAzn = parsedPrice;
                        }
                        else if (key == "IsOrtalamaQiymet")
                        {
                            bool parsedBool;
                            if (bool.TryParse(val, out parsedBool))
                                current.IsOrtalamaQiymet = parsedBool;
                        }
                        else if (key == "AlinmaTarixi")
                        {
                            if (isNullVal || string.IsNullOrWhiteSpace(val))
                            {
                                current.AlinmaTarixi = null;
                            }
                            else
                            {
                                DateTime parsedDate;
                                if (DateTime.TryParse(val, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
                                    current.AlinmaTarixi = parsedDate.Date;
                            }
                        }
                        else if (key == "SatisQiymetiAzn")
                        {
                            decimal parsedPrice;
                            if (decimal.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out parsedPrice))
                                current.SatisQiymetiAzn = parsedPrice;
                        }
                        else if (key == "SatisTarixi")
                        {
                            if (isNullVal || string.IsNullOrWhiteSpace(val))
                            {
                                current.SatisTarixi = null;
                            }
                            else
                            {
                                DateTime parsedDate;
                                if (DateTime.TryParse(val, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
                                    current.SatisTarixi = parsedDate.Date;
                            }
                        }
                        else if (key == "Platforma")
                        {
                            current.Platforma = val;
                        }
                        else if (key == "AlinanYer")
                        {
                            current.AlinanYer = val;
                        }
                        else if (key == "Status")
                        {
                            current.Status = val;
                        }
                    }
                }
            }

            return result;
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s))
                return string.Empty;

            StringBuilder sb = new StringBuilder(s.Length + 8);
            foreach (char c in s)
            {
                if (c == '\\') sb.Append("\\\\");
                else if (c == '\"') sb.Append("\\\"");
                else if (c == '\r') sb.Append("\\r");
                else if (c == '\n') sb.Append("\\n");
                else if (c == '\t') sb.Append("\\t");
                else sb.Append(c);
            }
            return sb.ToString();
        }

        private static string UnescapeJson(string s)
        {
            if (string.IsNullOrEmpty(s))
                return string.Empty;

            StringBuilder sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '\\' && i + 1 < s.Length)
                {
                    char next = s[i + 1];
                    if (next == '\"') { sb.Append('\"'); i++; }
                    else if (next == '\\') { sb.Append('\\'); i++; }
                    else if (next == '/') { sb.Append('/'); i++; }
                    else if (next == 'r') { sb.Append('\r'); i++; }
                    else if (next == 'n') { sb.Append('\n'); i++; }
                    else if (next == 't') { sb.Append('\t'); i++; }
                    else { sb.Append(next); i++; }
                }
                else
                {
                    sb.Append(s[i]);
                }
            }
            return sb.ToString();
        }
    }

    public class MainForm : Form
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);
        private const int EM_SETCUEBANNER = 0x1501;

        private List<GameItem> _oyunlar = new List<GameItem>();
        private Guid? _secilmisOyunId = null;
        private bool _cedvelYenilenir = false;

        private string _sortColumn = "AlinmaTarixi";
        private bool _sortAscending = false;

        private readonly string _dbFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "oyunlar_db.json");

        private TextBox txtAd;
        private NumericUpDown numQiymet;
        private CheckBox chkOrtalamaQiymet; // Yeni: Ortalama qiymət checkboxu
        private DateTimePicker dtpTarix;
        private CheckBox chkNamelumTarix;
        private NumericUpDown numSatisQiymeti;
        private DateTimePicker dtpSatisTarixi;
        private CheckBox chkSatilib;
        private ComboBox cmbPlatforma;
        private ComboBox cmbAlinanYer;
        private ComboBox cmbStatus;
        private TextBox txtAxtaris;
        private DataGridView dgvOyunlar;
        private Label lblStatistika;
        private Button btnElaveEt;
        private Button btnYenile;
        private Button btnSil;
        private Button btnTemizle;
        private Button btnExport;
        private Button btnImport;

        public MainForm()
        {
            InitializeComponent();
            this.Load += MainForm_Load;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            if (txtAxtaris.IsHandleCreated)
            {
                SendMessage(txtAxtaris.Handle, EM_SETCUEBANNER, 0, "Oyun adı, platforma və ya alınan yer...");
            }
            BazaniYukle();
            CedveliYenile();
            FormuTemizle();

            // Açılışda sətrin seçilməsini bloklamaq üçün qoruma
            this.BeginInvoke((MethodInvoker)delegate {
                dgvOyunlar.ClearSelection();
            });
        }

        private void InitializeComponent()
        {
            this.Text = "Oyun Kitabçası — Satış və Alış İdarəetməsi";
            this.Size = new Size(1180, 840);
            this.MinimumSize = new Size(1000, 620);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            this.BackColor = Color.FromArgb(245, 247, 250);

            Panel pnlSol = new Panel
            {
                Dock = DockStyle.Left,
                Width = 350,
                Padding = new Padding(16),
                BackColor = Color.White,
                AutoScroll = true
            };

            Label lblBasliq = new Label
            {
                Text = "Yeni Oyun / Redaktə",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(16, 12),
                AutoSize = true
            };

            int y = 46;
            int inputSpacing = 54;
            int checkSpacing = 32;

            // 1. Oyun adı
            pnlSol.Controls.Add(CreateLabel("Oyun adı:", 16, y));
            txtAd = new TextBox { Location = new Point(16, y + 22), Width = 300 };
            pnlSol.Controls.Add(txtAd);
            y += inputSpacing;

            // 2. Alış qiyməti
            pnlSol.Controls.Add(CreateLabel("Alış qiyməti (AZN):", 16, y));
            numQiymet = new NumericUpDown
            {
                Location = new Point(16, y + 22),
                Width = 300,
                DecimalPlaces = 2,
                Maximum = 10000,
                Minimum = 0,
                Increment = 1
            };
            pnlSol.Controls.Add(numQiymet);
            y += 46; // Aralıq kiçildildi çünki checkbox gəlir

            // Ortalama qiymət checkboxu
            chkOrtalamaQiymet = new CheckBox
            {
                Text = "Ortalama qiymət (Təxmini)",
                Location = new Point(16, y),
                AutoSize = true,
                ForeColor = Color.FromArgb(100, 116, 139)
            };
            pnlSol.Controls.Add(chkOrtalamaQiymet);
            y += checkSpacing;

            // 3. Alınma tarixi
            pnlSol.Controls.Add(CreateLabel("Alınma tarixi:", 16, y));
            dtpTarix = new DateTimePicker
            {
                Location = new Point(16, y + 22),
                Width = 300,
                Format = DateTimePickerFormat.Short
            };
            pnlSol.Controls.Add(dtpTarix);
            y += 46;

            // Alınma tarixini bilmirəm Checkbox-u
            chkNamelumTarix = new CheckBox
            {
                Text = "Alınma tarixini bilmirəm",
                Location = new Point(16, y),
                AutoSize = true,
                ForeColor = Color.FromArgb(100, 116, 139)
            };
            chkNamelumTarix.CheckedChanged += (s, e) => { dtpTarix.Enabled = !chkNamelumTarix.Checked; };
            pnlSol.Controls.Add(chkNamelumTarix);
            y += checkSpacing;

            // 4. Oyun satılıb checkbox-u
            chkSatilib = new CheckBox
            {
                Text = "Oyun satılıb",
                Location = new Point(16, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            chkSatilib.CheckedChanged += (s, e) => {
                dtpSatisTarixi.Enabled = chkSatilib.Checked;
                numSatisQiymeti.Enabled = chkSatilib.Checked;
            };
            pnlSol.Controls.Add(chkSatilib);
            y += checkSpacing;

            // 5. Satış qiyməti
            pnlSol.Controls.Add(CreateLabel("Satış qiyməti (AZN):", 16, y));
            numSatisQiymeti = new NumericUpDown
            {
                Location = new Point(16, y + 22),
                Width = 300,
                DecimalPlaces = 2,
                Maximum = 10000,
                Minimum = 0,
                Increment = 1,
                Enabled = false
            };
            pnlSol.Controls.Add(numSatisQiymeti);
            y += inputSpacing;

            // 6. Satış tarixi
            pnlSol.Controls.Add(CreateLabel("Satış tarixi:", 16, y));
            dtpSatisTarixi = new DateTimePicker
            {
                Location = new Point(16, y + 22),
                Width = 300,
                Format = DateTimePickerFormat.Short,
                Enabled = false
            };
            pnlSol.Controls.Add(dtpSatisTarixi);
            y += inputSpacing;

            // 7. Platforma
            pnlSol.Controls.Add(CreateLabel("Platforma:", 16, y));
            cmbPlatforma = new ComboBox
            {
                Location = new Point(16, y + 22),
                Width = 300,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbPlatforma.Items.AddRange(new object[] {
                "PlayStation 4", "PlayStation 3", "PlayStation 2", "PSP"
            });
            cmbPlatforma.SelectedIndex = 0;
            pnlSol.Controls.Add(cmbPlatforma);
            y += inputSpacing;

            // 8. Alınan yer
            pnlSol.Controls.Add(CreateLabel("Alınan yer (Mənbə):", 16, y));
            cmbAlinanYer = new ComboBox
            {
                Location = new Point(16, y + 22),
                Width = 300,
                DropDownStyle = ComboBoxStyle.DropDown
            };
            cmbAlinanYer.Items.AddRange(new object[] {
                "Aga Playstation", "Playroom", "Lalafo", "Tap.az", "Facebook", "Kontakt Home", "Trendyol", "eBay"
            });
            cmbAlinanYer.Text = "Aga Playstation";
            pnlSol.Controls.Add(cmbAlinanYer);
            y += inputSpacing;

            // 9. Status
            pnlSol.Controls.Add(CreateLabel("Status:", 16, y));
            cmbStatus = new ComboBox
            {
                Location = new Point(16, y + 22),
                Width = 300,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbStatus.Items.AddRange(new object[] { "Oynanılır", "Bitirilib", "Bitməyən oyun", "Gözləmədə", "İstək siyahısı" });
            cmbStatus.SelectedIndex = 0;
            pnlSol.Controls.Add(cmbStatus);
            y += 65;

            // Düymələr
            btnElaveEt = CreateButton("Əlavə et", 16, y, 145, 38, Color.FromArgb(37, 99, 235), Color.White);
            btnElaveEt.Click += BtnElaveEt_Click;

            btnYenile = CreateButton("Yadda saxla", 171, y, 145, 38, Color.FromArgb(16, 185, 129), Color.White);
            btnYenile.Enabled = false;
            btnYenile.Click += BtnYenile_Click;

            y += 46;
            btnSil = CreateButton("Seçiləni sil", 16, y, 145, 36, Color.FromArgb(239, 68, 68), Color.White);
            btnSil.Enabled = false;
            btnSil.Click += BtnSil_Click;

            btnTemizle = CreateButton("Formu təmizlə", 171, y, 145, 36, Color.FromArgb(100, 116, 139), Color.White);
            btnTemizle.Click += (s, e) => FormuTemizle();

            pnlSol.Controls.AddRange(new Control[] { lblBasliq, btnElaveEt, btnYenile, btnSil, btnTemizle });

            Panel pnlSag = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16)
            };

            Panel pnlUst = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48
            };

            Label lblAxtar = CreateLabel("Axtarış:", 0, 12);
            txtAxtaris = new TextBox
            {
                Location = new Point(62, 9),
                Width = 250
            };
            txtAxtaris.TextChanged += (s, e) => CedveliYenile();

            btnExport = CreateButton("Yedəklə (Export)", 330, 6, 155, 34, Color.FromArgb(79, 70, 229), Color.White);
            btnExport.Click += BtnExport_Click;

            btnImport = CreateButton("Bərpa et (Import)", 495, 6, 155, 34, Color.FromArgb(217, 119, 6), Color.White);
            btnImport.Click += BtnImport_Click;

            pnlUst.Controls.AddRange(new Control[] { lblAxtar, txtAxtaris, btnExport, btnImport });

            Panel pnlAlt = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 42,
                BackColor = Color.FromArgb(226, 232, 240),
                Padding = new Padding(12, 10, 12, 10)
            };

            lblStatistika = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Text = "Cəmi: 0 oyun  |  Xərc: 0.00 AZN  |  Gəlir: 0.00 AZN"
            };
            pnlAlt.Controls.Add(lblStatistika);

            dgvOyunlar = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false
            };
            dgvOyunlar.SelectionChanged += DgvOyunlar_SelectionChanged;
            dgvOyunlar.ColumnHeaderMouseClick += DgvOyunlar_ColumnHeaderMouseClick;
            dgvOyunlar.CellFormatting += DgvOyunlar_CellFormatting;

            pnlSag.Controls.Add(dgvOyunlar);
            pnlSag.Controls.Add(pnlUst);
            pnlSag.Controls.Add(pnlAlt);

            this.Controls.Add(pnlSag);
            this.Controls.Add(pnlSol);
        }

        private Label CreateLabel(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                ForeColor = Color.FromArgb(71, 85, 105)
            };
        }

        private Button CreateButton(string text, int x, int y, int w, int h, Color bg, Color fg)
        {
            return new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, h),
                BackColor = bg,
                ForeColor = fg,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
        }

        private void BazaniYukle()
        {
            try
            {
                if (File.Exists(_dbFilePath))
                {
                    string json = File.ReadAllText(_dbFilePath, Encoding.UTF8);
                    List<GameItem> data = JsonHelper.DeserializeGames(json);
                    if (data != null)
                        _oyunlar = data;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lokal baza oxunarkən xəta baş verdi:" + Environment.NewLine + ex.Message, "Xəta", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BazaniYaddaSaxla()
        {
            try
            {
                string json = JsonHelper.SerializeGames(_oyunlar);
                File.WriteAllText(_dbFilePath, json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Məlumat yadda saxlanılarkən xəta baş verdi:" + Environment.NewLine + ex.Message, "Xəta", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DgvOyunlar_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            string clickedColumn = dgvOyunlar.Columns[e.ColumnIndex].Name;

            if (_sortColumn == clickedColumn)
            {
                _sortAscending = !_sortAscending;
            }
            else
            {
                _sortColumn = clickedColumn;
                _sortAscending = true;
            }

            CedveliYenile();
        }

        private void DgvOyunlar_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvOyunlar.Columns["Status"] != null)
            {
                int statusColumnIndex = dgvOyunlar.Columns["Status"].Index;
                if (dgvOyunlar.Rows[e.RowIndex].Cells[statusColumnIndex].Value != null)
                {
                    string status = dgvOyunlar.Rows[e.RowIndex].Cells[statusColumnIndex].Value.ToString();
                    if (status == "Bitirilib")
                    {
                        dgvOyunlar.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.FromArgb(209, 250, 229);
                        dgvOyunlar.Rows[e.RowIndex].DefaultCellStyle.ForeColor = Color.FromArgb(6, 95, 70);
                    }
                    else
                    {
                        dgvOyunlar.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.White;
                        dgvOyunlar.Rows[e.RowIndex].DefaultCellStyle.ForeColor = Color.Black;
                    }
                }
            }
        }

        private void CedveliYenile()
        {
            _cedvelYenilenir = true;
            try
            {
                string axtaris = txtAxtaris.Text.Trim().ToLower();

                var filtirlenmis = _oyunlar
                    .Where(o => string.IsNullOrEmpty(axtaris) ||
                                (o.Ad != null && o.Ad.ToLower().Contains(axtaris)) ||
                                (o.Platforma != null && o.Platforma.ToLower().Contains(axtaris)) ||
                                (o.AlinanYer != null && o.AlinanYer.ToLower().Contains(axtaris)) ||
                                (o.Status != null && o.Status.ToLower().Contains(axtaris)));

                IEnumerable<GameItem> siralanmisQuery;

                switch (_sortColumn)
                {
                    case "Ad":
                        siralanmisQuery = _sortAscending ? filtirlenmis.OrderBy(o => o.Ad) : filtirlenmis.OrderByDescending(o => o.Ad);
                        break;
                    case "QiymetGosterisi": // Virtual column for sorting price
                        siralanmisQuery = _sortAscending ? filtirlenmis.OrderBy(o => o.QiymetAzn) : filtirlenmis.OrderByDescending(o => o.QiymetAzn);
                        break;
                    case "AlinmaTarixi":
                        siralanmisQuery = _sortAscending ? filtirlenmis.OrderBy(o => o.AlinmaTarixi ?? DateTime.MinValue) : filtirlenmis.OrderByDescending(o => o.AlinmaTarixi ?? DateTime.MinValue);
                        break;
                    case "SatisQiymetiAzn":
                        siralanmisQuery = _sortAscending ? filtirlenmis.OrderBy(o => o.SatisQiymetiAzn) : filtirlenmis.OrderByDescending(o => o.SatisQiymetiAzn);
                        break;
                    case "Platforma":
                        siralanmisQuery = _sortAscending ? filtirlenmis.OrderBy(o => o.Platforma) : filtirlenmis.OrderByDescending(o => o.Platforma);
                        break;
                    case "AlinanYer":
                        siralanmisQuery = _sortAscending ? filtirlenmis.OrderBy(o => o.AlinanYer) : filtirlenmis.OrderByDescending(o => o.AlinanYer);
                        break;
                    case "Status":
                        siralanmisQuery = _sortAscending ? filtirlenmis.OrderBy(o => o.Status) : filtirlenmis.OrderByDescending(o => o.Status);
                        break;
                    default:
                        siralanmisQuery = filtirlenmis.OrderByDescending(o => o.AlinmaTarixi ?? DateTime.MinValue);
                        break;
                }

                var siralanmis = siralanmisQuery
                    .Select(o => new
                    {
                        o.Id,
                        Ad = o.Ad,
                        QiymetGosterisi = o.QiymetAzn.ToString("0.00") + " AZN" + (o.IsOrtalamaQiymet ? " (Ort.)" : ""), // Təxmini qiymət işarəsi
                        AlinmaTarixi = o.AlinmaTarixi.HasValue ? o.AlinmaTarixi.Value.ToString("dd.MM.yyyy") : "Bilinmir",
                        SatisQiymetiAzn = o.SatisQiymetiAzn > 0 ? o.SatisQiymetiAzn.ToString("0.00") + " AZN" : "-",
                        SatisTarixi = o.SatisTarixi.HasValue ? o.SatisTarixi.Value.ToString("dd.MM.yyyy") : "-",
                        Platforma = o.Platforma,
                        AlinanYer = o.AlinanYer,
                        Status = o.Status
                    })
                    .ToList();

                dgvOyunlar.DataSource = siralanmis;

                if (dgvOyunlar.Columns["Id"] != null)
                    dgvOyunlar.Columns["Id"].Visible = false;
                if (dgvOyunlar.Columns["Ad"] != null)
                    dgvOyunlar.Columns["Ad"].HeaderText = "Oyun adı";
                if (dgvOyunlar.Columns["QiymetGosterisi"] != null)
                    dgvOyunlar.Columns["QiymetGosterisi"].HeaderText = "Alış qiyməti";
                if (dgvOyunlar.Columns["AlinmaTarixi"] != null)
                    dgvOyunlar.Columns["AlinmaTarixi"].HeaderText = "Alınma tarixi";
                if (dgvOyunlar.Columns["SatisQiymetiAzn"] != null)
                    dgvOyunlar.Columns["SatisQiymetiAzn"].HeaderText = "Satış qiyməti";
                if (dgvOyunlar.Columns["SatisTarixi"] != null)
                    dgvOyunlar.Columns["SatisTarixi"].HeaderText = "Satış tarixi";
                if (dgvOyunlar.Columns["Platforma"] != null)
                    dgvOyunlar.Columns["Platforma"].HeaderText = "Platforma";
                if (dgvOyunlar.Columns["AlinanYer"] != null)
                    dgvOyunlar.Columns["AlinanYer"].HeaderText = "Alınan yer";
                if (dgvOyunlar.Columns["Status"] != null)
                    dgvOyunlar.Columns["Status"].HeaderText = "Status";

                decimal umumiXerc = _oyunlar.Sum(o => o.QiymetAzn);
                decimal umumiGelir = _oyunlar.Sum(o => o.SatisQiymetiAzn);
                lblStatistika.Text = string.Format("Cəmi: {0} oyun  |  Xərc: {1:0.00} AZN  |  Gəlir: {2:0.00} AZN",
                    _oyunlar.Count, umumiXerc, umumiGelir);
            }
            finally
            {
                _cedvelYenilenir = false;
                dgvOyunlar.ClearSelection(); // Hər yeniləmədə seçimi təmizləyir
            }
        }

        private void BtnElaveEt_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtAd.Text))
            {
                MessageBox.Show("Zəhmət olmasa oyunun adını daxil edin.", "Xəbərdarlıq", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtAd.Focus();
                return;
            }

            GameItem yeniOyun = new GameItem
            {
                Ad = txtAd.Text.Trim(),
                QiymetAzn = numQiymet.Value,
                IsOrtalamaQiymet = chkOrtalamaQiymet.Checked, // Ortalama qiymət oxunur
                AlinmaTarixi = chkNamelumTarix.Checked ? (DateTime?)null : dtpTarix.Value.Date,
                SatisQiymetiAzn = chkSatilib.Checked ? numSatisQiymeti.Value : 0m,
                SatisTarixi = chkSatilib.Checked ? (DateTime?)dtpSatisTarixi.Value.Date : null,
                Platforma = cmbPlatforma.SelectedItem != null ? cmbPlatforma.SelectedItem.ToString() : "PlayStation 4",
                AlinanYer = !string.IsNullOrWhiteSpace(cmbAlinanYer.Text) ? cmbAlinanYer.Text.Trim() : "Aga Playstation",
                Status = cmbStatus.SelectedItem != null ? cmbStatus.SelectedItem.ToString() : "Oynanılır"
            };

            _oyunlar.Add(yeniOyun);
            BazaniYaddaSaxla();
            CedveliYenile();
            FormuTemizle();
        }

        private void BtnYenile_Click(object sender, EventArgs e)
        {
            if (_secilmisOyunId == null) return;

            GameItem oyun = _oyunlar.FirstOrDefault(o => o.Id == _secilmisOyunId.Value);
            if (oyun != null)
            {
                oyun.Ad = txtAd.Text.Trim();
                oyun.QiymetAzn = numQiymet.Value;
                oyun.IsOrtalamaQiymet = chkOrtalamaQiymet.Checked;
                oyun.AlinmaTarixi = chkNamelumTarix.Checked ? (DateTime?)null : dtpTarix.Value.Date;
                oyun.SatisQiymetiAzn = chkSatilib.Checked ? numSatisQiymeti.Value : 0m;
                oyun.SatisTarixi = chkSatilib.Checked ? (DateTime?)dtpSatisTarixi.Value.Date : null;
                oyun.Platforma = cmbPlatforma.SelectedItem != null ? cmbPlatforma.SelectedItem.ToString() : "PlayStation 4";
                oyun.AlinanYer = !string.IsNullOrWhiteSpace(cmbAlinanYer.Text) ? cmbAlinanYer.Text.Trim() : "Aga Playstation";
                oyun.Status = cmbStatus.SelectedItem != null ? cmbStatus.SelectedItem.ToString() : "Oynanılır";

                BazaniYaddaSaxla();
                CedveliYenile();
                FormuTemizle();
            }
        }

        private void BtnSil_Click(object sender, EventArgs e)
        {
            if (_secilmisOyunId == null) return;

            DialogResult tesdiq = MessageBox.Show("Seçilmiş oyunu siyahıdan silmək istədiyinizə əminsiniz?", "Silmə təsdiqi", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (tesdiq == DialogResult.Yes)
            {
                _oyunlar.RemoveAll(o => o.Id == _secilmisOyunId.Value);
                BazaniYaddaSaxla();
                CedveliYenile();
                FormuTemizle();
            }
        }

        private void DgvOyunlar_SelectionChanged(object sender, EventArgs e)
        {
            if (_cedvelYenilenir) return;

            if (dgvOyunlar.SelectedRows.Count > 0)
            {
                DataGridViewRow row = dgvOyunlar.SelectedRows[0];
                if (row.Cells["Id"] != null && row.Cells["Id"].Value is Guid)
                {
                    Guid id = (Guid)row.Cells["Id"].Value;
                    GameItem oyun = _oyunlar.FirstOrDefault(o => o.Id == id);
                    if (oyun != null)
                    {
                        _secilmisOyunId = oyun.Id;
                        txtAd.Text = oyun.Ad;

                        decimal qiymet = oyun.QiymetAzn;
                        if (qiymet < numQiymet.Minimum) qiymet = numQiymet.Minimum;
                        if (qiymet > numQiymet.Maximum) qiymet = numQiymet.Maximum;
                        numQiymet.Value = qiymet;

                        chkOrtalamaQiymet.Checked = oyun.IsOrtalamaQiymet; // Ortalama qiyməti bərpa edir

                        if (oyun.AlinmaTarixi.HasValue)
                        {
                            chkNamelumTarix.Checked = false;
                            dtpTarix.Value = oyun.AlinmaTarixi.Value;
                        }
                        else
                        {
                            chkNamelumTarix.Checked = true;
                            dtpTarix.Value = DateTime.Today;
                        }

                        if (oyun.SatisQiymetiAzn > 0 || oyun.SatisTarixi.HasValue)
                        {
                            chkSatilib.Checked = true;
                            numSatisQiymeti.Value = oyun.SatisQiymetiAzn;
                            if (oyun.SatisTarixi.HasValue)
                                dtpSatisTarixi.Value = oyun.SatisTarixi.Value;
                        }
                        else
                        {
                            chkSatilib.Checked = false;
                            numSatisQiymeti.Value = 0;
                            dtpSatisTarixi.Value = DateTime.Today;
                        }

                        if (cmbPlatforma.Items.Contains(oyun.Platforma))
                            cmbPlatforma.SelectedItem = oyun.Platforma;
                        else
                            cmbPlatforma.SelectedIndex = 0;

                        cmbAlinanYer.Text = oyun.AlinanYer;
                        cmbStatus.SelectedItem = oyun.Status;

                        btnYenile.Enabled = true;
                        btnSil.Enabled = true;
                    }
                }
            }
        }

        private void FormuTemizle()
        {
            _secilmisOyunId = null;
            txtAd.Clear();
            numQiymet.Value = 0;
            chkOrtalamaQiymet.Checked = false; // Temizləyir
            dtpTarix.Value = DateTime.Today;
            chkNamelumTarix.Checked = false;
            numSatisQiymeti.Value = 0;
            dtpSatisTarixi.Value = DateTime.Today;
            chkSatilib.Checked = false;
            cmbPlatforma.SelectedIndex = 0;
            cmbAlinanYer.Text = "Aga Playstation";
            cmbStatus.SelectedIndex = 0;
            btnYenile.Enabled = false;
            btnSil.Enabled = false;
            dgvOyunlar.ClearSelection();
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Title = "Oyun bazasını yedəklə (Export)";
                sfd.Filter = "JSON Yedək Faylı (*.json)|*.json";
                sfd.FileName = string.Format("oyunlar_yedek_{0:yyyy_MM_dd}.json", DateTime.Now);

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string json = JsonHelper.SerializeGames(_oyunlar);
                        File.WriteAllText(sfd.FileName, json, Encoding.UTF8);
                        MessageBox.Show("Məlumatlar uğurla yedəkləndi!" + Environment.NewLine + "Fayl: " + sfd.FileName, "Uğurlu Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Export zamanı xəta: " + ex.Message, "Xəta", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnImport_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Yedək faylını seçin (Import)";
                ofd.Filter = "JSON Yedək Faylı (*.json)|*.json";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string json = File.ReadAllText(ofd.FileName, Encoding.UTF8);
                        List<GameItem> yuklenenOyunlar = JsonHelper.DeserializeGames(json);

                        if (yuklenenOyunlar == null || yuklenenOyunlar.Count == 0)
                        {
                            MessageBox.Show("Seçilmiş faylda oyun məlumatı tapılmadı.", "Məlumat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        if (_oyunlar.Count > 0)
                        {
                            string mesaj = string.Format("Faylda {0} oyun tapıldı.", yuklenenOyunlar.Count) +
                                Environment.NewLine + Environment.NewLine +
                                "• 'Yes' (Bəli) — Mövcud siyahının üzərinə əlavə et (birləşdir)" + Environment.NewLine +
                                "• 'No' (Xeyr) — Köhnə siyahını sil və yalnız yeni fayldakıları saxla" + Environment.NewLine +
                                "• 'Cancel' — Ləğv et";

                            DialogResult secim = MessageBox.Show(
                                mesaj,
                                "Import rejimi",
                                MessageBoxButtons.YesNoCancel,
                                MessageBoxIcon.Question);

                            if (secim == DialogResult.Cancel) return;

                            if (secim == DialogResult.Yes)
                            {
                                foreach (GameItem item in yuklenenOyunlar)
                                {
                                    if (!_oyunlar.Any(o => o.Id == item.Id))
                                        _oyunlar.Add(item);
                                }
                            }
                            else if (secim == DialogResult.No)
                            {
                                _oyunlar = yuklenenOyunlar;
                            }
                        }
                        else
                        {
                            _oyunlar = yuklenenOyunlar;
                        }

                        BazaniYaddaSaxla();
                        CedveliYenile();
                        FormuTemizle();
                        MessageBox.Show("Məlumatlar uğurla bərpa edildi və lokal bazaya yazıldı!", "Uğurlu Import", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Import zamanı xəta: " + ex.Message, "Xəta", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }

    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}