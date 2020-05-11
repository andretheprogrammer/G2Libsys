﻿using G2Libsys.Commands;
using G2Libsys.Data.Repository;
using G2Libsys.Library;
using G2Libsys.Library.Models;
using G2Libsys.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace G2Libsys.ViewModels
{
    public class LibraryMainViewModel : BaseViewModel, IViewModel
    {
        public ICommand BookButton { get; private set; }
        private readonly IRepository _repo;

        private bool frontPage;
        private ObservableCollection<LibraryObject> fpLibObjects;
        private ObservableCollection<LibraryObject> libObjects;

        public ObservableCollection<LibraryObject> FpLibraryObjects
        {
            get => fpLibObjects;
            set
            {
                fpLibObjects = value;
                OnPropertyChanged(nameof(FpLibraryObjects));
            }
        }
        public ObservableCollection<LibraryObject> LibraryObjects
        {
            get => libObjects;
            set
            {
                libObjects = value;
                OnPropertyChanged(nameof(LibraryObjects));
            }
        }
        public bool FrontPage
        {
            get => frontPage;
            set
            {
                frontPage = value;
                OnPropertyChanged(nameof(FrontPage));
                OnPropertyChanged(nameof(SearchPage));
            }
        }

        public bool SearchPage => !FrontPage;


        public ICommand SearchCommand => new RelayCommand(_ => Search());

        private void Search()
        {
            if (FrontPage)
                FrontPage = false;
            else
                FrontPage = true;
        }

        public LibraryMainViewModel()
        {
            if (base.IsInDesignMode) return;

            FrontPage = true;
            LibraryObjects = new ObservableCollection<LibraryObject>();
            FpLibraryObjects = new ObservableCollection<LibraryObject>();
            _repo = new GeneralRepository();
            GetLibraryObjects();
            GetFpLibraryObjects();
            BookButton = new RelayCommand(x => BookButtonClick());
            
        }

        public LibraryObject SelectedLibraryObject
        {
            set => NavService.HostScreen.SubViewModel = NavService.CreateNewInstance(new LibraryObjectInfoViewModel(value));
        }
        public void BookButtonClick()
        {
            MessageBox.Show("test");
        }
        /// <summary>
        /// hämtar alla library objects ifrån databasen
        /// </summary>
        private async void GetLibraryObjects()
        {
            //LibraryObjects = new ObservableCollection<LibraryObject>(await _repo.GetAllAsync<LibraryObject>());
        }
        private async void GetFpLibraryObjects()
        {
            var list = (await _repo.GetAllAsync<LibraryObject>()).Where(x => x.Category == 1).ToList();
            FpLibraryObjects = new ObservableCollection<LibraryObject>(list.GetRange(0,4));
        }
    }
}
