using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SynergyStrapper.UI.ViewModels.Settings;

namespace SynergyStrapper.UI.Elements.Settings.Pages
{
    /// <summary>
    /// Interaction logic for SynergyStrapperPage.xaml
    /// </summary>
    public partial class SynergyStrapperPage
    {
        public SynergyStrapperPage()
        {
            DataContext = new SynergyStrapperViewModel();
            InitializeComponent();
        }
    }
}
