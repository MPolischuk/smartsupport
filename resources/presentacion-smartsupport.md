### 1) Título y contexto
- **SmartSupport: Asistente de Soporte con RAG + Gemini**
- Objetivo: responder consultas de soporte combinando PDF, DB y API con un LLM
- Presentador: Nombre, Rol, Empresa

### 2) Problema a resolver
- **Por qué importa**
  - Alto esfuerzo para “navegar” fuentes: PDF (políticas), DB (órdenes/tickets), APIs (tracking)
  - KPIs afectados: **AHT** alto, **FCR** bajo, **CSAT** resentido, tiempos de espera largos
  - Conocimiento fragmentado/desactualizado → respuestas **inconsistentes** entre agentes
  - Necesidad de **trazabilidad/compliance**: citar la política o el registro exacto
  - Escalabilidad: onboarding de agentes y picos (campañas, fechas clave)
- **Casos de dolor típicos**
  - “¿Cuándo llega mi pedido?”: política estándar vs. ETA real del tracking
  - “Llegó dañado”: ventanas de reporte, evidencia (fotos), reemplazo/reembolso
  - “Cambio de dirección/entrega”: restricciones, tiempos, costos
  - “Retrasos por transportista”: SLA por carrier y comunicación al cliente
- **Métricas objetivo (POC)**
  - **-20–40%** en AHT, **+10–20 pp** en FCR, **+CSAT**, reducción de escalaciones

### 3) Propuesta de solución
- **Qué es**
  - Asistente que **orquesta**: PDF (políticas) + **SQL** (órdenes/tickets) + **API** (tracking) + **Gemini**
- **Por qué funciona**
  - Usa **evidencias** con prioridad: **API > SQL > PDF** y devuelve **citas** verificables
  - Formato de salida consistente (estructura `AssistResponse`) y limpieza de respuestas LLM
  - Manejo de errores/compatibilidad de modelo (ListModels, fallback `v1`/`v1beta`)
- **Casos de uso (POC)**
  - Estado de pedido y **ETA**; **producto dañado** y devoluciones; tracking; FAQs
- **Beneficios esperados**
  - Respuestas **más rápidas**, **consistentes**, con **trazabilidad**, menor dependencia de expertos
- **Alcance y limitaciones POC**
  - Extracción de texto de PDF (sin OCR/tablas avanzadas), facts limitados, español, sin side effects en sistemas
- **Siguientes pasos**
  - Embeddings/chunking, acciones (tool use), guardrails y analítica de uso

### 4) Arquitectura (alto nivel)
- WebClient (Blazor WASM)
- API .NET: Orquestador + Providers (PDF/SQL/API) + Cliente Gemini
- ExternalData (EF Core / SQL Server) + ExternalService (HTTP)
- Gemini (Google Generative Language API)

### 5) Flujo de consulta
- Subir PDF → extraer texto (iText)
- Opcional: obtener facts de SQL y de API (con citas)
- Orquestador construye prompt → Gemini → respuesta
- Limpieza y mapeo → `AssistResponse` → UI (respuesta + citas)

### 6) Lecciones con Gemini
- Descubrimiento de modelos con ListModels, fallback `v1`/`v1beta`
- Normalizar payload y manejar 400/404 (modelo/versión)
- Respuestas con fences ``` y JSON interno: limpieza y parsing robusto

### 7) Extracción de PDF (iText)
- Lectura página a página, `SimpleTextExtractionStrategy`
- Límites de tamaño y soporte de cancelación
- Base sólida para POC; siguiente paso: OCR/tablas si aplica

### 8) UI y experiencia
- Overlay de loading bloqueante (accesible)
- Acordeones (Bootstrap) para fuentes: PDF/SQL/API
- Errores mostrados con cuerpo de respuesta para diagnóstico

### 9) Roadmap
- Mejorar parsing PDF (tablas/estructuras), chunking y embeddings
- Guardrails/validaciones, acciones (tool use) y observabilidad
- Feedback loop y métricas (latencia/tokens/éxito)

### 10) Plan de demo
- Caso base: PDF + prompt (sin SQL/API)
- Activar SQL y/o API y ver impacto en respuesta y citas
- Cierre: límites actuales y próximos pasos

---

### Guía para IA de diagramas (Arquitectura de SmartSupport)

Usa esta especificación para pedir a una IA de diagramas (p. ej. que genere un diagrama de arquitectura tipo contenedor/flujo):

```text
Objetivo: Diagrama de arquitectura de la solución SmartSupport (alto nivel), con agrupaciones y flujo de datos.

Estilo deseado:
- Disposición de izquierda a derecha o de arriba abajo, clara y minimalista
- Agrupar por dominios: Cliente, Backend/API, Fuentes de datos, LLM
- Usar iconos simples o formas consistentes; colores suaves (2–3 tonos)

Elementos (nodos):
- Usuario (Navegador)
- WebClient (Blazor WASM)
- SmartSupport.API (.NET)
  - AssistOrchestrator
  - PdfTextExtractor (iText)
  - SqlRagProvider (EF Core)
  - ApiRagProvider (HTTP)
  - GeminiLlmClient
- ExternalData DB (SQL Server)
- ExternalService (HTTP Tracking API)
- Recurso PDF (archivo subido)
- Gemini (Google Generative Language API)

Relaciones (aristas) con etiquetas:
- Usuario → WebClient: "Interacción UI (prompt + PDF)"
- WebClient → SmartSupport.API: "POST /assist/query (multipart)"
- SmartSupport.API → PdfTextExtractor: "Extraer texto"
- SmartSupport.API → ExternalData DB: "EF Core (facts)"
- SmartSupport.API → ExternalService: "HTTP (tracking facts)"
- SmartSupport.API → Gemini: "/models/{model}:generateContent"
- Gemini → SmartSupport.API: "Respuesta (texto)"
- SmartSupport.API → WebClient: "JSON AssistResponse (respuesta + citas)"

Agrupaciones (clusters):
- Cluster Cliente: Usuario, WebClient
- Cluster Backend/API: SmartSupport.API y sus componentes internos (orquestador, providers, LLM client)
- Cluster Fuentes: ExternalData DB, ExternalService, Recurso PDF
- Cluster LLM: Gemini

Anotaciones:
- Prioridad de evidencias: API > SQL > PDF
- Manejo de modelos y fallback (v1/v1beta)
- Limpieza de contenido (fences) y mapeo a AssistResponse

Salida: Diagrama con flechas dirigidas, etiquetas visibles y clusters con títulos.
```

Opcional: puedes pedir una versión de secuencia (sequence diagram) para el flujo de `/assist/query`.


