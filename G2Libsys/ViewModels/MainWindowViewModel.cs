﻿namespace G2Libsys.ViewModels
{
    #region Namespaces
    using G2Libsys.Commands;
    using G2Libsys.Data.Repository;
    using G2Libsys.Library;
    using G2Libsys.Models;
    using G2Libsys.Services;
    using Microsoft.Extensions.DependencyInjection;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Input;
    #endregion

    /// <summary>
    /// Main ViewModel
    /// </summary>
    public class MainWindowViewModel : BaseViewModel, IHostScreen
    {
        #region Private Fields
        private readonly IRepository _repo;
        private User currentUser;
        private IViewModel currentViewModel;
        private ISubViewModel subViewModel;
        private ICommand goToFrontPage;
        private ICommand logOutCommand;
        private ICommand cancelCommand;
        private ICommand infoButton;
        #endregion

        #region Public Properties

        /// <summary>
        /// Viewmodels for developer menu
        /// </summary>
        public List<UserMenuItem> ViewModelList { get; set; }

        /// <summary>
        /// Quick navigation for devmenu
        /// </summary>
        public UserMenuItem SelectedDevItem {
            set
            {
                value.Action.Execute(null);
                SubViewModel = null;
            }
        }

        /// <summary>
        /// Active user
        /// </summary>
        public User CurrentUser
        {
            get => currentUser;
            set
            {
                currentUser = value;
                OnPropertyChanged(nameof(CurrentUser));
                OnPropertyChanged(nameof(CanLogIn));
                OnPropertyChanged(nameof(IsLoggedIn));

                // Set user viewmodel access
                if (value != null) dispatcher.Invoke(SetUserAccess);
            }
        }

        /// <summary>
        /// User navigation access
        /// </summary>
        public ObservableCollection<UserMenuItem> MenuItems { get; private set; }

        /// <summary>
        /// Sets the active viewmodel
        /// </summary>
        public IViewModel CurrentViewModel
        {
            get => currentViewModel;
            set
            {
                if (value != null)
                {
                    currentViewModel = value;
                    SubViewModel = null;
                    OnPropertyChanged(nameof(CurrentViewModel));
                }
            }
        }

        /// <summary>
        /// Sets the active subviewmodel
        /// </summary>
        public ISubViewModel SubViewModel
        {
            get => subViewModel;
            set
            {
                subViewModel = value;
                OnPropertyChanged(nameof(SubViewModel));
            }
        }

        #region Bools

        /// <summary>
        /// True if user is not already logged in
        /// </summary>
        public bool CanLogIn => CurrentUser == null ? true : !CurrentUser.LoggedIn;

        /// <summary>
        /// Check if user is logged in
        /// </summary>
        public bool IsLoggedIn => CurrentUser == null ? false : CurrentUser.LoggedIn;

        /// <summary>
        /// Check if in developermode
        /// </summary>
        public bool DeveloperMode { get; private set; }

        #endregion

        #endregion

        #region Public Commands

        /// <summary>
        /// Call logout method
        /// </summary>
        public ICommand LogOutCommand => logOutCommand ??= new RelayCommand(_ => LogOut());
        public ICommand InfoButton => infoButton ??= new RelayCommand(_ =>GetInfo());
        /// <summary>
        /// Navigate to frontpage
        /// </summary>
        public ICommand GoToFrontPage => goToFrontPage ??= new RelayCommand(_ =>
        {
            SubViewModel = null;

            if (!(CurrentViewModel is LibraryMainViewModel model))
            {
                _navigationService.HostScreen.CurrentViewModel = _navigationService.GetViewModel(new LibraryMainViewModel());
            }
            else
            {
                var viewModel = model;
                viewModel.FrontPage = true;
            }
        });

        /// <summary>
        /// Reset Subviewmodel
        /// </summary>
        public ICommand CancelCommand => cancelCommand ??= new RelayCommand(_ => SubViewModel = null);

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public MainWindowViewModel()
        {
            if (base.IsInDesignMode) return;

            _repo = new GeneralRepository();

            Initialize();
        }

        #endregion

        #region Initialization
        /// <summary>
        /// Initial setup
        /// </summary>
        private void Initialize()
        {
            // Initialize navservice and set hostscreen to this MainWindowViewModel
            _navigationService.Setup(this);

            // Enable dev menu
            DeveloperMode = false;
            if (DeveloperMode) dispatcher.Invoke(DevelopSetup);

            // Initial viewmodel 
            CurrentViewModel = _navigationService.GetViewModel(new LibraryMainViewModel());

            // Initiate menuitems list
            MenuItems = new ObservableCollection<UserMenuItem>();

            // Aplication closing event handler
            Application.Current.MainWindow.Closing
                += new CancelEventHandler((o, e) =>
                {
                    // Call logout method
                    dispatcher.Invoke(LogOut);
                });
        }

        #endregion

        #region Methods
        /// <summary>
        /// info dialog for all users
        /// </summary>
        private void GetInfo()
        {
            _dialog.Alert("Info", "Allmänna vilkor:\n\u2022Vid lån av bok ska boken lämnas tillbaka inom 14 dagar av utlämnat datum.\n\u2022Vid lån av E-bok eller Film är endast för privat bruk.\nOm dessa vilkor inte hålls ges biblioteket rätten att spärra ditt lånekort och debitera eventuella avgifter.\n\nOm du har frågor kan du alltid kontakta personalen på bilbioteket");
        }
        /// <summary>
        /// Setup user navigation access
        /// </summary>
        /// 
        private void SetUserAccess()
        {
            MenuItems ??= new ObservableCollection<UserMenuItem>();

            if (MenuItems.Count > 0)
            {
                MenuItems.Clear();
            }

            // Create UserMenuItems
            MenuItems.Add(new UserMenuItem(typeof(UserProfileViewModel), "Profil"));
            
            MenuItems.Add(new UserMenuItem(typeof(LoanCheckoutViewModel), "Varukorg"));

            (CurrentUser.UserType switch
            {
                1 => new List<UserMenuItem>() // Admin
                {
                    new UserMenuItem(typeof(AdminViewModel), "Användare"),
                    new UserMenuItem(typeof(LibraryObjectAdministrationViewModel), "Katalog"),
                },
                2 => new List<UserMenuItem>() // Librarian
                {
                    new UserMenuItem(typeof(LibrarianViewModel), "Användare"),
                    new UserMenuItem(typeof(LibraryObjectAdministrationViewModel), "Katalog"),
                },
                3 => new List<UserMenuItem>() // Visitor
                {
                    // Implementera eventuella unika viewmodels för endast användare
                },
                _ => new List<UserMenuItem>() // Other
                {
                    new UserMenuItem(typeof(TestVM), "Saknas")
                }
            }).ForEach(item => MenuItems.Add(item));

            MenuItems.Add(new UserMenuItem("Logga ut", LogOutCommand));
        }

        /// <summary>
        /// Set viewmodels for developer menu
        /// </summary>
        private void DevelopSetup()
        {
            // Fill with needed viewmodels
            ViewModelList = new List<UserMenuItem>
            {
                new UserMenuItem(typeof(AdminViewModel)),
                new UserMenuItem(typeof(LibraryObjectAdministrationViewModel), "ObjectsAdmin"),
                new UserMenuItem(typeof(UserProfileViewModel), "Profile"),
            };
        }

        /// <summary>
        /// Execution logic for user logout
        /// </summary>
        private void LogOut()
        {
            if (CurrentUser is null) return;

            CurrentUser.LoggedIn = false;
            _repo.UpdateAsync(CurrentUser).ConfigureAwait(false);
            CurrentUser = null;
            NavigateToVM.Execute(typeof(LibraryMainViewModel));
        }

        #endregion
    }
}
