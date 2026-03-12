import { getAccessToken } from './auth.js';

export class ChatRealtimeClient extends EventTarget {
    constructor({ backendBaseUrl, hubPath }) {
        super();
        this.backendBaseUrl = (backendBaseUrl || '').replace(/\/+$/, '');
        this.hubPath = hubPath?.startsWith('/') ? hubPath : `/${hubPath || 'hubs/chat'}`;
        this.connection = null;
    }

    async connect() {
        if (this.connection) {
            return;
        }

        if (!window.signalR) {
            throw new Error('SignalR client script is not loaded.');
        }

        const hubUrl = `${this.backendBaseUrl}${this.hubPath}`;

        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(hubUrl, {
                accessTokenFactory: () => getAccessToken() || ''
            })
            .withAutomaticReconnect()
            .build();

        this.connection.onreconnecting(() => {
            this.dispatchEvent(new CustomEvent('connectionReconnecting'));
        });

        this.connection.onreconnected(() => {
            this.dispatchEvent(new CustomEvent('connectionReconnected'));
        });

        this.connection.onclose((error) => {
            this.dispatchEvent(new CustomEvent('connectionClosed', { detail: error }));
        });

        this.connection.on('ReceiveMessage', (message) => {
            if (!message) {
                return;
            }

            this.dispatchEvent(new CustomEvent('newMessage', {
                detail: {
                    ...message,
                    receivedAt: message.receivedAt || new Date().toISOString()
                }
            }));
        });

        this.connection.on('MessageReactionUpdated', (reaction) => {
            const normalizedReaction = normalizeReactionUpdate(reaction);
            this.dispatchEvent(new CustomEvent('messageReactionUpdated', { detail: normalizedReaction }));
            this.dispatchEvent(new CustomEvent(normalizedReaction.isRemoved ? 'messageReactionRemoved' : 'messageReactionAdded', { detail: normalizedReaction }));
        });
        this.connection.on('PinnedMessageUpdated', (pinUpdate) => {
            const normalizedPinUpdate = normalizePinnedUpdate(pinUpdate);
            this.dispatchEvent(new CustomEvent('pinnedMessageUpdated', { detail: normalizedPinUpdate }));
            this.dispatchEvent(new CustomEvent(normalizedPinUpdate.isPinned ? 'messagePinned' : 'messageUnpinned', { detail: normalizedPinUpdate }));
        });
        this.connection.on('TypingIndicatorUpdated', (typingIndicator) => {
            this.dispatchEvent(new CustomEvent('typingIndicator', { detail: typingIndicator }));
        });

        try {
            await this.connection.start();
            this.dispatchEvent(new CustomEvent('connectionReady'));
        } catch (error) {
            this.dispatchEvent(new CustomEvent('connectionFailed', { detail: error }));
            throw error;
        }
    }

    async disconnect() {
        if (!this.connection) {
            return;
        }

        await this.connection.stop();
        this.connection = null;
    }

    async joinConversation(conversationId) {
        if (!this.connection) {
            return;
        }

        await this.connection.invoke('JoinConversation', conversationId);
    }

    async leaveConversation(conversationId) {
        if (!this.connection) {
            return;
        }

        await this.connection.invoke('LeaveConversation', conversationId);
    }

    async startTyping(conversationId) {
        if (!this.connection) {
            return;
        }

        await this.connection.invoke('StartTyping', conversationId);
    }

    async stopTyping(conversationId) {
        if (!this.connection) {
            return;
        }

        await this.connection.invoke('StopTyping', conversationId);
    }
}


function normalizeReactionUpdate(reaction) {
    return {
        conversationId: Number.parseInt(String(reaction?.conversationId ?? reaction?.ConversationId ?? 0), 10) || 0,
        messageId: Number.parseInt(String(reaction?.messageId ?? reaction?.MessageId ?? 0), 10) || 0,
        userId: Number.parseInt(String(reaction?.userId ?? reaction?.UserId ?? 0), 10) || 0,
        username: reaction?.username ?? reaction?.Username ?? '',
        reactionType: reaction?.reactionType ?? reaction?.ReactionType ?? '',
        isRemoved: Boolean(reaction?.isRemoved ?? reaction?.IsRemoved),
        occurredAt: reaction?.occurredAt ?? reaction?.OccurredAt ?? new Date().toISOString()
    };
}

function normalizePinnedUpdate(pinUpdate) {
    return {
        conversationId: Number.parseInt(String(pinUpdate?.conversationId ?? pinUpdate?.ConversationId ?? 0), 10) || 0,
        messageId: Number.parseInt(String(pinUpdate?.messageId ?? pinUpdate?.MessageId ?? 0), 10) || 0,
        updatedByUserId: Number.parseInt(String(pinUpdate?.updatedByUserId ?? pinUpdate?.UpdatedByUserId ?? 0), 10) || 0,
        isPinned: Boolean(pinUpdate?.isPinned ?? pinUpdate?.IsPinned),
        occurredAt: pinUpdate?.occurredAt ?? pinUpdate?.OccurredAt ?? new Date().toISOString()
    };
}

