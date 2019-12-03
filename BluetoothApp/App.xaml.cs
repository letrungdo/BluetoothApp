using Xamarin.Forms;
using BluetoothApp.Services;
using BluetoothApp.Views;
using Prism;
using Prism.Ioc;
using Prism.Plugin.Popups;
using BluetoothApp.ViewModels;

namespace BluetoothApp
{
    public partial class App
    {
        /* 
         * The Xamarin Forms XAML Previewer in Visual Studio uses System.Activator.CreateInstance.
         * This imposes a limitation in which the App class must have a default constructor. 
         * App(IPlatformInitializer initializer = null) cannot be handled by the Activator.
         */
        public App() : this(null) { }

        public App(IPlatformInitializer initializer) : base(initializer) { }

        protected override void OnInitialized()
        {
            InitializeComponent();
            NavigationService.NavigateAsync($"{nameof(AppShell)}");
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // Register for Popup page
            containerRegistry.RegisterPopupNavigationService();

            // Popups

            // Pages
            containerRegistry.RegisterForNavigation<NavigationPage>();
            containerRegistry.RegisterForNavigation<AppShell>();
            containerRegistry.RegisterForNavigation<AboutPage, AboutViewModel>();
            containerRegistry.RegisterForNavigation<HomePage, HomeViewModel>();

            // Interface
            containerRegistry.Register(typeof(MockDataStore));
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}
