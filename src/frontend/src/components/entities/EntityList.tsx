import { useState, useEffect } from 'react';
import { Stack, TextInput, Select, Group, Card, Text, Badge, ActionIcon, Loader } from '@mantine/core';
import { entities as api, EntityListItem } from '../../api/client';

interface EntityListProps {
  onSelect: (id: string) => void;
  selectedId?: string;
}

export default function EntityList({ onSelect, selectedId }: EntityListProps) {
  const [items, setItems] = useState<EntityListItem[]>([]);
  const [types, setTypes] = useState<string[]>([]);
  const [search, setSearch] = useState('');
  const [typeFilter, setTypeFilter] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api.types().then(setTypes).catch(() => {});
  }, []);

  useEffect(() => {
    setLoading(true);
    api.list(typeFilter ?? undefined, search || undefined)
      .then(setItems)
      .finally(() => setLoading(false));
  }, [search, typeFilter]);

  return (
    <Stack>
      <Group>
        <TextInput
          flex={1}
          placeholder="Search entities..."
          value={search}
          onChange={(e) => setSearch(e.currentTarget.value)}
        />
        <Select
          placeholder="All types"
          data={types}
          value={typeFilter}
          onChange={setTypeFilter}
          clearable
          w={150}
        />
      </Group>

      {loading ? (
        <Loader mx="auto" mt="md" />
      ) : items.length === 0 ? (
        <Text c="dimmed" ta="center" mt="md">No entities found</Text>
      ) : (
        <Stack gap="xs">
          {items.map((item) => (
            <Card
              key={item.id}
              padding="sm"
              radius="sm"
              withBorder
              style={{ cursor: 'pointer', borderColor: item.id === selectedId ? 'var(--mantine-color-violet-6)' : undefined }}
              onClick={() => onSelect(item.id)}
            >
              <Group justify="space-between">
                <div>
                  <Text fw={500} size="sm">{item.name}</Text>
                  {item.description && <Text size="xs" c="dimmed" lineClamp={1}>{item.description}</Text>}
                </div>
                <Badge size="xs" variant="light">{item.entityType}</Badge>
              </Group>
              {item.tags.length > 0 && (
                <Group gap={4} mt={4}>
                  {item.tags.map((tag) => (
                    <Badge key={tag} size="xs" variant="outline">{tag}</Badge>
                  ))}
                </Group>
              )}
            </Card>
          ))}
        </Stack>
      )}
    </Stack>
  );
}
