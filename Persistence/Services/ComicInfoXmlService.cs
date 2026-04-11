using System.Xml;
using System.Xml.Serialization;
using Application.Interfaces;
using Domain.Errors;
using Domain.Primitives;

namespace Persistence.Services;

public class ComicInfoXmlService : IComicInfoXmlService
{
    private static Serilog.ILogger Log => Serilog.Log.ForContext<ComicInfoXmlService>();

    private static readonly XmlSerializer s_serializer = new(typeof(ComicInfoDocument));

    public Result<ComicInfoData> Read(string xmlPath)
    {
        if (!File.Exists(xmlPath))
        {
            return FileProcessingError.FileNotFound;
        }

        try
        {
            using var reader = XmlReader.Create(xmlPath);
            var doc = (ComicInfoDocument?)s_serializer.Deserialize(reader);
            if (doc is null)
            {
                return FileProcessingError.XmlReadError;
            }

            return doc.ToComicInfoData();
        }
        catch (Exception ex) when (ex is InvalidOperationException or XmlException)
        {
            Log.Error(ex, "Failed to parse ComicInfo.xml: {Path}", xmlPath);
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
            Log.Error(ex, "Failed to write ComicInfo.xml: {Path}", xmlPath);
            return FileProcessingError.XmlWriteError;
        }
    }

    [XmlRoot("ComicInfo")]
    public class ComicInfoDocument
    {
        public string? Title { get; set; }
        public string? Series { get; set; }
        public int Number { get; set; }
        public bool ShouldSerializeNumber() => Number != 0;
        public string? Summary { get; set; }
        public int Year { get; set; }
        public bool ShouldSerializeYear() => Year != 0;
        public int Month { get; set; }
        public bool ShouldSerializeMonth() => Month != 0;
        public int Day { get; set; }
        public bool ShouldSerializeDay() => Day != 0;
        public string? Writer { get; set; }
        public string? Penciller { get; set; }
        public string? Publisher { get; set; }

        [XmlElement("ISBN")]
        public string? Isbn { get; set; }

        public int PageCount { get; set; }
        public bool ShouldSerializePageCount() => PageCount != 0;

        public ComicInfoData ToComicInfoData() => new(
            Title: Title,
            Series: Series,
            Number: Number == 0 ? null : Number,
            Summary: Summary,
            Year: Year == 0 ? null : Year,
            Month: Month == 0 ? null : Month,
            Day: Day == 0 ? null : Day,
            Writer: Writer,
            Penciller: Penciller,
            Publisher: Publisher,
            Isbn: Isbn,
            PageCount: PageCount == 0 ? null : PageCount
        );

        public static ComicInfoDocument FromComicInfoData(ComicInfoData data) => new()
        {
            Title = data.Title,
            Series = data.Series,
            Number = data.Number ?? 0,
            Summary = data.Summary,
            Year = data.Year ?? 0,
            Month = data.Month ?? 0,
            Day = data.Day ?? 0,
            Writer = data.Writer,
            Penciller = data.Penciller,
            Publisher = data.Publisher,
            Isbn = data.Isbn,
            PageCount = data.PageCount ?? 0
        };
    }
}
