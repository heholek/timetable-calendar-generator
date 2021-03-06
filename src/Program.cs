﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace makecal
{
  public static class Program
  {
    private static async Task Main(string[] args)
    {
      try
      {
        Console.Clear();
        Console.CursorVisible = false;
        Console.WriteLine("TIMETABLE CALENDAR GENERATOR\n");

        var argumentParser = new ArgumentParser();
        var outputFormat = argumentParser.Parse(args);

        var settings = await InputReader.LoadSettingsAsync();
        var serviceAccountKey = (outputFormat.Type == OutputType.GoogleCalendar || outputFormat.Type == OutputType.GoogleCalendarPrimary
          || outputFormat.Type == OutputType.GoogleCalendarRemoveSecondary) ? await InputReader.LoadKeyAsync() : null;

        var people = await InputReader.LoadPeopleAsync();

        var calendarGenerator = new CalendarGenerator(settings);
        var calendarWriterFactory = new CalendarWriterFactory(outputFormat.Type, serviceAccountKey);
        
        Console.SetBufferSize(Math.Max(ConsoleHelper.MinConsoleWidth, Console.BufferWidth),
          Math.Max(ConsoleHelper.HeaderHeight + people.Count + ConsoleHelper.FooterHeight, Console.BufferHeight));
        Console.WriteLine($"\n{outputFormat.Text}:");

        var writeTasks = new List<Task>();
        using (var throttler = new SemaphoreSlim(outputFormat.SimultaneousRequests))
        {
          for (var i = 0; i < people.Count; i++)
          {
            var countLocal = i;
            await throttler.WaitAsync();
            var person = people[countLocal];
            var line = countLocal + ConsoleHelper.HeaderHeight;
            ConsoleHelper.WriteDescription(line, $"({countLocal + 1}/{people.Count}) {person.Email}");
            ConsoleHelper.WriteStatus(line, "...");

            writeTasks.Add(Task.Run(async () =>
            {
              try
              {
                var calendarWriter = calendarWriterFactory.GetCalendarWriter(person.Email);
                if (calendarWriterFactory.OutputType == OutputType.GoogleCalendarRemoveSecondary)
                {
                  await calendarWriter.WriteAsync(null);
                }
                else
                {
                  var events = calendarGenerator.Generate(person);
                  await calendarWriter.WriteAsync(events);
                }
                ConsoleHelper.WriteStatus(line, "Done.");
              }
              catch (Exception exc)
              {
                ConsoleHelper.WriteStatus(line, $"Failed. {exc.Message}", ConsoleColor.Red);
              }
              finally
              {
                throttler.Release();
              }
            }));
          }
          await Task.WhenAll(writeTasks);
        }

        Console.SetCursorPosition(0, ConsoleHelper.HeaderHeight + people.Count);
        Console.WriteLine("\nOperation complete.\n");
      }
      catch (Exception exc)
      {
        ConsoleHelper.WriteError(exc.Message);
      }
      finally
      {
        Console.CursorVisible = true;
      }
    }

  }
}
