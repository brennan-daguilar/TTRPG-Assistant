import { useState, useEffect } from 'react';
import { Stack, Title, SegmentedControl, Group, Text, Loader, Button } from '@mantine/core';
import { proposals as api, NoteProposal } from '../api/client';
import ProposalCard from '../components/chat/ProposalCard';

export default function ProposalsPage() {
  const [items, setItems] = useState<NoteProposal[]>([]);
  const [loading, setLoading] = useState(true);
  const [statusFilter, setStatusFilter] = useState('pending');

  const load = () => {
    setLoading(true);
    api.list(statusFilter === 'all' ? undefined : statusFilter)
      .then(setItems)
      .finally(() => setLoading(false));
  };

  useEffect(load, [statusFilter]);

  const pendingIds = items.filter((p) => p.status === 'pending').map((p) => p.id);

  const handleBulkApprove = async () => {
    if (pendingIds.length === 0) return;
    await api.bulkApprove(pendingIds);
    load();
  };

  const handleBulkReject = async () => {
    if (pendingIds.length === 0) return;
    await api.bulkReject(pendingIds);
    load();
  };

  return (
    <Stack maw={800}>
      <Group justify="space-between">
        <Title order={3}>Note Proposals</Title>
        {statusFilter === 'pending' && pendingIds.length > 0 && (
          <Group gap="xs">
            <Button size="xs" color="green" onClick={handleBulkApprove}>Approve All ({pendingIds.length})</Button>
            <Button size="xs" color="red" variant="outline" onClick={handleBulkReject}>Reject All</Button>
          </Group>
        )}
      </Group>

      <SegmentedControl
        value={statusFilter}
        onChange={setStatusFilter}
        data={[
          { label: 'Pending', value: 'pending' },
          { label: 'Approved', value: 'approved' },
          { label: 'Rejected', value: 'rejected' },
          { label: 'All', value: 'all' },
        ]}
      />

      {loading ? (
        <Loader mx="auto" mt="md" />
      ) : items.length === 0 ? (
        <Text c="dimmed" ta="center" mt="md">No proposals found</Text>
      ) : (
        <Stack gap="sm">
          {items.map((p) => (
            <ProposalCard key={p.id} proposal={p} onResolved={load} />
          ))}
        </Stack>
      )}
    </Stack>
  );
}
