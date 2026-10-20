import type { Schemas } from '@/api/client';

const daysOfWeek: Schemas['DayOfWeek'][] = [
    'Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday',
];

/** A calendar date as the API expects it (DateOnly): "2026-10-20", in the phone's own time zone. */
export function toIsoDate(date: Date): string {
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${date.getFullYear()}-${month}-${day}`;
}

/** The next match day of the group, counting today. */
export function nextMatchDate(matchDay: Schemas['DayOfWeek'], today = new Date()): string {
    const daysAhead = (daysOfWeek.indexOf(matchDay) - today.getDay() + 7) % 7;
    const date = new Date(today.getFullYear(), today.getMonth(), today.getDate() + daysAhead);
    return toIsoDate(date);
}

/** "2026-10-20" → "20/10/2026". */
export function formatDate(isoDate: string): string {
    const [year, month, day] = isoDate.split('-');
    return `${day}/${month}/${year}`;
}
