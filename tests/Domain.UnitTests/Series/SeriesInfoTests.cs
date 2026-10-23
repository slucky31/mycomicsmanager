using Domain.Series;

namespace Domain.UnitTests.Series;

public class SeriesInfoTests
{
    private static readonly Guid s_libraryId = Guid.CreateVersion7();

    [Fact]
    public void Create_Should_KeepTheTrimmedNameAndItsNormalizedForm()
    {
        var result = SeriesInfo.Create(s_libraryId, "  Les Vieux Fourneaux ", 8, isFinished: false);

        result.IsSuccess.Should().BeTrue();
        var info = result.Value!;
        info.Id.Should().NotBe(Guid.Empty);
        info.LibraryId.Should().Be(s_libraryId);
        info.Name.Should().Be("Les Vieux Fourneaux");
        info.NormalizedName.Should().Be("LES VIEUX FOURNEAUX");
        info.TotalVolumes.Should().Be(8);
        info.IsFinished.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_ReturnBadRequest_WhenTheNameIsBlank(string name)
    {
        SeriesInfo.Create(s_libraryId, name, 8, isFinished: false).Error.Should().Be(SeriesError.BadRequest);
    }

    [Fact]
    public void Create_Should_ReturnBadRequest_WhenTheNameIsTooLong()
    {
        var name = new string('a', SeriesConstants.MaxNameLength + 1);

        SeriesInfo.Create(s_libraryId, name, 8, isFinished: false).Error.Should().Be(SeriesError.BadRequest);
    }

    [Fact]
    public void Create_Should_ReturnBadRequest_WhenTheLibraryIsEmpty()
    {
        SeriesInfo.Create(Guid.Empty, "Thorgal", 8, isFinished: false).Error.Should().Be(SeriesError.BadRequest);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(SeriesConstants.MaxTotalVolumes + 1)]
    public void Create_Should_ReturnInvalidTotalVolumes_WhenTheTotalIsOutOfRange(int totalVolumes)
    {
        SeriesInfo.Create(s_libraryId, "Thorgal", totalVolumes, isFinished: false).Error.Should().Be(SeriesError.InvalidTotalVolumes);
    }

    [Fact]
    public void Update_Should_ForgetTheTotal_WhenItIsNull()
    {
        var info = SeriesInfo.Create(s_libraryId, "Thorgal", 41, isFinished: false).Value!;

        var result = info.Update(null, isFinished: true);

        result.IsSuccess.Should().BeTrue();
        info.TotalVolumes.Should().BeNull();
        info.IsFinished.Should().BeTrue();
    }

    [Fact]
    public void Update_Should_KeepTheValues_WhenTheTotalIsInvalid()
    {
        var info = SeriesInfo.Create(s_libraryId, "Thorgal", 41, isFinished: false).Value!;

        var result = info.Update(-3, isFinished: true);

        result.Error.Should().Be(SeriesError.InvalidTotalVolumes);
        info.TotalVolumes.Should().Be(41);
        info.IsFinished.Should().BeFalse();
    }
}
