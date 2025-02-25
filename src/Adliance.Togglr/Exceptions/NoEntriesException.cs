using System;

namespace Adliance.Togglr.Exceptions;

public class NoEntriesException(string project) : Exception($"No entries found for project {project}.");
