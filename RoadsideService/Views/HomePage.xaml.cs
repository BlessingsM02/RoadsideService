using RoadsideService.ViewModels;


namespace RoadsideService.Views
{
    public partial class HomePage : ContentPage
    {
        public HomePage()
        {
            InitializeComponent();
            BindingContext = new WorkingViewModel();
        }

        
    }
}
