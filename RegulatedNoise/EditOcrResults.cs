using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using RegulatedNoise.Core.DomainModel;
using RegulatedNoise.EDDB_Data;
using RegulatedNoise.Enums_and_Utility_Classes;

namespace RegulatedNoise
{
	public partial class EditOcrResults : RNBaseForm
	{
		public override string thisObjectName { get { return "EditOcrResults"; } }

		public string ReturnValue;
		private int lastRow;
		private int currentRow;

		private bool suspendTextChanged = false;
		private readonly IValidator<MarketDataRow> _validator;
		private readonly Commodities _commodities;

		public EditOcrResults(string dataToEdit, IValidator<MarketDataRow> validator, Commodities commodities)
		{
			if (validator == null)
			{
				throw new ArgumentNullException("validator");
			}
			if (commodities == null)
			{
				throw new ArgumentNullException("commodities");
			}
			_validator = validator;
			_commodities = commodities;
			InitializeComponent();

			foreach (DataGridViewColumn column in dgvData.Columns)
			{
				column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
			}

			dgvData.Rows.Clear();

			var rows = dataToEdit.Split(new string[] { "\r\n" }, StringSplitOptions.None);

			foreach (var row in rows)
			{
				bool implausible = !_validator.Validate(MarketDataRow.ReadCsv(row)).Plausible;

				string[] splitted = row.Split(';');

				if (splitted.GetUpperBound(0) == 11)
					dgvData.Rows.Add(splitted[0], splitted[1], splitted[2], splitted[3], splitted[4],
										  splitted[5], splitted[6], splitted[7], splitted[8], splitted[9],
										  splitted[10], splitted[11], implausible.ToString());

				SetRowStyle(dgvData.Rows[dgvData.RowCount - 1], implausible);
			}
		}

		private void SetRowStyle(DataGridViewRow dgvRow, bool implausible)
		{
			try
			{
				Debug.Print("setrowstyle");
				if (implausible)
				{
					dgvRow.DefaultCellStyle.BackColor = Color.LightCoral;
					dgvRow.DefaultCellStyle.SelectionBackColor = Color.LightCoral;
					dgvRow.DefaultCellStyle.SelectionForeColor = SystemColors.HighlightText;
					dgvRow.Visible = true;

				}
				else
				{
					dgvRow.DefaultCellStyle.BackColor = SystemColors.Window;
					dgvRow.DefaultCellStyle.SelectionBackColor = SystemColors.Highlight;
					dgvRow.DefaultCellStyle.SelectionForeColor = SystemColors.HighlightText;
					dgvRow.Visible = (!cbOnlyImplausible.Checked);
				}
			}
			catch (Exception ex)
			{
				Debug.Print("STOP : " + ex.Message);
			}
		}

		private void dgvData_CurrentCellChanged(object sender, EventArgs e)
		{
			StringBuilder SBuilder = new StringBuilder();
			suspendTextChanged = true;

			lastRow = currentRow;
			if (dgvData.CurrentRow != null)
				currentRow = dgvData.CurrentRow.Index;
			else
				currentRow = -1;

			if (lastRow >= 0)
			{
				for (int i = 0; i < 12; i++)
				{
					if (i > 0)
						SBuilder.Append(";");

					SBuilder.Append(dgvData.Rows[lastRow].Cells[i].Value);
				}

				bool implausible = !_validator.Validate(MarketDataRow.ReadCsv(SBuilder.ToString())).Plausible;
				dgvData.Rows[lastRow].Cells[12].Value = implausible.ToString();

				SetRowStyle(dgvData.Rows[lastRow], implausible);
			}

			if (dgvData.CurrentRow != null)
			{
				string rowId = dgvData.CurrentRow.Cells[11].Value.ToString();

				if (pbEditOcrResultsOriginalImage.Image != null)
					pbEditOcrResultsOriginalImage.Image.Dispose();

				if (File.Exists(".//OCR Correction Images//" + rowId + ".png"))
					pbEditOcrResultsOriginalImage.Image = Image.FromFile(".//OCR Correction Images//" + rowId + ".png");

				tbEditOcrResultsCommodityName.Text = dgvData.CurrentRow.Cells[2].Value.ToString();
				tbEditOcrResultsSellPrice.Text = dgvData.CurrentRow.Cells[3].Value.ToString();
				tbEditOcrResultsBuyPrice.Text = dgvData.CurrentRow.Cells[4].Value.ToString();
				tbEditOcrResultsDemand.Text = dgvData.CurrentRow.Cells[5].Value.ToString();
				tbEditOcrResultsDemandLevel.Text = dgvData.CurrentRow.Cells[6].Value.ToString();
				tbEditOcrResultsSupply.Text = dgvData.CurrentRow.Cells[7].Value.ToString();
				tbEditOcrResultsSupplyLevel.Text = dgvData.CurrentRow.Cells[8].Value.ToString();
			}

			suspendTextChanged = false;

		}

		private void tbEditOcrResultTextChanged(object sender, EventArgs e)
		{
			if (suspendTextChanged) return;

			dgvData.CurrentCellChanged -= dgvData_CurrentCellChanged;

			dgvData.CurrentRow.Cells[2].Value = tbEditOcrResultsCommodityName.Text;
			dgvData.CurrentRow.Cells[3].Value = tbEditOcrResultsSellPrice.Text;
			dgvData.CurrentRow.Cells[4].Value = tbEditOcrResultsBuyPrice.Text;
			dgvData.CurrentRow.Cells[5].Value = tbEditOcrResultsDemand.Text;
			dgvData.CurrentRow.Cells[6].Value = tbEditOcrResultsDemandLevel.Text;
			dgvData.CurrentRow.Cells[7].Value = tbEditOcrResultsSupply.Text;
			dgvData.CurrentRow.Cells[8].Value = tbEditOcrResultsSupplyLevel.Text;

			dgvData.CurrentCellChanged += dgvData_CurrentCellChanged;
		}

		private void bEditOcrResultsOK_Click(object sender, EventArgs e)
		{
			StringBuilder SBuilder = new StringBuilder();

			foreach (DataGridViewRow currentRow in dgvData.Rows)
			{
				for (int i = 0; i < 12; i++)
				{
					if (i > 0)
						SBuilder.Append(";");
					SBuilder.Append(currentRow.Cells[i].Value);
				}
				SBuilder.Append("\r\n");
			}

			ReturnValue = SBuilder.ToString();
			DialogResult = DialogResult.OK;
			Close();
		}


		public bool onlyImplausible
		{
			get
			{
				return cbOnlyImplausible.Checked;

			}

			set
			{
				cbOnlyImplausible.Checked = value;
			}
		}

		private void cbOnlyImplausible_CheckedChanged(object sender, EventArgs e)
		{
			int FirstVisible = -1;

			dgvData.CurrentCellChanged -= dgvData_CurrentCellChanged;

			foreach (DataGridViewRow currentRow in dgvData.Rows)
			{
				bool implausible = (((string)(currentRow.Cells[12].Value)) == (string)(true.ToString()));

				if (cbOnlyImplausible.Checked)
				{
					currentRow.Visible = implausible;

					if (FirstVisible < 0 && currentRow.Visible)
						FirstVisible = currentRow.Index;
				}
				else
					currentRow.Visible = true;

				SetRowStyle(currentRow, implausible);
			}

			dgvData.CurrentCellChanged += dgvData_CurrentCellChanged;

			if (dgvData.CurrentRow == null && FirstVisible >= 0)
				dgvData.CurrentCell = dgvData[0, FirstVisible];

		}

		private void cmdWarnLevels_Click(object sender, EventArgs e)
		{
			string commodityName = String.Empty;

			if (dgvData.CurrentRow != null)
				commodityName = dgvData.CurrentRow.Cells[2].Value.ToString();
			using (CommodityView cView = new CommodityView(_commodities, commodityName))
			{
				cView.ShowDialog(this);
				if (cView.DialogResult == DialogResult.OK)
				{
					checkWarnLevels();
					cbOnlyImplausible_CheckedChanged(this, null);
					dgvData.Refresh();
				}
			}
		}

		private void checkWarnLevels()
		{
			StringBuilder SBuilder = new StringBuilder();

			foreach (DataGridViewRow currentRow in dgvData.Rows)
			{
				SBuilder.Clear();

				for (int i = 0; i < 12; i++)
				{
					if (i > 0)
						SBuilder.Append(";");
					SBuilder.Append(currentRow.Cells[i].Value);
				}

				currentRow.Cells[12].Value = !_validator.Validate(MarketDataRow.ReadCsv(SBuilder.ToString())).Plausible;
			}
		}
	}
}
