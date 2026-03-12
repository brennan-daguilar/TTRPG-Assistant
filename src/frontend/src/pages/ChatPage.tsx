import { useState, useCallback } from 'react';
import { Grid } from '@mantine/core';
import ChatInterface from '../components/chat/ChatInterface';
import ConversationList from '../components/chat/ConversationList';

export default function ChatPage() {
  const [conversationId, setConversationId] = useState<string | null>(null);
  const [refreshKey, setRefreshKey] = useState(0);

  const handleConversationCreated = useCallback((id: string, _title: string) => {
    setConversationId(id);
    setRefreshKey((k) => k + 1);
  }, []);

  return (
    <Grid gutter="md">
      <Grid.Col span={{ base: 12, md: 3 }}>
        <ConversationList
          selectedId={conversationId}
          onSelect={setConversationId}
          refreshKey={refreshKey}
        />
      </Grid.Col>
      <Grid.Col span={{ base: 12, md: 9 }}>
        <ChatInterface
          conversationId={conversationId}
          onConversationCreated={handleConversationCreated}
        />
      </Grid.Col>
    </Grid>
  );
}
