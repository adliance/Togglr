using System;

namespace Adliance.Togglr.Exceptions;

public class NoEntriesException : Exception
{
    public NoEntriesException(string project) : base($"No entries found for project {project}.")
    {
    }
}