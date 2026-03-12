import { useState } from 'react';
import { Grid, Button, Group, Title } from '@mantine/core';
import EntityList from '../components/entities/EntityList';
import EntityEditor from '../components/entities/EntityEditor';

export default function EntitiesPage() {
  const [selectedId, setSelectedId] = useState<string | undefined>();
  const [refreshKey, setRefreshKey] = useState(0);

  const handleNew = () => setSelectedId(undefined);
  const handleSaved = () => setRefreshKey((k) => k + 1);
  const handleDeleted = () => {
    setSelectedId(undefined);
    setRefreshKey((k) => k + 1);
  };

  return (
    <>
      <Group justify="space-between" mb="md">
        <Title order={3}>Knowledge Base</Title>
        <Button variant="light" onClick={handleNew}>+ New Entity</Button>
      </Group>
      <Grid>
        <Grid.Col span={{ base: 12, md: 4 }}>
          <EntityList key={refreshKey} onSelect={setSelectedId} selectedId={selectedId} />
        </Grid.Col>
        <Grid.Col span={{ base: 12, md: 8 }}>
          <EntityEditor entityId={selectedId} onSaved={handleSaved} onDeleted={handleDeleted} />
        </Grid.Col>
      </Grid>
    </>
  );
}
