﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;

namespace makecal
{
  class GooglePrimaryCalendarWriter : ICalendarWriter
  {
    private static readonly string calendarId = "primary";
    private static readonly string appName = "makecal";
    private static readonly string eventColor = "5";

    private static readonly string tag = $"{appName}=true";

    private static readonly Event.ExtendedPropertiesData eventProperties = new Event.ExtendedPropertiesData
    {
      Private__ = new Dictionary<string, string> { { appName, "true" } }
    };
          
    private static readonly GoogleCalendarEventComparer comparer = new GoogleCalendarEventComparer();

    private CalendarService Service { get; }

    public GooglePrimaryCalendarWriter(string email, string serviceAccountKey)
    {
      Service = GetCalendarService(serviceAccountKey, email);
    }

    public async Task WriteAsync(IList<CalendarEvent> events)
    {
      var existingEvents = await GetExistingEventsAsync();
      
      var expectedEvents = events.Select(o => new Event
      {
        Summary = o.Title,
        Location = o.Location,
        Start = new EventDateTime { DateTime = o.Start },
        End = new EventDateTime { DateTime = o.End }
      }).ToList();

      await DeleteEventsAsync(existingEvents.Except(expectedEvents, comparer));
      await AddEventsAsync(expectedEvents.Except(existingEvents, comparer));
    }

    private static CalendarService GetCalendarService(string serviceAccountKey, string email)
    {
      var credential = GoogleCredential.FromJson(serviceAccountKey).CreateScoped(CalendarService.Scope.Calendar).CreateWithUser(email);

      return new CalendarService(new BaseClientService.Initializer
      {
        HttpClientInitializer = credential,
        ApplicationName = appName
      });
    }

    private async Task<IList<Event>> GetExistingEventsAsync()
    {
      var listRequest = Service.Events.List(calendarId);
      listRequest.PrivateExtendedProperty = tag;
      listRequest.Fields = "items(id,summary,location,start(dateTime),end(dateTime)),nextPageToken";
      return await listRequest.FetchAllWithRetryAsync(after: DateTime.Today);
    }

    private async Task DeleteEventsAsync(IEnumerable<Event> events)
    {
      var deleteBatch = new UnlimitedBatch(Service);
      foreach (var ev in events)
      {
        deleteBatch.Queue(Service.Events.Delete(calendarId, ev.Id));
      }
      await deleteBatch.ExecuteWithRetryAsync();
    }

    private async Task AddEventsAsync(IEnumerable<Event> events)
    {
      var insertBatch = new UnlimitedBatch(Service);
      foreach (var ev in events)
      {
        ev.ColorId = eventColor;
        ev.ExtendedProperties = eventProperties;
        var insertRequest = Service.Events.Insert(ev, calendarId);
        insertRequest.Fields = "id";
        insertBatch.Queue(insertRequest);
      }
      await insertBatch.ExecuteWithRetryAsync();
    }
  }
}
