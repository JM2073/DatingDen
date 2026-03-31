const entriesEl = document.querySelector('#entries');
const template = document.querySelector('#entry-template');
const entryDialogEl = document.querySelector('#entry-dialog');
const entryDialogCloseBtn = document.querySelector('#entry-dialog-close');
const openEntryDialogBtn = document.querySelector('#open-entry-dialog');
const quickAddEntryBtn = document.querySelector('#quick-add-entry');
const quickAddMemoBtn = document.querySelector('#quick-add-memo');
const form = document.querySelector('#entry-form');
const statusEl = document.querySelector('#form-status');
const totalCountEl = document.querySelector('#total-count');
const nextCountdownEl = document.querySelector('#next-countdown');
const relationshipCountHeroEl = document.querySelector('#relationship-count-hero');
const refreshBtn = document.querySelector('#refresh');
const resetBtn = document.querySelector('#reset-form');
const calendarGridEl = document.querySelector('#calendar-grid');
const calendarTitleEl = document.querySelector('#calendar-title');
const calendarSelectedDateEl = document.querySelector('#calendar-selected-date');
const calendarSummaryEl = document.querySelector('#calendar-summary');
const calendarAgendaEl = document.querySelector('#calendar-agenda');
const memoSelectedDateEl = document.querySelector('#memo-selected-date');
const memoDialogEl = document.querySelector('#memo-dialog');
const memoDialogCloseBtn = document.querySelector('#memo-dialog-close');
const memoFormEl = document.querySelector('#memo-form');
const memoTitleEl = document.querySelector('#memo-title');
const memoTextEl = document.querySelector('#memo-text');
const memoCanvasEl = document.querySelector('#memo-canvas');
const memoClearSketchBtn = document.querySelector('#memo-clear-sketch');
const memoResetBtn = document.querySelector('#memo-reset');
const memoStatusEl = document.querySelector('#memo-status');
const memoSaveBtn = document.querySelector('#memo-form button[type="submit"]');
const openMemoDialogBtn = document.querySelector('#open-memo-dialog');
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
const saveStatusBtn = document.querySelector('#save-status-btn');
const clearGameBtn = document.querySelector('#clear-game-btn');
const gameStatusEl = document.querySelector('#gameStatus');
const gamePlanOnlyFields = document.querySelectorAll('.game-plan-only');
const gamesWantToPlayEl = document.querySelector('#games-want-to-play');
const gamesPlayingEl = document.querySelector('#games-playing');
const gamesPlayedEl = document.querySelector('#games-played');
const gamesFinishedEl = document.querySelector('#games-finished');
const gameStatusBannerEls = document.querySelectorAll('[data-game-status-banner]');
const gameStatusSummaryEl = document.querySelector('.game-status-window .status.compact');
const gameStatusManagerEl = document.querySelector('#game-status-manager');
const gameRatingDialogEl = document.querySelector('#game-rating-dialog');
const gameRatingTitleEl = document.querySelector('#game-rating-title');
const gameRatingMetaEl = document.querySelector('#game-rating-meta');
const gameRatingOptionsEl = document.querySelector('#game-rating-options');
const gameRatingValueEl = document.querySelector('#game-rating-value');
const gameRatingSaveBtn = document.querySelector('#game-rating-save');
const gameRatingClearBtn = document.querySelector('#game-rating-clear');
const gameRatingCancelBtn = document.querySelector('#game-rating-cancel');
const gameRatingCloseBtn = document.querySelector('#game-rating-close');
const themeToggleBtn = document.querySelector('#theme-toggle');
const activeUserSelectEl = document.querySelector('#active-user');
const activeUserSwatchEl = document.querySelector('#active-user-swatch');
const plannerTabBtn = document.querySelector('#planner-tab');
const settingsTabBtn = document.querySelector('#settings-tab');
const plannerViewEl = document.querySelector('#planner-view');
const settingsViewEl = document.querySelector('#settings-view');
const settingsForm = document.querySelector('#settings-form');
const settingsResetBtn = document.querySelector('#settings-reset');
const settingsStatusEl = document.querySelector('#settings-status');
const coupleNameEl = document.querySelector('#coupleName');
const relationshipStartedAtEl = document.querySelector('#relationshipStartedAt');
const relationshipSummaryEl = document.querySelector('#relationship-summary');
const relationshipDetailsEl = document.querySelector('#relationship-details');
const relationshipCountEl = document.querySelector('#relationship-count');
const userRosterEl = document.querySelector('#user-roster');
const userForm = document.querySelector('#user-form');
const userNameEl = document.querySelector('#userName');
const userColorEl = document.querySelector('#userColor');
const userThemeEl = document.querySelector('#userTheme');
const userReminderMinutesEl = document.querySelector('#userReminderMinutes');
const userResetBtn = document.querySelector('#user-reset');
const userStatusEl = document.querySelector('#user-status');
const deleteDialogEl = document.querySelector('#delete-dialog');
const deleteDialogTitleEl = document.querySelector('#delete-dialog-title');
const deleteDialogMessageEl = document.querySelector('#delete-dialog-message');
const deleteDialogConfirmBtn = document.querySelector('#delete-dialog-confirm');
const deleteDialogCancelBtn = document.querySelector('#delete-dialog-cancel');
const deleteDialogCloseBtn = document.querySelector('#delete-dialog-close');

const fields = {
  kind: document.querySelector('#kind'),
  title: document.querySelector('#title'),
  details: document.querySelector('#details'),
  eventDate: document.querySelector('#eventDate'),
  reminderAt: document.querySelector('#reminderAt')
};

let editingId = null;
let entries = [];
let users = [];
let activeUserId = Number(localStorage.getItem('relationship-planner-active-user-id')) || 0;
let activeUser = null;
let userFormMode = 'edit';
let calendarCursor = startOfMonth(new Date());
let selectedDate = startOfDay(new Date());
let selectedGame = null;
let gameResults = [];
let ratingDialogEntryId = null;
let ratingDialogValue = '';
let editingMemoId = null;
let memoInkDataUrl = '';
let memoCanvasHasInk = false;
let memoCanvasContext = null;
let memoCanvasDrawing = false;
let memoCanvasLastPoint = null;
let pendingDeleteEntry = null;
let sharedSettings = {
  coupleName: '',
  relationshipStartedAt: ''
};

function escapeHtml(value) {
  return String(value)
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;')
    .replaceAll("'", '&#39;');
}

function getSystemTheme() {
  return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
}

function applyTheme(theme) {
  const nextTheme = theme === 'dark' ? 'dark' : 'light';
  document.body.dataset.theme = nextTheme;
  themeToggleBtn.textContent = nextTheme === 'dark' ? 'Light mode' : 'Dark mode';
  themeToggleBtn.setAttribute('aria-pressed', String(nextTheme === 'dark'));
}

function setBodyAccent(color) {
  document.body.style.setProperty('--user-accent', color || '#1976d2');
}

function syncThemeFromActiveUser() {
  if (!activeUser) {
    setBodyAccent('#1976d2');
    applyTheme(getSystemTheme());
    return;
  }
  setBodyAccent(activeUser.accentColor || '#1976d2');
  applyTheme(activeUser.preferredTheme || getSystemTheme());
}

function toggleTheme() {
  if (!activeUser) {
    return;
  }
  const nextTheme = activeUser.preferredTheme === 'dark' ? 'light' : 'dark';
  userThemeEl.value = nextTheme;
  userForm.requestSubmit();
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

function syncSharedSettingsForm() {
  coupleNameEl.value = sharedSettings.coupleName || '';
  relationshipStartedAtEl.value = toDatetimeLocal(sharedSettings.relationshipStartedAt);
}

function updateRelationshipSummary() {
  if (!sharedSettings.relationshipStartedAt) {
    relationshipSummaryEl.textContent = 'Set your start date';
    relationshipDetailsEl.textContent = 'This helps the app show how long you have been together.';
    relationshipCountEl.textContent = '--';
    if (relationshipCountHeroEl) {
      relationshipCountHeroEl.textContent = '--';
    }
    return;
  }

  relationshipSummaryEl.textContent = sharedSettings.coupleName
    ? `${sharedSettings.coupleName} started`
    : 'Relationship started';
  relationshipDetailsEl.textContent = `Since ${toFriendlyDate(sharedSettings.relationshipStartedAt)}. Next anniversary: ${computeNextAnniversary(sharedSettings.relationshipStartedAt)}.`;
  const duration = formatRelationshipDuration(sharedSettings.relationshipStartedAt);
  relationshipCountEl.textContent = duration;
  if (relationshipCountHeroEl) {
    relationshipCountHeroEl.textContent = duration;
  }
}

function updateMemoComposerSummary() {
  if (memoSelectedDateEl) {
    memoSelectedDateEl.textContent = `Memo for ${formatCalendarDate(selectedDate)}.`;
  }
  if (!editingMemoId && memoStatusEl) {
    memoStatusEl.textContent = `Memo for ${formatCalendarDate(selectedDate)}.`;
  }
}

function syncMemoComposerMode() {
  memoSaveBtn.textContent = editingMemoId ? 'Update memo' : 'Save memo';
  memoStatusEl.textContent = editingMemoId
    ? 'Editing an existing memo.'
    : `Memo for ${formatCalendarDate(selectedDate)}.`;
}

function openEntryDialog(entry = null) {
  setView('planner');
  if (entry) {
    editingId = entry.id;
    fields.kind.value = entry.kind;
    fields.title.value = entry.title;
    fields.details.value = entry.details;
    fields.eventDate.value = toDatetimeLocal(entry.eventDate);
    fields.reminderAt.value = entry.reminderAt ? toDatetimeLocal(entry.reminderAt) : '';
    if (['game_plan', 'game_status'].includes(entry.kind)) {
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
      gameStatusEl.value = entry.gameStatus || 'want_to_play';
      syncGamePlanControls();
    } else {
      clearSelectedGame();
      syncGamePlanControls();
    }
    form.querySelector('h2').textContent = 'Edit item';
    setStatus(`Editing "${entry.title}".`);
  } else {
    editingId = null;
    form.reset();
    fields.kind.value = 'date';
    syncFormDate(selectedDate);
    fields.reminderAt.value = '';
    clearSelectedGame();
    syncGamePlanControls();
    form.querySelector('h2').textContent = 'Add something special';
    setStatus('Ready to add a new item.');
  }

  if (entryDialogEl && !entryDialogEl.open) {
    entryDialogEl.showModal();
  }
}

function closeEntryDialog() {
  if (entryDialogEl?.open) {
    entryDialogEl.close();
  }
}

function openMemoDialog(entry = null) {
  setView('planner');
  if (entry) {
    setMemoComposerFromEntry(entry);
  } else {
    resetMemoComposer();
    editingMemoId = null;
    memoTitleEl.value = '';
    memoTextEl.value = '';
    clearMemoSketch(true);
    syncMemoComposerMode();
  }

  if (memoDialogEl && !memoDialogEl.open) {
    memoDialogEl.showModal();
  }
  resizeMemoCanvas();
  redrawMemoCanvas();
  updateMemoComposerSummary();
}

function closeMemoDialog() {
  if (memoDialogEl?.open) {
    memoDialogEl.close();
  }
}

function openDeleteDialog(entry) {
  pendingDeleteEntry = entry;
  deleteDialogTitleEl.textContent = `Delete "${entry.title}"?`;
  deleteDialogMessageEl.textContent = `This will permanently remove the ${friendlyKind(entry.kind).toLowerCase()} created by ${entry.createdByUserName || 'Unknown'}.`;
  if (deleteDialogEl && !deleteDialogEl.open) {
    deleteDialogEl.showModal();
  }
}

function closeDeleteDialog() {
  pendingDeleteEntry = null;
  if (deleteDialogEl?.open) {
    deleteDialogEl.close();
  }
}

async function confirmDeleteEntry() {
  if (!pendingDeleteEntry) {
    return;
  }

  const entry = pendingDeleteEntry;
  closeDeleteDialog();
  await fetch(`/api/entries/${entry.id}`, { method: 'DELETE' });
  if (editingId === entry.id) {
    editingId = null;
    closeEntryDialog();
  }
  if (editingMemoId === entry.id) {
    resetMemoComposer();
    closeMemoDialog();
  }
  await loadEntries();
  setStatus('Item deleted.');
}

function setMemoComposerFromEntry(entry) {
  const memoDetails = parseMemoDetails(entry.details);
  editingMemoId = entry.id;
  selectedDate = startOfDay(new Date(entry.eventDate));
  renderCalendar();
  renderAgenda();
  syncFormDate(selectedDate);
  memoTitleEl.value = entry.title || '';
  memoTextEl.value = memoDetails.memoText || '';
  memoInkDataUrl = memoDetails.memoInk || '';
  memoCanvasHasInk = Boolean(memoDetails.memoInk);
  syncMemoComposerMode();
}

function resizeMemoCanvas() {
  if (!memoCanvasEl) {
    return;
  }

  const rect = memoCanvasEl.getBoundingClientRect();
  const ratio = window.devicePixelRatio || 1;
  const width = Math.max(1, Math.floor(rect.width * ratio));
  const height = Math.max(1, Math.floor(rect.height * ratio));

  if (memoCanvasEl.width === width && memoCanvasEl.height === height) {
    return;
  }

  const snapshot = memoCanvasEl.width && memoCanvasEl.height ? memoCanvasEl.toDataURL() : '';
  memoCanvasEl.width = width;
  memoCanvasEl.height = height;
  memoCanvasContext = memoCanvasEl.getContext('2d');
  if (memoCanvasContext) {
    memoCanvasContext.scale(ratio, ratio);
    memoCanvasContext.lineWidth = 3;
    memoCanvasContext.lineCap = 'round';
    memoCanvasContext.lineJoin = 'round';
    memoCanvasContext.strokeStyle = getComputedStyle(document.body).getPropertyValue('--user-accent').trim() || '#1976d2';
    memoCanvasContext.clearRect(0, 0, rect.width, rect.height);
    if (snapshot) {
      const image = new Image();
      image.onload = () => {
        memoCanvasContext.clearRect(0, 0, rect.width, rect.height);
        memoCanvasContext.drawImage(image, 0, 0, rect.width, rect.height);
      };
      image.src = snapshot;
    }
  }
}

function redrawMemoCanvas() {
  if (!memoCanvasEl || !memoCanvasContext) {
    return;
  }

  const rect = memoCanvasEl.getBoundingClientRect();
  memoCanvasContext.clearRect(0, 0, rect.width, rect.height);
  if (memoInkDataUrl) {
    const image = new Image();
    image.onload = () => memoCanvasContext.drawImage(image, 0, 0, rect.width, rect.height);
    image.src = memoInkDataUrl;
  }
}

function clearMemoSketch(preserveStatus = false) {
  memoInkDataUrl = '';
  memoCanvasHasInk = false;
  memoCanvasDrawing = false;
  memoCanvasLastPoint = null;
  if (memoCanvasContext && memoCanvasEl) {
    const rect = memoCanvasEl.getBoundingClientRect();
    memoCanvasContext.clearRect(0, 0, rect.width, rect.height);
  }
  if (!preserveStatus && memoStatusEl) {
    memoStatusEl.textContent = 'Sketch cleared.';
  }
}

function captureMemoSketch() {
  if (!memoCanvasEl) {
    return '';
  }
  memoInkDataUrl = memoCanvasEl.toDataURL('image/png');
  return memoInkDataUrl;
}

function getCanvasPoint(event) {
  const rect = memoCanvasEl.getBoundingClientRect();
  return {
    x: event.clientX - rect.left,
    y: event.clientY - rect.top
  };
}

function drawMemoLine(from, to) {
  if (!memoCanvasContext) {
    return;
  }
  memoCanvasContext.beginPath();
  memoCanvasContext.moveTo(from.x, from.y);
  memoCanvasContext.lineTo(to.x, to.y);
  memoCanvasContext.stroke();
}

function setupMemoCanvas() {
  if (!memoCanvasEl) {
    return;
  }

  resizeMemoCanvas();

  memoCanvasEl.addEventListener('pointerdown', (event) => {
    event.preventDefault();
    memoCanvasEl.setPointerCapture(event.pointerId);
    memoCanvasDrawing = true;
    memoCanvasHasInk = true;
    memoCanvasLastPoint = getCanvasPoint(event);
    if (memoCanvasContext) {
      memoCanvasContext.strokeStyle = getComputedStyle(document.body).getPropertyValue('--user-accent').trim() || '#1976d2';
      memoCanvasContext.beginPath();
      memoCanvasContext.moveTo(memoCanvasLastPoint.x, memoCanvasLastPoint.y);
    }
  });

  memoCanvasEl.addEventListener('pointermove', (event) => {
    if (!memoCanvasDrawing || !memoCanvasLastPoint) {
      return;
    }
    const nextPoint = getCanvasPoint(event);
    memoCanvasHasInk = true;
    drawMemoLine(memoCanvasLastPoint, nextPoint);
    memoCanvasLastPoint = nextPoint;
  });

  const finishDrawing = () => {
    if (!memoCanvasDrawing) {
      return;
    }
    memoCanvasDrawing = false;
    memoCanvasLastPoint = null;
    captureMemoSketch();
  };

  memoCanvasEl.addEventListener('pointerup', finishDrawing);
  memoCanvasEl.addEventListener('pointercancel', finishDrawing);
  memoCanvasEl.addEventListener('pointerleave', finishDrawing);
  window.addEventListener('resize', () => {
    resizeMemoCanvas();
    redrawMemoCanvas();
  });
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
    memory: 'Memo',
    got_together: 'Got together',
    game_plan: 'Game plan',
    game_status: 'Game status'
  }[kind] || kind;
}

function parseMemoDetails(details) {
  if (!details) {
    return { memoText: '', memoInk: '' };
  }

  try {
    const parsed = JSON.parse(details);
    if (parsed && typeof parsed === 'object' && ('memoText' in parsed || 'memoInk' in parsed)) {
      return {
        memoText: typeof parsed.memoText === 'string' ? parsed.memoText : '',
        memoInk: typeof parsed.memoInk === 'string' ? parsed.memoInk : ''
      };
    }
  } catch {
    // Fall through to treat the details as plain text.
  }

  return {
    memoText: details,
    memoInk: ''
  };
}

function serializeMemoDetails(memoText, memoInk = '') {
  const payload = {
    memoText: memoText || '',
    memoInk: memoInk || ''
  };
  if (!payload.memoInk) {
    return payload.memoText;
  }
  return JSON.stringify(payload);
}

function getMemoTitle(memoText, memoInk) {
  const cleanText = String(memoText || '').trim();
  if (cleanText) {
    return cleanText.split(/\r?\n/)[0].slice(0, 64);
  }
  if (memoInk) {
    return 'Sketch memo';
  }
  return 'Day memo';
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

function setSettingsStatus(message, isError = false) {
  settingsStatusEl.textContent = message;
  settingsStatusEl.style.color = isError ? '#a03222' : '';
}

function setUserStatus(message, isError = false) {
  userStatusEl.textContent = message;
  userStatusEl.style.color = isError ? '#a03222' : '';
}

function syncFormDate(date = selectedDate) {
  fields.eventDate.value = toDatetimeLocal(date);
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

function clearSelectedGame() {
  selectedGame = null;
  gameSelectedTitleEl.textContent = 'Nothing selected yet';
  gameSelectedMetaEl.textContent = 'Search and pick a game to prefill the planner.';
  gameSelectedSummaryEl.textContent = '';
  gameSelectedCoverEl.removeAttribute('src');
  gameSelectedCoverEl.alt = '';
  useGameBtn.disabled = true;
  saveStatusBtn.disabled = true;
}

function setSelectedGame(game) {
  selectedGame = game;
  gameSelectedTitleEl.textContent = game.name;
  gameSelectedMetaEl.textContent = game.released ? `Released ${game.released}` : 'Release date unavailable';
  gameSelectedSummaryEl.textContent = gameSummary(game);
  gameSelectedCoverEl.src = game.backgroundImage || '';
  gameSelectedCoverEl.alt = game.name;
  useGameBtn.disabled = false;
  saveStatusBtn.disabled = false;
}

function prefillGamePlan(game, options = {}) {
  const normalizedGame = {
    id: game.id || 0,
    name: game.name || game.title || 'Untitled game',
    slug: game.slug || '',
    released: game.released || '',
    metacritic: game.metacritic || 0,
    backgroundImage: game.backgroundImage || '',
    platforms: game.platforms || []
  };

  setSelectedGame(normalizedGame);
  fields.kind.value = 'game_plan';
  fields.title.value = normalizedGame.name;
  fields.details.value = options.details || gamePlanDetails(normalizedGame);
  syncFormDate(options.eventDate || selectedDate);
  fields.reminderAt.value = options.reminderAt ?? '';
  gameStatusEl.value = options.gameStatus || 'want_to_play';
  syncGamePlanControls();
  setView('planner');
  fields.title.focus();
  fields.title.select();
  setStatus(options.statusMessage || `Loaded "${normalizedGame.name}" into the planner.`);
}

function openGameStatusInPlanner(entry) {
  prefillGamePlan(
    {
      id: entry.rawgGameId || entry.id || 0,
      name: entry.title,
      slug: entry.rawgSlug,
      released: entry.rawgReleased,
      metacritic: entry.rawgMetacritic,
      backgroundImage: entry.rawgBackgroundImage,
      platforms: entry.rawgPlatforms || []
    },
    {
      eventDate: entry.kind === 'game_plan' ? entry.eventDate : selectedDate,
      gameStatus: entry.gameStatus || 'want_to_play',
      statusMessage:
        entry.kind === 'game_plan'
          ? `Loaded "${entry.title}" back into the planner.`
          : `Quick-added "${entry.title}" to the planner.`
    }
  );
}

function findExistingGameEntry(rawgGameId) {
  if (!rawgGameId) {
    return null;
  }

  return entries.find(
    (entry) => ['game_plan', 'game_status'].includes(entry.kind) && entry.rawgGameId === rawgGameId
  ) || null;
}

function uniqueGameEntries(gameEntries) {
  const byKey = new Map();

  for (const entry of gameEntries) {
    const key = entry.rawgGameId ? `rawg:${entry.rawgGameId}` : `entry:${entry.id}`;
    const current = byKey.get(key);
    if (!current) {
      byKey.set(key, entry);
      continue;
    }

    if (current.kind !== 'game_plan' && entry.kind === 'game_plan') {
      byKey.set(key, entry);
      continue;
    }

    if (current.kind === entry.kind && current.updatedAt && entry.updatedAt) {
      if (Date.parse(entry.updatedAt) > Date.parse(current.updatedAt)) {
        byKey.set(key, entry);
      }
    }
  }

  return [...byKey.values()];
}

async function saveSelectedGameStatusOnly() {
  if (!selectedGame) {
    setGameStatus('Pick a game first.', true);
    return;
  }

  const payload = {
    userId: activeUserId,
    kind: 'game_status',
    title: selectedGame.name,
    details: gamePlanDetails(selectedGame),
    eventDate: new Date().toISOString(),
    reminderAt: '',
    rawgGameId: selectedGame.id || 0,
    rawgSlug: selectedGame.slug || '',
    rawgBackgroundImage: selectedGame.backgroundImage || '',
    rawgPlatforms: selectedGame.platforms || [],
    rawgMetacritic: selectedGame.metacritic || 0,
    rawgReleased: selectedGame.released || '',
    gameStatus: gameStatusEl.value || 'want_to_play'
  };

  const existing = findExistingGameEntry(payload.rawgGameId);

  const response = await fetch(existing ? `/api/entries/${existing.id}` : '/api/entries', {
    method: existing ? 'PUT' : 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(existing
      ? { ...payload, kind: existing.kind, eventDate: existing.eventDate }
      : payload)
  });
  const data = await response.json();
  if (!response.ok) {
    setGameStatus(data.error || 'Could not save game status.', true);
    return;
  }

  setGameStatus(existing ? `Updated "${selectedGame.name}" status-only record.` : `Saved "${selectedGame.name}" as a status-only game.`);
  await loadEntries();
}

function resetForm() {
  editingId = null;
  form.reset();
  fields.kind.value = 'date';
  syncFormDate(new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString());
  fields.reminderAt.value = '';
  clearSelectedGame();
  syncGamePlanControls();
  setStatus('Ready to add a new item.');
}

function resolveActiveUser() {
  if (!users.length) {
    activeUser = null;
    activeUserId = 0;
    localStorage.removeItem('relationship-planner-active-user-id');
    return null;
  }

  const resolved = users.find((user) => user.id === activeUserId) || users[0];
  activeUser = resolved;
  activeUserId = resolved.id;
  localStorage.setItem('relationship-planner-active-user-id', String(activeUserId));
  return activeUser;
}

function renderUserSwitcher() {
  activeUserSelectEl.innerHTML = '';
  for (const user of users) {
    const option = document.createElement('option');
    option.value = String(user.id);
    option.textContent = user.name;
    activeUserSelectEl.appendChild(option);
  }
  activeUserSelectEl.value = activeUser ? String(activeUser.id) : '';
  activeUserSelectEl.disabled = users.length === 0;
  activeUserSwatchEl.style.background = activeUser?.accentColor || '#1976d2';
  activeUserSwatchEl.title = activeUser ? `${activeUser.name} color` : 'Active user color';
}

function renderUsers() {
  userRosterEl.innerHTML = '';

  if (!users.length) {
    userRosterEl.innerHTML = '<p class="status">No users yet.</p>';
    return;
  }

  for (const user of users) {
    const button = document.createElement('button');
    button.type = 'button';
    button.className = 'user-card';
    if (activeUser && user.id === activeUser.id) {
      button.classList.add('is-active');
    }
    button.innerHTML = `
      <span class="user-card-swatch" style="background:${escapeHtml(user.accentColor)}"></span>
      <span class="user-card-name">${escapeHtml(user.name)}</span>
      <span class="user-card-meta">${escapeHtml(user.preferredTheme)} · ${escapeHtml(String(user.defaultReminderMinutes))}m reminders</span>
    `;
    button.addEventListener('click', async () => {
      await setActiveUser(user.id);
    });
    userRosterEl.appendChild(button);
  }
}

function syncUserForm(mode = userFormMode) {
  const user = mode === 'edit' ? activeUser : activeUser;
  const defaults = user || {
    name: '',
    accentColor: '#1976d2',
    preferredTheme: getSystemTheme(),
    defaultReminderMinutes: 60
  };

  userNameEl.value = mode === 'edit' && user ? user.name : '';
  userColorEl.value = defaults.accentColor || '#1976d2';
  userThemeEl.value = defaults.preferredTheme || getSystemTheme();
  userReminderMinutesEl.value = String(defaults.defaultReminderMinutes ?? 60);

  const saveButton = userForm.querySelector('button[type="submit"]');
  saveButton.textContent = mode === 'create' ? 'Create user' : 'Save user';
  userResetBtn.textContent = mode === 'create' ? 'Cancel' : 'New user';

  if (mode === 'create') {
    setUserStatus('Creating a new profile.');
  } else if (user) {
    setUserStatus(`Editing ${user.name}.`);
  } else {
    setUserStatus('Create your first user.');
  }
}

async function persistUser(payload, method, userId = 0) {
  const response = await fetch(method === 'POST' ? '/api/users' : `/api/users/${userId}`, {
    method,
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload)
  });
  const data = await response.json();
  if (!response.ok) {
    throw new Error(data.error || 'Could not save user.');
  }
  return data.user;
}

async function setActiveUser(userId) {
  activeUserId = Number(userId);
  localStorage.setItem('relationship-planner-active-user-id', String(activeUserId));
  resolveActiveUser();
  userFormMode = 'edit';
  syncThemeFromActiveUser();
  renderUserSwitcher();
  renderUsers();
  syncUserForm('edit');
  await loadEntries();
}

function buildSharedSettingsPayload() {
  return {
    coupleName: coupleNameEl.value.trim(),
    relationshipStartedAt: relationshipStartedAtEl.value ? new Date(relationshipStartedAtEl.value).toISOString() : ''
  };
}

function buildUserPayload() {
  return {
    name: userNameEl.value.trim(),
    accentColor: userColorEl.value,
    preferredTheme: userThemeEl.value,
    defaultReminderMinutes: Number(userReminderMinutesEl.value || 0)
  };
}

async function updateGameStatus(entryId, gameStatus) {
  const response = await fetch(`/api/entries/${entryId}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      userId: activeUserId,
      gameStatus
    })
  });

  const data = await response.json();
  if (!response.ok) {
    setGameStatus(data.error || 'Could not update game status.', true);
    return;
  }

  setGameStatus(`Updated "${data.entry?.title || 'game'}" to ${gameStatusLabel(gameStatus)}.`);
  await loadEntries();
}

async function updateGameRating(entryId, gameRating) {
  const response = await fetch(`/api/entries/${entryId}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      userId: activeUserId,
      gameRating
    })
  });

  const data = await response.json();
  if (!response.ok) {
    setGameStatus(data.error || 'Could not update game rating.', true);
    return;
  }

  const label = gameRating ? `${gameRating}/5` : 'cleared';
  setGameStatus(`Updated "${data.entry?.title || 'game'}" rating to ${label}.`);
  await loadEntries();
}

function formatGameRating(value) {
  return value ? `${value}/5` : 'Not rated';
}

function setRatingDialogValue(value) {
  ratingDialogValue = value || '';
  gameRatingValueEl.textContent = formatGameRating(ratingDialogValue);
  for (const button of gameRatingOptionsEl.querySelectorAll('[data-game-rating-option]')) {
    const isActive = button.dataset.gameRatingOption === ratingDialogValue;
    button.classList.toggle('is-active', isActive);
    button.setAttribute('aria-pressed', String(isActive));
  }
}

function openGameRatingDialog(entry) {
  ratingDialogEntryId = entry.id;
  gameRatingTitleEl.textContent = entry.title;
  gameRatingMetaEl.textContent = `${gameStatusLabel(entry.gameStatus)} - ${toFriendlyDate(entry.eventDate)}`;
  setRatingDialogValue(entry.gameRating || '');
  gameRatingDialogEl.showModal();
}

function closeGameRatingDialog() {
  if (gameRatingDialogEl.open) {
    gameRatingDialogEl.close();
  }
  ratingDialogEntryId = null;
  ratingDialogValue = '';
}

function renderSummary() {
  totalCountEl.textContent = String(entries.length);
  const next = entries.find((entry) => entry.kind !== 'game_status' && Date.parse(entry.eventDate) >= Date.now()) || entries.find((entry) => entry.kind !== 'game_status');
  nextCountdownEl.textContent = next ? timeUntil(next.eventDate) : '--';
}

function renderGameStatusWindow() {
  const gamePlans = uniqueGameEntries(entries.filter((entry) => ['game_plan', 'game_status'].includes(entry.kind)));
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

  if (gameStatusSummaryEl) {
    gameStatusSummaryEl.textContent = activeUser
      ? `Saved game statuses for ${activeUser.name}.`
      : 'Saved game statuses.';
  }

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

function renderGameStatusManager() {
  const gamePlans = uniqueGameEntries(entries.filter((entry) => ['game_plan', 'game_status'].includes(entry.kind)));
  gameStatusManagerEl.innerHTML = '';

  if (gamePlans.length === 0) {
    const empty = document.createElement('p');
    empty.className = 'status compact';
    empty.textContent = 'No game statuses yet. Search RAWG or save a status-only game to manage them here.';
    gameStatusManagerEl.appendChild(empty);
    return;
  }

  for (const entry of gamePlans) {
    const card = document.createElement('article');
    card.className = 'game-status-item';
    const plannerButtonLabel = entry.kind === 'game_status' ? 'Quick add to planner' : 'Edit planner';
    const ratingText = entry.gameRating ? ` · Rating ${formatGameRating(entry.gameRating)}` : ' · Not rated';
    card.innerHTML = `
      <div class="game-status-item-head">
        <div class="game-status-item-banner">
          ${
            entry.rawgBackgroundImage
              ? `<img src="${escapeHtml(entry.rawgBackgroundImage)}" alt="${escapeHtml(entry.title)}" />`
              : `<span class="game-status-item-placeholder">${escapeHtml(entry.title.slice(0, 2).toUpperCase())}</span>`
          }
        </div>
        <div class="game-status-item-copy">
          <p class="tag">${escapeHtml(friendlyKind(entry.kind))}</p>
          <h4>${escapeHtml(entry.title)}</h4>
          <p class="game-meta">${escapeHtml(gameStatusLabel(entry.gameStatus))} · ${escapeHtml(toFriendlyDate(entry.eventDate))}${escapeHtml(ratingText)}</p>
        </div>
      </div>
      <div class="game-status-item-actions">
        <label>
          <span>Status</span>
          <select data-game-status-select="${entry.id}">
            <option value="want_to_play">Want to play</option>
            <option value="playing">Playing</option>
            <option value="played">Played</option>
            <option value="finished">Finished</option>
          </select>
        </label>
        <div class="game-status-item-buttons">
          <button type="button" class="secondary" data-game-status-planner="${entry.id}">${plannerButtonLabel}</button>
          <button type="button" class="secondary" data-game-status-rate="${entry.id}">Rate</button>
          <button type="button" class="secondary" data-game-status-save="${entry.id}">Save status</button>
        </div>
      </div>
    `;

    const select = card.querySelector(`[data-game-status-select="${entry.id}"]`);
    const plannerButton = card.querySelector(`[data-game-status-planner="${entry.id}"]`);
    const rateButton = card.querySelector(`[data-game-status-rate="${entry.id}"]`);
    const saveButton = card.querySelector(`[data-game-status-save="${entry.id}"]`);
    select.value = entry.gameStatus || 'want_to_play';
    plannerButton.addEventListener('click', () => {
      openGameStatusInPlanner(entry);
    });
    rateButton.addEventListener('click', () => {
      openGameRatingDialog(entry);
    });
    saveButton.addEventListener('click', async () => {
      await updateGameStatus(entry.id, select.value);
    });
    gameStatusManagerEl.appendChild(card);
  }
}

function entriesForDate(date) {
  const key = toLocalDateKey(date);
  return entries.filter((entry) => entry.kind !== 'game_status' && toLocalDateKey(entry.eventDate) === key);
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
  updateMemoComposerSummary();
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
    item.style.setProperty('--entry-accent', entry.createdByUserColor || 'var(--accent)');

    const tag = document.createElement('p');
    tag.className = 'tag';
    tag.textContent = friendlyKind(entry.kind);

    const title = document.createElement('h3');
    title.textContent = entry.title;

    const meta = document.createElement('p');
    meta.className = 'agenda-meta';
    const ratingText = ['game_plan', 'game_status'].includes(entry.kind) && entry.gameRating
      ? ` · Rating ${formatGameRating(entry.gameRating)}`
      : '';
    const memoDetails = entry.kind === 'memory' ? parseMemoDetails(entry.details) : null;
    meta.textContent =
      entry.kind === 'got_together'
        ? `Together for ${timeSince(entry.eventDate)} - since ${toFriendlyDate(entry.eventDate)}`
        : entry.kind === 'game_status'
          ? `Status-only game - ${toFriendlyDate(entry.eventDate)}${ratingText}`
          : entry.kind === 'game_plan'
            ? `Planned game session - ${toFriendlyDate(entry.eventDate)}${ratingText}`
          : `${toFriendlyDate(entry.eventDate)} - ${timeUntil(entry.eventDate)}`;

    const owner = document.createElement('span');
    owner.className = 'user-chip';
    owner.style.setProperty('--chip-color', entry.createdByUserColor || '#1976d2');
    owner.textContent = entry.createdByUserName || 'Unknown';

    item.append(tag, title, meta, owner);

    if (memoDetails?.memoText || memoDetails?.memoInk) {
      const memoBody = document.createElement('div');
      memoBody.className = 'memo-body';
      if (memoDetails.memoInk) {
        const memoInk = document.createElement('img');
        memoInk.className = 'memo-sketch-preview';
        memoInk.src = memoDetails.memoInk;
        memoInk.alt = `${entry.title} sketch`;
        memoInk.loading = 'lazy';
        memoBody.appendChild(memoInk);
      }
      if (memoDetails.memoText) {
        const memoText = document.createElement('p');
        memoText.className = 'details';
        memoText.textContent = memoDetails.memoText;
        memoBody.appendChild(memoText);
      } else if (memoDetails.memoInk) {
        const memoText = document.createElement('p');
        memoText.className = 'details';
        memoText.textContent = 'Sketch memo';
        memoBody.appendChild(memoText);
      }
      item.appendChild(memoBody);
    }

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
      <img class="game-result-cover" src="${escapeHtml(game.backgroundImage || '')}" alt="${escapeHtml(game.name)}" />
      <div class="game-result-copy">
        <p class="tag">RAWG</p>
        <h3>${escapeHtml(game.name)}</h3>
        <p class="game-meta">${escapeHtml(gameSummary(game))}</p>
      </div>
      <div class="game-result-actions">
        <button class="secondary" type="button" data-action="plan">Use</button>
        <button class="secondary" type="button" data-action="status">Status only</button>
      </div>
    `;

    const planButton = card.querySelector('[data-action="plan"]');
    const statusButton = card.querySelector('[data-action="status"]');
    planButton.addEventListener('click', () => {
      prefillGamePlan(game);
      setGameStatus(`Selected "${game.name}" for planning.`);
    });
    statusButton.addEventListener('click', async () => {
      setSelectedGame(game);
      await saveSelectedGameStatusOnly();
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
    renderGameStatusManager();
    renderCalendar();
    renderAgenda();
    return;
  }

  for (const entry of entries) {
    const node = template.content.firstElementChild.cloneNode(true);
    node.style.setProperty('--entry-accent', entry.createdByUserColor || 'var(--accent)');
    const header = node.querySelector('.entry-top > div');
    const colorDot = document.createElement('span');
    colorDot.className = 'entry-color-dot';
    colorDot.style.setProperty('--dot-color', entry.createdByUserColor || '#1976d2');
    header.prepend(colorDot);
    node.querySelector('.tag').textContent = friendlyKind(entry.kind);
    node.querySelector('h3').textContent = entry.title;

    const metaEl = node.querySelector('.entry-meta');
    metaEl.innerHTML = '';
    const creatorChip = document.createElement('span');
    creatorChip.className = 'user-chip';
    creatorChip.style.setProperty('--chip-color', entry.createdByUserColor || '#1976d2');
    creatorChip.textContent = entry.createdByUserName || 'Unknown';
    const ratingText = ['game_plan', 'game_status'].includes(entry.kind)
      ? ` - Rating ${formatGameRating(entry.gameRating)}`
      : '';
    const metaText = document.createElement('span');
    metaText.textContent =
      entry.kind === 'got_together'
        ? `Together for ${timeSince(entry.eventDate)} - since ${toFriendlyDate(entry.eventDate)}`
        : ['game_plan', 'game_status'].includes(entry.kind)
          ? `${entry.kind === 'game_status' ? 'Status only' : toFriendlyDate(entry.eventDate)} - ${gameStatusLabel(entry.gameStatus)}${ratingText}${entry.rawgMetacritic ? ` - Metacritic ${entry.rawgMetacritic}` : ''}`
          : `${timeUntil(entry.eventDate)} - ${toFriendlyDate(entry.eventDate)}`;
    metaEl.append(creatorChip, metaText);

    const detailsParts = [];
    if (entry.kind === 'memory') {
      const memoDetails = parseMemoDetails(entry.details);
      if (memoDetails.memoText) {
        detailsParts.push(memoDetails.memoText);
      } else if (memoDetails.memoInk) {
        detailsParts.push('Sketch memo');
      }
      if (memoDetails.memoInk) {
        const memoArt = document.createElement('img');
        memoArt.className = 'memo-sketch-preview entry-memo-sketch';
        memoArt.src = memoDetails.memoInk;
        memoArt.alt = `${entry.title} sketch`;
        memoArt.loading = 'lazy';
        node.appendChild(memoArt);
      }
    } else if (entry.details) {
      detailsParts.push(entry.details);
    }
    if (['game_plan', 'game_status'].includes(entry.kind)) {
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

    if (['game_plan', 'game_status'].includes(entry.kind)) {
      const banner = document.createElement('div');
      banner.className = 'entry-banner';
      banner.style.setProperty('--entry-accent', entry.createdByUserColor || '#1976d2');
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
      if (entry.kind === 'game_status') {
        setGameStatus('Edit status-only games from the Games management card.', true);
        window.scrollTo({ top: 0, behavior: 'smooth' });
        return;
      }
      if (entry.kind === 'memory') {
        openMemoDialog(entry);
        return;
      }
      openEntryDialog(entry);
      window.scrollTo({ top: 0, behavior: 'smooth' });
    });

    if (entry.kind === 'game_status') {
      const editBtn = node.querySelector('.edit-btn');
      editBtn.disabled = true;
      editBtn.title = 'Use the Games status cards to update this item.';
    }

    node.querySelector('.delete-btn').addEventListener('click', async () => {
      openDeleteDialog(entry);
    });

    entriesEl.appendChild(node);
  }

  renderSummary();
  renderGameStatusWindow();
  renderGameStatusManager();
  renderCalendar();
  renderAgenda();
}

async function loadSharedSettings() {
  const response = await fetch('/api/settings');
  const data = await response.json();
  sharedSettings = {
    coupleName: data.settings?.coupleName || '',
    relationshipStartedAt: data.settings?.relationshipStartedAt || ''
  };
  syncSharedSettingsForm();
  updateRelationshipSummary();
}

async function loadUsers() {
  const response = await fetch('/api/users');
  const data = await response.json();
  users = data.users || [];
  resolveActiveUser();
  userFormMode = 'edit';
  renderUserSwitcher();
  renderUsers();
  syncUserForm('edit');
  syncThemeFromActiveUser();
}

async function loadEntries() {
  const response = await fetch(`/api/entries?userId=${encodeURIComponent(activeUserId || 0)}`);
  const data = await response.json();
  entries = data.entries || [];
  if (data.activeUserId && Number(data.activeUserId) !== activeUserId) {
    activeUserId = Number(data.activeUserId);
    localStorage.setItem('relationship-planner-active-user-id', String(activeUserId));
    resolveActiveUser();
    renderUserSwitcher();
    renderUsers();
    syncUserForm('edit');
    syncThemeFromActiveUser();
  }
  renderEntries();
}

async function saveSharedSettings() {
  const payload = buildSharedSettingsPayload();
  const response = await fetch('/api/settings', {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload)
  });
  const data = await response.json();
  if (!response.ok) {
    throw new Error(data.error || 'Could not save settings.');
  }
  sharedSettings = {
    coupleName: data.settings?.coupleName || '',
    relationshipStartedAt: data.settings?.relationshipStartedAt || ''
  };
  syncSharedSettingsForm();
  updateRelationshipSummary();
  setSettingsStatus('Settings saved.');
}

async function saveUserFromForm() {
  const payload = buildUserPayload();
  if (userFormMode === 'create') {
    if (!payload.name) {
      throw new Error('Display name is required.');
    }
    const user = await persistUser(payload, 'POST');
    users = [...users, user].sort((a, b) => a.id - b.id);
    userFormMode = 'edit';
    activeUserId = user.id;
    localStorage.setItem('relationship-planner-active-user-id', String(activeUserId));
    resolveActiveUser();
    renderUserSwitcher();
    renderUsers();
    syncUserForm('edit');
    syncThemeFromActiveUser();
    await loadEntries();
    setUserStatus(`Created ${user.name}.`);
    return;
  }

  if (!activeUser) {
    throw new Error('No user selected.');
  }

  const user = await persistUser(payload, 'PUT', activeUser.id);
  users = users.map((item) => (item.id === user.id ? user : item));
  resolveActiveUser();
  renderUserSwitcher();
  renderUsers();
  syncUserForm('edit');
  syncThemeFromActiveUser();
  await loadEntries();
  setUserStatus(`Saved ${user.name}.`);
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

async function saveEntry(event) {
  event.preventDefault();

  const eventDateValue = new Date(fields.eventDate.value).toISOString();
  let reminderAtValue = fields.reminderAt.value ? new Date(fields.reminderAt.value).toISOString() : '';

  if (!reminderAtValue && fields.kind.value !== 'memory' && activeUser) {
    const reminderDate = new Date(Date.parse(eventDateValue) - activeUser.defaultReminderMinutes * 60_000);
    reminderAtValue = reminderDate.toISOString();
  }

  const payload = {
    userId: activeUserId,
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

  if (!['game_plan', 'game_status'].includes(payload.kind)) {
    payload.rawgGameId = 0;
    payload.rawgSlug = '';
    payload.rawgBackgroundImage = '';
    payload.rawgPlatforms = [];
    payload.rawgMetacritic = 0;
    payload.rawgReleased = '';
    payload.gameStatus = 'want_to_play';
  }

  const existingGameEntry =
    !editingId && ['game_plan', 'game_status'].includes(payload.kind)
      ? findExistingGameEntry(payload.rawgGameId)
      : null;

  const method = editingId || existingGameEntry ? 'PUT' : 'POST';
  const url = editingId
    ? `/api/entries/${editingId}`
    : existingGameEntry
      ? `/api/entries/${existingGameEntry.id}`
      : '/api/entries';
  const body = existingGameEntry
    ? { ...payload, kind: payload.kind, eventDate: payload.eventDate }
    : payload;

  const response = await fetch(url, {
    method,
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body)
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
  closeEntryDialog();
  await loadEntries();
}

function resetMemoComposer() {
  editingMemoId = null;
  memoFormEl.reset();
  memoTitleEl.value = '';
  memoTextEl.value = '';
  clearMemoSketch(true);
  updateMemoComposerSummary();
  syncMemoComposerMode();
}

async function saveMemo(event) {
  event.preventDefault();

  const memoText = memoTextEl.value.trim();
  const memoInk = memoCanvasHasInk ? (memoInkDataUrl || captureMemoSketch()) : '';
  const wasEditing = Boolean(editingMemoId);
  if (!memoText && !memoInk) {
    memoStatusEl.textContent = 'Add some text or sketch something first.';
    return;
  }

  const payload = {
    userId: activeUserId,
    kind: 'memory',
    title: memoTitleEl.value.trim() || getMemoTitle(memoText, memoInk),
    details: serializeMemoDetails(memoText, memoInk),
    eventDate: selectedDate.toISOString(),
    reminderAt: ''
  };

  const response = await fetch(editingMemoId ? `/api/entries/${editingMemoId}` : '/api/entries', {
    method: editingMemoId ? 'PUT' : 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload)
  });
  const data = await response.json();
  if (!response.ok) {
    memoStatusEl.textContent = data.error || 'Could not save memo.';
    return;
  }

  resetMemoComposer();
  memoStatusEl.textContent = wasEditing
    ? `Updated memo for ${formatCalendarDate(selectedDate)}.`
    : `Saved memo for ${formatCalendarDate(selectedDate)}.`;
  closeMemoDialog();
  await loadEntries();
}

refreshBtn.addEventListener('click', loadEntries);
resetBtn.addEventListener('click', resetForm);
form.addEventListener('submit', saveEntry);
memoFormEl.addEventListener('submit', saveMemo);
openEntryDialogBtn.addEventListener('click', () => {
  openEntryDialog();
});
quickAddEntryBtn.addEventListener('click', () => {
  openEntryDialog();
});
openMemoDialogBtn.addEventListener('click', () => {
  openMemoDialog();
});
quickAddMemoBtn.addEventListener('click', () => {
  openMemoDialog();
});
entryDialogCloseBtn.addEventListener('click', closeEntryDialog);
memoDialogCloseBtn.addEventListener('click', closeMemoDialog);
memoClearSketchBtn.addEventListener('click', () => {
  clearMemoSketch();
});
memoResetBtn.addEventListener('click', () => {
  resetMemoComposer();
});
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
saveStatusBtn.addEventListener('click', async () => {
  await saveSelectedGameStatusOnly();
});
clearGameBtn.addEventListener('click', () => {
  clearSelectedGame();
  setGameStatus('Game selection cleared.');
});
for (const button of gameRatingOptionsEl.querySelectorAll('[data-game-rating-option]')) {
  button.addEventListener('click', () => {
    setRatingDialogValue(button.dataset.gameRatingOption || '');
  });
}
gameRatingSaveBtn.addEventListener('click', async () => {
  if (!ratingDialogEntryId) {
    return;
  }
  await updateGameRating(ratingDialogEntryId, ratingDialogValue);
  closeGameRatingDialog();
});
gameRatingClearBtn.addEventListener('click', async () => {
  if (!ratingDialogEntryId) {
    return;
  }
  await updateGameRating(ratingDialogEntryId, '');
  closeGameRatingDialog();
});
gameRatingCancelBtn.addEventListener('click', closeGameRatingDialog);
gameRatingCloseBtn.addEventListener('click', closeGameRatingDialog);
gameRatingDialogEl.addEventListener('click', (event) => {
  if (event.target === gameRatingDialogEl) {
    closeGameRatingDialog();
  }
});
deleteDialogConfirmBtn.addEventListener('click', confirmDeleteEntry);
deleteDialogCancelBtn.addEventListener('click', closeDeleteDialog);
deleteDialogCloseBtn.addEventListener('click', closeDeleteDialog);
deleteDialogEl.addEventListener('click', (event) => {
  if (event.target === deleteDialogEl) {
    closeDeleteDialog();
  }
});
themeToggleBtn.addEventListener('click', toggleTheme);
fields.kind.addEventListener('change', syncGamePlanControls);
activeUserSelectEl.addEventListener('change', async () => {
  await setActiveUser(Number(activeUserSelectEl.value));
});
plannerTabBtn.addEventListener('click', () => setView('planner'));
settingsTabBtn.addEventListener('click', () => setView('settings'));
settingsForm.addEventListener('submit', async (event) => {
  event.preventDefault();
  setSettingsStatus('Saving settings...');
  try {
    await saveSharedSettings();
  } catch (error) {
    setSettingsStatus(error instanceof Error ? error.message : 'Could not save settings.', true);
  }
});
settingsResetBtn.addEventListener('click', () => {
  syncSharedSettingsForm();
  setSettingsStatus('Settings reset to the last saved values.');
});
userForm.addEventListener('submit', async (event) => {
  event.preventDefault();
  setUserStatus(userFormMode === 'create' ? 'Creating user...' : 'Saving user...');
  try {
    await saveUserFromForm();
  } catch (error) {
    setUserStatus(error instanceof Error ? error.message : 'Could not save user.', true);
  }
});
userResetBtn.addEventListener('click', () => {
  if (userFormMode === 'create') {
    userFormMode = 'edit';
    syncUserForm('edit');
    return;
  }
  userFormMode = 'create';
  userNameEl.value = '';
  userColorEl.value = activeUser?.accentColor || '#1976d2';
  userThemeEl.value = activeUser?.preferredTheme || getSystemTheme();
  userReminderMinutesEl.value = String(activeUser?.defaultReminderMinutes ?? 60);
  syncUserForm('create');
});
window.addEventListener('hashchange', () => {
  setView(resolveInitialView());
});
entryDialogEl.addEventListener('click', (event) => {
  if (event.target === entryDialogEl) {
    closeEntryDialog();
  }
});
memoDialogEl.addEventListener('click', (event) => {
  if (event.target === memoDialogEl) {
    closeMemoDialog();
  }
});

fields.eventDate.value = toDatetimeLocal(new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString());
setStatus('Ready to add a new item.');
clearSelectedGame();
syncGamePlanControls();
applyTheme(getSystemTheme());
setView(resolveInitialView());
updateMemoComposerSummary();
syncMemoComposerMode();
setupMemoCanvas();
closeEntryDialog();
closeMemoDialog();

await loadUsers();
await loadSharedSettings();
await loadEntries();
