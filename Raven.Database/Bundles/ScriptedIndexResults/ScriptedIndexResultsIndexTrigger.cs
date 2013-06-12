﻿// -----------------------------------------------------------------------
//  <copyright file="ScriptedIndexResultsIndexTrigger.cs" company="Hibernating Rhinos LTD">
//      Copyright (c) Hibernating Rhinos LTD. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Lucene.Net.Documents;
using Raven.Abstractions.Data;
using Raven.Database.Json;
using Raven.Database.Plugins;
using Raven.Abstractions.Extensions;
using Raven.Json.Linq;

namespace Raven.Database.Bundles.ScriptedIndexResults
{
    [InheritedExport(typeof(AbstractIndexUpdateTrigger))]
    [ExportMetadata("Bundle", "ScriptedIndexResults")]
    public class ScriptedIndexResultsIndexTrigger : AbstractIndexUpdateTrigger
    {
        public override AbstractIndexUpdateTriggerBatcher CreateBatcher(string indexName)
        {
            //Only apply the trigger if there is a setup doc for this particular index
            var jsonSetupDoc = Database.Get(Abstractions.Data.ScriptedIndexResults.IdPrefix + indexName, null);
            if (jsonSetupDoc == null)
                return null;
            var scriptedIndexResults = jsonSetupDoc.DataAsJson.JsonDeserialization<Abstractions.Data.ScriptedIndexResults>();
            return new Batcher(Database, scriptedIndexResults);
        }

        public class Batcher : AbstractIndexUpdateTriggerBatcher
        {
            private readonly DocumentDatabase database;
            private readonly Abstractions.Data.ScriptedIndexResults scriptedIndexResults;

            private readonly Dictionary<string, RavenJObject> created = new Dictionary<string, RavenJObject>(StringComparer.InvariantCultureIgnoreCase);
            private readonly HashSet<string> removed = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

            public Batcher(DocumentDatabase database, Abstractions.Data.ScriptedIndexResults scriptedIndexResults)
            {
                this.database = database;
                this.scriptedIndexResults = scriptedIndexResults;
            }

            public override void OnIndexEntryCreated(string entryKey, Lucene.Net.Documents.Document document)
            {
                created.Add(entryKey, CreateJsonDocumentFromLuceneDocument(document));
                removed.Remove(entryKey);
            }

            public override void OnIndexEntryDeleted(string entryKey)
            {
                removed.Add(entryKey);
            }

            public override void Dispose()
            {
                var patcher = new ScriptedIndexResultsJsonPatcher(database);

                if (string.IsNullOrEmpty(scriptedIndexResults.DeleteScript) == false)
                {
                    foreach (var removeKey in removed)
                    {
                        patcher.Apply(new RavenJObject(), new ScriptedPatchRequest
                        {
                            Script = scriptedIndexResults.DeleteScript,
                            Values =
							{
								{"key", removeKey}
							}
                        });
                    }
                }

                if (string.IsNullOrEmpty(scriptedIndexResults.IndexScript) == false)
                {
                    foreach (var kvp in created)
                    {
                        patcher.Apply(kvp.Value, new ScriptedPatchRequest
                        {
                            Script = scriptedIndexResults.IndexScript,
                            Values =
							{
								{"key", kvp.Key}
							}
                        });
                    }
                }

                database.TransactionalStorage.Batch(accessor =>
                {
                    if (patcher.CreatedDocs != null)
                    {
                        foreach (var jsonDocument in patcher.CreatedDocs)
                        {
                            patcher.DocumentsToDelete.Remove(jsonDocument.Key);
                            database.Put(jsonDocument.Key, jsonDocument.Etag, jsonDocument.DataAsJson, jsonDocument.Metadata, null);
                        }
                    }

                    foreach (var doc in patcher.DocumentsToDelete)
                    {
                        database.Delete(doc, null, null);
                    }
                });
            }

            public class ScriptedIndexResultsJsonPatcher : ScriptedJsonPatcher
            {
                public ScriptedIndexResultsJsonPatcher(DocumentDatabase database)
                    : base(database)
                {
                }

                public HashSet<string> DocumentsToDelete = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

                protected override void CustomizeEngine(Jint.JintEngine jintEngine)
                {
                    jintEngine.SetFunction("DeleteDocument", ((Action<string>)(document => DocumentsToDelete.Remove(document))));
                }

                protected override void RemoveEngineCustomizations(Jint.JintEngine jintEngine)
                {
                    jintEngine.RemoveParameter("DeleteDocument");
                }
            }

            private static RavenJObject CreateJsonDocumentFromLuceneDocument(Document document)
            {
                var field = document.GetField(Constants.ReduceValueFieldName);
                if (field != null)
                    return RavenJObject.Parse(field.StringValue);

                var ravenJObject = new RavenJObject();

                foreach (var fieldable in document.GetFields())
                {
                    var stringValue = GetStringValue(fieldable);
                    RavenJToken value;
                    if (ravenJObject.TryGetValue(fieldable.Name, out value) == false)
                    {
                        ravenJObject[fieldable.Name] = stringValue;
                    }
                    else
                    {
                        var ravenJArray = value as RavenJArray;
                        if (ravenJArray != null)
                        {
                            ravenJArray.Add(stringValue);
                        }
                        else
                        {
                            ravenJArray = new RavenJArray { value, stringValue };
                            ravenJObject[fieldable.Name] = ravenJArray;
                        }
                    }
                }
                return ravenJObject;
            }

            private static string GetStringValue(IFieldable field)
            {
                switch (field.StringValue)
                {
                    case Constants.NullValue:
                        return null;
                    case Constants.EmptyString:
                        return string.Empty;
                    default:
                        return field.StringValue;
                }
            }
        }
    }
}