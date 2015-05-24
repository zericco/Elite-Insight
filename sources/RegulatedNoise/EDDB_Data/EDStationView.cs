using System;
using System.Linq;
using RegulatedNoise.Enums_and_Utility_Classes;

namespace RegulatedNoise.EDDB_Data
{
    public partial class EDStationView : RNBaseForm
    {
        private EDStation m_Station;

        internal EDStationView()
        {
            InitializeComponent();
        }

        internal EDStationView(EDStation Station)
        {

            InitializeComponent();

            m_Station = Station;

            showData();

            this.Show();
        }

        internal void showData()
        {
            for (int i = 0; i < m_Station.GetType().GetProperties().Count(); i++)
            {
                String Name = m_Station.GetType().GetProperties()[i].Name;
                String PropType = m_Station.GetType().GetProperties()[i].PropertyType.UnderlyingSystemType.Name;
                
            }

        }
    }
}
