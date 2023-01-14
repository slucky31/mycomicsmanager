﻿using Xunit;
using FluentAssertions;
using MyComicsManagerApi.DataParser;

namespace MyComicsManagerApiTests
{
    public class BedethequeComicHtmlDataParserTest
    {
        private BedethequeComicHtmlDataParser Parser { get; set; }

        [Fact]
        public void Parse()
        {
            Parser = new BedethequeComicHtmlDataParser();
            var results = Parser.Parse("9782756009131");
            results[ComicDataEnum.TITRE].Should().Be("On a marché sur la lune");
            results[ComicDataEnum.SERIE].Should().Be("Les Aventures de Tintin");
            results[ComicDataEnum.SERIE_URL].Should().Be("https://www.bdphile.info/series/bd/809-les-aventures-de-tintin");
            results[ComicDataEnum.SCENARISTE].Should().Be("Hergé (Georges Remi)");
            results[ComicDataEnum.DESSINATEUR].Should().Be("Hergé (Georges Remi)");
            results[ComicDataEnum.TOME].Should().Be("17");
            results[ComicDataEnum.DATE_PARUTION].Should().Be("1975");
            results[ComicDataEnum.ISBN].Should().Be("978-2-2030-0116-9");
            results[ComicDataEnum.URL].Should().Be("https://www.bdphile.info/album/view/74161/");
            results[ComicDataEnum.EDITEUR].Should().Be("Casterman");
            results[ComicDataEnum.NOTE].Should().Be("4.5");
            results[ComicDataEnum.ONESHOT].Should().Be("False");
        }


    }
}
