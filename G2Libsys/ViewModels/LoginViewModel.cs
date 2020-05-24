﻿namespace G2Libsys.ViewModels
{
    #region Namespaces
    using System;
    using System.Windows.Input;
    using G2Libsys.Library.Extensions;
    using G2Libsys.Services;
    using G2Libsys.Commands;
    using G2Libsys.Data.Repository;
    using G2Libsys.Library;
    using System.Security;
    #endregion

    /// <summary>
    /// Viewmodel for logging in user
    /// </summary>
    public class LoginViewModel : BaseViewModel, ISubViewModel
    {
        #region Private Fields
        private readonly IUserRepository _repo;
        private string username;
        private SecureString password;
        private SecureString newPassword;
        private string emailValidationMessage;
        private User newUser;
        #endregion

        #region Public Properties

        /// <summary>
        /// User email
        /// </summary>
        public string Username
        {
            get => username;
            set
            {
                username = value;
                OnPropertyChanged(nameof(Username));
            }
        }

        /// <summary>
        /// User password
        /// TODO: Change to secure password and passwordbox
        /// </summary>
        public SecureString Password
        {
            get => password;
            set
            {
                password = value;
                OnPropertyChanged(nameof(Password));
            }
        }

        /// <summary>
        /// Password for new user
        /// </summary>
        public SecureString NewPassword
        {
            get => newPassword;
            set
            {
                newPassword = value;
                OnPropertyChanged(nameof(newPassword));
            }
        }

        /// <summary>
        /// For registring new user
        /// </summary>
        public User NewUser
        {
            get => newUser;
            set
            {
                newUser = value;
                Username = string.Empty;
                Password = null;
                NewPassword = null;
                OnPropertyChanged(nameof(NewUser));
            }
        }

        /// <summary>
        /// Error message for email
        /// </summary>
        public string EmailValidationMessage
        {
            get => emailValidationMessage;
            set
            {
                emailValidationMessage = value;
                OnPropertyChanged(nameof(EmailValidationMessage));
            }
        }

        #endregion

        #region Commands
        /// <summary>
        /// Login user command
        /// </summary>
        public ICommand LogIn { get; set; }

        /// <summary>
        /// Verify if canExecute login command
        /// </summary>
        private Predicate<object> CanLogin =>
            _ => !string.IsNullOrWhiteSpace(Username)
              && Password?.Length > 0;

        /// <summary>
        /// Register new user command
        /// </summary>
        public ICommand Register { get; set; }

        /// <summary>
        /// Verify if canExecute Register command
        /// </summary>
        private Predicate<object> CanRegister =>
            _ => !string.IsNullOrWhiteSpace(NewUser.Firstname)
              && !string.IsNullOrWhiteSpace(NewUser.Lastname)
              && !string.IsNullOrWhiteSpace(NewUser.Email)
              && NewPassword?.Length > 0;

        public ICommand CancelCommand => new RelayCommand(_ => _navigationService.HostScreen.SubViewModel = null);

        #endregion

        #region Constructor
        /// <summary>
        /// Default constructor
        /// </summary>
        public LoginViewModel()
        {
            if (base.IsInDesignMode) return;

            _repo = new UserRepository();

            EmailValidationMessage = string.Empty;
            NewUser = new User();

            // Create commands
            LogIn = new RelayCommand(_ => VerifyLogin(), CanLogin);
            Register = new RelayCommand(_ => VerifyRegister(), CanRegister);
        }
        #endregion

        #region Private Methods

        /// <summary>
        /// Verify user credentials and login
        /// </summary>
        private async void VerifyLogin()
        {
            // Check for user with correct credentials
            var user = await _repo.VerifyLoginAsync(Username, Password.Unsecure());

            if (user is null)
            {
                _dialog.Alert("Fel lösenord", "Försök igen.");
            }
            else
            {
                // Set userstatus to logged in
                user.LoggedIn = true;

                // Update userstatus in db
                await _repo.UpdateAsync(user).ConfigureAwait(false);

                // Set current active user
                _navigationService.HostScreen.CurrentUser = user;

                // On successfull login go to frontpage
                _navigationService.GoToAndReset(new LibraryMainViewModel());

                // Exit LoginViewModel
                CancelCommand.Execute(null);
            }

            // Reset new user
            EmailValidationMessage = string.Empty;
            NewUser = new User();
        }

        /// <summary>
        /// Verify user registering credentials
        /// </summary>
        private async void VerifyRegister()
        {
            if (!NewUser.Email.IsValidEmail())
            {
                // Email is not valid
                EmailValidationMessage = "Ogiltig mailadress";
            }
            else if (await _repo.VerifyEmailAsync(NewUser.Email))
            {
                // Email already exists in database
                EmailValidationMessage = "Mailadressen finns redan";
            }
            else
            {
                EmailValidationMessage = string.Empty;
                // Call
                RegisterUser();
            }
        }

        /// <summary>
        /// Try to register new user
        /// </summary>
        private async void RegisterUser()
        {
            try
            {
                NewUser.Password = NewPassword.Unsecure();

                // Insert new user
                await _repo.AddAsync(NewUser);
                _dialog.Alert("Registrerad", "Logga in med: " + NewUser.Email);
            }
            catch (Exception ex)
            {
                // Insert failed
                _dialog.Alert("Misslyckades", "Kunde inte lägga till användare:\n" + ex.Message);
            }
            finally
            {
                // Reset NewUser
                NewUser = new User();
            }
        }

        #endregion
    }
}
