const AUTH_STORAGE_KEY = 'ssn.auth';
const OAUTH_BOOTSTRAP_COOKIE_KEY = 'ssn.oauth.auth';

function decodeBase64Url(value) {
    let base64 = value.replace(/-/g, '+').replace(/_/g, '/');

    while (base64.length % 4 !== 0) {
        base64 += '=';
    }

    return atob(base64);
}

function readCookie(name) {
    const escapedName = name.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    const match = document.cookie.match(new RegExp(`(?:^|; )${escapedName}=([^;]*)`));
    return match ? decodeURIComponent(match[1]) : null;
}

function deleteCookie(name) {
    document.cookie = `${name}=; path=/; expires=Thu, 01 Jan 1970 00:00:00 GMT; SameSite=Lax`;
}

function readAuthState() {
    importOAuthBootstrapCookie();

    try {
        const raw = localStorage.getItem(AUTH_STORAGE_KEY);
        return raw ? JSON.parse(raw) : null;
    } catch {
        return null;
    }
}

function writeAuthState(value) {
    if (!value) {
        localStorage.removeItem(AUTH_STORAGE_KEY);
    } else {
        localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(value));
    }

    if (window.syncNavAuthState) {
        window.syncNavAuthState();
    }
}

function importOAuthBootstrapCookie() {
    const encoded = readCookie(OAUTH_BOOTSTRAP_COOKIE_KEY);
    if (!encoded) {
        return;
    }

    try {
        const decoded = decodeBase64Url(encoded);
        const payload = JSON.parse(decoded);

        if (payload?.token) {
            writeAuthState({
                userId: payload.userId,
                username: payload.username,
                email: payload.email,
                token: payload.token,
                expiresAt: payload.expiresAt,
                refreshToken: payload.refreshToken,
                refreshTokenExpiresAt: payload.refreshTokenExpiresAt
            });
        }
    } catch {
        // Ignore malformed bootstrap cookie and clear it.
    } finally {
        deleteCookie(OAUTH_BOOTSTRAP_COOKIE_KEY);
    }
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

async function requestJson(path, method, body, token) {
    const headers = {
        'Content-Type': 'application/json'
    };

    if (token) {
        headers.Authorization = `Bearer ${token}`;
    }

    const response = await fetch(path, {
        method,
        headers,
        body: body ? JSON.stringify(body) : undefined
    });

    return parseApiResponse(response);
}

export function getAccessToken() {
    return readAuthState()?.token ?? null;
}

export function getRefreshToken() {
    return readAuthState()?.refreshToken ?? null;
}

export function getAuthState() {
    return readAuthState();
}

export function clearAuthState() {
    writeAuthState(null);
}

export function setAuthProfile(profile) {
    const current = readAuthState() || {};

    writeAuthState({
        ...current,
        userId: profile?.userId ?? current.userId,
        username: profile?.username ?? current.username,
        email: profile?.email ?? current.email,
        avatarUrl: profile?.avatarUrl ?? current.avatarUrl,
        bio: profile?.bio ?? current.bio,
        accountProvider: profile?.accountProvider ?? current.accountProvider,
        createdAt: profile?.createdAt ?? current.createdAt,
        connectedProviders: profile?.connectedProviders ?? current.connectedProviders,
        token: current.token,
        expiresAt: current.expiresAt,
        refreshToken: current.refreshToken,
        refreshTokenExpiresAt: current.refreshTokenExpiresAt
    });
}

function storeAuthPayload(authPayload) {
    if (!authPayload?.token) {
        throw new Error('Authentication payload is missing token.');
    }

    const existing = readAuthState() || {};

    writeAuthState({
        ...existing,
        userId: authPayload.userId,
        username: authPayload.username,
        email: authPayload.email,
        token: authPayload.token,
        expiresAt: authPayload.expiresAt,
        refreshToken: authPayload.refreshToken,
        refreshTokenExpiresAt: authPayload.refreshTokenExpiresAt
    });

    return authPayload;
}

export async function refreshToken() {
    const refreshTokenValue = getRefreshToken();
    if (!refreshTokenValue) {
        clearAuthState();
        throw new Error('Refresh token not available.');
    }

    const data = await requestJson('/api/auth/refresh-token', 'POST', {
        refreshToken: refreshTokenValue
    });

    return storeAuthPayload(data);
}

export async function logout() {
    const auth = getAuthState();

    try {
        if (auth?.token) {
            await requestJson('/api/auth/logout', 'POST', {
                refreshToken: auth.refreshToken
            }, auth.token);
        }
    } catch {
        // Clear local auth state regardless of backend logout result.
    }

    clearAuthState();
    deleteCookie(OAUTH_BOOTSTRAP_COOKIE_KEY);
}

export async function getMe() {
    const token = getAccessToken();
    if (!token) {
        throw new Error('Not authenticated.');
    }

    return requestJson('/api/auth/me', 'GET', null, token);
}

export async function authorizedFetch(url, options = {}, allowRefresh = true) {
    const token = getAccessToken();
    if (!token) {
        throw new Error('Not authenticated.');
    }

    const headers = new Headers(options.headers || {});
    headers.set('Authorization', `Bearer ${token}`);

    const response = await fetch(url, {
        ...options,
        headers
    });

    if (response.status === 401 && allowRefresh) {
        await refreshToken();
        return authorizedFetch(url, options, false);
    }

    return response;
}

const logoutButton = document.getElementById('nav-logout-button');
if (logoutButton && !logoutButton.dataset.boundLogout) {
    logoutButton.dataset.boundLogout = 'true';
    logoutButton.addEventListener('click', async () => {
        await logout();
        window.location.replace('/auth/login');
    });
}

window.ChatAuth = {
    getAccessToken,
    getRefreshToken,
    getAuthState,
    clearAuthState,
    setAuthProfile,
    refreshToken,
    logout,
    getMe,
    authorizedFetch
};
