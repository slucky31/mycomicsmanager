namespace Application.FeedImports.Manage;

public enum FeedImportDecisionAction
{
    ForceDownload, // Download despite a (probable) duplicate
    Ignore,        // Nothing more is done with this decision
    Retry          // Failed, ignored or interrupted download: analyze / download again
}
