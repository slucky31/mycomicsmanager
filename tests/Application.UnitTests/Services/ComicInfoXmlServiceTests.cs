using Application.Interfaces;
using Domain.Errors;
using Persistence.Services;

namespace Application.UnitTests.Services;

public sealed class ComicInfoXmlServiceTests : IDisposable
{
    private readonly ComicInfoXmlService _service = new();
    private readonly string _tempDir;

    public ComicInfoXmlServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
        GC.SuppressFinalize(this);
    }

    // -------------------------------------------------------
    // Read
    // -------------------------------------------------------

    [Fact]
    public void Read_Should_ReturnSuccess_WhenValidXml()
    {
        // Arrange
        var xmlPath = Path.Combine(_tempDir, "ComicInfo.xml");
        File.WriteAllText(xmlPath, """
            <?xml version="1.0" encoding="utf-8"?>
            <ComicInfo>
              <Title>Quelque part entre les ombres</Title>
              <Series>Blacksad</Series>
              <Number>1</Number>
              <Writer>Juan Diaz Canales</Writer>
              <Publisher>Dargaud</Publisher>
            </ComicInfo>
            """);

        // Act
        var result = _service.Read(xmlPath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public void Read_Should_ParseAllFields()
    {
        // Arrange
        var xmlPath = Path.Combine(_tempDir, "ComicInfo.xml");
        File.WriteAllText(xmlPath, """
            <?xml version="1.0" encoding="utf-8"?>
            <ComicInfo>
              <Title>Quelque part entre les ombres</Title>
              <Series>Blacksad</Series>
              <Number>1</Number>
              <Summary>Un chat détective noir...</Summary>
              <Year>2000</Year>
              <Month>11</Month>
              <Day>1</Day>
              <Writer>Juan Diaz Canales</Writer>
              <Penciller>Juanjo Guarnido</Penciller>
              <Publisher>Dargaud</Publisher>
              <ISBN>9782205050196</ISBN>
              <PageCount>48</PageCount>
            </ComicInfo>
            """);

        // Act
        var result = _service.Read(xmlPath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Title.Should().Be("Quelque part entre les ombres");
        result.Value.Series.Should().Be("Blacksad");
        result.Value.Number.Should().Be(1);
        result.Value.Summary.Should().Be("Un chat détective noir...");
        result.Value.Year.Should().Be(2000);
        result.Value.Month.Should().Be(11);
        result.Value.Day.Should().Be(1);
        result.Value.Writer.Should().Be("Juan Diaz Canales");
        result.Value.Penciller.Should().Be("Juanjo Guarnido");
        result.Value.Publisher.Should().Be("Dargaud");
        result.Value.Isbn.Should().Be("9782205050196");
        result.Value.PageCount.Should().Be(48);
    }

    [Fact]
    public void Read_Should_HandleMissingOptionalFields()
    {
        // Arrange
        var xmlPath = Path.Combine(_tempDir, "ComicInfo.xml");
        File.WriteAllText(xmlPath, """
            <?xml version="1.0" encoding="utf-8"?>
            <ComicInfo>
              <Title>Test Title</Title>
            </ComicInfo>
            """);

        // Act
        var result = _service.Read(xmlPath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Series.Should().BeNull();
        result.Value.Number.Should().BeNull();
        result.Value.Year.Should().BeNull();
        result.Value.Month.Should().BeNull();
        result.Value.PageCount.Should().BeNull();
    }

    [Fact]
    public void Read_Should_ReturnError_WhenFileDoesNotExist()
    {
        // Act
        var result = _service.Read(Path.Combine(_tempDir, "missing.xml"));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileProcessingError.FileNotFound);
    }

    [Fact]
    public void Read_Should_ReturnError_WhenXmlIsInvalid()
    {
        // Arrange
        var xmlPath = Path.Combine(_tempDir, "ComicInfo.xml");
        File.WriteAllText(xmlPath, "this is not valid xml <<< !!!");

        // Act
        var result = _service.Read(xmlPath);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileProcessingError.XmlReadError);
    }

    // -------------------------------------------------------
    // Write
    // -------------------------------------------------------

    [Fact]
    public void Write_Should_CreateValidXmlFile()
    {
        // Arrange
        var xmlPath = Path.Combine(_tempDir, "ComicInfo.xml");
        var data = new ComicInfoData("My Title", "My Series", 1, null, null, null, null, null, null, null, null, null);

        // Act
        var result = _service.Write(xmlPath, data);

        // Assert
        result.IsSuccess.Should().BeTrue();
        File.Exists(xmlPath).Should().BeTrue();
        var content = File.ReadAllText(xmlPath);
        content.Should().Contain("<ComicInfo");
        content.Should().Contain("<Title>My Title</Title>");
    }

    [Fact]
    public void Write_Should_WriteAllFields()
    {
        // Arrange
        var xmlPath = Path.Combine(_tempDir, "ComicInfo.xml");
        var data = new ComicInfoData(
            Title: "Quelque part entre les ombres",
            Series: "Blacksad",
            Number: 1,
            Summary: "Un chat noir...",
            Year: 2000,
            Month: 11,
            Day: 1,
            Writer: "Juan Diaz Canales",
            Penciller: "Juanjo Guarnido",
            Publisher: "Dargaud",
            Isbn: "9782205050196",
            PageCount: 48);

        // Act
        _service.Write(xmlPath, data);

        // Read back and verify
        var readResult = _service.Read(xmlPath);

        // Assert
        readResult.IsSuccess.Should().BeTrue();
        readResult.Value!.Title.Should().Be(data.Title);
        readResult.Value.Series.Should().Be(data.Series);
        readResult.Value.Number.Should().Be(data.Number);
        readResult.Value.Year.Should().Be(data.Year);
        readResult.Value.Month.Should().Be(data.Month);
        readResult.Value.Writer.Should().Be(data.Writer);
        readResult.Value.Publisher.Should().Be(data.Publisher);
        readResult.Value.Isbn.Should().Be(data.Isbn);
        readResult.Value.PageCount.Should().Be(data.PageCount);
    }

    [Fact]
    public void Write_Should_OmitNullFields()
    {
        // Arrange
        var xmlPath = Path.Combine(_tempDir, "ComicInfo.xml");
        var data = new ComicInfoData("Title Only", null, null, null, null, null, null, null, null, null, null, null);

        // Act
        _service.Write(xmlPath, data);

        // Assert
        var content = File.ReadAllText(xmlPath);
        content.Should().Contain("<Title>Title Only</Title>");
        content.Should().NotContain("<Series>");
        content.Should().NotContain("<Number>");
        content.Should().NotContain("<Year>");
        content.Should().NotContain("<PageCount>");
    }
}
