using MongoDB.Driver;
using System.Collections.Generic;
using MyComicsManager.Model.Shared.Models;
using MyComicsManagerApi.DataParser;
using MyComicsManagerApi.Settings;
using MyComicsManagerApi.Utils;
using Serilog;

namespace MyComicsManagerApi.Services
{
    public class BookService
    {
        private static ILogger Log => Serilog.Log.ForContext<BookService>();

        private readonly IMongoCollection<Book> _books;
        private readonly IGoogleSearchService _googleSearchService;

        /// <summary>
        /// Initializes a new instance of the <see cref="BookService"/> class.
        /// </summary>
        /// <param name="dbSettings">The database settings.</param>
        public BookService(IDatabaseSettings dbSettings, IGoogleSearchService googleSearchService)
        {
            Log.Here().Debug("settings = {@Settings}", dbSettings);
            var client = new MongoClient(dbSettings.ConnectionString);
            var database = client.GetDatabase(dbSettings.DatabaseName);
            _books = database.GetCollection<Book>(dbSettings.BooksCollectionName);
            _googleSearchService = googleSearchService;
        }

        /// <summary>
        /// Retrieves all books from the 'Book' collection.
        /// </summary>
        /// <returns>A list of all books.</returns>
        public List<Book> Get() =>
            _books.Find(book => true).SortByDescending(book => book.Added).ToList();

        /// <summary>
        /// Retrieves a book by its ID from the 'Book' collection.
        /// </summary>
        /// <param name="id">The ID of the book.</param>
        /// <returns>The book with the specified ID.</returns>
        public Book Get(string id) =>
            _books.Find(book => book.Id == id).FirstOrDefault();

        /// <summary>
        /// Creates a new book in the 'Book' collection.
        /// </summary>
        /// <param name="bookIn">The book to create.</param>
        /// <returns>The created book.</returns>
        public Book Create(Book bookIn)
        {
            _books.InsertOne(bookIn);
            return bookIn;
        }

        /// <summary>
        /// Updates a book in the 'Book' collection.
        /// </summary>
        /// <param name="id">The ID of the book to update.</param>
        /// <param name="bookIn">The updated book.</param>
        public void Update(string id, Book bookIn)
        {
            _books.ReplaceOne(book => book.Id == id, bookIn);
        }

        /// <summary>
        /// Removes a book from the 'Book' collection.
        /// </summary>
        /// <param name="bookIn">The book to remove.</param>
        public void Remove(Book bookIn)
        {
            _books.DeleteOne(book => book.Id == bookIn.Id);
        }

        /// <summary>
        /// Searches for comic book information using an ISBN and updates the book record with the retrieved information.
        /// </summary>
        /// <param name="isbn">The ISBN to search for.</param>
        /// <returns>The book with the updated comic book information.</returns>
        public Book SearchComicInfoAndUpdate(string isbn)
        {
            if (string.IsNullOrEmpty(isbn))
            {
                return null;
            }

            var book = new Book();

            var parser = new BedethequeComicHtmlDataParser(_googleSearchService);
            var results = parser.SearchComicInfoFromIsbn(isbn);

            if (results.Count > 0)
            {
                // Récupération des informations du comic
                // TODO : que se passe t'il si la clé n'existe pas dans results ?
                book.Isbn = results[ComicDataEnum.ISBN];
                book.Serie = results[ComicDataEnum.SERIE];
                book.Title = results[ComicDataEnum.TITRE];

                if (int.TryParse(results[ComicDataEnum.TOME], out var intValue))
                {
                    book.Volume = intValue;
                }
                else
                {
                    Log.Here().Warning("An error occurred while parsing the volume: {Tome}", results[ComicDataEnum.TOME]);
                }
            }
            else
            {
                book.Isbn = isbn;
            }

            Update(book.Id, book);
            return book;
        }
    }
}