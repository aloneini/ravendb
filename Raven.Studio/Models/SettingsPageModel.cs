﻿using System.Collections.Generic;
using System.Windows.Input;
using Raven.Abstractions.Data;
using Raven.Json.Linq;
using Raven.Studio.Commands;
using Raven.Studio.Features.Settings;
using Raven.Studio.Infrastructure;
using System.Linq;
using Raven.Client.Connection;

namespace Raven.Studio.Models
{
    public class SettingsPageModel : PageViewModel
    {
        public SettingsPageModel()
        {
            Settings = new SettingsModel();
        }

        public string CurrentDatabase
        {
            get { return ApplicationModel.Current.Server.Value.SelectedDatabase.Value.Name; }
        }

	    protected override async void OnViewLoaded()
	    {
		    var databaseName = ApplicationModel.Current.Server.Value.SelectedDatabase.Value.Name;

		    if (databaseName == Constants.SystemDatabase)
		    {
			    var apiKeys = new ApiKeysSectionModel();
			    Settings.Sections.Add(apiKeys);
			    Settings.SelectedSection.Value = apiKeys;
			    Settings.Sections.Add(new WindowsAuthSettingsSectionModel());

			    return;
		    }

		    var debug = await ApplicationModel.DatabaseCommands.CreateRequest("/debug/config".NoCache(), "GET").ReadResponseJsonAsync();
		    var bundles = ApplicationModel.CreateSerializer()
		                                  .Deserialize<List<string>>(
			                                  new RavenJTokenReader(debug.SelectToken("ActiveBundles")));

		    if (ApplicationModel.Current.Server.Value.UserInfo.IsAdminGlobal)
		    {
			    var doc = await ApplicationModel.Current.Server.Value.DocumentStore
			                                    .AsyncDatabaseCommands
			                                    .ForSystemDatabase()
			                                    .CreateRequest("/admin/databases/" + databaseName, "GET")
			                                    .ReadResponseJsonAsync();

			    if (doc != null)
			    {
				    var databaseDocument =
					    ApplicationModel.CreateSerializer().Deserialize<DatabaseDocument>(new RavenJTokenReader(doc));
				    Settings.DatabaseDocument = databaseDocument;

				    var databaseSettingsSectionViewModel = new DatabaseSettingsSectionViewModel();
				    Settings.Sections.Add(databaseSettingsSectionViewModel);
				    Settings.SelectedSection.Value = databaseSettingsSectionViewModel;
				    Settings.Sections.Add(new PeriodicBackupSettingsSectionModel());

				    if (bundles.Contains("Quotas"))
					    Settings.Sections.Add(new QuotaSettingsSectionModel());

				    foreach (var settingsSectionModel in Settings.Sections)
				    {
					    settingsSectionModel.LoadFor(databaseDocument);
				    }
			    }
		    }

		    if (bundles.Contains("Replication"))
			    AddModel(new ReplicationSettingsSectionModel());

		    if (bundles.Contains("SqlReplication"))
			    AddModel(new SqlReplicationSettingsSectionModel());

		    if (bundles.Contains("Versioning"))
			    AddModel(new VersioningSettingsSectionModel());

		    if (bundles.Contains("ScriptedIndexResults"))
			    AddModel(new ScriptedIndexSettingsSectionModel());

		    if (bundles.Contains("Authorization"))
		    {
			    var triggers = ApplicationModel.Current.Server.Value.SelectedDatabase.Value.Statistics.Value.Triggers;
			    if (triggers.Any(info => info.Name.Contains("Authorization")))
			    {
				    var authModel = new AuthorizationSettingsSectionModel();
				    Settings.Sections.Add(authModel);
					authModel.LoadFor(null);
			    }
		    }

		    if (Settings.Sections.Count == 0)
			    Settings.Sections.Add(new NoSettingsSectionModel());

			var url = new UrlParser(UrlUtil.Url);

			var id = url.GetQueryParam("id");
			if (string.IsNullOrWhiteSpace(id) == false)
			{
				switch (id)
				{
					case "scripted":
						if(Settings.Sections.Any(model => model is ScriptedIndexSettingsSectionModel))
							Settings.SelectedSection.Value = Settings.Sections.FirstOrDefault(model => model is ScriptedIndexSettingsSectionModel);
						break;
				}
			}
			else
				Settings.SelectedSection.Value = Settings.Sections[0];
	    }

		private void AddModel(SettingsSectionModel model)
		{
			Settings.Sections.Add(model);
			model.LoadFor(null);
		}

	    public SettingsModel Settings { get; private set; }

        private ICommand saveSettingsCommand;
        public ICommand SaveSettings { get { return saveSettingsCommand ?? (saveSettingsCommand = new SaveSettingsCommand(Settings)); } }
    }
}