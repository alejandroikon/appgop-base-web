# Modelo de Datos — Creación de Pozo Nuevo V2.0

**Referencia:** `spec.md` RN-01 a RN-40

---

## 1. Entidad Principal: Well (Pozo)

```csharp
// GOP.Domain/Entities/Well.cs
public class Well : AuditableEntity  // hereda Id, CreatedAt, CreatedBy, LastModifiedAt, etc.
{
    // ─── Identificación ──────────────────────────────────────
    public string NombrePozo { get; private set; }     // Generado: {Campo}-{Denominacion}-{Consecutivo}
    public string? Uwi { get; private set; }           // Null si BORRADOR, generado al Finalizar
    public string Operadora { get; private set; }      // TenantName (denorm, RN-01)
    
    // ─── Contrato ────────────────────────────────────────────
    public int ContratoId { get; private set; }        // FK
    public string Contrato { get; private set; }       // Denorm
    public string TipoContrato { get; private set; }   // Denorm (RN-03)
    public string Cuenca { get; private set; }         // Denorm (RN-04)
    
    // ─── Campo ───────────────────────────────────────────────
    public int? CampoId { get; private set; }          // Nullable: Exploratorio/Estratigráfico (RN-11,13)
    public string? Campo { get; private set; }         // Denorm
    
    // ─── Datos Técnicos ──────────────────────────────────────
    public string Denominacion { get; private set; }   // MAYÚSCULAS (RN-07, RN-20)
    public int Consecutivo { get; private set; }       // 1–9999 (RN-08)
    public TipoTrayectoria TipoTrayectoria { get; private set; }
    public Clasificacion Clasificacion { get; private set; }
    public SubClasificacionExploratoria? SubClasificacion { get; private set; } // Null si no Exploratorio
    public TipoUbicacion TipoUbicacion { get; private set; }
    public TipoAngulo TipoAngulo { get; private set; }
    public TipoObjetivo TipoObjetivo { get; private set; }
    public TipoTerminacion TipoTerminacion { get; private set; }
    
    // ─── Ubicación Geográfica ────────────────────────────────
    public int? DepartamentoId { get; private set; }   // Nullable para borrador parcial
    public string? Departamento { get; private set; }
    public string? CodigoDaneDpto { get; private set; } // 2 dígitos (RN-28)
    public int? MunicipioId { get; private set; }
    public string? Municipio { get; private set; }
    public string? CodigoDaneMpio { get; private set; } // 3 dígitos (RN-29)
    public int? ClusterId { get; private set; }
    public string? Cluster { get; private set; }
    
    // ─── Estado ──────────────────────────────────────────────
    public WellStatus Estado { get; private set; }     // BORRADOR | CREADO
    public bool Forma101Radicada { get; private set; } // Flag de bloqueo (RN-40)
    
    // ─── Multi-Tenancy ───────────────────────────────────────
    public int TenantId { get; private set; }          // Auto del JWT (RN-01)
    
    // ─── Soft Delete (heredado de AuditableEntity) ───────────
    // public bool IsDeleted { get; set; }
    // public DateTime? DeletedAt { get; set; }
}
```

---

## 2. Enums

### 2.1. WellStatus

```csharp
public enum WellStatus
{
    Borrador,   // Datos parciales, sin UWI
    Creado      // Datos completos, UWI generado
}
```

> **Cambio vs iteraciones 005-007:** Se eliminan `PendingUwi`, `ReadyFiscal`, `Fiscalizado`. El flujo V2.0 es Borrador → Creado. El bloqueo post-Forma-101 se controla con el flag `Forma101Radicada`, no con estados adicionales.

### 2.2. TipoTrayectoria

```csharp
public enum TipoTrayectoria
{
    ST,  // Side Track
    P,   // Piloto
    PR,  // Profundización
    ML,  // Multilateral
    G,   // Gemelo
    O    // Original
}
```

### 2.3. Clasificacion

```csharp
public enum Clasificacion
{
    Exploratorio,
    Desarrollo,
    Estratigrafico
}
```

### 2.4. SubClasificacionExploratoria (NUEVO V2.0)

```csharp
public enum SubClasificacionExploratoria
{
    A3,   // Área nueva (wildcat)
    A2a,  // Yacimiento nuevo, campo nuevo
    A2b,  // Yacimiento nuevo, campo conocido
    A2c,  // Yacimiento conocido, campo conocido
    A1    // Pozo de Avanzada (appraisal)
}
```

### 2.5. TipoUbicacion

```csharp
public enum TipoUbicacion
{
    Continental,
    CostaFuera
}
```

### 2.6. TipoAngulo

```csharp
public enum TipoAngulo
{
    H,  // Horizontal
    V,  // Vertical
    D   // Desviado
}
```

### 2.7. TipoObjetivo (ACTUALIZADO V2.0)

```csharp
public enum TipoObjetivo
{
    PH,  // Producción de Hidrocarburos
    I,   // Inyección
    M,   // Monitoreo
    D,   // Disposición
    C,   // Captación         ← NUEVO V2.0
    GT,  // Geotérmico        ← NUEVO V2.0
    O    // Otro              ← NUEVO V2.0
}
```

### 2.8. TipoTerminacion

```csharp
public enum TipoTerminacion
{
    CD,  // Casing y Cementación
    LC,  // Liner Cementado
    LR,  // Liner con Ranuras
    GP,  // Gravel Pack
    CC,  // Completación Compuesta
    OH,  // Hoyo Abierto (RN-23 advertencia)
    O    // Otro
}
```

---

## 3. Value Object: UWI

```csharp
// GOP.Domain/ValueObjects/Uwi.cs
public sealed class Uwi : ValueObject
{
    public string Value { get; }
    
    // Componentes desglosados para auditoría
    public string DptoCode { get; }      // 2 dígitos
    public string MpioCode { get; }      // 3 dígitos
    public string Sigla { get; }         // 4 chars
    public string Numero { get; }        // 4 dígitos
    public string ClusterCode { get; }   // 6 chars (2α+4n)
    public string AnguloCode { get; }    // 1 char
    public string TrayectoriaCode { get; } // variable
    public string ObjetivoCode { get; }  // variable
    public string TerminacionCode { get; } // variable
    
    // Factory method que implementa el algoritmo PPDM
    public static Result<Uwi> Generate(
        string codigoDaneDpto,
        string codigoDaneMpio,
        string denominacion,
        int consecutivo,
        string? clusterNombre,
        TipoAngulo angulo,
        TipoTrayectoria trayectoria,
        TipoObjetivo objetivo,
        TipoTerminacion terminacion,
        bool isAnh);
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
```

---

## 4. Entidades Catálogo

### 4.1. Contrato

```csharp
public class Contrato : Entity
{
    public string Nombre { get; set; }
    public string Tipo { get; set; }          // E&P, TEA, Convenio, etc.
    public string Cuenca { get; set; }
    public TipoUbicacion UbicacionDefault { get; set; } // Continental/CostaFuera
    public int OperadoraId { get; set; }      // Tenant que posee el contrato
    
    // Navegación
    public ICollection<Campo> Campos { get; set; }
}
```

### 4.2. Campo

```csharp
public class Campo : Entity
{
    public string Nombre { get; set; }
    public int ContratoId { get; set; }
    
    // Navegación
    public Contrato Contrato { get; set; }
    public ICollection<Cluster> Clusters { get; set; }
}
```

### 4.3. Departamento

```csharp
public class Departamento : Entity
{
    public string Nombre { get; set; }
    public string CodigoDane { get; set; }    // 2 dígitos
    
    // Navegación
    public ICollection<Municipio> Municipios { get; set; }
}
```

### 4.4. Municipio

```csharp
public class Municipio : Entity
{
    public string Nombre { get; set; }
    public int DepartamentoId { get; set; }
    public string CodigoDane { get; set; }    // 5 dígitos (dpto+mpio)
    
    // Navegación
    public Departamento Departamento { get; set; }
}
```

### 4.5. Cluster

```csharp
public class Cluster : Entity
{
    public string Nombre { get; set; }
    public string Abreviatura { get; set; }   // 2 chars (para UWI)
    public int CampoId { get; set; }
    
    // Navegación
    public Campo Campo { get; set; }
}
```

---

## 5. Diagrama de Relaciones

```
┌──────────────┐
│   Operadora   │ (Tenant — no es entidad propia, viene del JWT)
│   tenantId    │
└──────┬───────┘
       │ 1:N
┌──────┴───────┐     ┌──────────────┐
│   Contrato    │────▶│    Cuenca     │ (campo denormalizado en Contrato)
│   id, nombre  │     └──────────────┘
│   tipo, cuenca│
└──────┬───────┘
       │ 1:N
┌──────┴───────┐
│    Campo      │
│   id, nombre  │
└──────┬───────┘
       │ 1:N
┌──────┴───────┐
│   Cluster     │
│ id, nombre,   │
│ abreviatura   │
└──────────────┘

┌──────────────┐
│ Departamento  │
│ id, nombre,   │
│ codigoDane(2) │
└──────┬───────┘
       │ 1:N
┌──────┴───────┐
│  Municipio    │
│ id, nombre,   │
│ codigoDane(5) │
└──────────────┘

┌──────────────┐
│     Well      │──FK──▶ Contrato, Campo?, Departamento?, Municipio?, Cluster?
│  (agregado)   │
│  uwi (VO)     │
│  estado       │
│  tenantId     │
└──────────────┘
```

---

## 6. Diferencias vs Modelo Anterior (Iters 005-007)

| Aspecto | Modelo anterior | Modelo V2.0 |
|---------|----------------|-------------|
| WellStatus | 4 estados (BORRADOR, PENDING_UWI, READY_FISCAL, FISCALIZADO) | 2 estados (BORRADOR, CREADO) + flag `Forma101Radicada` |
| UWI | Generado por backend en transición ENVIAR. Formato: `CO-{dpto}-{mpio}-{denom}-{consec}-{tray}` | Generado al Finalizar. Formato PPDM completo. |
| SubClasificación | No existía | `SubClasificacionExploratoria` (A3, A2a, A2b, A2c, A1) |
| TipoObjetivo | 4 valores (PH, I, M, D) | 7 valores (+C, +GT, +O) |
| Cluster.Abreviatura | No existía | 2 chars para codificación UWI |
| Campo nullable | Según flujo de estado | Según Clasificación (RN-11, RN-13) |
| WellTransitionHistory | Entidad de historial de transiciones | **Se mantiene** para el historial de ediciones futuro |
| Denominación padding | No aplicaba | MAYÚSCULAS + padding a 4 para sigla UWI |
| Consecutivo | 2 dígitos (01-99) | Sin límite de dígitos en entrada, padding a 4 en UWI (1-9999) |
