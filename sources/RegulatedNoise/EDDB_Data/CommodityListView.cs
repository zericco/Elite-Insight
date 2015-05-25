using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Elite.Insight.Core.DomainModel;
using RegulatedNoise.Enums_and_Utility_Classes;

namespace RegulatedNoise.EDDB_Data
{
	public partial class CommodityListView : RNBaseForm
	{
		private readonly Commodities _commodities;

		private Boolean _dataChanged = false;
		private string _oldValue;
		private readonly List<Commodity> _dataSource;

		public CommodityListView(Commodities commodities, string selectedCommodity = "")
		{
			if (commodities == null)
			{
				throw new ArgumentNullException("commodities");
			}
			_commodities = commodities;
			InitializeComponent();
			_dataSource = commodities.Select(x => new Commodity(x)).OrderBy(x => x.Name).ToList();
			dgvWarnlevels.DataSource = _dataSource;

			//foreach (Commodity commodity in _commodities)
			//{
			//	dgvWarnlevels.Rows.Add(commodity.Name, commodity.Category, commodity.AveragePrice,
			//								  commodity.DemandWarningLevels.Sell.Low, commodity.DemandWarningLevels.Sell.High,
			//								  commodity.DemandWarningLevels.Buy.Low, commodity.DemandWarningLevels.Buy.High,
			//								  commodity.SupplyWarningLevels.Sell.Low, commodity.SupplyWarningLevels.Sell.High,
			//								  commodity.SupplyWarningLevels.Sell.Low, commodity.SupplyWarningLevels.Sell.High);
			//}

			dgvWarnlevels.Columns[3].HeaderCell.Style.ForeColor = Color.DarkGreen;
			dgvWarnlevels.Columns[4].HeaderCell.Style.ForeColor = Color.DarkGreen;
			dgvWarnlevels.Columns[5].HeaderCell.Style.ForeColor = Color.DarkGreen;
			dgvWarnlevels.Columns[6].HeaderCell.Style.ForeColor = Color.DarkGreen;

			dgvWarnlevels.Columns[7].HeaderCell.Style.ForeColor = Color.DarkGoldenrod;
			dgvWarnlevels.Columns[8].HeaderCell.Style.ForeColor = Color.DarkGoldenrod;
			dgvWarnlevels.Columns[9].HeaderCell.Style.ForeColor = Color.DarkGoldenrod;
			dgvWarnlevels.Columns[10].HeaderCell.Style.ForeColor = Color.DarkGoldenrod;

			int selectedRow = 0;
			if (!string.IsNullOrEmpty(selectedCommodity))
			{
				string baseName = commodities.GetBasename(selectedCommodity);
				if (string.IsNullOrEmpty(baseName))
				{
					baseName = selectedCommodity;
				}
				for (; selectedRow < _dataSource.Count; ++selectedRow)
				{
					if (_dataSource[selectedRow].Name == baseName)
					{
						break;
					}
				}
				selectedRow = selectedRow >= _dataSource.Count ? selectedRow : 0;
			}
			dgvWarnlevels.CurrentCell = dgvWarnlevels.Rows[selectedRow].Cells[3];
		}

		private void dataGridView1_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
		{
			_oldValue = dgvWarnlevels[e.ColumnIndex, e.RowIndex].Value.ToString();
		}

		private void dataGridView1_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
		{
			if ((_oldValue != null) && (!_oldValue.Equals(e.FormattedValue)))
				_dataChanged = true;

			_oldValue = null;
		}

		private void dataGridView1_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
		{
			if (e.ColumnIndex >= 4 && e.RowIndex >= 0)
				if (int.Parse(e.FormattedValue.ToString()) <= 0)
					e.CellStyle.BackColor = Color.LightCoral;
		}

		private void cmdOk_Click(object sender, EventArgs e)
		{
			if (_dataChanged)
			{
				if (MessageBox.Show("Save Changed Data ?", "Commodity Data Changed", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
				{
					// save and change
					foreach (var commodity in _dataSource)
					{
						_commodities.Update(commodity);
					}
					this.Close();
				}
				else
				{
					this.DialogResult = DialogResult.None;
				}
			}
			else
				this.Close();
		}

		private void cmdCancel_Click(object sender, EventArgs e)
		{
			if (_dataChanged)
			{
				if (MessageBox.Show("Dismiss Changed Data ?", "Commodity Data Changed", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
				{
					this.Close();
				}
				else
				{
					this.DialogResult = DialogResult.None;
				}
			}
			else
				this.Close();
		}

		//private void UpdateCommodityList()
		//{
		//	try
		//	{
		//		foreach (DataGridViewRow commodity in dgvWarnlevels.Rows)
		//		{
		//			var currentCommodity = _commodities.Find(x => x.Name == commodity.Cells["Name"].Value.ToString());
		//			currentCommodity.DemandWarningLevels.Sell.Low = (int)commodity.Cells[3].Value;
		//			currentCommodity.DemandWarningLevels.Sell.High = (int)commodity.Cells[4].Value;
		//			currentCommodity.DemandWarningLevels.Buy.Low = (int)commodity.Cells[5].Value;
		//			currentCommodity.DemandWarningLevels.Buy.High = (int)commodity.Cells[6].Value;
		//			currentCommodity.SupplyWarningLevels.Sell.Low = (int)commodity.Cells[7].Value;
		//			currentCommodity.SupplyWarningLevels.Sell.High = (int)commodity.Cells[8].Value;
		//			currentCommodity.SupplyWarningLevels.Buy.Low = (int)commodity.Cells[9].Value;
		//			currentCommodity.SupplyWarningLevels.Buy.High = (int)commodity.Cells[10].Value;
		//		}
		//	}
		//	catch (Exception ex)
		//	{
		//		Debug.Print("STOP : " + ex.Message);
		//	}
		//}
	}
}
