/**
 * PokemonApi.DatasetAudit
 * ---------------------------------------------------------------------------
 * Inspecciona el dataset ya normalizado y resume los rangos de valores que
 * alimentan los tipos enumerados del dominio y las reglas de traduccion.
 *
 * No corrige nada: su trabajo es responder preguntas que el dataset responde
 * mejor mirandolo que leyendo el codigo. Por ejemplo, quais son los valores
 * reales de `relativePhysicalStats` (para no inventar un enumerado que luego no
 * se pueda deserializar), que generacion es la mas reciente, o si algun campo
 * mezcla tipos entre registros.
 *
 * Se ejecuta sobre el JSON versionado, sin red:
 *   node tools/audit-dataset.js
 */

const d = require('../src/PokemonApi.Infrastructure/Data/pokemon.json');

const seen = {};

function note(path, v) {
  if (v === null || v === undefined) return;
  const t = Array.isArray(v) ? 'array' : typeof v;
  (seen[path] = seen[path] || new Set()).add(t);
}

function scan(obj, prefix) {
  for (const k of Object.keys(obj)) {
    note(prefix + k, obj[k]);
    const v = obj[k];
    if (v && typeof v === 'object' && !Array.isArray(v)) scan(v, prefix + k + '.');
  }
}

for (const p of d.pokemon) {
  scan(p, 'pokemon.');
  for (const a of p.abilities) scan(a, 'ability[].');
  for (const ev of p.evolvesTo) {
    scan(ev, 'evolvesTo[].');
    for (const r of ev.requirements) scan(r, 'req.');
  }
}

scan(d.generations[0], 'generations.');
scan(d.abilities[0], 'abilities.');
scan(d.types[0], 'types.');
scan(d.eggGroups[0], 'eggGroups.');
scan(d.habitats[0], 'habitats.');
scan(d.metadata, 'metadata.');

const bad = Object.entries(seen).filter(([, s]) => s.size > 1);
console.log('--- campos con tipos no nulos mixtos ---');
console.log(bad.length ? bad.map(([p, s]) => `${p} -> ${[...s].join(' | ')}`).join('\n') : '(ninguno)');

const reqs = d.pokemon.flatMap((p) => p.evolvesTo.flatMap((e) => e.requirements));
const distinct = (key) => [...new Set(reqs.map((r) => `${key}=${JSON.stringify(r[key])}`))].sort();

console.log();
console.log('req.gender          ->', distinct('gender').join(' '));
console.log('req.timeOfDay       ->', distinct('timeOfDay').join(' '));
console.log('req.relativePhys    ->', distinct('relativePhysicalStats').join(' '));
console.log('req.knownMoveType   ->', distinct('knownMoveType').slice(0, 25).join(' '));
console.log('req.trigger         ->', distinct('trigger').join(' '));
console.log();
console.log('pokemon.genderRate  ->', [...new Set(d.pokemon.map((p) => `${p.genderRate}`))].sort((a, b) => a - b).join(','));
console.log('pokemon.captureRate ->', typeof d.pokemon[0].captureRate, 'rango', Math.min(...d.pokemon.map((p) => p.captureRate)), '-', Math.max(...d.pokemon.map((p) => p.captureRate)));
console.log('pokemon.height      ->', typeof d.pokemon[0].height, 'rango', Math.min(...d.pokemon.map((p) => p.height)), '-', Math.max(...d.pokemon.map((p) => p.height)));
console.log('baseExperience nulls->', d.pokemon.filter((p) => p.baseExperience === null).length);
console.log('totalStats correct  ->', d.pokemon.every((p) => Object.values(p.stats).reduce((a, b) => a + b, 0) === p.totalStats));
