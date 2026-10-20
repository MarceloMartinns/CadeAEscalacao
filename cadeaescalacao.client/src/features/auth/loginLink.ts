export interface LoginLink {
    email: string;
    code: string;
}

/** Link of the e-mail (ADR-022): /login#email=...&code=123456. The fragment never reaches a server. */
export function parseLoginLink(hash: string): LoginLink | null {
    const params = new URLSearchParams(hash.replace(/^#/, ''));
    const email = params.get('email');
    const code = params.get('code');
    return email && code && /^[0-9]{6}$/.test(code) ? { email, code } : null;
}
