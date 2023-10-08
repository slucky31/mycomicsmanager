using System.IO;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using MyComicsManagerApi.DataParser;
using MyComicsManagerApi.Settings;

namespace MyComicsManagerApiTests
{
    public class BedethequeComicHtmlDataParserTest
    {
        private readonly BedethequeComicHtmlDataParser _parser;

        private readonly IGoogleSearchService _subGoogleSearch;

        public BedethequeComicHtmlDataParserTest()
        {
            _subGoogleSearch = Substitute.For<IGoogleSearchService>();
            _parser = new BedethequeComicHtmlDataParser(_subGoogleSearch);
        }

        [Fact]
        public void Parse()
        {
            _subGoogleSearch.SearchLinkFromKeywordAndPattern("2847897666").Returns("https://www.bedetheque.com/serie-11609-BD-Goon-The-Delcourt__10000.html");
            
            var results = _parser.SearchComicInfoFromIsbn("2847897666");

            results.Count.Should().BeGreaterThan(0);
            results[ComicDataEnum.TITRE].Should().Be("Rien que de la misère");
            results[ComicDataEnum.SERIE].Should().Be("The goon");
            results[ComicDataEnum.SERIE_URL].Should().Be("https://www.bedetheque.com/serie-11609-BD-Goon-The-Delcourt__10000.html");
            results[ComicDataEnum.SCENARISTE].Should().Be("Powell, Eric");
            results[ComicDataEnum.DESSINATEUR].Should().Be("Powell, Eric");
            results[ComicDataEnum.TOME].Should().Be("1");
            results[ComicDataEnum.DATE_PARUTION].Should().Be("05/2005");
            results[ComicDataEnum.ISBN].Should().Be("2847897666");
            results[ComicDataEnum.URL].Should().Be("https://www.bedetheque.com/BD-Goon-Tome-1-Rien-que-de-la-misere-46851.html");
            results[ComicDataEnum.EDITEUR].Should().Be("Delcourt");
            results[ComicDataEnum.NOTE].Should().Be("3.9");
            results[ComicDataEnum.ONESHOT].Should().Be("False");
        }
        
        [Fact]
        public void ParseSerie()
        {
            _subGoogleSearch.SearchLinkFromKeywordAndPattern("The goon").Returns("https://www.bedetheque.com/serie-11609-BD-Goon-The-Delcourt__10000.html");
            
            var results = _parser.SearchComicInfoFromSerie("The goon", 1);

            results.Count.Should().BeGreaterThan(0);
            results[ComicDataEnum.TITRE].Should().Be("Rien que de la misère");
            results[ComicDataEnum.SERIE].Should().Be("The goon");
            results[ComicDataEnum.SERIE_URL].Should().Be("https://www.bedetheque.com/serie-11609-BD-Goon-The-Delcourt__10000.html");
            results[ComicDataEnum.SCENARISTE].Should().Be("Powell, Eric");
            results[ComicDataEnum.DESSINATEUR].Should().Be("Powell, Eric");
            results[ComicDataEnum.TOME].Should().Be("1");
            results[ComicDataEnum.DATE_PARUTION].Should().Be("05/2005");
            results[ComicDataEnum.ISBN].Should().Be("2847897666");
            results[ComicDataEnum.URL].Should().Be("https://www.bedetheque.com/BD-Goon-Tome-1-Rien-que-de-la-misere-46851.html");
            results[ComicDataEnum.EDITEUR].Should().Be("Delcourt");
            results[ComicDataEnum.NOTE].Should().Be("3.9");
            results[ComicDataEnum.ONESHOT].Should().Be("False");
        }
    }
}
