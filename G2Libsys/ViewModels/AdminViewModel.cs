﻿namespace G2Libsys.ViewModels
{
    using G2Libsys.Commands;
    using G2Libsys.Data.Repository;
    using G2Libsys.Dialogs;
    using G2Libsys.Library;
    using G2Libsys.Library.Extensions;
    using G2Libsys.Library.Models;
    using G2Libsys.Services;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Win32;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Documents;
    using System.Windows.Input;
    

    public class AdminViewModel : BaseViewModel, IViewModel
    {
        #region Fields
        private readonly IUserRepository _userRepo;
        private readonly IRepository _repo;
        private User newUser;
        private User selectedUser;
        private ObservableCollection<User> users;
        private ObservableCollection<UserType> _userTypes;
        private string searchstring;
        private ICommand goToUser;
        private UserType selectedUserType;
        private string filePath;
        private RemoveItemDialogViewModel RemoveItemDialogVM { get; set; } = new RemoveItemDialogViewModel();
        //public ICommand RunDialogCommand => new AnotherCommandImplementation(ExecuteRunDialog);
        #endregion

        #region Properties

        /// <summary>
        /// filepath for reports
        /// </summary>
        public string FilePath
        {
            get => filePath;
            set
            {
                filePath = value;
                OnPropertyChanged(nameof(FilePath));
            }
        }

        public string SearchString
        {
            get => searchstring;
            set
            {
                searchstring = value;
                OnPropertyChanged(nameof(SearchString));
            }
        }
        /// <summary>
        /// Collection of users
        /// </summary>
        public ObservableCollection<User> Users
        {
            get => users;
            set
            {
                users = value;
                OnPropertyChanged(nameof(Users));
            }
        }

        /// <summary>
        /// Collection of usertypes
        /// </summary>
        public ObservableCollection<UserType> UserTypes
        {
            get => _userTypes;
            set
            {
                _userTypes = value;
                OnPropertyChanged(nameof(UserTypes));
            }
        }

        

        /// <summary>
        /// Store new user
        /// </summary>
        public User NewUser
        {
            get => newUser;
            set
            {
                newUser = value;
                OnPropertyChanged(nameof(NewUser));
            }
        }

        /// <summary>
        /// Selected User
        /// </summary>
        public User SelectedUser
        {
            get => selectedUser;
            set
            {
                selectedUser = value;
                OnPropertyChanged(nameof(SelectedUser));
            }
        }

        public UserType SelectedUserType
        {
            get => selectedUserType;
            set
            {
                selectedUserType = value;
                OnPropertyChanged(nameof(SelectedUserType));
            }
        }

        #endregion

        #region Commands

        public ICommand searchbutton { get; }
        public ICommand cancelsearch { get; }

        /// <summary>
        /// Add new user
        /// </summary>
        public ICommand AddUserCommand { get; }

        /// <summary>
        /// Remove selected user
        /// </summary>
        public ICommand RemoveUserCommand { get; }


        /// <summary>
        /// Command for downloading a csv file with deleted users
        /// </summary>
        public ICommand DownloadUserLogCommand => new RelayCommand(SaveDialogBoxAsync);

        /// <summary>
        /// Go to details for selected user
        /// </summary>
        public ICommand GoToUser => goToUser ??=
            new RelayCommand(_ =>
            {
                _navigationService.HostScreen.SubViewModel = (ISubViewModel)_navigationService.CreateNewInstance(new UserAdministrationViewModel(SelectedUser));
            }, _ => SelectedUser != null);

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public AdminViewModel()
        {
            if (base.IsInDesignMode) return;

            _userRepo = new UserRepository();
            _repo = new GeneralRepository();

            GetUserTypes();
            GetUsers();

            AddUserCommand = new RelayCommand(_ => AddUser());
            RemoveUserCommand = new RelayCommand(_ => RemoveUser());
            searchbutton = new RelayCommand(_ => Search());
            cancelsearch = new RelayCommand(_ => GetUsers());
        }

        #endregion

        #region Methods

        private async void GetUserTypes()
        {
            UserTypes = new ObservableCollection<UserType>(await _repo.GetAllAsync<UserType>());
        }

        private async void Search()
        {
            Users.Clear();
            Users = new ObservableCollection<User>((await _userRepo.GetRangeAsync(SearchString)));
            OnPropertyChanged(nameof(Users));
        }

        /// <summary>
        /// Get users from database
        /// </summary>
        private async void GetUsers()
        {
            NewUser = new User();
            Users = new ObservableCollection<User>(await _userRepo.GetAllAsync());
        }

        /// <summary>
        /// Remove selected user
        /// </summary>
        private async void RemoveUser()
        {
            if (SelectedUser == null) return;
        
            var myVM = new RemoveItemDialogViewModel("Ta bort användare");
            var dialogresult = _dialog.Show(myVM);

            //Skapar en CSV fil med anledning till borttagning av användare
            if (!dialogresult.isSuccess) return;
            AdminAction adminAction = new AdminAction()
            {
                Comment = $"AnvändarID: {SelectedUser.ID} Anledning:  { dialogresult.msg}",
                Actiondate = DateTime.Now,
                ActionType = 1
            };

            bool isSuccess = false;
            try
            {
                await _userRepo.RemoveAsync(SelectedUser.ID);
                Users.Remove(SelectedUser);
                isSuccess = true;
            }
            catch (Exception ex)
            {
                _dialog.Alert("Fel", "Borttagning misslyckades, försök igen");
                Debug.WriteLine(ex.Message);
            }
            finally
            {
                // Reset NewUser
                NewUser = new User();
            }

            if (isSuccess)
            {
                await _repo.AddAsync(adminAction);
            }
        }



        public async void SaveDialogBoxAsync(object param = null) //används till att spara .csv fil 
        {
            

            var adminActions = new List<AdminAction>(await _repo.GetAllAsync<AdminAction>(1));

            // Inställningar för save file dialog box
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.FileName = "LibsysUserLog"; // Default file name
            dlg.DefaultExt = ".csv"; // Default file extension
            dlg.Filter = "Excel documents (.csv)|*.csv"; // Filter files by extension

            // Visa save file dialog box true if user input string
            var _fileService = IoC.ServiceProvider.GetService<IFileService>();
            bool? fileCreated = _fileService.CreateFile("LibsysUserLog");
            if (fileCreated==true)
            {
                bool success = _fileService.ExportCSV(adminActions);
                if (success)
                {
                    _dialog.Alert("", "");
                }
                else
                {
                    _dialog.Alert("Filen används redan", "Stäng filen och försök igen");
                }

            }
            bool? saveresult = dlg.ShowDialog();

            // Process save file dialog box results
            if (saveresult == true)
            {
                if (dlg.FileName.IsFileBusy())
                {
                    _dialog.Alert("Fel!", "Stäng filen först.");
                    return;
                }

                // Create a FileStream with mode CreateNew  
                FileStream stream = new FileStream(dlg.FileName, FileMode.OpenOrCreate);

                stream.SetLength(0);

                // Create a StreamWriter from FileStream  
                using StreamWriter writer = new StreamWriter(stream, Encoding.UTF8);

                writer.WriteLine("ID,Action,Comment,ActionDate");

                foreach (var action in adminActions)
                {
                    writer.WriteLine($"{action.ID},{action.ActionType},{action.Comment},{action.Actiondate}");
                }
            }
        }

        /// <summary>
        /// Create new user
        /// </summary>
        private async void AddUser()
        {
            if (SelectedUserType == null)
            {
                _dialog.Alert("Fel", "Välj användartyp");
                return;
            }

            if (await _userRepo.VerifyEmailAsync(NewUser.Email))
            {
                _dialog.Alert("Fel", "Emailadressen finns redan.");
                return;
            }

            NewUser.UserType = SelectedUserType.ID;

            try
            {
                NewUser.ID = await _userRepo.AddAsync(NewUser);
                Users.Add(NewUser);
            }
            catch (Exception ex)
            {
                _dialog.Alert("Fel", "Kunde inte lägga till ny användare, försök igen");
                Debug.WriteLine(ex.Message);
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
