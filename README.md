# SmartSupport Retail

> 🎤 **DEMO para .NET Conf 2025 - Corrientes, Argentina**  
> Este proyecto fue creado como demostración para una charla presentada en la conferencia .NET Conf 2025 realizada en Corrientes, Argentina.

Sistema de soporte inteligente que utiliza LLM (Large Language Models) y RAG (Retrieval Augmented Generation) para proporcionar respuestas contextualizadas sobre políticas de envío, devoluciones y soporte al cliente.

## 🚀 Características

- **Asistencia Inteligente**: Procesa consultas de clientes utilizando LLM con contexto enriquecido
- **RAG Multi-Fuente**: Integra información de:
  - **PDF**: Extracción de texto de documentos de políticas
  - **SQL**: Consultas a base de datos de pedidos y clientes
  - **API Externa**: Información de tracking y servicios externos
- **Interfaz Web Moderna**: Cliente Blazor WebAssembly con prompts prefijados
- **Documentación API**: Swagger UI integrado para probar los endpoints
- **Arquitectura Modular**: Separación clara de responsabilidades con múltiples proyectos

## 📋 Requisitos Previos

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) o superior
- SQL Server (LocalDB recomendado para desarrollo) o SQL Server Express
- Visual Studio 2022 / Visual Studio Code / JetBrains Rider (opcional)

## 🏗️ Arquitectura del Proyecto

El proyecto está organizado en 4 proyectos principales:

```
SmartSupport/
├── src/
│   ├── SmartSupport.API/              # API principal (Backend)
│   ├── SmartSupport.ExternalService/  # Servicio externo de tracking
│   ├── SmartSupport.ExternalData/     # Acceso a datos (Entity Framework)
│   └── SmartSupport.WebClient/        # Cliente web (Blazor WebAssembly)
└── resources/                          # Recursos (PDFs, SQL de prueba)
```

### Proyectos

#### 1. SmartSupport.API
API principal que expone los endpoints de asistencia inteligente.

**Tecnologías:**
- ASP.NET Core 10.0
- Entity Framework Core
- Swashbuckle (Swagger)
- iText (extracción de PDF)
- Integración con Google Gemini LLM

**Endpoints principales:**
- `POST /assist/query` - Procesa consultas con archivo PDF
- `GET /assist/models` - Lista modelos disponibles de LLM

**Swagger UI:** `https://localhost:7086/swagger` (en desarrollo)

#### 2. SmartSupport.ExternalService
Servicio externo simulado que proporciona información de tracking de paquetes.

**Endpoints:**
- `GET /tracking/{trackingNumber}?mode={mode}` - Obtiene estado de tracking

**Swagger UI:** `https://localhost:7073/swagger` (en desarrollo)

#### 3. SmartSupport.ExternalData
Capa de acceso a datos con Entity Framework Core.

**Entidades:**
- `Customer` - Información de clientes
- `Order` - Pedidos
- `OrderEvent` - Eventos de pedidos
- `Ticket` - Tickets de soporte

#### 4. SmartSupport.WebClient
Cliente web desarrollado en Blazor WebAssembly.

**Características:**
- Interfaz de usuario moderna con Bootstrap
- Prompts prefijados para facilitar consultas
- Integración con la API principal
- Visualización de respuestas y citas (sources)

## 🛠️ Instalación y Configuración

### 1. Clonar el repositorio

```bash
git clone <repository-url>
cd netConf2025
```

### 2. Restaurar dependencias

```bash
cd src
dotnet restore
```

### 3. Configurar la base de datos

El proyecto usa SQL Server LocalDB por defecto. La base de datos se creará automáticamente al ejecutar la aplicación (migraciones automáticas).

**Connection String (appsettings.json):**
```json
{
  "ConnectionStrings": {
    "ExternalData": "Server=(localdb)\\MSSQLLocalDB;Database=SmartSupportDemo;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

### 4. Configurar LLM (Opcional)

Por defecto, el proyecto está configurado para usar el modo **Mock** del LLM. Para usar Google Gemini:

1. Obtener una API Key de [Google AI Studio](https://makersuite.google.com/app/apikey)
2. Configurar en `appsettings.json`:

```json
{
  "LLM": {
    "Provider": "Gemini",
    "Mock": false,
    "BaseUrl": "https://generativelanguage.googleapis.com",
    "Model": "gemini-1.5-flash",
    "ApiKey": "tu-api-key-aqui"
  }
}
```

O usar variable de entorno:
```bash
export GEMINI_API_KEY=tu-api-key-aqui
```

## 🚀 Ejecución

### Ejecutar todos los proyectos

Abre la solución en Visual Studio o ejecuta cada proyecto en una terminal separada:

#### Terminal 1: API Principal
```bash
cd src/SmartSupport.API
dotnet run
```
**URLs:**
- HTTPS: `https://localhost:7086`
- Swagger: `https://localhost:7086/swagger`

#### Terminal 2: External Service
```bash
cd src/SmartSupport.ExternalService
dotnet run
```
**URLs:**
- HTTPS: `https://localhost:7073`
- Swagger: `https://localhost:7073/swagger`

#### Terminal 3: Web Client
```bash
cd src/SmartSupport.WebClient
dotnet run
```
**URL:** `https://localhost:7248` o `http://localhost:5052`

### Ejecutar desde Visual Studio

1. Abre `src/SmartSupport.sln`
2. Configura múltiples proyectos de inicio:
   - SmartSupport.API
   - SmartSupport.ExternalService
   - SmartSupport.WebClient
3. Presiona F5 o ejecuta con Ctrl+F5

## 📚 Uso

### 1. Usar el Cliente Web

1. Navega a `https://localhost:7248/assistant`
2. Sube un archivo PDF (ej: `resources/Políticas de Envío y Devoluciones 2025.pdf`)
3. Selecciona un prompt prefijado o escribe tu propia consulta
4. Opcionalmente, proporciona un número de pedido (ej: `AT-1003`)
5. Habilita las opciones de RAG según necesites:
   - **Usar DB (RAG SQL)**: Incluye información de la base de datos
   - **Usar API externa (RAG API)**: Incluye información de tracking
6. Haz clic en "Consultar"

### 2. Probar la API con Swagger

1. Navega a `https://localhost:7086/swagger`
2. Expande el endpoint `POST /assist/query`
3. Haz clic en "Try it out"
4. Completa el formulario multipart:
   - `prompt`: Tu pregunta
   - `file`: Archivo PDF
   - `orderNumber`: (opcional) Número de pedido
   - `useSqlRag`: `true` o `false`
   - `useApiRag`: `true` o `false`
5. Haz clic en "Execute"

### 3. Ejemplos de Prompts

- **Estado de pedido**: "¿Cuándo llega mi pedido AT-1003?"
- **Políticas**: "¿Cuántos días tengo para devolver un producto?"
- **Producto dañado**: "¿Qué hago si mi pedido llega dañado?"
- **Tracking**: "¿Dónde está mi pedido con tracking 1Z999SMART?"

## 🔧 Configuración Avanzada

### Variables de Entorno

Puedes configurar valores mediante variables de entorno:

```bash
export GEMINI_API_KEY=tu-api-key
export ASPNETCORE_ENVIRONMENT=Development
```

### CORS

El API está configurado para aceptar peticiones desde:
- `https://localhost:7248`
- `http://localhost:5052`

Para agregar otros orígenes, edita `Program.cs` en `SmartSupport.API`.

## 📖 Estructura de Datos

### Entidades Principales

- **Customer**: Clientes del sistema
- **Order**: Pedidos con información de tracking
- **OrderEvent**: Historial de eventos de pedidos
- **Ticket**: Tickets de soporte

### Datos de Prueba

El proyecto incluye datos de seed para pruebas:
- Cliente: `juan@example.com` (Juan Pérez)
- Pedido: `AT-1003` con tracking `1Z999SMART`
- Tracking especial: `1Z999SMART` con diferentes modos de respuesta

## 🧪 Testing

### Probar el Tracking Service

El servicio de tracking tiene un número especial para pruebas: `1Z999SMART`

Puedes usar el parámetro `mode` para diferentes escenarios:
- `?mode=delayed` - Simula un pedido retrasado
- `?mode=in_transit` - Simula un pedido en tránsito
- (sin mode) - Simula un pedido "out for delivery"

Ejemplo:
```bash
GET https://localhost:7073/tracking/1Z999SMART?mode=delayed
```

## 📦 Dependencias Principales

### SmartSupport.API
- `Microsoft.EntityFrameworkCore.SqlServer` (10.0.0)
- `Swashbuckle.AspNetCore` (6.11.1)
- `itext` (9.4.0)

### SmartSupport.ExternalService
- `Swashbuckle.AspNetCore` (6.11.1)

### SmartSupport.WebClient
- Blazor WebAssembly
- Bootstrap 5

## 🤝 Contribución

1. Fork el proyecto
2. Crea una rama para tu feature (`git checkout -b feature/AmazingFeature`)
3. Commit tus cambios (`git commit -m 'Add some AmazingFeature'`)
4. Push a la rama (`git push origin feature/AmazingFeature`)
5. Abre un Pull Request

## 📝 Licencia

Este proyecto es una demostración técnica y no incluye licencia comercial.

## 👨‍💻 Autor

Este proyecto fue desarrollado como **DEMO** para una charla presentada en la **.NET Conf 2025** realizada en **Corrientes, Argentina**. El proyecto demuestra el uso de tecnologías modernas de .NET como Blazor WebAssembly, ASP.NET Core, Entity Framework Core, integración con LLM y RAG (Retrieval Augmented Generation).

## 🙏 Agradecimientos

- Google Gemini para el LLM
- .NET Foundation
- Comunidad de Blazor

---

Para más información o soporte, consulta la documentación en Swagger UI o revisa el código fuente.

