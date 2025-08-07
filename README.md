# Nova - Servidor MCP para Google Calendar

Un servidor Model Context Protocol (MCP) que proporciona herramientas para interactuar con Google Calendar.

## Configuración de Google Calendar API

### 1. Crear un proyecto en Google Cloud Console

1. Ve a [Google Cloud Console](https://console.cloud.google.com/)
2. Crea un nuevo proyecto o selecciona uno existente
3. Habilita la API de Google Calendar:
   - Ve a "APIs & Services" > "Library"
   - Busca "Google Calendar API"
   - Haz clic en "Enable"

### 2. Crear credenciales OAuth 2.0

1. Ve a "APIs & Services" > "Credentials"
2. Haz clic en "Create Credentials" > "OAuth client ID"
3. Si es la primera vez, configura la pantalla de consentimiento OAuth:
   - Ve a "OAuth consent screen"
   - Selecciona "External" (a menos que tengas Google Workspace)
   - Completa la información requerida:
     - App name: "Nova MCP Server"
     - User support email: tu email
     - Developer contact information: tu email
   - Agrega los siguientes scopes:
     - `https://www.googleapis.com/auth/calendar`
   - Agrega tu email como usuario de prueba
4. Vuelve a "Credentials" y crea el OAuth client ID:
   - Application type: "Desktop application"
   - Name: "Nova MCP Client"
5. Descarga el archivo JSON de credenciales
6. Renómbralo a `credentials.json` y colócalo en la raíz del proyecto

### 3. Configuración del proyecto

```bash
# Restaurar paquetes NuGet
dotnet restore

# Construir el proyecto
dotnet build
```

### 4. Ejecutar el servidor

```bash
dotnet run
```

La primera vez que ejecutes el servidor, se abrirá un navegador para autorizar la aplicación. Acepta los permisos y el servidor guardará el token de acceso en la carpeta `tokens/`.

## Herramientas disponibles

### create_event
Crea un nuevo evento en Google Calendar.

**Parámetros:**
- `summary` (requerido): Título del evento
- `start` (requerido): Fecha y hora de inicio (formato ISO 8601)
- `end` (requerido): Fecha y hora de fin (formato ISO 8601)
- `description` (opcional): Descripción del evento
- `location` (opcional): Ubicación del evento

**Ejemplo:**
```json
{
  "summary": "Reunión de equipo",
  "description": "Reunión semanal del equipo de desarrollo",
  "start": "2025-08-08T10:00:00",
  "end": "2025-08-08T11:00:00",
  "location": "Sala de conferencias A"
}
```

### list_events
Lista eventos del calendario.

**Parámetros:**
- `maxResults` (opcional): Número máximo de eventos (default: 10)
- `timeMin` (opcional): Fecha mínima (formato ISO 8601)
- `timeMax` (opcional): Fecha máxima (formato ISO 8601)

### update_event
Actualiza un evento existente.

**Parámetros:**
- `eventId` (requerido): ID del evento a actualizar
- `summary` (opcional): Nuevo título
- `description` (opcional): Nueva descripción
- `start` (opcional): Nueva fecha de inicio
- `end` (opcional): Nueva fecha de fin
- `location` (opcional): Nueva ubicación

### delete_event
Elimina un evento.

**Parámetros:**
- `eventId` (requerido): ID del evento a eliminar

## Estructura del proyecto

```
nova/
├── Program.cs              # Punto de entrada del servidor MCP
├── Tools.cs               # Herramientas y servicio de Google Calendar
├── nova.csproj            # Configuración del proyecto
├── credentials.json       # Credenciales de Google (no incluir en git)
├── credentials.json.example # Ejemplo de estructura de credenciales
├── tokens/                # Tokens de OAuth (no incluir en git)
└── README.md              # Este archivo
```

## Notas importantes

- El archivo `credentials.json` contiene información sensible. **NO** lo incluyas en el control de versiones.
- La carpeta `tokens/` también contiene información sensible. Agrégala al `.gitignore`.
- La primera ejecución requerirá autorización manual a través del navegador.
- Los eventos se crean en el calendario principal del usuario autenticado.

## Solución de problemas

### Error de credenciales
- Verifica que el archivo `credentials.json` esté en la raíz del proyecto
- Asegúrate de que las credenciales sean para una aplicación de escritorio
- Verifica que la API de Google Calendar esté habilitada en tu proyecto

### Error de autorización
- Borra la carpeta `tokens/` y vuelve a autorizar
- Verifica que tu email esté en la lista de usuarios de prueba
- Asegúrate de haber configurado correctamente la pantalla de consentimiento OAuth
