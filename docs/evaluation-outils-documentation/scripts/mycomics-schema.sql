CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;
CREATE TABLE "Libraries" (
    "Id" uuid NOT NULL,
    "Name" text NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone,
    CONSTRAINT "PK_Libraries" PRIMARY KEY ("Id")
);

CREATE TABLE "Users" (
    "Id" uuid NOT NULL,
    "Email" text NOT NULL,
    "AuthId" text NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone,
    CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250531191428_InitialCreate', '10.0.13');

COMMIT;

START TRANSACTION;
CREATE TABLE "Books" (
    "Id" uuid NOT NULL,
    "Serie" text NOT NULL,
    "Title" text NOT NULL,
    "ISBN" text NOT NULL,
    "VolumeNumber" integer NOT NULL,
    "ImageLink" text NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone,
    CONSTRAINT "PK_Books" PRIMARY KEY ("Id")
);

CREATE TABLE "ReadingDates" (
    "Id" uuid NOT NULL,
    "Date" timestamp with time zone NOT NULL,
    "Note" text NOT NULL,
    "BookId" uuid NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone,
    CONSTRAINT "PK_ReadingDates" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ReadingDates_Books_BookId" FOREIGN KEY ("BookId") REFERENCES "Books" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_ReadingDates_BookId" ON "ReadingDates" ("BookId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250922194526_AddBook', '10.0.13');

COMMIT;

START TRANSACTION;
ALTER TABLE "Books" ADD "Rating" integer NOT NULL DEFAULT 0;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260131102359_AddRatingToBooks', '10.0.13');

COMMIT;

START TRANSACTION;
ALTER TABLE "ReadingDates" DROP COLUMN "Note";

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260201111729_RemoveNoteFromReadingDate', '10.0.13');

COMMIT;

START TRANSACTION;
ALTER TABLE "Books" ADD "Authors" text NOT NULL DEFAULT '';

ALTER TABLE "Books" ADD "NumberOfPages" integer;

ALTER TABLE "Books" ADD "PublishDate" date;

ALTER TABLE "Books" ADD "Publishers" text NOT NULL DEFAULT '';

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260207133302_AddBookMetadata', '10.0.13');

COMMIT;

START TRANSACTION;
ALTER TABLE "Books" ALTER COLUMN "Title" TYPE character varying(200);

ALTER TABLE "Books" ALTER COLUMN "Serie" TYPE character varying(200);

ALTER TABLE "Books" ALTER COLUMN "Publishers" TYPE character varying(200);

ALTER TABLE "Books" ALTER COLUMN "ImageLink" TYPE character varying(500);

ALTER TABLE "Books" ALTER COLUMN "ISBN" TYPE character varying(20);

ALTER TABLE "Books" ALTER COLUMN "Authors" TYPE character varying(200);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260207181318_AddBookStringMaxLengths', '10.0.13');

COMMIT;

START TRANSACTION;
ALTER TABLE "Books" DROP COLUMN "Rating";

ALTER TABLE "ReadingDates" ADD "Rating" integer NOT NULL DEFAULT 0;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260222214022_MoveRatingToReadingDate', '10.0.13');

COMMIT;

START TRANSACTION;
ALTER TABLE "Libraries" ALTER COLUMN "Name" TYPE character varying(200);

ALTER TABLE "Libraries" ADD "BookType" integer NOT NULL DEFAULT 0;

ALTER TABLE "Libraries" ADD "Color" character varying(7) NOT NULL DEFAULT '';

ALTER TABLE "Libraries" ADD "Icon" character varying(100) NOT NULL DEFAULT '';

ALTER TABLE "Libraries" ADD "IsDefault" boolean NOT NULL DEFAULT FALSE;

ALTER TABLE "Libraries" ADD "UserId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE "Books" ADD "LibraryId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

CREATE TABLE "DigitalBooks" (
    "Id" uuid NOT NULL,
    CONSTRAINT "PK_DigitalBooks" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_DigitalBooks_Books_Id" FOREIGN KEY ("Id") REFERENCES "Books" ("Id") ON DELETE CASCADE
);

CREATE TABLE "PhysicalBooks" (
    "Id" uuid NOT NULL,
    CONSTRAINT "PK_PhysicalBooks" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PhysicalBooks_Books_Id" FOREIGN KEY ("Id") REFERENCES "Books" ("Id") ON DELETE CASCADE
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260227082728_AddLibrariesAndBookTypes', '10.0.13');

COMMIT;

START TRANSACTION;
UPDATE "Libraries" SET "BookType" = 0 WHERE "BookType" = 2;

ALTER TABLE "Libraries" DROP COLUMN "IsDefault";

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260227102924_RemoveIsDefaultFromLibraries', '10.0.13');

COMMIT;

START TRANSACTION;
CREATE INDEX "IX_Books_LibraryId" ON "Books" ("LibraryId");

ALTER TABLE "Books" ADD CONSTRAINT "FK_Books_Libraries_LibraryId" FOREIGN KEY ("LibraryId") REFERENCES "Libraries" ("Id") ON DELETE CASCADE;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260301152834_AddLibraryForeignKeyToBooks', '10.0.13');

COMMIT;

START TRANSACTION;
ALTER TABLE "Books" ALTER COLUMN "LibraryId" DROP DEFAULT;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260301161511_RemoveLibraryIdDefault', '10.0.13');

COMMIT;

START TRANSACTION;
ALTER TABLE "Libraries" ADD "DefaultBookSortOrder" integer NOT NULL DEFAULT 0;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260308201803_AddDefaultBookSortOrderToLibrary', '10.0.13');

COMMIT;

START TRANSACTION;
CREATE TABLE "IsbnBedethequeUrls" (
    "Id" uuid NOT NULL,
    "ISBN" character varying(20) NOT NULL,
    "Url" character varying(500) NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone,
    CONSTRAINT "PK_IsbnBedethequeUrls" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "IX_IsbnBedethequeUrls_ISBN" ON "IsbnBedethequeUrls" ("ISBN");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260322203047_AddIsbnBedethequeCacheTable', '10.0.13');

COMMIT;

START TRANSACTION;
ALTER TABLE "DigitalBooks" ADD "FilePath" character varying(500) NOT NULL DEFAULT '';

ALTER TABLE "DigitalBooks" ADD "FileSize" bigint NOT NULL DEFAULT 0;

CREATE TABLE "ImportJobs" (
    "Id" uuid NOT NULL,
    "OriginalFileName" character varying(260) NOT NULL,
    "OriginalFilePath" character varying(500) NOT NULL,
    "OriginalFileSize" bigint NOT NULL,
    "LibraryId" uuid NOT NULL,
    "DigitalBookId" uuid,
    "Status" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CompletedAt" timestamp with time zone,
    "ErrorMessage" character varying(2000),
    "ErrorStep" character varying(100),
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone,
    CONSTRAINT "PK_ImportJobs" PRIMARY KEY ("Id")
);

CREATE INDEX "IX_ImportJobs_LibraryId" ON "ImportJobs" ("LibraryId");

CREATE INDEX "IX_ImportJobs_Status" ON "ImportJobs" ("Status");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260411144336_AddDigitalBookFieldsAndImportJobs', '10.0.13');

COMMIT;

START TRANSACTION;
ALTER TABLE "Books" ALTER COLUMN "ISBN" DROP NOT NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260411212340_MakeIsbnNullable', '10.0.13');

COMMIT;

START TRANSACTION;
ALTER TABLE "ImportJobs" ADD "ConvertedImagesCount" integer NOT NULL DEFAULT 0;

ALTER TABLE "ImportJobs" ADD "TotalImagesToConvert" integer NOT NULL DEFAULT 0;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260412195057_AddConversionProgressToImportJob', '10.0.13');

COMMIT;

START TRANSACTION;
CREATE UNIQUE INDEX "IX_ImportJobs_OriginalFilePath" ON "ImportJobs" ("OriginalFilePath") WHERE "Status" NOT IN (6, 7);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260914214155_AddImportJobConcurrencyAndUniqueFilePath', '10.0.13');

COMMIT;

START TRANSACTION;
CREATE TABLE "FeedImportDecisions" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "MinifluxEntryId" bigint NOT NULL,
    "EntryTitle" character varying(500) NOT NULL,
    "EntryUrl" character varying(2000) NOT NULL,
    "PublishedAt" timestamp with time zone,
    "ParsedSerie" character varying(200),
    "ParsedTitle" character varying(300),
    "ParsedVolume" integer,
    "Links" jsonb,
    "ChosenMirror" character varying(2000),
    "MatchedBookId" uuid,
    "ImportJobId" uuid,
    "DigitalBookId" uuid,
    "Status" integer NOT NULL,
    "Reason" character varying(1000) NOT NULL,
    "DecidedBy" integer NOT NULL,
    "ErrorMessage" character varying(2000),
    "ErrorStep" character varying(100),
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone,
    CONSTRAINT "PK_FeedImportDecisions" PRIMARY KEY ("Id")
);

CREATE TABLE "FeedImportDecisionEvents" (
    "Id" uuid NOT NULL,
    "FeedImportDecisionId" uuid NOT NULL,
    "OccurredAt" timestamp with time zone NOT NULL,
    "PreviousStatus" integer,
    "Status" integer NOT NULL,
    "DecidedBy" integer NOT NULL,
    "Description" character varying(1000) NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone,
    CONSTRAINT "PK_FeedImportDecisionEvents" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_FeedImportDecisionEvents_FeedImportDecisions_FeedImportDeci~" FOREIGN KEY ("FeedImportDecisionId") REFERENCES "FeedImportDecisions" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_FeedImportDecisionEvents_FeedImportDecisionId" ON "FeedImportDecisionEvents" ("FeedImportDecisionId");

CREATE UNIQUE INDEX "IX_FeedImportDecisions_UserId_MinifluxEntryId" ON "FeedImportDecisions" ("UserId", "MinifluxEntryId");

CREATE INDEX "IX_FeedImportDecisions_UserId_Status" ON "FeedImportDecisions" ("UserId", "Status");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261001205519_AddFeedImportDecisions', '10.0.13');

COMMIT;

START TRANSACTION;
DROP INDEX "IX_FeedImportDecisions_UserId_MinifluxEntryId";

ALTER TABLE "FeedImportDecisions" ADD "ArbitrationKind" integer NOT NULL DEFAULT 0;

ALTER TABLE "FeedImportDecisions" ADD "ItemIndex" integer NOT NULL DEFAULT 0;

CREATE UNIQUE INDEX "IX_FeedImportDecisions_UserId_MinifluxEntryId_ItemIndex" ON "FeedImportDecisions" ("UserId", "MinifluxEntryId", "ItemIndex");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261002195323_AddFeedImportItemsAndArbitration', '10.0.13');

COMMIT;

START TRANSACTION;
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261003171656_FeedImportDecisionEventIdNotGenerated', '10.0.13');

COMMIT;

START TRANSACTION;
ALTER TABLE "FeedImportDecisions" DROP COLUMN "DigitalBookId";

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261004093116_RemoveFeedImportDecisionDigitalBookId', '10.0.13');

COMMIT;

START TRANSACTION;
ALTER TABLE "DigitalBooks" ADD "LastReadPage" integer NOT NULL DEFAULT 0;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261009054139_AddDigitalBookLastReadPage', '10.0.13');

COMMIT;

START TRANSACTION;
ALTER TABLE "DigitalBooks" ADD "IsbnCandidates" text[] NOT NULL DEFAULT ARRAY[]::text[];

ALTER TABLE "DigitalBooks" ADD "IsbnScannedAt" timestamp with time zone;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261010074455_AddDigitalBookIsbnScan', '10.0.13');

COMMIT;

START TRANSACTION;
CREATE TABLE "DataProtectionKeys" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "FriendlyName" text,
    "Xml" text,
    CONSTRAINT "PK_DataProtectionKeys" PRIMARY KEY ("Id")
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261010125019_AddDataProtectionKeys', '10.0.13');

COMMIT;

START TRANSACTION;
CREATE TABLE "FeatureToggleOverrides" (
    "Id" uuid NOT NULL,
    "Toggle" character varying(64) NOT NULL,
    "Enabled" boolean NOT NULL,
    "CreatedOnUtc" timestamp with time zone NOT NULL,
    "ModifiedOnUtc" timestamp with time zone,
    CONSTRAINT "PK_FeatureToggleOverrides" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "IX_FeatureToggleOverrides_Toggle" ON "FeatureToggleOverrides" ("Toggle");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261010140938_AddFeatureToggleOverrides', '10.0.13');

COMMIT;

START TRANSACTION;
ALTER TABLE "DigitalBooks" ADD "ReaderLayout" integer NOT NULL DEFAULT 0;

ALTER TABLE "DigitalBooks" ADD "ReaderScrolling" integer NOT NULL DEFAULT 0;

ALTER TABLE "DigitalBooks" ADD "ReadingDirection" integer NOT NULL DEFAULT 0;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261012173235_AddDigitalBookReaderSettings', '10.0.13');

COMMIT;

