import { describe, it, expect } from 'vitest';
import {
  validateDenominacion,
  validateConsecutivo,
  isClasificacionValidForAnh,
  isCampoRequired,
} from './well-form.validators';

// ─── validateDenominacion ─────────────────────────────────────────────────────

describe('validateDenominacion', () => {
  it('letras simples → válido', () => {
    expect(validateDenominacion('Alpha').valid).toBe(true);
  });

  it('letras con acento → válido', () => {
    expect(validateDenominacion('Cusianá').valid).toBe(true);
  });

  it('letras y espacios → válido', () => {
    expect(validateDenominacion('Cusiana Renata').valid).toBe(true);
  });

  it('letras y guión → válido', () => {
    expect(validateDenominacion('Pozo-Norte').valid).toBe(true);
  });

  it('vacío → required', () => {
    const r = validateDenominacion('');
    expect(r.valid).toBe(false);
    expect(r.error).toBe('required');
  });

  it('solo espacios → required', () => {
    const r = validateDenominacion('   ');
    expect(r.valid).toBe(false);
    expect(r.error).toBe('required');
  });

  it('con números → pattern', () => {
    const r = validateDenominacion('Pozo123');
    expect(r.valid).toBe(false);
    expect(r.error).toBe('pattern');
  });

  it('con caracteres especiales → pattern', () => {
    const r = validateDenominacion('Pozo#Norte');
    expect(r.valid).toBe(false);
    expect(r.error).toBe('pattern');
  });

  it('exactamente 50 chars → válido', () => {
    expect(validateDenominacion('A'.repeat(50)).valid).toBe(true);
  });

  it('más de 50 chars → maxlength', () => {
    const r = validateDenominacion('A'.repeat(51));
    expect(r.valid).toBe(false);
    expect(r.error).toBe('maxlength');
  });
});

// ─── validateConsecutivo ──────────────────────────────────────────────────────

describe('validateConsecutivo', () => {
  it('1 → válido', () => {
    expect(validateConsecutivo(1).valid).toBe(true);
  });

  it('9999 → válido', () => {
    expect(validateConsecutivo(9999).valid).toBe(true);
  });

  it('0 → range', () => {
    const r = validateConsecutivo(0);
    expect(r.valid).toBe(false);
    expect(r.error).toBe('range');
  });

  it('10000 → range', () => {
    expect(validateConsecutivo(10000).valid).toBe(false);
  });

  it('NaN → required', () => {
    expect(validateConsecutivo(NaN).valid).toBe(false);
  });
});

// ─── isClasificacionValidForAnh ───────────────────────────────────────────────

describe('isClasificacionValidForAnh', () => {
  it('ANH + ESTRATIGRAFICO → true', () => {
    expect(isClasificacionValidForAnh('ESTRATIGRAFICO', true)).toBe(true);
  });

  it('ANH + EXPLORATORIO → false', () => {
    expect(isClasificacionValidForAnh('EXPLORATORIO', true)).toBe(false);
  });

  it('ANH + DESARROLLO → false', () => {
    expect(isClasificacionValidForAnh('DESARROLLO', true)).toBe(false);
  });

  it('no-ANH + cualquier clasificación → true', () => {
    expect(isClasificacionValidForAnh('EXPLORATORIO', false)).toBe(true);
    expect(isClasificacionValidForAnh('DESARROLLO', false)).toBe(true);
    expect(isClasificacionValidForAnh('ESTRATIGRAFICO', false)).toBe(true);
  });
});

// ─── isCampoRequired ──────────────────────────────────────────────────────────

describe('isCampoRequired', () => {
  it('DESARROLLO → campo requerido', () => {
    expect(isCampoRequired('DESARROLLO')).toBe(true);
  });

  it('EXPLORATORIO → campo opcional', () => {
    expect(isCampoRequired('EXPLORATORIO')).toBe(false);
  });

  it('ESTRATIGRAFICO → campo opcional', () => {
    expect(isCampoRequired('ESTRATIGRAFICO')).toBe(false);
  });
});
