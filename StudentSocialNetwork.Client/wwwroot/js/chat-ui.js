import {
    getAccessToken,
    getMe,
    setAuthProfile
} from './auth.js';
import {
    getConversations,
    createPrivateConversation,
    createGroupConversation,
    addMember,
    removeMember,
    leaveConversation,
    getMessages,
    sendMessage,
    setReaction,
    setPin,
    getPinnedMessages,
    markConversationRead,
    uploadFiles
} from './chat-api.js';
import { ChatRealtimeClient } from './chat-realtime.js';
import { initializeUserSearch } from './user-search.js';

const REACTION_EMOJI_MAP = {
    Like: '👍',
    Love: '❤️',
    Haha: '😂',
    Wow: '😮',
    Sad: '😢',
    Angry: '😡'
};

const state = {
    me: null,
    conversations: [],
    currentConversationId: null,
    messages: [],
    nextCursor: null,
    pinnedMessages: [],
    replyToMessageId: null,
    pendingAttachments: [],
    typingUsers: new Map(),
    typingExpiryTimers: new Map(),
    typingStarted: false,
    typingTimer: null,
    realtime: null,
    conversationFilter: '',
    unreadByConversation: new Map(),
    isLoadingOlder: false,
    newMessageCount: 0,
    markReadTimer: null,
    openRequestId: 0,
    userSearchController: null,
    groupMemberSearchController: null,
    selectedGroupMembers: new Map(),
    joinedRealtimeConversationIds: new Set()
};

const dom = {};

document.addEventListener('DOMContentLoaded', () => {
    initialize().catch(handleError);
});

async function initialize() {
    dom.root = document.getElementById('chat-app');
    if (!dom.root) {
        return;
    }

    if (!getAccessToken()) {
        window.location.replace('/auth/login');
        return;
    }

    bindDom();
    bindEvents();

    state.me = await getMe();
    setAuthProfile(state.me);
    state.conversations = await getConversations();
    hydrateUnreadCounts();
    renderConversations();

    state.userSearchController?.dispose?.();
    state.userSearchController = initializeUserSearch({
        inputElement: dom.userSearchInput,
        resultsElement: dom.userSearchResults,
        templateElement: dom.userSearchResultTemplate,
        onSelectUser: async (user) => {
            await openOrCreatePrivateConversation(user?.userId);
        }
    });

    state.groupMemberSearchController?.dispose?.();
    state.groupMemberSearchController = initializeUserSearch({
        inputElement: dom.groupMemberSearchInput,
        resultsElement: dom.groupMemberSearchResults,
        templateElement: dom.userSearchResultTemplate,
        onSelectUser: async (user) => {
            addSelectedGroupMember(user);
        }
    });
    renderSelectedGroupMembers();

    await setupRealtime();
    await syncRealtimeConversationSubscriptions();

    const initialConversationIdValue = dom.root.dataset.initialConversationId;
    const initialConversationId = Number.parseInt(initialConversationIdValue, 10);

    if (Number.isInteger(initialConversationId) && initialConversationId > 0) {
        await openConversation(initialConversationId);
    } else if (state.conversations.length > 0) {
        await openConversation(state.conversations[0].id);
    } else {
        clearConversationView();
    }
}

function bindDom() {
    dom.conversationList = document.getElementById('conversation-list');
    dom.userSearchInput = document.getElementById('user-search-input');
    dom.userSearchResults = document.getElementById('user-search-results');
    dom.userSearchResultTemplate = document.getElementById('user-search-result-template');
    dom.conversationSearchInput = document.getElementById('conversation-search-input');
    dom.messageScrollRegion = document.getElementById('message-scroll-region');
    dom.messageList = document.getElementById('message-list');
    dom.olderLoadingIndicator = document.getElementById('older-loading-indicator');
    dom.newMessagesIndicator = document.getElementById('new-messages-indicator');
    dom.pinnedList = document.getElementById('pinned-message-list');

    dom.activeConversationName = document.getElementById('active-conversation-name');
    dom.activeConversationMeta = document.getElementById('active-conversation-meta');
    dom.activeConversationAvatar = document.getElementById('active-conversation-avatar');

    dom.infoConversationName = document.getElementById('info-conversation-name');
    dom.infoConversationType = document.getElementById('info-conversation-type');
    dom.infoConversationAvatar = document.getElementById('info-conversation-avatar');
    dom.infoConversationCreatedAt = document.getElementById('info-conversation-created-at');
    dom.memberCountBadge = document.getElementById('member-count-badge');
    dom.memberList = document.getElementById('member-list');
    dom.memberManagementPanel = document.getElementById('member-management-panel');

    dom.typingIndicator = document.getElementById('typing-indicator');
    dom.typingIndicatorText = document.getElementById('typing-indicator-text');

    dom.messageForm = document.getElementById('message-form');
    dom.messageInput = document.getElementById('message-input');
    dom.messageFiles = document.getElementById('message-files');
    dom.pendingAttachments = document.getElementById('pending-attachments');
    dom.composerAttachButton = document.getElementById('composer-attach-button');
    dom.composerEmojiButton = document.getElementById('composer-emoji-button');
    dom.composerEmojiPicker = document.getElementById('composer-emoji-picker');

    dom.replyBanner = document.getElementById('reply-banner');
    dom.replyPreview = document.getElementById('reply-preview');
    dom.clearReplyButton = document.getElementById('clear-reply-btn');

    dom.createGroupForm = document.getElementById('create-group-form');
    dom.groupName = document.getElementById('group-name');
    dom.groupMemberSearchInput = document.getElementById('group-member-search-input');
    dom.groupMemberSearchResults = document.getElementById('group-member-search-results');
    dom.groupMemberSelected = document.getElementById('group-member-selected');

    dom.addMemberForm = document.getElementById('add-member-form');
    dom.addMemberUserId = document.getElementById('add-member-user-id');
    dom.removeMemberForm = document.getElementById('remove-member-form');
    dom.removeMemberUserId = document.getElementById('remove-member-user-id');
    dom.leaveConversationButton = document.getElementById('leave-conversation-btn');

    dom.conversationTemplate = document.getElementById('conversation-item-template');
    dom.messageTemplate = document.getElementById('message-bubble-template');
}

function bindEvents() {
    dom.conversationList?.addEventListener('click', async (event) => {
        const button = event.target.closest('.conversation-item');
        if (!button) {
            return;
        }

        const conversationId = Number.parseInt(button.dataset.conversationId, 10);
        if (!Number.isInteger(conversationId) || conversationId <= 0) {
            return;
        }

        await openConversation(conversationId);
    });

    dom.conversationSearchInput?.addEventListener('input', () => {
        state.conversationFilter = (dom.conversationSearchInput?.value || '').trim().toLowerCase();
        renderConversations();
    });

    dom.messageForm?.addEventListener('submit', async (event) => {
        event.preventDefault();
        await sendCurrentMessage();
    });

    dom.messageInput?.addEventListener('input', () => {
        autoResizeComposer();
        triggerTyping();
    });

    dom.messageInput?.addEventListener('keydown', async (event) => {
        if (event.key === 'Enter' && !event.shiftKey) {
            event.preventDefault();
            await sendCurrentMessage();
        }
    });

    dom.messageInput?.addEventListener('blur', () => {
        stopTyping().catch(() => { });
    });

    dom.composerAttachButton?.addEventListener('click', () => {
        dom.messageFiles?.click();
    });

    dom.messageFiles?.addEventListener('change', async () => {
        const files = Array.from(dom.messageFiles.files || []);
        if (!files.length) {
            return;
        }

        try {
            const uploaded = await uploadFiles(files);
            state.pendingAttachments.push(...uploaded);
            renderPendingAttachments();
            dom.messageFiles.value = '';
        } catch (error) {
            handleError(error);
        }
    });

    dom.pendingAttachments?.addEventListener('click', (event) => {
        const button = event.target.closest('[data-remove-attachment-index]');
        if (!button) {
            return;
        }

        const index = Number.parseInt(button.dataset.removeAttachmentIndex, 10);
        if (Number.isInteger(index)) {
            state.pendingAttachments.splice(index, 1);
            renderPendingAttachments();
        }
    });

    dom.composerEmojiButton?.addEventListener('click', (event) => {
        event.preventDefault();
        dom.composerEmojiPicker?.classList.toggle('d-none');
    });

    dom.composerEmojiPicker?.addEventListener('click', (event) => {
        const emojiButton = event.target.closest('[data-emoji]');
        if (!emojiButton || !dom.messageInput) {
            return;
        }

        insertEmojiAtCursor(emojiButton.dataset.emoji || '');
    });

    dom.clearReplyButton?.addEventListener('click', () => {
        clearReply();
    });

    dom.newMessagesIndicator?.addEventListener('click', () => {
        scrollMessagesToBottom(true);
        hideNewMessagesIndicator();
        scheduleMarkRead(120);
    });

    dom.messageScrollRegion?.addEventListener('scroll', () => {
        if (isNearBottom()) {
            hideNewMessagesIndicator();
            scheduleMarkRead(200);
        }

        maybeLoadOlderMessages().catch(handleError);
    });

    dom.messageList?.addEventListener('click', async (event) => {
        const messageItem = event.target.closest('.message-item');
        const messageId = Number.parseInt(messageItem?.dataset.messageId || '', 10);

        if (!Number.isInteger(messageId)) {
            return;
        }

        const actionButton = event.target.closest('[data-action]');
        if (actionButton) {
            const action = actionButton.dataset.action;

            if (action === 'reply') {
                const message = state.messages.find((x) => x.id === messageId);
                if (message) {
                    state.replyToMessageId = message.id;
                    dom.replyPreview.textContent = `${message.senderUsername}: ${truncate(message.content, 64)}`;
                    dom.replyBanner?.classList.remove('d-none');
                    dom.messageInput?.focus();
                }
                return;
            }

            if (action === 'react') {
                toggleReactionPicker(messageItem);
                return;
            }

            if (action === 'pin') {
                await togglePin(messageId);
                return;
            }
        }

        const reactionPickerButton = event.target.closest('[data-reaction-type]');
        if (reactionPickerButton) {
            const reactionType = reactionPickerButton.dataset.reactionType;
            if (reactionType) {
                await applyReaction(messageId, reactionType);
                hideAllReactionPickers();
            }
            return;
        }

        const reactionChip = event.target.closest('[data-reaction-chip]');
        if (reactionChip) {
            const reactionType = reactionChip.dataset.reactionType;
            if (reactionType) {
                await applyReaction(messageId, reactionType);
            }
        }
    });

    dom.pinnedList?.addEventListener('click', async (event) => {
        const unpinButton = event.target.closest('[data-action="unpin"]');
        if (unpinButton && state.currentConversationId) {
            const messageId = Number.parseInt(unpinButton.dataset.messageId || '', 10);
            if (Number.isInteger(messageId)) {
                await setPin(state.currentConversationId, messageId, false);
                state.pinnedMessages = state.pinnedMessages.filter((x) => x.messageId !== messageId);
                renderPinnedMessages();
                renderMessages();
            }
            return;
        }

        const jumpButton = event.target.closest('[data-action="jump-message"]');
        if (jumpButton) {
            const messageId = Number.parseInt(jumpButton.dataset.messageId || '', 10);
            if (Number.isInteger(messageId)) {
                jumpToMessage(messageId);
            }
        }
    });


    dom.createGroupForm?.addEventListener('submit', async (event) => {
        event.preventDefault();

        const name = dom.groupName?.value?.trim();
        if (!name) {
            window.appToast('Group name is required.');
            return;
        }

        const memberIds = Array.from(state.selectedGroupMembers.keys());
        if (memberIds.length === 0) {
            window.appToast('Please select at least one member.');
            return;
        }

        const conversation = await createGroupConversation(name, memberIds);
        dom.groupName.value = '';
        clearSelectedGroupMembers();

        await refreshConversations(conversation.id);
    });
    dom.groupMemberSelected?.addEventListener('click', (event) => {
        const removeButton = event.target.closest('[data-action="remove-group-member"]');
        if (!removeButton) {
            return;
        }

        const userId = Number.parseInt(removeButton.dataset.userId || '', 10);
        if (!Number.isInteger(userId) || userId <= 0) {
            return;
        }

        state.selectedGroupMembers.delete(userId);
        renderSelectedGroupMembers();
    });

    dom.addMemberForm?.addEventListener('submit', async (event) => {
        event.preventDefault();

        if (!state.currentConversationId) {
            window.appToast('Select a conversation first.');
            return;
        }

        const userId = Number.parseInt(dom.addMemberUserId?.value || '', 10);
        if (!Number.isInteger(userId) || userId <= 0) {
            window.appToast('Enter a valid user id to add.');
            return;
        }

        await addMember(state.currentConversationId, userId);
        dom.addMemberUserId.value = '';
        await refreshConversations(state.currentConversationId);
    });

    dom.removeMemberForm?.addEventListener('submit', async (event) => {
        event.preventDefault();

        if (!state.currentConversationId) {
            window.appToast('Select a conversation first.');
            return;
        }

        const userId = Number.parseInt(dom.removeMemberUserId?.value || '', 10);
        if (!Number.isInteger(userId) || userId <= 0) {
            window.appToast('Enter a valid user id to remove.');
            return;
        }

        await removeMember(state.currentConversationId, userId);
        dom.removeMemberUserId.value = '';
        await refreshConversations(state.currentConversationId);
    });

    dom.leaveConversationButton?.addEventListener('click', async () => {
        if (!state.currentConversationId) {
            window.appToast('Select a conversation first.');
            return;
        }

        const previousConversationId = state.currentConversationId;

        await leaveConversation(previousConversationId);

        state.currentConversationId = null;
        if (state.realtime) {
            await state.realtime.leaveConversation(previousConversationId);
        }

        await refreshConversations(null);
        clearConversationView();
    });

    document.addEventListener('click', (event) => {
        const clickedEmojiButton = event.target.closest('#composer-emoji-button');
        const clickedEmojiPicker = event.target.closest('#composer-emoji-picker');

        if (!clickedEmojiButton && !clickedEmojiPicker) {
            dom.composerEmojiPicker?.classList.add('d-none');
        }

        const clickedMessageAction = event.target.closest('.message-actions');
        if (!clickedMessageAction) {
            hideAllReactionPickers();
        }
    });
}

async function setupRealtime() {
    const backendBaseUrl = dom.root.dataset.backendBaseUrl;
    const hubPath = dom.root.dataset.hubEndpoint;

    state.realtime = new ChatRealtimeClient({
        backendBaseUrl,
        hubPath
    });

    state.realtime.addEventListener('newMessage', (event) => {
        onNewMessage(event.detail);
    });

    state.realtime.addEventListener('connectionReconnected', async () => {
        try {
            state.joinedRealtimeConversationIds.clear();
            await syncRealtimeConversationSubscriptions();
        } catch {
            // Non-blocking. We will retry on the next conversation refresh.
        }
    });

    state.realtime.addEventListener('connectionFailed', (event) => {
        const detail = event.detail;
        const message = detail?.message || 'Realtime connection failed. Please refresh the page.';
        if (window.appToast) {
            window.appToast(message);
        }
    });
    state.realtime.addEventListener('messageReactionAdded', (event) => {
        onReactionAdded(event.detail);
    });

    state.realtime.addEventListener('messageReactionRemoved', (event) => {
        onReactionRemoved(event.detail);
    });

    state.realtime.addEventListener('messagePinned', (event) => {
        onPinnedAdded(event.detail);
    });

    state.realtime.addEventListener('messageUnpinned', (event) => {
        onPinnedRemoved(event.detail);
    });

    state.realtime.addEventListener('typingIndicator', (event) => {
        onTypingIndicator(event.detail);
    });

    await state.realtime.connect();
}

function hydrateUnreadCounts() {
    state.unreadByConversation.clear();

    for (const conversation of state.conversations) {
        const unread = Number.parseInt(conversation?.unreadCount, 10);
        state.unreadByConversation.set(conversation.id, Number.isInteger(unread) && unread > 0 ? unread : 0);
    }
}

async function openOrCreatePrivateConversation(otherUserId) {
    const parsedUserId = Number.parseInt(String(otherUserId || ''), 10);
    if (!Number.isInteger(parsedUserId) || parsedUserId <= 0) {
        window.appToast?.('Please select a valid user.');
        return;
    }

    const existing = findPrivateConversationByUserId(parsedUserId);
    if (existing) {
        await openConversation(existing.id);
        return;
    }

    const createdOrExisting = await createPrivateConversation(parsedUserId);
    await refreshConversations(createdOrExisting.id);
}

function findPrivateConversationByUserId(otherUserId) {
    const meId = getCurrentUserId();

    return state.conversations.find((conversation) => {
        if (String(conversation?.type || '').toLowerCase() !== 'private') {
            return false;
        }

        const members = Array.isArray(conversation.members) ? conversation.members : [];
        if (members.length !== 2) {
            return false;
        }

        const hasMe = members.some((member) => member.userId === meId);
        const hasOther = members.some((member) => member.userId === otherUserId);

        return hasMe && hasOther;
    }) || null;
}

async function syncRealtimeConversationSubscriptions() {
    if (!state.realtime) {
        return;
    }

    const desiredConversationIds = new Set(
        state.conversations
            .map((conversation) => Number.parseInt(String(conversation?.id || ''), 10))
            .filter((conversationId) => Number.isInteger(conversationId) && conversationId > 0)
    );

    for (const conversationId of Array.from(state.joinedRealtimeConversationIds)) {
        if (desiredConversationIds.has(conversationId)) {
            continue;
        }

        await state.realtime.leaveConversation(conversationId);
        state.joinedRealtimeConversationIds.delete(conversationId);
    }

    for (const conversationId of desiredConversationIds) {
        if (state.joinedRealtimeConversationIds.has(conversationId)) {
            continue;
        }

        await state.realtime.joinConversation(conversationId);
        state.joinedRealtimeConversationIds.add(conversationId);
    }
}

function addSelectedGroupMember(user) {
    const userId = Number.parseInt(String(user?.userId || user?.id || ''), 10);
    if (!Number.isInteger(userId) || userId <= 0) {
        return;
    }

    if (userId === getCurrentUserId()) {
        window.appToast?.('You are already included as the group creator.');
        return;
    }

    if (state.selectedGroupMembers.has(userId)) {
        return;
    }

    state.selectedGroupMembers.set(userId, {
        userId,
        username: user?.username || `User #${userId}`,
        email: user?.email || '',
        avatarUrl: user?.avatarUrl || null
    });

    renderSelectedGroupMembers();
}

function clearSelectedGroupMembers() {
    state.selectedGroupMembers.clear();

    if (dom.groupMemberSearchInput) {
        dom.groupMemberSearchInput.value = '';
    }

    renderSelectedGroupMembers();
}

function renderSelectedGroupMembers() {
    if (!dom.groupMemberSelected) {
        return;
    }

    dom.groupMemberSelected.innerHTML = '';

    if (state.selectedGroupMembers.size === 0) {
        dom.groupMemberSelected.innerHTML = '<div class="group-member-selected-empty">No members selected yet.</div>';
        return;
    }

    for (const member of state.selectedGroupMembers.values()) {
        const chip = document.createElement('div');
        chip.className = 'group-member-chip';

        const avatar = document.createElement('span');
        avatar.className = 'group-member-chip-avatar avatar';
        setAvatarElement(avatar, member.avatarUrl, getInitials(member.username));

        const text = document.createElement('span');
        text.className = 'group-member-chip-text';
        text.innerHTML = `<span class="group-member-chip-name">${escapeHtml(member.username)}</span><span class="group-member-chip-email">${escapeHtml(member.email)}</span>`;

        const removeButton = document.createElement('button');
        removeButton.type = 'button';
        removeButton.className = 'group-member-chip-remove';
        removeButton.dataset.action = 'remove-group-member';
        removeButton.dataset.userId = String(member.userId);
        removeButton.setAttribute('aria-label', `Remove ${member.username}`);
        removeButton.textContent = '×';

        chip.appendChild(avatar);
        chip.appendChild(text);
        chip.appendChild(removeButton);

        dom.groupMemberSelected.appendChild(chip);
    }
}
async function refreshConversations(conversationToOpen = null) {
    state.conversations = await getConversations();
    await syncRealtimeConversationSubscriptions();

    for (const conversation of state.conversations) {
        const serverUnread = Number.parseInt(conversation?.unreadCount, 10);
        const localUnread = state.unreadByConversation.get(conversation.id) || 0;

        if (conversation.id === state.currentConversationId) {
            state.unreadByConversation.set(conversation.id, 0);
            continue;
        }

        if (Number.isInteger(serverUnread) && serverUnread >= 0) {
            state.unreadByConversation.set(conversation.id, Math.max(localUnread, serverUnread));
            continue;
        }

        if (!state.unreadByConversation.has(conversation.id)) {
            state.unreadByConversation.set(conversation.id, 0);
        }
    }
    renderConversations();
    renderConversationInfo();

    if (Number.isInteger(conversationToOpen) && conversationToOpen > 0) {
        await openConversation(conversationToOpen);
    }
}

async function openConversation(conversationId) {
    if (state.currentConversationId === conversationId) {
        return;
    }

    const requestId = ++state.openRequestId;

    state.currentConversationId = conversationId;
    state.messages = [];
    state.nextCursor = null;
    state.pinnedMessages = [];
    state.replyToMessageId = null;
    state.newMessageCount = 0;
    clearTypingState();

    hideNewMessagesIndicator();
    clearReply();
    renderMessages();
    renderPinnedMessages();
    renderConversations();
    renderConversationInfo();

    await syncRealtimeConversationSubscriptions();

    await Promise.all([
        loadMessages(null),
        loadPinnedMessages(conversationId)
    ]);

    if (requestId !== state.openRequestId) {
        return;
    }

    setUnreadCount(conversationId, 0);
    renderConversations();
    renderConversationInfo();
    scheduleMarkRead(80);
}

async function loadMessages(cursor, options = {}) {
    if (!state.currentConversationId) {
        return;
    }

    const page = await getMessages(state.currentConversationId, cursor, 30);
    const incoming = page.items || [];

    for (const message of incoming) {
        upsertMessage(message);
    }

    state.nextCursor = page.nextCursor || null;
    renderMessages();

    if (!options.skipAutoScroll && !cursor) {
        scrollMessagesToBottom(true);
    }
}

async function maybeLoadOlderMessages() {
    if (state.isLoadingOlder || !state.currentConversationId || !state.nextCursor || !dom.messageScrollRegion) {
        return;
    }

    if (dom.messageScrollRegion.scrollTop > 64) {
        return;
    }

    state.isLoadingOlder = true;
    dom.olderLoadingIndicator?.classList.remove('d-none');

    const beforeHeight = dom.messageScrollRegion.scrollHeight;
    const beforeTop = dom.messageScrollRegion.scrollTop;

    try {
        await loadMessages(state.nextCursor, { skipAutoScroll: true });

        const afterHeight = dom.messageScrollRegion.scrollHeight;
        dom.messageScrollRegion.scrollTop = beforeTop + (afterHeight - beforeHeight);
    } finally {
        state.isLoadingOlder = false;
        dom.olderLoadingIndicator?.classList.add('d-none');
    }
}

async function loadPinnedMessages(conversationId) {
    state.pinnedMessages = await getPinnedMessages(conversationId);
    renderPinnedMessages();
}

function clearConversationView() {
    state.messages = [];
    state.pinnedMessages = [];
    state.replyToMessageId = null;
    state.nextCursor = null;
    clearTypingState();
    clearReply();
    hideNewMessagesIndicator();

    if (dom.messageList) {
        dom.messageList.innerHTML = '<div class="empty-state">Select a conversation to start chatting.</div>';
    }

    if (dom.pinnedList) {
        dom.pinnedList.innerHTML = '<div class="empty-state">No pinned messages.</div>';
    }

    if (dom.memberList) {
        dom.memberList.innerHTML = '<div class="empty-state">No members to display.</div>';
    }

    if (dom.activeConversationName) {
        dom.activeConversationName.textContent = 'Select a conversation';
    }

    if (dom.activeConversationMeta) {
        dom.activeConversationMeta.textContent = '';
    }

    setAvatarElement(dom.activeConversationAvatar, null, 'C');
    renderConversationInfo();
}
function renderConversations() {
    if (!dom.conversationList || !dom.conversationTemplate) {
        return;
    }

    dom.conversationList.innerHTML = '';

    if (!state.conversations.length) {
        dom.conversationList.innerHTML = '<div class="empty-state">No conversations yet.</div>';
        return;
    }

    const sorted = [...state.conversations].sort((a, b) => {
        const aTime = new Date(a.lastMessageAt || a.createdAt).getTime();
        const bTime = new Date(b.lastMessageAt || b.createdAt).getTime();
        return bTime - aTime;
    });

    const filtered = sorted.filter((conversation) => {
        if (!state.conversationFilter) {
            return true;
        }

        const display = getConversationDisplay(conversation);
        const haystack = `${display.name} ${conversation.lastMessagePreview || ''}`.toLowerCase();
        return haystack.includes(state.conversationFilter);
    });

    if (!filtered.length) {
        dom.conversationList.innerHTML = '<div class="empty-state">No conversations matched your search.</div>';
        return;
    }

    for (const conversation of filtered) {
        const node = dom.conversationTemplate.content.firstElementChild.cloneNode(true);
        const display = getConversationDisplay(conversation);

        node.dataset.conversationId = String(conversation.id);
        node.classList.toggle('active', conversation.id === state.currentConversationId);

        const unread = getUnreadCount(conversation.id);
        node.classList.toggle('has-unread', unread > 0);

        const nameElement = node.querySelector('.conversation-name');
        const previewElement = node.querySelector('.conversation-preview');
        const unreadBadge = node.querySelector('.unread-badge');

        nameElement.textContent = display.name;
        previewElement.textContent = truncate(conversation.lastMessagePreview || 'No messages yet.', 48);
        nameElement.classList.toggle('conversation-unread', unread > 0);
        previewElement.classList.toggle('conversation-unread', unread > 0);
        previewElement.classList.toggle('text-muted', unread <= 0);

        const timeValue = conversation.lastMessageAt || conversation.createdAt;
        const timeElement = node.querySelector('.conversation-time');
        timeElement.textContent = formatConversationTime(timeValue);
        timeElement.setAttribute('datetime', timeValue || '');

        setAvatarElement(node.querySelector('.conversation-item-avatar'), display.avatarUrl, getInitials(display.name));

        unreadBadge.classList.toggle('d-none', unread <= 0);
        unreadBadge.textContent = unread > 0 ? (unread > 99 ? '99+' : String(unread)) : '';

        dom.conversationList.appendChild(node);
    }
}

function renderConversationInfo() {
    const conversation = state.conversations.find((x) => x.id === state.currentConversationId);

    if (!conversation) {
        dom.infoConversationName.textContent = 'No conversation selected';
        dom.infoConversationType.textContent = '-';
        dom.infoConversationCreatedAt.textContent = '-';
        dom.memberCountBadge.textContent = '0';
        dom.memberList.innerHTML = '<div class="empty-state">No members to display.</div>';
        dom.memberManagementPanel?.classList.add('d-none');
        setAvatarElement(dom.infoConversationAvatar, null, 'C');
        return;
    }

    const display = getConversationDisplay(conversation);
    const memberCount = Array.isArray(conversation.members) ? conversation.members.length : 0;

    dom.activeConversationName.textContent = display.name;
    dom.activeConversationMeta.textContent = `${conversation.type} · ${memberCount} member(s)`;
    setAvatarElement(dom.activeConversationAvatar, display.avatarUrl, getInitials(display.name));

    dom.infoConversationName.textContent = display.name;
    dom.infoConversationType.textContent = `${conversation.type} conversation`;
    dom.infoConversationCreatedAt.textContent = formatLongTime(conversation.createdAt);
    dom.memberCountBadge.textContent = String(memberCount);
    setAvatarElement(dom.infoConversationAvatar, display.avatarUrl, getInitials(display.name));

    renderMemberList(conversation);
    dom.memberManagementPanel?.classList.toggle('d-none', !isGroupConversation(conversation));
}

function renderMemberList(conversation) {
    if (!dom.memberList) {
        return;
    }

    const members = Array.isArray(conversation?.members) ? conversation.members : [];
    dom.memberList.innerHTML = '';

    if (!members.length) {
        dom.memberList.innerHTML = '<div class="empty-state">No members.</div>';
        return;
    }

    for (const member of members) {
        const item = document.createElement('div');
        item.className = 'member-item';

        const left = document.createElement('div');
        left.className = 'd-flex align-items-center gap-2 min-w-0';

        const avatar = document.createElement('div');
        avatar.className = 'avatar';
        setAvatarElement(avatar, member.avatarUrl, getInitials(member.username));

        const text = document.createElement('div');
        text.className = 'min-w-0';
        text.innerHTML = `<div class="member-item-name text-truncate">${escapeHtml(member.username)}</div><div class="member-item-role">${escapeHtml(member.role || 'Member')}</div>`;

        left.appendChild(avatar);
        left.appendChild(text);

        const badge = document.createElement('span');
        badge.className = 'badge rounded-pill text-bg-light border';
        badge.textContent = member.userId === getCurrentUserId() ? 'You' : `#${member.userId}`;

        item.appendChild(left);
        item.appendChild(badge);

        dom.memberList.appendChild(item);
    }
}

function renderMessages() {
    if (!dom.messageList || !dom.messageTemplate) {
        return;
    }

    dom.messageList.innerHTML = '';

    const ordered = getOrderedMessages();
    if (!ordered.length) {
        dom.messageList.innerHTML = '<div class="empty-state">No messages yet.</div>';
        return;
    }

    const messageMap = new Map(ordered.map((x) => [x.id, x]));

    ordered.forEach((message, index) => {
        const previous = index > 0 ? ordered[index - 1] : null;
        const next = index < ordered.length - 1 ? ordered[index + 1] : null;

        const isMine = message.senderId === getCurrentUserId();
        const isGroupStart = !previous || previous.senderId !== message.senderId;
        const isGroupEnd = !next || next.senderId !== message.senderId;

        const node = dom.messageTemplate.content.firstElementChild.cloneNode(true);
        node.dataset.messageId = String(message.id);
        node.classList.toggle('is-mine', isMine);
        node.classList.toggle('is-group-start', isGroupStart);
        node.classList.toggle('is-group-end', isGroupEnd);

        const senderProfile = resolveUserProfile(message.senderId, message.senderUsername);
        const avatarWrap = node.querySelector('.message-avatar-wrap');
        const avatarElement = node.querySelector('[data-role="avatar"]');
        setAvatarElement(avatarElement, senderProfile.avatarUrl, getInitials(senderProfile.username));
        avatarWrap.classList.toggle('invisible', isMine || !isGroupEnd);

        const senderElement = node.querySelector('.message-sender');
        senderElement.textContent = senderProfile.username;
        senderElement.classList.toggle('d-none', isMine || !isGroupStart);

        const timeElement = node.querySelector('.message-time');
        timeElement.textContent = formatTime(message.createdAt);
        timeElement.classList.toggle('d-none', !isGroupEnd);

        const replyElement = node.querySelector('.message-reply');
        if (message.replyToMessageId) {
            const replyTo = messageMap.get(message.replyToMessageId);
            const preview = replyTo
                ? `${replyTo.senderUsername}: ${truncate(replyTo.content, 72)}`
                : `Replying to message #${message.replyToMessageId}`;

            replyElement.classList.remove('d-none');
            replyElement.textContent = preview;
        } else {
            replyElement.classList.add('d-none');
            replyElement.textContent = '';
        }

        const bubbleElement = node.querySelector('.message-bubble');
        bubbleElement.textContent = message.content || (message.attachments?.length ? '[Attachment]' : '');
        bubbleElement.classList.toggle('d-none', !message.content && (!message.attachments || message.attachments.length === 0));

        const attachmentsContainer = node.querySelector('.message-attachments');
        attachmentsContainer.innerHTML = '';
        for (const attachment of message.attachments || []) {
            const link = document.createElement('a');
            link.className = 'attachment-link';
            link.target = '_blank';
            link.rel = 'noopener';
            link.href = attachment.fileUrl;
            link.textContent = `${attachment.fileName} (${formatFileSize(attachment.fileSize)})`;
            attachmentsContainer.appendChild(link);
        }
        attachmentsContainer.classList.toggle('d-none', (message.attachments || []).length === 0);

        const reactionsContainer = node.querySelector('.message-reactions');
        renderMessageReactions(reactionsContainer, message);

        const pinButton = node.querySelector('[data-action="pin"]');
        pinButton.textContent = isPinnedMessage(message.id) ? 'Unpin' : 'Pin';

        dom.messageList.appendChild(node);
    });
}

function renderMessageReactions(container, message) {
    container.innerHTML = '';

    const grouped = buildReactionStats(message.reactions || []);
    if (!grouped.length) {
        container.classList.add('d-none');
        return;
    }

    container.classList.remove('d-none');

    for (const reaction of grouped) {
        const chip = document.createElement('button');
        chip.type = 'button';
        chip.className = 'message-reaction-chip';
        chip.dataset.reactionChip = 'true';
        chip.dataset.reactionType = reaction.reactionType;
        chip.dataset.messageId = String(message.id);
        chip.classList.toggle('is-mine', reaction.hasMine);
        chip.title = reaction.usernames.join(', ');
        chip.textContent = `${reaction.emoji} ${reaction.count}`;

        container.appendChild(chip);
    }
}

function renderPinnedMessages() {
    if (!dom.pinnedList) {
        return;
    }

    dom.pinnedList.innerHTML = '';

    if (!state.pinnedMessages.length) {
        dom.pinnedList.innerHTML = '<div class="empty-state">No pinned messages.</div>';
        return;
    }

    for (const pinned of state.pinnedMessages) {
        const container = document.createElement('article');
        container.className = 'pinned-item';

        const text = document.createElement('div');
        text.className = 'pinned-item-text';
        text.textContent = truncate(pinned.message?.content || `Message #${pinned.messageId}`, 96);

        const meta = document.createElement('div');
        meta.className = 'pinned-item-meta mt-1';
        meta.textContent = `Pinned by ${pinned.pinnedByUsername || `user-${pinned.pinnedBy}`} · ${formatTime(pinned.pinnedAt)}`;

        const actions = document.createElement('div');
        actions.className = 'd-flex gap-2 mt-2';

        const jumpButton = document.createElement('button');
        jumpButton.type = 'button';
        jumpButton.className = 'btn btn-outline-primary btn-sm';
        jumpButton.dataset.action = 'jump-message';
        jumpButton.dataset.messageId = String(pinned.messageId);
        jumpButton.textContent = 'Jump';

        const unpinButton = document.createElement('button');
        unpinButton.type = 'button';
        unpinButton.className = 'btn btn-outline-secondary btn-sm';
        unpinButton.dataset.action = 'unpin';
        unpinButton.dataset.messageId = String(pinned.messageId);
        unpinButton.textContent = 'Unpin';

        actions.appendChild(jumpButton);
        actions.appendChild(unpinButton);

        container.appendChild(text);
        container.appendChild(meta);
        container.appendChild(actions);

        dom.pinnedList.appendChild(container);
    }
}

function renderPendingAttachments() {
    if (!dom.pendingAttachments) {
        return;
    }

    dom.pendingAttachments.innerHTML = '';

    state.pendingAttachments.forEach((attachment, index) => {
        const chip = document.createElement('span');
        chip.className = 'attachment-chip';

        const text = document.createElement('span');
        text.textContent = truncate(attachment.fileName, 32);

        const removeButton = document.createElement('button');
        removeButton.type = 'button';
        removeButton.className = 'btn btn-sm btn-link p-0';
        removeButton.dataset.removeAttachmentIndex = String(index);
        removeButton.textContent = '×';

        chip.appendChild(text);
        chip.appendChild(removeButton);
        dom.pendingAttachments.appendChild(chip);
    });
}

function renderTypingIndicator() {
    if (!dom.typingIndicator || !dom.typingIndicatorText) {
        return;
    }

    const usernames = Array.from(state.typingUsers.values());
    if (!usernames.length) {
        dom.typingIndicator.classList.add('d-none');
        dom.typingIndicatorText.textContent = '';
        return;
    }

    dom.typingIndicator.classList.remove('d-none');

    if (usernames.length === 1) {
        dom.typingIndicatorText.textContent = `${usernames[0]} is typing...`;
        return;
    }

    if (usernames.length === 2) {
        dom.typingIndicatorText.textContent = `${usernames[0]} and ${usernames[1]} are typing...`;
        return;
    }

    dom.typingIndicatorText.textContent = `${usernames[0]} and ${usernames.length - 1} others are typing...`;
}

function clearTypingState() {
    state.typingUsers.clear();

    for (const timer of state.typingExpiryTimers.values()) {
        clearTimeout(timer);
    }

    state.typingExpiryTimers.clear();
    renderTypingIndicator();
}

function triggerTyping() {
    if (!state.currentConversationId || !state.realtime) {
        return;
    }

    if (!state.typingStarted) {
        state.typingStarted = true;
        state.realtime.startTyping(state.currentConversationId).catch(() => { });
    }

    if (state.typingTimer) {
        clearTimeout(state.typingTimer);
    }

    state.typingTimer = setTimeout(() => {
        stopTyping().catch(() => { });
    }, 1400);
}

async function stopTyping() {
    if (!state.typingStarted || !state.currentConversationId || !state.realtime) {
        return;
    }

    state.typingStarted = false;

    if (state.typingTimer) {
        clearTimeout(state.typingTimer);
        state.typingTimer = null;
    }

    await state.realtime.stopTyping(state.currentConversationId);
}
async function sendCurrentMessage() {
    if (!state.currentConversationId) {
        window.appToast('Please select a conversation first.');
        return;
    }

    const content = dom.messageInput?.value?.trim() || '';
    if (!content && state.pendingAttachments.length === 0) {
        return;
    }

    const message = await sendMessage({
        conversationId: state.currentConversationId,
        content,
        messageType: state.pendingAttachments.length ? 'Attachment' : 'Text',
        replyToMessageId: state.replyToMessageId,
        attachments: state.pendingAttachments
    });

    dom.messageInput.value = '';
    autoResizeComposer();

    state.pendingAttachments = [];
    renderPendingAttachments();
    clearReply();

    upsertMessage(message);
    setConversationLastMessage(message.conversationId, message);
    renderMessages();
    renderConversations();

    hideNewMessagesIndicator();
    scrollMessagesToBottom(true);

    setUnreadCount(message.conversationId, 0);
    scheduleMarkRead(120);

    await stopTyping();
}

async function applyReaction(messageId, reactionType) {
    const message = state.messages.find((x) => x.id === messageId);
    if (!message) {
        return;
    }

    const currentUserId = getCurrentUserId();
    const hasMyReaction = (message.reactions || []).some((x) => x.userId === currentUserId && x.reactionType === reactionType);
    const reactionResult = await setReaction(messageId, reactionType, hasMyReaction);

    if (hasMyReaction) {
        removeReactionFromMessage(message, currentUserId, reactionType);
    } else if (reactionResult) {
        upsertReactionOnMessage(message, reactionResult);
    }

    renderMessages();
}

async function togglePin(messageId) {
    if (!state.currentConversationId) {
        return;
    }

    const isPinned = isPinnedMessage(messageId);
    await setPin(state.currentConversationId, messageId, !isPinned);

    if (isPinned) {
        state.pinnedMessages = state.pinnedMessages.filter((x) => x.messageId !== messageId);
    } else {
        const message = state.messages.find((x) => x.id === messageId);
        if (message) {
            upsertPinnedMessage({
                messageId: message.id,
                conversationId: state.currentConversationId,
                message,
                pinnedAt: new Date().toISOString(),
                pinnedBy: getCurrentUserId(),
                pinnedByUsername: state.me?.username || 'you'
            });
        }
    }

    renderPinnedMessages();
    renderMessages();
}

function toggleReactionPicker(messageItem) {
    const picker = messageItem.querySelector('[data-role="reaction-picker"]');
    const actions = messageItem.querySelector('.message-actions');

    if (!picker || !actions) {
        return;
    }

    const isHidden = picker.classList.contains('d-none');
    hideAllReactionPickers();

    if (isHidden) {
        picker.classList.remove('d-none');
        actions.classList.add('is-open');
    } else {
        picker.classList.add('d-none');
        actions.classList.remove('is-open');
    }
}

function hideAllReactionPickers() {
    for (const picker of dom.messageList?.querySelectorAll('[data-role="reaction-picker"]') || []) {
        picker.classList.add('d-none');
    }

    for (const actionBar of dom.messageList?.querySelectorAll('.message-actions') || []) {
        actionBar.classList.remove('is-open');
    }
}

function clearReply() {
    state.replyToMessageId = null;
    dom.replyBanner?.classList.add('d-none');

    if (dom.replyPreview) {
        dom.replyPreview.textContent = '';
    }
}

function upsertReactionOnMessage(message, reaction) {
    if (!message || !reaction) {
        return;
    }

    message.reactions = message.reactions || [];

    const reactionUserId = Number.parseInt(String(reaction.userId || ''), 10);
    const existingIndex = message.reactions.findIndex((item) =>
        Number.parseInt(String(item.userId || ''), 10) === reactionUserId
        && item.reactionType === reaction.reactionType);

    if (existingIndex >= 0) {
        message.reactions[existingIndex] = {
            ...message.reactions[existingIndex],
            ...reaction
        };
        return;
    }

    message.reactions.push(reaction);
}

function removeReactionFromMessage(message, userId, reactionType) {
    if (!message) {
        return;
    }

    const normalizedUserId = Number.parseInt(String(userId || ''), 10);
    message.reactions = (message.reactions || []).filter((item) =>
        !(Number.parseInt(String(item.userId || ''), 10) === normalizedUserId
            && item.reactionType === reactionType));
}

function upsertPinnedMessage(pinnedMessage) {
    if (!pinnedMessage) {
        return;
    }

    const existingIndex = state.pinnedMessages.findIndex((item) => item.messageId === pinnedMessage.messageId);
    if (existingIndex >= 0) {
        state.pinnedMessages[existingIndex] = {
            ...state.pinnedMessages[existingIndex],
            ...pinnedMessage
        };
        return;
    }

    state.pinnedMessages.unshift(pinnedMessage);
}

function upsertMessage(message) {
    const existingIndex = state.messages.findIndex((x) => x.id === message.id);
    if (existingIndex >= 0) {
        state.messages[existingIndex] = message;
    } else {
        state.messages.push(message);
    }
}

function onNewMessage(message) {
    if (!message || !message.conversationId) {
        return;
    }

    setConversationLastMessage(message.conversationId, message);

    if (message.conversationId !== state.currentConversationId) {
        if (message.senderId !== getCurrentUserId()) {
            incrementUnreadCount(message.conversationId);
        }
        renderConversations();
        notifyIncomingMessage(message);

        const knownConversation = state.conversations.some((x) => x.id === message.conversationId);
        if (!knownConversation) {
            refreshConversations().catch(() => { });
        }

        return;
    }

    const shouldStick = isNearBottom(120);

    upsertMessage(message);
    renderMessages();
    renderConversations();

    if (shouldStick || message.senderId === getCurrentUserId()) {
        scrollMessagesToBottom(true);
        hideNewMessagesIndicator();
    } else {
        showNewMessagesIndicator();
    }

    if (message.senderId !== getCurrentUserId()) {
        scheduleMarkRead(220);
    }
}

function notifyIncomingMessage(message) {
    if (!message || message.senderId === getCurrentUserId()) {
        return;
    }

    const senderName = resolveNotificationSenderName(message);
    const preview = truncate(buildMessagePreview(message), 96);

    if (window.appNotify) {
        window.appNotify({
            title: senderName,
            message: preview,
            type: 'primary',
            tag: `conversation-${message.conversationId}`,
            url: `/conversations/chat/${message.conversationId}`
        });
        return;
    }

    if (window.appToast) {
        window.appToast(`${senderName}: ${preview}`, 'primary', 4500);
    }
}

function resolveNotificationSenderName(message) {
    if (message?.senderUsername) {
        return message.senderUsername;
    }

    const conversation = state.conversations.find((x) => x.id === message?.conversationId);
    const member = conversation?.members?.find((x) => x.userId === message?.senderId);

    if (member?.username) {
        return member.username;
    }

    if (message?.senderId === getCurrentUserId()) {
        return state.me?.username || 'You';
    }

    return `User ${message?.senderId || ''}`.trim();
}

function buildMessagePreview(message) {
    const content = String(message?.content || '').trim();
    if (content) {
        return content;
    }

    const attachments = Array.isArray(message?.attachments) ? message.attachments : [];
    if (attachments.length === 1) {
        const fileName = attachments[0]?.fileName || 'Attachment';
        return `[Attachment] ${fileName}`;
    }

    if (attachments.length > 1) {
        return `[${attachments.length} attachments]`;
    }

    return '[New message]';
}
function onReactionAdded(reaction) {
    if (!reaction || Number.parseInt(String(reaction.conversationId || 0), 10) !== Number.parseInt(String(state.currentConversationId || 0), 10)) {
        return;
    }

    const messageId = Number.parseInt(String(reaction.messageId || 0), 10);
    const message = state.messages.find((x) => Number.parseInt(String(x.id || 0), 10) === messageId);
    if (!message) {
        loadMessages(null, { skipAutoScroll: true }).catch(() => { });
        return;
    }

    upsertReactionOnMessage(message, {
        id: 0,
        userId: reaction.userId,
        username: reaction.username,
        reactionType: reaction.reactionType,
        createdAt: reaction.occurredAt
    });

    renderMessages();
}
function onReactionRemoved(reaction) {
    if (!reaction || Number.parseInt(String(reaction.conversationId || 0), 10) !== Number.parseInt(String(state.currentConversationId || 0), 10)) {
        return;
    }

    const messageId = Number.parseInt(String(reaction.messageId || 0), 10);
    const message = state.messages.find((x) => Number.parseInt(String(x.id || 0), 10) === messageId);
    if (!message) {
        loadMessages(null, { skipAutoScroll: true }).catch(() => { });
        return;
    }

    removeReactionFromMessage(message, reaction.userId, reaction.reactionType);
    renderMessages();
}
function onPinnedAdded(pinUpdate) {
    if (!pinUpdate || Number.parseInt(String(pinUpdate.conversationId || 0), 10) !== Number.parseInt(String(state.currentConversationId || 0), 10)) {
        return;
    }

    const messageId = Number.parseInt(String(pinUpdate.messageId || 0), 10);
    const message = state.messages.find((x) => Number.parseInt(String(x.id || 0), 10) === messageId);
    if (!message) {
        loadPinnedMessages(state.currentConversationId).catch(() => { });
        return;
    }

    upsertPinnedMessage({
        messageId: message.id,
        conversationId: state.currentConversationId,
        message,
        pinnedAt: pinUpdate.occurredAt,
        pinnedBy: pinUpdate.updatedByUserId,
        pinnedByUsername: `user-${pinUpdate.updatedByUserId}`
    });

    renderPinnedMessages();
    renderMessages();
}
function onPinnedRemoved(pinUpdate) {
    if (!pinUpdate || Number.parseInt(String(pinUpdate.conversationId || 0), 10) !== Number.parseInt(String(state.currentConversationId || 0), 10)) {
        return;
    }

    const messageId = Number.parseInt(String(pinUpdate.messageId || 0), 10);
    state.pinnedMessages = state.pinnedMessages.filter((x) => Number.parseInt(String(x.messageId || 0), 10) !== messageId);
    renderPinnedMessages();
    renderMessages();
}
function onTypingIndicator(typingIndicator) {
    if (!typingIndicator || typingIndicator.conversationId !== state.currentConversationId) {
        return;
    }

    if (typingIndicator.userId === getCurrentUserId()) {
        return;
    }

    if (typingIndicator.isTyping) {
        state.typingUsers.set(typingIndicator.userId, typingIndicator.username || `User ${typingIndicator.userId}`);

        const existingTimer = state.typingExpiryTimers.get(typingIndicator.userId);
        if (existingTimer) {
            clearTimeout(existingTimer);
        }

        const expiryTimer = setTimeout(() => {
            state.typingUsers.delete(typingIndicator.userId);
            state.typingExpiryTimers.delete(typingIndicator.userId);
            renderTypingIndicator();
        }, 4500);

        state.typingExpiryTimers.set(typingIndicator.userId, expiryTimer);
    } else {
        state.typingUsers.delete(typingIndicator.userId);

        const existingTimer = state.typingExpiryTimers.get(typingIndicator.userId);
        if (existingTimer) {
            clearTimeout(existingTimer);
            state.typingExpiryTimers.delete(typingIndicator.userId);
        }
    }

    renderTypingIndicator();
}
function getConversationDisplay(conversation) {
    if (!conversation) {
        return {
            name: 'Conversation',
            avatarUrl: null
        };
    }

    if (conversation.type === 'Private' && Array.isArray(conversation.members)) {
        const meId = getCurrentUserId();
        const other = conversation.members.find((member) => member.userId !== meId);

        if (other) {
            return {
                name: other.username,
                avatarUrl: other.avatarUrl || conversation.avatarUrl || null
            };
        }
    }

    return {
        name: conversation.name || `${conversation.type} #${conversation.id}`,
        avatarUrl: conversation.avatarUrl || null
    };
}

function resolveUserProfile(userId, fallbackUsername) {
    if (userId === getCurrentUserId()) {
        return {
            username: state.me?.username || fallbackUsername || 'You',
            avatarUrl: state.me?.avatarUrl || null
        };
    }

    const conversation = state.conversations.find((x) => x.id === state.currentConversationId);
    const member = conversation?.members?.find((x) => x.userId === userId);

    return {
        username: member?.username || fallbackUsername || `User ${userId}`,
        avatarUrl: member?.avatarUrl || null
    };
}

function isPinnedMessage(messageId) {
    return state.pinnedMessages.some((x) => x.messageId === messageId);
}

function isGroupConversation(conversation) {
    return String(conversation?.type || '').toLowerCase() === 'group';
}

function getOrderedMessages() {
    return [...state.messages].sort((a, b) => {
        const aTime = new Date(a.createdAt).getTime();
        const bTime = new Date(b.createdAt).getTime();
        return aTime === bTime ? a.id - b.id : aTime - bTime;
    });
}

function buildReactionStats(reactions) {
    const grouped = new Map();

    for (const reaction of reactions) {
        if (!grouped.has(reaction.reactionType)) {
            grouped.set(reaction.reactionType, {
                reactionType: reaction.reactionType,
                count: 0,
                usernames: [],
                hasMine: false,
                emoji: mapReactionToEmoji(reaction.reactionType)
            });
        }

        const bucket = grouped.get(reaction.reactionType);
        bucket.count += 1;
        bucket.usernames.push(reaction.username || `User ${reaction.userId}`);
        if (reaction.userId === getCurrentUserId()) {
            bucket.hasMine = true;
        }
    }

    return Array.from(grouped.values()).sort((a, b) => b.count - a.count);
}

function jumpToMessage(messageId) {
    const target = dom.messageList?.querySelector(`.message-item[data-message-id="${messageId}"]`);
    if (!target || !dom.messageScrollRegion) {
        return;
    }

    const top = target.offsetTop - 100;
    dom.messageScrollRegion.scrollTo({ top: Math.max(top, 0), behavior: 'smooth' });

    target.classList.add('bg-warning-subtle');
    setTimeout(() => target.classList.remove('bg-warning-subtle'), 1200);
}

function showNewMessagesIndicator() {
    state.newMessageCount += 1;

    if (!dom.newMessagesIndicator) {
        return;
    }

    dom.newMessagesIndicator.classList.remove('d-none');
    dom.newMessagesIndicator.textContent = state.newMessageCount > 1
        ? `${state.newMessageCount} new messages`
        : 'New message';
}

function hideNewMessagesIndicator() {
    state.newMessageCount = 0;

    if (!dom.newMessagesIndicator) {
        return;
    }

    dom.newMessagesIndicator.classList.add('d-none');
    dom.newMessagesIndicator.textContent = 'New messages';
}

function scheduleMarkRead(delay = 150) {
    if (!state.currentConversationId) {
        return;
    }

    if (state.markReadTimer) {
        clearTimeout(state.markReadTimer);
    }

    state.markReadTimer = setTimeout(async () => {
        if (!state.currentConversationId) {
            return;
        }

        try {
            await markConversationRead(state.currentConversationId);
            setUnreadCount(state.currentConversationId, 0);
            renderConversations();
        } catch {
            // Non-blocking.
        }
    }, delay);
}

function autoResizeComposer() {
    if (!dom.messageInput) {
        return;
    }

    dom.messageInput.style.height = 'auto';
    const newHeight = Math.min(dom.messageInput.scrollHeight, 160);
    dom.messageInput.style.height = `${Math.max(newHeight, 42)}px`;
}

function insertEmojiAtCursor(emoji) {
    if (!emoji || !dom.messageInput) {
        return;
    }

    const input = dom.messageInput;
    const start = input.selectionStart || 0;
    const end = input.selectionEnd || 0;

    input.value = `${input.value.slice(0, start)}${emoji}${input.value.slice(end)}`;

    const cursor = start + emoji.length;
    input.focus();
    input.selectionStart = cursor;
    input.selectionEnd = cursor;

    autoResizeComposer();
    triggerTyping();
}

function setConversationLastMessage(conversationId, message) {
    const conversation = state.conversations.find((x) => x.id === conversationId);
    if (!conversation) {
        return;
    }

    conversation.lastMessageId = message.id;
    conversation.lastMessageAt = message.createdAt;
    conversation.lastMessagePreview = buildMessagePreview(message);
}

function setUnreadCount(conversationId, value) {
    state.unreadByConversation.set(conversationId, Math.max(0, value));
}

function incrementUnreadCount(conversationId) {
    const current = state.unreadByConversation.get(conversationId) || 0;
    state.unreadByConversation.set(conversationId, current + 1);
}

function getUnreadCount(conversationId) {
    return state.unreadByConversation.get(conversationId) || 0;
}

function getCurrentUserId() {
    return state.me?.userId ?? state.me?.id ?? 0;
}

function scrollMessagesToBottom(smooth = false) {
    if (!dom.messageScrollRegion) {
        return;
    }

    const top = dom.messageScrollRegion.scrollHeight;

    if (smooth) {
        dom.messageScrollRegion.scrollTo({ top, behavior: 'smooth' });
    } else {
        dom.messageScrollRegion.scrollTop = top;
    }
}

function isNearBottom(threshold = 80) {
    if (!dom.messageScrollRegion) {
        return true;
    }

    const remaining = dom.messageScrollRegion.scrollHeight - dom.messageScrollRegion.scrollTop - dom.messageScrollRegion.clientHeight;
    return remaining <= threshold;
}

function setAvatarElement(element, avatarUrl, fallbackText) {
    if (!element) {
        return;
    }

    if (avatarUrl) {
        element.innerHTML = `<img src="${escapeHtml(avatarUrl)}" alt="avatar" loading="lazy" />`;
        return;
    }

    element.textContent = fallbackText || '?';
}

function getInitials(value) {
    if (!value) {
        return '?';
    }

    const parts = String(value)
        .trim()
        .split(/\s+/)
        .filter(Boolean)
        .slice(0, 2);

    if (!parts.length) {
        return '?';
    }

    return parts.map((x) => x[0].toUpperCase()).join('');
}

function mapReactionToEmoji(reactionType) {
    return REACTION_EMOJI_MAP[reactionType] || '👍';
}

function truncate(value, maxLength) {
    if (!value) {
        return '';
    }

    return value.length <= maxLength ? value : `${value.slice(0, maxLength - 3)}...`;
}

function formatFileSize(size) {
    if (!size) {
        return '0 B';
    }

    const units = ['B', 'KB', 'MB', 'GB'];
    let index = 0;
    let value = size;

    while (value >= 1024 && index < units.length - 1) {
        value /= 1024;
        index += 1;
    }

    return `${value.toFixed(value >= 10 || index === 0 ? 0 : 1)} ${units[index]}`;
}

function formatTime(value) {
    if (!value) {
        return '';
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
        return '';
    }

    return date.toLocaleString([], {
        hour: '2-digit',
        minute: '2-digit',
        month: 'short',
        day: 'numeric'
    });
}

function formatLongTime(value) {
    if (!value) {
        return '';
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
        return '';
    }

    return date.toLocaleString([], {
        month: 'short',
        day: 'numeric',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
    });
}

function formatConversationTime(value) {
    if (!value) {
        return '';
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
        return '';
    }

    const now = new Date();
    const sameDay = date.toDateString() === now.toDateString();

    if (sameDay) {
        return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    }

    return date.toLocaleDateString([], { month: 'short', day: 'numeric' });
}

function escapeHtml(value) {
    return String(value)
        .replaceAll('&', '&amp;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;')
        .replaceAll('"', '&quot;')
        .replaceAll("'", '&#39;');
}

function handleError(error) {
    const message = error?.message || 'Unexpected error.';

    if (window.appToast) {
        window.appToast(message);
    } else {
        console.error(message);
    }
}






















