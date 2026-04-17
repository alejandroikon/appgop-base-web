# Feature 008 — Design Tokens & Tema Visual ANH

> Iteración: 6 (solo frontend)
> Autor: Alejandro Gutiérrez / Claude Opus
> Fecha: 2026-04-17
> Dependencia: Storybook UIKit de María Alejandra Sanjuan (appgop-web-feature-MS-user-rol-creation)

---

## 1. Objetivo

Completar la integración del sistema de Design Tokens proveniente del UIKit de Storybook, alinear el tema PrimeNG con la identidad visual ANH, agregar soporte de tipografía institucional (Inter + Montserrat), e incorporar tema oscuro (dark mode). Al finalizar esta iteración, toda la aplicación (login, sidebar, listado de pozos, wizard, detalle) se verá con los colores institucionales ANH sin modificar ningún componente funcional.

## 2. Alcance

### Incluido
- Completar `_tokens.css` con tokens semánticos faltantes
- Completar `_typography.css` con aliases faltantes
- Ampliar `_overrides.css` con más variables PrimeNG
- Actualizar `styles.css` con imports de fuentes, estilos base de body/headings, clase `.sr-only`
- Actualizar `index.html` con `lang="es"`, título "GOP 360°", y meta description
- Agregar tema oscuro via `[data-theme='modern']`
- Migrar colores hardcodeados de componentes existentes a tokens semánticos

### Excluido
- Migrar componentes a BEM SCSS (Fase B futura)
- Incorporar componentes del UIKit (Button, Input, etc.)
- Instalar lucide-angular (se queda con PrimeIcons por ahora)
- Cambios en backend

## 3. Fuente de Verdad

Los valores de tokens provienen del archivo `src/styles/_tokens.scss` del proyecto Storybook UIKit (`appgop-web-feature-MS-user-rol-creation`). Este archivo fue creado por María Alejandra Sanjuan siguiendo la identidad visual oficial de ANH Colombia.

## 4. Análisis de Diferencias (Estado Actual vs. UIKit)

### 4.1 Tokens faltantes en `_tokens.css`

| Token | Valor | Propósito |
|-------|-------|-----------|
| `--color-secondary` | `var(--primitive-navy-700)` | Acciones alternas, variante moderna |
| `--color-accent` | `var(--primitive-amber-400)` | Focus visible, marcas destacadas ANH |
| `--color-heading` | `var(--primitive-navy-900)` | Títulos h1-h6 en navy institucional |
| `--color-bg-base` | `var(--color-surface)` | Alias retrocompatibilidad |
| `--color-text-base` | `var(--color-text-primary)` | Alias retrocompatibilidad |

### 4.2 Tokens que ya existen y son correctos (no tocar)

Todos los primitivos (blue, navy, amber, gray, red, green, yellow, white, black) y la mayoría de semánticos (primary, surface, border, text, error, success, warning, shadow, radius) ya coinciden exactamente con el UIKit.

### 4.3 Tokens del sidebar (mantener)

Los tokens `--sidebar-*` son propios de GOP 360° y no existen en el UIKit. Se mantienen porque el sidebar del proyecto los usa activamente.

### 4.4 Tipografía faltante

| Token | Valor | Propósito |
|-------|-------|-----------|
| `--font-sans` | `var(--font-family-sans)` | Alias corto usado por UIKit |
| `--font-heading` | `'Montserrat', sans-serif` | Fuente de encabezados ANH |

### 4.5 PrimeNG overrides adicionales necesarios

El UIKit no tiene más overrides que los actuales, pero la aplicación necesita cubrir más variables para consistencia visual completa:

| Variable PrimeNG | Mapeo | Efecto |
|------------------|-------|--------|
| `--p-primary-50` | `var(--color-primary-light)` | Fondo hover suave en tablas/selecciones |
| `--p-surface-50` | `var(--color-surface-alt)` | Fondo alternativo en componentes |
| `--p-surface-100` | `var(--primitive-gray-100)` | Striped tables, hover rows |
| `--p-content-border-radius` | `var(--radius-md)` | Bordes redondeados en cards, panels |

## 5. Reglas de Negocio Visual

### RN-V01: Jerarquía de tokens
Los componentes y templates SOLO deben usar tokens semánticos (Capa 2: `--color-*`, `--font-*`, etc.). Nunca usar primitivos (`--primitive-*`) directamente. Los primitivos son la paleta; los semánticos son el significado.

### RN-V02: Consistencia con UIKit
Cualquier nuevo token que se agregue debe seguir la convención de nombres del UIKit de María Alejandra: `--color-{propósito}` para colores, `--font-{propiedad}` para tipografía, `--shadow-{tamaño}` para sombras, `--radius-{tamaño}` para bordes.

### RN-V03: Tema oscuro no activo por defecto
El tema oscuro se implementa como opt-in via `[data-theme='modern']` en `<html>`. La aplicación arranca siempre en tema claro. La activación del dark mode se implementará en una iteración futura (toggle en header).

### RN-V04: Google Fonts con display=swap
Las fuentes Inter y Montserrat deben cargarse via Google Fonts con `display=swap` para evitar FOIT (Flash of Invisible Text). JetBrains Mono es opcional (solo para código/UWI).

### RN-V05: Zero colores hardcodeados
Después de esta iteración, ningún componente existente debe tener valores hexadecimales (`#xxx`) directamente en sus templates o estilos. Todo debe referenciar tokens.

### RN-V06: Accesibilidad — Focus visible
Todos los elementos interactivos deben tener `:focus-visible` con `outline: 2px solid var(--color-accent)` y `outline-offset: 2px`. El amarillo ANH (#FFCB05) como color de focus es estándar del UIKit.

## 6. Criterios de Aceptación

### CA-01: Build exitoso
`npx ng build --configuration production` compila sin errores ni warnings de CSS.

### CA-02: Tokens completos
Los archivos `_tokens.css`, `_typography.css` y `_overrides.css` contienen todos los tokens del UIKit más los específicos de GOP (sidebar).

### CA-03: Fuentes cargadas
Al abrir la app en Netlify, Inter se usa en el body y Montserrat en los títulos. Verificar en DevTools > Computed > font-family.

### CA-04: Visual consistente
El login, sidebar, header, listado de pozos y wizard de creación se ven con colores ANH (navy sidebar, azul primario en botones, amber en acentos).

### CA-05: Sin hardcoded colors
`grep -r "#[0-9a-fA-F]\{3,6\}" src/app --include="*.html" --include="*.css"` retorna cero resultados (excluyendo archivos de test y mock).

### CA-06: Dark mode funcional
Agregar manualmente `data-theme="modern"` al `<html>` en DevTools cambia toda la app a tema oscuro sin errores visuales.

### CA-07: Accesibilidad
Tab-navigation muestra outline amarillo en todos los botones, inputs y links.

### CA-08: Título y metadata
`<html lang="es">`, `<title>GOP 360°</title>`, y `<meta name="description">` presentes en index.html.

## 7. Historias de Usuario

### HU-V01: Como desarrollador, quiero un sistema de tokens completo y alineado con el UIKit de Storybook, para que cualquier componente nuevo use los mismos colores y tipografía que el Design System.

### HU-V02: Como usuario de ANH, quiero ver la identidad visual institucional (colores navy, amber, tipografía Inter/Montserrat) en toda la aplicación, para reconocer que es un producto oficial.

### HU-V03: Como usuario con preferencia de tema oscuro, quiero que la aplicación soporte un modo oscuro, para reducir fatiga visual en sesiones largas de trabajo.

### HU-V04: Como usuario con discapacidad visual, quiero que los elementos interactivos tengan un indicador de foco visible (outline amarillo), para poder navegar la aplicación con teclado.
