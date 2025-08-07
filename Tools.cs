using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace Nova;

public class GoogleCalendarService
{
    private const string ApplicationName = "Nova MCP Server";
    private const string CredentialsFileName = "credentials.json";
    private const string TokensPath = "tokens";
    private CalendarService? _service;

    private readonly string[] Scopes = { CalendarService.Scope.Calendar };

    public async Task<CalendarService> GetServiceAsync()
    {
        if (_service != null)
            return _service;

        UserCredential credential;

        // Cargar credenciales desde el archivo credentials.json
        using (var stream = new FileStream(CredentialsFileName, FileMode.Open, FileAccess.Read))
        {
            credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.FromStream(stream).Secrets,
                Scopes,
                "user",
                CancellationToken.None,
                new FileDataStore(TokensPath, true));
        }

        // Crear el servicio de Calendar
        _service = new CalendarService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = ApplicationName,
        });

        return _service;
    }

    public async Task<object> CreateEventAsync(object parameters)
    {
        try
        {
            var service = await GetServiceAsync();
            var paramDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
                System.Text.Json.JsonSerializer.Serialize(parameters));

            var eventItem = new Event()
            {
                Summary = paramDict!["summary"].ToString(),
                Description = paramDict.ContainsKey("description") ? paramDict["description"].ToString() : null,
                Location = paramDict.ContainsKey("location") ? paramDict["location"].ToString() : null,
                Start = new EventDateTime()
                {
                    DateTime = DateTime.Parse(paramDict["start"].ToString()!),
                    TimeZone = "America/Mexico_City"
                },
                End = new EventDateTime()
                {
                    DateTime = DateTime.Parse(paramDict["end"].ToString()!),
                    TimeZone = "America/Mexico_City"
                }
            };

            var request = service.Events.Insert(eventItem, "primary");
            var createdEvent = await request.ExecuteAsync();

            return new
            {
                success = true,
                eventId = createdEvent.Id,
                summary = createdEvent.Summary,
                start = createdEvent.Start.DateTime,
                end = createdEvent.End.DateTime,
                htmlLink = createdEvent.HtmlLink
            };
        }
        catch (Exception ex)
        {
            return new { success = false, error = ex.Message };
        }
    }

    public async Task<object> ListEventsAsync(object parameters)
    {
        try
        {
            var service = await GetServiceAsync();
            var paramDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
                System.Text.Json.JsonSerializer.Serialize(parameters));

            var request = service.Events.List("primary");
            request.TimeMin = paramDict!.ContainsKey("timeMin") 
                ? DateTime.Parse(paramDict["timeMin"].ToString()!) 
                : DateTime.Now;
            
            if (paramDict.ContainsKey("timeMax"))
                request.TimeMax = DateTime.Parse(paramDict["timeMax"].ToString()!);
            
            request.ShowDeleted = false;
            request.SingleEvents = true;
            request.MaxResults = paramDict.ContainsKey("maxResults") 
                ? Convert.ToInt32(paramDict["maxResults"]) 
                : 10;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            var events = await request.ExecuteAsync();

            return new
            {
                success = true,
                events = events.Items.Select(e => new
                {
                    id = e.Id,
                    summary = e.Summary,
                    description = e.Description,
                    location = e.Location,
                    start = e.Start.DateTime ?? DateTime.Parse(e.Start.Date),
                    end = e.End.DateTime ?? DateTime.Parse(e.End.Date),
                    htmlLink = e.HtmlLink
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            return new { success = false, error = ex.Message };
        }
    }

    public async Task<object> UpdateEventAsync(object parameters)
    {
        try
        {
            var service = await GetServiceAsync();
            var paramDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
                System.Text.Json.JsonSerializer.Serialize(parameters));

            var eventId = paramDict!["eventId"].ToString();
            var eventItem = await service.Events.Get("primary", eventId).ExecuteAsync();

            // Actualizar solo los campos proporcionados
            if (paramDict.ContainsKey("summary"))
                eventItem.Summary = paramDict["summary"].ToString();
            
            if (paramDict.ContainsKey("description"))
                eventItem.Description = paramDict["description"].ToString();
            
            if (paramDict.ContainsKey("location"))
                eventItem.Location = paramDict["location"].ToString();
            
            if (paramDict.ContainsKey("start"))
                eventItem.Start.DateTime = DateTime.Parse(paramDict["start"].ToString()!);
            
            if (paramDict.ContainsKey("end"))
                eventItem.End.DateTime = DateTime.Parse(paramDict["end"].ToString()!);

            var request = service.Events.Update(eventItem, "primary", eventId);
            var updatedEvent = await request.ExecuteAsync();

            return new
            {
                success = true,
                eventId = updatedEvent.Id,
                summary = updatedEvent.Summary,
                start = updatedEvent.Start.DateTime,
                end = updatedEvent.End.DateTime
            };
        }
        catch (Exception ex)
        {
            return new { success = false, error = ex.Message };
        }
    }

    public async Task<object> DeleteEventAsync(object parameters)
    {
        try
        {
            var service = await GetServiceAsync();
            var paramDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
                System.Text.Json.JsonSerializer.Serialize(parameters));

            var eventId = paramDict!["eventId"].ToString();
            await service.Events.Delete("primary", eventId).ExecuteAsync();

            return new { success = true, message = "Evento eliminado exitosamente" };
        }
        catch (Exception ex)
        {
            return new { success = false, error = ex.Message };
        }
    }
}

public static class CalendarTools
{
    // Esquemas para las herramientas
    public static object CreateEventSchema => new
    {
        type = "object",
        properties = new
        {
            summary = new { type = "string", description = "Título del evento" },
            description = new { type = "string", description = "Descripción del evento" },
            start = new { type = "string", description = "Fecha y hora de inicio (ISO 8601)" },
            end = new { type = "string", description = "Fecha y hora de fin (ISO 8601)" },
            location = new { type = "string", description = "Ubicación del evento" }
        },
        required = new[] { "summary", "start", "end" }
    };

    public static object ListEventsSchema => new
    {
        type = "object",
        properties = new
        {
            maxResults = new { type = "integer", description = "Número máximo de eventos a retornar", default_ = 10 },
            timeMin = new { type = "string", description = "Fecha mínima (ISO 8601)" },
            timeMax = new { type = "string", description = "Fecha máxima (ISO 8601)" }
        }
    };

    public static object UpdateEventSchema => new
    {
        type = "object",
        properties = new
        {
            eventId = new { type = "string", description = "ID del evento a actualizar" },
            summary = new { type = "string", description = "Nuevo título del evento" },
            description = new { type = "string", description = "Nueva descripción del evento" },
            start = new { type = "string", description = "Nueva fecha y hora de inicio (ISO 8601)" },
            end = new { type = "string", description = "Nueva fecha y hora de fin (ISO 8601)" },
            location = new { type = "string", description = "Nueva ubicación del evento" }
        },
        required = new[] { "eventId" }
    };

    public static object DeleteEventSchema => new
    {
        type = "object",
        properties = new
        {
            eventId = new { type = "string", description = "ID del evento a eliminar" }
        },
        required = new[] { "eventId" }
    };

    // Handlers para las herramientas
    public static async Task<object> CreateEventHandler(object parameters, IServiceProvider services)
    {
        var calendarService = services.GetRequiredService<GoogleCalendarService>();
        return await calendarService.CreateEventAsync(parameters);
    }

    public static async Task<object> ListEventsHandler(object parameters, IServiceProvider services)
    {
        var calendarService = services.GetRequiredService<GoogleCalendarService>();
        return await calendarService.ListEventsAsync(parameters);
    }

    public static async Task<object> UpdateEventHandler(object parameters, IServiceProvider services)
    {
        var calendarService = services.GetRequiredService<GoogleCalendarService>();
        return await calendarService.UpdateEventAsync(parameters);
    }

    public static async Task<object> DeleteEventHandler(object parameters, IServiceProvider services)
    {
        var calendarService = services.GetRequiredService<GoogleCalendarService>();
        return await calendarService.DeleteEventAsync(parameters);
    }
}
