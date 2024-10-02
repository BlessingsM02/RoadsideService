using RoadsideService.ViewModels;

namespace RoadsideService.Views
{
    public partial class HistoryPage : ContentPage
    {
        private HistoryViewModel _viewModel;

        public HistoryPage()
        {
            InitializeComponent();
            _viewModel = (HistoryViewModel)BindingContext;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadCompletedRequestsAsync();
        }
    }
}
