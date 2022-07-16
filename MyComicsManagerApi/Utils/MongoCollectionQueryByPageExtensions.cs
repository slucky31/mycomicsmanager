﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace MyComicsManagerApi.Utils;

// Source : https://kevsoft.net/2020/01/27/paging-data-in-mongodb-with-csharp.html

public static class MongoCollectionQueryByPageExtensions
{
    public static async Task<(int totalPages, IReadOnlyList<TDocument> data)> AggregateByPage<TDocument>(
        this IMongoCollection<TDocument> collection,
        FilterDefinition<TDocument> filterDefinition,
        SortDefinition<TDocument> sortDefinition,
        int page,
        int pageSize)
    {
        var countFacet = AggregateFacet.Create("count",
            PipelineDefinition<TDocument, AggregateCountResult>.Create(new[]
            {
                PipelineStageDefinitionBuilder.Count<TDocument>()
            }));

        var dataFacet = AggregateFacet.Create("data",
            PipelineDefinition<TDocument, TDocument>.Create(new[]
            {
                PipelineStageDefinitionBuilder.Sort(sortDefinition),
                PipelineStageDefinitionBuilder.Skip<TDocument>((page - 1) * pageSize),
                PipelineStageDefinitionBuilder.Limit<TDocument>(pageSize),
            }));


        var aggregation = await collection.Aggregate()
            .Match(filterDefinition)
            .Facet(countFacet, dataFacet)
            .ToListAsync();

        var count = aggregation.First()
            .Facets.First(x => x.Name == "count")
            .Output<AggregateCountResult>()
            ?.FirstOrDefault()
            ?.Count;
        
        var totalPages = (int)Math.Ceiling((double)count/ pageSize);

        var data = aggregation.First()
            .Facets.First(x => x.Name == "data")
            .Output<TDocument>();

        return (totalPages, data);
    }
}