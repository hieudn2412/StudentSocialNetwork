import { searchUsers } from './chat-api.js';

const SEARCH_DELAY_MS = 220;

export function initializeUserSearch({
    inputElement,
    resultsElement,
    templateElement,
    onSelectUser
}) {
    if (!inputElement || !resultsElement) {
        return {
            dispose() { }
        };
    }

    let timer = null;
    let requestId = 0;

    const clearResults = () => {
        resultsElement.innerHTML = '';
        resultsElement.classList.add('d-none');
    };

    const showHint = (text, className = 'user-search-hint') => {
        const hint = document.createElement('div');
        hint.className = className;
        hint.textContent = text;
        resultsElement.innerHTML = '';
        resultsElement.appendChild(hint);
        resultsElement.classList.remove('d-none');
    };

    const createResultNode = (user) => {
        let node = null;

        if (templateElement?.content?.firstElementChild) {
            node = templateElement.content.firstElementChild.cloneNode(true);
        } else {
            node = document.createElement('button');
            node.type = 'button';
            node.className = 'user-search-result';
            node.innerHTML = [
                '<span class="user-search-avatar" data-role="avatar"></span>',
                '<span class="user-search-body">',
                '<span class="user-search-name"></span>',
                '<span class="user-search-email"></span>',
                '</span>'
            ].join('');
        }

        node.dataset.userId = String(user.userId || user.id || 0);
        node.dataset.username = user.username || '';
        node.dataset.email = user.email || '';
        node.dataset.avatarUrl = user.avatarUrl || '';

        const avatar = node.querySelector('[data-role="avatar"]');
        if (avatar) {
            setAvatar(avatar, user.avatarUrl, user.username);
        }

        const name = node.querySelector('.user-search-name');
        if (name) {
            name.textContent = user.username || `User #${user.userId}`;
        }

        const email = node.querySelector('.user-search-email');
        if (email) {
            email.textContent = user.email || '';
        }

        return node;
    };

    const renderResults = (users) => {
        resultsElement.innerHTML = '';

        if (!Array.isArray(users) || users.length === 0) {
            showHint('No users found.');
            return;
        }

        for (const user of users) {
            const node = createResultNode(user);
            resultsElement.appendChild(node);
        }

        resultsElement.classList.remove('d-none');
    };

    const runSearch = async () => {
        const keyword = inputElement.value.trim();

        if (!keyword) {
            clearResults();
            return;
        }

        const currentRequest = ++requestId;
        showHint('Searching...', 'user-search-loading');

        try {
            const users = await searchUsers(keyword, 8);
            if (currentRequest !== requestId) {
                return;
            }

            renderResults(users);
        } catch (error) {
            if (currentRequest !== requestId) {
                return;
            }

            showHint('Unable to search users right now.');
            if (window.appToast) {
                window.appToast(error?.message || 'Unable to search users.');
            }
        }
    };

    const onInput = () => {
        if (timer) {
            clearTimeout(timer);
        }

        timer = setTimeout(() => {
            runSearch().catch(() => { });
        }, SEARCH_DELAY_MS);
    };

    const onResultsClick = async (event) => {
        const item = event.target.closest('.user-search-result');
        if (!item) {
            return;
        }

        const userId = Number.parseInt(item.dataset.userId || '', 10);
        if (!Number.isInteger(userId) || userId <= 0) {
            return;
        }

        const user = {
            userId,
            username: item.dataset.username || item.querySelector('.user-search-name')?.textContent || `User #${userId}`,
            email: item.dataset.email || item.querySelector('.user-search-email')?.textContent || '',
            avatarUrl: item.dataset.avatarUrl || ''
        };

        clearResults();
        inputElement.value = '';

        if (typeof onSelectUser === 'function') {
            await onSelectUser(user);
        }
    };

    const onGlobalClick = (event) => {
        if (event.target.closest('.user-search-box')) {
            return;
        }

        clearResults();
    };

    inputElement.addEventListener('input', onInput);
    resultsElement.addEventListener('click', onResultsClick);
    document.addEventListener('click', onGlobalClick);

    return {
        dispose() {
            if (timer) {
                clearTimeout(timer);
            }

            inputElement.removeEventListener('input', onInput);
            resultsElement.removeEventListener('click', onResultsClick);
            document.removeEventListener('click', onGlobalClick);
        }
    };
}

function setAvatar(element, avatarUrl, name) {
    if (!element) {
        return;
    }

    if (avatarUrl) {
        element.innerHTML = `<img src="${escapeHtml(avatarUrl)}" alt="avatar" loading="lazy" />`;
        return;
    }

    const initials = getInitials(name || 'U');
    element.textContent = initials;
}

function getInitials(value) {
    return String(value)
        .split(/\s+/)
        .filter(Boolean)
        .slice(0, 2)
        .map((x) => x[0]?.toUpperCase() || '')
        .join('') || 'U';
}

function escapeHtml(value) {
    return String(value)
        .replaceAll('&', '&amp;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;')
        .replaceAll('"', '&quot;')
        .replaceAll("'", '&#39;');
}


