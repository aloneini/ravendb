﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Expression.Interactivity.Core;
using Raven.Abstractions.Data;
using Raven.Client.Connection.Async;
using Raven.Studio.Features.Documents;
using Raven.Studio.Infrastructure;
using Raven.Studio.Extensions;

namespace Raven.Studio.Models
{
	public class DocumentsPageModel : PageViewModel, IHasPageTitle
	{
	    private const string CollectionsIndex = "Raven/DocumentsByEntityName";
	    private string initialSelectedCollectionName;
		public BindableCollection<CollectionModel> Collections { get; set; }
		public Observable<CollectionModel> SelectedCollection { get; set; }

	    private CollectionDocumentsCollectionSource collectionSource; 
	    private DocumentsModel documentsModel;
	    private DocumentsModel collectionDocumentsModel;
	    private DocumentsModel allDocumentsDocumentsModel;
	    private double collectionsListWidth;
	    private ICommand collapseCollectionsListCommand;
	    public static readonly double CollapsedCollectionsListWidth = 25;
	  //  private const double DefaultCollectionsListWidth = 175;
	    private double maximisedCollectionsListWidth;
	    private ICommand expandCollectionsListCommand;

	    public CollectionViewSource SortedCollectionsList { get; private set; }

		public Observable<string> SelectedCollectionSortingMode { get; set; } 

		private double DefaultCollectionsListWidth
		{
			get { return Settings.Instance.CollectionWidth; }
		}

	    private string GetSelectedCollectionName()
	    {
	        return SelectedCollection.Value != null ? SelectedCollection.Value.Name  : "";
	    }

        public DocumentsModel DocumentsModel
        {
            get { return documentsModel; }
            private set
            {
                if (documentsModel != value)
                {
                    documentsModel = value;
                    OnPropertyChanged(() => DocumentsModel);
                }
            }
        }

	    private void PutCollectionNameInTheUrl()
		{
			var urlParser = new UrlParser(UrlUtil.Url);
			var collection = SelectedCollection.Value;
			if (collection == null)
				return;
			var name = collection.Name;
			initialSelectedCollectionName = name;
			if (urlParser.GetQueryParam("collection") != name)
			{
			    if (name != "")
			    {
			        urlParser.SetQueryParam("collection", name);
			    }
			    else
			    {
			        urlParser.RemoveQueryParam("collection");
			    }

			    UrlUtil.Navigate(urlParser.BuildUrl());
			}
		}

        public DocumentsPageModel()
		{
			ModelUrl = "/documents";
		
			ApplicationModel.Current.Server.Value.RawUrl = null;
            SelectedCollectionSortingMode = new Observable<string> { Value = Settings.Instance.CollectionSortingMode };

            collectionSource = new CollectionDocumentsCollectionSource();
            collectionDocumentsModel = new DocumentsModel(collectionSource)
            {
                DocumentNavigatorFactory = (id, index) =>
                                           DocumentNavigator.Create(id, index, CollectionsIndex,
                                                                    new IndexQuery
                                                                    {
                                                                        Query = "Tag:" + GetSelectedCollectionName()
                                                                    })
            };
            collectionDocumentsModel.SetChangesObservable(d => d.IndexChanges
                     .Where(n => n.Name.Equals(CollectionsIndex, StringComparison.InvariantCulture))
                     .Select(m => Unit.Default));

            allDocumentsDocumentsModel = new DocumentsModel(new DocumentsCollectionSource())
            {
                DocumentNavigatorFactory = (id, index) => DocumentNavigator.Create(id, index),
                Context = "AllDocuments",
            };

            allDocumentsDocumentsModel.SetChangesObservable(d => d.DocumentChanges.Select(s => Unit.Default));

            Collections = new BindableCollection<CollectionModel>(model => model.Name)
            {
                new AllDocumentsCollectionModel()
            };

            SelectedCollection = new Observable<CollectionModel>();
            SelectedCollection.PropertyChanged += (sender, args) =>
            {
                PutCollectionNameInTheUrl();

                var selectedCollectionName = GetSelectedCollectionName();
                if (selectedCollectionName == "")
                {
                    DocumentsModel = allDocumentsDocumentsModel;
                }
                else
                {
                    collectionSource.CollectionName = selectedCollectionName;
                    collectionDocumentsModel.Context = "Collection/" + GetSelectedCollectionName();
                    DocumentsModel = collectionDocumentsModel;
                }
            };

		    SortedCollectionsList = new CollectionViewSource
		    {
		        Source = Collections,
		        SortDescriptions =
		        {
					GetSortDescription()
		        }
		    };

            SelectedCollectionSortingMode.PropertyChanged += (sender, args) =>
			{
			    using (SortedCollectionsList.DeferRefresh())
			    {
			        SortedCollectionsList.SortDescriptions.Clear();
			        SortedCollectionsList.SortDescriptions.Add(GetSortDescription());
			    }

				Settings.Instance.CollectionSortingMode = SelectedCollectionSortingMode.Value;
			};

            CollectionsListWidth = DefaultCollectionsListWidth;
		}

	    private SortDescription GetSortDescription()
	    {
            return SelectedCollectionSortingMode.Value == "Name"
	                   ? new SortDescription("Name", ListSortDirection.Ascending)
	                   : new SortDescription("SortableCount", ListSortDirection.Descending);
	    }

	    public override void LoadModelParameters(string parameters)
		{
			ApplicationModel.Current.Refresh();
			var urlParser = new UrlParser(parameters);
			var name = urlParser.GetQueryParam("collection");
			initialSelectedCollectionName = name;
            RefreshCollectionsList();
		}

		private void RefreshCollectionsList()
		{
			DatabaseCommands.GetTermsCount(CollectionsIndex, "Tag", "", 100)
				.ContinueOnSuccess(collections =>
				                   	{
										var collectionModels = 
                                            new[] { new AllDocumentsCollectionModel() { Count = (int)Database.Value.Statistics.Value.CountOfDocuments}, }.Concat(
                                            collections
											.Where(x=>x.Count > 0)
											.Select(col => new CollectionModel { Name = col.Name, Count = col.Count }))
											.ToArray();

                                        Collections.Match(collectionModels, () => AfterUpdate(collectionModels));
				                   	})
				.Catch(ex =>
				                           	{
				                           		var urlParser = new UrlParser(UrlUtil.Url);
				                           		if (urlParser.RemoveQueryParam("collection"))
				                           			UrlUtil.Navigate(urlParser.BuildUrl());
				                           		ApplicationModel.Current.AddErrorNotification(ex, "Unable to retrieve collections from server.");
				                           	});
		}

		private void AfterUpdate(CollectionModel[] collectionDocumentsCount)
		{
            // update documents count
		    var nameToCount = collectionDocumentsCount.ToDictionary(i => i.Name, i => i.Count);
		    foreach (var collectionModel in Collections)
		    {
		        collectionModel.Count = nameToCount[collectionModel.Name];
		    }

		    initialSelectedCollectionName = initialSelectedCollectionName ?? "";

			if ((SelectedCollection.Value == null || SelectedCollection.Value.Name != initialSelectedCollectionName || Collections.Contains(SelectedCollection.Value) == false))
			{
				SelectedCollection.Value = Collections.FirstOrDefault(x => x.Name == initialSelectedCollectionName);
			}

			if (SelectedCollection.Value == null)
				SelectedCollection.Value = Collections.FirstOrDefault();

            SortedCollectionsList.View.Refresh();
		}

		public string PageTitle
		{
			get
			{
				if (SelectedCollection.Value == null || string.IsNullOrEmpty(SelectedCollection.Value.Name))
					return "Documents";
				return "Collection: " + SelectedCollection.Value.Name;
			}
		}

	    public IList<string> CollectionsSortingModes
	    {
            get { return new[] {"Name", "Count"}; }
	    }

	    protected override void OnViewLoaded()
        {
            var databaseChanged = Database.ObservePropertyChanged()
                .Select(_ => Unit.Default)
                .TakeUntil(Unloaded);

            databaseChanged
                .Do(_ => RefreshCollectionsList())
                .Subscribe(_ => ObserveIndexChanges(databaseChanged));

            ObserveIndexChanges(databaseChanged);
        }

	    private void ObserveIndexChanges(IObservable<Unit> databaseChanged)
	    {
            if (Database.Value != null)
            {
                Database.Value.IndexChanges
                    .Where(n => n.Name.Equals(CollectionsIndex, StringComparison.InvariantCulture))
                    .SampleResponsive(TimeSpan.FromSeconds(2))
                    .TakeUntil(Unloaded.Merge(databaseChanged))
                    .ObserveOnDispatcher()
                    .Subscribe(__ => RefreshCollectionsList());
            }
	    }

        public double CollectionsListWidth
        {
            get { return collectionsListWidth; }
            set
            {
                collectionsListWidth = value;
	            Settings.Instance.CollectionWidth = value;
                OnPropertyChanged(() => CollectionsListWidth);
            }
        }

	    public ICommand CollapseCollectionsList
	    {
	        get
	        {
	            return collapseCollectionsListCommand ??
	                   (collapseCollectionsListCommand = new ActionCommand(HandleCollapseCollectionsList));
	        }
	    }

        public ICommand ExpandCollectionsList
        {
            get
            {
                return expandCollectionsListCommand ??
                       (expandCollectionsListCommand = new ActionCommand(HandleExpandCollectionsList));
            }
        }

	    private void HandleExpandCollectionsList()
	    {
	        CollectionsListWidth = maximisedCollectionsListWidth <= CollapsedCollectionsListWidth
	                                   ? DefaultCollectionsListWidth
	                                   : maximisedCollectionsListWidth;
		    if (Math.Abs(CollectionsListWidth - 0) < 25.5) // it is set to collapse
			    CollectionsListWidth = 175; //Default
	    }

	    private void HandleCollapseCollectionsList()
	    {
	        maximisedCollectionsListWidth = CollectionsListWidth;
	        CollectionsListWidth = CollapsedCollectionsListWidth;
	    }
	}
}