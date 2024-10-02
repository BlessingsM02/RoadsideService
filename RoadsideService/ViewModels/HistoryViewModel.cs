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
        public ObservableCollection<RequestData> CompletedRequests { get; private set; }
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

        public HistoryViewModel()
        {
            _firebaseClient = new FirebaseClient("https://roadside-service-f65db-default-rtdb.firebaseio.com/");
            CompletedRequests = new ObservableCollection<RequestData>();

            // Initialize the refresh command
            RefreshCommand = new Command(async () => await RefreshCompletedRequestsAsync());
        }

        public async Task LoadCompletedRequestsAsync()
        {
            try
            {
                IsBusy = true;
                var mobileNumber = Preferences.Get("mobile_number", string.Empty);

                OnPropertyChanged(nameof(IsBusy));

                // Fetch all completed requests from Firebase
                var allRequests = await _firebaseClient
                    .Child("complete")
                    .OnceAsync<RequestData>();

                CompletedRequests.Clear(); // Clear previous data
                TotalAmount = 0; // Reset total amount before calculation

                // Filter and add completed requests
                foreach (var request in allRequests)
                {
                    if (request.Object.ServiceProviderId == mobileNumber)
                    {
                        CompletedRequests.Add(request.Object);
                        TotalAmount += request.Object.Amount; // Add to total amount
                    }
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to load completed requests: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(IsBusy));
            }
        }

        private async Task RefreshCompletedRequestsAsync()
        {
            await LoadCompletedRequestsAsync();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
