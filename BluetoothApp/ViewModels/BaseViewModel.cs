using System;
using Xamarin.Forms;
using Prism.Mvvm;
using Prism.Navigation;
using Prism.AppModel;
using Prism.Services;
using System.Threading.Tasks;

namespace BluetoothApp.ViewModels
{
    public class BaseViewModel : BindableBase, INavigationAware, IDestructible, IPageLifecycleAware, IApplicationLifecycleAware
    {
        bool _isBusy = false;
        public bool IsBusy
        {
            get { return _isBusy; }
            set { SetProperty(ref _isBusy, value); }
        }

        string _title = string.Empty;
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        protected INavigationService NavigationService { get; private set; }
        protected IPageDialogService PageDialogService { get; private set; }

        private bool _isRunning;

        public BaseViewModel(INavigationService navigationService, IPageDialogService pageDialogService)
        {
            NavigationService = navigationService;
            PageDialogService = pageDialogService;
        }

        public virtual void OnNavigatedFrom(INavigationParameters parameters)
        {

        }

        public virtual void OnNavigatedTo(INavigationParameters parameters)
        {
            _isRunning = false;
        }

        public virtual void OnNavigatingTo(INavigationParameters parameters)
        {

        }

        public void CanExecutable(bool running, Action execute)
        {
            if (running)
            {
                return;
            }
            execute.Invoke();
        }

        public Task<INavigationResult> NavigateAsync(string pageName, INavigationParameters param = null, bool? useModal = null, bool animated = true)
        {
            var task = new TaskCompletionSource<INavigationResult>();
            // Block multi tap
            if (_isRunning)
                return task.Task;

            _isRunning = true;

            Device.BeginInvokeOnMainThread(async () =>
            {
                var result = await NavigationService.NavigateAsync(pageName, param, useModal, animated);
                if (!result.Success)
                {
                    _isRunning = false;
                }
                task.SetResult(result);
            });
            return task.Task;
        }

        public Task<INavigationResult> GoBackAsync(INavigationParameters param = null, bool? useModal = null, bool anim = true)
        {
            var task = new TaskCompletionSource<INavigationResult>();
            Device.BeginInvokeOnMainThread(async () =>
            {
                var result = await NavigationService.GoBackAsync(param, useModal, anim);
                task.SetResult(result);
            });
            return task.Task;
        }

        public virtual void Destroy()
        {

        }

        public virtual void OnAppearing()
        {
        }

        public virtual void OnDisappearing()
        {
        }

        public virtual void OnResume()
        {
        }

        public virtual void OnSleep()
        {
        }
    }
}
