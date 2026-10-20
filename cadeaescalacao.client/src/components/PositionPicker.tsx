import { Chip, Group, Input } from '@mantine/core';
import { positionLabels, positions, type Position } from '@/app/labels';

/** Choose one or more positions (the API requires at least one). */
export function PositionPicker({
    value,
    onChange,
    label = 'Posições',
}: {
    value: Position[];
    onChange: (positions: Position[]) => void;
    label?: string;
}) {
    return (
        <Input.Wrapper label={label}>
            <Chip.Group multiple value={value} onChange={(selected) => onChange(selected as Position[])}>
                <Group gap="xs" mt={4}>
                    {positions.map((position) => (
                        <Chip key={position} value={position} size="sm">
                            {positionLabels[position]}
                        </Chip>
                    ))}
                </Group>
            </Chip.Group>
        </Input.Wrapper>
    );
}
