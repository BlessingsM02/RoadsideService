using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Database;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using RoadsideService.Models;

namespace RoadsideService.ViewModels
{
    public class HistoryViewModel : BaseViewModel
    {
        private readonly FirebaseClient _firebaseClient;
        private ObservableCollection<RequestData> _completedRequests;
        private double _totalEarnings;

        private ObservableCollection<RequestData> CompletedRequests
        {
            get => _completedRequests;
            set => SetProperty(ref _completedRequests, value);
        }

        public double TotalEarnings
        {
            get => _totalEarnings;
            set => SetProperty(ref _totalEarnings, value);
        }

        public ICommand RefreshCommand { get; }

        public HistoryViewModel()
        {
            _firebaseClient = new FirebaseClient("https://roadside-service-f65db-default-rtdb.firebaseio.com/");
            CompletedRequests = new ObservableCollection<RequestData>();
            RefreshCommand = new Command(async () => await LoadCompletedRequestsAsync());
        }

        // Fetches completed requests and calculates total earnings
        private async Task LoadCompletedRequestsAsync()
        {
            IsBusy = true;
            try
            {
                var mobileNumber = Preferences.Get("mobile_number", string.Empty);

                var requests = await _firebaseClient
                    .Child("requests")
                    .OnceAsync<RequestData>();

                var completedRequests = requests
                    .Where(r => r.Object.DriverId == mobileNumber && r.Object.Status == "Completed")
                    .Select(r => r.Object)
                    .ToList();

                CompletedRequests.Clear();
                foreach (var request in completedRequests)
                {
                    CompletedRequests.Add(request);
                }

                // Calculate total earnings
                TotalEarnings = CompletedRequests.Sum(r => r.Amount);
            }
            catch (Exception ex)
            {
                // Handle errors, possibly notify the user
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to load requests: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
