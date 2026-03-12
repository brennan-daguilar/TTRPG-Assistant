import { useState } from 'react';
import { Card, Group, Text, Badge, Button, Textarea, Stack } from '@mantine/core';
import { proposals as api } from '../../api/client';
import type { NoteProposal } from '../../api/client';

interface ProposalCardProps {
  proposal: NoteProposal;
  onResolved?: () => void;
}

export default function ProposalCard({ proposal, onResolved }: ProposalCardProps) {
  const [status, setStatus] = useState(proposal.status);
  const [editing, setEditing] = useState(false);
  const [editedContent, setEditedContent] = useState(proposal.proposedContent);
  const [loading, setLoading] = useState(false);

  const handleApprove = async () => {
    setLoading(true);
    try {
      const content = editing ? editedContent : undefined;
      await api.approve(proposal.id, content);
      setStatus('approved');
      onResolved?.();
    } finally {
      setLoading(false);
    }
  };

  const handleReject = async () => {
    setLoading(true);
    try {
      await api.reject(proposal.id);
      setStatus('rejected');
      onResolved?.();
    } finally {
      setLoading(false);
    }
  };

  const isResolved = status !== 'pending';
  const name = proposal.proposalType === 'create' ? proposal.newEntityName : proposal.targetEntityName;
  const label = proposal.proposalType === 'create' ? 'Create' : 'Update';

  return (
    <Card padding="sm" radius="sm" withBorder>
      <Group justify="space-between" mb="xs">
        <Group gap="xs">
          <Badge size="sm" color={proposal.proposalType === 'create' ? 'green' : 'blue'}>
            {label}
          </Badge>
          <Text size="sm" fw={500}>{name}</Text>
        </Group>
        {isResolved && (
          <Badge size="sm" color={status === 'approved' ? 'green' : 'red'}>
            {status}
          </Badge>
        )}
      </Group>

      {proposal.description && (
        <Text size="xs" c="dimmed" mb="xs">{proposal.description}</Text>
      )}

      {editing ? (
        <Textarea
          value={editedContent}
          onChange={(e) => setEditedContent(e.currentTarget.value)}
          minRows={6}
          autosize
          styles={{ input: { fontFamily: 'monospace', fontSize: '12px' } }}
          mb="xs"
        />
      ) : (
        <Text
          size="xs"
          style={{ fontFamily: 'monospace', whiteSpace: 'pre-wrap', maxHeight: 200, overflow: 'auto' }}
          bg="dark.8"
          p="xs"
          mb="xs"
        >
          {proposal.proposedContent}
        </Text>
      )}

      {!isResolved && (
        <Group gap="xs">
          <Button size="xs" color="green" onClick={handleApprove} loading={loading}>
            Approve
          </Button>
          <Button size="xs" variant="light" onClick={() => setEditing(!editing)}>
            {editing ? 'Preview' : 'Edit'}
          </Button>
          <Button size="xs" color="red" variant="outline" onClick={handleReject} loading={loading}>
            Reject
          </Button>
        </Group>
      )}
    </Card>
  );
}
