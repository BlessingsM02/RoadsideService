namespace RoadsideService
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("MzQyNzYzNEAzMjM2MmUzMDJlMzBYS" +
                "jU5dXJxdDJ0aGlZNXZZY3N4dHB2TTNzMDczZDJFQVZDYWV6cEpRZkVRPQ==");

            MainPage = new AppShell();
        }
    }
}
