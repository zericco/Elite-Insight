using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace RegulatedNoise.Enums_and_Utility_Classes
{
	public partial class RNBaseForm : Form
	{
		private bool m_LoadingDone = false;

		public RNBaseForm()
		{
			InitializeComponent();
		}

		protected void LoadWindowPosition()
		{

			if (this.IsDesignMode() || ApplicationContext.RegulatedNoiseSettings == null)
				return;

			string classname = this.GetType().Name;
			WindowData formPosition;

			if (ApplicationContext.RegulatedNoiseSettings.WindowBaseData.TryGetValue(classname, out formPosition))
			{

				if (formPosition.Position.Height > -1)
				{
					Top = formPosition.Position.Top;
					Left = formPosition.Position.Left;
					Height = formPosition.Position.Height;
					Width = formPosition.Position.Width;
					WindowState = formPosition.State;
				}
				else
				{
					formPosition.Position.Y = Top;
					formPosition.Position.X = Left;
					formPosition.Position.Height = Height;
					formPosition.Position.Width = Width;
					formPosition.State = WindowState;
				}

			}
			else
			{
				ApplicationContext.RegulatedNoiseSettings.WindowBaseData.Add(classname, new WindowData());
				LoadWindowPosition();
				//MessageBox.Show("Not positioninfo for <" + Classname + "> found !");
			}
			m_LoadingDone = true;
		}

		protected void SaveWindowPosition()
		{
			bool changed = false;

			string classname = GetType().Name;
			WindowData formPosition;

			if (ApplicationContext.RegulatedNoiseSettings.WindowBaseData.TryGetValue(classname, out formPosition))
			{
				if (WindowState != FormWindowState.Minimized)
					if (formPosition.State != this.WindowState)
					{
						formPosition.State = this.WindowState;
						changed = true;
					}

				if (this.WindowState == FormWindowState.Normal)
				{
					if ((formPosition.Position.Y != this.Top) ||
						 (formPosition.Position.X != this.Left) ||
						 (formPosition.Position.Height != this.Height) ||
						 (formPosition.Position.Width != this.Width))
					{
						formPosition.Position.Y = this.Top;
						formPosition.Position.X = this.Left;
						formPosition.Position.Height = this.Height;
						formPosition.Position.Width = this.Width;

						changed = true;
					}
				}

				if (changed)
				{
					//SaveSettings();
				}
			}
		}

		protected void Form_Resize(object sender, EventArgs e)
		{
			if (m_LoadingDone)
                try
                {
                    SaveWindowPosition();
                }
                catch (Exception ex)
                {
                    Trace.TraceError("unable to save window position " + ex);
                }
        }

		protected void Form_ResizeEnd(object sender, EventArgs e)
		{
			if (m_LoadingDone)
                try
                {
                    SaveWindowPosition();
                }
                catch (Exception ex)
                {
                    Trace.TraceError("unable to save window position " + ex);
                }
		}

		private void Form_Shown(object sender, EventArgs e)
		{
            try
            {
                LoadWindowPosition();
            }
            catch (Exception ex)
            {
                Trace.TraceError("unable to load window position " + ex);
            }
		}

	}
}
