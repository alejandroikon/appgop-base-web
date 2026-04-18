import { describe, it, expect } from 'vitest';
import { generateWellName } from './well-name.generator';

describe('generateWellName', () => {
  it('con campo: {CAMPO}-{DENOM}-{CONSEC}', () => {
    expect(generateWellName('Rubiales', 'E&P Llanos', 'Cusiana Renata', 1))
      .toBe('RUBIALES-CUSIANA RENATA-1');
  });

  it('sin campo (exploratorio): usa contrato como prefijo', () => {
    expect(generateWellName(null, 'E&P Llanos', 'Alpha', 5))
      .toBe('E&P LLANOS-ALPHA-5');
  });

  it('denominación siempre en MAYÚSCULAS', () => {
    expect(generateWellName('Rubiales', null, 'alpha', 1))
      .toBe('RUBIALES-ALPHA-1');
  });

  it('consecutivo sin ceros a la izquierda', () => {
    expect(generateWellName('Rubiales', null, 'Alpha', 1))
      .toBe('RUBIALES-ALPHA-1');
  });

  it('campo vacío + sin contrato → cadena vacía', () => {
    expect(generateWellName(null, null, 'Alpha', 1)).toBe('');
  });

  it('denominación vacía → cadena vacía', () => {
    expect(generateWellName('Rubiales', null, '', 1)).toBe('');
  });

  it('consecutivo 0 → cadena vacía', () => {
    expect(generateWellName('Rubiales', null, 'Alpha', 0)).toBe('');
  });

  it('campo con espacios se trimea y uppercase', () => {
    expect(generateWellName('  Rubiales  ', null, 'Alpha', 2))
      .toBe('RUBIALES-ALPHA-2');
  });
});
