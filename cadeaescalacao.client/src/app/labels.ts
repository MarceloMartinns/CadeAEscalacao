import type { Schemas } from '@/api/client';

// Every text the screens show for a value of the API, in Portuguese (ADR-018).

/** Position is a flags enum in the API, so the OpenAPI document describes it only as text. */
export type Position = 'Goalkeeper' | 'Defender' | 'Midfielder' | 'Forward';

export const positions: Position[] = ['Goalkeeper', 'Defender', 'Midfielder', 'Forward'];

export const positionLabels: Record<Position, string> = {
    Goalkeeper: 'Goleiro',
    Defender: 'Defesa',
    Midfielder: 'Meio',
    Forward: 'Ataque',
};

export const playerTypeLabels: Record<Schemas['PlayerType'], string> = {
    Regular: 'Mensalista',
    Substitute: 'Suplente',
};

export const entryStatusLabels: Record<Schemas['EntryStatus'], string> = {
    Pending: 'Não respondeu',
    Confirmed: 'Confirmado',
    Declined: 'Fora',
};

export const listStatusLabels: Record<Schemas['WeeklyListStatus'], string> = {
    Open: 'Lista aberta',
    Closed: 'Lista fechada',
    Cancelled: 'Jogo cancelado',
};

export const lineupStatusLabels: Record<NonNullable<Schemas['LineupStatus']>, string> = {
    WaitingForPartner: 'Esperando a dupla do dia',
    Drafting: 'Professor montando os times',
    WaitingForPartnerChoice: 'Dupla escolhendo o time',
    Completed: 'Times definidos',
};

export const dayOfWeekLabels: Record<Schemas['DayOfWeek'], string> = {
    Sunday: 'Domingo',
    Monday: 'Segunda',
    Tuesday: 'Terça',
    Wednesday: 'Quarta',
    Thursday: 'Quinta',
    Friday: 'Sexta',
    Saturday: 'Sábado',
};

/** "2026-10-20" (DateOnly of the API) → "ter., 20/10". Read as a calendar date, not converted to another time zone. */
export function formatMatchDate(isoDate: string): string {
    const [year, month, day] = isoDate.split('-').map(Number);
    return new Date(year, month - 1, day).toLocaleDateString('pt-BR', {
        weekday: 'short',
        day: '2-digit',
        month: '2-digit',
    });
}
