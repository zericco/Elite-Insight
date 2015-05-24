using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using RegulatedNoise.Core.DomainModel;
using RegulatedNoise.Enums_and_Utility_Classes;

namespace RegulatedNoise.EDDB_Data
{
	public partial class CommodityView : RNBaseForm
	{
		private readonly Commodities _commodities;

		private Boolean m_DataChanged = false;
		private string m_OldValue;
		private Boolean m_NoRefresh = false;
		private readonly List<Commodity> _dataSource;

		public CommodityView(Commodities commodities, string selectedCommodity = "")
		{
			if (commodities == null)
			{
				throw new ArgumentNullException("commodities");
			}
			_commodities = commodities;
			InitializeComponent();
			cmdCommodity.Sorted = false;
			_dataSource = commodities.Select(c => new Commodity(c)).OrderBy(c => c.Name).ToList();
			cmdCommodity.DataSource = _dataSource;
			cmdCommodity.ValueMember = "Name";
			cmdCommodity.DisplayMember = "Name";

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
				cmdCommodity.SelectedIndex = selectedRow >= _dataSource.Count ? selectedRow : 0;
				if (cmdCommodity.SelectedIndex == 0)
				{
					cmdCommodity_SelectedIndexChanged(this, new EventArgs());
				}
			}
		}

		private void cmdCommodity_SelectedIndexChanged(object sender, EventArgs e)
		{

			m_NoRefresh = true;

			Commodity currentCommodity = CurrentCommodity;
			if (currentCommodity == null)
				return;
			
			txtCategory.Text = currentCommodity.Category;
			txtAveragePrice.Text = currentCommodity.AveragePrice.ToString();
			txtDemandSellLow.Text = currentCommodity.DemandWarningLevels.Sell.Low.ToString();
			txtDemandSellHigh.Text = currentCommodity.DemandWarningLevels.Sell.High.ToString();
			txtDemandBuyLow.Text = currentCommodity.DemandWarningLevels.Buy.Low.ToString();
			txtDemandBuyHigh.Text = currentCommodity.DemandWarningLevels.Buy.High.ToString();

			txtSupplySellLow.Text = currentCommodity.SupplyWarningLevels.Sell.Low.ToString();
			txtSupplySellHigh.Text = currentCommodity.SupplyWarningLevels.Sell.High.ToString();
			txtSupplyBuyLow.Text = currentCommodity.SupplyWarningLevels.Buy.Low.ToString();
			txtSupplyBuyHigh.Text = currentCommodity.SupplyWarningLevels.Buy.High.ToString();

			m_NoRefresh = false;
		}

		private Commodity CurrentCommodity
		{
			get
			{
				var commodity = ((Commodity) cmdCommodity.SelectedItem);
				if (commodity == null)
				{
					Trace.TraceWarning("no commodity selected");
				}
				return commodity;
			}
		}

		private void txtField_TextChanged(object sender, EventArgs e)
		{
			if (!m_NoRefresh)
			{
				int intValue;
				TextBox currentTextBox = ((TextBox)sender);

				if (int.TryParse(currentTextBox.Text, out intValue))
				{
					m_DataChanged = true;

					Commodity currentCommodity = CurrentCommodity;
					if (currentCommodity == null)
						return;

					currentCommodity.DemandWarningLevels.Sell.Low = int.Parse(txtDemandSellLow.Text);
					currentCommodity.DemandWarningLevels.Sell.High = int.Parse(txtDemandSellHigh.Text);
					currentCommodity.DemandWarningLevels.Buy.Low = int.Parse(txtDemandBuyLow.Text);
					currentCommodity.DemandWarningLevels.Buy.High = int.Parse(txtDemandBuyHigh.Text);

					currentCommodity.SupplyWarningLevels.Sell.Low = int.Parse(txtSupplySellLow.Text);
					currentCommodity.SupplyWarningLevels.Sell.High = int.Parse(txtSupplySellHigh.Text);
					currentCommodity.SupplyWarningLevels.Buy.Low = int.Parse(txtSupplyBuyLow.Text);
					currentCommodity.SupplyWarningLevels.Buy.High = int.Parse(txtSupplyBuyHigh.Text);
				}
				else
				{
					currentTextBox.Text = m_OldValue;
				}
			}
		}

		private void txtField_GotFocus(object sender, EventArgs e)
		{
			m_OldValue = ((TextBox)sender).Text;
		}

		private void cmdOk_Click(object sender, EventArgs e)
		{
			if (m_DataChanged)
			{
				if (MessageBox.Show("Save Changed Data ?", "Commodity Data Changed", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
				{
					// save and change
					foreach (var commodity in _dataSource)
					{
						_commodities.Update(commodity);
					} 
					_commodities.Save(@"./Data/commodities_RN.json", true);
					Close();
				}
				else
				{
					DialogResult = DialogResult.None;
				}
			}
			else
				Close();
		}

		private void cmdCancel_Click(object sender, EventArgs e)
		{
			if (m_DataChanged)
			{
				if (MessageBox.Show("Dismiss Changed Data ?", "Commodity Data Changed", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
				{
					Close();
				}
				else
				{
					DialogResult = DialogResult.None;
				}
			}
			else
				this.Close();
		}

		private void cmdFullList_Click(object sender, EventArgs e)
		{
			CommodityListView view = new CommodityListView(_commodities, cmdCommodity.Text);
			Visible = false;
			view.ShowDialog(this);
			Close();
		}

	}
}
