import http from 'node:http';
import path from 'node:path';
import { createHash } from 'node:crypto';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { DatabaseSync } from 'node:sqlite';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const publicDir = path.join(__dirname, 'public');
const rawgCacheDir = path.join(publicDir, 'rawg-cache');
const dataDir = path.join(__dirname, 'data');
const envPath = path.join(__dirname, '.env');
const dbPath = path.join(dataDir, 'relationship-planner.db');
const port = Number(process.env.PORT || 3000);
const host = process.env.HOST || '0.0.0.0';

function loadEnvFile(filePath) {
  if (!existsSync(filePath)) {
    return;
  }

  const contents = readFileSync(filePath, 'utf8');
  for (const line of contents.split(/\r?\n/)) {
    const trimmed = line.trim();
    if (!trimmed || trimmed.startsWith('#')) {
      continue;
    }
    const equalsIndex = trimmed.indexOf('=');
    if (equalsIndex === -1) {
      continue;
    }
    const key = trimmed.slice(0, equalsIndex).trim();
    let value = trimmed.slice(equalsIndex + 1).trim();
    if ((value.startsWith('"') && value.endsWith('"')) || (value.startsWith("'") && value.endsWith("'"))) {
      value = value.slice(1, -1);
    }
    if (key && process.env[key] === undefined) {
      process.env[key] = value;
    }
  }
}

loadEnvFile(envPath);

if (!existsSync(dataDir)) {
  mkdirSync(dataDir, { recursive: true });
}

if (!existsSync(rawgCacheDir)) {
  mkdirSync(rawgCacheDir, { recursive: true });
}

const db = new DatabaseSync(dbPath);
db.exec('PRAGMA journal_mode = WAL;');

function ensureSchema() {
  const existing = db
    .prepare("SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'entries'")
    .get();

  if (!existing) {
    db.exec(`
      CREATE TABLE entries (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        kind TEXT NOT NULL CHECK(kind IN ('date', 'reminder', 'countdown', 'memory', 'got_together', 'game_plan')),
        title TEXT NOT NULL,
        details TEXT NOT NULL DEFAULT '',
        event_date TEXT NOT NULL,
        reminder_at TEXT NOT NULL DEFAULT '',
        game_status TEXT NOT NULL DEFAULT 'want_to_play',
        rawg_game_id INTEGER NOT NULL DEFAULT 0,
        rawg_slug TEXT NOT NULL DEFAULT '',
        rawg_background_image TEXT NOT NULL DEFAULT '',
        rawg_platforms TEXT NOT NULL DEFAULT '',
        rawg_metacritic INTEGER NOT NULL DEFAULT 0,
        rawg_released TEXT NOT NULL DEFAULT '',
        created_at TEXT NOT NULL DEFAULT (datetime('now')),
        updated_at TEXT NOT NULL DEFAULT (datetime('now'))
      );
    `);
    return;
  }

  const legacyColumns = db.prepare("PRAGMA table_info(entries)").all().map((column) => column.name);
  const columns = db.prepare("PRAGMA table_info(entries)").all().map((column) => column.name);
  const requiredColumns = [
    'game_status',
    'rawg_game_id',
    'rawg_slug',
    'rawg_background_image',
    'rawg_platforms',
    'rawg_metacritic',
    'rawg_released'
  ];
  const needsGamePlan = !(existing.sql || '').includes("'game_plan'");
  const missingColumns = requiredColumns.filter((column) => !columns.includes(column));

  if (!needsGamePlan && missingColumns.length === 0) {
    return;
  }

  const hasRawgColumns = requiredColumns.every((column) => legacyColumns.includes(column));
  const hasGameStatusColumn = legacyColumns.includes('game_status');
  const selectColumns = hasRawgColumns
    ? `
      id,
      kind,
      title,
      details,
      event_date,
      reminder_at,
      ${hasGameStatusColumn ? "COALESCE(game_status, 'want_to_play')" : "'want_to_play'"},
      COALESCE(rawg_game_id, 0),
      COALESCE(rawg_slug, ''),
      COALESCE(rawg_background_image, ''),
      COALESCE(rawg_platforms, ''),
      COALESCE(rawg_metacritic, 0),
      COALESCE(rawg_released, ''),
      created_at,
      updated_at
    `
    : `
      id,
      kind,
      title,
      details,
      event_date,
      reminder_at,
      'want_to_play',
      0,
      '',
      '',
      '',
      0,
      '',
      created_at,
      updated_at
    `;

  db.exec(`
    ALTER TABLE entries RENAME TO entries_legacy;
    CREATE TABLE entries (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      kind TEXT NOT NULL CHECK(kind IN ('date', 'reminder', 'countdown', 'memory', 'got_together', 'game_plan')),
      title TEXT NOT NULL,
      details TEXT NOT NULL DEFAULT '',
      event_date TEXT NOT NULL,
      reminder_at TEXT NOT NULL DEFAULT '',
      game_status TEXT NOT NULL DEFAULT 'want_to_play',
      rawg_game_id INTEGER NOT NULL DEFAULT 0,
      rawg_slug TEXT NOT NULL DEFAULT '',
      rawg_background_image TEXT NOT NULL DEFAULT '',
      rawg_platforms TEXT NOT NULL DEFAULT '',
      rawg_metacritic INTEGER NOT NULL DEFAULT 0,
      rawg_released TEXT NOT NULL DEFAULT '',
      created_at TEXT NOT NULL DEFAULT (datetime('now')),
      updated_at TEXT NOT NULL DEFAULT (datetime('now'))
    );
    INSERT INTO entries (id, kind, title, details, event_date, reminder_at, game_status, rawg_game_id, rawg_slug, rawg_background_image, rawg_platforms, rawg_metacritic, rawg_released, created_at, updated_at)
    SELECT ${selectColumns} FROM entries_legacy;
    DROP TABLE entries_legacy;
  `);
}

ensureSchema();

db.exec(`
  CREATE TABLE IF NOT EXISTS settings (
    key TEXT PRIMARY KEY,
    value TEXT NOT NULL DEFAULT ''
  );
`);

const defaultSettings = {
  couple_name: '',
  relationship_started_at: '',
  default_reminder_minutes: '60',
  preferred_theme: 'light'
};

for (const [key, value] of Object.entries(defaultSettings)) {
  db.prepare('INSERT OR IGNORE INTO settings (key, value) VALUES (?, ?)').run(key, value);
}

function sendJson(res, statusCode, payload) {
  const body = JSON.stringify(payload);
  res.writeHead(statusCode, {
    'Content-Type': 'application/json; charset=utf-8',
    'Content-Length': Buffer.byteLength(body),
    'Cache-Control': 'no-store'
  });
  res.end(body);
}

function sendText(res, statusCode, text, contentType = 'text/plain; charset=utf-8') {
  res.writeHead(statusCode, {
    'Content-Type': contentType,
    'Cache-Control': 'no-store'
  });
  res.end(text);
}

function contentTypeForImage(extname) {
  return {
    '.jpg': 'image/jpeg',
    '.jpeg': 'image/jpeg',
    '.png': 'image/png',
    '.gif': 'image/gif',
    '.webp': 'image/webp',
    '.avif': 'image/avif',
    '.svg': 'image/svg+xml'
  }[extname] || 'application/octet-stream';
}

function readBody(req) {
  return new Promise((resolve, reject) => {
    let raw = '';
    req.on('data', (chunk) => {
      raw += chunk;
      if (raw.length > 1_000_000) {
        reject(new Error('Request body too large'));
        req.destroy();
      }
    });
    req.on('end', () => {
      if (!raw) {
        resolve({});
        return;
      }
      try {
        resolve(JSON.parse(raw));
      } catch {
        reject(new Error('Invalid JSON body'));
      }
    });
    req.on('error', reject);
  });
}

function isValidIso(value) {
  return typeof value === 'string' && !Number.isNaN(Date.parse(value));
}

function isRemoteImageUrl(value) {
  return typeof value === 'string' && /^https?:\/\//i.test(value);
}

function cacheFileNameForUrl(urlString, keyHint = '') {
  const parsed = new URL(urlString);
  const ext = path.extname(parsed.pathname).toLowerCase();
  const normalizedExt = ['.jpg', '.jpeg', '.png', '.gif', '.webp', '.avif', '.svg'].includes(ext) ? ext : '.jpg';
  const hash = createHash('sha1').update(urlString).update(keyHint).digest('hex').slice(0, 24);
  return `${hash}${normalizedExt}`;
}

async function cacheRawgImage(sourceUrl, keyHint = '') {
  if (!isRemoteImageUrl(sourceUrl)) {
    return sourceUrl || '';
  }

  const fileName = cacheFileNameForUrl(sourceUrl, keyHint);
  const publicPath = `/rawg-cache/${fileName}`;
  const diskPath = path.join(rawgCacheDir, fileName);

  if (existsSync(diskPath)) {
    return publicPath;
  }

  try {
    const response = await fetch(sourceUrl);
    if (!response.ok) {
      return sourceUrl;
    }
    const buffer = Buffer.from(await response.arrayBuffer());
    writeFileSync(diskPath, buffer);
    return publicPath;
  } catch {
    return sourceUrl;
  }
}

function normalizeEntry(row) {
  const eventDate = new Date(row.event_date).toISOString();
  const reminderAt = row.reminder_at ? new Date(row.reminder_at).toISOString() : '';
  const now = Date.now();
  const eventMs = Date.parse(eventDate);
  const daysUntil = Math.ceil((eventMs - now) / (24 * 60 * 60 * 1000));
  return {
    id: row.id,
    kind: row.kind,
    title: row.title,
    details: row.details,
    eventDate,
    reminderAt,
    gameStatus: row.game_status || 'want_to_play',
    rawgGameId: row.rawg_game_id || 0,
    rawgSlug: row.rawg_slug || '',
    rawgBackgroundImage: row.rawg_background_image || '',
    rawgPlatforms: row.rawg_platforms ? JSON.parse(row.rawg_platforms) : [],
    rawgMetacritic: row.rawg_metacritic || 0,
    rawgReleased: row.rawg_released || '',
    createdAt: row.created_at,
    updatedAt: row.updated_at,
    daysUntil
  };
}

async function backfillRawgArtwork() {
  const rows = db.prepare(`
    SELECT id, rawg_game_id, rawg_slug, rawg_background_image
    FROM entries
    WHERE kind = 'game_plan'
      AND rawg_background_image LIKE 'http%'
  `).all();

  for (const row of rows) {
    const localPath = await cacheRawgImage(
      row.rawg_background_image,
      [row.rawg_game_id || 0, row.rawg_slug || '', row.id].filter(Boolean).join('-')
    );
    if (localPath && localPath !== row.rawg_background_image) {
      db.prepare('UPDATE entries SET rawg_background_image = ?, updated_at = datetime(\'now\') WHERE id = ?')
        .run(localPath, row.id);
    }
  }
}

async function prepareRawgArtwork(entry, hint = '') {
  if (entry.kind !== 'game_plan') {
    return entry;
  }
  entry.rawg_background_image = await cacheRawgImage(
    entry.rawg_background_image || '',
    [entry.rawg_game_id || 0, entry.rawg_slug || '', hint].filter(Boolean).join('-')
  );
  return entry;
}

function isKnownKind(kind) {
  return ['date', 'reminder', 'countdown', 'memory', 'got_together', 'game_plan'].includes(kind);
}

function getSetting(key, fallback = '') {
  const row = db.prepare('SELECT value FROM settings WHERE key = ?').get(key);
  return row?.value ?? fallback;
}

function setSetting(key, value) {
  db.prepare(`
    INSERT INTO settings (key, value)
    VALUES (?, ?)
    ON CONFLICT(key) DO UPDATE SET value = excluded.value
  `).run(key, String(value ?? ''));
}

function readSettings() {
  return {
    coupleName: getSetting('couple_name', ''),
    relationshipStartedAt: getSetting('relationship_started_at', ''),
    defaultReminderMinutes: Number(getSetting('default_reminder_minutes', '60')) || 60,
    preferredTheme: getSetting('preferred_theme', 'light')
  };
}

function updateSettingsFromBody(body) {
  if (body.coupleName !== undefined) {
    setSetting('couple_name', typeof body.coupleName === 'string' ? body.coupleName.trim() : '');
  }
  if (body.relationshipStartedAt !== undefined) {
    if (body.relationshipStartedAt && !isValidIso(body.relationshipStartedAt)) {
      const error = new Error('relationshipStartedAt must be an ISO date string');
      error.statusCode = 400;
      throw error;
    }
    setSetting('relationship_started_at', body.relationshipStartedAt || '');
  }
  if (body.defaultReminderMinutes !== undefined) {
    const minutes = Number(body.defaultReminderMinutes);
    if (!Number.isInteger(minutes) || minutes < 0 || minutes > 1440) {
      const error = new Error('defaultReminderMinutes must be an integer between 0 and 1440');
      error.statusCode = 400;
      throw error;
    }
    setSetting('default_reminder_minutes', String(minutes));
  }
  if (body.preferredTheme !== undefined) {
    if (!['light', 'dark'].includes(body.preferredTheme)) {
      const error = new Error('preferredTheme must be light or dark');
      error.statusCode = 400;
      throw error;
    }
    setSetting('preferred_theme', body.preferredTheme);
  }
}

function parsePlatforms(rawValue) {
  if (!rawValue) {
    return [];
  }
  try {
    const parsed = JSON.parse(rawValue);
    return Array.isArray(parsed) ? parsed : [];
  } catch {
    return [];
  }
}

function summarizePlatforms(platforms) {
  return platforms
    .map((platform) => platform?.platform?.name || platform?.platform?.slug || '')
    .filter(Boolean)
    .join(', ');
}

async function searchRawgGames(query) {
  const apiKey = process.env.RAWG_API_KEY;
  if (!apiKey) {
    const error = new Error('RAWG_API_KEY is not configured on the server.');
    error.statusCode = 500;
    throw error;
  }

  const url = new URL('https://api.rawg.io/api/games');
  url.searchParams.set('key', apiKey);
  url.searchParams.set('search', query);
  url.searchParams.set('page_size', '8');
  url.searchParams.set('search_precise', 'true');
  url.searchParams.set('ordering', '-added');

  const response = await fetch(url);
  if (!response.ok) {
    const error = new Error(`RAWG request failed with status ${response.status}`);
    error.statusCode = response.status;
    throw error;
  }

  const payload = await response.json();
  return (payload.results || []).map((game) => ({
    id: game.id,
    name: game.name,
    slug: game.slug,
    released: game.released || '',
    metacritic: game.metacritic || 0,
    backgroundImage: game.background_image || '',
    platforms: game.platforms || [],
    platformNames: summarizePlatforms(game.platforms || []),
    playtime: game.playtime || 0
  }));
}

function getDefaultReminderIso(eventDateIso) {
  const minutes = Number(getSetting('default_reminder_minutes', '60'));
  if (!Number.isFinite(minutes) || minutes <= 0) {
    return '';
  }
  const date = Date.parse(eventDateIso);
  if (Number.isNaN(date)) {
    return '';
  }
  return new Date(date - minutes * 60_000).toISOString();
}

function serveStatic(req, res, pathname) {
  const safePath = pathname === '/' ? '/index.html' : pathname;
  const filePath = path.normalize(path.join(publicDir, safePath));
  if (!filePath.startsWith(publicDir)) {
    sendText(res, 403, 'Forbidden');
    return true;
  }
  if (!existsSync(filePath) || !/\.html?$|\.css$|\.js$|\.svg$|\.png$|\.jpe?g$|\.webp$|\.gif$|\.avif$|\.ico$/i.test(filePath)) {
    return false;
  }
  const ext = path.extname(filePath).toLowerCase();
  const contentType = {
    '.html': 'text/html; charset=utf-8',
    '.css': 'text/css; charset=utf-8',
    '.js': 'text/javascript; charset=utf-8',
    '.svg': 'image/svg+xml',
    '.png': 'image/png',
    '.jpg': 'image/jpeg',
    '.jpeg': 'image/jpeg',
    '.webp': 'image/webp',
    '.gif': 'image/gif',
    '.avif': 'image/avif',
    '.ico': 'image/x-icon'
  }[ext] || contentTypeForImage(ext) || 'application/octet-stream';
  sendText(res, 200, readFileSync(filePath), contentType);
  return true;
}

const listEntries = db.prepare(`
  SELECT id, kind, title, details, event_date, reminder_at, game_status, rawg_game_id, rawg_slug, rawg_background_image, rawg_platforms, rawg_metacritic, rawg_released, created_at, updated_at
  FROM entries
  ORDER BY datetime(event_date) ASC, id DESC
`);

const getEntry = db.prepare(`
  SELECT id, kind, title, details, event_date, reminder_at, game_status, rawg_game_id, rawg_slug, rawg_background_image, rawg_platforms, rawg_metacritic, rawg_released, created_at, updated_at
  FROM entries
  WHERE id = ?
`);

const insertEntry = db.prepare(`
  INSERT INTO entries (kind, title, details, event_date, reminder_at, game_status, rawg_game_id, rawg_slug, rawg_background_image, rawg_platforms, rawg_metacritic, rawg_released, updated_at)
  VALUES (@kind, @title, @details, @event_date, @reminder_at, @game_status, @rawg_game_id, @rawg_slug, @rawg_background_image, @rawg_platforms, @rawg_metacritic, @rawg_released, datetime('now'))
`);

const updateEntry = db.prepare(`
  UPDATE entries
  SET kind = @kind,
      title = @title,
      details = @details,
      event_date = @event_date,
      reminder_at = @reminder_at,
      game_status = @game_status,
      rawg_game_id = @rawg_game_id,
      rawg_slug = @rawg_slug,
      rawg_background_image = @rawg_background_image,
      rawg_platforms = @rawg_platforms,
      rawg_metacritic = @rawg_metacritic,
      rawg_released = @rawg_released,
      updated_at = datetime('now')
  WHERE id = @id
`);

const deleteEntry = db.prepare('DELETE FROM entries WHERE id = ?');

export function createServer() {
  return http.createServer(async (req, res) => {
    const url = new URL(req.url || '/', `http://${req.headers.host || 'localhost'}`);

    if (req.method === 'GET' && url.pathname === '/health') {
      sendJson(res, 200, { ok: true });
      return;
    }

    if (url.pathname.startsWith('/api/')) {
      try {
        if (req.method === 'GET' && url.pathname === '/api/settings') {
          sendJson(res, 200, { settings: readSettings() });
          return;
        }

        if (req.method === 'PUT' && url.pathname === '/api/settings') {
          const body = await readBody(req);
          updateSettingsFromBody(body);
          sendJson(res, 200, { settings: readSettings() });
          return;
        }

        if (req.method === 'GET' && url.pathname === '/api/rawg/search') {
          const query = (url.searchParams.get('q') || '').trim();
          if (!query) {
            sendJson(res, 400, { error: 'q is required' });
            return;
          }
          const results = await searchRawgGames(query);
          sendJson(res, 200, { results });
          return;
        }

        if (req.method === 'GET' && url.pathname === '/api/entries') {
          const rows = listEntries.all().map(normalizeEntry);
          sendJson(res, 200, { entries: rows });
          return;
        }

        if (req.method === 'GET' && /^\/api\/entries\/\d+$/.test(url.pathname)) {
          const id = Number(url.pathname.split('/').pop());
          const row = getEntry.get(id);
          if (!row) {
            sendJson(res, 404, { error: 'Not found' });
            return;
          }
          sendJson(res, 200, { entry: normalizeEntry(row) });
          return;
        }

        if (req.method === 'POST' && url.pathname === '/api/entries') {
          const body = await readBody(req);
          const entry = {
            kind: body.kind,
            title: typeof body.title === 'string' ? body.title.trim() : '',
            details: typeof body.details === 'string' ? body.details.trim() : '',
            event_date: body.eventDate,
            reminder_at: body.reminderAt || '',
            game_status: typeof body.gameStatus === 'string' ? body.gameStatus : 'want_to_play',
            rawg_game_id: Number(body.rawgGameId || 0),
            rawg_slug: typeof body.rawgSlug === 'string' ? body.rawgSlug : '',
            rawg_background_image: typeof body.rawgBackgroundImage === 'string' ? body.rawgBackgroundImage : '',
            rawg_platforms: JSON.stringify(Array.isArray(body.rawgPlatforms) ? body.rawgPlatforms : []),
            rawg_metacritic: Number(body.rawgMetacritic || 0),
            rawg_released: typeof body.rawgReleased === 'string' ? body.rawgReleased : ''
          };

          if (!isKnownKind(entry.kind)) {
            sendJson(res, 400, { error: 'Invalid kind' });
            return;
          }
          if (!entry.title) {
            sendJson(res, 400, { error: 'Title is required' });
            return;
          }
          if (!isValidIso(entry.event_date)) {
            sendJson(res, 400, { error: 'eventDate must be an ISO date string' });
            return;
          }
          if (entry.reminder_at && !isValidIso(entry.reminder_at)) {
            sendJson(res, 400, { error: 'reminderAt must be an ISO date string' });
            return;
          }
          if (entry.kind === 'game_plan' && !['want_to_play', 'playing', 'played', 'finished'].includes(entry.game_status)) {
            sendJson(res, 400, { error: 'gameStatus must be want_to_play, playing, played, or finished' });
            return;
          }
          if (entry.kind !== 'game_plan') {
            entry.game_status = 'want_to_play';
          }
          await prepareRawgArtwork(entry, `create-${Date.now()}`);
          if (!entry.reminder_at && entry.kind !== 'memory') {
            entry.reminder_at = getDefaultReminderIso(entry.event_date);
          }

          const info = insertEntry.run(entry);
          const created = getEntry.get(info.lastInsertRowid);
          sendJson(res, 201, { entry: normalizeEntry(created) });
          return;
        }

        if (req.method === 'PUT' && /^\/api\/entries\/\d+$/.test(url.pathname)) {
          const id = Number(url.pathname.split('/').pop());
          const existing = getEntry.get(id);
          if (!existing) {
            sendJson(res, 404, { error: 'Not found' });
            return;
          }
          const body = await readBody(req);
          const entry = {
            id,
            kind: body.kind ?? existing.kind,
            title: typeof body.title === 'string' ? body.title.trim() : existing.title,
            details: typeof body.details === 'string' ? body.details.trim() : existing.details,
            event_date: body.eventDate ?? existing.event_date,
            reminder_at: body.reminderAt ?? existing.reminder_at,
            game_status: typeof body.gameStatus === 'string' ? body.gameStatus : existing.game_status || 'want_to_play',
            rawg_game_id: body.rawgGameId ?? existing.rawg_game_id ?? 0,
            rawg_slug: body.rawgSlug ?? existing.rawg_slug ?? '',
            rawg_background_image: body.rawgBackgroundImage ?? existing.rawg_background_image ?? '',
            rawg_platforms: JSON.stringify(body.rawgPlatforms ?? parsePlatforms(existing.rawg_platforms)),
            rawg_metacritic: body.rawgMetacritic ?? existing.rawg_metacritic ?? 0,
            rawg_released: body.rawgReleased ?? existing.rawg_released ?? ''
          };
          if (!isKnownKind(entry.kind)) {
            sendJson(res, 400, { error: 'Invalid kind' });
            return;
          }
          if (!entry.title) {
            sendJson(res, 400, { error: 'Title is required' });
            return;
          }
          if (!isValidIso(entry.event_date)) {
            sendJson(res, 400, { error: 'eventDate must be an ISO date string' });
            return;
          }
          if (entry.reminder_at && !isValidIso(entry.reminder_at)) {
            sendJson(res, 400, { error: 'reminderAt must be an ISO date string' });
            return;
          }
          if (!entry.reminder_at && entry.kind !== 'memory') {
            entry.reminder_at = getDefaultReminderIso(entry.event_date);
          }
          if (entry.kind === 'game_plan' && !['want_to_play', 'playing', 'played', 'finished'].includes(entry.game_status)) {
            sendJson(res, 400, { error: 'gameStatus must be want_to_play, playing, played, or finished' });
            return;
          }
          if (entry.kind !== 'game_plan') {
            entry.game_status = 'want_to_play';
          }
          await prepareRawgArtwork(entry, `update-${id}`);
          updateEntry.run(entry);
          const updated = getEntry.get(id);
          sendJson(res, 200, { entry: normalizeEntry(updated) });
          return;
        }

        if (req.method === 'DELETE' && /^\/api\/entries\/\d+$/.test(url.pathname)) {
          const id = Number(url.pathname.split('/').pop());
          deleteEntry.run(id);
          sendJson(res, 200, { ok: true });
          return;
        }

        sendJson(res, 404, { error: 'API route not found' });
        return;
      } catch (error) {
        sendJson(res, 500, { error: error.message || 'Server error' });
        return;
      }
    }

    if (serveStatic(req, res, url.pathname)) {
      return;
    }

    if (req.method === 'GET' && url.pathname === '/') {
      serveStatic(req, res, '/index.html');
      return;
    }

    sendText(res, 404, 'Not found');
  });
}

if (import.meta.url === pathToFileURL(process.argv[1]).href) {
  await backfillRawgArtwork();
  const server = createServer();
  server.listen(port, host, () => {
    console.log(`Relationship planner running on http://${host}:${port}`);
    console.log(`Database stored at ${dbPath}`);
  });

  process.on('SIGINT', () => {
    server.close(() => {
      db.close();
      process.exit(0);
    });
  });

  process.on('SIGTERM', () => {
    server.close(() => {
      db.close();
      process.exit(0);
    });
  });
}
