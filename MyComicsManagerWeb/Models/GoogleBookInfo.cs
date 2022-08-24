using System;
using System.Collections.Generic;
using System.Linq;
using Google.Apis.Books.v1.Data;
using MyComicsManager.Model.Shared;

namespace MyComicsManagerWeb.Models;

public class GoogleBookInfo
{
    public  string Title { get; set; }
    public string Isbn { get; set; }
    public bool IsIsbnChecked  { get; set; }
    public Comic searchComic  { get; set; }

    public GoogleBookInfo(Volume volume)
    {
        Title = volume.VolumeInfo.Title;
        Isbn = ExtractIsbn(volume);
        IsIsbnChecked = false;
        searchComic = null;
    }
    
    public override string ToString()
    {
        var s= "GoogleBookInfo : " + Title + " - " + Isbn + Environment.NewLine;
        s += searchComic.ToString();
        return s;
    }
    
    private static string ExtractIsbn(Volume volume)
    {
        var industryIdentifiers = volume.VolumeInfo.IndustryIdentifiers;
        
        if (null == industryIdentifiers)
        {
            return "";
        }
    
        try
        {
            var identifier = industryIdentifiers.First(i => i.Type.Equals("ISBN_13"));
            return identifier.Identifier;
        }
        catch (Exception)
        {
            return "";
        }
        
    }
}