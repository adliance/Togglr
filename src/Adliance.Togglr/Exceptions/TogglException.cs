using System;
using TogglApi.Client.Exceptions;

namespace Adliance.Togglr.Exceptions;

public class TogglException(TogglApiException ex) : Exception($"Unable to download results from Toggl API: {ex.Message}", ex);
