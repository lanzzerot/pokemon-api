/**
 * PokemonApi.DatasetBuilder
 * ---------------------------------------------------------------------------
 * Descarga el catalogo completo de Pokemon desde PokeAPI y lo normaliza en un
 * unico archivo JSON desnormalizado que se versiona dentro del repositorio.
 *
 * Objetivo: que la API en ejecucion sea 100% offline. Sin red en runtime, sin
 * base de datos, arranque en milisegundos y resultados reproducibles.
 *
 * Fuentes:
 *  - GraphQL (https://beta.pokeapi.co/graphql/v1beta) para el catalogo de
 *    Pokemon, especies, tipos, habilidades, etc. Una sola consulta.
 *  - REST    (https://pokeapi.co/api/v2) para las cadenas evolutivas, porque el
 *    endpoint GraphQL no expone la columna `evolves_from_species_id` y por lo
 *    tanto no permite reconstruir ramas (Eevee, Tyrogue, Poliwag...).
 *
 * Uso:  node build-dataset.mjs <ruta-de-salida.json>
 */

import { writeFile } from 'node:fs/promises';

const GRAPHQL_ENDPOINT = 'https://beta.pokeapi.co/graphql/v1beta';
const REST_BASE = 'https://pokeapi.co/api/v2';
const LANGUAGE_ENGLISH_ID = 9;
const POKEAPI_VERSION = '0.7.0';

const SPRITES = {
  officialArtwork: (id) => `https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/other/official-artwork/${id}.png`,
  officialArtworkShiny: (id) => `https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/other/official-artwork/shiny/${id}.png`,
  homeArtwork: (id) => `https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/other/home/${id}.png`,
  frontDefault: (id) => `https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/${id}.png`,
  frontShiny: (id) => `https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/shiny/${id}.png`,
  pixelArt: (id) => `https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/versions/generation-viii/icons/${id}.png`,
  spritesheet: (id) => `https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/versions/generation-viii/icons/spritesheet/pokemon_${id}.png`,
};

// ---------------------------------------------------------------------------
// Utilidades de normalizacion
// ---------------------------------------------------------------------------

/** Numerales romanos usados por los nombres de generacion. */
const ROMAN_NUMERALS = new Map([
  ['i', 'I'], ['ii', 'II'], ['iii', 'III'], ['iv', 'IV'], ['v', 'V'],
  ['vi', 'VI'], ['vii', 'VII'], ['viii', 'VIII'], ['ix', 'IX'], ['x', 'X'],
  ['xi', 'XI'], ['xii', 'XII'],
]);

function capitalize(word) {
  if (!word) return word;
  return word.charAt(0).toUpperCase() + word.slice(1);
}

/** "generation-iii" -> "Generation III", "kanto" -> "Kanto". */
function titleCase(slug) {
  if (!slug) return slug;
  return slug
    .split(/[\s-]/)
    .map((part) => ROMAN_NUMERALS.get(part) ?? capitalize(part))
    .join(' ');
}

/**
 * Los nombres de Pokemon y habilidad llevan unaffijos internos
 * ("deoxys-normal", "mr-mime", "high-pressure"). Los acronimos
 * se capitalizan por completo; el resto solo la inicial.
 */
const ACRONYMS = new Set(['mr', 'mime', 'jr', 'ho', 'oh', 'nidoran', 'farfetchd', "farfetch'd", 'type']);

function titleizeEntityName(name) {
  if (!name) return name;
  return name
    .split('-')
    .map((part) => (ACRONYMS.has(part.toLowerCase()) ? part.toUpperCase() : titleCase(part)))
    .join(' ');
}

const STAT_KEYS = {
  hp: 'hp',
  attack: 'attack',
  defense: 'defense',
  'special-attack': 'specialAttack',
  'special-defense': 'specialDefense',
  speed: 'speed',
};

function buildStatBlock(statRows) {
  const block = { hp: 0, attack: 0, defense: 0, specialAttack: 0, specialDefense: 0, speed: 0 };
  for (const row of statRows) {
    const key = STAT_KEYS[row.pokemon_v2_stat.name];
    if (key) block[key] = row.base_stat;
  }
  return block;
}

function buildSprites(id) {
  return {
    officialArtwork: SPRITES.officialArtwork(id),
    officialArtworkShiny: SPRITES.officialArtworkShiny(id),
    homeArtwork: SPRITES.homeArtwork(id),
    frontDefault: SPRITES.frontDefault(id),
    frontShiny: SPRITES.frontShiny(id),
    pixelArt: SPRITES.pixelArt(id),
    spritesheet: SPRITES.spritesheet(id),
  };
}

// ---------------------------------------------------------------------------
// Cliente GraphQL
// ---------------------------------------------------------------------------

async function gql(query) {
  const response = await fetch(GRAPHQL_ENDPOINT, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ query }),
  });

  if (!response.ok) throw new Error(`GraphQL ${response.status} ${response.statusText}`);

  const payload = await response.json();
  if (payload.errors?.length) throw new Error(`GraphQL errors:\n${JSON.stringify(payload.errors, null, 2)}`);

  return payload.data;
}

// ---------------------------------------------------------------------------
// Cliente REST con reintentos y concurrencia limitada
// ---------------------------------------------------------------------------

const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

async function fetchJsonWithRetry(url, attempts = 4) {
  let lastError;
  for (let attempt = 1; attempt <= attempts; attempt += 1) {
    try {
      const response = await fetch(url, { headers: { Accept: 'application/json' } });
      if (response.status === 404) return null;
      if (!response.ok) throw new Error(`HTTP ${response.status} ${url}`);
      return await response.json();
    } catch (error) {
      lastError = error;
      if (attempt < attempts) await sleep(250 * 2 ** (attempt - 1));
    }
  }
  throw lastError;
}

/** Ejecuta `worker` sobre `items` con un maximo de `limit` operaciones en vuelo. */
async function mapWithConcurrency(items, limit, worker) {
  const results = new Array(items.length);
  let cursor = 0;

  async function run() {
    while (cursor < items.length) {
      const index = cursor;
      cursor += 1;
      results[index] = await worker(items[index], index);
    }
  }

  await Promise.all(Array.from({ length: Math.min(limit, items.length) }, run));
  return results;
}

/**
 * Descarga un catalogo paginado de la API REST y devuelve un mapa
 * `id -> name`. Necesario porque las URLs de la API REST solo contienen ids
 * numericos, no nombres.
 */
async function fetchIdToNameMap(resource) {
  const response = await fetch(`${REST_BASE}/${resource}?limit=100000`, { headers: { Accept: 'application/json' } });
  if (!response.ok) throw new Error(`No se pudo leer el catalogo '${resource}': HTTP ${response.status}`);

  const payload = await response.json();
  const map = new Map();
  for (const entry of payload.results) map.set(resourceId(entry.url), entry.name);
  return map;
}

// ---------------------------------------------------------------------------
// Consultas
// ---------------------------------------------------------------------------

const POKEMON_QUERY = /* GraphQL */ `
{
  pokemon_v2_pokemon(where: { is_default: { _eq: true } }, order_by: { id: asc }) {
    id
    name
    height
    weight
    base_experience
    pokemon_v2_pokemontypes(order_by: { slot: asc }) {
      slot
      pokemon_v2_type { id name }
    }
    pokemon_v2_pokemonstats {
      base_stat
      effort
      pokemon_v2_stat { id name }
    }
    pokemon_v2_pokemonabilities(order_by: { slot: asc }) {
      slot
      is_hidden
      pokemon_v2_ability { id name }
    }
    pokemon_v2_pokemonspecy {
      id
      name
      generation_id
      is_legendary
      is_mythical
      is_baby
      capture_rate
      base_happiness
      gender_rate
      hatch_counter
      has_gender_differences
      growth_rate_id
      pokemon_color_id
      pokemon_habitat_id
      pokemon_shape_id
      evolution_chain_id
      evolves_from_species_id
      pokemon_v2_generation { id name pokemon_v2_region { id name } }
      pokemon_v2_growthrate { id name }
      pokemon_v2_pokemoncolor { id name }
      pokemon_v2_pokemonhabitat { id name }
      pokemon_v2_pokemonshape { id name }
      pokemon_v2_pokemonegggroups { pokemon_v2_egggroup { id name } }
    }
  }
}
`;

const LOOKUP_QUERY = /* GraphQL */ `
{
  pokemon_v2_type(order_by: { id: asc }) { id name }
  pokemon_v2_ability(order_by: { id: asc }) {
    id
    name
    is_main_series
    pokemon_v2_abilityeffecttexts(where: { language_id: { _eq: ${LANGUAGE_ENGLISH_ID} } }) { short_effect }
  }
  pokemon_v2_egggroup(order_by: { id: asc }) { id name }
  pokemon_v2_stat(order_by: { id: asc }) { id name is_battle_only }
  pokemon_v2_generation(order_by: { id: asc }) { id name pokemon_v2_region { id name } }
  pokemon_v2_growthrate(order_by: { id: asc }) { id name formula }
  pokemon_v2_pokemoncolor(order_by: { id: asc }) { id name }
  pokemon_v2_pokemonhabitat(order_by: { id: asc }) { id name }
  pokemon_v2_pokemonshape(order_by: { id: asc }) { id name }
}
`;

const SPECIES_DETAILS_QUERY = /* GraphQL */ `
{
  pokemon_v2_pokemonspeciesname(where: { language_id: { _eq: ${LANGUAGE_ENGLISH_ID} } }) {
    pokemon_species_id
    genus
  }
  pokemon_v2_pokemonspeciesflavortext(where: { language_id: { _eq: ${LANGUAGE_ENGLISH_ID} } }) {
    pokemon_species_id
    flavor_text
    pokemon_v2_version { id name }
  }
}
`;

// ---------------------------------------------------------------------------
// Reconstruccion de las cadenas evolutivas
// ---------------------------------------------------------------------------

/** Extrae el id numerico del final de una URL de recurso de PokeAPI. */
function resourceId(url) {
  return Number.parseInt(url.split('/').filter(Boolean).at(-1), 10);
}

function mapRelativePhysicalStats(value) {
  if (value === undefined || value === null) return null;
  if (typeof value === 'number') return value;
  const toNumber = (v) => (v === 1 ? 1 : v === 0 ? 0 : v === -1 ? -1 : 0);
  return [toNumber(value['0']), toNumber(value['1']), toNumber(value['2'])];
}

/**
 * Aplana el arbol de una cadena evolutiva en una lista plana de transiciones
 * `{ fromSpeciesId, toSpeciesId, conditions }`.
 *
 * Se descartan los nodos "party" (Wobbuffet -> Wynaut), que en los juegos
 *companen a un Pokemon party en lugar de evolucionar de el.
 */
/**
 * Traduce las URLs de la API REST (que solo contienen ids numericos) a
 * nombres legibles, usando catalogos `id -> name` descargados una sola vez.
 */
function createConditionTranslator(catalogs) {
  const lookup = (map, url) => (url ? map.get(resourceId(url)) ?? null : null);

  return (detail) => ({
    trigger: lookup(catalogs.triggers, detail.trigger?.url) ?? 'unknown',
    minLevel: detail.min_level ?? null,
    item: lookup(catalogs.items, detail.item?.url),
    heldItem: lookup(catalogs.items, detail.held_item?.url),
    location: lookup(catalogs.locations, detail.location?.url),
    gender: detail.gender != null ? catalogs.genders.get(detail.gender) ?? null : null,
    knownMove: lookup(catalogs.moves, detail.known_move?.url),
    knownMoveType: lookup(catalogs.types, detail.known_move_type?.url),
    timeOfDay: detail.time_of_day || null,
    minHappiness: detail.min_happiness ?? null,
    minAffection: detail.min_affection ?? null,
    minBeauty: detail.min_beauty ?? null,
    needsOverworldRain: detail.needs_overworld_rain ?? false,
    turnUpsideDown: detail.turn_upside_down ?? false,
    relativePhysicalStats: mapRelativePhysicalStats(detail.relative_physical_stats),
    partyType: lookup(catalogs.types, detail.party_type?.url),
    tradeSpecies: lookup(catalogs.species, detail.trade_species?.url),
  });
}

/**
 * Aplana el arbol de una cadena evolutiva en una lista de transiciones
 * `{ fromSpeciesId, toSpeciesId, conditions }`.
 *
 * Se descartan los nodos "party" (Wobbuffet -> Wynaut), que en los juegos
 * companen a un Pokemon party en lugar de evolucionar de el.
 */
function flattenEvolutionChain(chain, translate) {
  const transitions = [];
  const rootId = resourceId(chain.chain.species.url);

  function walk(node, fromSpeciesId) {
    for (const child of node.evolves_to ?? []) {
      const toSpeciesId = resourceId(child.species.url);

      for (const detail of child.evolution_details ?? []) {
        // Transicion de "party Pokemon": no es una evolucion real.
        if (detail.party_species) continue;

        transitions.push({ fromSpeciesId, toSpeciesId, conditions: translate(detail) });
      }

      walk(child, toSpeciesId);
    }
  }

  walk(chain.chain, rootId);
  return transitions;
}

async function fetchAllEvolutionChains(chainIds, translate) {
  console.log(`> descargando ${chainIds.length} cadenas evolutivas (REST, concurrencia 24)...`);
  const chains = await mapWithConcurrency(chainIds, 24, async (chainId) =>
    fetchJsonWithRetry(`${REST_BASE}/evolution-chain/${chainId}`),
  );

  const transitions = [];
  for (const chain of chains) {
    if (chain) transitions.push(...flattenEvolutionChain(chain, translate));
  }
  return transitions;
}

// ---------------------------------------------------------------------------
// Construccion del dataset
// ---------------------------------------------------------------------------

async function main() {
  const outPath = process.argv[2];
  if (!outPath) {
    console.error('Uso: node build-dataset.mjs <salida.json>');
    process.exit(1);
  }

  console.log('> descargando pokemon (GraphQL)...');
  const { pokemon_v2_pokemon: rawPokemon } = await gql(POKEMON_QUERY);

  console.log('> descargando catalogos (GraphQL)...');
  const lookups = await gql(LOOKUP_QUERY);

  console.log('> descargando generos y descripciones (GraphQL)...');
  const details = await gql(SPECIES_DETAILS_QUERY);

  console.log('> descargando catalogos de referencia (REST)...');
  const catalogs = {
    triggers: await fetchIdToNameMap('evolution-trigger'),
    items: await fetchIdToNameMap('item'),
    locations: await fetchIdToNameMap('location'),
    moves: await fetchIdToNameMap('move'),
    types: await fetchIdToNameMap('type'),
    species: await fetchIdToNameMap('pokemon-species'),
    genders: new Map([[1, 'female'], [2, 'male'], [3, 'genderless']]),
  };

  // --- Indices auxiliares ---------------------------------------------------

  const genusBySpecies = new Map(details.pokemon_v2_pokemonspeciesname.map((n) => [n.pokemon_species_id, n.genus]));

  // Descripcion en ingles: nos quedamos con la del juego mas reciente.
  const bestFlavorBySpecies = new Map();
  for (const row of details.pokemon_v2_pokemonspeciesflavortext) {
    const current = bestFlavorBySpecies.get(row.pokemon_species_id);
    if (!current || row.pokemon_v2_version.id > current.versionId) {
      bestFlavorBySpecies.set(row.pokemon_species_id, {
        versionId: row.pokemon_v2_version.id,
        text: row.flavor_text,
      });
    }
  }

  const chainIds = [...new Set(rawPokemon.map((p) => p.pokemon_v2_pokemonspecy.evolution_chain_id))].sort((a, b) => a - b);
  const translate = createConditionTranslator(catalogs);
  const transitions = await fetchAllEvolutionChains(chainIds, translate);

  const evolvesToBySpeciesId = new Map();
  for (const t of transitions) {
    if (!evolvesToBySpeciesId.has(t.fromSpeciesId)) evolvesToBySpeciesId.set(t.fromSpeciesId, []);
    evolvesToBySpeciesId.get(t.fromSpeciesId).push(t);
  }

  // Dos APO por evolucion:
  //  - `pokemonNameBySpeciesId` resuelve el nombre del Pokemon por defecto de
  //    una especie (importante: muchas especies tienen una forma principal con
  //    sufijo, p. ej. especie "dudunsparce" -> Pokemon "dudunsparce-two-segment").
  //  - `pokemonIdByName` permite resolver tanto por id como por nombre.
  const pokemonNameBySpeciesId = new Map(rawPokemon.map((p) => [p.pokemon_v2_pokemonspecy.id, p.name]));
  const pokemonIdByName = new Map(rawPokemon.map((p) => [p.name, p.id]));
  const speciesNameById = new Map(rawPokemon.map((p) => [p.pokemon_v2_pokemonspecy.id, p.pokemon_v2_pokemonspecy.name]));

  // --- Generaciones ---------------------------------------------------------

  const countByGenerationId = new Map();
  for (const p of rawPokemon) {
    const id = p.pokemon_v2_pokemonspecy.generation_id;
    countByGenerationId.set(id, (countByGenerationId.get(id) ?? 0) + 1);
  }

  const generations = lookups.pokemon_v2_generation
    .filter((g) => countByGenerationId.has(g.id))
    .map((g) => ({
      id: g.id,
      slug: g.name,
      name: titleCase(g.name),
      region: g.pokemon_v2_region?.name ?? null,
      regionName: g.pokemon_v2_region ? titleCase(g.pokemon_v2_region.name) : null,
      pokemonCount: countByGenerationId.get(g.id),
    }));

  // --- Pokemon --------------------------------------------------------------

  let discardedEvolutions = 0;

  const pokemon = rawPokemon.map((p) => {
    const species = p.pokemon_v2_pokemonspecy;
    const stats = buildStatBlock(p.pokemon_v2_pokemonstats);
    const flavor = bestFlavorBySpecies.get(species.id);

    // Una misma transicion puede admitir varios requisitos mutuamente
    // excluyentes (Eevee -> Glaceon por 6 piedras de hielo o Knowing move).
    // Se agrupa por Pokemon destino para no duplicar la evolucion, y se
    // eliminan los requisitos identicos que PokeAPI repite entre formas.
    const grouped = new Map();
    for (const t of evolvesToBySpeciesId.get(species.id) ?? []) {
      const targetName = pokemonNameBySpeciesId.get(t.toSpeciesId);
      if (!targetName || !pokemonIdByName.has(targetName)) {
        discardedEvolutions += 1;
        continue;
      }

      if (!grouped.has(targetName)) grouped.set(targetName, new Map());
      grouped.get(targetName).set(JSON.stringify(t.conditions), t.conditions);
    }

    const evolvesTo = [...grouped.entries()]
      .map(([name, requirements]) => ({
        id: pokemonIdByName.get(name),
        name,
        displayName: titleizeEntityName(name),
        requirements: [...requirements.values()],
      }))
      .sort((a, b) => a.id - b.id);

    return {
      id: p.id,
      name: p.name,
      displayName: titleizeEntityName(p.name),
      genus: genusBySpecies.get(species.id) ?? null,
      description: flavor?.text ?? null,
      speciesId: species.id,
      generationId: species.pokemon_v2_generation.id,
      generation: species.pokemon_v2_generation.name,
      region: species.pokemon_v2_generation.pokemon_v2_region?.name ?? null,
      types: p.pokemon_v2_pokemontypes.map((t) => t.pokemon_v2_type.name),
      height: p.height,
      weight: p.weight,
      baseExperience: p.base_experience ?? null,
      stats,
      totalStats: stats.hp + stats.attack + stats.defense + stats.specialAttack + stats.specialDefense + stats.speed,
      abilities: p.pokemon_v2_pokemonabilities.map((a) => ({
        name: a.pokemon_v2_ability.name,
        isHidden: a.is_hidden,
        slot: a.slot,
      })),
      isLegendary: species.is_legendary,
      isMythical: species.is_mythical,
      isBaby: species.is_baby ?? false,
      captureRate: species.capture_rate,
      baseHappiness: species.base_happiness,
      genderRate: species.gender_rate,
      hasGenderDifferences: species.has_gender_differences,
      hatchCounter: species.hatch_counter,
      growthRate: species.pokemon_v2_growthrate?.name ?? null,
      eggGroups: species.pokemon_v2_pokemonegggroups.map((e) => e.pokemon_v2_egggroup.name).sort(),
      color: species.pokemon_v2_pokemoncolor?.name ?? null,
      habitat: species.pokemon_v2_pokemonhabitat?.name ?? null,
      shape: species.pokemon_v2_pokemonshape?.name ?? null,
      evolvesFrom: species.evolves_from_species_id ? pokemonNameBySpeciesId.get(species.evolves_from_species_id) ?? null : null,
      evolutionChainId: species.evolution_chain_id,
      evolvesTo,
      sprites: buildSprites(p.id),
    };
  });

  // --- Catalogos auxiliares -------------------------------------------------

  const types = lookups.pokemon_v2_type.map((t) => ({ id: t.id, slug: t.name, name: titleCase(t.name) }));

  const abilities = lookups.pokemon_v2_ability
    .map((a) => ({
      id: a.id,
      slug: a.name,
      name: titleizeEntityName(a.name),
      isMainSeries: a.is_main_series,
      shortEffect: a.pokemon_v2_abilityeffecttexts[0]?.short_effect ?? null,
    }))
    .sort((a, b) => a.id - b.id);

  const growthRates = lookups.pokemon_v2_growthrate.map((g) => ({ id: g.id, slug: g.name, name: titleCase(g.name), formula: g.formula }));
  const eggGroups = lookups.pokemon_v2_egggroup.map((e) => ({ id: e.id, slug: e.name, name: titleCase(e.name) }));
  const colors = lookups.pokemon_v2_pokemoncolor.map((c) => ({ id: c.id, slug: c.name, name: titleCase(c.name) }));
  const habitats = lookups.pokemon_v2_pokemonhabitat.map((h) => ({ id: h.id, slug: h.name, name: titleCase(h.name) }));
  const shapes = lookups.pokemon_v2_pokemonshape.map((s) => ({ id: s.id, slug: s.name, name: titleCase(s.name) }));
  const statsCatalog = lookups.pokemon_v2_stat
    .filter((s) => s.name in STAT_KEYS)
    .map((s) => ({ id: s.id, slug: s.name, name: titleCase(s.name), isBattleOnly: s.is_battle_only }));

  // --- Documento final ------------------------------------------------------

  const evolutionCount = pokemon.reduce((total, p) => total + p.evolvesTo.length, 0);

  const dataset = {
    metadata: {
      schemaVersion: '1.0.0',
      source: 'PokeAPI',
      sourceVersion: POKEAPI_VERSION,
      sourceUrl: 'https://pokeapi.co',
      license: 'BSD-3-Clause',
      attribution: 'Datos provistos por PokeAPI (pokeapi.co). Sprites propiedad de The Pokemon Company International.',
      generatedAtUtc: new Date().toISOString(),
      pokemonCount: pokemon.length,
      generationCount: generations.length,
      evolutionCount,
    },
    generations,
    types,
    abilities,
    eggGroups,
    growthRates,
    colors,
    habitats,
    shapes,
    stats: statsCatalog,
    pokemon,
  };

  const json = JSON.stringify(dataset);
  await writeFile(outPath, json, 'utf8');

  console.log(`OK  pokemon=${pokemon.length}  generaciones=${generations.length}  tipos=${types.length}  habilidades=${abilities.length}  evoluciones=${evolutionCount}`);
  console.log(`    transiciones=${transitions.length}  descartadas=${discardedEvolutions}`);
  console.log(`    tamaño=${(json.length / 1024 / 1024).toFixed(2)} MB`);
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});
