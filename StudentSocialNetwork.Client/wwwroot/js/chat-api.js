import { authorizedFetch, getAccessToken } from './auth.js';

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

async function authorizedJson(path, method = 'GET', body = null) {
    const headers = {};
    if (body !== null) {
        headers['Content-Type'] = 'application/json';
    }

    const response = await authorizedFetch(path, {
        method,
        headers,
        body: body !== null ? JSON.stringify(body) : undefined
    });

    return parseApiResponse(response);
}

export function getConversations() {
    return authorizedJson('/api/conversations');
}

export function createPrivateConversation(otherUserId) {
    return authorizedJson('/api/conversations/private', 'POST', { otherUserId });
}

export function createGroupConversation(name, memberIds) {
    return authorizedJson('/api/conversations', 'POST', { name, memberIds, avatarUrl: null });
}

export function addMember(conversationId, userId) {
    return authorizedJson(`/api/conversations/${conversationId}/members`, 'POST', { userId, role: 'Member' });
}

export function removeMember(conversationId, memberUserId) {
    return authorizedJson(`/api/conversations/${conversationId}/members/${memberUserId}`, 'DELETE');
}

export function leaveConversation(conversationId) {
    return authorizedJson(`/api/conversations/${conversationId}/leave`, 'POST');
}

export function searchUsers(query, limit = 8) {
    const trimmed = String(query || '').trim();
    if (!trimmed) {
        return Promise.resolve([]);
    }

    const params = new URLSearchParams();
    params.set('q', trimmed);
    params.set('limit', String(limit));

    return authorizedJson(`/api/users/search?${params.toString()}`);
}

export function getMessages(conversationId, cursor = null, limit = 30) {
    const query = new URLSearchParams();
    if (cursor) {
        query.set('cursor', cursor);
    }

    if (limit) {
        query.set('limit', String(limit));
    }

    const suffix = query.toString() ? `?${query}` : '';
    return authorizedJson(`/api/messages/${conversationId}${suffix}`);
}

export function sendMessage({ conversationId, content, messageType, replyToMessageId, attachments }) {
    return authorizedJson('/api/messages', 'POST', {
        conversationId,
        content,
        messageType: messageType || 'Text',
        replyToMessageId,
        attachments: attachments || []
    });
}

export function setReaction(messageId, reactionType, isRemove) {
    return authorizedJson('/api/messages/reaction', 'POST', {
        messageId,
        reactionType,
        isRemove
    });
}

export function setPin(conversationId, messageId, isPinned) {
    return authorizedJson('/api/messages/pin', 'POST', {
        conversationId,
        messageId,
        isPinned
    });
}

export function getPinnedMessages(conversationId) {
    return authorizedJson(`/api/messages/${conversationId}/pinned`);
}

export function markConversationRead(conversationId) {
    return authorizedJson(`/api/messages/${conversationId}/read`, 'POST');
}

export async function uploadFiles(files) {
    if (!files || files.length === 0) {
        return [];
    }

    const token = getAccessToken();
    if (!token) {
        throw new Error('Not authenticated.');
    }

    const formData = new FormData();
    files.forEach((file) => formData.append('files', file));

    const response = await fetch('/api/files/upload', {
        method: 'POST',
        headers: {
            Authorization: `Bearer ${token}`
        },
        body: formData
    });

    return parseApiResponse(response);
}
