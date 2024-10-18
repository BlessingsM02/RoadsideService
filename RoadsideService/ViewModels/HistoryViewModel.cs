using Firebase.Database;
using RoadsideService.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace RoadsideService.ViewModels
{
    internal class HistoryViewModel : INotifyPropertyChanged
    {
        private readonly FirebaseClient _firebaseClient;
        public ObservableCollection<RequestData> AllRequests { get; private set; }
        public ObservableCollection<RequestData> FilteredRequests { get; private set; }
        public bool IsBusy { get; private set; }

        private double _totalAmount; // Field to hold the total amount
        public double TotalAmount
        {
            get => _totalAmount;
            private set
            {
                _totalAmount = value;
                OnPropertyChanged(nameof(TotalAmount));
            }
        }

        public ICommand RefreshCommand { get; private set; }
        public ICommand CompletedCommand { get; private set; }
        public ICommand CanceledCommand { get; private set; }

        public HistoryViewModel()
        {
            _firebaseClient = new FirebaseClient("https://roadside-service-f65db-default-rtdb.firebaseio.com/");
            AllRequests = new ObservableCollection<RequestData>();
            FilteredRequests = new ObservableCollection<RequestData>();

            // Initialize commands
            RefreshCommand = new Command(async () => await LoadRequestsAsync());
            CompletedCommand = new Command(ShowCompletedRequests);
            CanceledCommand = new Command(ShowCanceledRequests);
        }

        public async Task LoadRequestsAsync()
        {
            try
            {
                IsBusy = true;
                var mobileNumber = Preferences.Get("mobile_number", string.Empty);

                OnPropertyChanged(nameof(IsBusy));

                // Fetch all requests from Firebase
                var allRequests = await _firebaseClient.Child("complete").OnceAsync<RequestData>();

                AllRequests.Clear(); // Clear previous data
                TotalAmount = 0; // Reset total amount before calculation

                // Add all requests to the collection
                foreach (var request in allRequests)
                {
                    AllRequests.Add(request.Object);
                    TotalAmount += request.Object.Price; // Add to total amount
                }

                // Show completed requests by default
                ShowCompletedRequests();
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to load requests: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(IsBusy));
            }
        }

        private void ShowCompletedRequests()
        {
            FilteredRequests.Clear();
            foreach (var request in AllRequests.Where(r => r.Status == "Completed")) // Change "Completed" to your actual status
            {
                FilteredRequests.Add(request);
            }
        }

        private void ShowCanceledRequests()
        {
            FilteredRequests.Clear();
            foreach (var request in AllRequests.Where(r => r.Status == "Canceled")) // Change "Canceled" to your actual status
            {
                FilteredRequests.Add(request);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
