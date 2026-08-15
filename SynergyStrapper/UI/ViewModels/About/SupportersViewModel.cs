using System.Windows;

namespace SynergyStrapper.UI.ViewModels.About
{
    public class SupportersViewModel : NotifyPropertyChangedViewModel
    {
        public SupporterData? SupporterData { get; private set; }
        
        public GenericTriState LoadedState { get; set; } = GenericTriState.Unknown;

        public string LoadError { get; set; } = "";

        public int Columns { get; set; } = 3;

        public SizeChangedEventHandler? WindowResizeEvent;

        public SupportersViewModel()
        {
            WindowResizeEvent += OnWindowResize;

            // this will cause momentary freezes only when ran under the debugger
            LoadSupporterData();
        }

        private void OnWindowResize(object sender, SizeChangedEventArgs e)
        {
            if (!e.WidthChanged)
                return;

            int newCols = (int)Math.Floor(e.NewSize.Width / 200);

            if (Columns == newCols)
                return;
             
            Columns = newCols;
            OnPropertyChanged(nameof(Columns));
        }

        public void LoadSupporterData()
        {
            // Synergy Strapper does not ship a remote supporter feed yet.
            // Keep the page functional and avoid contacting an upstream service.
            SupporterData = new SupporterData();
            LoadedState = GenericTriState.Successful;
            OnPropertyChanged(nameof(SupporterData));
            OnPropertyChanged(nameof(LoadedState));
        }
    }
}
