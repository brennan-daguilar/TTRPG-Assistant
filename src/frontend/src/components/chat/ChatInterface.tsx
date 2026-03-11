import { useState, useRef, useEffect, useCallback } from 'react';
import { Stack, TextInput, Paper, Text, ScrollArea, Group, Badge, ActionIcon, Loader } from '@mantine/core';
import { createChatConnection, SourceReference } from '../../api/client';
import type { HubConnection } from '@microsoft/signalr';

interface Message {
  role: 'user' | 'assistant';
  content: string;
  sources?: SourceReference[];
}

export default function ChatInterface() {
  const [messages, setMessages] = useState<Message[]>([]);
  const [input, setInput] = useState('');
  const [isStreaming, setIsStreaming] = useState(false);
  const [connection, setConnection] = useState<HubConnection | null>(null);
  const scrollRef = useRef<HTMLDivElement>(null);
  const streamBuffer = useRef('');

  useEffect(() => {
    const conn = createChatConnection();

    conn.on('SourceReferences', (sources: SourceReference[]) => {
      setMessages((prev) => {
        const updated = [...prev];
        if (updated.length > 0 && updated[updated.length - 1].role === 'assistant') {
          updated[updated.length - 1] = { ...updated[updated.length - 1], sources };
        }
        return updated;
      });
    });

    conn.start().then(() => setConnection(conn));

    return () => {
      conn.stop();
    };
  }, []);

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

    // Add empty assistant message for streaming
    setMessages((prev) => [...prev, { role: 'assistant', content: '' }]);

    try {
      const stream = connection.stream('Ask', userMessage, null, null);

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
          {messages.length === 0 && (
            <Text c="dimmed" ta="center" mt="xl">
              Ask a question about your worldbuilding notes...
            </Text>
          )}
          {messages.map((msg, i) => (
            <Paper
              key={i}
              p="sm"
              radius="md"
              bg={msg.role === 'user' ? 'violet.9' : 'dark.6'}
              style={{ alignSelf: msg.role === 'user' ? 'flex-end' : 'flex-start', maxWidth: '80%' }}
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
