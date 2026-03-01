namespace Domain.Libraries;

public enum LibraryBookType
{
    Physical,   // library contains only PhysicalBooks
    Digital     // library contains only DigitalBooks, has a storage folder (RelativePath)
}
