(function () {
    let notificationPermissionRequested = false;

    function escapeHtml(value) {
        return String(value)
            .replaceAll('&', '&amp;')
            .replaceAll('<', '&lt;')
            .replaceAll('>', '&gt;')
            .replaceAll('"', '&quot;')
            .replaceAll("'", '&#39;');
    }

    function getAuthState() {
        try {
            const raw = localStorage.getItem('ssn.auth');
            return raw ? JSON.parse(raw) : null;
        } catch {
            return null;
        }
    }

    function getInitials(value) {
        const text = String(value || 'U')
            .trim()
            .split(/\s+/)
            .filter(Boolean)
            .slice(0, 2)
            .map((x) => x[0]?.toUpperCase() || '')
            .join('');

        return text || 'U';
    }

    function resolveAvatarUrl(auth) {
        if (auth?.avatarUrl) {
            return auth.avatarUrl;
        }

        const initials = getInitials(auth?.username || auth?.email || 'U');
        const svg = `<svg xmlns='http://www.w3.org/2000/svg' width='96' height='96'><rect width='100%' height='100%' fill='%23e2e8f0'/><text x='50%' y='53%' dominant-baseline='middle' text-anchor='middle' font-family='Segoe UI, Arial' font-size='34' fill='%23334155'>${initials}</text></svg>`;
        return `data:image/svg+xml;charset=UTF-8,${encodeURIComponent(svg)}`;
    }

    function syncNavAuthState() {
        const auth = getAuthState();
        const loginLink = document.getElementById('nav-login-link');
        const authMenu = document.getElementById('nav-auth-menu');
        const profileName = document.getElementById('nav-profile-name');
        const profileAvatar = document.getElementById('nav-profile-avatar');

        if (!loginLink || !authMenu) {
            return;
        }

        const isAuthenticated = !!auth?.token;
        loginLink.classList.toggle('d-none', isAuthenticated);
        authMenu.classList.toggle('d-none', !isAuthenticated);

        if (!isAuthenticated) {
            return;
        }

        if (profileName) {
            profileName.textContent = auth?.username || auth?.email || 'Account';
        }

        if (profileAvatar) {
            profileAvatar.src = resolveAvatarUrl(auth);
        }
    }

    function normalizeNavigationUrl(url) {
        const raw = String(url || '').trim();
        if (!raw) {
            return null;
        }

        if (raw.startsWith('/')) {
            return raw;
        }

        try {
            const parsed = new URL(raw, window.location.origin);
            if (parsed.origin === window.location.origin) {
                return `${parsed.pathname}${parsed.search}${parsed.hash}`;
            }
        } catch {
            return null;
        }

        return null;
    }

    function appToast(message, type = 'danger', delay = 3500, options = {}) {
        const container = document.getElementById('toast-container');
        if (!container) {
            alert(message);
            return;
        }

        const targetUrl = normalizeNavigationUrl(options?.url);

        const toastElement = document.createElement('div');
        toastElement.className = `toast align-items-center text-bg-${type} border-0`;
        toastElement.setAttribute('role', 'alert');
        toastElement.setAttribute('aria-live', 'assertive');
        toastElement.setAttribute('aria-atomic', 'true');

        if (targetUrl) {
            toastElement.classList.add('cursor-pointer');
            toastElement.title = 'Click to open conversation';
        }

        const safeMessage = escapeHtml(message);
        toastElement.innerHTML = [
            '<div class="d-flex">',
            `  <div class="toast-body">${safeMessage}</div>`,
            '  <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>',
            '</div>'
        ].join('');

        container.appendChild(toastElement);

        const toast = bootstrap.Toast.getOrCreateInstance(toastElement, {
            autohide: true,
            delay
        });

        if (targetUrl) {
            toastElement.addEventListener('click', (event) => {
                if (event.target.closest('[data-bs-dismiss="toast"], .btn-close')) {
                    return;
                }

                toast.hide();
                window.location.assign(targetUrl);
            });
        }

        toastElement.addEventListener('hidden.bs.toast', () => toastElement.remove());
        toast.show();
    }

    async function ensureNotificationPermission() {
        if (!('Notification' in window)) {
            return 'unsupported';
        }

        let permission = Notification.permission;

        if (permission === 'default' && !notificationPermissionRequested) {
            notificationPermissionRequested = true;

            try {
                permission = await Notification.requestPermission();
            } catch {
                permission = Notification.permission;
            }
        }

        return permission;
    }

    async function showBrowserNotification({ title, message, tag, url }) {
        const permission = await ensureNotificationPermission();
        if (permission !== 'granted') {
            return;
        }

        const normalizedUrl = normalizeNavigationUrl(url);
        const notification = new Notification(title, {
            body: message,
            icon: '/favicon.ico',
            tag,
            renotify: true
        });

        notification.onclick = () => {
            window.focus();

            if (normalizedUrl) {
                window.location.assign(normalizedUrl);
            }

            notification.close();
        };

        setTimeout(() => {
            notification.close();
        }, 7000);
    }

    function appNotify({ title, message, type = 'primary', delay = 4500, tag, url } = {}) {
        const safeTitle = String(title || 'Notification').trim() || 'Notification';
        const safeMessage = String(message || '').trim();
        const toastText = safeMessage ? `${safeTitle}: ${safeMessage}` : safeTitle;

        appToast(toastText, type, delay, { url });
        void showBrowserNotification({
            title: safeTitle,
            message: safeMessage || safeTitle,
            tag,
            url
        });
    }

    document.addEventListener('DOMContentLoaded', syncNavAuthState);

    window.appToast = appToast;
    window.appNotify = appNotify;
    window.syncNavAuthState = syncNavAuthState;
})();
