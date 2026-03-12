import { useState, useEffect } from 'react';
import { Stack, NavLink, ActionIcon, Group, Text, Loader, Modal, Button } from '@mantine/core';
import { conversations as api } from '../../api/client';
import type { ConversationListItem } from '../../api/client';

interface ConversationListProps {
  selectedId: string | null;
  onSelect: (id: string | null) => void;
  refreshKey: number;
}

export default function ConversationList({ selectedId, onSelect, refreshKey }: ConversationListProps) {
  const [items, setItems] = useState<ConversationListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [deleteTarget, setDeleteTarget] = useState<ConversationListItem | null>(null);

  useEffect(() => {
    setLoading(true);
    api.list().then(setItems).finally(() => setLoading(false));
  }, [refreshKey]);

  const handleDelete = async () => {
    if (!deleteTarget) return;
    await api.delete(deleteTarget.id);
    setItems((prev) => prev.filter((i) => i.id !== deleteTarget.id));
    if (selectedId === deleteTarget.id) onSelect(null);
    setDeleteTarget(null);
  };

  if (loading) return <Loader size="sm" mx="auto" mt="md" />;

  return (
    <>
      <Stack gap={2}>
        <NavLink
          label="New Chat"
          active={selectedId === null}
          onClick={() => onSelect(null)}
          fw={500}
        />
        {items.map((item) => (
          <NavLink
            key={item.id}
            label={
              <Group justify="space-between" wrap="nowrap" gap={4}>
                <Text size="sm" lineClamp={1} style={{ flex: 1 }}>{item.title}</Text>
                <ActionIcon
                  size="xs"
                  variant="subtle"
                  color="red"
                  onClick={(e) => {
                    e.stopPropagation();
                    setDeleteTarget(item);
                  }}
                >
                  ×
                </ActionIcon>
              </Group>
            }
            active={selectedId === item.id}
            onClick={() => onSelect(item.id)}
          />
        ))}
      </Stack>

      <Modal opened={!!deleteTarget} onClose={() => setDeleteTarget(null)} title="Delete Conversation" size="sm">
        <Text size="sm" mb="md">
          Delete "{deleteTarget?.title}"? This cannot be undone.
        </Text>
        <Group justify="flex-end">
          <Button variant="subtle" onClick={() => setDeleteTarget(null)}>Cancel</Button>
          <Button color="red" onClick={handleDelete}>Delete</Button>
        </Group>
      </Modal>
    </>
  );
}
