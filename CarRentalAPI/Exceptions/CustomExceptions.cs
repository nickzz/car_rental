using System;
using System.Collections.Generic;

namespace CarRentalAPI.Exceptions
{
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }
    }

    public class BadRequestException : Exception
    {
        public BadRequestException(string message) : base(message) { }
    }

    public class UnauthorizedException : Exception
    {
        public UnauthorizedException(string message) : base(message) { }
    }

    public class ValidationException : Exception
    {
        public List<string> Errors { get; }

        public ValidationException(string message, List<string> errors = null) : base(message)
        {
            Errors = errors ?? new List<string>();
        }
    }

    public class ConflictException : Exception
    {
        public ConflictException(string message) : base(message) { }
    }
}