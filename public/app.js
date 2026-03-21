const entriesEl = document.querySelector('#entries');
const template = document.querySelector('#entry-template');
const form = document.querySelector('#entry-form');
const statusEl = document.querySelector('#form-status');
const totalCountEl = document.querySelector('#total-count');
const nextCountdownEl = document.querySelector('#next-countdown');
const refreshBtn = document.querySelector('#refresh');
const resetBtn = document.querySelector('#reset-form');
const calendarGridEl = document.querySelector('#calendar-grid');
const calendarTitleEl = document.querySelector('#calendar-title');
const calendarSelectedDateEl = document.querySelector('#calendar-selected-date');
const calendarSummaryEl = document.querySelector('#calendar-summary');
const calendarAgendaEl = document.querySelector('#calendar-agenda');
const calendarPrevBtn = document.querySelector('#calendar-prev');
const calendarNextBtn = document.querySelector('#calendar-next');
const calendarTodayBtn = document.querySelector('#calendar-today');
const useSelectedDateBtn = document.querySelector('#use-selected-date');
const gameQueryEl = document.querySelector('#game-query');
const gameSearchBtn = document.querySelector('#game-search-btn');
const gameSearchStatusEl = document.querySelector('#game-search-status');
const gameResultsEl = document.querySelector('#game-results');
const gameSelectedCoverEl = document.querySelector('#game-selected-cover');
const gameSelectedTitleEl = document.querySelector('#game-selected-title');
const gameSelectedMetaEl = document.querySelector('#game-selected-meta');
const gameSelectedSummaryEl = document.querySelector('#game-selected-summary');
const useGameBtn = document.querySelector('#use-game-btn');
const clearGameBtn = document.querySelector('#clear-game-btn');
const gameStatusEl = document.querySelector('#gameStatus');
const gamePlanOnlyFields = document.querySelectorAll('.game-plan-only');
const gamesWantToPlayEl = document.querySelector('#games-want-to-play');
const gamesPlayingEl = document.querySelector('#games-playing');
const gamesPlayedEl = document.querySelector('#games-played');
const gamesFinishedEl = document.querySelector('#games-finished');
const gameStatusBannerEls = document.querySelectorAll('[data-game-status-banner]');
const themeToggleBtn = document.querySelector('#theme-toggle');
const plannerTabBtn = document.querySelector('#planner-tab');
const settingsTabBtn = document.querySelector('#settings-tab');
const plannerViewEl = document.querySelector('#planner-view');
const settingsViewEl = document.querySelector('#settings-view');
const settingsForm = document.querySelector('#settings-form');
const settingsResetBtn = document.querySelector('#settings-reset');
const settingsStatusEl = document.querySelector('#settings-status');
const coupleNameEl = document.querySelector('#coupleName');
const relationshipStartedAtEl = document.querySelector('#relationshipStartedAt');
const defaultReminderMinutesEl = document.querySelector('#defaultReminderMinutes');
const preferredThemeEl = document.querySelector('#preferredTheme');
const relationshipSummaryEl = document.querySelector('#relationship-summary');
const relationshipDetailsEl = document.querySelector('#relationship-details');
const relationshipCountEl = document.querySelector('#relationship-count');

const fields = {
  kind: document.querySelector('#kind'),
  title: document.querySelector('#title'),
  details: document.querySelector('#details'),
  eventDate: document.querySelector('#eventDate'),
  reminderAt: document.querySelector('#reminderAt')
};

let editingId = null;
let entries = [];
let calendarCursor = startOfMonth(new Date());
let selectedDate = startOfDay(new Date());
let selectedGame = null;
let gameResults = [];
let settings = {
  coupleName: '',
  relationshipStartedAt: '',
  defaultReminderMinutes: 60,
  preferredTheme: 'light'
};

function getPreferredTheme() {
  const stored = localStorage.getItem('relationship-planner-theme');
  if (stored === 'light' || stored === 'dark') {
    return stored;
  }
  return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
}

function applyTheme(theme) {
  document.body.dataset.theme = theme;
  themeToggleBtn.textContent = theme === 'dark' ? 'Light mode' : 'Dark mode';
  themeToggleBtn.setAttribute('aria-pressed', String(theme === 'dark'));
  localStorage.setItem('relationship-planner-theme', theme);
}

function toggleTheme() {
  applyTheme(document.body.dataset.theme === 'dark' ? 'light' : 'dark');
}

function formatRelationshipDuration(value) {
  if (!value) return '--';
  return timeSince(value);
}

function computeNextAnniversary(value) {
  if (!value) return '--';
  const start = new Date(value);
  if (Number.isNaN(start.getTime())) return '--';
  const now = new Date();
  let next = new Date(now.getFullYear(), start.getMonth(), start.getDate());
  if (next < now) {
    next = new Date(now.getFullYear() + 1, start.getMonth(), start.getDate());
  }
  return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium' }).format(next);
}

function syncSettingsForm() {
  coupleNameEl.value = settings.coupleName || '';
  relationshipStartedAtEl.value = toDatetimeLocal(settings.relationshipStartedAt);
  defaultReminderMinutesEl.value = String(settings.defaultReminderMinutes ?? 60);
  preferredThemeEl.value = settings.preferredTheme || 'light';
}

function updateRelationshipSummary() {
  if (!settings.relationshipStartedAt) {
    relationshipSummaryEl.textContent = 'Set your start date';
    relationshipDetailsEl.textContent = 'This helps the app show how long you’ve been together.';
    relationshipCountEl.textContent = '--';
    return;
  }

  relationshipSummaryEl.textContent = settings.coupleName
    ? `${settings.coupleName} started`
    : 'Relationship started';
  relationshipDetailsEl.textContent = `Since ${toFriendlyDate(settings.relationshipStartedAt)}. Next anniversary: ${computeNextAnniversary(settings.relationshipStartedAt)}.`;
  relationshipCountEl.textContent = formatRelationshipDuration(settings.relationshipStartedAt);
}

function setView(view) {
  const isPlanner = view !== 'settings';
  plannerViewEl.hidden = !isPlanner;
  settingsViewEl.hidden = isPlanner;
  plannerTabBtn.setAttribute('aria-pressed', String(isPlanner));
  settingsTabBtn.setAttribute('aria-pressed', String(!isPlanner));
  if (isPlanner) {
    plannerTabBtn.setAttribute('aria-current', 'page');
    settingsTabBtn.removeAttribute('aria-current');
  } else {
    settingsTabBtn.setAttribute('aria-current', 'page');
    plannerTabBtn.removeAttribute('aria-current');
  }
  location.hash = isPlanner ? '#planner' : '#settings';
  window.scrollTo({ top: 0, behavior: 'auto' });
}

function resolveInitialView() {
  return location.hash === '#settings' ? 'settings' : 'planner';
}

function startOfDay(date) {
  const value = new Date(date);
  value.setHours(0, 0, 0, 0);
  return value;
}

function startOfMonth(date) {
  const value = startOfDay(date);
  value.setDate(1);
  return value;
}

function addMonths(date, delta) {
  const value = new Date(date);
  value.setMonth(value.getMonth() + delta);
  return startOfMonth(value);
}

function toLocalDateKey(date) {
  const value = startOfDay(date);
  const year = value.getFullYear();
  const month = String(value.getMonth() + 1).padStart(2, '0');
  const day = String(value.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

function formatCalendarHeader(date) {
  return new Intl.DateTimeFormat(undefined, {
    month: 'long',
    year: 'numeric'
  }).format(date);
}

function formatDayNumber(date) {
  return new Intl.DateTimeFormat(undefined, {
    day: 'numeric'
  }).format(date);
}

function formatCalendarDate(date) {
  return new Intl.DateTimeFormat(undefined, {
    weekday: 'long',
    month: 'long',
    day: 'numeric',
    year: 'numeric'
  }).format(date);
}

function toDatetimeLocal(value) {
  if (!value) return '';
  const date = new Date(value);
  const offset = date.getTimezoneOffset();
  const local = new Date(date.getTime() - offset * 60_000);
  return local.toISOString().slice(0, 16);
}

function toFriendlyDate(value) {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short'
  }).format(new Date(value));
}

function timeUntil(value) {
  const diff = Date.parse(value) - Date.now();
  const abs = Math.abs(diff);
  const days = Math.floor(abs / 86_400_000);
  const hours = Math.floor((abs % 86_400_000) / 3_600_000);
  const minutes = Math.floor((abs % 3_600_000) / 60_000);
  if (diff >= 0) {
    if (days > 0) return `in ${days}d ${hours}h`;
    if (hours > 0) return `in ${hours}h ${minutes}m`;
    return `in ${minutes}m`;
  }
  if (days > 0) return `${days}d ago`;
  if (hours > 0) return `${hours}h ago`;
  return `${minutes}m ago`;
}

function timeSince(value) {
  const start = new Date(value);
  const now = new Date();
  if (Number.isNaN(start.getTime())) return '--';

  let years = now.getFullYear() - start.getFullYear();
  let months = now.getMonth() - start.getMonth();
  let days = now.getDate() - start.getDate();

  if (days < 0) {
    months -= 1;
    const previousMonth = new Date(now.getFullYear(), now.getMonth(), 0);
    days += previousMonth.getDate();
  }

  if (months < 0) {
    years -= 1;
    months += 12;
  }

  const parts = [];
  if (years > 0) parts.push(`${years}y`);
  if (months > 0) parts.push(`${months}m`);
  if (days > 0 || parts.length === 0) parts.push(`${days}d`);
  return parts.join(' ');
}

function friendlyKind(kind) {
  return {
    date: 'Date idea',
    reminder: 'Reminder',
    countdown: 'Countdown',
    memory: 'Memory',
    got_together: 'Got together',
    game_plan: 'Game plan'
  }[kind] || kind;
}

function gameStatusLabel(status) {
  return {
    want_to_play: 'Want to play',
    playing: 'Playing',
    played: 'Played',
    finished: 'Finished'
  }[status] || 'Want to play';
}

function formatGamePlatforms(platforms) {
  if (!Array.isArray(platforms) || platforms.length === 0) {
    return 'Platform info unavailable';
  }
  return platforms
    .map((platform) => platform?.platform?.name || platform?.platform?.slug || '')
    .filter(Boolean)
    .join(', ');
}

function gameSummary(game) {
  if (!game) return 'Search and pick a game to prefill the planner.';
  const parts = [];
  if (game.released) parts.push(`Released ${game.released}`);
  if (game.metacritic) parts.push(`Metacritic ${game.metacritic}`);
  if (game.playtime) parts.push(`${game.playtime}h average playtime`);
  parts.push(formatGamePlatforms(game.platforms));
  return parts.join(' · ');
}

function gamePlanDetails(game) {
  if (!game) return '';
  return [
    `RAWG game: ${game.name}`,
    game.released ? `Released: ${game.released}` : '',
    game.metacritic ? `Metacritic: ${game.metacritic}` : '',
    game.playtime ? `Avg playtime: ${game.playtime}h` : '',
    `Platforms: ${formatGamePlatforms(game.platforms)}`
  ].filter(Boolean).join('\n');
}

function setStatus(message, isError = false) {
  statusEl.textContent = message;
  statusEl.style.color = isError ? '#a03222' : '';
}

function setGameStatus(message, isError = false) {
  gameSearchStatusEl.textContent = message;
  gameSearchStatusEl.style.color = isError ? '#a03222' : '';
}

function syncFormDate(date = selectedDate) {
  fields.eventDate.value = toDatetimeLocal(date);
}

function clearSelectedGame() {
  selectedGame = null;
  gameSelectedTitleEl.textContent = 'Nothing selected yet';
  gameSelectedMetaEl.textContent = 'Search and pick a game to prefill the planner.';
  gameSelectedSummaryEl.textContent = '';
  gameSelectedCoverEl.removeAttribute('src');
  gameSelectedCoverEl.alt = '';
  useGameBtn.disabled = true;
}

function setSelectedGame(game) {
  selectedGame = game;
  gameSelectedTitleEl.textContent = game.name;
  gameSelectedMetaEl.textContent = game.released ? `Released ${game.released}` : 'Release date unavailable';
  gameSelectedSummaryEl.textContent = gameSummary(game);
  gameSelectedCoverEl.src = game.backgroundImage || '';
  gameSelectedCoverEl.alt = game.name;
  useGameBtn.disabled = false;
}

function syncGamePlanControls() {
  const isGamePlan = fields.kind.value === 'game_plan';
  for (const field of gamePlanOnlyFields) {
    field.hidden = !isGamePlan;
  }
  if (isGamePlan && !gameStatusEl.value) {
    gameStatusEl.value = 'want_to_play';
  }
}

function prefillGamePlan(game) {
  setSelectedGame(game);
  fields.kind.value = 'game_plan';
  fields.title.value = game.name;
  fields.details.value = gamePlanDetails(game);
  syncFormDate();
  fields.reminderAt.value = '';
  gameStatusEl.value = 'want_to_play';
  syncGamePlanControls();
  setStatus(`Loaded "${game.name}" into the planner.`);
}

function resetForm() {
  editingId = null;
  form.reset();
  fields.kind.value = 'date';
  syncFormDate(new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString());
  fields.reminderAt.value = '';
  clearSelectedGame();
  setStatus('Ready to add a new item.');
}

function renderSummary() {
  totalCountEl.textContent = String(entries.length);
  const next = entries.find((entry) => Date.parse(entry.eventDate) >= Date.now()) || entries[0];
  nextCountdownEl.textContent = next ? timeUntil(next.eventDate) : '--';
}

function renderGameStatusWindow() {
  const gamePlans = entries.filter((entry) => entry.kind === 'game_plan');
  const counts = gamePlans.reduce((acc, entry) => {
    const status = entry.gameStatus || 'want_to_play';
    acc[status] = (acc[status] || 0) + 1;
    return acc;
  }, {
    want_to_play: 0,
    playing: 0,
    played: 0,
    finished: 0
  });

  gamesWantToPlayEl.textContent = String(counts.want_to_play);
  gamesPlayingEl.textContent = String(counts.playing);
  gamesPlayedEl.textContent = String(counts.played);
  gamesFinishedEl.textContent = String(counts.finished);

  for (const banner of gameStatusBannerEls) {
    const status = banner.dataset.gameStatusBanner;
    const preview = gamePlans.find((entry) => (entry.gameStatus || 'want_to_play') === status && entry.rawgBackgroundImage);
    if (preview?.rawgBackgroundImage) {
      banner.src = preview.rawgBackgroundImage;
      banner.alt = preview.title;
      banner.hidden = false;
    } else {
      banner.removeAttribute('src');
      banner.alt = '';
      banner.hidden = true;
    }
  }
}

function entriesForDate(date) {
  const key = toLocalDateKey(date);
  return entries.filter((entry) => toLocalDateKey(entry.eventDate) === key);
}

function renderCalendar() {
  const monthStart = new Date(calendarCursor);
  const monthEnd = new Date(calendarCursor);
  monthEnd.setMonth(monthEnd.getMonth() + 1);
  const gridStart = new Date(monthStart);
  gridStart.setDate(gridStart.getDate() - gridStart.getDay());
  const gridEnd = new Date(monthEnd);
  gridEnd.setDate(gridEnd.getDate() + (6 - gridEnd.getDay()));

  calendarTitleEl.textContent = formatCalendarHeader(monthStart);
  calendarGridEl.innerHTML = '';

  const todayKey = toLocalDateKey(new Date());
  const selectedKey = toLocalDateKey(selectedDate);
  const current = new Date(gridStart);

  while (current <= gridEnd) {
    const dayDate = new Date(current);
    const currentKey = toLocalDateKey(dayDate);
    const dayEntries = entriesForDate(dayDate);
    const button = document.createElement('button');
    button.type = 'button';
    button.className = 'calendar-day';
    button.setAttribute('role', 'gridcell');
    button.setAttribute('aria-label', formatCalendarDate(dayDate));
    button.innerHTML = `
      <span class="calendar-day-number">${formatDayNumber(dayDate)}</span>
      <span class="calendar-day-badges"></span>
    `;

    if (dayDate.getMonth() !== monthStart.getMonth()) {
      button.classList.add('is-outside');
    }
    if (currentKey === todayKey) {
      button.classList.add('is-today');
    }
    if (currentKey === selectedKey) {
      button.classList.add('is-selected');
    }

    const badgeWrap = button.querySelector('.calendar-day-badges');
    const counts = dayEntries.reduce((acc, entry) => {
      acc[entry.kind] = (acc[entry.kind] || 0) + 1;
      return acc;
    }, {});
    for (const [kind, count] of Object.entries(counts)) {
      const badge = document.createElement('span');
      badge.className = `calendar-badge ${kind}`;
      badge.textContent = count > 1 ? `${friendlyKind(kind)} x${count}` : friendlyKind(kind);
      badgeWrap.appendChild(badge);
    }

    button.addEventListener('click', () => {
      selectedDate = new Date(dayDate);
      renderCalendar();
      renderAgenda();
      syncFormDate(selectedDate);
      setStatus(`Selected ${formatCalendarDate(selectedDate)}.`);
    });

    calendarGridEl.appendChild(button);
    current.setDate(current.getDate() + 1);
  }

  const selectedEntries = entriesForDate(selectedDate);
  calendarSelectedDateEl.textContent = formatCalendarDate(selectedDate);
  calendarSummaryEl.textContent =
    selectedEntries.length === 0
      ? 'No saved items on this day yet.'
      : `${selectedEntries.length} item${selectedEntries.length === 1 ? '' : 's'} on this day.`;
}

function renderAgenda() {
  const selectedEntries = entriesForDate(selectedDate);
  calendarAgendaEl.innerHTML = '';

  if (selectedEntries.length === 0) {
    const empty = document.createElement('p');
    empty.className = 'status';
    empty.textContent = 'Nothing scheduled for the selected day.';
    calendarAgendaEl.appendChild(empty);
    return;
  }

  for (const entry of selectedEntries) {
    const item = document.createElement('article');
    item.className = 'agenda-item';
    const meta =
      entry.kind === 'got_together'
        ? `Together for ${timeSince(entry.eventDate)} - since ${toFriendlyDate(entry.eventDate)}`
        : entry.kind === 'game_plan'
          ? `Planned game session - ${toFriendlyDate(entry.eventDate)}`
          : `${toFriendlyDate(entry.eventDate)} - ${timeUntil(entry.eventDate)}`;
    item.innerHTML = `
      <p class="tag">${friendlyKind(entry.kind)}</p>
      <h3>${entry.title}</h3>
      <p class="agenda-meta">${meta}</p>
    `;
    calendarAgendaEl.appendChild(item);
  }
}

function renderGameResults() {
  gameResultsEl.innerHTML = '';

  if (gameResults.length === 0) {
    gameResultsEl.innerHTML = '<p class="status compact">No game results yet.</p>';
    return;
  }

  for (const game of gameResults) {
    const card = document.createElement('article');
    card.className = 'game-result';
    card.innerHTML = `
      <img class="game-result-cover" src="${game.backgroundImage || ''}" alt="${game.name}" />
      <div class="game-result-copy">
        <p class="tag">RAWG</p>
        <h3>${game.name}</h3>
        <p class="game-meta">${gameSummary(game)}</p>
      </div>
      <button class="secondary" type="button">Use</button>
    `;

    const button = card.querySelector('button');
    button.addEventListener('click', () => {
      prefillGamePlan(game);
      setGameStatus(`Selected "${game.name}" for planning.`);
    });

    gameResultsEl.appendChild(card);
  }
}

function renderEntries() {
  entriesEl.innerHTML = '';

  if (entries.length === 0) {
    entriesEl.innerHTML = '<p class="status">No saved items yet.</p>';
    renderSummary();
    renderGameStatusWindow();
    renderCalendar();
    renderAgenda();
    return;
  }

  for (const entry of entries) {
    const node = template.content.firstElementChild.cloneNode(true);
    node.querySelector('.tag').textContent = friendlyKind(entry.kind);
    node.querySelector('h3').textContent = entry.title;
    node.querySelector('.entry-meta').textContent =
      entry.kind === 'got_together'
        ? `Together for ${timeSince(entry.eventDate)} - since ${toFriendlyDate(entry.eventDate)}`
        : entry.kind === 'game_plan'
          ? `${toFriendlyDate(entry.eventDate)} - ${gameStatusLabel(entry.gameStatus)}${entry.rawgMetacritic ? ` - Metacritic ${entry.rawgMetacritic}` : ''}`
          : `${timeUntil(entry.eventDate)} - ${toFriendlyDate(entry.eventDate)}`;

    const detailsParts = [entry.details];
    if (entry.kind === 'game_plan') {
      detailsParts.push(
        [
          entry.rawgReleased ? `Released: ${entry.rawgReleased}` : '',
          entry.rawgMetacritic ? `Metacritic: ${entry.rawgMetacritic}` : '',
          entry.rawgPlatforms.length ? `Platforms: ${formatGamePlatforms(entry.rawgPlatforms)}` : ''
        ].filter(Boolean).join('\n')
      );
    }
    if (entry.reminderAt) {
      detailsParts.push(`Reminder: ${toFriendlyDate(entry.reminderAt)}`);
    }
    node.querySelector('.details').textContent = detailsParts.filter(Boolean).join('\n');

    if (entry.kind === 'game_plan') {
      const banner = document.createElement('div');
      banner.className = 'entry-banner';
      if (entry.rawgBackgroundImage) {
        const image = document.createElement('img');
        image.className = 'entry-banner-image';
        image.src = entry.rawgBackgroundImage;
        image.alt = entry.title;
        image.loading = 'lazy';
        banner.appendChild(image);
      }
      const label = document.createElement('span');
      label.textContent = entry.title;
      banner.appendChild(label);
      node.prepend(banner);
    }

    node.querySelector('.edit-btn').addEventListener('click', () => {
      editingId = entry.id;
      fields.kind.value = entry.kind;
      fields.title.value = entry.title;
      fields.details.value = entry.details;
      fields.eventDate.value = toDatetimeLocal(entry.eventDate);
      fields.reminderAt.value = entry.reminderAt ? toDatetimeLocal(entry.reminderAt) : '';
      if (entry.kind === 'game_plan') {
        selectedGame = {
          id: entry.rawgGameId,
          name: entry.title,
          slug: entry.rawgSlug,
          released: entry.rawgReleased,
          metacritic: entry.rawgMetacritic,
          backgroundImage: entry.rawgBackgroundImage,
          platforms: entry.rawgPlatforms || []
        };
        setSelectedGame(selectedGame);
        fields.details.value = entry.details;
        gameStatusEl.value = entry.gameStatus || 'want_to_play';
        syncGamePlanControls();
      } else {
        clearSelectedGame();
        syncGamePlanControls();
      }
      setStatus(`Editing "${entry.title}".`);
      window.scrollTo({ top: 0, behavior: 'smooth' });
    });

    node.querySelector('.delete-btn').addEventListener('click', async () => {
      if (!confirm(`Delete "${entry.title}"?`)) return;
      await fetch(`/api/entries/${entry.id}`, { method: 'DELETE' });
      await loadEntries();
      setStatus('Item deleted.');
    });

    entriesEl.appendChild(node);
  }

  renderSummary();
  renderGameStatusWindow();
  renderCalendar();
  renderAgenda();
}

async function loadEntries() {
  const response = await fetch('/api/entries');
  const data = await response.json();
  entries = data.entries || [];
  renderEntries();
}

async function loadSettings() {
  const response = await fetch('/api/settings');
  const data = await response.json();
  settings = {
    coupleName: data.settings?.coupleName || '',
    relationshipStartedAt: data.settings?.relationshipStartedAt || '',
    defaultReminderMinutes: Number(data.settings?.defaultReminderMinutes || 60) || 60,
    preferredTheme: data.settings?.preferredTheme || 'light'
  };
  const storedTheme = localStorage.getItem('relationship-planner-theme');
  if (storedTheme === 'light' || storedTheme === 'dark') {
    settings.preferredTheme = storedTheme;
  } else {
    applyTheme(settings.preferredTheme);
  }
  syncSettingsForm();
  updateRelationshipSummary();
}

async function saveSettings() {
  const payload = {
    coupleName: coupleNameEl.value.trim(),
    relationshipStartedAt: relationshipStartedAtEl.value ? new Date(relationshipStartedAtEl.value).toISOString() : '',
    defaultReminderMinutes: Number(defaultReminderMinutesEl.value || 0),
    preferredTheme: preferredThemeEl.value
  };

  const response = await fetch('/api/settings', {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload)
  });

  const data = await response.json();
  if (!response.ok) {
    settingsStatusEl.textContent = data.error || 'Could not save settings.';
    settingsStatusEl.style.color = '#a03222';
    return;
  }

  settings = {
    coupleName: data.settings?.coupleName || '',
    relationshipStartedAt: data.settings?.relationshipStartedAt || '',
    defaultReminderMinutes: Number(data.settings?.defaultReminderMinutes || 60) || 60,
    preferredTheme: data.settings?.preferredTheme || 'light'
  };
  syncSettingsForm();
  updateRelationshipSummary();
  applyTheme(settings.preferredTheme);
  settingsStatusEl.textContent = 'Settings saved.';
  settingsStatusEl.style.color = '';
}

async function searchGames() {
  const query = gameQueryEl.value.trim();
  if (!query) {
    setGameStatus('Type a game name first.', true);
    return;
  }

  setGameStatus('Searching RAWG...');
  gameResultsEl.innerHTML = '<p class="status compact">Loading game results...</p>';

  const response = await fetch(`/api/rawg/search?q=${encodeURIComponent(query)}`);
  const data = await response.json();

  if (!response.ok) {
    gameResults = [];
    renderGameResults();
    setGameStatus(data.error || 'Could not search RAWG.', true);
    return;
  }

  gameResults = data.results || [];
  renderGameResults();
  setGameStatus(gameResults.length ? `Found ${gameResults.length} game${gameResults.length === 1 ? '' : 's'}.` : 'No matches found.');
}

form.addEventListener('submit', async (event) => {
  event.preventDefault();

  const eventDateValue = new Date(fields.eventDate.value).toISOString();
  let reminderAtValue = fields.reminderAt.value ? new Date(fields.reminderAt.value).toISOString() : '';
  if (!reminderAtValue && fields.kind.value !== 'memory' && settings.defaultReminderMinutes > 0) {
    const reminderDate = new Date(Date.parse(eventDateValue) - settings.defaultReminderMinutes * 60_000);
    reminderAtValue = reminderDate.toISOString();
  }

  const payload = {
    kind: fields.kind.value,
    title: fields.title.value.trim(),
    details: fields.details.value.trim(),
    eventDate: eventDateValue,
    reminderAt: reminderAtValue,
    rawgGameId: selectedGame?.id || 0,
    rawgSlug: selectedGame?.slug || '',
    rawgBackgroundImage: selectedGame?.backgroundImage || '',
    rawgPlatforms: selectedGame?.platforms || [],
    rawgMetacritic: selectedGame?.metacritic || 0,
    rawgReleased: selectedGame?.released || '',
    gameStatus: gameStatusEl.value || 'want_to_play'
  };

  if (payload.kind !== 'game_plan') {
    payload.rawgGameId = 0;
    payload.rawgSlug = '';
    payload.rawgBackgroundImage = '';
    payload.rawgPlatforms = [];
    payload.rawgMetacritic = 0;
    payload.rawgReleased = '';
    payload.gameStatus = 'want_to_play';
  }

  const method = editingId ? 'PUT' : 'POST';
  const url = editingId ? `/api/entries/${editingId}` : '/api/entries';

  const response = await fetch(url, {
    method,
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload)
  });

  const data = await response.json();
  if (!response.ok) {
    setStatus(data.error || 'Could not save item.', true);
    return;
  }

  setStatus(editingId ? 'Item updated.' : 'Item saved.');
  editingId = null;
  form.reset();
  fields.kind.value = 'date';
  syncFormDate(new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString());
  fields.reminderAt.value = '';
  gameStatusEl.value = 'want_to_play';
  clearSelectedGame();
  syncGamePlanControls();
  await loadEntries();
});

refreshBtn.addEventListener('click', loadEntries);
resetBtn.addEventListener('click', resetForm);
calendarPrevBtn.addEventListener('click', () => {
  calendarCursor = addMonths(calendarCursor, -1);
  renderCalendar();
});
calendarNextBtn.addEventListener('click', () => {
  calendarCursor = addMonths(calendarCursor, 1);
  renderCalendar();
});
calendarTodayBtn.addEventListener('click', () => {
  calendarCursor = startOfMonth(new Date());
  selectedDate = startOfDay(new Date());
  renderCalendar();
  renderAgenda();
  syncFormDate(selectedDate);
});
useSelectedDateBtn.addEventListener('click', () => {
  syncFormDate(selectedDate);
  fields.kind.focus();
  setStatus(`Form date set to ${formatCalendarDate(selectedDate)}.`);
});
gameSearchBtn.addEventListener('click', searchGames);
gameQueryEl.addEventListener('keydown', (event) => {
  if (event.key === 'Enter') {
    event.preventDefault();
    searchGames();
  }
});
useGameBtn.addEventListener('click', () => {
  if (!selectedGame) return;
  prefillGamePlan(selectedGame);
});
clearGameBtn.addEventListener('click', () => {
  clearSelectedGame();
  setGameStatus('Game selection cleared.');
});
themeToggleBtn.addEventListener('click', toggleTheme);
fields.kind.addEventListener('change', syncGamePlanControls);
plannerTabBtn.addEventListener('click', () => setView('planner'));
settingsTabBtn.addEventListener('click', () => setView('settings'));
settingsForm.addEventListener('submit', async (event) => {
  event.preventDefault();
  settingsStatusEl.textContent = 'Saving settings...';
  settingsStatusEl.style.color = '';
  try {
    await saveSettings();
  } catch (error) {
    settingsStatusEl.textContent = error instanceof Error ? error.message : 'Could not save settings.';
    settingsStatusEl.style.color = '#a03222';
  }
});
settingsResetBtn.addEventListener('click', () => {
  syncSettingsForm();
  settingsStatusEl.textContent = 'Settings reset to the last saved values.';
  settingsStatusEl.style.color = '';
});
window.addEventListener('hashchange', () => {
  setView(resolveInitialView());
});

fields.eventDate.value = toDatetimeLocal(new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString());
setStatus('Ready to add a new item.');
clearSelectedGame();
syncGamePlanControls();
applyTheme(getPreferredTheme());
setView(resolveInitialView());

loadSettings().catch(() => {
  settingsStatusEl.textContent = 'Could not load settings.';
  settingsStatusEl.style.color = '#a03222';
});

loadEntries().catch(() => {
  setStatus('Could not load entries.', true);
});

