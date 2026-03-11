import { useState, useEffect } from 'react';
import { Stack, TextInput, Textarea, Select, TagsInput, Button, Group, Text, Loader } from '@mantine/core';
import { entities as api, WorldEntity } from '../../api/client';

interface EntityEditorProps {
  entityId?: string;
  onSaved?: () => void;
}

const ENTITY_TYPES = ['Character', 'Location', 'Faction', 'Item', 'Event', 'Lore'];

export default function EntityEditor({ entityId, onSaved }: EntityEditorProps) {
  const [entity, setEntity] = useState<Partial<WorldEntity>>({
    name: '',
    entityType: 'Lore',
    description: '',
    content: '',
    tags: [],
  });
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (entityId) {
      setLoading(true);
      api.get(entityId).then((e) => setEntity(e)).finally(() => setLoading(false));
    } else {
      setEntity({ name: '', entityType: 'Lore', description: '', content: '', tags: [] });
    }
  }, [entityId]);

  const handleSave = async () => {
    setSaving(true);
    try {
      if (entityId) {
        await api.update(entityId, entity);
      } else {
        await api.create({
          name: entity.name!,
          entityType: entity.entityType!,
          description: entity.description,
          content: entity.content,
          tags: entity.tags,
        });
      }
      onSaved?.();
    } finally {
      setSaving(false);
    }
  };

  if (loading) return <Loader mx="auto" mt="md" />;

  return (
    <Stack>
      <Text fw={600} size="lg">{entityId ? 'Edit Entity' : 'New Entity'}</Text>
      <TextInput
        label="Name"
        required
        value={entity.name ?? ''}
        onChange={(e) => setEntity({ ...entity, name: e.currentTarget.value })}
      />
      <Select
        label="Type"
        data={ENTITY_TYPES}
        value={entity.entityType ?? 'Lore'}
        onChange={(v) => setEntity({ ...entity, entityType: v ?? 'Lore' })}
      />
      <TextInput
        label="Description"
        value={entity.description ?? ''}
        onChange={(e) => setEntity({ ...entity, description: e.currentTarget.value })}
      />
      <TagsInput
        label="Tags"
        value={entity.tags ?? []}
        onChange={(tags) => setEntity({ ...entity, tags })}
      />
      <Textarea
        label="Content"
        minRows={12}
        autosize
        value={entity.content ?? ''}
        onChange={(e) => setEntity({ ...entity, content: e.currentTarget.value })}
        styles={{ input: { fontFamily: 'monospace' } }}
      />
      <Group>
        <Button onClick={handleSave} loading={saving}>
          {entityId ? 'Save Changes' : 'Create Entity'}
        </Button>
      </Group>
    </Stack>
  );
}
