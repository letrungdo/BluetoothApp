using System;
using Prism;
using Prism.Navigation;
using Prism.Services;

namespace BluetoothApp.ViewModels
{
    public class TabbedBaseViewModel : BaseViewModel, IActiveAware
    {
        public TabbedBaseViewModel(INavigationService navigationService, IPageDialogService pageDialogService) : base(navigationService, pageDialogService)
        {
        }

        // NOTE: Prism.Forms only sets IsActive, and does not do anything with the event.
        public event EventHandler IsActiveChanged;

        private bool _isActive;
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:WhatsGoo.ViewModels.TabbedChildViewModelBase"/> is active.
        /// </summary>
        /// <value><c>true</c> if is active; otherwise, <c>false</c>.</value>
        public bool IsActive
        {
            get { return _isActive; }
            set { SetProperty(ref _isActive, value, RaiseIsActiveChanged); }
        }

        /// <summary>
        /// Raises the is active changed.
        /// </summary>
        protected virtual void RaiseIsActiveChanged()
        {
            IsActiveChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
