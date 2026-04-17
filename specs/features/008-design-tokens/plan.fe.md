# Plan Frontend — 008 Design Tokens & Tema Visual ANH

> Solo frontend. No hay cambios de backend ni contract.yml.
> Rama: `claude/fe-008-design-tokens`

---

## Estrategia General

Esta iteración modifica SOLO archivos de estilos globales y el index.html. No se tocan componentes funcionales (lógica, servicios, mocks). Los cambios son aditivos — se agregan tokens y estilos, no se eliminan nada que ya funcione.

El orden de ejecución es crítico: primero tokens, luego tipografía, luego overrides PrimeNG, luego styles.css global, luego index.html, y finalmente la auditoría de hardcoded colors en componentes.

## Archivos a Modificar

### Bloque 1: Tokens (sin dependencias)
| Archivo | Acción | Descripción |
|---------|--------|-------------|
| `src/styles/_tokens.css` | MODIFICAR | Agregar `--color-secondary`, `--color-accent`, `--color-heading`, aliases, y bloque dark mode |

### Bloque 2: Tipografía (sin dependencias)
| Archivo | Acción | Descripción |
|---------|--------|-------------|
| `src/styles/_typography.css` | MODIFICAR | Agregar `--font-sans`, `--font-heading` aliases |

### Bloque 3: PrimeNG Overrides (depende de Bloque 1)
| Archivo | Acción | Descripción |
|---------|--------|-------------|
| `src/styles/_overrides.css` | MODIFICAR | Agregar `--p-primary-50`, `--p-surface-50`, `--p-surface-100`, `--p-content-border-radius` |

### Bloque 4: Estilos Globales (depende de Bloques 1-3)
| Archivo | Acción | Descripción |
|---------|--------|-------------|
| `src/styles.css` | MODIFICAR | Agregar Google Fonts import, estilos body, headings, .sr-only, lucide-icon fix, focus-visible global |

### Bloque 5: HTML Base
| Archivo | Acción | Descripción |
|---------|--------|-------------|
| `src/index.html` | MODIFICAR | `lang="es"`, `<title>GOP 360°</title>`, meta description |

### Bloque 6: Auditoría de Hardcoded Colors (depende de Bloque 1)
| Archivos | Acción | Descripción |
|----------|--------|-------------|
| `src/app/**/*.css` | MODIFICAR (si hay) | Reemplazar `#hexcolor` por `var(--token)` correspondiente |
| `src/app/**/*.html` | MODIFICAR (si hay) | Reemplazar inline styles con colores hex por tokens |

### Bloque 7: Verificación
| Acción | Comando |
|--------|---------|
| Build producción | `npx ng build --configuration production` |
| Auditoría hex | `grep -rn "#[0-9a-fA-F]\{3,6\}" src/app --include="*.html" --include="*.css"` |
| Tests existentes | `npx ng test --watch=false --browsers=ChromeHeadless` |

## Riesgos y Mitigaciones

| Riesgo | Probabilidad | Mitigación |
|--------|-------------|------------|
| Google Fonts no carga en Netlify | Baja | `display=swap` garantiza fallback a system-ui |
| Dark mode rompe componentes PrimeNG | Media | Solo implementar overrides de tokens CSS, no tocar estilos inline de PrimeNG |
| Tailwind utilities hardcodeadas con colores | Media | Auditar clases `bg-*`, `text-*` en templates y reemplazar por tokens |
| Sidebar pierde estilos al modificar tokens | Baja | Mantener tokens `--sidebar-*` intactos, solo agregar nuevos tokens |

## Resultado Esperado

Después del merge y deploy en Netlify:
1. La app se ve con tipografía Inter (body) y Montserrat (títulos)
2. Los botones primarios son azul ANH (#2563eb)
3. El sidebar mantiene su navy (#083075)
4. Los focus tienen outline amarillo ANH (#FFCB05)
5. Agregar `data-theme="modern"` en DevTools cambia a dark mode
6. Cero colores hardcodeados en el código fuente
