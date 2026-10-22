using System.Globalization;
using System.Xml;
using System.Xml.Serialization;
using Application.Interfaces;
using Domain.Errors;
using Domain.Primitives;
using Microsoft.Extensions.Logging;

namespace Persistence.Services;

public class ComicInfoXmlService(ILogger<ComicInfoXmlService> logger) : IComicInfoXmlService
{
    private static readonly XmlSerializer s_serializer = new(typeof(ComicInfoDocument));

    public Result<ComicInfoData> Read(string xmlPath)
    {
        if (!File.Exists(xmlPath))
        {
            return FileProcessingError.FileNotFound;
        }

        try
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersFromEntities = 0,
                MaxCharactersInDocument = 1_000_000,
            };

            using var reader = XmlReader.Create(xmlPath, settings);
            var doc = (ComicInfoDocument?)s_serializer.Deserialize(reader);
            if (doc is null)
            {
                return FileProcessingError.XmlReadError;
            }

            return doc.ToComicInfoData();
        }
        catch (Exception ex) when (ex is InvalidOperationException or XmlException)
        {
            logger.LogError(ex, "Failed to parse ComicInfo.xml: {Path}", xmlPath);
            return FileProcessingError.XmlReadError;
        }
    }

    public Result Write(string xmlPath, ComicInfoData data)
    {
        try
        {
            var doc = ComicInfoDocument.FromComicInfoData(data);
            var settings = new XmlWriterSettings
            {
                Indent = true,
                Encoding = System.Text.Encoding.UTF8
            };

            using var writer = XmlWriter.Create(xmlPath, settings);
            s_serializer.Serialize(writer, doc);
            return Result.Success();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to write ComicInfo.xml: {Path}", xmlPath);
            return FileProcessingError.XmlWriteError;
        }
    }

    // The numeric fields are read as text: the ComicInfo schema types Number as a string ("1.5", "HS"...) and
    // taggers write free text in the others, so a value that is not an integer becomes null instead of failing the file.
    [XmlRoot("ComicInfo")]
    public class ComicInfoDocument
    {
        public string? Title { get; set; }
        public string? Series { get; set; }
        public string? Number { get; set; }
        public string? Volume { get; set; }
        public string? Summary { get; set; }
        public string? Year { get; set; }
        public string? Month { get; set; }
        public string? Day { get; set; }
        public string? Writer { get; set; }
        public string? Penciller { get; set; }
        public string? Publisher { get; set; }

        [XmlElement("ISBN")]
        public string? Isbn { get; set; }

        public string? PageCount { get; set; }

        public ComicInfoData ToComicInfoData() => new(
            Title: Title,
            Series: Series,
            // Some taggers leave Number empty and put the tome in Volume.
            Number: ParseVolume(Number) ?? ParsePositiveInt(Volume),
            Summary: Summary,
            Year: ParsePositiveInt(Year),
            Month: ParsePositiveInt(Month),
            Day: ParsePositiveInt(Day),
            Writer: Writer,
            Penciller: Penciller,
            Publisher: Publisher,
            Isbn: Isbn,
            PageCount: ParsePositiveInt(PageCount)
        );

        public static ComicInfoDocument FromComicInfoData(ComicInfoData data) => new()
        {
            Title = data.Title,
            Series = data.Series,
            Number = FormatInt(data.Number),
            Summary = data.Summary,
            Year = FormatInt(data.Year),
            Month = FormatInt(data.Month),
            Day = FormatInt(data.Day),
            Writer = data.Writer,
            Penciller = data.Penciller,
            Publisher = data.Publisher,
            Isbn = data.Isbn,
            PageCount = FormatInt(data.PageCount)
        };

        private static int? ParseInt(string? value) =>
            int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) ? number : null;

        // Taggers write the volume as "3" or "3.0": a whole decimal is kept, "1.5" or "HS" is not a volume.
        private static int? ParseVolume(string? value) =>
            decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number)
            && number == decimal.Truncate(number) && number is >= int.MinValue and <= int.MaxValue
                ? (int)number
                : null;

        // Year, Month, Day and PageCount use 0 or -1 for "unknown" in the ComicInfo schema.
        private static int? ParsePositiveInt(string? value) => ParseInt(value) is > 0 and var number ? number : null;

        private static string? FormatInt(int? value) => value?.ToString(CultureInfo.InvariantCulture);
    }
}
