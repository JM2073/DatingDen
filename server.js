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
db.exec('PRAGMA foreign_keys = ON;');

function tableColumns(tableName) {
  return db.prepare(`PRAGMA table_info(${tableName})`).all().map((column) => column.name);
}

function ensureSchema() {
  db.exec(`
    CREATE TABLE IF NOT EXISTS users (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      name TEXT NOT NULL,
      created_at TEXT NOT NULL DEFAULT (datetime('now')),
      updated_at TEXT NOT NULL DEFAULT (datetime('now'))
    );

    CREATE TABLE IF NOT EXISTS user_settings (
      user_id INTEGER NOT NULL,
      key TEXT NOT NULL,
      value TEXT NOT NULL DEFAULT '',
      updated_at TEXT NOT NULL DEFAULT (datetime('now')),
      PRIMARY KEY (user_id, key),
      FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
    );

    CREATE TABLE IF NOT EXISTS entry_user_settings (
      entry_id INTEGER NOT NULL,
      user_id INTEGER NOT NULL,
      key TEXT NOT NULL,
      value TEXT NOT NULL DEFAULT '',
      updated_at TEXT NOT NULL DEFAULT (datetime('now')),
      PRIMARY KEY (entry_id, user_id, key),
      FOREIGN KEY (entry_id) REFERENCES entries(id) ON DELETE CASCADE,
      FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
    );

    CREATE TABLE IF NOT EXISTS settings (
      key TEXT PRIMARY KEY,
      value TEXT NOT NULL DEFAULT ''
    );
  `);

  const existingColumns = tableColumns('entries');
  const existingEntryTable = db
    .prepare("SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'entries'")
    .get();
  const existingEntrySql = existingEntryTable?.sql || '';

  if (existingColumns.length === 0) {
    db.exec(`
      CREATE TABLE entries (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        kind TEXT NOT NULL CHECK(kind IN ('date', 'reminder', 'countdown', 'memory', 'got_together', 'game_plan', 'game_status')),
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
        created_by_user_id INTEGER NOT NULL DEFAULT 1,
        updated_by_user_id INTEGER NOT NULL DEFAULT 1,
        created_at TEXT NOT NULL DEFAULT (datetime('now')),
        updated_at TEXT NOT NULL DEFAULT (datetime('now'))
      );
    `);
  } else if (!existingEntrySql.includes("'game_status'")) {
    const legacyColumns = new Set(existingColumns);
    const selectColumns = [
      'id',
      'kind',
      'title',
      'details',
      'event_date',
      'reminder_at',
      legacyColumns.has('game_status') ? "COALESCE(game_status, 'want_to_play')" : "'want_to_play'",
      legacyColumns.has('rawg_game_id') ? 'COALESCE(rawg_game_id, 0)' : '0',
      legacyColumns.has('rawg_slug') ? "COALESCE(rawg_slug, '')" : "''",
      legacyColumns.has('rawg_background_image') ? "COALESCE(rawg_background_image, '')" : "''",
      legacyColumns.has('rawg_platforms') ? "COALESCE(rawg_platforms, '')" : "''",
      legacyColumns.has('rawg_metacritic') ? 'COALESCE(rawg_metacritic, 0)' : '0',
      legacyColumns.has('rawg_released') ? "COALESCE(rawg_released, '')" : "''",
      legacyColumns.has('created_by_user_id') ? 'COALESCE(created_by_user_id, 1)' : '1',
      legacyColumns.has('updated_by_user_id') ? 'COALESCE(updated_by_user_id, 1)' : '1',
      'created_at',
      'updated_at'
    ].join(', ');

    db.exec(`
      ALTER TABLE entries RENAME TO entries_legacy;
      CREATE TABLE entries (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        kind TEXT NOT NULL CHECK(kind IN ('date', 'reminder', 'countdown', 'memory', 'got_together', 'game_plan', 'game_status')),
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
        created_by_user_id INTEGER NOT NULL DEFAULT 1,
        updated_by_user_id INTEGER NOT NULL DEFAULT 1,
        created_at TEXT NOT NULL DEFAULT (datetime('now')),
        updated_at TEXT NOT NULL DEFAULT (datetime('now'))
      );
      INSERT INTO entries (
        id, kind, title, details, event_date, reminder_at, game_status,
        rawg_game_id, rawg_slug, rawg_background_image, rawg_platforms, rawg_metacritic,
        rawg_released, created_by_user_id, updated_by_user_id, created_at, updated_at
      )
      SELECT ${selectColumns} FROM entries_legacy;
      DROP TABLE entries_legacy;
    `);
  } else {
    if (!existingColumns.includes('created_by_user_id')) {
      db.exec(`ALTER TABLE entries ADD COLUMN created_by_user_id INTEGER NOT NULL DEFAULT 1;`);
    }
    if (!existingColumns.includes('updated_by_user_id')) {
      db.exec(`ALTER TABLE entries ADD COLUMN updated_by_user_id INTEGER NOT NULL DEFAULT 1;`);
    }
  }

  const entryUserColumns = tableColumns('entry_user_settings');
  const entryUserForeignKeys = db.prepare('PRAGMA foreign_key_list(entry_user_settings)').all();
  const entryUserNeedsRepair =
    entryUserColumns.length > 0 &&
    entryUserForeignKeys.some((foreignKey) => foreignKey.table !== 'entries');

  if (entryUserNeedsRepair) {
    db.exec(`
      ALTER TABLE entry_user_settings RENAME TO entry_user_settings_legacy;
      CREATE TABLE entry_user_settings (
        entry_id INTEGER NOT NULL,
        user_id INTEGER NOT NULL,
        key TEXT NOT NULL,
        value TEXT NOT NULL DEFAULT '',
        updated_at TEXT NOT NULL DEFAULT (datetime('now')),
        PRIMARY KEY (entry_id, user_id, key),
        FOREIGN KEY (entry_id) REFERENCES entries(id) ON DELETE CASCADE,
        FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
      );
      INSERT INTO entry_user_settings (entry_id, user_id, key, value, updated_at)
      SELECT entry_id, user_id, key, value, updated_at
      FROM entry_user_settings_legacy
      WHERE EXISTS (SELECT 1 FROM entries WHERE entries.id = entry_user_settings_legacy.entry_id);
      DROP TABLE entry_user_settings_legacy;
    `);
  }

  const defaultSettings = {
    couple_name: '',
    relationship_started_at: ''
  };

  for (const [key, value] of Object.entries(defaultSettings)) {
    db.prepare('INSERT OR IGNORE INTO settings (key, value) VALUES (?, ?)').run(key, value);
  }

  const usersCount = db.prepare('SELECT COUNT(*) AS count FROM users').get().count;
  if (usersCount === 0) {
    const info = db.prepare('INSERT INTO users (name) VALUES (?)').run('You');
    const userId = info.lastInsertRowid;
    db.prepare('INSERT OR IGNORE INTO user_settings (user_id, key, value) VALUES (?, ?, ?)').run(userId, 'accent_color', '#1976d2');
    db.prepare('INSERT OR IGNORE INTO user_settings (user_id, key, value) VALUES (?, ?, ?)').run(userId, 'preferred_theme', 'light');
    db.prepare('INSERT OR IGNORE INTO user_settings (user_id, key, value) VALUES (?, ?, ?)').run(userId, 'default_reminder_minutes', '60');
  }
}

ensureSchema();

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

function normalizeEntry(row, context = {}) {
  const eventDate = new Date(row.event_date).toISOString();
  const reminderAt = row.reminder_at ? new Date(row.reminder_at).toISOString() : '';
  const now = Date.now();
  const eventMs = Date.parse(eventDate);
  const daysUntil = Math.ceil((eventMs - now) / (24 * 60 * 60 * 1000));
  const users = context.users || new Map();
  const userStatuses = context.userStatuses || new Map();
  const userRatings = context.userRatings || new Map();
  const createdBy = users.get(row.created_by_user_id) || null;
  const updatedBy = users.get(row.updated_by_user_id) || null;
  let rawgPlatforms = [];
  try {
    rawgPlatforms = row.rawg_platforms ? JSON.parse(row.rawg_platforms) : [];
  } catch {
    rawgPlatforms = [];
  }
  return {
    id: row.id,
    kind: row.kind,
    title: row.title,
    details: row.details,
    eventDate,
    reminderAt,
    gameStatus: row.kind === 'game_plan' || row.kind === 'game_status'
      ? userStatuses.get(row.id) || row.game_status || 'want_to_play'
      : 'want_to_play',
    gameRating: row.kind === 'game_plan' || row.kind === 'game_status'
      ? userRatings.get(row.id) || ''
      : '',
    rawgGameId: row.rawg_game_id || 0,
    rawgSlug: row.rawg_slug || '',
    rawgBackgroundImage: row.rawg_background_image || '',
    rawgPlatforms,
    rawgMetacritic: row.rawg_metacritic || 0,
    rawgReleased: row.rawg_released || '',
    createdByUserId: row.created_by_user_id || 0,
    createdByUserName: createdBy?.name || 'Unknown',
    createdByUserColor: createdBy?.accentColor || '#1976d2',
    updatedByUserId: row.updated_by_user_id || 0,
    updatedByUserName: updatedBy?.name || 'Unknown',
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
  if (!['game_plan', 'game_status'].includes(entry.kind)) {
    return entry;
  }
  entry.rawg_background_image = await cacheRawgImage(
    entry.rawg_background_image || '',
    [entry.rawg_game_id || 0, entry.rawg_slug || '', hint].filter(Boolean).join('-')
  );
  return entry;
}

function isKnownKind(kind) {
  return ['date', 'reminder', 'countdown', 'memory', 'got_together', 'game_plan', 'game_status'].includes(kind);
}

function normalizeColor(value, fallback = '#1976d2') {
  return typeof value === 'string' && /^#[0-9a-f]{6}$/i.test(value) ? value : fallback;
}

function normalizeTheme(value, fallback = 'light') {
  return value === 'dark' ? 'dark' : value === 'light' ? 'light' : fallback;
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

function readSharedSettings() {
  return {
    coupleName: getSetting('couple_name', ''),
    relationshipStartedAt: getSetting('relationship_started_at', '')
  };
}

function updateSharedSettingsFromBody(body) {
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
}

function getUserSetting(userId, key, fallback = '') {
  const row = db.prepare('SELECT value FROM user_settings WHERE user_id = ? AND key = ?').get(userId, key);
  return row?.value ?? fallback;
}

function setUserSetting(userId, key, value) {
  db.prepare(`
    INSERT INTO user_settings (user_id, key, value, updated_at)
    VALUES (?, ?, ?, datetime('now'))
    ON CONFLICT(user_id, key) DO UPDATE SET
      value = excluded.value,
      updated_at = datetime('now')
  `).run(userId, key, String(value ?? ''));
}

function readUserProfile(row) {
  const reminderMinutes = Number(getUserSetting(row.id, 'default_reminder_minutes', '60'));
  return {
    id: row.id,
    name: row.name,
    accentColor: normalizeColor(getUserSetting(row.id, 'accent_color', '#1976d2')),
    preferredTheme: normalizeTheme(getUserSetting(row.id, 'preferred_theme', 'light')),
    defaultReminderMinutes: Number.isFinite(reminderMinutes) ? reminderMinutes : 60,
    createdAt: row.created_at,
    updatedAt: row.updated_at
  };
}

function readUsers() {
  return db.prepare(`
    SELECT id, name, created_at, updated_at
    FROM users
    ORDER BY id ASC
  `).all().map(readUserProfile);
}

function readUserById(userId) {
  const row = db.prepare(`
    SELECT id, name, created_at, updated_at
    FROM users
    WHERE id = ?
  `).get(userId);
  return row ? readUserProfile(row) : null;
}

function ensureUserExists(userId) {
  return readUserById(userId) ?? readUsers()[0] ?? null;
}

function resolveUserId(value) {
  const requested = Number(value);
  const users = readUsers();
  if (Number.isInteger(requested) && requested > 0 && users.some((user) => user.id === requested)) {
    return requested;
  }
  return users[0]?.id ?? 0;
}

function updateUserSettingsFromBody(userId, body) {
  if (body.name !== undefined) {
    const name = typeof body.name === 'string' ? body.name.trim() : '';
    if (!name) {
      const error = new Error('name is required');
      error.statusCode = 400;
      throw error;
    }
    db.prepare('UPDATE users SET name = ?, updated_at = datetime(\'now\') WHERE id = ?').run(name, userId);
  }

  if (body.accentColor !== undefined) {
    setUserSetting(userId, 'accent_color', normalizeColor(body.accentColor));
  }

  if (body.preferredTheme !== undefined) {
    const theme = normalizeTheme(body.preferredTheme, '');
    if (!theme) {
      const error = new Error('preferredTheme must be light or dark');
      error.statusCode = 400;
      throw error;
    }
    setUserSetting(userId, 'preferred_theme', theme);
  }

  if (body.defaultReminderMinutes !== undefined) {
    const minutes = Number(body.defaultReminderMinutes);
    if (!Number.isInteger(minutes) || minutes < 0 || minutes > 1440) {
      const error = new Error('defaultReminderMinutes must be an integer between 0 and 1440');
      error.statusCode = 400;
      throw error;
    }
    setUserSetting(userId, 'default_reminder_minutes', String(minutes));
  }
}

function createUserFromBody(body) {
  const name = typeof body.name === 'string' ? body.name.trim() : '';
  if (!name) {
    const error = new Error('name is required');
    error.statusCode = 400;
    throw error;
  }

  const accentColor = normalizeColor(body.accentColor);
  const preferredTheme = normalizeTheme(body.preferredTheme);
  let defaultReminderMinutes = 60;
  if (body.defaultReminderMinutes !== undefined) {
    defaultReminderMinutes = Number(body.defaultReminderMinutes);
    if (!Number.isInteger(defaultReminderMinutes) || defaultReminderMinutes < 0 || defaultReminderMinutes > 1440) {
      const error = new Error('defaultReminderMinutes must be an integer between 0 and 1440');
      error.statusCode = 400;
      throw error;
    }
  }

  const info = db.prepare('INSERT INTO users (name) VALUES (?)').run(name);
  const userId = info.lastInsertRowid;
  setUserSetting(userId, 'accent_color', accentColor);
  setUserSetting(userId, 'preferred_theme', preferredTheme);
  setUserSetting(userId, 'default_reminder_minutes', String(defaultReminderMinutes));
  return readUserById(userId);
}

function getEntryUserStatusMap(userId) {
  if (!userId) {
    return new Map();
  }
  return new Map(
    db.prepare(`
      SELECT entry_id, value
      FROM entry_user_settings
      WHERE user_id = ? AND key = 'game_status'
    `).all(userId).map((row) => [row.entry_id, row.value])
  );
}

function getEntryUserRatingMap(userId) {
  if (!userId) {
    return new Map();
  }
  return new Map(
    db.prepare(`
      SELECT entry_id, value
      FROM entry_user_settings
      WHERE user_id = ? AND key = 'game_rating'
    `).all(userId).map((row) => [row.entry_id, row.value])
  );
}

function getEntryUserSetting(entryId, userId, key, fallback = '') {
  const row = db.prepare(`
    SELECT value
    FROM entry_user_settings
    WHERE entry_id = ? AND user_id = ? AND key = ?
  `).get(entryId, userId, key);
  return row?.value ?? fallback;
}

function setEntryUserSetting(entryId, userId, key, value) {
  db.prepare(`
    INSERT INTO entry_user_settings (entry_id, user_id, key, value, updated_at)
    VALUES (?, ?, ?, ?, datetime('now'))
    ON CONFLICT(entry_id, user_id, key) DO UPDATE SET
      value = excluded.value,
      updated_at = datetime('now')
  `).run(entryId, userId, key, String(value ?? ''));
}

function deleteEntryUserSettings(entryId, key) {
  db.prepare(`
    DELETE FROM entry_user_settings
    WHERE entry_id = ? AND key = ?
  `).run(entryId, key);
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

function getDefaultReminderIso(eventDateIso, minutes = 60) {
  if (!Number.isFinite(minutes) || minutes <= 0) {
    return '';
  }
  const date = Date.parse(eventDateIso);
  if (Number.isNaN(date)) {
    return '';
  }
  return new Date(date - minutes * 60_000).toISOString();
}

function getUserReminderMinutes(userId) {
  const user = readUserById(userId);
  return user?.defaultReminderMinutes ?? 60;
}

function buildEntryContext(userId) {
  return {
    userId,
    users: new Map(readUsers().map((user) => [user.id, user])),
    userStatuses: getEntryUserStatusMap(userId),
    userRatings: getEntryUserRatingMap(userId)
  };
}

const entrySelectColumns = `
  id,
  kind,
  title,
  details,
  event_date,
  reminder_at,
  game_status,
  rawg_game_id,
  rawg_slug,
  rawg_background_image,
  rawg_platforms,
  rawg_metacritic,
  rawg_released,
  created_by_user_id,
  updated_by_user_id,
  created_at,
  updated_at
`;

function getEntriesForUser(userId) {
  const context = buildEntryContext(userId);
  return db.prepare(`
    SELECT ${entrySelectColumns}
    FROM entries
    ORDER BY datetime(event_date) ASC, id DESC
  `).all().map((row) => normalizeEntry(row, context));
}

function getEntryForUser(entryId, userId) {
  const context = buildEntryContext(userId);
  const row = db.prepare(`
    SELECT ${entrySelectColumns}
    FROM entries
    WHERE id = ?
  `).get(entryId);
  return row ? normalizeEntry(row, context) : null;
}

function getEntryRow(entryId) {
  return db.prepare(`
    SELECT ${entrySelectColumns}
    FROM entries
    WHERE id = ?
  `).get(entryId);
}

function validateEntryPayload(entry) {
  if (!isKnownKind(entry.kind)) {
    const error = new Error('Invalid kind');
    error.statusCode = 400;
    throw error;
  }
  if (!entry.title) {
    const error = new Error('Title is required');
    error.statusCode = 400;
    throw error;
  }
  if (!isValidIso(entry.event_date)) {
    const error = new Error('eventDate must be an ISO date string');
    error.statusCode = 400;
    throw error;
  }
  if (entry.reminder_at && !isValidIso(entry.reminder_at)) {
    const error = new Error('reminderAt must be an ISO date string');
    error.statusCode = 400;
    throw error;
  }
  if (['game_plan', 'game_status'].includes(entry.kind) && !['want_to_play', 'playing', 'played', 'finished'].includes(entry.game_status)) {
    const error = new Error('gameStatus must be want_to_play, playing, played, or finished');
    error.statusCode = 400;
    throw error;
  }
  if (entry.game_rating && !['1', '2', '3', '4', '5'].includes(entry.game_rating)) {
    const error = new Error('gameRating must be between 1 and 5');
    error.statusCode = 400;
    throw error;
  }
}

async function saveEntryPayload({ body, existingRow = null, entryId = null, userId }) {
  const editorId = resolveUserId(userId);
  const creatorId = existingRow?.created_by_user_id || editorId;
  const updaterId = editorId || creatorId;
  const entry = {
    kind: body.kind ?? existingRow?.kind,
    title: typeof body.title === 'string' ? body.title.trim() : existingRow?.title || '',
    details: typeof body.details === 'string' ? body.details.trim() : existingRow?.details || '',
    event_date: body.eventDate ?? existingRow?.event_date ?? new Date().toISOString(),
    reminder_at: body.reminderAt ?? existingRow?.reminder_at ?? '',
    game_status: typeof body.gameStatus === 'string'
      ? body.gameStatus
      : existingRow?.game_status || 'want_to_play',
    game_rating: typeof body.gameRating === 'string'
      ? body.gameRating
      : existingRow ? getEntryUserSetting(existingRow.id, editorId, 'game_rating', '') : '',
    rawg_game_id: body.rawgGameId ?? existingRow?.rawg_game_id ?? 0,
    rawg_slug: typeof body.rawgSlug === 'string' ? body.rawgSlug : existingRow?.rawg_slug || '',
    rawg_background_image: typeof body.rawgBackgroundImage === 'string'
      ? body.rawgBackgroundImage
      : existingRow?.rawg_background_image || '',
    rawg_platforms: JSON.stringify(body.rawgPlatforms ?? parsePlatforms(existingRow?.rawg_platforms || '')),
    rawg_metacritic: body.rawgMetacritic ?? existingRow?.rawg_metacritic ?? 0,
    rawg_released: typeof body.rawgReleased === 'string' ? body.rawgReleased : existingRow?.rawg_released || '',
    created_by_user_id: creatorId,
    updated_by_user_id: updaterId
  };

  validateEntryPayload(entry);
  const dbEntry = { ...entry };
  delete dbEntry.game_rating;

  if (!['game_plan', 'game_status'].includes(entry.kind)) {
    entry.game_status = 'want_to_play';
  }

  await prepareRawgArtwork(entry, `${entry.kind}-${entry.title}-${Date.now()}`);

  if (!entry.reminder_at && !['memory', 'game_status'].includes(entry.kind)) {
    entry.reminder_at = getDefaultReminderIso(entry.event_date, getUserReminderMinutes(editorId));
  }

  if (entryId) {
    db.prepare(`
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
          created_by_user_id = @created_by_user_id,
          updated_by_user_id = @updated_by_user_id,
          updated_at = datetime('now')
      WHERE id = @id
    `).run({ ...dbEntry, id: entryId });
  } else {
    const info = db.prepare(`
      INSERT INTO entries (
        kind,
        title,
        details,
        event_date,
        reminder_at,
        game_status,
        rawg_game_id,
        rawg_slug,
        rawg_background_image,
        rawg_platforms,
        rawg_metacritic,
        rawg_released,
        created_by_user_id,
        updated_by_user_id,
        updated_at
      )
      VALUES (
        @kind,
        @title,
        @details,
        @event_date,
        @reminder_at,
        @game_status,
        @rawg_game_id,
        @rawg_slug,
        @rawg_background_image,
        @rawg_platforms,
        @rawg_metacritic,
        @rawg_released,
        @created_by_user_id,
        @updated_by_user_id,
        datetime('now')
      )
    `).run(dbEntry);
    entryId = info.lastInsertRowid;
  }

  if (['game_plan', 'game_status'].includes(entry.kind)) {
    setEntryUserSetting(entryId, editorId, 'game_status', entry.game_status);
    if (entry.game_rating) {
      setEntryUserSetting(entryId, editorId, 'game_rating', entry.game_rating);
    } else {
      deleteEntryUserSettings(entryId, 'game_rating');
    }
  } else {
    deleteEntryUserSettings(entryId, 'game_status');
    deleteEntryUserSettings(entryId, 'game_rating');
  }

  return getEntryForUser(entryId, editorId);
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
          sendJson(res, 200, { settings: readSharedSettings() });
          return;
        }

        if (req.method === 'PUT' && url.pathname === '/api/settings') {
          const body = await readBody(req);
          updateSharedSettingsFromBody(body);
          sendJson(res, 200, { settings: readSharedSettings() });
          return;
        }

        if (req.method === 'GET' && url.pathname === '/api/users') {
          sendJson(res, 200, { users: readUsers() });
          return;
        }

        if (req.method === 'GET' && /^\/api\/users\/\d+$/.test(url.pathname)) {
          const id = Number(url.pathname.split('/').pop());
          const user = readUserById(id);
          if (!user) {
            sendJson(res, 404, { error: 'Not found' });
            return;
          }
          sendJson(res, 200, { user });
          return;
        }

        if (req.method === 'POST' && url.pathname === '/api/users') {
          const body = await readBody(req);
          const user = createUserFromBody(body);
          sendJson(res, 201, { user });
          return;
        }

        if (req.method === 'PUT' && /^\/api\/users\/\d+$/.test(url.pathname)) {
          const id = Number(url.pathname.split('/').pop());
          const user = readUserById(id);
          if (!user) {
            sendJson(res, 404, { error: 'Not found' });
            return;
          }
          const body = await readBody(req);
          updateUserSettingsFromBody(id, body);
          sendJson(res, 200, { user: readUserById(id) });
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
          const userId = resolveUserId(url.searchParams.get('userId'));
          sendJson(res, 200, {
            entries: getEntriesForUser(userId),
            activeUserId: userId
          });
          return;
        }

        if (req.method === 'GET' && /^\/api\/entries\/\d+$/.test(url.pathname)) {
          const id = Number(url.pathname.split('/').pop());
          const userId = resolveUserId(url.searchParams.get('userId'));
          const entry = getEntryForUser(id, userId);
          if (!entry) {
            sendJson(res, 404, { error: 'Not found' });
            return;
          }
          sendJson(res, 200, { entry, activeUserId: userId });
          return;
        }

        if (req.method === 'POST' && url.pathname === '/api/entries') {
          const body = await readBody(req);
          const userId = resolveUserId(body.userId);
          const entry = await saveEntryPayload({ body, userId });
          sendJson(res, 201, { entry, activeUserId: userId });
          return;
        }

        if (req.method === 'PUT' && /^\/api\/entries\/\d+$/.test(url.pathname)) {
          const id = Number(url.pathname.split('/').pop());
          const existing = getEntryRow(id);
          if (!existing) {
            sendJson(res, 404, { error: 'Not found' });
            return;
          }
          const body = await readBody(req);
          const userId = resolveUserId(body.userId);
          const entry = await saveEntryPayload({ body, existingRow: existing, entryId: id, userId });
          sendJson(res, 200, { entry, activeUserId: userId });
          return;
        }

        if (req.method === 'DELETE' && /^\/api\/entries\/\d+$/.test(url.pathname)) {
          const id = Number(url.pathname.split('/').pop());
          db.prepare('DELETE FROM entries WHERE id = ?').run(id);
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
