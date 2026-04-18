# Algoritmo de Generación de UWI Fiscalizado — PPDM

**Referencia:** `docs/source/INSTRUCTIVO_UWI_03032026.pdf`
**Reglas de negocio:** RN-27 a RN-38 en `spec.md`
**Vigencia legal:** A partir del 1-jun-2026, solo GOP genera UWIs (RN-39)

---

## 1. Estructura General

```
UWI = [DptoDANE][MpioDANE][Sigla][Número][Cluster][Ángulo][Trayectoria][Objetivo]-[Terminación]
       ├──2──┤├───3───┤├──4──┤├──4──┤├──6──┤├─1─┤├──var──┤├──var──┤ ├──var──┤
```

| Segmento | Posición | Longitud | Fuente | Regla |
|----------|----------|----------|--------|-------|
| DptoDANE | 1-2 | 2 fija | Código DANE Departamento | RN-28 |
| MpioDANE | 3-5 | 3 fija | Código DANE Municipio (parte municipal) | RN-29 |
| Sigla | 6-9 | 4 fija | Denominación del pozo | RN-30 |
| Número | 10-13 | 4 fija | Consecutivo con zero-padding | RN-31 |
| Cluster | 14-19 | 6 fija | Cluster/Locación: 2α + 4n | RN-32 |
| Ángulo | 20 | 1 fija | Tipo por Ángulo: H/V/D | RN-33 |
| Trayectoria | 21+ | variable | Tipo por Trayectoria | RN-34 |
| Objetivo | var | variable | Tipo por Objetivo | RN-35 |
| `-` | var | 1 fija | Separador | RN-36 |
| Terminación | var | variable | Tipo por Terminación | RN-36 |

**Longitud total:** 20 caracteres fijos + segmentos variables (trayectoria + objetivo + guión + terminación). Mínimo ~23 caracteres, máximo ~30 caracteres.

---

## 2. Detalle por Segmento

### 2.1. DptoDANE (RN-28)

- **Longitud:** 2 dígitos, fija.
- **Fuente:** Código DANE del Departamento seleccionado en Sección 3.
- **Padding:** zero-padding izquierdo si código < 10 (e.g., departamento 5 → `05`).
- **Ejemplo:** Meta → `50`, Santander → `68`.

### 2.2. MpioDANE (RN-29)

- **Longitud:** 3 dígitos, fija.
- **Fuente:** Código DANE del Municipio, **solo la parte municipal** (3 últimos dígitos del código DANE de 5 dígitos).
- **Cálculo:** Si código DANE completo = `50568`, DptoDANE = `50`, MpioDANE = `568`.
- **Padding:** zero-padding izquierdo si parte municipal < 100.
- **Ejemplo:** Puerto Gaitán (DANE 50568) → `568`, Acacías (DANE 50006) → `006`.

### 2.3. Sigla (RN-30)

- **Longitud:** 4 caracteres, fija. MAYÚSCULAS.
- **Fuente:** Denominación del Pozo.

**Algoritmo de generación:**

```
SI la Denominación tiene UNA sola palabra:
    Sigla = primeras 4 letras
    SI longitud < 4: padding con 'X' a la derecha
    
SI la Denominación tiene DOS o más palabras:
    Sigla = 2 primeras letras de la palabra 1 + 2 primeras letras de la palabra 2
    
EXCEPCIÓN ANH (operadora = ANH):
    Sigla = 'ANH' + primera letra de la Denominación
```

| Denominación | Palabras | Sigla | Regla aplicada |
|-------------|----------|-------|----------------|
| Cusiana Renata | 2 | `CURE` | 2+2 |
| Alpha | 1 | `ALPH` | primeras 4 |
| Sol | 1 (3 letras) | `SOLX` | primeras 3 + padding X |
| Rio Magdalena Sur | 3+ | `RIMA` | 2+2 (solo palabras 1 y 2) |
| AB | 1 (2 letras) | `ABXX` | primeras 2 + padding XX |
| A | 1 (1 letra) | `AXXX` | primera + padding XXX |
| Cusiana (ANH) | N/A | `ANHC` | excepción ANH: ANH + C |

**Reglas adicionales:**
- Se eliminan caracteres no alfabéticos antes de computar (guiones, espacios → solo letras).
- Todo se convierte a MAYÚSCULAS.
- Los artículos y preposiciones ("de", "del", "la", "el") se ignoran al determinar palabras significativas.

### 2.4. Número (RN-31)

- **Longitud:** 4 dígitos, fija.
- **Fuente:** Consecutivo ingresado por el usuario.
- **Padding:** zero-padding izquierdo.
- **Ejemplo:** 1 → `0001`, 42 → `0042`, 9999 → `9999`.

### 2.5. Cluster/Locación (RN-32)

- **Longitud:** 6 caracteres, fija. Formato: 2 alfabéticos + 4 numéricos.
- **Fuente:** Cluster-Locación seleccionado en Sección 3.

**Algoritmo:**

```
SI existe cluster seleccionado:
    abreviatura = primeras 2 letras del nombre del cluster (MAYÚSCULAS)
    número = número secuencial del cluster (zero-padding a 4 dígitos)
    Cluster = abreviatura + número

SI NO existe cluster (cluster = pozo individual):
    Cluster = 'CX' + '0000'
```

| Cluster | Codificación | Explicación |
|---------|-------------|-------------|
| Locación A | `LA0000` | LA (abreviatura) + 0000 (sin número) |
| Cluster Norte 3 | `CN0003` | CN + 0003 |
| (sin cluster) | `CX0000` | Indicador de pozo individual |
| Pad Sur 15 | `PS0015` | PS + 0015 |

### 2.6. Ángulo (RN-33)

- **Longitud:** 1 carácter, fija.
- **Valores:** `H` (Horizontal), `V` (Vertical), `D` (Desviado).
- **Directo** del campo Tipo de Pozo por Ángulo.

### 2.7. Trayectoria (RN-34)

- **Longitud:** variable (0–4 caracteres).
- **Fuente:** Tipo de Pozo por Trayectoria.

| Tipo Trayectoria | Código UWI | Notas |
|-----------------|------------|-------|
| Original (O) | *(vacío)* | No se incluye segmento |
| Side Track (ST) | `ST` | Si hay consecutivo de trayectoria: `ST2`, `ST3` |
| Piloto (P) | `P` | |
| Profundización (PR) | `PR` | |
| Multilateral (ML) | `ML` | |
| Gemelo (G) | `G` | Si hay consecutivo: `G2` |

**Nota:** El consecutivo de trayectoria aplica cuando hay más de un Side Track o Gemelo en el mismo pozo. Para el primer Side Track o Gemelo, no se incluye número. A partir del segundo, se agrega: `ST2`, `G2`, etc.

### 2.8. Objetivo (RN-35)

- **Longitud:** variable (1–2 caracteres).
- **Directo** del campo Tipo de Pozo por Objetivo.

| Tipo Objetivo | Código UWI |
|--------------|------------|
| Producción de Hidrocarburos | `PH` |
| Inyección | `I` |
| Monitoreo | `M` |
| Disposición | `D` |
| Captación | `C` |
| Geotérmico | `GT` |
| Otro | `O` |

### 2.9. Separador + Terminación (RN-36)

- **Separador:** guión (`-`), 1 carácter fijo.
- **Terminación:** variable (1–2 caracteres). **Siempre precedida del guión.**

| Tipo Terminación | Código UWI |
|-----------------|------------|
| Casing y Cementación | `-CD` |
| Liner Cementado | `-LC` |
| Liner con Ranuras | `-LR` |
| Gravel Pack | `-GP` |
| Completación Compuesta | `-CC` |
| Hoyo Abierto | `-OH` |
| Otro | `-O` |

---

## 3. Ejemplos Completos

### Ejemplo 1 — Pozo Original Vertical

| Campo | Valor |
|-------|-------|
| Departamento | Meta (DANE 50) |
| Municipio | Puerto Gaitán (DANE 50568) |
| Denominación | Cusiana Renata |
| Consecutivo | 1 |
| Cluster | Locación A |
| Ángulo | Vertical |
| Trayectoria | Original |
| Objetivo | Producción de Hidrocarburos |
| Terminación | Hoyo Abierto |

```
UWI = 50 568 CURE 0001 LA0000 V PH -OH
    = 50568CURE0001LA0000VPH-OH
```

### Ejemplo 2 — Side Track Horizontal

| Campo | Valor |
|-------|-------|
| Departamento | Santander (DANE 68) |
| Municipio | Barrancabermeja (DANE 68081) |
| Denominación | Alpha |
| Consecutivo | 42 |
| Cluster | Cluster Norte 3 |
| Ángulo | Horizontal |
| Trayectoria | Side Track (2do) |
| Objetivo | Inyección |
| Terminación | Casing y Cementación |

```
UWI = 68 081 ALPH 0042 CN0003 H ST2 I -CD
    = 68081ALPH0042CN0003HST2I-CD
```

### Ejemplo 3 — Pozo ANH Estratigráfico

| Campo | Valor |
|-------|-------|
| Departamento | Putumayo (DANE 86) |
| Municipio | Orito (DANE 86320) |
| Denominación | Exploración Sur |
| Consecutivo | 1 |
| Cluster | (sin cluster) |
| Ángulo | Vertical |
| Trayectoria | Original |
| Objetivo | Geotérmico |
| Terminación | Otro |

```
UWI = 86 320 ANHE 0001 CX0000 V GT -O
    = 86320ANHE0001CX0000VGT-O
```

Nota: Sigla = `ANHE` (excepción ANH: "ANH" + primera letra de "Exploración").

---

## 4. Validaciones del UWI

| Validación | Momento | Acción si falla |
|-----------|---------|-----------------|
| Formato PPDM correcto | Frontend: `computed()` | UWI muestra error visual |
| Unicidad global | Backend: `Finalizar Registro` | HTTP 409 Conflict |
| Inmutabilidad post-creación | Backend: `PUT` | HTTP 422 si UWI cambia en pozo `CREADO` con Forma 101 |
| Longitud máxima 50 caracteres | Backend + Frontend | Error de validación |
| Caracteres válidos: A-Z, 0-9, guión | Frontend + Backend | Error de formato |

---

## 5. Implementación: Responsabilidades FE vs BE

| Aspecto | Frontend | Backend |
|---------|----------|---------|
| Cómputo de UWI preview | ✅ (computed, tiempo real) | — |
| Generación definitiva de UWI | — | ✅ (al finalizar) |
| Validación de formato | ✅ (regex, preview) | ✅ (validator) |
| Validación de unicidad | — | ✅ (DB unique constraint) |
| Inmutabilidad post-creación | — | ✅ (handler reject) |

> **Principio:** El frontend computa el UWI como preview para feedback inmediato. El backend es la fuente de verdad y genera el UWI definitivo al finalizar, aplicando las mismas reglas. Si hay discrepancia, el backend prevalece.
