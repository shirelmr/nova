using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol;
using Nova;

var builder = Host.CreateApplicationBuilder(args);

// Configurar servicios
builder.Services.AddSingleton<GoogleCalendarService>();

var host = builder.Build();

// Crear el servidor MCP
var mcpServer = new McpServer(host.Services);

// Agregar herramientas de calendario
mcpServer.AddTool("create_event", "Crear un evento en Google Calendar", CalendarTools.CreateEventSchema, CalendarTools.CreateEventHandler);
mcpServer.AddTool("list_events", "Listar eventos del calendario", CalendarTools.ListEventsSchema, CalendarTools.ListEventsHandler);
mcpServer.AddTool("update_event", "Actualizar un evento existente", CalendarTools.UpdateEventSchema, CalendarTools.UpdateEventHandler);
mcpServer.AddTool("delete_event", "Eliminar un evento", CalendarTools.DeleteEventSchema, CalendarTools.DeleteEventHandler);

// Ejecutar el servidor
await mcpServer.RunAsync();
