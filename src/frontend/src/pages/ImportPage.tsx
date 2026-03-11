import { useState, useCallback } from 'react';
import { Stack, Title, Textarea, TextInput, Select, TagsInput, Button, Text, Paper, Group, Notification } from '@mantine/core';
import { imports } from '../api/client';

const ENTITY_TYPES = ['Character', 'Location', 'Faction', 'Item', 'Event', 'Lore'];

export default function ImportPage() {
  const [content, setContent] = useState('');
  const [name, setName] = useState('');
  const [entityType, setEntityType] = useState<string>('Lore');
  const [tags, setTags] = useState<string[]>([]);
  const [importing, setImporting] = useState(false);
  const [result, setResult] = useState<{ success: boolean; message: string } | null>(null);

  const handleImport = useCallback(async () => {
    if (!content.trim()) return;
    setImporting(true);
    setResult(null);
    try {
      const entity = await imports.markdown({
        content,
        name: name || undefined,
        entityType,
        tags,
      });
      setResult({ success: true, message: `Created "${entity.name}" successfully.` });
      setContent('');
      setName('');
      setTags([]);
    } catch (err) {
      setResult({ success: false, message: `Import failed: ${err}` });
    } finally {
      setImporting(false);
    }
  }, [content, name, entityType, tags]);

  const handleFileDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    const file = e.dataTransfer.files[0];
    if (file) {
      const reader = new FileReader();
      reader.onload = () => {
        setContent(reader.result as string);
        if (!name) setName(file.name.replace(/\.[^.]+$/, ''));
      };
      reader.readAsText(file);
    }
  }, [name]);

  return (
    <Stack maw={800}>
      <Title order={3}>Import Content</Title>

      {result && (
        <Notification
          color={result.success ? 'green' : 'red'}
          onClose={() => setResult(null)}
        >
          {result.message}
        </Notification>
      )}

      <Group grow>
        <TextInput label="Name" placeholder="Auto-detected from content" value={name} onChange={(e) => setName(e.currentTarget.value)} />
        <Select label="Entity Type" data={ENTITY_TYPES} value={entityType} onChange={(v) => setEntityType(v ?? 'Lore')} />
      </Group>

      <TagsInput label="Tags" value={tags} onChange={setTags} />

      <Paper
        p="md"
        withBorder
        style={{ borderStyle: 'dashed' }}
        onDragOver={(e) => e.preventDefault()}
        onDrop={handleFileDrop}
      >
        <Textarea
          label="Content (Markdown)"
          placeholder="Paste or drop a markdown file here..."
          minRows={15}
          autosize
          value={content}
          onChange={(e) => setContent(e.currentTarget.value)}
          styles={{ input: { fontFamily: 'monospace' } }}
        />
      </Paper>

      <Button onClick={handleImport} loading={importing} disabled={!content.trim()}>
        Import
      </Button>
    </Stack>
  );
}
