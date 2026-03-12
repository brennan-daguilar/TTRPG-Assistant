import { AppShell, NavLink, Group, Title, ActionIcon, useMantineColorScheme } from '@mantine/core';
import { useDisclosure } from '@mantine/hooks';
import { Outlet, useNavigate, useLocation } from 'react-router-dom';

export default function Shell() {
  const [opened, { toggle }] = useDisclosure();
  const navigate = useNavigate();
  const location = useLocation();
  const { toggleColorScheme } = useMantineColorScheme();

  const navItems = [
    { label: 'Chat', path: '/' },
    { label: 'Knowledge Base', path: '/entities' },
    { label: 'Import', path: '/import' },
    { label: 'Proposals', path: '/proposals' },
  ];

  return (
    <AppShell
      header={{ height: 50 }}
      navbar={{ width: 220, breakpoint: 'sm', collapsed: { mobile: !opened } }}
      padding="md"
    >
      <AppShell.Header>
        <Group h="100%" px="md" justify="space-between">
          <Group>
            <ActionIcon variant="subtle" onClick={toggle} hiddenFrom="sm" aria-label="Toggle nav">
              ☰
            </ActionIcon>
            <Title order={4}>TTRPG Assistant</Title>
          </Group>
          <ActionIcon variant="subtle" onClick={toggleColorScheme} aria-label="Toggle color scheme">
            ◐
          </ActionIcon>
        </Group>
      </AppShell.Header>

      <AppShell.Navbar p="xs">
        {navItems.map((item) => (
          <NavLink
            key={item.path}
            label={item.label}
            active={location.pathname === item.path}
            onClick={() => {
              navigate(item.path);
              toggle();
            }}
          />
        ))}
      </AppShell.Navbar>

      <AppShell.Main>
        <Outlet />
      </AppShell.Main>
    </AppShell>
  );
}
