import {
    authorizedFetch,
    clearAuthState,
    getAccessToken,
    getMe,
    setAuthProfile
} from './auth.js';

const PROVIDERS = ['Google'];
const OAUTH_PROVIDERS = ['Google'];
const SETTINGS_STORAGE_KEY = 'ssn.settings.notifications';

let currentProfile = null;

document.addEventListener('DOMContentLoaded', () => {
    initialize().catch(handleError);
});

async function initialize() {
    const root = document.getElementById('account-page');
    if (!root) {
        return;
    }

    if (!getAccessToken()) {
        window.location.replace('/auth/login');
        return;
    }

    bindEvents();
    loadNotificationSettings();

    await refreshProfile();
}

function bindEvents() {
    document.getElementById('profile-form')?.addEventListener('submit', async (event) => {
        event.preventDefault();
        await saveProfileFromForm('profile');
    });

    document.getElementById('settings-account-form')?.addEventListener('submit', async (event) => {
        event.preventDefault();
        await saveProfileFromForm('settings');
    });

    document.getElementById('profile-avatar-upload-btn')?.addEventListener('click', () => {
        document.getElementById('profile-avatar-file')?.click();
    });

    document.getElementById('settings-upload-avatar-btn')?.addEventListener('click', () => {
        document.getElementById('settings-avatar-file')?.click();
    });

    document.getElementById('profile-avatar-file')?.addEventListener('change', async (event) => {
        await uploadAvatar(event.target.files?.[0]);
        event.target.value = '';
    });

    document.getElementById('settings-avatar-file')?.addEventListener('change', async (event) => {
        await uploadAvatar(event.target.files?.[0]);
        event.target.value = '';
    });

    document.getElementById('settings-save-notification-btn')?.addEventListener('click', () => {
        saveNotificationSettings();
    });

    document.getElementById('settings-logout-all-btn')?.addEventListener('click', async () => {
        await logoutAllDevices();
    });
}

async function refreshProfile() {
    const profile = await getMe();
    currentProfile = profile;

    setAuthProfile({
        userId: profile.userId,
        username: profile.username,
        email: profile.email,
        avatarUrl: profile.avatarUrl,
        bio: profile.bio,
        accountProvider: profile.accountProvider,
        createdAt: profile.createdAt,
        connectedProviders: profile.connectedProviders
    });

    renderProfile(profile);
}

function renderProfile(profile) {
    setText('profile-display-name', profile.username || 'My profile');
    setText('profile-display-email', profile.email || '-');
    setText('profile-bio-summary', profile.bio || 'Chua cap nhat tieu su.');
    setText('profile-provider', profile.accountProvider || 'Google');
    setText('profile-created-at', formatDate(profile.createdAt));
    setText('profile-status', profile.status || '-');

    setInputValue('profile-username', profile.username);
    setInputValue('profile-email', profile.email);
    setInputValue('profile-bio', profile.bio || '');

    setText('settings-display-name', profile.username || 'Account');
    setText('settings-display-email', profile.email || '-');
    setText('settings-provider-summary', `Primary: ${profile.accountProvider || 'Google'}`);

    setInputValue('settings-username', profile.username);
    setInputValue('settings-bio', profile.bio || '');

    setAvatar('profile-avatar-preview', profile.avatarUrl, profile.username);
    setAvatar('settings-avatar-preview', profile.avatarUrl, profile.username);

    renderConnectedProviders('profile-connected-providers', profile.connectedProviders || []);
    renderConnectedProviders('settings-connected-providers', profile.connectedProviders || []);
}

async function saveProfileFromForm(section) {
    const username = getInputValue(section === 'profile' ? 'profile-username' : 'settings-username');
    const bio = getInputValue(section === 'profile' ? 'profile-bio' : 'settings-bio');

    if (!username) {
        window.appToast('Username is required.');
        return;
    }

    const payload = {
        username,
        bio,
        avatarUrl: currentProfile?.avatarUrl || null
    };

    const updated = await authorizedJson('/api/users/profile', {
        method: 'PUT',
        body: JSON.stringify(payload)
    });

    currentProfile = updated;
    renderProfile(updated);
    document.getElementById('edit-profile')?.classList.remove('open');
    window.appToast('Profile updated successfully.', 'success');
}

async function uploadAvatar(file) {
    if (!file) {
        return;
    }

    const formData = new FormData();
    formData.append('avatar', file);

    const token = getAccessToken();
    if (!token) {
        throw new Error('Not authenticated.');
    }

    const response = await fetch('/api/users/avatar', {
        method: 'POST',
        headers: {
            Authorization: `Bearer ${token}`
        },
        body: formData
    });

    const payload = await parseApiResponse(response);

    currentProfile = payload.profile || currentProfile;
    if (payload.avatarUrl) {
        currentProfile.avatarUrl = payload.avatarUrl;
    }

    renderProfile(currentProfile);
    window.appToast('Avatar updated.', 'success');
}

function renderConnectedProviders(containerId, connectedProviders) {
    const container = document.getElementById(containerId);
    if (!container) {
        return;
    }

    const connectedSet = new Set((connectedProviders || []).map((x) => String(x).toLowerCase()));
    container.innerHTML = '';

    PROVIDERS.forEach((provider) => {
        const isConnected = connectedSet.has(provider.toLowerCase());

        const item = document.createElement('div');
        item.className = `provider-item ${isConnected ? 'is-connected' : ''}`;

        const left = document.createElement('div');
        left.innerHTML = `<div class="provider-name">${provider}</div>`;

        const right = document.createElement('div');
        right.className = `provider-status ${isConnected ? 'connected' : 'disconnected'}`;
        right.textContent = isConnected ? '✓ Connected' : '✗ Not connected';

        item.appendChild(left);
        item.appendChild(right);

        if (!isConnected && OAUTH_PROVIDERS.includes(provider)) {
            const connectLink = document.createElement('a');
            connectLink.href = `/auth/login/${provider.toLowerCase()}`;
            connectLink.className = 'btn btn-outline-primary btn-sm ms-2';
            connectLink.textContent = 'Connect';
            item.appendChild(connectLink);
        }

        container.appendChild(item);
    });
}

async function logoutAllDevices() {
    await authorizedJson('/api/auth/logout', {
        method: 'POST',
        body: JSON.stringify({})
    });

    clearAuthState();
    window.location.replace('/auth/login');
}

function loadNotificationSettings() {
    try {
        const raw = localStorage.getItem(SETTINGS_STORAGE_KEY);
        if (!raw) {
            return;
        }

        const settings = JSON.parse(raw);

        const enableNotifications = document.getElementById('settings-enable-notifications');
        const muteConversations = document.getElementById('settings-mute-conversations');

        if (enableNotifications) {
            enableNotifications.checked = !!settings.enableNotifications;
        }

        if (muteConversations) {
            muteConversations.checked = !!settings.muteConversations;
        }
    } catch {
        // Ignore malformed local settings.
    }
}

function saveNotificationSettings() {
    const enableNotifications = document.getElementById('settings-enable-notifications')?.checked ?? false;
    const muteConversations = document.getElementById('settings-mute-conversations')?.checked ?? false;

    localStorage.setItem(SETTINGS_STORAGE_KEY, JSON.stringify({
        enableNotifications,
        muteConversations
    }));

    window.appToast('Notification settings saved.', 'success');
}

async function authorizedJson(path, options) {
    const headers = new Headers(options?.headers || {});
    if (!headers.has('Content-Type') && !(options?.body instanceof FormData)) {
        headers.set('Content-Type', 'application/json');
    }

    const response = await authorizedFetch(path, {
        ...options,
        headers
    });

    return parseApiResponse(response);
}

async function parseApiResponse(response) {
    let payload = null;

    try {
        payload = await response.json();
    } catch {
        payload = null;
    }

    if (!response.ok) {
        throw new Error(payload?.message || `Request failed with status ${response.status}.`);
    }

    if (payload && payload.success === false) {
        throw new Error(payload.message || 'Request failed.');
    }

    return payload?.data ?? payload;
}

function setAvatar(id, avatarUrl, fallbackText) {
    const image = document.getElementById(id);
    if (!image) {
        return;
    }

    if (avatarUrl) {
        image.src = avatarUrl;
        return;
    }

    const initials = getInitials(fallbackText || 'U');
    const svg = `<svg xmlns='http://www.w3.org/2000/svg' width='256' height='256'><rect width='100%' height='100%' fill='%23e2e8f0'/><text x='50%' y='53%' dominant-baseline='middle' text-anchor='middle' font-family='Segoe UI, Arial' font-size='96' fill='%23334155'>${initials}</text></svg>`;
    image.src = `data:image/svg+xml;charset=UTF-8,${encodeURIComponent(svg)}`;
}
function getInitials(value) {
    return String(value)
        .split(/\s+/)
        .filter(Boolean)
        .slice(0, 2)
        .map((x) => x[0].toUpperCase())
        .join('') || 'U';
}

function setText(id, value) {
    const element = document.getElementById(id);
    if (element) {
        element.textContent = value || '-';
    }
}

function setInputValue(id, value) {
    const element = document.getElementById(id);
    if (element) {
        element.value = value || '';
    }
}

function getInputValue(id) {
    const element = document.getElementById(id);
    return element ? element.value.trim() : '';
}

function formatDate(value) {
    if (!value) {
        return '-';
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
        return '-';
    }

    return date.toLocaleString([], {
        year: 'numeric',
        month: 'short',
        day: 'numeric'
    });
}

function handleError(error) {
    const message = error?.message || 'Unexpected error.';

    if (window.appToast) {
        window.appToast(message);
    } else {
        console.error(message);
    }
}
