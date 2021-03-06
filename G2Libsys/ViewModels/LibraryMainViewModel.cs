﻿namespace G2Libsys.ViewModels
{
	#region NameSpaces
	using G2Libsys.Commands;
	using G2Libsys.Data.Repository;
	using G2Libsys.Library;
    using G2Libsys.Models;
    using G2Libsys.Services;
	using System;
	using System.Collections.ObjectModel;
	using System.Linq;
	using System.Runtime.Serialization;
    using System.Threading.Tasks;
    using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Input;
	using System.Windows.Media.TextFormatting;
	#endregion

	public class LibraryMainViewModel : BaseViewModel, IViewModel
	{
		#region Fields
		private readonly IRepository _repo;
		private ObservableCollection<LibraryObject> libObjects;
		private Category selectedCatagory;
		private string searchtext;
		private bool frontPage;
		private bool basicSearch;
		private byte[] _rawImageData;
		private SearchObject searchObject;
		#endregion

		#region Properties

		/// <summary>
		/// This is used when Uploading/downloading image blobs from the server.
		/// </summary>

		public byte[] RawImageData
		{
			get { return _rawImageData; }
			set
			{
				if (value != _rawImageData)
				{
					_rawImageData = value;
					OnPropertyChanged(nameof(RawImageData));
				}
			}
		}

		/// <summary>
		/// We create this object when doing an advanced search query. Its instanse-variables are used for the search method.
		/// </summary>
		

		public SearchObject SearchObject
		{
			get => searchObject;
			set
			{
				searchObject = value;
				OnPropertyChanged(nameof(SearchObject));
			}
		}

		/// <summary>
		/// En lista med Categories
		/// </summary>
		public ObservableCollection<Category> Categories { get; private set; }

		/// <summary>
		/// Selected search category
		/// </summary>
		public Category SelectedCategory
		{
			get => selectedCatagory ?? Categories?.FirstOrDefault();
			set
			{
				selectedCatagory = value;
				OnPropertyChanged(nameof(SelectedCategory));
			}
		}

		/// <summary>
		/// Selected navigation category
		/// </summary>
		public Category NavigateCategory
		{
			// Get libraryobject with selected category id
			set => GetLibraryObjects(value.ID);
		}

		/// <summary>
		/// den andra listan med libobjects, används för att binda i blogposterna
		/// </summary>
		public ObservableCollection<LibraryObject> FpLibraryObjects { get; set; }

		/// <summary>
		/// The list of LibraryObjects
		/// </summary>
		public ObservableCollection<LibraryObject> LibraryObjects
		{
			get => libObjects;
			set
			{
				libObjects = value;
				OnPropertyChanged(nameof(LibraryObjects));
			}
		}
		/// <summary>
		/// Basic search string
		/// </summary>
		public string SearchText
		{
			get => searchtext;
			set
			{
				searchtext = value;
				OnPropertyChanged(nameof(SearchText));
			}
		}

		#endregion

		#region Bools

		/// <summary>
		/// If frontpage enabled
		/// </summary>
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

		/// <summary>
		/// If frontpage disabled
		/// </summary>
		public bool SearchPage => !FrontPage;

		/// <summary>
		/// If Basicsearch enabled
		/// </summary>
		public bool BasicSearch
		{
			get => basicSearch;
			set
			{
				basicSearch = value;
				OnPropertyChanged(nameof(BasicSearch));
				OnPropertyChanged(nameof(AdvSearch));
			}
		}

		public bool HasSearchResult => !(LibraryObjects?.Count > 0);

		/// <summary>
		/// If Advancedsearch enabled
		/// </summary>
		public bool AdvSearch => !BasicSearch;

		#endregion

		#region Commands

		public ICommand AdvancedSearchCommand { get; }
		public ICommand AdvClearCommand { get; }
		public ICommand SearchCommand { get; }
		public ICommand EnableAdvancedSearch { get; }
		public ICommand OpenObjectInfo { get; }

		#endregion

		#region Constructor

		public LibraryMainViewModel()
		{
			if (base.IsInDesignMode) return;
			
			_repo = new GeneralRepository();

			LibraryObjects = new ObservableCollection<LibraryObject>();
			Categories = new ObservableCollection<Category>();

			FrontPage = true;
			BasicSearch = true;

			GetCategories();
			GetFpLibraryObjects();
			ResetSearchObject();
			OpenObjectInfo = new RelayCommand<LibraryObject>(GetObj);
			SearchCommand = new RelayCommand(_ => GetSearchObjects());
			AdvancedSearchCommand = new RelayCommand(_ => GetAdvancedSearchObjects());
			AdvClearCommand = new RelayCommand(_ => ResetSearchObject());
			EnableAdvancedSearch = new RelayCommand(_ => BasicSearch = !BasicSearch);
        }

		#endregion

		#region Private methods
		/// <summary>
		/// get the view for the selected object
		/// </summary>
		/// <param name="param"></param>
		private void GetObj(LibraryObject param)
		{
			_navigationService.HostScreen.SubViewModel = (ISubViewModel)_navigationService.CreateNewInstance(new LibraryObjectInfoViewModel(param));
		}
		/// <summary>
		/// Get library objects with the matching category id
		/// </summary>
		private async void GetLibraryObjects(int id)
		{
			FrontPage = false;
			LibraryObjects = new ObservableCollection<LibraryObject>(await _repo.GetAllAsync<LibraryObject>(id));
			OnPropertyChanged(nameof(HasSearchResult));
		}

		/// <summary>
		/// Get search results
		/// </summary>
		private async void GetSearchObjects()
		{
			FrontPage = false;
			LibraryObjects = new ObservableCollection<LibraryObject>((await _repo.GetRangeAsync<LibraryObject>(SearchText)).Where(o => o.Category == SelectedCategory.ID));
			OnPropertyChanged(nameof(HasSearchResult));
		}

		/// <summary>
		/// Uses the filteredsearch. Takes a SearchObject that contains filtering parameters,
		/// and gets all the Libraryobjects that match these conditions.
		/// For instance: if myBookobject.Title is "Harry", then send myBookobject if you want all books that match that title.
		/// </summary>
		private async void GetAdvancedSearchObjects()
		{
			FrontPage = false;

			LibraryObjects = new ObservableCollection<LibraryObject>(await _repo.GetRangeAsync<LibraryObject>(SearchObject));
			OnPropertyChanged(nameof(HasSearchResult));
		}

		/// <summary>
		/// Initiate or reset SearchObject
		/// </summary>
		private void ResetSearchObject() => SearchObject = new SearchObject() { DateAdded = DateTime.Now.AddDays(-7) };

		/// <summary>
		/// Get category navigation
		/// </summary>
		private async void GetCategories()
		{
			try
			{
				Categories = new ObservableCollection<Category>(await _repo.GetAllAsync<Category>());
				if (Categories?.Count > 0) Categories[0].Name = "Böcker";
			}
			catch
			{
				Categories = new ObservableCollection<Category>() { new Category() { ID = 1, Name = "Saknas" } };
			}
		}

		/// <summary>
		/// Get frontpage content
		/// </summary>
		private async void GetFpLibraryObjects()
		{
			var list = (await _repo.GetAllAsync<LibraryObject>(1)).ToList();
			if (list.Count > 3)
			{
				FpLibraryObjects = new ObservableCollection<LibraryObject>(list.GetRange(0, 2));
			}
		}

        #endregion
    }
}

