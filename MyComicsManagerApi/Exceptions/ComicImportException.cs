using System;

namespace MyComicsManagerApi.Exceptions
{
    public class ComicImportException : Exception
    {
        
        public ComicImportException()
        {
        }

        public ComicImportException(string message)
            : base(message)
        {
        }

        public ComicImportException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}