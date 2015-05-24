using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RegulatedNoise
{
	public partial class SplashScreenForm : Form, ISplashScreen
	{
		public SplashScreenForm()
		{
			InitializeComponent();
			listBox1.Items.Add("");
		}

		public void InfoAdd(string info)
		{
			listBox1.Items.Add(info);
			listBox1.SelectedIndex = listBox1.Items.Count - 1;
		}

		public void InfoChange(string info)
		{
			if (listBox1.SelectedIndex >= 0)
			{
				listBox1.Items[listBox1.SelectedIndex] = info;
			}
		}

		public void Close(TimeSpan delay)
		{
			//new TaskFactory(TaskScheduler.Default).
			Task.Delay(delay).ContinueWith(task => Close(), TaskScheduler.FromCurrentSynchronizationContext());
		}

		public void SetPosition(WindowData windowData)
		{
			if ((windowData != null) && (windowData.Position.Top >= 0))
			{
				Rectangle rec_WA = Screen.FromRectangle(windowData.Position).WorkingArea;
				Location = new Point((Int32)(rec_WA.X + ((rec_WA.Width - this.Width) / 2)), (Int32)(rec_WA.Y + ((rec_WA.Height - this.Height) / 2)));
			}
		}
	}
}
