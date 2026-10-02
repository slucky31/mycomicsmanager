using Domain.FeedImports;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Serilog;
using Web.Models;
using Web.Services;

namespace Web.Components.Pages;

public partial class FeedImports
{
    private static readonly int[] s_pageSizeOptions = [20, 50, 100];

    [Inject] private IFeedImportService FeedImportService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private MudTable<FeedImportDecisionViewModel>? _table;
    private FeedImportDecisionStatus? _statusFilter;
    private string? _searchTerm;
    private bool _isSyncRequested;
    private readonly HashSet<Guid> _expandedIds = [];
    private TableData<FeedImportDecisionViewModel> _lastData = new() { Items = [], TotalItems = 0 };

    internal async Task<TableData<FeedImportDecisionViewModel>> LoadServerDataAsync(TableState state, CancellationToken cancellationToken)
    {
        var status = _statusFilter;
        var searchTerm = _searchTerm;

        // MudTable pages are 0-based; the service is 1-based.
        var result = await FeedImportService.GetDecisionsAsync(status, searchTerm, state.Page + 1, state.PageSize, cancellationToken);

        // MudTable cancels the token when a newer load starts; also ignore results for outdated filters.
        if (cancellationToken.IsCancellationRequested || status != _statusFilter || searchTerm != _searchTerm)
        {
            return _lastData;
        }

        if (result.IsSuccess)
        {
            _lastData = new TableData<FeedImportDecisionViewModel>
            {
                Items = result.Value!.Items,
                TotalItems = result.Value.TotalCount
            };
        }
        else if (result.IsFailure)
        {
            Snackbar.Add("Impossible de charger les décisions d'import.", Severity.Error);
            Log.Error("FeedImports: failed to load decisions: {ErrorDescription}", result.Error?.Description);
        }

        return _lastData;
    }

    private async Task OnStatusChangedAsync(FeedImportDecisionStatus? status)
    {
        _statusFilter = status;
        await ReloadAsync();
    }

    private async Task OnSearchChangedAsync(string? searchTerm)
    {
        _searchTerm = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim();
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        if (_table is not null)
        {
            await _table.ReloadServerData();
        }
    }

    private bool IsExpanded(Guid id) => _expandedIds.Contains(id);

    private void ToggleDetail(Guid id)
    {
        if (!_expandedIds.Remove(id))
        {
            _expandedIds.Add(id);
        }
    }

    internal async Task SyncNowAsync()
    {
        _isSyncRequested = true;
        try
        {
            var result = FeedImportService.TriggerSync();
            if (result.IsSuccess)
            {
                Snackbar.Add("Synchronisation lancée. Rafraîchissez la liste dans quelques instants.", Severity.Info);
            }
            else if (result.IsFailure)
            {
                Snackbar.Add(result.Error?.Description ?? "Impossible de lancer la synchronisation.", Severity.Error);
                Log.Error("FeedImports: failed to trigger sync: {ErrorDescription}", result.Error?.Description);
            }

            await ReloadAsync();
        }
        finally
        {
            _isSyncRequested = false;
        }
    }
}
