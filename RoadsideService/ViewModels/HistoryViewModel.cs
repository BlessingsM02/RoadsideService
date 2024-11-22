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
        private readonly FirebaseClient _firebaseClient2;
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
            _firebaseClient2 = new FirebaseClient("https://roadside1-1ffd7-default-rtdb.firebaseio.com/");
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
                    if (request.Object.ServiceProviderId == mobileNumber)
                    {
                        // Get the service provider's name using the DriverId (mobile number)
                        var users = await _firebaseClient2
                                                    .Child("users")
                                                    .OnceAsync<Users>();

                        var user = users.FirstOrDefault(v => v.Object.MobileNumber == request.Object.DriverId)?.Object;
                        if (user != null)
                        {
                            // Store the service provider's name in the request data
                            request.Object.ServiceProviderName = user.FullName;
                        }

                        // Add the request to the collection and update the total amount
                        AllRequests.Add(request.Object);
                        TotalAmount += request.Object.Price;
                    }
                }
              

                // Show completed requests by default
                ShowCompletedRequests();
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Something went wrong. Check your internet connection", "OK");
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
