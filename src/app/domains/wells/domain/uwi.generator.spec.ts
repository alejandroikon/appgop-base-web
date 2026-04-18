import { describe, it, expect } from 'vitest';
import { generateUwiPreview, computeSigla, computeClusterCode } from './uwi.generator';

// ─── computeSigla ─────────────────────────────────────────────────────────────

describe('computeSigla', () => {
  it('2 palabras → 2+2 primeras letras', () => {
    expect(computeSigla('Cusiana Renata', false)).toBe('CURE');
  });

  it('1 palabra larga → primeras 4 letras', () => {
    expect(computeSigla('Alpha', false)).toBe('ALPH');
  });

  it('1 palabra de 3 letras → padding X a la derecha', () => {
    expect(computeSigla('Sol', false)).toBe('SOLX');
  });

  it('1 palabra de 2 letras → padding XX', () => {
    expect(computeSigla('AB', false)).toBe('ABXX');
  });

  it('1 palabra de 1 letra → padding XXX', () => {
    expect(computeSigla('A', false)).toBe('AXXX');
  });

  it('3+ palabras → solo palabras 1 y 2', () => {
    expect(computeSigla('Rio Magdalena Sur', false)).toBe('RIMA');
  });

  it('excepción ANH → ANH + 1er char', () => {
    expect(computeSigla('Cusiana', true)).toBe('ANHC');
  });

  it('excepción ANH con denominación compuesta', () => {
    expect(computeSigla('Exploración Sur', true)).toBe('ANHE');
  });

  it('ignora artículos como stop words', () => {
    // "El Norte" → palabras significativas = [NORTE] → NORT (solo 1 significativa)
    expect(computeSigla('El Norte', false)).toBe('NORT');
  });
});

// ─── computeClusterCode ───────────────────────────────────────────────────────

describe('computeClusterCode', () => {
  it('sin cluster → CX0000', () => {
    expect(computeClusterCode(null)).toBe('CX0000');
  });

  it('cluster con abreviatura explícita', () => {
    expect(computeClusterCode('Locación A', 'LA')).toBe('LA0000');
  });

  it('cluster con número → incluye número en código', () => {
    expect(computeClusterCode('Cluster Norte 3', 'CN')).toBe('CN0003');
  });

  it('cluster con abreviatura y número grande', () => {
    expect(computeClusterCode('Pad Sur 15', 'PS')).toBe('PS0015');
  });

  it('cluster sin abreviatura usa primeras 2 letras del nombre', () => {
    expect(computeClusterCode('Locación A', null)).toBe('LO0000');
  });
});

// ─── generateUwiPreview — Ejemplos del algoritmo ─────────────────────────────

describe('generateUwiPreview', () => {
  // Ejemplo 1 — Pozo Original Vertical (uwi-algorithm.md §3)
  it('Ejemplo 1: pozo original vertical con hoyo abierto', () => {
    const result = generateUwiPreview({
      codigoDaneDpto:  '50',
      codigoDaneMpio:  '50568',
      denominacion:    'Cusiana Renata',
      consecutivo:     1,
      clusterNombre:   'Locación A',
      clusterAbreviatura: 'LA',
      tipoAngulo:      'V',
      tipoTrayectoria: 'O',
      tipoObjetivo:    'PH',
      tipoTerminacion: 'OH',
      isAnh:           false,
    });
    expect(result.uwi).toBe('50568CURE0001LA0000VPH-OH');
    expect(result.components.sigla).toBe('CURE');
    expect(result.components.dptoCode).toBe('50');
    expect(result.components.mpioCode).toBe('568');
    expect(result.components.numero).toBe('0001');
    expect(result.components.clusterCode).toBe('LA0000');
    expect(result.components.trayectoriaCode).toBe('');
  });

  // Ejemplo 2 — Side Track Horizontal (uwi-algorithm.md §3)
  it('Ejemplo 2: side track horizontal casing', () => {
    const result = generateUwiPreview({
      codigoDaneDpto:  '68',
      codigoDaneMpio:  '68081',
      denominacion:    'Alpha',
      consecutivo:     42,
      clusterNombre:   'Cluster Norte 3',
      clusterAbreviatura: 'CN',
      tipoAngulo:      'H',
      tipoTrayectoria: 'ST',
      tipoObjetivo:    'I',
      tipoTerminacion: 'CD',
      isAnh:           false,
    });
    expect(result.uwi).toBe('68081ALPH0042CN0003HST I-CD'.replace(' ', ''));
    // Verificar UWI completo
    expect(result.uwi).toBe('68081ALPH0042CN0003HSTI-CD');
  });

  // Ejemplo 3 — Pozo ANH Estratigráfico (uwi-algorithm.md §3)
  it('Ejemplo 3: pozo ANH estrat sin cluster', () => {
    const result = generateUwiPreview({
      codigoDaneDpto:  '86',
      codigoDaneMpio:  '86320',
      denominacion:    'Exploración Sur',
      consecutivo:     1,
      clusterNombre:   null,
      tipoAngulo:      'V',
      tipoTrayectoria: 'O',
      tipoObjetivo:    'GT',
      tipoTerminacion: 'O',
      isAnh:           true,
    });
    expect(result.uwi).toBe('86320ANHE0001CX0000VGT-O');
    expect(result.components.sigla).toBe('ANHE');
    expect(result.components.clusterCode).toBe('CX0000');
    expect(result.components.trayectoriaCode).toBe('');
  });

  it('trayectoria no-original incluye el código', () => {
    const result = generateUwiPreview({
      codigoDaneDpto:  '50',
      codigoDaneMpio:  '50568',
      denominacion:    'Alpha',
      consecutivo:     1,
      clusterNombre:   null,
      tipoAngulo:      'V',
      tipoTrayectoria: 'ST',
      tipoObjetivo:    'PH',
      tipoTerminacion: 'CD',
      isAnh:           false,
    });
    expect(result.components.trayectoriaCode).toBe('ST');
    expect(result.uwi).toContain('ST');
  });

  it('consecutivo se zero-padea a 4 dígitos', () => {
    const result = generateUwiPreview({
      codigoDaneDpto:  '50',
      codigoDaneMpio:  '50568',
      denominacion:    'Alpha',
      consecutivo:     5,
      clusterNombre:   null,
      tipoAngulo:      'V',
      tipoTrayectoria: 'O',
      tipoObjetivo:    'PH',
      tipoTerminacion: 'CD',
      isAnh:           false,
    });
    expect(result.components.numero).toBe('0005');
  });
});
