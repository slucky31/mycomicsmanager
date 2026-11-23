namespace Domain.Books;

// Where the cover of a digital book comes from (#1225), stored as a number: keep the order of the values.
public enum CoverSource
{
    // Found on the web or in a catalogue: the first page of the archive replaces it.
    Web,

    // The first page of the archive.
    Archive
}
