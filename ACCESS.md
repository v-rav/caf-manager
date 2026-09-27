# Access & Login

The CAF Operations Portal uses **custom logins** (username + password). The whole app sits behind a
sign-in page; there is no anonymous access to the UI.

## First login (default admin)
On first run the portal seeds one administrator:

| Username | Password | Role |
|----------|----------|------|
| `admin`  | `admin`  | Admin |

1. Open the portal (e.g. `http://localhost:5080`).
2. Sign in with `admin` / `admin`.
3. You are **required to set a new password** before continuing (minimum 6 characters).

> ⚠ Change the default password immediately. The default only exists to bootstrap the first admin and is
> forced to change on first login.

## Roles
| Role | Purpose |
|------|---------|
| **Admin** | Manages users and configuration; full access. |
| **Lead** | Read-across governance / leadership views. |
| **SA** | Owns and updates nominations (gates, blockers). |

Roles gate features going forward (e.g. the **Users & access** panel and admin operations are Admin-only).

## Managing users (Admin only)
**Configuration → Users & access**:
- **Add user** — enter username, display name, role, and a temporary password. The new user must change
  it at their first login.
- **Change role** — per-user dropdown (Admin / Lead / SA).
- **Enable / Disable** — disabled users cannot sign in. (You cannot disable your own account.)
- **Reset password** — sets a temporary password and forces a change at next login.

## Changing your own password
Top-right **user menu → Change password**.

## How it works (technical)
- **Cookie auth** — an HttpOnly, same-origin session cookie (`caf.auth`), 8-hour sliding expiry. No tokens
  are stored in the browser/JS.
- **Password storage** — PBKDF2 (SHA-256, 100k iterations, per-user salt). Plain passwords are never stored.
- **API** — unauthenticated API calls return `401`; insufficient role returns `403`. `GET /api/auth/me`
  returns the current user.
- **Endpoints** — `POST /api/auth/login`, `POST /api/auth/logout`, `GET /api/auth/me`,
  `POST /api/auth/change-password`; `GET/POST/PUT /api/users` and `POST /api/users/{id}/reset-password`
  (Admin only).

## Troubleshooting
- **Stuck on the sign-in page after entering correct details** — hard-reload the page (Ctrl+F5); the
  session cookie is set on login.
- **Forgot the admin password** — an Admin can reset any user from Users & access. If *no* admin can sign
  in, the default admin is only re-seeded on a database with **no users**; contact the maintainer to reset.
- **"Must reset" badge** on a user — that account still has a temporary password and will be prompted to
  change it at next login.

## Not yet (planned)
- API-level lockdown of the existing data endpoints (today the **UI** is gated; data endpoints are opened
  up until the gate engine wires per-write `updated-by`).
- Password complexity policy / lockout after failed attempts.
