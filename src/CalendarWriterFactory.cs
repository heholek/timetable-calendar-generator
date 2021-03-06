﻿using System;
using System.IO;

namespace makecal
{
  public class CalendarWriterFactory
  {
    public OutputType OutputType { get; set; }
    private string ServiceAccountKey { get; set; }
    private string OutputDirectory { get; set; }

    public CalendarWriterFactory(OutputType outputType, string serviceAccountKey)
    {
      OutputType = outputType;

      switch (OutputType)
      {
        case OutputType.Csv:
          OutputDirectory = CreateOutputDirectory("csv");
          break;
        case OutputType.Ical:
          OutputDirectory = CreateOutputDirectory("ical");
          break;
        case OutputType.GoogleCalendar:
        case OutputType.GoogleCalendarPrimary:
        case OutputType.GoogleCalendarRemoveSecondary:
          ServiceAccountKey = serviceAccountKey;
          break;
      }
    }

    public ICalendarWriter GetCalendarWriter(string email)
    {
      if (OutputType == OutputType.GoogleCalendar || OutputType == OutputType.GoogleCalendarRemoveSecondary)
      {
        return new GoogleCalendarWriter(email, ServiceAccountKey, removeCalendars: OutputType == OutputType.GoogleCalendarRemoveSecondary);
      }
      if (OutputType == OutputType.GoogleCalendarPrimary)
      {
        return new GooglePrimaryCalendarWriter(email, ServiceAccountKey);
      }

      var userName = email.Split('@')[0];
      var outputFileName = Path.Combine(OutputDirectory, userName);

      return OutputType switch
      {
        OutputType.Csv => new CsvCalendarWriter(outputFileName + ".csv"),
        OutputType.Ical => new IcalCalendarWriter(outputFileName + ".ics"),
        _ => throw new NotImplementedException()
      };
    }

    private static string CreateOutputDirectory(string subfolder)
    {
      var directory = Path.Combine(AppContext.BaseDirectory, "calendars", subfolder);
      Directory.CreateDirectory(directory);
      return directory;
    }
  }
}
