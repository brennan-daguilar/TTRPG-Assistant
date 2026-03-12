import { useState, useRef, useEffect, useCallback } from 'react';
import { Stack, TextInput, Paper, Text, ScrollArea, Group, Badge, ActionIcon, Loader, Box } from '@mantine/core';
import { createChatConnection, conversations as convApi } from '../../api/client';
import type { SourceReference, NoteProposal } from '../../api/client';
import type { HubConnection } from '@microsoft/signalr';
import ProposalCard from './ProposalCard';

interface Message {
  role: 'user' | 'assistant';
  content: string;
  sources?: SourceReference[];
  proposals?: NoteProposal[];
}

interface ChatInterfaceProps {
  conversationId: string | null;
  onConversationCreated: (id: string, title: string) => void;
}

export default function ChatInterface({ conversationId, onConversationCreated }: ChatInterfaceProps) {
  const [messages, setMessages] = useState<Message[]>([]);
  const [input, setInput] = useState('');
  const [isStreaming, setIsStreaming] = useState(false);
  const [connection, setConnection] = useState<HubConnection | null>(null);
  const [loading, setLoading] = useState(false);
  const scrollRef = useRef<HTMLDivElement>(null);
  const streamBuffer = useRef('');
  const currentConvId = useRef<string | null>(conversationId);

  // Keep ref in sync
  currentConvId.current = conversationId;

  useEffect(() => {
    const conn = createChatConnection();

    conn.on('SourceReferences', (sources: SourceReference[]) => {
      setMessages((prev) => {
        const updated = [...prev];
        const lastIdx = updated.length - 1;
        if (lastIdx >= 0 && updated[lastIdx].role === 'assistant') {
          updated[lastIdx] = { ...updated[lastIdx], sources };
        }
        return updated;
      });
    });

    conn.on('ConversationCreated', (id: string, title: string) => {
      currentConvId.current = id;
      onConversationCreated(id, title);
    });

    conn.on('NoteProposals', (proposals: NoteProposal[]) => {
      setMessages((prev) => {
        const updated = [...prev];
        const lastIdx = updated.length - 1;
        if (lastIdx >= 0 && updated[lastIdx].role === 'assistant') {
          updated[lastIdx] = { ...updated[lastIdx], proposals };
        }
        return updated;
      });
    });

    conn.start().then(() => setConnection(conn));

    return () => {
      conn.stop();
    };
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  // Load conversation history when conversationId changes
  useEffect(() => {
    if (conversationId) {
      setLoading(true);
      convApi.get(conversationId)
        .then((conv) => {
          setMessages(
            conv.messages.map((m) => ({
              role: m.role as 'user' | 'assistant',
              content: m.content,
            }))
          );
        })
        .catch(() => setMessages([]))
        .finally(() => setLoading(false));
    } else {
      setMessages([]);
    }
  }, [conversationId]);

  useEffect(() => {
    scrollRef.current?.scrollTo({ top: scrollRef.current.scrollHeight, behavior: 'smooth' });
  }, [messages]);

  const sendMessage = useCallback(async () => {
    if (!input.trim() || !connection || isStreaming) return;

    const userMessage = input.trim();
    setInput('');
    setMessages((prev) => [...prev, { role: 'user', content: userMessage }]);
    setIsStreaming(true);
    streamBuffer.current = '';

    setMessages((prev) => [...prev, { role: 'assistant', content: '' }]);

    try {
      const stream = connection.stream('Ask', userMessage, currentConvId.current, null);

      stream.subscribe({
        next: (chunk: string) => {
          streamBuffer.current += chunk;
          const content = streamBuffer.current;
          setMessages((prev) => {
            const updated = [...prev];
            updated[updated.length - 1] = { ...updated[updated.length - 1], content };
            return updated;
          });
        },
        error: (err: Error) => {
          console.error('Stream error:', err);
          setMessages((prev) => {
            const updated = [...prev];
            updated[updated.length - 1] = {
              ...updated[updated.length - 1],
              content: streamBuffer.current || 'An error occurred while generating a response.',
            };
            return updated;
          });
          setIsStreaming(false);
        },
        complete: () => {
          setIsStreaming(false);
        },
      });
    } catch (err) {
      console.error('Failed to send:', err);
      setIsStreaming(false);
    }
  }, [input, connection, isStreaming]);

  return (
    <Stack h="calc(100vh - 100px)">
      <ScrollArea flex={1} viewportRef={scrollRef}>
        <Stack gap="md" p="xs">
          {loading && <Loader mx="auto" mt="xl" />}
          {!loading && messages.length === 0 && (
            <Text c="dimmed" ta="center" mt="xl">
              Ask a question about your worldbuilding notes...
            </Text>
          )}
          {messages.map((msg, i) => (
            <Box key={i} style={{ display: 'flex', flexDirection: 'column', alignItems: msg.role === 'user' ? 'flex-end' : 'flex-start' }}>
              <Paper
                p="sm"
                radius="md"
                bg={msg.role === 'user' ? 'violet.9' : 'dark.6'}
                style={{ maxWidth: '80%' }}
              >
                <Text size="sm" style={{ whiteSpace: 'pre-wrap' }}>
                  {msg.content}
                  {isStreaming && i === messages.length - 1 && msg.role === 'assistant' && '▊'}
                </Text>
                {msg.sources && msg.sources.length > 0 && (
                  <Group gap={4} mt="xs">
                    {msg.sources.map((s, j) => (
                      <Badge key={j} size="xs" variant="outline">
                        {s.entityName}
                      </Badge>
                    ))}
                  </Group>
                )}
              </Paper>
              {msg.proposals && msg.proposals.length > 0 && (
                <Stack gap="xs" mt="xs" style={{ maxWidth: '80%', width: '100%' }}>
                  {msg.proposals.map((p) => (
                    <ProposalCard key={p.id} proposal={p} />
                  ))}
                </Stack>
              )}
            </Box>
          ))}
        </Stack>
      </ScrollArea>

      <Group>
        <TextInput
          flex={1}
          placeholder="Ask about your world..."
          value={input}
          onChange={(e) => setInput(e.currentTarget.value)}
          onKeyDown={(e) => e.key === 'Enter' && sendMessage()}
          disabled={isStreaming}
          rightSection={isStreaming ? <Loader size="xs" /> : undefined}
        />
        <ActionIcon size="lg" variant="filled" onClick={sendMessage} disabled={isStreaming || !input.trim()}>
          →
        </ActionIcon>
      </Group>
    </Stack>
  );
}
