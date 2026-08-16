using SynergyStrapper.UI.ViewModels.Settings;

namespace SynergyStrapper.UI.Elements.Settings.Pages;

public partial class FeaturesPage
{
    public FeaturesPage()
    {
        DataContext = new FeaturesViewModel();
        InitializeComponent();
    }
}
