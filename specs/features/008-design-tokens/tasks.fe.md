# Tasks Frontend — 008 Design Tokens & Tema Visual ANH

> Rama: `claude/fe-008-design-tokens`
> Base: `gop-base-web` en `alejandroikon/appgop-base-web`
> Total: 15 tasks en 7 bloques

---

## Bloque 1: Completar Design Tokens (sin dependencias)

### TASK-FE-001: Agregar tokens semánticos faltantes a `_tokens.css`

**Archivo**: `src/styles/_tokens.css`

Agregar los siguientes tokens DENTRO del bloque `:root` existente, DESPUÉS de los tokens de sidebar y ANTES de los tokens de sombras:

```css
  /* Secundario (usado en sidebar, header, variante moderna) */
  --color-secondary:        var(--primitive-navy-700);

  /* Acento / Marca secundaria ANH */
  --color-accent:           var(--primitive-amber-400);

  /* Títulos y encabezados — navy institucional ANH */
  --color-heading:          var(--primitive-navy-900);
```

Agregar los siguientes ALIASES al final del bloque `:root`, DESPUÉS de los tokens de border-radius:

```css
  /* Aliases de retrocompatibilidad con UIKit Storybook */
  --color-bg-base:   var(--color-surface);
  --color-text-base: var(--color-text-primary);
```

**NO modificar** ningún token existente. Solo agregar.

**Verificación**: Abrir DevTools en cualquier página y confirmar que `document.documentElement.style.getPropertyValue('--color-accent')` no retorna vacío.

### TASK-FE-002: Agregar tema oscuro a `_tokens.css`

**Archivo**: `src/styles/_tokens.css`

Agregar AL FINAL del archivo (fuera del `:root` principal) el siguiente bloque:

```css
/* ============================================================
   Tema oscuro / Propuesta 2: Moderno
   Activo con [data-theme='modern'] en <html> o clase .theme-modern
   ============================================================ */
:root[data-theme='modern'],
.theme-modern {
  --color-heading:       var(--primitive-white);
  --color-primary:       var(--primitive-navy-900);
  --color-primary-hover: var(--primitive-navy-800);
  --color-primary-light: color-mix(in srgb, var(--primitive-navy-900) 10%, transparent);
  --color-secondary:     var(--primitive-navy-700);

  --color-surface:       var(--primitive-gray-900);
  --color-surface-alt:   var(--primitive-gray-700);
  --color-surface-raised:var(--primitive-gray-700);

  --color-border:        var(--primitive-gray-700);
  --color-border-strong: var(--primitive-gray-500);

  --color-text-primary:  var(--primitive-white);
  --color-text-secondary:var(--primitive-gray-400);
  --color-text-disabled: var(--primitive-gray-500);
}
```

**Verificación**: Agregar `data-theme="modern"` al `<html>` en DevTools y verificar que `--color-surface` cambia a `#111827`.

---

## Bloque 2: Completar Tipografía (sin dependencias)

### TASK-FE-003: Agregar aliases de fuentes a `_typography.css`

**Archivo**: `src/styles/_typography.css`

Agregar DENTRO del bloque `:root`, después de `--font-family-mono`:

```css
  /* Aliases usados por componentes del UIKit Storybook */
  --font-sans:    var(--font-family-sans);
  --font-heading: 'Montserrat', sans-serif;
```

**Verificación**: En DevTools, `getComputedStyle(document.documentElement).getPropertyValue('--font-heading')` retorna `'Montserrat', sans-serif`.

---

## Bloque 3: Ampliar PrimeNG Overrides (depende de Bloque 1)

### TASK-FE-004: Agregar overrides PrimeNG adicionales a `_overrides.css`

**Archivo**: `src/styles/_overrides.css`

Agregar las siguientes variables DENTRO del bloque `:root` existente, después de `--p-border-radius-lg`:

```css
  /* Fondos */
  --p-primary-50:             var(--color-primary-light);
  --p-surface-50:             var(--color-surface-alt);
  --p-surface-100:            var(--primitive-gray-100);

  /* Border radius de contenido */
  --p-content-border-radius:  var(--radius-md);
```

**Verificación**: Las tablas PrimeNG (p-table) muestran hover con fondo azul claro (#eff6ff).

---

## Bloque 4: Estilos Globales (depende de Bloques 1-3)

### TASK-FE-005: Actualizar `styles.css` con imports y estilos base

**Archivo**: `src/styles.css`

Reemplazar TODO el contenido del archivo por:

```css
/* Google Fonts — Inter (body) + Montserrat (headings) */
@import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=Montserrat:wght@500;600;700&display=swap');

/* Design tokens — orden obligatorio: tokens -> tipografía -> overrides */
@import './styles/_tokens.css';
@import './styles/_typography.css';
@import './styles/_overrides.css';

@tailwind base;
@tailwind components;
@tailwind utilities;

/* ============================================================
   Estilos base del documento
   ============================================================ */
body {
  margin: 0;
  font-family: var(--font-sans, var(--font-family-sans));
  background-color: var(--color-surface);
  color: var(--color-text-primary);
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
}

h1, h2, h3, h4, h5, h6, .headline {
  font-family: var(--font-heading);
  color: var(--color-heading);
}

/* ============================================================
   Focus visible global — Accesibilidad WCAG 2.1 AA
   Outline amarillo ANH en todos los elementos interactivos
   ============================================================ */
:focus-visible {
  outline: 2px solid var(--color-accent);
  outline-offset: 2px;
}

/* ============================================================
   Clase sr-only — accesibilidad NTC 5854
   Oculta visualmente pero accesible para screen readers
   ============================================================ */
.sr-only {
  position: absolute;
  width: 1px;
  height: 1px;
  padding: 0;
  margin: -1px;
  overflow: hidden;
  clip: rect(0, 0, 0, 0);
  white-space: nowrap;
  border: 0;
}
```

**IMPORTANTE**: El import de Google Fonts DEBE ir ANTES de los imports de tokens. El import de `primeicons.css` ya no es necesario aquí porque PrimeNG 21 lo incluye automáticamente. Si el build falla por primeicons faltantes, restaurar la línea `@import "primeicons/primeicons.css";` después del import de Google Fonts.

**Verificación**: `npx ng build --configuration production` compila sin errores.

---

## Bloque 5: HTML Base (sin dependencias)

### TASK-FE-006: Actualizar `index.html` con metadata ANH

**Archivo**: `src/index.html`

Reemplazar el contenido por:

```html
<!doctype html>
<html lang="es">
<head>
  <meta charset="utf-8">
  <title>GOP 360° — ANH Colombia</title>
  <base href="/">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <meta name="description" content="Gestión de Operaciones de Pozos — Agencia Nacional de Hidrocarburos de Colombia">
  <link rel="icon" type="image/x-icon" href="favicon.ico">
</head>
<body>
  <app-root></app-root>
</body>
</html>
```

**Cambios**:
- `lang="en"` → `lang="es"`
- `<title>Myapp</title>` → `<title>GOP 360° — ANH Colombia</title>`
- Agregar `<meta name="description">`

**Verificación**: La pestaña del navegador muestra "GOP 360° — ANH Colombia".

---

## Bloque 6: Migración de Colores Hardcodeados (depende de Bloque 1)

### TASK-FE-007: Migrar colores Tailwind en `login.component.html`

**Archivo**: `src/app/core/auth/features/login/login.component.html`

Reemplazar las clases Tailwind de colores hardcodeados por clases que usen custom properties. La estrategia es:

| Clase Tailwind actual | Reemplazo |
|-----------------------|-----------|
| `text-gray-900` | `text-[var(--color-text-primary)]` |
| `text-gray-400` | `text-[var(--color-text-secondary)]` |
| `text-gray-500` | `text-[var(--color-text-secondary)]` |
| `text-gray-600` | `text-[var(--color-text-primary)]` |
| `text-gray-700` | `text-[var(--color-text-primary)]` |
| `text-red-500` | `text-[var(--color-error)]` |
| `text-red-600` | `text-[var(--color-error)]` |
| `bg-white` | `bg-[var(--color-surface)]` |
| `bg-red-50` | `bg-[var(--color-error-light)]` |
| `border-gray-200` | `border-[var(--color-border)]` |
| `border-red-200` | `border-[var(--color-error)]` |
| `focus:ring-slate-400` | `focus:ring-[var(--color-primary)]` |
| `focus:border-slate-400` | `focus:border-[var(--color-primary)]` |
| `placeholder-gray-400` | `placeholder-[var(--color-text-disabled)]` |

**IMPORTANTE**: Solo reemplazar clases de COLOR. NO tocar clases de layout (flex, p-3, rounded-md, etc.).

**Verificación**: El formulario de login se ve igual visualmente pero usa tokens. Verificar en DevTools que los colores vienen de custom properties.

### TASK-FE-008: Migrar colores Tailwind en `forgot-password.component.html`

**Archivo**: `src/app/core/auth/features/forgot-password/forgot-password.component.html`

Aplicar la misma tabla de reemplazos de TASK-FE-007.

**Verificación**: La página de forgot-password se ve igual que antes.

### TASK-FE-009: Migrar colores Tailwind en `auth-layout.component.html`

**Archivo**: `src/app/core/layout/auth-layout/auth-layout.component.html`

Aplicar la misma tabla de reemplazos de TASK-FE-007.

**Verificación**: El layout de autenticación se ve igual que antes.

---

## Bloque 7: Verificación Final (depende de todo lo anterior)

### TASK-FE-010: Build de producción

Ejecutar:
```bash
npx ng build --configuration production
```

**Criterio**: Cero errores. Warnings solo si son de Tailwind (no críticos).

### TASK-FE-011: Auditoría de colores hardcodeados

Ejecutar:
```bash
grep -rn "text-gray-\|text-red-\|text-blue-\|bg-gray-\|bg-red-\|bg-blue-\|bg-white\|border-gray-\|border-red-\|border-slate\|ring-slate\|text-slate" src/app --include="*.html" --include="*.css"
```

**Criterio**: Cero resultados (excluyendo archivos de test/mock/spec).

### TASK-FE-012: Ejecutar tests existentes

Ejecutar:
```bash
npx ng test --watch=false --browsers=ChromeHeadless
```

**Criterio**: Todos los tests existentes pasan. Esta iteración NO agrega tests nuevos porque los cambios son solo CSS/HTML visual.

### TASK-FE-013: Verificar tema oscuro

Agregar `data-theme="modern"` al `<html>` del build y verificar que:
- El fondo general cambia a gris oscuro (#111827)
- Los textos se ven en blanco
- Los bordes se ajustan a gris medio
- No hay texto invisible (texto oscuro sobre fondo oscuro)

### TASK-FE-014: Verificar fuentes

En DevTools > Elements > seleccionar `<body>`:
- Computed font-family debe incluir "Inter"

Seleccionar cualquier `<h1>` o `<h2>`:
- Computed font-family debe incluir "Montserrat"

### TASK-FE-015: Commit y push

```bash
git add -A
git commit -m "feat(design-tokens): complete ANH visual identity tokens, dark mode, typography, and global styles

- Add missing semantic tokens: --color-secondary, --color-accent, --color-heading
- Add dark mode support via [data-theme='modern']
- Add Google Fonts (Inter + Montserrat) with display=swap
- Add global body/heading styles using token system
- Add focus-visible with ANH amber outline for WCAG 2.1 AA
- Add sr-only utility class for NTC 5854 compliance
- Expand PrimeNG overrides (--p-primary-50, --p-surface-50, etc.)
- Migrate hardcoded Tailwind colors to CSS custom properties in auth components
- Update index.html: lang=es, title GOP 360°, meta description

Source: Storybook UIKit by María Alejandra Sanjuan (appgop-web-feature-MS-user-rol-creation)"
git push origin claude/fe-008-design-tokens
```
