import { useState, useEffect } from 'react';
import { Stack, TextInput, Textarea, Select, TagsInput, Button, Group, Text, Loader, Modal, Card, Badge, ActionIcon } from '@mantine/core';
import { entities as api, relationships as relApi, WorldEntity, Relationship, EntityListItem } from '../../api/client';

interface EntityEditorProps {
  entityId?: string;
  onSaved?: () => void;
  onDeleted?: () => void;
}

const ENTITY_TYPES = ['Character', 'Location', 'Faction', 'Item', 'Event', 'Lore'];
const RELATIONSHIP_TYPES = ['member_of', 'located_in', 'allied_with', 'enemy_of', 'owns', 'created_by', 'related_to'];

export default function EntityEditor({ entityId, onSaved, onDeleted }: EntityEditorProps) {
  const [entity, setEntity] = useState<Partial<WorldEntity>>({
    name: '',
    entityType: 'Lore',
    description: '',
    content: '',
    tags: [],
  });
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [deleteModalOpen, setDeleteModalOpen] = useState(false);
  const [deleting, setDeleting] = useState(false);

  // Relationships
  const [rels, setRels] = useState<Relationship[]>([]);
  const [showAddRel, setShowAddRel] = useState(false);
  const [allEntities, setAllEntities] = useState<EntityListItem[]>([]);
  const [newRelTarget, setNewRelTarget] = useState<string | null>(null);
  const [newRelType, setNewRelType] = useState<string>('related_to');

  useEffect(() => {
    if (entityId) {
      setLoading(true);
      Promise.all([
        api.get(entityId),
        relApi.forEntity(entityId),
      ]).then(([e, r]) => {
        setEntity(e);
        setRels(r);
      }).finally(() => setLoading(false));
    } else {
      setEntity({ name: '', entityType: 'Lore', description: '', content: '', tags: [] });
      setRels([]);
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

  const handleDelete = async () => {
    if (!entityId) return;
    setDeleting(true);
    try {
      await api.delete(entityId);
      setDeleteModalOpen(false);
      onDeleted?.();
    } finally {
      setDeleting(false);
    }
  };

  const handleAddRelationship = async () => {
    if (!entityId || !newRelTarget) return;
    await relApi.create({
      fromEntityId: entityId,
      toEntityId: newRelTarget,
      relationshipType: newRelType,
    });
    const updated = await relApi.forEntity(entityId);
    setRels(updated);
    setShowAddRel(false);
    setNewRelTarget(null);
  };

  const handleRemoveRelationship = async (relId: string) => {
    await relApi.delete(relId);
    setRels((prev) => prev.filter((r) => r.id !== relId));
  };

  const openAddRelationship = async () => {
    const all = await api.list();
    setAllEntities(all.filter((e) => e.id !== entityId));
    setShowAddRel(true);
  };

  if (loading) return <Loader mx="auto" mt="md" />;

  return (
    <Stack>
      <Group justify="space-between">
        <Text fw={600} size="lg">{entityId ? 'Edit Entity' : 'New Entity'}</Text>
        {entityId && (
          <Button size="xs" color="red" variant="outline" onClick={() => setDeleteModalOpen(true)}>
            Delete
          </Button>
        )}
      </Group>
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

      {/* Relationships section */}
      {entityId && (
        <Stack gap="xs">
          <Group justify="space-between">
            <Text fw={500} size="sm">Relationships</Text>
            <Button size="xs" variant="light" onClick={openAddRelationship}>+ Add</Button>
          </Group>
          {rels.length === 0 ? (
            <Text size="xs" c="dimmed">No relationships</Text>
          ) : (
            rels.map((rel) => {
              const isFrom = rel.fromEntityId === entityId;
              const otherName = isFrom ? rel.toEntityName : rel.fromEntityName;
              const direction = isFrom ? '→' : '←';
              return (
                <Card key={rel.id} padding="xs" withBorder>
                  <Group justify="space-between">
                    <Group gap="xs">
                      <Text size="xs">{direction}</Text>
                      <Badge size="xs" variant="light">{rel.relationshipType}</Badge>
                      <Text size="xs">{otherName}</Text>
                    </Group>
                    <ActionIcon size="xs" variant="subtle" color="red" onClick={() => handleRemoveRelationship(rel.id)}>
                      ×
                    </ActionIcon>
                  </Group>
                </Card>
              );
            })
          )}
        </Stack>
      )}

      <Group>
        <Button onClick={handleSave} loading={saving}>
          {entityId ? 'Save Changes' : 'Create Entity'}
        </Button>
      </Group>

      {/* Delete confirmation modal */}
      <Modal opened={deleteModalOpen} onClose={() => setDeleteModalOpen(false)} title="Delete Entity" size="sm">
        <Text size="sm" mb="md">
          Delete "{entity.name}"? All chunks and embeddings will be removed. This cannot be undone.
        </Text>
        <Group justify="flex-end">
          <Button variant="subtle" onClick={() => setDeleteModalOpen(false)}>Cancel</Button>
          <Button color="red" onClick={handleDelete} loading={deleting}>Delete</Button>
        </Group>
      </Modal>

      {/* Add relationship modal */}
      <Modal opened={showAddRel} onClose={() => setShowAddRel(false)} title="Add Relationship" size="sm">
        <Stack>
          <Select
            label="Related Entity"
            data={allEntities.map((e) => ({ value: e.id, label: `${e.name} (${e.entityType})` }))}
            value={newRelTarget}
            onChange={setNewRelTarget}
            searchable
          />
          <Select
            label="Relationship Type"
            data={RELATIONSHIP_TYPES}
            value={newRelType}
            onChange={(v) => setNewRelType(v ?? 'related_to')}
          />
          <Group justify="flex-end">
            <Button variant="subtle" onClick={() => setShowAddRel(false)}>Cancel</Button>
            <Button onClick={handleAddRelationship} disabled={!newRelTarget}>Add</Button>
          </Group>
        </Stack>
      </Modal>
    </Stack>
  );
}
