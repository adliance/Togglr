using System;
using TogglApi.Client.Exceptions;

namespace Adliance.Togglr.Exceptions;

public class TogglException : Exception
{
    public TogglException(TogglApiException ex) : base($"Unable to download results from Toggl API: {ex.Message}", ex)
    {
    }
}
